namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a build phase, plus the wider roster the
/// draw assertions need.
/// </summary>
/// <remarks>
/// <para>
/// The tests open the files and hand the simulation text, exactly as
/// <see cref="TheRun"/>, <see cref="TheRuleset"/> and <see cref="TheSchedule"/>
/// do.
/// </para>
/// <para>
/// <b>Why a wider roster.</b> The committed table has two creeps in it and the
/// committed offering carries two options, so every round of a committed run
/// offers both of them and "drawn fresh each round" is a claim nothing there
/// could contradict. Six creeps against three options makes the draw a draw,
/// which is what an assertion about it needs. The extra rows are appended to
/// the committed table rather than replacing it, so everything the schedule
/// names still resolves.
/// </para>
/// </remarks>
public static class TheBuild
{
    /// <summary>How many ordinary options the wide ruleset offers.</summary>
    public const int WideOrdinary = 3;

    /// <summary>
    /// Four more creeps, at four prices, appended to the committed table. Ids
    /// ascend past the committed four; every one of them walks, so every one is
    /// something the offering can draw.
    /// </summary>
    private const string MoreCreeps = """
        unit   5   drifter  moving  1500   100   0  0  0  0  0  0  none  0  12  20  none  arcane    0
        unit   6   lancer   moving  1800   120   0  0  0  0  0  0  none  0  12  25  none  swift     0
        unit   7   bulwark  moving  3000    60   0  0  0  0  0  0  none  0  12  35  none  armoured  0
        unit   8   wisp     moving   800   200   0  0  0  0  0  0  none  0  12  12  none  arcane    0
        """;

    /// <summary>The committed table plus four more creeps: six that walk in all.</summary>
    public static UnitTypeTable WideTypes() =>
        UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile) + "\n" + MoreCreeps + "\n");

    /// <summary>The committed ruleset with the offering's ordinary count moved.</summary>
    public static Ruleset RulesOffering(int ordinary) =>
        Ruleset.Parse(TheRuleset.Replace(
            TheRuleset.CommittedText(),
            "offering        2         3",
            "offering        " + ordinary.ToString(System.Globalization.CultureInfo.InvariantCulture) + "         3"));

    /// <summary>A run over the wide roster, at whatever ordinary count is wanted.</summary>
    public static Run Wide(
        int waves = Run.DefaultWaves,
        int fieldSize = 4,
        int ordinary = WideOrdinary,
        ulong seed = TheRun.Seed)
    {
        UnitTypeTable types = WideTypes();
        Ruleset rules = RulesOffering(ordinary);

        return new Run(
            TheMatch.Map(),
            rules,
            types,
            TheSchedule.Committed(types),
            TheRun.Pool(types),
            seed,
            waves,
            fieldSize);
    }

    /// <summary>A run over the committed content, offering and all.</summary>
    public static Run Committed(
        int waves = Run.DefaultWaves,
        int fieldSize = 4,
        ulong seed = TheRun.Seed) =>
        TheRun.Fresh(waves, fieldSize, deathEndsTheRun: true, seed: seed);

    /// <summary>The defense that stands while a build phase decides what is sent.</summary>
    public static TowerLayout Defense(UnitTypeTable types) => TheMatch.Layout(types);

    /// <summary>Every option on a round's menu, as the pair a decision names.</summary>
    public static (OptionKind Kind, int Id)[] Named(Offering offering) =>
        offering.Options.Select(option => (option.Kind, option.Id)).ToArray();

    /// <summary>The first thing on a round's menu, which is always an ordinary option.</summary>
    public static BuildPhase TakeFirst(Offering offering, params WaveSlot[] slots) =>
        BuildPhase.Of(offering.Options[0].Kind, offering.Options[0].Id, slots);
}
