using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// One match, played to the end with nobody watching, and the two files that
/// fall out of it: the rolling per-tick hash trace and the landmark table.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is instant-resolve, and instant-resolve is not a mode.</b> Nothing
/// here asks the match for a snapshot -- that is the whole of the difference
/// between a headless run and a watched one, and it is the absence of a call
/// rather than a flag, a branch or a second implementation. The loop below is
/// the same <see cref="Match.Advance(int, IMatchEvents)"/> the view drives,
/// with one tick per call because a per-tick trace needs the hash of every
/// tick, and with a listener attached because the landmark table is derived
/// from the event stream.
/// </para>
/// <para>
/// <b>The caller opened the file; the simulation is handed bytes.</b> Nothing
/// in the simulation assembly can open anything -- <c>System.IO</c> is a banned
/// namespace there and the build gate scans the compiled image for it -- so
/// reading a replay off disk is this program's job and parsing it is the
/// simulation's.
/// </para>
/// </remarks>
internal sealed class HeadlessRun
{
    private HeadlessRun(
        ReplayBundle bundle,
        Restaging? restaging,
        MatchResult result,
        string trace,
        Landmarks landmarks)
    {
        Bundle = bundle;
        Restaging = restaging;
        Result = result;
        Trace = trace;
        Landmarks = landmarks;
    }

    /// <summary>The record that was replayed, as it came off the disk.</summary>
    public ReplayBundle Bundle { get; }

    /// <summary>
    /// What was set aside to run it, or null where nothing was and this is an
    /// ordinary replay. Whatever prints a run reads this: a restaging's outcome
    /// is not the record's result and must never be shown as though it were.
    /// </summary>
    public Restaging? Restaging { get; }

    /// <summary>Leaked, total, final tick, and the final rolling hash.</summary>
    public MatchResult Result { get; }

    /// <summary>One line per tick, from tick zero, with no trailing newline.</summary>
    public string Trace { get; }

    /// <summary>The interesting ticks, as the event stream reported them.</summary>
    public Landmarks Landmarks { get; }

    /// <summary>
    /// Reads the bundle, checks it through the replay gate, and runs it to the
    /// end.
    /// </summary>
    public static HeadlessRun Of(byte[] bundleBytes, string unitsText, string upgradesText, string rulesText)
    {
        UnitTypeTable types = Roster(unitsText, upgradesText);
        Ruleset rules = Ruleset.Parse(rulesText);
        ReplayBundle bundle = ReplayBundle.FromBytes(bundleBytes);

        // The replay gate, not the read gate: the simulation version, the
        // content hash and the map hash all have to be this build's, or the
        // record is refused by name. A trace produced under a ruleset the
        // record was not made against would be a confidently wrong answer that
        // still validates.
        Match match = bundle.Replay(types, rules);

        return Play(bundle, restaging: null, match);
    }

    /// <summary>
    /// Reads the bundle and runs its defense and wave under today's rules,
    /// whatever it was recorded under.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what keeps a permanent record playable across a simulation
    /// version bump.</b> <c>content/golden/</c> holds one bundle per defense
    /// record format version that ever shipped, and those bundles can never be
    /// made again -- the writer emits the current version and only the current
    /// version. A bump retires every one of them for <see cref="Of"/>, and
    /// retiring the only evidence that a reader branch still reads is not a
    /// thing a version bump should be able to do.
    /// </para>
    /// <para>
    /// Nothing is weakened by asking the question this way. A golden is evidence
    /// about a <i>reader</i>: that these bytes still parse into that defense and
    /// that wave. Restaging parses them exactly as replaying does and runs the
    /// result to a pinned outcome; the only check it sets aside is "were these
    /// the same rules?", which is a question about a competitive record and not
    /// about a reader branch. And it is set aside by name -- the
    /// <see cref="Sim.Restaging"/> says so in the file it produces -- rather
    /// than by a gate quietly not running.
    /// </para>
    /// </remarks>
    public static HeadlessRun Restaged(
        byte[] bundleBytes,
        string unitsText,
        string upgradesText,
        string rulesText)
    {
        UnitTypeTable types = Roster(unitsText, upgradesText);
        Ruleset rules = Ruleset.Parse(rulesText);
        ReplayBundle bundle = ReplayBundle.FromBytes(bundleBytes);
        Restaging restaging = bundle.RestageUnderCurrentRules(types, rules);

        return Play(bundle, restaging, restaging.Match);
    }

    /// <summary>
    /// The roster a bundle is run against: the table, with the ladder folded into
    /// its content hash.
    /// </summary>
    /// <remarks>
    /// <b>Which ladder a golden is restaged against does not matter, and that is
    /// by design rather than by luck.</b>
    /// <see cref="ReplayBundle.RestageUnderCurrentRules"/> enforces the map hash
    /// alone and skips the content-hash gate on purpose, so a restaging never
    /// asks what roster -- or what ladder -- a record was made with.
    /// <see cref="Of"/> is the other case: there the folded hash is exactly what
    /// the gate compares, so the ladder handed in has to be the one the record
    /// was recorded against.
    /// </remarks>
    private static UnitTypeTable Roster(string unitsText, string upgradesText)
    {
        UnitTypeTable types = UnitTypeTable.Parse(unitsText);

        return types.WithLadder(UpgradeLadder.Parse(upgradesText, types));
    }

    /// <summary>The tick loop, which is the same one either side of the gate.</summary>
    private static HeadlessRun Play(ReplayBundle bundle, Restaging? restaging, Match match)
    {
        var landmarks = new Landmarks();
        var trace = new StringBuilder();

        trace.Append(GoldenTrace.Line(0, match.StateHash));

        while (!match.IsFinished)
        {
            landmarks.EnteringTick(match.Tick + 1);
            match.Advance(1, landmarks);
            trace.Append('\n').Append(GoldenTrace.Line(match.Tick, match.StateHash));
        }

        return new HeadlessRun(bundle, restaging, match.Result(), trace.ToString(), landmarks);
    }

    /// <summary>
    /// The defense's id and the reader branch its bytes actually went through.
    /// </summary>
    /// <remarks>
    /// The format version is printed rather than assumed because the historical
    /// versions are replayed forever: <c>content/golden/</c> holds one bundle per
    /// defense format version that ever shipped, and this line is how the file a
    /// run produces says which branch read it. A golden that quietly started
    /// being read through a different branch would otherwise look identical.
    /// </remarks>
    public string DefenseLine() =>
        "defense    "
        + Bundle.GhostId.ToString()
        + ", read at defense record format "
        + Bundle.Ghost.Header.FormatVersion.ToString(PlainText.Culture);

    /// <summary>
    /// The provenance block both generated files carry: the record's own header,
    /// its seed, the ids of the two records inside it, and what it came to.
    /// </summary>
    /// <remarks>
    /// A restaging's label goes at the top of it, so a trace or a landmark table
    /// that is not a replay's says so in its own bytes rather than only in the
    /// terminal of whoever produced it.
    /// </remarks>
    private string[] Provenance()
    {
        var lines = new List<string>();

        if (Restaging is not null)
        {
            lines.Add("  restaged   " + Restaging.ToString());
        }

        lines.Add("  " + Bundle.Header.ToString());
        lines.Add("  seed       " + Bundle.Seed.ToString(PlainText.Culture));
        lines.Add("  " + DefenseLine());
        lines.Add("  wave       " + Bundle.WaveId.ToString());
        lines.Add("  " + Summary());

        return lines.ToArray();
    }

    /// <summary>What a person reads: the result triple and the final hash.</summary>
    public string Summary() =>
        "result     "
        + Result.Leaked.ToString(PlainText.Culture)
        + " of "
        + Result.Total.ToString(PlainText.Culture)
        + " leaked, tick "
        + Result.FinalTick.ToString(PlainText.Culture)
        + " ("
        + (Result.FinalTick / Match.TicksPerSecond).ToString(PlainText.Culture)
        + " seconds at "
        + Match.TicksPerSecond.ToString(PlainText.Culture)
        + " ticks a second), state "
        + Result.RollingStateHash.ToString();

    /// <summary>
    /// The hash trace as the committed file: the prose that says what it is,
    /// what run produced it, and then a line per tick.
    /// </summary>
    /// <remarks>
    /// The run is named by what is intrinsic to it -- the record's own header,
    /// its seed, the ids of the two records inside it -- and never by the paths
    /// it was invoked with. A file whose bytes depended on how somebody spelled
    /// an argument could not be compared against a committed copy by anything.
    /// </remarks>
    public string TraceFile() =>
        PlainText.File(
            new[]
            {
                "The rolling per-tick state hash of one match, as a real run produced it.",
                "Regenerate it with tools/run-headless-match.ps1 -Regenerate; never edit it by hand.",
                string.Empty,
                "THIS IS A GENERATED FILE AND IT IS COMMITTED ON PURPOSE. It is the oracle the",
                "build gate compares a live run against, one tick at a time, and it is the trace",
                "the engine-side parity run diffs its own against. It is deliberately not produced",
                "by whatever is checking it -- a trace written by the run it validates is a test",
                "that cannot fail.",
                string.Empty,
                "WHAT IS HASHED IS INTERNAL SIMULATION STATE, NOT THE SNAPSHOT. The fold covers",
                "the raw Q32.32 remainder under every creep's distance, the position of the one",
                "dice stream, the running count of target-selection ties, every tower's",
                "cooldown counter and the damage each projectile in flight is carrying. Those",
                "are exactly the fields a view never needs and exactly the ones likeliest to",
                "drift, so a divergence in something nothing draws is still caught here.",
                string.Empty,
                "IT IS COMPARED PER TICK, NOT AT THE END. An end-of-match comparison says a run",
                "diverged; this says which tick it diverged on, before the difference has had",
                "the rest of the match to contaminate everything downstream.",
                string.Empty,
                "The run this came from:",
                string.Empty,
            }
            .Concat(Provenance())
            .Concat(new[]
            {
                string.Empty,
                "Any of those moving moves every line below it. That is the point: the trace is",
                "retired loudly rather than silently comparing a run against a different match.",
                string.Empty,
                "   tick  state hash",
            })
            .ToArray(),
            Trace);

    /// <summary>The landmark table as the committed file.</summary>
    public string LandmarkFile() =>
        PlainText.File(
            new[]
            {
                "The interesting ticks of one match, as a real run reported them.",
                "Regenerate it with tools/run-headless-match.ps1 -Regenerate; never edit it by hand.",
                string.Empty,
                "THIS IS A GENERATED FILE AND IT IS COMMITTED ON PURPOSE. These are the tick",
                "numbers nobody knows until the match runs, and the sit-down checklist is written",
                "against them -- which is why it can say \"drag to 412 and back to 400\" instead of",
                "\"hunt for the moment\". Regenerating it after a content change is a diff of four",
                "rows, so a checklist pointed at a moment that has moved goes stale loudly.",
                string.Empty,
                "EVERY ROW CAME OUT OF THE EVENT STREAM. The run that produced this pulled no",
                "snapshots and inspected nothing: it listened. A moment the simulation does not",
                "report is a moment that cannot appear here, which is deliberate -- inferring one",
                "from the outside would mean a second copy of a rule, living in a program that",
                "would not be rebuilt when the rule changed.",
                string.Empty,
                "The run this came from:",
                string.Empty,
            }
            .Concat(Provenance())
            .Concat(new[]
            {
                string.Empty,
                "        landmark                 tick    who  other",
                string.Empty,
                "tick  : the tick the moment happened on",
                "who   : the entity it happened to -- a creep, or a projectile",
                "other : the other entity involved, or zero where there is only one",
            })
            .ToArray(),
            Landmarks.ToText());
}
