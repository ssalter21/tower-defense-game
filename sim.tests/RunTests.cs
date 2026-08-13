using System.Globalization;
using System.Reflection;

namespace Sim.Tests;

/// <summary>
/// The run: N waves, a field of K, a health pool denominated in gold, and an
/// outcome that is a vector rather than a score.
/// </summary>
/// <remarks>
/// <para>
/// <b>The arithmetic is tested against tables that cannot roll.</b> Where a
/// number has to be exact, the run is fought with a unit table whose walking
/// units cannot be killed, so every wave leaks in full, the field's average is
/// the wave's own cost and the health it spends can be written down in advance
/// rather than read off the run under test.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class RunTests
{
    /// <summary>
    /// Every usage scenario, as arguments rather than as code paths. Each one is
    /// whether death ends it, whether anybody reads the outcome as it goes, and
    /// whether N and K were spelled out or left to their defaults.
    /// </summary>
    public static TheoryData<string, bool, bool, bool, bool> Scenarios => new()
    {
        { "normal play", true, true, false, false },
        { "a sweep row", true, false, true, false },
        { "a no-death harness run", false, false, true, false },
        { "a server re-validating", true, false, false, true },
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_usage_scenario_is_the_same_call_with_different_arguments(
        string scenario,
        bool deathEndsTheRun,
        bool readsTheOutcomeAsItGoes,
        bool spellsOutTheLengths,
        bool replaysWhatWasSubmitted)
    {
        // If any of these needed its own code path the surface would be wrong.
        // They do not: the differences are a boolean, two arguments left to
        // their defaults, a property nobody reads until the end, and -- for the
        // server -- building the whole run a second time out of the same
        // arguments and playing it again.
        //
        // Every row is checked against a vector a real run produced rather than
        // against a run this test computed, so a lifecycle regression cannot
        // move both sides of the comparison at once.
        //
        // Every row builds as well as shops, because a run opens on an empty
        // board and one that only ever shops runs out of health in its seventh
        // round. The wall it puts up is the same wall every round, so the four
        // scenarios still differ in nothing but their arguments.
        //
        // The run survives its last wave by 96 gold of health, which is what
        // makes the death flag inert here: the no-death row produces the same
        // vector as the rest rather than a longer one, so the flag is an
        // argument and not a different lifecycle.
        //
        // OBSERVED: give the flag a lifecycle of its own -- when death is off,
        // have Run.Advance record an empty round and return without resolving
        // anything. The no-death row goes red on the health it finished with,
        // 96 against 1500, which is what a second code path hiding behind an
        // argument looks like from the outside.
        Run run = spellsOutTheLengths
            ? TheRun.Fresh(Run.DefaultWaves, Run.DefaultFieldSize, deathEndsTheRun)
            : TheRun.Fresh(deathEndsTheRun: deathEndsTheRun);

        while (!run.IsOver)
        {
            run.Advance(TheBuild.Fortifying(run));

            if (readsTheOutcomeAsItGoes)
            {
                Assert.Equal(run.Round, run.Outcome.Rounds.Count);
                Assert.Equal(run.Health, run.Outcome.HealthRemaining);
            }
        }

        if (replaysWhatWasSubmitted)
        {
            // What a server does with a run somebody sent it: build the same run
            // out of the same arguments and play it again. Not a mode -- the
            // same two calls, made twice.
            Run again = TheRun.Fresh(Run.DefaultWaves, Run.DefaultFieldSize, deathEndsTheRun);

            while (!again.IsOver)
            {
                again.Advance(TheBuild.Fortifying(again));
            }

            Assert.Equal(run.Outcome.LeakCostDealt, again.Outcome.LeakCostDealt);
            Assert.Equal(run.Outcome.LeakCostTaken, again.Outcome.LeakCostTaken);
            Assert.Equal(run.Outcome.HealthRemaining, again.Outcome.HealthRemaining);
            run = again;
        }

        IReadOnlyList<RoundOutcome> expected = TheRun.TheCommittedRun;
        RunOutcome actual = run.Outcome;

        Assert.Equal(RunEnding.OutOfWaves, actual.Ending);
        Assert.Equal(Run.DefaultWaves, actual.Rounds.Count);
        Assert.Equal(expected.Count, actual.Rounds.Count);
        Assert.Equal(TheRun.HealthLeftInTheCommittedRun, actual.HealthRemaining);
        Assert.Equal(Run.DefaultWaves, actual.WavesSurvived);
        Assert.Equal(10, run.Sent.Count);

        for (int round = 0; round < expected.Count; round++)
        {
            Assert.Equal(expected[round].LeakCostDealt, actual.Rounds[round].LeakCostDealt);
            Assert.Equal(expected[round].LeakCostTaken, actual.Rounds[round].LeakCostTaken);
        }

        Assert.NotEqual(string.Empty, scenario);
    }

    [Fact]
    public void A_run_is_ten_waves_and_a_field_of_ten_until_the_caller_says_otherwise()
    {
        // Ten and ten are this map's answers and both are expected to move, so
        // both are arguments with a default rather than constants in the loop.
        //
        // OBSERVED: draw Run.DefaultFieldSize opponents in Run.FieldFor instead
        // of FieldSize. The last assertion goes red, not 274 against 274: the
        // two-opponent run comes back with the ten-opponent run's numbers,
        // because K was a constant wearing an argument's name.
        Run defaults = TheRun.Fresh();

        Assert.Equal(10, defaults.Waves);
        Assert.Equal(10, defaults.FieldSize);
        Assert.True(defaults.DeathEndsTheRun);
        Assert.Equal(Run.DefaultWaves, defaults.Waves);
        Assert.Equal(Run.DefaultFieldSize, defaults.FieldSize);

        Run shorter = Played(TheRun.Fresh(waves: 3, fieldSize: 10));
        Run narrower = Played(TheRun.Fresh(waves: 3, fieldSize: 2));

        Assert.Equal(3, shorter.Outcome.Rounds.Count);
        Assert.Equal(3, narrower.Outcome.Rounds.Count);
        Assert.NotEqual(shorter.Outcome.LeakCostTaken, narrower.Outcome.LeakCostTaken);
    }

    [Fact]
    public void A_leaked_creep_costs_health_equal_to_its_cost_one_for_one()
    {
        // The exchange rate, at the one arrangement where it is arithmetic: a
        // table whose walkers cannot be killed, so exactly one wave leaks in
        // full every round and the health it costs is the wave's own cost
        // column added up.
        //
        // OBSERVED: charge a leak one health per creep rather than its price --
        // `cost += leaked[index]` in Run.LeakCost. The round assertion goes red,
        // 383 against 40, and the pool that was meant to be worth three waves of
        // average creep value becomes worth thirty-seven.
        // The wave being priced is the one the pool sends at this run every
        // round, not the one the run shops for: what a leak costs is a fact
        // about whatever walked past, and what walks past this run's defense is
        // the field's wave.
        UnitTypeTable types = TheRun.UnkillableTypes();
        Run run = TheRun.Unstoppable(fieldSize: 4);
        int incoming = TheRun.FullLeakCost(run.Costs, TheRun.Orders(types).Wave);

        Assert.Equal((23 * 10) + (17 * 9), incoming);
        Assert.Equal(383, incoming);

        // The pool is worth about three waves of average creep value: the third
        // concession is affordable and the fourth is the end of the run.
        Assert.Equal(1500, TheRuleset.Committed().HealthPoolGold);

        var health = new List<int>();

        while (!run.IsOver)
        {
            RoundReport round = run.Advance(TheBuild.Shopping(run));
            Assert.Equal(incoming, round.Outcome.LeakCostTaken);
            health.Add(run.Health);
        }

        Assert.Equal(new[] { 1117, 734, 351, 0 }, health);
        Assert.Equal(RunEnding.OutOfHealth, run.Ending);
        Assert.Equal(3, run.Outcome.WavesSurvived);
        Assert.Equal(4, run.Round);
    }

    [Fact]
    public void Damage_taken_in_a_round_is_the_field_average_and_never_the_sum()
    {
        // Ten opponents' leaks are one round's worth of damage between them.
        // Summed, a field would be a punishment for being in one: the same
        // round against ten identical opponents would cost ten times what it
        // costs against one.
        //
        // Two fields of ten on one seed, fought with a table nothing can kill,
        // so every wave leaks in full and each pairing costs exactly what the
        // wave it faced cost. Ten opponents sending the 383 wave cost 383; a
        // field split between that wave and a 100 wave costs something strictly
        // between the two, which is what neither the sum, the largest nor the
        // smallest of them can be.
        //
        // OBSERVED: drop the division in Run.Play and record the sums. The
        // uniform assertion goes red, 383 against 3830, and the unstoppable run
        // in the test above dies inside its first round instead of its fourth.
        //
        // OBSERVED: keep the largest of the K instead -- `taken = one *
        // field.Length > taken ? one * field.Length : taken;` in Run.Play.
        // The lopsided assertion goes red at 383, outside the range 101 to 382:
        // a field is only ever as costly as its worst member, and the nine
        // others in it stop counting for anything.
        UnitTypeTable types = TheRun.UnkillableTypes();
        RoundOrders heavy = TheRun.Orders(types, 6, 6);
        RoundOrders light = TheRun.Orders(types, 6, 1);

        Assert.Equal(383, TheRun.FullLeakCost(TheRuleset.Costs(), heavy.Wave));
        Assert.Equal(100, TheRun.FullLeakCost(TheRuleset.Costs(), light.Wave));

        Assert.Equal(383, TheRun.Against(types, heavy).Outcome.LeakCostTaken);
        Assert.InRange(TheRun.Against(types, heavy, light).Outcome.LeakCostTaken, 101, 382);
    }

    [Fact]
    public void A_rounds_offense_is_the_average_of_the_K_resolutions_and_not_the_best_of_them()
    {
        // One round against one defense, at every field size from one to ten.
        // A pairing's match seed is derived from the pairing rather than from
        // who was drawn, so widening the field adds resolutions and never moves
        // the ones already there -- which makes the sequence of scores an oracle
        // for how the K of them are folded. Averaged it wanders; taking the best
        // of them, it can only ever climb, because the best of a longer list
        // cannot be smaller than the best of its own prefix.
        //
        // OBSERVED: keep the best of the K resolutions instead of averaging --
        // `dealt = one * field.Length > dealt ? one * field.Length : dealt;` in
        // Run.Play. The sequence goes red as a staircase that never falls,
        // [17, 17, 17, 17, 17, 17, 17, 17, 17, 17] -- flat here because the
        // luckiest pairing of a one-member pool is already in the field of one,
        // and a score decided by that pairing is a score the other nine cannot
        // move.
        //
        // The wave is the one the round bought out of its opening purse, so the
        // ceiling every score is held under is what that wave cost rather than
        // a number this test named.
        UnitTypeTable types = TheMatch.Types();
        RoundOrders thin = TheRun.Orders(types, 1, 6);
        RoundOrders whole = TheRun.Orders(types, 6, 6);

        int[] widening = Enumerable.Range(1, 10)
            .Select(fieldSize => TheRun.Against(types, fieldSize, whole).Outcome.LeakCostDealt)
            .ToArray();

        bool fell = false;

        for (int index = 1; index < widening.Length; index++)
        {
            fell |= widening[index] < widening[index - 1];
        }

        Assert.True(
            fell,
            "The offense score only ever climbed as the field widened -- ["
            + string.Join(", ", widening.Select(score => score.ToString(CultureInfo.InvariantCulture)))
            + "] -- which is what the best of the K looks like rather than the average of them.");

        // And what the field is made of moves the score, so it is a fold over
        // the whole of it rather than over whoever came first.
        RoundReport against = TheRun.Against(types, thin, whole);
        int weakest = TheRun.Against(types, thin).Outcome.LeakCostDealt;
        int mixed = against.Outcome.LeakCostDealt;
        int ceiling = TheRun.FullLeakCost(TheRuleset.Costs(), against.Build.Wave);

        Assert.True(mixed < weakest, "A field with whole defenses in it scored no less than a field without.");
        Assert.InRange(mixed, 1, ceiling);
        Assert.InRange(weakest, 1, ceiling);
    }

    [Fact]
    public void Death_is_a_flag_so_a_sweep_row_always_yields_N_rounds_of_data()
    {
        // The same run that dies in its fourth round above, with the flag off:
        // ten rounds of data, health on the floor from the fourth onwards, and
        // waves survived still saying three. A sweep needs the full row.
        //
        // OBSERVED: have Run.IsOver report true at zero health whatever the
        // flag says. The round-count assertion goes red, 10 against 4, and a
        // sweep's rows become as long as each row's luck.
        Run run = Played(TheRun.Unstoppable(deathEndsTheRun: false, fieldSize: 1));

        Assert.Equal(10, run.Outcome.Rounds.Count);
        Assert.Equal(RunEnding.OutOfWaves, run.Ending);
        Assert.Equal(0, run.Health);
        Assert.Equal(3, run.Outcome.WavesSurvived);
        Assert.All(run.Outcome.Rounds, round => Assert.Equal(383, round.LeakCostTaken));
    }

    [Fact]
    public void The_run_ends_the_moment_health_reaches_zero()
    {
        // The wall sits at the bottom of a graded pool, and it is a wall: there
        // is no eleventh-hour round to be sold one of.
        //
        // OBSERVED: let health end only a run whose round cap was lifted --
        // add `&& waves == Purse.RoundCapLifted` to the first branch of
        // RunOutcome's ending. The round-count assertion goes red, 4 against
        // 10, and the run plays out its remaining six waves on a pool of
        // nothing.
        Run run = Played(TheRun.Unstoppable(fieldSize: 1));

        Assert.Equal(0, run.Health);
        Assert.True(run.IsOver);
        Assert.Equal(4, run.Round);
        Assert.True(run.Waves > run.Round, "The run ran out of waves rather than out of health.");

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => run.Advance(TheBuild.BuyingNothing(run.Offering)));

        Assert.Contains("This run is over", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_anywhere_in_a_run_can_put_health_back()
    {
        // Gold cannot repair health, so the pool is a clock and nobody is sold
        // a way to stay in a run they are losing. The claim is structural: a
        // Repair(int) added later would look perfectly reasonable at the call
        // site that used it, so what is asserted is that Advance is the only
        // member that moves anything at all.
        //
        // OBSERVED: add an empty `public void Repair(int gold)` to Run. The
        // first assertion goes red, ["Advance", "OfferingAt"] against
        // ["Advance", "OfferingAt", "Repair"], which is the whole of what this
        // test is here to notice -- and it notices a member that does not even
        // do anything yet.
        //
        // Two names are on the list and only one of them moves anything.
        // Advance takes a build phase and nothing else, so what it spends and
        // what it unlocks are read off a round's own offering.
        // OfferingAt is a draw over the run's seed and is asserted below to
        // leave the run exactly where it found it.
        string[] movers = typeof(Run)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Advance", "OfferingAt" }, movers);
        Assert.Null(typeof(Run).GetProperty("Health")!.SetMethod);
        Assert.Null(typeof(RunOutcome).GetProperty("HealthRemaining")!.SetMethod);

        // Reading the offering is a read: it costs nothing, moves nothing and
        // answers the same thing twice.
        //
        // OBSERVED: pay the run a coin for reading -- open Run.OfferingAt with
        // Purse = Purse.Holding(Purse.Gold + 1). The purse assertion goes red,
        // 104 against 108, which is a member on the movers list moving
        // something while the list itself stays exactly as long.
        Run untouched = TheRun.Fresh(waves: 4, fieldSize: 3);

        Assert.Equal(untouched.Health, Drawn(untouched).Health);
        Assert.Equal(untouched.Purse.Gold, Drawn(untouched).Purse.Gold);
        Assert.Equal(0, Drawn(untouched).Unlocks.Count);

        // And across a run it only ever goes one way, whatever the purse beside
        // it did. The purse is checked against the round's own ledger -- what it
        // opened on, less what the build cost, plus what the wave paid -- so a
        // round that shops has its money accounted for both ways while the
        // health beside it is only ever spent.
        //
        // OBSERVED: pay the run for its leaks -- write
        // `Purse.Holding(purse.Gold + outcome.LeakCostTaken)` in place of the
        // purse Run.Commit is handed. The ledger goes red, 117 against 218: gold
        // appeared in the purse that no build phase and no wave payment
        // accounts for.
        Run run = TheRun.Fresh(waves: 4, fieldSize: 3);
        int previous = run.Health;
        int purse = run.Purse.Gold;

        while (!run.IsOver)
        {
            RoundReport round = run.Advance(TheBuild.Shopping(run));

            Assert.True(
                run.Health <= previous,
                "Health went up from "
                + previous.ToString(CultureInfo.InvariantCulture)
                + " to "
                + run.Health.ToString(CultureInfo.InvariantCulture)
                + ".");

            Assert.True(round.Build.Spent > 0, "The round bought nothing, so its purse never moved.");
            Assert.Equal(purse - round.Build.Spent + round.Payment.Total, run.Purse.Gold);

            previous = run.Health;
            purse = run.Purse.Gold;
        }
    }

    [Fact]
    public void Runs_rank_by_waves_survived_then_health_and_the_offense_never_enters_the_placing()
    {
        // The graded pool is both the resource during the run and the order at
        // the end of it. What the offense earned is gold, and it is on the
        // vector, and it is nowhere in the comparison.
        //
        // OBSERVED: compare LeakCostDealt between the waves and the health in
        // RunOutcome.CompareTo. The last pair of assertions goes red, 0 against
        // 1: two runs that survived the same waves on the same health are no
        // longer level, because one of them sent a better wave.
        int pool = TheRuleset.Committed().HealthPoolGold;

        RunOutcome healthier = Outcome(pool, (0, 400), (0, 400), (0, 400));
        RunOutcome thinner = Outcome(pool, (0, 500), (0, 500), (0, 400));
        RunOutcome shorter = Outcome(pool, (0, 1500));
        RunOutcome loud = Outcome(pool, (9000, 400), (9000, 400), (9000, 400));

        Assert.Equal(3, healthier.WavesSurvived);
        Assert.Equal(300, healthier.HealthRemaining);
        Assert.Equal(3, thinner.WavesSurvived);
        Assert.Equal(100, thinner.HealthRemaining);
        Assert.Equal(0, shorter.WavesSurvived);
        Assert.Equal(0, shorter.HealthRemaining);

        // Fewer waves is below more waves, whatever the health.
        Assert.True(healthier.CompareTo(shorter) < 0);
        Assert.True(shorter.CompareTo(healthier) > 0);

        // Level on waves, so health decides.
        Assert.True(healthier.CompareTo(thinner) < 0);
        Assert.True(thinner.CompareTo(healthier) > 0);

        // Level on both, and one of them earned twenty-seven thousand gold.
        Assert.Equal(27000, loud.LeakCostDealt);
        Assert.Equal(0, healthier.LeakCostDealt);
        Assert.Equal(0, healthier.CompareTo(loud));
        Assert.Equal(0, loud.CompareTo(healthier));
    }

    [Fact]
    public void Health_and_waves_survived_are_folds_over_the_vector_and_need_no_re_simulation()
    {
        // The whole reason the outcome is a vector: a percentile band, a
        // placing or a retrospective computed later is arithmetic over what was
        // stored, and nothing has to be simulated twice to get it.
        //
        // OBSERVED: fold the run against `_rules.HealthPoolGold * 2` in
        // Run.Folded, so that the run's own health comes off a pool the vector
        // does not carry. The first assertion goes red, 2237 against 737 --
        // which is what one number kept in two places looks like the moment the
        // two of them disagree.
        Run run = Played(TheRun.Fresh(waves: 5, fieldSize: 4));

        RunOutcome rebuilt = RunOutcome.Of(
            TheRuleset.Committed().HealthPoolGold,
            run.Outcome.Rounds,
            run.Waves,
            run.DeathEndsTheRun);

        Assert.Equal(run.Outcome.HealthRemaining, rebuilt.HealthRemaining);
        Assert.Equal(run.Outcome.WavesSurvived, rebuilt.WavesSurvived);
        Assert.Equal(run.Outcome.LeakCostDealt, rebuilt.LeakCostDealt);
        Assert.Equal(run.Outcome.LeakCostTaken, rebuilt.LeakCostTaken);
        Assert.Equal(run.Outcome.Ending, rebuilt.Ending);
        Assert.Equal(run.Health, rebuilt.HealthRemaining);
    }

    [Fact]
    public void The_distribution_the_bands_are_measured_against_comes_out_of_the_pool()
    {
        // The canned field is a parameter and not a fixture: the pool a run is
        // handed IS the population its percentile is a percentile of, so
        // swapping the canned stand-in for a stored population of real rounds is
        // that one argument and nothing else. A run cannot be given a pool and a
        // distribution that disagree, because there is only the one of them.
        //
        // OBSERVED: measure nothing -- return PerformanceField.Of(new int[0])
        // from Run.MeasureField, which is the Absent a run carried before there
        // was a pool to measure. This goes red on the first assertion, IsPresent
        // against a field of nobody, and the run goes back to paying the base
        // alone.
        Run run = TheRun.Fresh(waves: 4, fieldSize: 4);

        Assert.True(run.Field.IsPresent);
        Assert.Equal(Run.FieldSamples, run.Field.Size);

        // The same seed, the same shape, and two populations: one whose rounds
        // are worth almost nothing and one whose rounds are worth three hundred
        // gold. Two hundred tops the first outright and beats none of the
        // second, which no distribution fixed in code could say.
        UnitTypeTable types = TheMatch.Types();

        Run againstThin = Against(types, FieldPool.Of(new[] { TheRun.Orders(types, 6, 1) }));
        Run againstFat = Against(types, FieldPool.Of(new[] { TheRun.Orders(types, 1, 4) }));

        Assert.Equal(100, againstThin.Field.PercentileOf(200));
        Assert.Equal(0, againstFat.Field.PercentileOf(200));

        // What the measurement says does not depend on when it is asked or on
        // what the run has done: it is the seed, the pool and K, and nothing a
        // round moves is in it.
        againstThin.Advance(TheBuild.Shopping(againstThin));

        Assert.Equal(100, againstThin.Field.PercentileOf(200));
        Assert.Equal(Run.FieldSamples, againstThin.Field.Size);
    }

    [Fact]
    public void What_a_run_earned_for_its_waves_is_arithmetic_over_its_vector_and_never_a_second_play()
    {
        // A round of a run that shops moves the purse three ways: the build
        // phase takes what its wave cost out of it, the bank pays interest on
        // whatever survived that, and the wave pays the flat base and the band
        // its offense reached in the field. Folded here out of the stored
        // vector, the run's own field and what each round reported spending,
        // with no match resolved and no tick replayed -- and it has to come out
        // at the gold the run actually holds.
        //
        // The run spends a third of its purse a round, so all three lines are
        // real numbers rather than one line and two zeroes: it buys every round,
        // it banks the rest at compound interest, and every one of its ten
        // rounds is placed above the bottom band.
        //
        // OBSERVED: pay the wave off outcome.LeakCostTaken rather than
        // outcome.LeakCostDealt in Run.Play. The purse assertion goes red: the
        // run starts being paid for what got past it rather than for what it
        // got past the field, and the fold and the payment stop being the same
        // arithmetic.
        //
        // OBSERVED: leave the spend out of the fold -- drop the Purse.Holding
        // line below, which is the shape this test had while the run it folded
        // over bought nothing. It goes red at 824 against 8399: the 4006 gold of
        // creeps, and the interest a bank that never paid for them would have
        // compounded on top.
        Run run = TheRun.Wealthy(2000);
        Ruleset rules = run.Rules;
        var rounds = new List<RoundReport>();

        while (!run.IsOver)
        {
            rounds.Add(run.Advance(TheBuild.Shopping(run, run.Purse.Gold / 3)));
        }

        Purse folded = Purse.Holding(rules.StartingPurseGold);
        int bonus = 0;
        int spent = 0;

        for (int round = 0; round < run.Outcome.Rounds.Count; round++)
        {
            spent += rounds[round].Build.Spent;
            folded = Purse.Holding(folded.Gold - rounds[round].Build.Spent);

            WavePayment paid = folded.CloseWave(
                rules, run.Field, run.Outcome.Rounds[round].LeakCostDealt);

            bonus += paid.Bonus;
            folded = paid.Purse;
        }

        Assert.Equal(run.Purse.Gold, folded.Gold);
        Assert.Equal(bonus, Purse.BonusOver(rules, run.Field, run.Outcome));

        // And all three are money rather than columns of zeroes: the run bought
        // waves, attacking paid its sender, and turning up paid on top.
        Assert.Equal(4006, spent);
        Assert.Equal(330, bonus);
        Assert.Equal(10, rounds.Count(round => round.Payment.Bonus > 0));
        Assert.Equal(1680, rules.IncomeBasePerWave * run.Round);
    }

    [Fact]
    public void A_rounds_field_is_drawn_from_the_seed_and_the_round_and_not_from_where_the_last_draw_left_off()
    {
        // The claim that makes a run reproducible from its record, and the one
        // an ambient stream fails: round three's field cannot depend on what
        // rounds one and two did. Two runs on one seed come out of two
        // different openings and then send one wave into one field, and what
        // that wave got past has to come back identical.
        //
        // OBSERVED: mix the health the run has spent so far into the field
        // draw's position in Run.FieldFor -- the shape a draw taken from
        // wherever the match stream left off has, since where that is depends
        // on how many shots were fired. The offense assertion goes red, 185
        // against 122: two runs meet different fields in a round they sent one
        // identical wave into.
        //
        // The two runs differ in the board each of them stands behind and in
        // nothing else: one of them opens by building a tower, the other by
        // building nothing, and neither sends a creep until the third round.
        // What a round deals is a fact about the wave it sent and the defenses
        // that met it, so a tower costs its run gold and saves it health
        // without ever moving what it got past the field. The third round is
        // shopped to the smaller of the two purses, so both send one identical
        // wave; what that round's draw must not be able to see is the health
        // and the gold between them.
        // A cell content/defense.txt stands a mortar on, so it is one a tower
        // can be put on and one that watches the corridor.
        BuildAction mortar = BuildAction.Of(ActionKind.Place, 4, 9, 0);

        Run one = TheRun.Fresh(waves: 3, fieldSize: 4);
        Run two = TheRun.Fresh(waves: 3, fieldSize: 4);

        one.Advance(TheBuild.BuyingNothing(one.Offering));
        two.Advance(TheBuild.BuyingNothing(two.Offering).With(mortar));

        one.Advance(TheBuild.BuyingNothing(one.Offering));
        two.Advance(TheBuild.BuyingNothing(two.Offering));

        int budget = Math.Min(one.Purse.Gold, two.Purse.Gold);

        RoundOutcome fromOne = one.Advance(TheBuild.Shopping(one, budget)).Outcome;
        RoundOutcome fromTwo = two.Advance(TheBuild.Shopping(two, budget)).Outcome;

        Assert.True(
            fromOne.LeakCostDealt > 0,
            "Round three got nothing past anybody, so which field it met cannot be read off what it dealt.");

        Assert.Equal(fromOne.LeakCostDealt, fromTwo.LeakCostDealt);

        // The openings really were different, so the agreement above is about
        // the derivation rather than about two runs that played the same game.
        Assert.NotEqual(one.Outcome.Rounds[0].LeakCostTaken, two.Outcome.Rounds[0].LeakCostTaken);

        // Same seed, same run. A different seed, a different run.
        Assert.Equal(
            Played(TheRun.Fresh(waves: 2, fieldSize: 4)).Outcome.LeakCostTaken,
            Played(TheRun.Fresh(waves: 2, fieldSize: 4)).Outcome.LeakCostTaken);

        Assert.NotEqual(
            Played(TheRun.Fresh(waves: 2, fieldSize: 4)).Outcome.LeakCostTaken,
            Played(TheRun.Fresh(2, 4, true, TheRun.Seed + 1)).Outcome.LeakCostTaken);
    }

    [Fact]
    public void The_run_stores_its_own_rounds_so_they_can_enter_somebody_elses_field()
    {
        // Symmetry across a field of ten is restored across time rather than
        // within a round: what I stood and sent this week is what somebody else
        // is measured against next week. It is stored unconditionally, in every
        // configuration, and it is the same type a field is made of -- so a
        // finished run drops into a pool with nothing converted.
        //
        // OBSERVED: only record the orders while the run is alive -- guard the
        // _sent.Add on Health being above zero. The no-death row goes red, 10
        // against 4, and the rounds a losing player played stop entering
        // anybody's field.
        // What a run stands is what it built, so the wall goes up a tower a
        // round and every set of orders it stored carries the board of the
        // round it was sent in: one more tower each time, and the last of them
        // the board the run finished on.
        Run alive = Played(TheRun.Fresh(waves: 3, fieldSize: 2), TheBuild.Fortifying);
        Run dead = Played(TheRun.Unstoppable(deathEndsTheRun: false, fieldSize: 1));

        Assert.Equal(3, alive.Sent.Count);
        Assert.Equal(new[] { 1, 2, 3 }, alive.Sent.Select(sent => sent.Defense.Count));
        Assert.Equal(
            TheMatch.Spelling(alive.Board.Layout()),
            TheMatch.Spelling(alive.Sent[alive.Sent.Count - 1].Defense));
        Assert.All(alive.Sent, sent => Assert.True(sent.Wave.TotalUnits > 0));
        Assert.Equal(10, dead.Sent.Count);

        // And what came out is what a field is made of.
        FieldPool next = FieldPool.Of(alive.Sent);
        Assert.Equal(3, next.Size);
        Assert.Same(alive.Sent[0], next.At(0));
    }

    [Fact]
    public void A_field_of_nobody_at_all_is_refused()
    {
        // OBSERVED: drop the fieldSize guard in the Run constructor. This goes
        // red having caught nothing -- a run of no opponents at all constructs
        // perfectly, and what it does when somebody advances it is decided by
        // whichever arithmetic reaches zero first.
        SimulationException thrown = Assert.Throws<SimulationException>(() => TheRun.Fresh(fieldSize: 0));

        Assert.Contains("field of 0 opponents", thrown.Message, StringComparison.Ordinal);
        Assert.Throws<SimulationException>(() => TheRun.Fresh(fieldSize: -1));
    }

    [Fact]
    public void A_pool_with_nobody_in_it_is_refused()
    {
        // OBSERVED: drop the empty check in FieldPool.Of. This goes red having
        // caught nothing: a population of nobody builds a perfectly good pool,
        // and what a run makes of it is left to whichever draw reaches the
        // empty array first.
        SimulationException thrown =
            Assert.Throws<SimulationException>(() => FieldPool.Of(new RoundOrders[0]));

        Assert.Contains("pool of nobody", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_canned_pool_is_one_pair_of_orders_and_a_field_is_that_pair_over_and_over()
    {
        // The population every run in this repository is played against, and it
        // is composed here rather than by whoever happened to read the two
        // files: a defense and a wave are one member, and a field of any width
        // is that member drawn as many times.
        //
        // OBSERVED: hand Canned the pool's own wave twice -- Of(new[] { orders,
        // orders }) -- and the size assertion goes red at 2 against 1. A pool
        // whose members are all the same opponent is a field that reads exactly
        // like a wide one and is not one.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout defense = TheMatch.Layout(types);
        WaveScript wave = TheRun.FieldWave(types);

        FieldPool canned = FieldPool.Canned(defense, wave);

        Assert.Equal(1, canned.Size);
        Assert.Same(defense, canned.At(0).Defense);
        Assert.Same(wave, canned.At(0).Wave);
    }

    [Fact]
    public void A_run_with_no_last_wave_and_no_death_in_it_is_refused()
    {
        // A run is bounded by its wave count or by its health pool. Lifting
        // both is a loop rather than a row, and it is refused before it starts
        // rather than found by whoever notices the harness never returned.
        //
        // OBSERVED: drop the guard in the Run constructor. This goes red having
        // caught nothing, and what it built is a run whose Ending is Unfinished
        // after every round it will ever resolve.
        Ruleset capped = Ruleset.Parse(
            PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 500"));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => new Run(
                TheMatch.Map(),
                capped,
                TheMatch.Types(),
                TheSchedule.Committed(),
                TheRun.Pool(),
                TheRun.Seed,
                Purse.RoundCapLifted,
                4,
                deathEndsTheRun: false));

        Assert.Contains("no last wave", thrown.Message, StringComparison.Ordinal);

        // With death left on, the same run is fine: health is what bounds it.
        var bounded = new Run(
            TheMatch.Map(),
            capped,
            TheMatch.Types(),
            TheSchedule.Committed(),
            TheRun.Pool(),
            TheRun.Seed,
            Purse.RoundCapLifted,
            4);

        Assert.Equal(Purse.RoundCapLifted, bounded.Waves);
        Assert.Equal(RunEnding.Unfinished, bounded.Ending);
    }

    [Fact]
    public void A_run_that_compounds_with_nothing_to_stop_it_is_refused_where_it_is_constructed()
    {
        // The refusal the purse owns, called from the one place that knows how
        // many rounds there are -- before a wave resolves rather than after the
        // run has produced numbers somebody will keep.
        //
        // OBSERVED: take the RequireBoundedCompounding call out of the Run
        // constructor. This goes red having caught nothing, and a run against
        // the committed ruleset with the cap lifted banks compound interest
        // forever.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => TheRun.Fresh(waves: Purse.RoundCapLifted));

        Assert.Contains("no round cap", thrown.Message, StringComparison.Ordinal);
        Assert.Throws<SimulationException>(() => TheRun.Fresh(waves: -1));
    }

    [Fact]
    public void A_vector_with_more_rounds_on_it_than_the_run_had_waves_is_refused()
    {
        // OBSERVED: drop the length check in RunOutcome.Of. This goes red
        // having caught nothing, and a fourth round folds into a three-wave run
        // -- moving its health and its waves survived by an amount nobody
        // played for.
        int pool = TheRuleset.Committed().HealthPoolGold;

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Outcome(pool, 3, (0, 1), (0, 1), (0, 1), (0, 1)));

        Assert.Contains("4 rounds on it", thrown.Message, StringComparison.Ordinal);

        // The cap being lifted takes any number of them, which is what it is for.
        Assert.Equal(4, Outcome(pool, Purse.RoundCapLifted, (0, 1), (0, 1), (0, 1), (0, 1)).Rounds.Count);
    }

    [Fact]
    public void A_round_recorded_as_having_dealt_or_taken_less_than_nothing_is_refused()
    {
        // OBSERVED: drop the guard in RoundOutcome.Amount. Both rows go red
        // having caught nothing, and a negative round adds health back to a pool
        // that is supposed to be a clock.
        Assert.Throws<SimulationException>(() => new RoundOutcome(-1, 0));
        Assert.Throws<SimulationException>(() => new RoundOutcome(0, -1));

        Assert.Equal(0, default(RoundOutcome).LeakCostDealt);
    }

    [Fact]
    public void A_run_folded_over_a_pool_of_nothing_is_refused()
    {
        // OBSERVED: drop the health-pool guard in RunOutcome.Of. This goes red
        // having caught nothing, and a run that began on no health reads back as
        // a death nothing caused -- zero waves survived against an opponent it
        // never met.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => RunOutcome.Of(0, new RoundOutcome[0], 10, true));

        Assert.Contains("started on 0 health", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_leak_priced_beyond_what_a_purse_can_hold_is_a_throw_and_not_a_wrap()
    {
        // A wave whose orders each fit in a purse and whose total does not. The
        // refusal wanted is the one over the summed orders: the cost table's own
        // guard is per line item and never sees the wave.
        //
        // OBSERVED: sum the leak cost into an int in Run.LeakCost and drop the
        // range check. The refusal fires one layer later, from PerformanceField,
        // naming a field that dealt -294967296 in leak cost -- so this goes red
        // on the message rather than on the throw. That is why the refusal is
        // asserted by name: a wrapped total should say which arithmetic wrapped
        // rather than surface as a field that gave gold back.
        UnitTypeTable types = TheRun.RuinouslyPricedTypes();

        var run = new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheSchedule.Committed(types),
            FieldPool.Of(new[] { TheRun.Orders(types) }),
            TheRun.Seed,
            waves: 1,
            fieldSize: 1);

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => run.Advance(TheBuild.BuyingNothing(run.Offering)));

        Assert.Contains("does not fit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_round_that_throws_after_its_matches_leaves_the_run_exactly_where_it_was()
    {
        // Every match of the round resolves, and then the wave is paid, which
        // can refuse: a purse near the top of its range is the refusal a run can
        // be built to reach, because the interest alone carries the close past
        // what a purse is kept in. A build phase has resolved a take and a
        // purchase in front of all of it, and both have to survive the refusal
        // as nothing at all.
        //
        // OBSERVED: put the two Add calls and the purse assignment back above
        // Purse.CloseWave in Run.Play. The round count goes red, 1 against 0,
        // and the run carries a round on its vector and in what it sent for a
        // wave that was never paid for -- with an outcome still folded from
        // before it, which nothing downstream could tell from a run somebody
        // played.
        //
        // OBSERVED: assign build.Unlocks and build.Purse to the run in
        // Run.Advance before the round is played. The unlock count goes red, 1
        // against 0, and the run carries a take and a spent purse for a wave
        // nobody was in the run to send.
        Ruleset rules = Ruleset.Parse(
            PlantedText.Replace(TheRuleset.CommittedText(), "purse         100", "purse  2147483000"));

        UnitTypeTable types = TheMatch.Types();

        var run = new Run(
            TheMatch.Map(),
            rules,
            types,
            TheSchedule.Committed(types),
            TheRun.Pool(types),
            TheRun.Seed,
            waves: 2,
            fieldSize: 2);

        // A take and a slot of the creep it unlocks, so the round has both a
        // purse to leave alone and an unlock to leave untaken.
        BuildPhase phase = TheBuild.TakeFirst(
            run.Offering, WaveSlot.Of(run.Offering.Options[0].TypeId, 1));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => run.Advance(phase));

        Assert.Contains(
            "does not fit in the 32-bit integer a purse is kept in",
            thrown.Message,
            StringComparison.Ordinal);

        Assert.Equal(0, run.Round);
        Assert.Empty(run.Sent);
        Assert.Empty(run.Outcome.Rounds);
        Assert.Equal(0, run.Unlocks.Count);
        Assert.Equal(rules.HealthPoolGold, run.Health);
        Assert.Equal(rules.StartingPurseGold, run.Purse.Gold);
        Assert.False(run.IsOver);
    }

    [Fact]
    public void A_round_whose_field_measurement_refuses_leaves_the_run_exactly_where_it_was()
    {
        // The second of the three things a round can refuse at. What a round of
        // the pool is worth is measured on first use, which is inside the first
        // round of the run, and measuring plays matches of its own -- so it can
        // refuse for the reason a round's own matches can.
        //
        // The measurement is the only thing here that reaches for that refusal,
        // and the pool is two members so that it is. Ruinously priced, the long
        // wave costs more than a purse can hold once it leaks and the short one
        // does not; the round is fought against the short member and the
        // measurement draws the long one. Measured: with the Field read in
        // Run.Play moved back down to the CloseWave call, so that the round's
        // own matches all resolve first, this still throws -- none of them was
        // ever over the limit.
        //
        // OBSERVED: give the pool's second member the short wave too --
        // TheRun.Orders(types, 6, 1) for both. The test goes red having caught
        // nothing, because the round plays out: no match in it, measured or
        // fought, leaks more gold than a purse can hold.
        UnitTypeTable types = TheRun.RuinouslyPricedTypes();
        Ruleset rules = TheRuleset.Committed();

        var run = new Run(
            TheMatch.Map(),
            rules,
            types,
            TheSchedule.Committed(types),
            FieldPool.Of(new[] { TheRun.Orders(types, 6, 1), TheRun.Orders(types, 6, 6) }),
            TheRun.Seed,
            waves: 1,
            fieldSize: 1);

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => run.Advance(TheBuild.BuyingNothing(run.Offering)));

        Assert.Contains(
            "does not fit in the 32-bit integer health and gold",
            thrown.Message,
            StringComparison.Ordinal);

        Assert.Equal(0, run.Round);
        Assert.Empty(run.Sent);
        Assert.Empty(run.Outcome.Rounds);
        Assert.Equal(0, run.Unlocks.Count);
        Assert.Equal(rules.HealthPoolGold, run.Health);
        Assert.Equal(rules.StartingPurseGold, run.Purse.Gold);
    }

    [Fact]
    public void A_round_whose_outcome_will_not_fold_leaves_the_run_exactly_where_it_was()
    {
        // The third. What a run has dealt and taken are totals over the whole
        // vector, so the round that carries one of them out of the range gold is
        // counted in is refused by the fold rather than by anything inside the
        // round -- and the fold is the last thing a round does.
        //
        // A pool sending ten ruinously priced leakers costs a round a thousand
        // million in health, which two rounds fit and three do not. Death does
        // not end this run, so the rounds past the first are rounds it is still
        // in to play.
        //
        // OBSERVED: fold after the appends rather than before them -- hand
        // Run.Commit the run's current _outcome in place of
        // FoldedWith(outcome), and end Commit with
        // `_outcome = Folded(_rounds);`. The round count goes red, 3 against 2,
        // and the run carries a round its own outcome was never folded over.
        UnitTypeTable types = TheRun.RuinouslyPricedTypes();

        var run = new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheSchedule.Committed(types),
            FieldPool.Of(new[] { TheRun.Orders(types, 6, 1) }),
            TheRun.Seed,
            waves: 3,
            fieldSize: 1,
            deathEndsTheRun: false);

        run.Advance(TheBuild.BuyingNothing(run.Offering));
        run.Advance(TheBuild.BuyingNothing(run.Offering));

        int purse = run.Purse.Gold;
        RunOutcome outcome = run.Outcome;

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => run.Advance(TheBuild.BuyingNothing(run.Offering)));

        Assert.Contains(
            "in leak cost taken, which does not fit in the 32-bit integer gold is counted in",
            thrown.Message,
            StringComparison.Ordinal);

        Assert.Equal(2, run.Round);
        Assert.Equal(2, run.Sent.Count);
        Assert.Equal(2, run.Unlocks.Count);
        Assert.Equal(purse, run.Purse.Gold);
        Assert.Same(outcome, run.Outcome);
    }

    [Fact]
    public void A_round_hands_back_what_it_took_what_it_cost_and_how_its_wave_was_paid()
    {
        // A round settles all three while it is being played, so all three come
        // back. What is asserted is that they are the round's own numbers and
        // not a second computation beside it: the pair is the one the run folded
        // in, the build's purse is the one the payment opened on, and the
        // payment's purse is the one the run now holds.
        //
        // OBSERVED: compose the report's payment from
        // build.Purse.CloseWaveAtBest(Rules) -- the ceiling the load walk
        // carries -- rather than from the payment the round made. The closing
        // assertion goes red, 192 against 212: a report of what a wave earned
        // that says what it could have earned instead.
        Run run = TheBuild.Fresh(waves: 2);

        Option first = run.Offering.Options[0];
        int opening = run.Purse.Gold;
        int price = run.Costs.PriceOf(Purchase.Unit(first.TypeId));

        RoundReport round = run.Advance(BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, 1)));

        // The pair is the one on the run's own vector.
        Assert.Equal(run.Outcome.Rounds[0].LeakCostDealt, round.Outcome.LeakCostDealt);
        Assert.Equal(run.Outcome.Rounds[0].LeakCostTaken, round.Outcome.LeakCostTaken);

        // The build is the decision as it resolved, priced out of the run's own
        // table and leaving the purse it charged.
        Assert.True(price > 0, "The creep on this round's menu is free, so nothing here costs anything.");
        Assert.Equal(first.Id, round.Build.Taken.Id);
        Assert.Equal(price, round.Build.Spent);
        Assert.Equal(opening - price, round.Build.Purse.Gold);

        // And the payment runs from what the wave left to what the run holds,
        // itemised into the three lines that make up the difference.
        Assert.Equal(round.Build.Purse.Gold, round.Payment.Opening);
        Assert.Equal(run.Purse.Gold, round.Payment.Purse.Gold);
        Assert.Equal(run.Rules.IncomeBasePerWave, round.Payment.IncomeBase);
        Assert.Equal(
            run.Purse.Gold - round.Payment.Opening,
            round.Payment.Interest + round.Payment.IncomeBase + round.Payment.Bonus);
    }

    [Fact]
    public void A_rounds_line_counts_what_stands_after_its_own_building()
    {
        // The count is read off the board the phase left rather than off the
        // one it was handed, because the purse walks the take, then the
        // actions, then the slots: the board this round's incoming waves meet
        // is the built one, so the line beside those waves has to say so.
        //
        // OBSERVED: hand the board the phase was given to the Build it returns
        // -- `board` rather than `built` at the bottom of BuildPhase.Resolve.
        // This goes red, the line reading 6 towers standing where 7 was
        // wanted: a board from before the round it is written on.
        Run run = TheBuild.Fresh(waves: 2);
        int opening = run.Board.Count;

        RoundReport round = run.Advance(
            TheBuild.BuyingNothing(run.Offering).With(TheCommands.PlacedOnFreeCell));

        Assert.Contains(
            ", "
            + (opening + 1).ToString(CultureInfo.InvariantCulture)
            + " towers standing, spent ",
            round.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_match_counts_its_leaks_by_the_order_that_sent_them_because_a_total_cannot_be_priced()
    {
        // What a leak costs is what the thing that leaked cost, so the count a
        // run prices has to say which order walked past. A total cannot: the
        // committed wave sends two types at two prices.
        //
        // OBSERVED: increment _leakedByOrder[0] rather than
        // [creep.OrderIndex] in Match.MoveCreeps. The spread assertion goes red
        // saying every leak in the committed run came from one order, and every
        // leak in the game is priced as though it were a grunt out of the wave's
        // first line.
        UnitTypeTable types = TheMatch.Types();
        WaveScript wave = TheMatch.Wave(types);
        Match match = TheMatch.Fresh();
        MatchResult result = match.Resolve();

        Assert.Equal(wave.Count, match.LeakedByOrder.Count);
        Assert.Equal(result.Leaked, match.LeakedByOrder.Sum());
        Assert.Equal(TheMatch.LeakedInTheCommittedRun, match.LeakedByOrder.Sum());

        Assert.True(
            match.LeakedByOrder.Count(leaked => leaked > 0) > 1,
            "Every leak in the committed run came from one order, so nothing here needs splitting.");

        CostTable costs = TheRuleset.Costs();
        int priced = 0;

        for (int index = 0; index < wave.Count; index++)
        {
            priced += costs.PriceOf(Purchase.Unit(wave.Orders[index].TypeId), match.LeakedByOrder[index]);
        }

        Assert.True(priced > 0);
        Assert.NotEqual(result.Leaked * costs.PriceOf(Purchase.Unit(1)), priced);
    }

    /// <summary>A one-round run on the committed content against a population written out here.</summary>
    private static Run Against(UnitTypeTable types, FieldPool pool) =>
        new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheSchedule.Committed(types),
            pool,
            TheRun.Seed,
            waves: 1,
            fieldSize: 4);

    /// <summary>The same run, after every offering of it has been read.</summary>
    private static Run Drawn(Run run)
    {
        for (int wave = 1; wave <= run.Waves; wave++)
        {
            Assert.Equal(wave, run.OfferingAt(wave).Wave);
        }

        return run;
    }

    /// <summary>A run driven to its end, every round shopping behind its own board.</summary>
    private static Run Played(Run run) => Played(run, TheBuild.Shopping);

    /// <summary>The same, by whichever scripted player the scenario is about.</summary>
    private static Run Played(Run run, Func<Run, BuildPhase> decide)
    {
        while (!run.IsOver)
        {
            run.Advance(decide(run));
        }

        return run;
    }

    /// <summary>An outcome built from pairs rather than from a simulation.</summary>
    private static RunOutcome Outcome(int healthPoolGold, params (int Dealt, int Taken)[] rounds) =>
        Outcome(healthPoolGold, 10, rounds);

    private static RunOutcome Outcome(int healthPoolGold, int waves, params (int Dealt, int Taken)[] rounds) =>
        RunOutcome.Of(
            healthPoolGold,
            rounds.Select(round => new RoundOutcome(round.Dealt, round.Taken)).ToArray(),
            waves,
            deathEndsTheRun: true);
}
