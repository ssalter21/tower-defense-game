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
    private HeadlessRun(ReplayBundle bundle, MatchResult result, string trace, Landmarks landmarks)
    {
        Bundle = bundle;
        Result = result;
        Trace = trace;
        Landmarks = landmarks;
    }

    /// <summary>The record that was replayed, as it came off the disk.</summary>
    public ReplayBundle Bundle { get; }

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
    public static HeadlessRun Of(byte[] bundleBytes, string unitsText)
    {
        UnitTypeTable types = UnitTypeTable.Parse(unitsText);
        ReplayBundle bundle = ReplayBundle.FromBytes(bundleBytes);

        // The replay gate, not the read gate: the simulation version, the
        // content hash and the map hash all have to be this build's, or the
        // record is refused by name. A trace produced under a ruleset the
        // record was not made against would be a confidently wrong answer that
        // still validates.
        Match match = bundle.Replay(types);

        var landmarks = new Landmarks();
        var trace = new StringBuilder();

        trace.Append(GoldenTrace.Line(0, match.StateHash));

        while (!match.IsFinished)
        {
            landmarks.EnteringTick(match.Tick + 1);
            match.Advance(1, landmarks);
            trace.Append('\n').Append(GoldenTrace.Line(match.Tick, match.StateHash));
        }

        return new HeadlessRun(bundle, match.Result(), trace.ToString(), landmarks);
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
                "  " + Bundle.Header.ToString(),
                "  seed       " + Bundle.Seed.ToString(PlainText.Culture),
                "  " + DefenseLine(),
                "  wave       " + Bundle.WaveId.ToString(),
                "  " + Summary(),
                string.Empty,
                "Any of those moving moves every line below it. That is the point: the trace is",
                "retired loudly rather than silently comparing a run against a different match.",
                string.Empty,
                "   tick  state hash",
            },
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
                "  " + Bundle.Header.ToString(),
                "  seed       " + Bundle.Seed.ToString(PlainText.Culture),
                "  " + DefenseLine(),
                "  wave       " + Bundle.WaveId.ToString(),
                "  " + Summary(),
                string.Empty,
                "        landmark                 tick    who  other",
                string.Empty,
                "tick  : the tick the moment happened on",
                "who   : the entity it happened to -- a creep, or a projectile",
                "other : the other entity involved, or zero where there is only one",
            },
            Landmarks.ToText());
}
