using Sim;

namespace Sim.Cli;

/// <summary>
/// How long a run lasts, how wide its field is and whether death ends it: N, K
/// and the flag.
/// </summary>
/// <remarks>
/// <para>
/// <b>They are arguments and no record stamps them.</b> A command stream holds
/// the decisions and the seed they were made under, and the same decisions
/// played against a wider field are a different set of numbers -- so the shape
/// is printed into whatever a run writes down, where a diff can see it, rather
/// than being left to whoever spelled the invocation.
/// </para>
/// <para>
/// <b>Death is a flag rather than a rule</b>, which is what lets a harness ask
/// for N rounds of data out of every run instead of a short one wherever a
/// build failed. It defaults to ending a run, because that is the game.
/// </para>
/// </remarks>
internal readonly struct RunShape
{
    public RunShape(int waves, int fieldSize, bool deathEndsTheRun)
    {
        Waves = waves;
        FieldSize = fieldSize;
        DeathEndsTheRun = deathEndsTheRun;
    }

    /// <summary>N: how many waves the run lasts.</summary>
    public int Waves { get; }

    /// <summary>K: how many opponents each round is resolved against.</summary>
    public int FieldSize { get; }

    /// <summary>Whether health reaching zero stops the run.</summary>
    public bool DeathEndsTheRun { get; }
}

/// <summary>
/// The authored content a run is built out of, parsed once: the board, the
/// tables, the shape, the defense that stands and the wave the canned field
/// sends back.
/// </summary>
/// <remarks>
/// <para>
/// <b>The caller opened seven files; the simulation was handed seven
/// strings.</b> Nothing in the simulation assembly can open anything --
/// <c>System.IO</c> is a banned namespace there and the build gate scans the
/// compiled image for it -- so reading the content is this program's job and
/// parsing it is the simulation's. Every parser here is the same one the tests
/// and the engine call.
/// </para>
/// <para>
/// <b>The defense file is the opponents' and nothing else.</b> It is the wall
/// the canned field stands behind; a run stands whatever its own build phases
/// put on the map, and it opens with nothing there.
/// See <c>docs/adr/0040-a-run-is-authored-as-text-and-compiled-to-a-record.md</c>.
/// </para>
/// <para>
/// <b>The upgrade ladder is parsed, folded into the roster's content hash, held
/// -- and never handed to <see cref="Run"/>.</b> That absence is what makes
/// "the simulation does not enforce the ladder" a property of the code rather
/// than a promise about it, the same move as banning <c>System.IO</c> from the
/// simulation assembly and then scanning the compiled image for it.
/// </para>
/// <para>
/// <b>The field is canned and it stands in for a ghost pool that does not
/// exist.</b> What that means -- one pair of orders, drawn with replacement, so
/// a field of ten is that opponent ten times -- is composed by
/// <see cref="FieldPool.Canned"/> and described there. It is the simulation's
/// answer to how thin a pool may be rather than this reader's, which is why the
/// two files meeting here does not make it this file's decision.
/// </para>
/// </remarks>
internal sealed class RunContent
{
    /// <summary>
    /// The tick a build phase's wave opens on. Everything behind the first
    /// creep follows from the counts: a slot's position is its release order,
    /// so each order stands one spawn interval per creep behind the one above
    /// it.
    /// </summary>
    private const int FirstReleaseTick = 0;

    private readonly HexMap _map;

    private readonly Ruleset _rules;

    private readonly FieldPool _pool;

    private RunContent(
        HexMap map,
        UnitTypeTable types,
        UpgradeLadder ladder,
        Ruleset rules,
        TowerLayout defense,
        WaveScript field)
    {
        _map = map;
        _rules = rules;
        _pool = FieldPool.Canned(defense, field);
        Types = types;
        Ladder = ladder;
    }

    /// <summary>
    /// The roster every creep, cost and offering in the run is read out of, with
    /// the ladder folded into its content hash.
    /// </summary>
    public UnitTypeTable Types { get; }

    /// <summary>
    /// Which unit follows which. Held here and handed to nothing that ticks --
    /// see the remarks on <see cref="RunContent"/>.
    /// </summary>
    public UpgradeLadder Ladder { get; }

    /// <summary>
    /// Parses the seven files a run needs. Order matters: the ladder and the
    /// three tables all check against the roster, and the roster the rest are
    /// read against is the one the ladder has been folded into.
    /// </summary>
    public static RunContent Of(
        string mapText,
        string unitsText,
        string upgradesText,
        string rulesText,
        string defenseText,
        string fieldText)
    {
        UnitTypeTable roster = UnitTypeTable.Parse(unitsText);
        UpgradeLadder ladder = UpgradeLadder.Parse(upgradesText, roster);
        UnitTypeTable types = roster.WithLadder(ladder);

        return new RunContent(
            HexMap.Parse(mapText),
            types,
            ladder,
            Ruleset.Parse(rulesText),
            TowerLayout.Parse(defenseText, types),
            Field(fieldText, types));
    }

    /// <summary>
    /// The canned opponent's wave, refused unless it is a round's worth of
    /// orders rather than a match's.
    /// </summary>
    /// <remarks>
    /// An authored match wave parses here perfectly -- same keyword, same
    /// fields, same table -- and a report swept against one reads exactly like a
    /// real one while separating no creep from any other. What tells the two
    /// apart is the release schedule. A stored round is one column at one
    /// cadence: it opens on tick zero, and each order after it stands one
    /// <see cref="Match.SpawnIntervalTicks"/> per creep behind the order above
    /// it, because a slot's position is its release order. Any other spacing is
    /// a wave nothing in this economy composes.
    /// </remarks>
    /// <remarks>
    /// The check was "every order on tick zero" until #191, which is what a
    /// stored round looked like while a build phase composed what was sent and
    /// not when. It is tighter now rather than looser: tick zero admitted any
    /// number of simultaneous columns, and this admits exactly one arrangement
    /// per set of counts.
    /// See <c>docs/adr/0040-a-run-is-authored-as-text-and-compiled-to-a-record.md</c>.
    /// </remarks>
    private static WaveScript Field(string fieldText, UnitTypeTable types)
    {
        WaveScript field = WaveScript.Parse(RunContentFiles.Field.Option, fieldText, types);

        int due = FirstReleaseTick;

        for (int index = 0; index < field.Count; index++)
        {
            int tick = field.Orders[index].TickOffset;

            if (tick != due)
            {
                throw new UsageException(
                    "--"
                    + RunContentFiles.Field.Option
                    + " names a wave whose order "
                    + (index + 1).ToString(PlainText.Culture)
                    + " releases on tick "
                    + tick.ToString(PlainText.Culture)
                    + " where a build phase would have released it on "
                    + due.ToString(PlainText.Culture)
                    + ". The canned field stands in for a population of stored rounds, and a stored "
                    + "round is one column at one cadence: it opens on tick 0, and every order after it "
                    + "stands one release behind the whole of the order above it, because a slot's "
                    + "position is the order its creeps walk out in. A wave spaced any other way is a "
                    + "whole authored match, which is what --wave means on the 'record' verb -- and "
                    + "swept against, it outspends every opponent it faces and reports a total loss on "
                    + "every row.");
            }

            due += field.Orders[index].Count * Match.SpawnIntervalTicks;
        }

        return field;
    }

    /// <summary>A run on this content, with nothing played into it yet.</summary>
    public Run Fresh(ulong seed, RunShape shape) =>
        new Run(
            _map,
            _rules,
            Types,
            Ladder,
            _pool,
            seed,
            shape.Waves,
            shape.FieldSize,
            shape.DeathEndsTheRun);

    /// <summary>
    /// A sweep over this content: the same map, rules, roster, shape and canned
    /// field a run is played against, with the economy's dials and the
    /// harness's own bounds on top.
    /// </summary>
    /// <remarks>
    /// The dials arrive as <see cref="SweepPlan.AsAuthored"/> where the command
    /// line was not told otherwise, so an unmentioned dial is the ruleset's own
    /// number rather than one this program chose.
    /// </remarks>
    public SweepPlan Sweep(
        RunShape shape,
        ulong firstSeed,
        int runsPerCreep,
        int freeSnapshotsPerRun,
        int snapshotPriceGold,
        int mostCreeps) =>
        new SweepPlan(
            _map,
            _rules,
            Types,
            Ladder,
            _pool,
            firstSeed,
            runsPerCreep,
            shape.Waves,
            shape.FieldSize,
            shape.DeathEndsTheRun,
            freeSnapshotsPerRun,
            snapshotPriceGold,
            mostCreeps);
}
