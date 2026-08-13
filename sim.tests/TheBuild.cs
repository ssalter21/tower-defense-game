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
    /// <summary>
    /// The committed ruleset, which is what a run of this suite plays.
    /// </summary>
    /// <remarks>
    /// It planted an offering ratio into the text before #179 deleted the
    /// offering. The name is kept and the argument ignored so that the call
    /// sites reading a purse or a health pool off "the rules this suite plays"
    /// keep saying that; there is nothing left for them to have planted.
    /// </remarks>
    public static Ruleset RulesOffering(int ordinary) => TheRuleset.Committed();

    /// <summary>How many ordinary options the committed ruleset offered, before it offered none.</summary>
    public const int Ordinary = 3;

    /// <summary>A run over the committed roster and the committed rules.</summary>
    public static Run Fresh(
        int waves = Run.DefaultWaves,
        int fieldSize = 4,
        int ordinary = Ordinary,
        ulong seed = TheRun.Seed,
        bool deathEndsTheRun = true)
    {
        UnitTypeTable types = TheMatch.Types();
        Ruleset rules = RulesOffering(ordinary);

        return new Run(
            TheMatch.Map(),
            rules,
            types,
            TheLadder.Committed(types),
            TheRun.Pool(types),
            seed,
            waves,
            fieldSize,
            deathEndsTheRun);
    }

    /// <summary>The defense the canned opponents of this suite stand behind.</summary>
    public static TowerLayout Defense(UnitTypeTable? types = null) => TheMatch.Layout(types ?? TheMatch.Types());

    /// <summary>
    /// The wall a run of this suite builds for itself, in the order it builds
    /// it: <see cref="Defense"/>'s own cells as place actions, cheapest row
    /// first.
    /// </summary>
    /// <remarks>
    /// A run opens on an empty board, so a scenario that means to reach its
    /// tenth wave has to build one -- there is no authored defense behind it any
    /// more. The cells are read out of the defense file rather than written down
    /// here, because that file is a wall somebody sat down and made cover the
    /// corridor and a second copy of it would be free to drift. The order is by
    /// price, because an opening purse holds one archer and not one mage; the
    /// sort is stable, so towers of one price stay in the order the file wrote
    /// them.
    /// </remarks>
    private static BuildAction[] Wall(UnitTypeTable types) =>
        Defense(types).Towers
            .OrderBy(tower => tower.Type.Cost)
            .Select(tower => BuildAction.Of(ActionKind.Place, tower.Type.Id, tower.Column, tower.Row))
            .ToArray();

    /// <summary>
    /// A round that takes the first thing on its menu, adds the next tower of
    /// the wall where the purse can pay for one, and spends what is left on the
    /// creep the take unlocked.
    /// </summary>
    /// <remarks>
    /// The tower comes off the purse before the wave does, which is the order
    /// the payer walks in, so the wave this composes is one the round can still
    /// afford after building.
    /// </remarks>
    public static BuildPhase Fortifying(Run run)
    {
        BuildAction[] wall = Wall(run.Types);

        if (run.Board.Count >= wall.Length)
        {
            return Shopping(run);
        }

        BuildAction next = wall[run.Board.Count];
        int tower = run.Costs.PriceOf(Purchase.Unit(next.TypeId));

        return tower > run.Purse.Gold
            ? Shopping(run)
            : Shopping(run, run.Purse.Gold - tower).With(next);
    }

    /// <summary>A round filling the slots named here and building nothing.</summary>
    /// <remarks>
    /// A decision that names no slot at all is spelled
    /// <see cref="BuyingNothing"/>, so that the two spellings of a wave nobody
    /// paid for are one.
    /// </remarks>
    public static BuildPhase Filling(params WaveSlot[] slots) => BuildPhase.Of(slots);

    /// <summary>
    /// A round that buys nothing at all.
    /// </summary>
    /// <remarks>
    /// No slot named, so the purse carries into the wave exactly as it came out
    /// of the last one. It is named for that because a round that spends
    /// nothing is one build phase among many and not a way into a run that
    /// never charges.
    /// </remarks>
    public static BuildPhase BuyingNothing() => BuildPhase.Of();

    /// <summary>A round that spends the purse on the cheapest creep the roster has.</summary>
    public static BuildPhase Shopping(Run run) => Shopping(run, run.Purse.Gold);

    /// <summary>
    /// The same round, held to a budget rather than to the whole purse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The creep is the first walking row of the roster, which nothing gates:
    /// every creep is sendable from wave one, so the shortest decision that
    /// shops at all is one slot on whichever row comes first.
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
        UnitType first = FirstCreep(run.Types);
        int count = budget / run.Costs.PriceOf(Purchase.Unit(first.Id));

        return count == 0
            ? BuildPhase.Of()
            : BuildPhase.Of(WaveSlot.Of(first.Id, count));
    }

    /// <summary>The first walking row of a roster, which is what a fixture sends.</summary>
    public static UnitType FirstCreep(UnitTypeTable types)
    {
        for (int index = 0; index < types.Count; index++)
        {
            if (types.Types[index].Role == UnitRole.Moving)
            {
                return types.Types[index];
            }
        }

        throw new SimulationException(
            "A fixture asked a roster with no walking row in it for a creep to send.");
    }
}
