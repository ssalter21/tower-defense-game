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
/// <b>The caller opened six files; the simulation was handed six strings.</b>
/// Nothing in the simulation assembly can open anything -- <c>System.IO</c> is
/// a banned namespace there and the build gate scans the compiled image for it
/// -- so reading the content is this program's job and parsing it is the
/// simulation's. Every parser here is the same one the tests and the engine
/// call.
/// </para>
/// <para>
/// <b>The field is canned and it stands in for a ghost pool that does not
/// exist.</b> A round is resolved against K opponents drawn from a population
/// of other players' rounds, and there is no such population until runs are
/// stored; until then the population is the one pair of orders this content
/// describes, drawn with replacement, so a field of ten is that opponent ten
/// times. That is a thin pool rather than a missing one, and widening it is a
/// bigger list here and no change anywhere else.
/// </para>
/// </remarks>
internal sealed class RunContent
{
    private readonly HexMap _map;

    private readonly Ruleset _rules;

    private readonly AnchorSchedule _schedule;

    private readonly FieldPool _pool;

    private RunContent(
        HexMap map,
        UnitTypeTable types,
        Ruleset rules,
        AnchorSchedule schedule,
        TowerLayout defense,
        WaveScript wave)
    {
        _map = map;
        _rules = rules;
        _schedule = schedule;
        _pool = FieldPool.Of(new[] { RoundOrders.Of(defense, wave) });
        Types = types;
        Defense = defense;
    }

    /// <summary>The roster every creep, cost and offering in the run is read out of.</summary>
    public UnitTypeTable Types { get; }

    /// <summary>What stands while each of the run's waves is sent.</summary>
    public TowerLayout Defense { get; }

    /// <summary>Parses the six files a run needs. Order matters: the tables check against the roster.</summary>
    public static RunContent Of(
        string mapText,
        string unitsText,
        string rulesText,
        string scheduleText,
        string defenseText,
        string waveText)
    {
        UnitTypeTable types = UnitTypeTable.Parse(unitsText);

        return new RunContent(
            HexMap.Parse(mapText),
            types,
            Ruleset.Parse(rulesText),
            AnchorSchedule.Parse(scheduleText, types),
            TowerLayout.Parse(defenseText, types),
            WaveScript.Parse(waveText, types));
    }

    /// <summary>A run on this content, with nothing played into it yet.</summary>
    public Run Fresh(ulong seed, RunShape shape) =>
        new Run(
            _map,
            _rules,
            Types,
            _schedule,
            _pool,
            seed,
            shape.Waves,
            shape.FieldSize,
            shape.DeathEndsTheRun);

    /// <summary>
    /// A sweep over this content: the same map, rules, roster, shape, defense
    /// and canned field a run is played against, with the economy's dials and
    /// the harness's own bounds on top.
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
        int ordinaryOptionsPerRound,
        int gameChangersPerAnchor,
        int freeSnapshotsPerRun,
        int snapshotPriceGold,
        int mostCreeps) =>
        new SweepPlan(
            _map,
            _rules,
            Types,
            _schedule,
            Defense,
            _pool,
            firstSeed,
            runsPerCreep,
            shape.Waves,
            shape.FieldSize,
            shape.DeathEndsTheRun,
            ordinaryOptionsPerRound,
            gameChangersPerAnchor,
            freeSnapshotsPerRun,
            snapshotPriceGold,
            mostCreeps);
}
