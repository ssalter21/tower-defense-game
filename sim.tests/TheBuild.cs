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
        Ruleset.Parse(PlantedText.Replace(
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
            Standing(types),
            seed,
            waves,
            fieldSize);
    }

    /// <summary>The defense that stands while a build phase decides what is sent.</summary>
    public static TowerLayout Defense(UnitTypeTable? types = null) => TheMatch.Layout(types ?? TheMatch.Types());

    /// <summary>That same defense as the board a run opens holding.</summary>
    public static Board Standing(UnitTypeTable? types = null) => Board.Of(Defense(types));

    /// <summary>Every option on a round's menu, as the pair a decision names.</summary>
    public static (OptionKind Kind, int Id)[] Named(Offering offering) =>
        offering.Options.Select(option => (option.Kind, option.Id)).ToArray();

    /// <summary>
    /// The first thing on a round's menu, which is always an ordinary option,
    /// filling the slots named here.
    /// </summary>
    /// <remarks>
    /// A decision that names no slot at all is spelled
    /// <see cref="BuyingNothing"/>, so that the two spellings of a wave nobody
    /// paid for are one.
    /// </remarks>
    public static BuildPhase TakeFirst(Offering offering, params WaveSlot[] slots) =>
        BuildPhase.Of(offering.Options[0].Kind, offering.Options[0].Id, slots);

    /// <summary>
    /// A round that takes the first thing on its menu and buys nothing at all.
    /// </summary>
    /// <remarks>
    /// No slot named, so the purse carries into the wave exactly as it came out
    /// of the last one. It is named for that because a round that spends
    /// nothing is one build phase among many and not a way into a run that
    /// never charges: the take is still read off the round's own offering, and
    /// what it unlocks is still what the run may field.
    /// </remarks>
    public static BuildPhase BuyingNothing(Offering offering) =>
        BuildPhase.Of(offering.Options[0].Kind, offering.Options[0].Id);

    /// <summary>A round that takes the first thing on its menu and spends the purse on the creep it unlocks.</summary>
    public static BuildPhase Shopping(Run run) => Shopping(run, run.Purse.Gold);

    /// <summary>
    /// The same round, held to a budget rather than to the whole purse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The take comes first because unlocking happens before buying, so the
    /// creep a round takes is the creep its wave sends -- which is what makes
    /// this the shortest decision that shops at all.
    /// </para>
    /// <para>
    /// A budget short of one body fills no slot rather than borrowing against
    /// it, so a fixture priced beyond what its purse can hold shops for nothing
    /// instead of being refused on the way in.
    /// </para>
    /// </remarks>
    /// <param name="run">The run whose next round is being decided.</param>
    /// <param name="budget">What the round is willing to spend, in gold.</param>
    public static BuildPhase Shopping(Run run, int budget)
    {
        Option first = run.Offering.Options[0];
        int count = budget / run.Costs.PriceOf(Purchase.Unit(first.TypeId));

        return count == 0
            ? BuildPhase.Of(first.Kind, first.Id)
            : BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, count));
    }
}
