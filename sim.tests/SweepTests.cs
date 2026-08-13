using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The balance harness, tested as what it is: a pure function from parameters
/// to rows.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here writes a file and nothing here reads one the harness saw.</b>
/// The tests open the content and hand over text, the harness hands back rows,
/// and the comma-separated file is somebody else's job -- which is the split
/// this whole surface exists to make possible.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class SweepTests
{
    [Fact]
    public void Two_sweeps_with_the_same_parameters_produce_identical_rows()
    {
        // The property the whole harness rests on. A sweep is a few hundred
        // runs and a fold, and every draw in every one of them is derived from
        // the plan's own seed -- so two sweeps of one plan are the same numbers
        // or something in there is reading the machine.
        //
        // The rows are compared field by field rather than by their printed
        // form, because ToString carries three of the fourteen numbers and a
        // sweep that moved the other eleven would compare equal. The two payment
        // columns and the two gold columns are in that comparison for the same
        // reason; what pins their values rather than their determinism is the
        // row test below.
        //
        // OBSERVED: seed the runs from the run index alone -- drop the plan's
        // seed out of Hash64.Start(RunLabel) in SweepPlan.SeedOf. This stays
        // GREEN, because two sweeps of one plan still agree; what goes red is
        // the seed test below, which is why both exist.
        //
        // OBSERVED: fold System.DateTime.Now.Millisecond into that same
        // derivation. This goes red on the first field comparison, and
        // IlScanTests goes red twice beside it -- on the committed image and on
        // a fresh build of the same sources -- which is the other half of why
        // the simulation cannot reach for a clock in the first place.
        SweepReport first = Sweep.Of(TheSweep.Plan());
        SweepReport second = Sweep.Of(TheSweep.Plan());

        Assert.Equal(first.Rows.Count, second.Rows.Count);

        for (int index = 0; index < first.Rows.Count; index++)
        {
            SweepRow left = first.Rows[index];
            SweepRow right = second.Rows[index];

            Assert.Equal(left.TypeId, right.TypeId);
            Assert.Equal(left.Ingredients, right.Ingredients);
            Assert.Equal(left.Runs, right.Runs);
            Assert.Equal(left.Rounds, right.Rounds);
            Assert.Equal(left.Wins, right.Wins);
            Assert.Equal(left.WinRateBasisPoints, right.WinRateBasisPoints);
            Assert.Equal(left.LeakCostDealt, right.LeakCostDealt);
            Assert.Equal(left.LeakCostTaken, right.LeakCostTaken);
            Assert.Equal(left.GoldSpent, right.GoldSpent);
            Assert.Equal(left.DefenseGold, right.DefenseGold);
            Assert.Equal(left.UnspentGold, right.UnspentGold);
            Assert.Equal(left.DealtPerHundredGold, right.DealtPerHundredGold);
            Assert.Equal(left.IncomeBaseGold, right.IncomeBaseGold);
            Assert.Equal(left.BonusGold, right.BonusGold);
        }
    }

    [Fact]
    public void A_row_says_what_attacking_earned_its_sender_beside_what_turning_up_paid()
    {
        // The half of the economy the report would otherwise be silent about. A
        // row carries what its runs were paid for happening and what they were
        // paid for how they did, as two integers, so a reader can see the second
        // is not zero and can divide one by the other without trusting a ratio
        // this type computed.
        //
        // The base is arithmetic -- the flat income times the rounds the row's
        // runs resolved between them -- which is what makes the bonus beside it
        // readable as a share.
        //
        // OBSERVED: pass PerformanceField.Absent in place of run.Field to
        // Purse.BonusOver in Sweep.Play. This goes red saying "Every creep in
        // the report earned nothing at all for what it sent, over 8064 gold of
        // flat base", and every other number in the report stays exactly as it
        // was -- which is what an economy paying the base alone looks like from
        // every other column.
        Ruleset rules = TheRuleset.Committed();
        SweepReport report = Sweep.Of(TheSweep.Plan(rules: rules));
        long bonus = 0;
        long incomeBase = 0;

        for (int index = 0; index < report.Rows.Count; index++)
        {
            SweepRow row = report.Rows[index];

            Assert.True(
                row.BonusGold >= 0,
                row.Label
                + " earned "
                + row.BonusGold.ToString(CultureInfo.InvariantCulture)
                + " gold in bonuses, which is a penalty.");

            if (row.Ingredients != SweepRow.AllIngredients)
            {
                continue;
            }

            Assert.Equal(rules.IncomeBasePerWave * (long)row.Rounds, row.IncomeBaseGold);

            bonus += row.BonusGold;
            incomeBase += row.IncomeBaseGold;
        }

        Assert.True(
            bonus > 0,
            "Every creep in the report earned nothing at all for what it sent, over "
            + incomeBase.ToString(CultureInfo.InvariantCulture)
            + " gold of flat base -- which is an economy paying the base alone.");
    }

    [Fact]
    public void A_sweep_on_another_seed_is_another_population_of_runs()
    {
        // The other half of determinism, and the one that catches a sweep whose
        // runs are all the same run. Two plans that differ in nothing but the
        // seed have to disagree, or the seed is not reaching the runs and the
        // sample size is a lie.
        //
        // What separates them is what they bought. Gold spent moves with the
        // whole run -- which offering was drawn, which creep was taken, what it
        // cost to send -- where what got past the field moves only with the
        // half of the purse that walked, and a plan this short can spend two
        // populations' worth of wave on the same two creeps.
        //
        // OBSERVED: drop the plan's seed out of SweepPlan.SeedOf so a run's seed
        // is derived from its index alone. This goes red -- every number on the
        // minion's whole-population row is identical across the two plans -- and
        // the determinism test above stays green, which is exactly the hole it
        // cannot see.
        SweepRow one = TheSweep.Whole(Sweep.Of(TheSweep.Plan()), "minion");
        SweepRow other = TheSweep.Whole(Sweep.Of(TheSweep.Plan(seed: TheSweep.Seed + 1)), "minion");

        Assert.NotEqual(one.GoldSpent, other.GoldSpent);
    }

    [Fact]
    public void A_no_death_sweep_yields_a_round_for_every_wave_of_every_run()
    {
        // Death is a flag rather than a rule so that a sweep always gets N
        // rounds of data out of a row instead of a short one wherever a build
        // failed, and this is that claim as arithmetic: rounds resolved is runs
        // times N, exactly, against a field that would otherwise have killed
        // every one of them.
        //
        // The lethal field is the skeleton's authored match -- three hundred and
        // eighty gold a round against a purse that holds a hundred -- and the
        // pool is thinned to what one of its rounds spends, so a run that could
        // die dies inside the four waves this suite plays.
        //
        // OBSERVED: hand true to the run in place of plan.DeathEndsTheRun in
        // Sweep.Play. The rounds assertion goes red, 12 where 24 was expected --
        // a sweep quietly reporting on half the waves it was asked for, with
        // every rate on the row still perfectly well formed.
        UnitTypeTable types = TheMatch.Types();

        SweepRow living = TheSweep.Whole(
            Sweep.Of(TheSweep.Plan(
                types: types,
                rules: TheSweep.ThinHealth(),
                field: TheSweep.LethalField(types))),
            "minion");

        SweepRow dying = TheSweep.Whole(
            Sweep.Of(TheSweep.Plan(
                types: types,
                rules: TheSweep.ThinHealth(),
                field: TheSweep.LethalField(types),
                deathEndsTheRun: true)),
            "minion");

        Assert.Equal(TheSweep.Runs * TheSweep.Waves, living.Rounds);
        Assert.True(
            dying.Rounds < living.Rounds,
            "A field that costs more health a round than the pool holds ended no run early, so the death "
            + "flag has nothing to be a flag about here: " + dying.ToString());

        // And the rounds the death flag cut off are rounds of data the no-death
        // sweep has and the other one does not.
        Assert.True(living.LeakCostTaken > dying.LeakCostTaken, living.ToString() + " / " + dying.ToString());
    }

    [Fact]
    public void A_sweep_and_a_run_agree_about_what_death_does_by_default()
    {
        // One knob, one default. A harness that quietly played a different game
        // from the one the same content plays through the run verbs would be a
        // report about a rule nobody chose -- so no-death is asked for, on both
        // surfaces, and never assumed on one of them.
        //
        // OBSERVED: default deathEndsTheRun to false in SweepPlan. This goes
        // red, and nothing else in the suite does: every other sweep test names
        // the flag, and the committed report is produced by a script that passes
        // --no-death explicitly.
        Assert.Equal(TheRun.Fresh().DeathEndsTheRun, TheSweep.Plan(deathEndsTheRun: true).DeathEndsTheRun);
        Assert.True(new SweepPlan(
            TheMatch.Map(),
            TheRuleset.Committed(),
            TheMatch.Types(),
            TheLadder.Committed(),
            TheSweep.Field(TheMatch.Types()),
            TheSweep.Seed,
            TheSweep.Runs).DeathEndsTheRun);
    }

    [Fact]
    public void The_ingredient_bins_partition_the_creeps_whole_population()
    {
        // The U-shaped meta failure mode is read down this axis, so the axis has
        // to be a partition: every run is in exactly one bin and the bins add up
        // to the row above them. A binning that dropped or double-counted a run
        // would produce a shape in the win rate that nothing in the game caused.
        //
        // The bins are also required to be more than one, because a column whose
        // every run lands in the same bin is a column that cannot show a shape
        // and would pass a partition test perfectly.
        //
        // OBSERVED: bin on Unlocks.Count rather than on distinct type ids in
        // Sweep.Ingredients. The partition still holds -- it is still a
        // function of the run -- and the bins collapse to one, N, because there
        // is exactly one take a round: the bin-count assertion goes red having
        // found a single bin, and the whole column stops saying anything.
        SweepReport report = Sweep.Of(TheSweep.Plan());
        SweepRow whole = TheSweep.Whole(report, "minion");
        int runs = 0;
        int wins = 0;
        int rounds = 0;
        int bins = 0;

        for (int index = 0; index < report.Rows.Count; index++)
        {
            SweepRow row = report.Rows[index];

            if (row.Label != "minion" || row.Ingredients == SweepRow.AllIngredients)
            {
                continue;
            }

            Assert.InRange(row.Ingredients, 1, TheSweep.Waves);

            runs += row.Runs;
            wins += row.Wins;
            rounds += row.Rounds;
            bins++;
        }

        Assert.Equal(whole.Runs, runs);
        Assert.Equal(whole.Wins, wins);
        Assert.Equal(whole.Rounds, rounds);
        Assert.True(bins > 1, "Every run of the minion landed in one ingredient bin, so the column is flat.");
    }

    [Fact]
    public void A_sweep_that_names_no_policy_is_played_by_the_even_share_bot()
    {
        // The scripted player is a value on the plan and its default is
        // declared there rather than reached for inside the fold, which is what
        // makes the committed report the even-share bot's report rather than
        // the harness's only possible one.
        //
        // OBSERVED: copy the banking policy below into SweepPlan and default to
        // it. This goes red naming both methods -- "Method = Banks" against the
        // "Method = Decide" expected -- and five more rows of this class go red
        // behind it, because a sweep that buys nothing has no seed to separate
        // and no offering ratio to be sensitive to.
        Assert.Equal(new BuildPolicy(EvenShareBot.Decide), TheSweep.Plan().Policy);
    }

    [Fact]
    public void Another_build_policy_is_an_argument_rather_than_an_edit()
    {
        // The property the whole seam exists for: scoring a roster under a
        // different scripted player costs one argument to the plan and nothing
        // at all in the fold that scores it. The second policy takes its option
        // and fills no slot, so every run of it banks -- and the bins it
        // produces are still a partition of the population above them, which is
        // the fold doing its job without knowing what decided the waves.
        //
        // OBSERVED: call EvenShareBot.Decide in Sweep.Play in place of the
        // plan's policy. This goes red on the first spend assertion, 2502 gold
        // where nothing was expected, and the plan's parameter goes back to
        // being a field nothing reads.
        SweepReport banked = Sweep.Of(TheSweep.Plan(policy: TheSweep.Banks));
        SweepRow whole = TheSweep.Whole(banked, "minion");
        int binnedRuns = 0;

        for (int index = 0; index < banked.Rows.Count; index++)
        {
            SweepRow row = banked.Rows[index];

            Assert.Equal(0, row.GoldSpent);
            Assert.Equal(0, row.LeakCostDealt);

            if (row.Label == "minion" && row.Ingredients != SweepRow.AllIngredients)
            {
                Assert.True(row.Ingredients >= 1, row.ToString());
                binnedRuns += row.Runs;
            }
        }

        Assert.Equal(whole.Runs, binnedRuns);

        // And the default player does spend, so the assertions above are about
        // the policy that was supplied rather than about a sweep that never
        // buys anything.
        Assert.True(
            TheSweep.Whole(Sweep.Of(TheSweep.Plan()), "minion").GoldSpent > 0,
            "The even-share bot bought nothing either, so a report of no spending says nothing about "
            + "which player produced it.");
    }

    [Fact]
    public void Gold_spent_is_what_walked_and_the_defense_has_a_column_of_its_own()
    {
        // A phase pays for its towers and its creeps out of one purse, and the
        // report splits that bill in two. Gold spent is the denominator of the
        // cost-efficiency column, so a player that builds a board and sends
        // nothing has spent nothing on creeps however much its purse moved --
        // and what it did spend is on the row beside it rather than nowhere.
        //
        // OBSERVED: report the whole bill as the wave's -- add Build.Spent to
        // Sweep.Play's spent total and leave the defense out of it. The first
        // row goes red, 1800 gold of creeps where nothing walked, and the
        // cost-efficiency column quietly becomes leak cost per hundred gold of
        // tower.
        SweepReport built = Sweep.Of(TheSweep.Plan(policy: TheSweep.Builds));

        for (int index = 0; index < built.Rows.Count; index++)
        {
            SweepRow row = built.Rows[index];

            Assert.Equal(0, row.GoldSpent);
            Assert.True(
                row.DefenseGold > 0,
                "A player that spends half of every purse on towers built nothing at all: " + row);
        }

        // And a player that builds nothing reports nothing built, so the column
        // above is about what the policy did rather than about a number that is
        // always positive.
        SweepReport banked = Sweep.Of(TheSweep.Plan(policy: TheSweep.Banks));

        for (int index = 0; index < banked.Rows.Count; index++)
        {
            Assert.Equal(0, banked.Rows[index].DefenseGold);
        }

        // The default player does both halves, which is what every row of the
        // committed report is played by.
        SweepRow whole = TheSweep.Whole(Sweep.Of(TheSweep.Plan()), "minion");

        Assert.True(whole.GoldSpent > 0 && whole.DefenseGold > 0, whole.ToString());
    }

    [Fact]
    public void A_run_that_died_holding_gold_reports_it_like_any_other()
    {
        // What a run ended holding is the other half of what it earned, and a
        // report that counted it only for the runs that survived would read the
        // banking rule off the survivors alone -- which is the population that
        // banked well enough to still be alive.
        //
        // The field here kills every run in its first round, so every gold in
        // the unspent column of this report was in a dead run's purse.
        //
        // OBSERVED: count the purse only where the run ran out of waves --
        // guard run.Purse.Gold in Sweep.Play on run.Ending. This goes red on the
        // first row, "a run that died reported an empty purse", and the no-death
        // sweep the committed report is produced by stays green.
        UnitTypeTable types = TheMatch.Types();

        SweepReport dying = Sweep.Of(TheSweep.Plan(
            types: types,
            rules: TheSweep.ThinHealth(),
            field: TheSweep.LethalField(types),
            deathEndsTheRun: true));

        for (int index = 0; index < dying.Rows.Count; index++)
        {
            SweepRow row = dying.Rows[index];

            Assert.Equal(row.Runs, row.Rounds);
            Assert.True(row.UnspentGold > 0, "A run that died reported an empty purse: " + row);
        }
    }

    [Fact]
    public void A_win_rate_is_the_wins_over_the_runs_in_basis_points()
    {
        // There is no floating point in the simulation and the build gate scans
        // for it, so a rate is an integer and it arrives beside the two numbers
        // it was computed from. This is the claim that the third number is the
        // other two -- and that a reader who recomputes it gets what is printed.
        //
        // OBSERVED: fold the rate as wins * 100 / runs in Sweep.Cell.Row, which
        // is the percentage somebody will reach for first. This goes red on the
        // first row with a win on it, 33 where 3333 was expected -- and every
        // row at zero stays green, which is why the assertion walks the whole
        // report rather than one row.
        SweepReport report = Sweep.Of(TheSweep.Plan());

        for (int index = 0; index < report.Rows.Count; index++)
        {
            SweepRow row = report.Rows[index];

            Assert.InRange(row.Wins, 0, row.Runs);
            Assert.Equal((int)(10000L * row.Wins / row.Runs), row.WinRateBasisPoints);
            Assert.Equal(
                row.GoldSpent == 0 ? 0 : (int)(100 * row.LeakCostDealt / row.GoldSpent),
                row.DealtPerHundredGold);
        }
    }

    [Fact]
    public void A_bounded_sweep_says_in_its_own_output_what_it_left_out()
    {
        // The bound the sweep places on itself, reported as a row of the report
        // rather than as a warning somewhere else. A truncated sweep that said
        // nothing would read exactly like a complete one three months later --
        // same columns, same shape, fewer rows -- and nobody would know to ask.
        //
        // OBSERVED: report the bound as the roster's whole width -- pass
        // plan.Roster twice to the creeps CoverageBound in Sweep.Of. The
        // covered-count assertion goes red, 6 where 2 was expected; the rows
        // themselves are unchanged, which is the failure exactly: a report that
        // scored a third of the roster and says it scored all of it.
        SweepReport report = Sweep.Of(TheSweep.Plan(mostCreeps: TheSweep.Creeps));
        CoverageBound creeps = TheSweep.Bound(report, "creeps");

        Assert.Equal(TheSweep.Creeps, creeps.Covered);
        Assert.Equal(TheSweep.Plan().Roster, creeps.Available);
        Assert.True(creeps.IsBounded, creeps.ToString());

        // And the rows agree with the bound, because a coverage row that did not
        // describe the rows would be a second thing to be wrong.
        Assert.Equal(TheSweep.Creeps, Scored(report));
    }

    [Fact]
    public void An_unbounded_sweep_says_that_too_rather_than_saying_nothing()
    {
        // The other half of the mechanism, and the reason coverage is always
        // reported instead of only when something was cut: completeness is a
        // value in the output rather than the absence of a warning, so a reader
        // never has to know whether this build's sweep would have told them.
        //
        // OBSERVED: emit the creeps bound only when it cuts something -- guard
        // the CoverageBound in Sweep.Of on plan.Creeps.Count < plan.Roster. This
        // goes red inside TheSweep.Bound, "The report says nothing about its
        // coverage of creeps", and the bounded test above stays perfectly green.
        SweepReport report = Sweep.Of(TheSweep.Plan(mostCreeps: SweepPlan.WholeRoster));
        CoverageBound creeps = TheSweep.Bound(report, "creeps");

        Assert.Equal(creeps.Available, creeps.Covered);
        Assert.False(creeps.IsBounded, creeps.ToString());
        Assert.Equal(creeps.Available, Scored(report));
    }

    [Fact]
    public void The_seed_axis_is_reported_as_the_sample_it_can_only_be()
    {
        // A run count is a sample of a space 2^64 wide, so it is bounded however
        // large it is and there is no number of runs that would make this report
        // complete. Saying so is what stops a report of a thousand runs a creep
        // reading as the whole answer.
        //
        // OBSERVED: give the seed axis an Available of the run count instead of
        // Unbounded. This goes red, 6 where 0 was expected, and IsBounded goes
        // false behind it -- a sample presenting as an exhaustive enumeration of
        // itself, which is true and useless.
        CoverageBound seeds = TheSweep.Bound(Sweep.Of(TheSweep.Plan()), "seeds");

        Assert.Equal(TheSweep.Runs, seeds.Covered);
        Assert.Equal(CoverageBound.Unbounded, seeds.Available);
        Assert.True(seeds.IsBounded, seeds.ToString());
    }

    [Fact]
    public void The_offering_ratio_is_a_sweep_parameter_and_it_reaches_the_runs()
    {
        // The ratio deciding whether a merged anchor menu is a real trade, moved
        // from the harness rather than from a second ruleset file. Two things
        // have to be true: the rules the sweep plays carry the number, and the
        // runs come out different -- a dial that reached the ruleset and not the
        // offering would pass the first half alone.
        //
        // What the runs come out different in is gold spent rather than leak
        // cost dealt, for the reason the seed test above gives: a wave bought
        // out of half a purse gets nothing past the canned field, so every row
        // of this plan deals zero.
        //
        // OBSERVED: drop the argument on the floor in SweepPlan -- pass
        // rules.OrdinaryOptionsPerRound to Ruleset.With in place of the Or that
        // reads it. The parameter assertion goes red, 3 where 1 was expected,
        // and the report is the committed ratio's report twice over.
        SweepPlan narrow = TheSweep.Plan(ordinaryOptionsPerRound: 1);
        SweepPlan authored = TheSweep.Plan();

        Assert.Equal(1, narrow.Rules.OrdinaryOptionsPerRound);
        Assert.Equal(TheRuleset.Committed().OrdinaryOptionsPerRound, authored.Rules.OrdinaryOptionsPerRound);
        Assert.NotEqual(authored.Rules.ContentHash, narrow.Rules.ContentHash);

        Assert.NotEqual(
            TheSweep.Whole(Sweep.Of(authored), "minion").GoldSpent,
            TheSweep.Whole(Sweep.Of(narrow), "minion").GoldSpent);
    }

    [Fact]
    public void The_free_snapshot_count_and_the_snapshot_price_are_sweep_parameters()
    {
        // Ten free a run is a starting point the harness can move rather than a
        // decision, so both numbers are arguments here and both are folded into
        // the rules the sweep plays -- content hash and all, which is what makes
        // a retuned sweep loudly a different sweep.
        //
        // NOTHING CONSUMES THE FREE COUNT YET and that is the build order rather
        // than a fault: scouting lands as data first and as an interface later,
        // so what this asserts is that the parameter is present and reaches the
        // ruleset, which is the whole of what it can assert today.
        //
        // OBSERVED: swap the free-snapshot and snapshot-price arguments over in
        // SweepPlan's call to Ruleset.With. This goes red on the free count, 40
        // where 3 was expected -- which is exactly the failure an argument list
        // of four same-typed integers has, and the reason both are asserted
        // separately rather than through the hash they share.
        SweepPlan retuned = TheSweep.Plan(freeSnapshotsPerRun: 3, snapshotPriceGold: 40);

        Assert.Equal(3, retuned.Rules.FreeSnapshotsPerRun);
        Assert.Equal(40, retuned.Rules.SnapshotPriceGold);
        Assert.NotEqual(TheRuleset.Committed().ContentHash, retuned.Rules.ContentHash);

        // And a run priced against them prices a snapshot at the sweep's number,
        // because the cost table is built out of the rules the run holds.
        Assert.Equal(40, CostTable.From(retuned.Rules, retuned.Types).PriceOf(Purchase.Snapshot));
    }

    [Fact]
    public void A_creep_that_costs_nothing_is_refused_rather_than_bought_without_bound()
    {
        // The harness budgets a slot by dividing a share of the purse by a
        // price, so a price of nothing is not a cheap creep, it is a creep a
        // purse buys until the record's count column overflows. It is also a
        // creep whose leak charges no health at all, which puts it outside the
        // exchange rate the whole economy is denominated in.
        //
        // OBSERVED: return the price unguarded from Sweep.PriceOf. This goes
        // red having thrown DivideByZeroException instead -- the same refusal
        // with none of the sentence, arriving from inside a fold rather than
        // from the rule that was broken.
        SimulationException refused = Assert.Throws<SimulationException>(
            () => Sweep.Of(TheSweep.Plan(types: TheSweep.FreeTypes())));

        Assert.Contains("costs nothing to send", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_pointed_at_a_roster_of_towers_is_refused()
    {
        // A row of this report is what one creep did, so a roster with nothing
        // that walks is a sweep with nothing to be about. Refused by name rather
        // than answered with an empty report, on the rule the whole project
        // follows: a result that came back is a result somebody keeps.
        //
        // The plan is built out of two tables that were never checked against
        // each other, because that is the only way in: an anchor opens offense
        // and never defense, so a schedule whose changers field towers does not
        // load at all. See TheSweep.TowerRoster.
        //
        // OBSERVED: let the empty roster through SweepPlan.Scored by comparing
        // its count against zero the other way round. This goes red having had
        // no exception thrown at all: the plan builds, and what comes back from
        // Sweep.Of is a balance report of no rows -- which reads like a roster
        // nothing is wrong with.
        SimulationException refused = Assert.Throws<SimulationException>(() => TheSweep.TowerRoster());

        Assert.Contains("no creep in it to score", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, "runs per creep")]
    [InlineData(-1, "runs per creep")]
    public void A_cell_of_no_runs_is_refused(int runs, string named)
    {
        // A rate is a share of a population and there is no share of nothing.
        // Refused where the plan is built rather than where the division
        // happens, because a plan that has already played a few hundred runs is
        // a plan somebody is waiting on.
        //
        // OBSERVED: lower the runs-per-creep floor in SweepPlan from one to a
        // thousand below zero. Both rows go red having had no exception thrown
        // at all -- the plan builds, and the refusal only arrives later out of
        // Cell.Row, after the roster has been walked and with the wrong subject
        // in its sentence.
        SimulationException refused = Assert.Throws<SimulationException>(() => TheSweep.Plan(runs: runs));

        Assert.Contains(named, refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_with_no_last_wave_is_not_a_sweep_row()
    {
        // A row is a bounded run. Lifting the wave cap makes it a loop, and a
        // loop has no outcome to fold, so the plan refuses it here rather than
        // letting the run's own refusal arrive from underneath a sweep.
        //
        // OBSERVED: lower the waves floor in SweepPlan from one to
        // Purse.RoundCapLifted. This goes red having had no exception thrown at
        // all: the plan builds, and the refusal waits until Run is constructed
        // inside the fold -- which is a sweep somebody is already waiting on.
        SimulationException refused = Assert.Throws<SimulationException>(
            () => TheSweep.Plan(waves: Purse.RoundCapLifted));

        Assert.Contains("waves", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>How many creeps the report actually carries a whole-population row for.</summary>
    private static int Scored(SweepReport report)
    {
        int scored = 0;

        for (int index = 0; index < report.Rows.Count; index++)
        {
            if (report.Rows[index].Ingredients == SweepRow.AllIngredients)
            {
                scored++;
            }
        }

        return scored;
    }
}
