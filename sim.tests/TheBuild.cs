namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a build phase.
/// </summary>
/// <remarks>
/// <para>
/// The tests open the files and hand the simulation text, exactly as
/// <see cref="TheRun"/>, <see cref="TheRuleset"/> and <see cref="TheSchedule"/>
/// do.
/// </para>
/// <para>
/// <b>The draw assertions are fought over the committed roster.</b> Six walkers
/// against three ordinary options is what makes a round's menu a draw rather
/// than the whole roster read back, and that is what an assertion about drawing
/// needs. Nothing here appends a creep of its own: a synthetic roster standing
/// in for the committed one is a fixture that can be green while the content it
/// stands for cannot draw a menu at all.
/// </para>
/// </remarks>
public static class TheBuild
{
    /// <summary>How many ordinary options the committed ruleset offers.</summary>
    public const int Ordinary = 3;

    /// <summary>The committed ruleset with the offering's ordinary count moved.</summary>
    public static Ruleset RulesOffering(int ordinary) =>
        Ruleset.Parse(TheRuleset.Replace(
            TheRuleset.CommittedText(),
            "offering        3         3",
            "offering        " + ordinary.ToString(System.Globalization.CultureInfo.InvariantCulture) + "         3"));

    /// <summary>A run over the committed roster, at whatever ordinary count is wanted.</summary>
    public static Run Fresh(
        int waves = Run.DefaultWaves,
        int fieldSize = 4,
        int ordinary = Ordinary,
        ulong seed = TheRun.Seed)
    {
        UnitTypeTable types = TheMatch.Types();
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

    /// <summary>The defense that stands while a build phase decides what is sent.</summary>
    public static TowerLayout Defense(UnitTypeTable? types = null) => TheMatch.Layout(types ?? TheMatch.Types());

    /// <summary>Every option on a round's menu, as the pair a decision names.</summary>
    public static (OptionKind Kind, int Id)[] Named(Offering offering) =>
        offering.Options.Select(option => (option.Kind, option.Id)).ToArray();

    /// <summary>The first thing on a round's menu, which is always an ordinary option.</summary>
    public static BuildPhase TakeFirst(Offering offering, params WaveSlot[] slots) =>
        BuildPhase.Of(offering.Options[0].Kind, offering.Options[0].Id, slots);
}
