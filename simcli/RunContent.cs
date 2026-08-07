using Sim;

namespace Sim.Cli;

/// <summary>
/// How long a run lasts and how wide its field is: N and K.
/// </summary>
/// <remarks>
/// <b>They are arguments and no record stamps them.</b> A command stream holds
/// the decisions and the seed they were made under, and the same decisions
/// played against a wider field are a different set of numbers -- so the shape
/// is printed into whatever a run writes down, where a diff can see it, rather
/// than being left to whoever spelled the invocation.
/// </remarks>
internal readonly struct RunShape
{
    public RunShape(int waves, int fieldSize)
    {
        Waves = waves;
        FieldSize = fieldSize;
    }

    /// <summary>N: how many waves the run lasts.</summary>
    public int Waves { get; }

    /// <summary>K: how many opponents each round is resolved against.</summary>
    public int FieldSize { get; }
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
        new Run(_map, _rules, Types, _schedule, _pool, seed, shape.Waves, shape.FieldSize);
}
