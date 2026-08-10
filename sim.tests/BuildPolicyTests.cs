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

    /// <summary>The committed ranger: forty gold and four hexes, and the row this player never reaches.</summary>
    private const int Ranger = 14;

    /// <summary>The creep the runs below are about.</summary>
    private const int Minion = 1;

    /// <summary>
    /// How many waves the runs below last. Fourteen soldiers cover the whole
    /// corridor and a round buys one or two of them, so a shorter run would
    /// observe the first phase alone and say nothing about the second.
    /// </summary>
    private const int Waves = 16;

    /// <summary>How many opponents a round of them is resolved against.</summary>
    private const int FieldSize = 2;

    [Fact]
    public void The_defense_takes_half_the_purse_and_what_it_declines_to_spend_banks()
    {
        // Half the purse, rounded down, and the wave gets the rest of the purse
        // rather than the rest of the half. An opening round is the clearest
        // place to read it: a hundred gold splits fifty and fifty, the board
        // takes one thirty-gold soldier and cannot afford a second, and the
        // twenty it did not spend banks instead of buying two more minions.
        //
        // That banking is what the interest rate and the unspent-gold column
        // have to be about -- a player that always emptied its purse would make
        // both of them constants.
        //
        // OBSERVED: hand the wave what the defense left rather than the half it
        // was never offered -- price the actions in EvenShareBot.Decide and take
        // that off the purse for the wave's share. This goes red, 100 spent
        // where 80 was expected, with an identical board underneath it, and two
        // more rows of this class go red behind it on a purse that empties a
        // round earlier.
        Run run = TheBuild.Fresh(fieldSize: FieldSize);

        Assert.Equal(100, run.Purse.Gold);
        Assert.Equal(50, CoverThenUpgradeBot.BudgetOf(run.Purse));

        BuildPhase phase = EvenShareBot.Decide(run, Minion);
        RoundReport report = run.Advance(phase);

        Assert.Equal(Soldier, Assert.Single(phase.Actions).TypeId);
        Assert.Equal(80, report.Build.Spent);
        Assert.Equal(20, report.Build.Purse.Gold);

        // And the half is rounded down, so an odd purse leaves the odd coin on
        // the wave's side rather than on the board's.
        Assert.Equal(50, CoverThenUpgradeBot.BudgetOf(Purse.Holding(101)));
    }

    [Fact]
    public void The_cell_a_tower_goes_on_is_the_first_in_canonical_order_to_reach_the_best_score()
    {
        // Candidate cells are walked by row and then by column, and a cell has
        // to beat the count standing to take it -- so where several cells reach
        // the same number of route hexes, the earliest of them keeps it. That is
        // a total order over the cells with no sort and no comparator, which is
        // how every other tie in the simulation is settled.
        //
        // The committed map makes the tie real: three ground cells put five
        // route hexes inside a soldier's one hex of range and no cell puts six,
        // so the rule is what picks between them rather than the arithmetic.
        //
        // OBSERVED: take a cell on a tie as well -- compare with >= in
        // CoverThenUpgradeBot.Best. This goes red, (12, 6) where (11, 2) was
        // expected, which is the last maximiser instead of the first, and
        // nothing else in this class notices.
        HexMap map = TheMatch.Map();
        UnitType soldier = TheMatch.Types().ById(Soldier);
        var maximisers = new List<(int Column, int Row)>();
        int best = 0;

        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                if (!Footing.Of(map, soldier, column, row).Possible)
                {
                    continue;
                }

                int reached = Reached(map, Board.Empty.Place(soldier, column, row));

                if (reached > best)
                {
                    best = reached;
                    maximisers.Clear();
                }

                if (reached == best)
                {
                    maximisers.Add((column, row));
                }
            }
        }

        Assert.True(
            maximisers.Count > 1,
            "One cell alone reaches "
            + best
            + " route hexes with a soldier on it, so this map settles the choice by arithmetic and the "
            + "walk order is not what picks the cell.");

        BuildAction opening = Assert.Single(
            CoverThenUpgradeBot.Decide(TheBuild.Fresh(fieldSize: FieldSize)));

        Assert.Equal(maximisers[0], (opening.Column, opening.Row));
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
    public void The_bot_builds_no_ranger_because_the_archer_ties_it_on_price_with_the_lower_id()
    {
        // A consequence of the rule rather than a rule of its own, written down
        // because it is the kind of thing somebody finds in the report and reads
        // as a bug. The first phase walks up the price list and the second
        // upgrades to the cheapest type dearer than the one standing; the archer
        // and the ranger cost the same forty gold, and a tie goes to the lower
        // type id, so the ranger is never the answer to either question. Its
        // extra hex of range never gets to matter.
        //
        // OBSERVED: break the tie the other way -- compare type ids with > in
        // CoverThenUpgradeBot.Ahead. This goes red having found the ranger among
        // the types built, and no other row of this class notices: the mage
        // still arrives on schedule behind it and every action still buys
        // something.
        UnitTypeTable types = TheMatch.Types();
        CostTable costs = CostTable.From(TheRuleset.Committed(), types);
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

        Assert.DoesNotContain(Ranger, built);

        // And the other three rows of the roster all do get built, so the
        // ranger's absence is this rule and not a player that never builds.
        Assert.Contains(Soldier, built);
        Assert.Contains(Archer, built);
        Assert.Contains(Mage, built);

        // And the reason: same price, higher id.
        Assert.Equal(costs.PriceOf(Purchase.Unit(Archer)), costs.PriceOf(Purchase.Unit(Ranger)));
        Assert.True(Archer < Ranger);
    }

    [Fact]
    public void One_policy_call_carries_both_the_actions_and_the_slots()
    {
        // A round is one decision over one wallet, so the two halves arrive as
        // one build phase and are paid for out of one purse in one walk. The
        // even share is the composition and not a second decision: what to build
        // is asked of the defensive player and copied in unchanged, in the order
        // it was decided, because the placement ordinals fall out of that order.
        //
        // OBSERVED: append the actions in reverse in EvenShareBot.Decide. This
        // goes red on the round that builds two towers, place at column 12 row 6
        // where column 3 row 4 was expected -- two placements swapping ordinals,
        // which is a different run rather than a different spelling of one.
        Run run = TheBuild.Fresh(fieldSize: FieldSize);

        run.Advance(EvenShareBot.Decide(run, Minion));

        IReadOnlyList<BuildAction> defensive = CoverThenUpgradeBot.Decide(run);
        BuildPhase phase = EvenShareBot.Decide(run, Minion);

        Assert.Equal(2, defensive.Count);
        Assert.Equal(defensive, phase.Actions);

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
