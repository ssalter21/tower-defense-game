namespace Sim.Tests;

/// <summary>
/// The two scripted players a sweep row is played by: the defensive half that
/// covers the route and then upgrades what it stood, and the even share that
/// composes it with a wave.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything here is fought over the committed content.</b> Which tower is
/// cheapest, how many of them the corridor needs and where the ties fall are all
/// facts about <c>content/map.txt</c> and <c>content/units.txt</c> -- so a
/// synthetic roster standing in for them would be a fixture that stays green
/// while the board every row of the balance report is actually played on has
/// changed underneath it.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class BuildPolicyTests
{
    /// <summary>The committed soldier: thirty gold and one hex of range, so the cheapest row that can stand.</summary>
    private const int Soldier = 11;

    /// <summary>The committed archer: forty gold and three hexes.</summary>
    private const int Archer = 3;

    /// <summary>The committed mage: ninety-two gold, and the dearest row on the roster.</summary>
    private const int Mage = 4;

    /// <summary>The committed ranger: forty gold and four hexes, and the widest reach on the roster.</summary>
    private const int Ranger = 14;

    /// <summary>The creep the runs below are about.</summary>
    private const int Minion = 1;

    /// <summary>
    /// How many waves the runs below last. Three towers cover the whole corridor
    /// and each upgrade behind them costs more than an early round's half, so a
    /// shorter run would observe the first phase alone and say nothing about the
    /// second.
    /// </summary>
    private const int Waves = 16;

    /// <summary>
    /// How many rounds <see cref="Banked"/> spends taking and sending nothing.
    /// Four is what it takes for half a purse to hold more than one action.
    /// </summary>
    private const int Idle = 4;

    /// <summary>How many opponents a round of them is resolved against.</summary>
    private const int FieldSize = 2;

    [Fact]
    public void The_defense_takes_half_the_purse_and_what_it_declines_to_spend_banks()
    {
        // Half the purse, rounded down, and the wave gets the rest of the purse
        // rather than the rest of the half. An opening round is the clearest
        // place to read it: a hundred gold splits fifty and fifty, the board
        // takes one forty-gold archer and cannot afford a second, and the ten it
        // did not spend banks instead of buying another minion.
        //
        // That banking is what the interest rate and the unspent-gold column
        // have to be about -- a player that always emptied its purse would make
        // both of them constants.
        //
        // OBSERVED: hand the wave what the defense left rather than the half it
        // was never offered -- price the actions in EvenShareBot.Decide and take
        // that off the purse for the wave's share. This goes red, 100 spent
        // where 90 was expected, and three more rows of this class go red behind
        // it on runs that reach different boards with the extra gold.
        Run run = TheBuild.Fresh(fieldSize: FieldSize);

        Assert.Equal(100, run.Purse.Gold);
        Assert.Equal(50, CoverThenUpgradeBot.BudgetOf(run.Purse));

        BuildPhase phase = EvenShareBot.Decide(run, Minion);
        RoundReport report = run.Advance(phase);

        Assert.Equal(Archer, Assert.Single(phase.Actions).TypeId);
        Assert.Equal(90, report.Build.Spent);
        Assert.Equal(10, report.Build.Purse.Gold);

        // And the half is rounded down, so an odd purse leaves the odd coin on
        // the wave's side rather than on the board's.
        Assert.Equal(50, CoverThenUpgradeBot.BudgetOf(Purse.Holding(101)));
    }

    [Fact]
    public void The_cell_a_tower_goes_on_is_the_first_in_canonical_order_to_reach_the_best_score()
    {
        // Candidate cells are walked by row and then by column, and a cell has
        // to beat the count standing to take it -- so where several cells reach
        // the same number of route hexes the board does not, the earliest of
        // them keeps it. That is a total order over the cells with no sort and
        // no comparator, which is how every other tie in the simulation is
        // settled.
        //
        // Every placement of a whole run is checked against the cells that were
        // available to it, and the run is required to have met a tie at least
        // once -- otherwise the arithmetic picked every cell and this asserts
        // nothing. On the committed map the tie falls on the third tower, which
        // has four hexes of corridor left to reach and two cells that reach
        // them.
        //
        // OBSERVED: take a cell on a tie as well -- compare with >= in
        // CoverThenUpgradeBot.Best. This goes red, (3, 4) where (1, 4) was
        // expected, which is the last maximiser instead of the first.
        HexMap map = TheMatch.Map();
        int ties = 0;

        foreach ((Board opening, IReadOnlyList<BuildAction> actions) in Played())
        {
            Board board = opening;

            foreach (BuildAction action in actions)
            {
                UnitType type = TheMatch.Types().ById(action.TypeId);

                if (action.Kind == ActionKind.Upgrade)
                {
                    board = board.Upgrade(type, action.Column, action.Row);
                    continue;
                }

                List<(int Column, int Row)> maximisers = Maximisers(map, board, type);

                Assert.Equal(maximisers[0], (action.Column, action.Row));

                if (maximisers.Count > 1)
                {
                    ties++;
                }

                board = board.Place(type, action.Column, action.Row);
            }
        }

        Assert.True(
            ties > 0,
            "No placement of this run had two cells to choose between, so the arithmetic settled every one of "
            + "them and the walk order is not what picked a cell.");
    }

    [Fact]
    public void Every_action_the_bot_takes_buys_new_coverage_or_upgrades_a_placement()
    {
        // The bot cannot burn gold on nothing, and it is avoided by
        // construction rather than accepted. A tower that reaches no part of the
        // route is a legal decision -- a player is allowed to build somewhere
        // useless -- so one bought by mistake would stand on the board and never
        // show up as unspent gold, because the gold was spent.
        //
        // Every place is therefore required to reach a route hex the board did
        // not already reach, and every upgrade to name a cell something already
        // stands on.
        //
        // OBSERVED: score the coverage the board already holds -- drop the
        // covered check out of CoverThenUpgradeBot.Gained. This goes red, "place
        // type 11 at column 10, row 2 reaches no route hex the board did not
        // already reach", which is a soldier bought for thirty gold to watch a
        // stretch of corridor three others were already watching.
        HexMap map = TheMatch.Map();
        int places = 0;
        int upgrades = 0;

        foreach ((Board opening, IReadOnlyList<BuildAction> actions) in Played())
        {
            Board board = opening;

            foreach (BuildAction action in actions)
            {
                UnitType type = TheMatch.Types().ById(action.TypeId);

                if (action.Kind == ActionKind.Upgrade)
                {
                    Assert.False(board.IsFree(action.Column, action.Row), action.ToString() + " names an empty cell.");

                    board = board.Upgrade(type, action.Column, action.Row);
                    upgrades++;
                    continue;
                }

                Assert.True(board.IsFree(action.Column, action.Row), action.ToString() + " names a taken cell.");

                int before = Reached(map, board);

                board = board.Place(type, action.Column, action.Row);
                places++;

                Assert.True(
                    Reached(map, board) > before,
                    action.ToString() + " reaches no route hex the board did not already reach.");
            }
        }

        Assert.True(places > 0 && upgrades > 0, "This run took " + places + " places and " + upgrades + " upgrades.");
    }

    [Fact]
    public void The_bot_opens_on_the_archer_and_never_reaches_the_ranger_at_all()
    {
        // Two consequences of the rules rather than rules of their own, written
        // down because both are the kind of thing somebody finds in the report
        // and reads as a bug.
        //
        // THE OPENING PURSE BUYS AN ARCHER. The ranger reaches further for the
        // same forty gold and used to win this purchase outright. #179 made it
        // the target of the ladder's one edge, so it may not be placed: the
        // dearest-reaching row the ladder lets the bot stand is the archer.
        //
        // THE RANGER IS THEN NEVER BUILT AT ALL. The upgrade half climbs to a
        // strictly dearer row, and the ranger costs exactly what the archer
        // costs -- so nothing this bot does ever reaches it. That is worth
        // knowing rather than fixing here: it means the sweep's defense never
        // exercises the one upgrade edge the roster has, so a balance question
        // about the ranger cannot be answered from content/sweep.csv. It is a
        // property of this bot's rule and not of the ladder.
        //
        // OBSERVED: score by price alone -- return the first type with anything
        // to gain out of CoverThenUpgradeBot.BestValue. This goes red on the
        // opening action, type 11 where 3 was expected.
        //
        // OBSERVED: drop the Placeable filter from CoverThenUpgradeBot.Decide.
        // This goes red the other way, type 14 where 3 was expected, and every
        // sweep in the project dies on the placement the ladder refuses.
        UnitTypeTable types = TheMatch.Types();
        CostTable costs = CostTable.From(TheRuleset.Committed(), types);
        HexMap map = TheMatch.Map();
        var built = new List<int>();

        foreach ((Board _, IReadOnlyList<BuildAction> actions) in Played())
        {
            foreach (BuildAction action in actions)
            {
                if (!built.Contains(action.TypeId))
                {
                    built.Add(action.TypeId);
                }
            }
        }

        Assert.Equal(Archer, built[0]);
        Assert.DoesNotContain(Ranger, built);

        // The soldier and the mage do get built, so the ranger's absence is the
        // two rules above and not a player that only ever buys one thing: the
        // soldier once the archer's cells stop paying, the mage as the climb
        // the upgrade half does make.
        Assert.Contains(Soldier, built);
        Assert.Contains(Mage, built);

        // And the reason, in the two comparisons the rule makes: the archer
        // reaches more corridor per gold than the soldier even though the
        // soldier is cheaper, and the ranger would have beaten the archer on
        // reach at the same price if the ladder let it be placed at all.
        int ranger = Widest(map, types.ById(Ranger));
        int archer = Widest(map, types.ById(Archer));
        int soldier = Widest(map, types.ById(Soldier));

        Assert.Equal(costs.PriceOf(Purchase.Unit(Archer)), costs.PriceOf(Purchase.Unit(Ranger)));
        Assert.True(ranger > archer);
        Assert.True(
            archer * costs.PriceOf(Purchase.Unit(Soldier)) > soldier * costs.PriceOf(Purchase.Unit(Archer)),
            "The soldier reaches " + soldier + " route hexes against the archer's " + archer + ".");

        // And the price the ranger is never reached over: equal to the archer's,
        // so a climb that wants a strictly dearer row steps straight past it.
        Assert.Equal(costs.PriceOf(Purchase.Unit(Archer)), costs.PriceOf(Purchase.Unit(Ranger)));
    }

    [Fact]
    public void One_policy_call_carries_both_the_actions_and_the_slots()
    {
        // A round is one decision over one wallet, so the two halves arrive as
        // one build phase and are paid for out of one purse in one walk. The
        // even share is the composition and not a second decision: what to build
        // is asked of the defensive player and copied in unchanged.
        //
        // OBSERVED: compose the phase out of the slots alone -- drop the loop
        // that copies the actions in at the end of EvenShareBot.Decide. This
        // goes red, no actions where the defensive half decided two places, and
        // every other row of this class goes red behind it on a run that never
        // builds anything.
        Run run = TheBuild.Fresh(fieldSize: FieldSize);

        run.Advance(EvenShareBot.Decide(run, Minion));

        IReadOnlyList<BuildAction> defensive = CoverThenUpgradeBot.Decide(run);
        BuildPhase phase = EvenShareBot.Decide(run, Minion);

        Assert.Equal(defensive, phase.Actions);
        Assert.NotEmpty(phase.Actions);
        Assert.All(phase.Actions, action => Assert.Equal(ActionKind.Place, action.Kind));

        // And the slots were filled out of what the defensive half was not
        // offered, so the one purse pays for both.
        int wave = run.Purse.Gold - CoverThenUpgradeBot.BudgetOf(run.Purse);
        int sent = 0;

        foreach (WaveSlot slot in phase.Slots)
        {
            sent += run.Costs.PriceOf(Purchase.Unit(slot.TypeId), slot.Count);
        }

        Assert.InRange(sent, wave - run.Costs.PriceOf(Purchase.Unit(Minion)) + 1, wave);
    }

    [Fact]
    public void The_actions_of_a_round_arrive_in_the_order_they_were_decided()
    {
        // The placement ordinals fall out of the order the actions are walked
        // in, so a round that builds several things is a different run when they
        // arrive in a different order rather than a different spelling of one.
        //
        // A round of the ordinary run buys at most one thing -- the cheapest
        // purchase worth making is a forty-gold ranger against a half that opens
        // at fifty -- so the order needs a purse fat enough to act on twice, and
        // four rounds of taking an option and sending nothing is what makes one.
        //
        // OBSERVED: append the actions in reverse in EvenShareBot.Decide. This
        // goes red on the first of the five, upgrade type 4 at column 11 row 4
        // where place type 14 at column 6 row 4 was expected -- a board that
        // upgrades a cell nothing stands on.
        Run run = Banked();
        IReadOnlyList<BuildAction> defensive = CoverThenUpgradeBot.Decide(run);
        BuildPhase phase = EvenShareBot.Decide(run, Minion);

        Assert.True(defensive.Count > 1, "This round decided " + defensive.Count + " actions.");
        Assert.Equal(defensive, phase.Actions);
    }

    /// <summary>
    /// A run four rounds into sending nothing, so that the
    /// defense's half of the purse holds more than one action.
    /// </summary>
    private static Run Banked()
    {
        Run run = TheBuild.Fresh(waves: Waves, fieldSize: FieldSize, deathEndsTheRun: false);

        for (int round = 0; round < Idle; round++)
        {
            run.Advance(BuildPhase.Of());
        }

        return run;
    }

    /// <summary>
    /// The free cells this type would reach the most unreached route hexes
    /// from, in the order the bot walks them. Empty where it would reach none:
    /// a cell that adds nothing is not a cell the bot chooses between.
    /// </summary>
    private static List<(int Column, int Row)> Maximisers(HexMap map, Board board, UnitType type)
    {
        var maximisers = new List<(int Column, int Row)>();
        int before = Reached(map, board);
        int best = 0;

        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                if (!board.IsFree(column, row) || !Footing.Of(map, type, column, row).Possible)
                {
                    continue;
                }

                int gained = Reached(map, board.Place(type, column, row)) - before;

                if (gained > best)
                {
                    best = gained;
                    maximisers.Clear();
                }

                if (gained == best && gained > 0)
                {
                    maximisers.Add((column, row));
                }
            }
        }

        return maximisers;
    }

    /// <summary>How much of the route one of these reaches from the best cell on an empty board.</summary>
    private static int Widest(HexMap map, UnitType type)
    {
        (int column, int row) = Maximisers(map, Board.Empty, type)[0];

        return Reached(map, Board.Empty.Place(type, column, row));
    }

    /// <summary>
    /// How many of the route's hexes a board's towers reach between them, read
    /// off the coverage intervals a match resolves range through rather than
    /// recomputed here.
    /// </summary>
    private static int Reached(HexMap map, Board board)
    {
        TowerCoverage coverage = TowerCoverage.For(map, board.Layout());
        int reached = 0;

        for (int step = 0; step < map.Route.Count; step++)
        {
            for (int tower = 0; tower < coverage.TowerCount; tower++)
            {
                if (coverage.Covers(tower, Fix64.FromInt(step)))
                {
                    reached++;
                    break;
                }
            }
        }

        return reached;
    }

    /// <summary>
    /// A run played by the even-share bot, as the board each round opened on
    /// beside what that round built.
    /// </summary>
    /// <remarks>
    /// Long, and with death switched off, because the whole of the rule takes a
    /// while to show: the corridor swallows fourteen soldiers before the first
    /// phase runs out of route to cover, and the upgrades only start after that.
    /// </remarks>
    private static List<(Board Opening, IReadOnlyList<BuildAction> Actions)> Played()
    {
        Run run = TheBuild.Fresh(waves: Waves, fieldSize: FieldSize, deathEndsTheRun: false);
        var rounds = new List<(Board, IReadOnlyList<BuildAction>)>();

        while (!run.IsOver)
        {
            Board opening = run.Board;
            BuildPhase phase = EvenShareBot.Decide(run, Minion);

            rounds.Add((opening, phase.Actions));
            run.Advance(phase);
        }

        return rounds;
    }
}
