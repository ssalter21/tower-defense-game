using System.Globalization;
using System.Reflection;

namespace Sim.Tests;

/// <summary>
/// The run: N waves, a field of K, a health pool denominated in sauce, and an
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
        // The run survives its last wave by 29 sauce of health, which is what
        // makes the death flag inert here: the no-death row produces the same
        // vector as the rest rather than a longer one, so the flag is an
        // argument and not a different lifecycle.
        //
        // OBSERVED: give the flag a lifecycle of its own -- when death is off,
        // have Run.Advance record an empty round and return without resolving
        // anything. The no-death row goes red on the health it finished with,
        // 29 against 1500, which is what a second code path hiding behind an
        // argument looks like from the outside.
        RoundOrders orders = TheRun.Orders();

        Run run = spellsOutTheLengths
            ? TheRun.Fresh(Run.DefaultWaves, Run.DefaultFieldSize, deathEndsTheRun)
            : TheRun.Fresh(deathEndsTheRun: deathEndsTheRun);

        while (!run.IsOver)
        {
            run.Advance(orders);

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
                again.Advance(orders);
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
        // of FieldSize. The last assertion goes red, not 450 against 450: the
        // two-opponent run comes back with the ten-opponent run's numbers,
        // because K was a constant wearing an argument's name.
        Run defaults = TheRun.Fresh();

        Assert.Equal(10, defaults.Waves);
        Assert.Equal(10, defaults.FieldSize);
        Assert.True(defaults.DeathEndsTheRun);
        Assert.Equal(Run.DefaultWaves, defaults.Waves);
        Assert.Equal(Run.DefaultFieldSize, defaults.FieldSize);

        RoundOrders orders = TheRun.Orders();

        Run shorter = Played(TheRun.Fresh(waves: 3, fieldSize: 10), orders);
        Run narrower = Played(TheRun.Fresh(waves: 3, fieldSize: 2), orders);

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
        // 485 against 40, and the pool that was meant to be worth three waves of
        // average creep value becomes worth thirty-seven.
        UnitTypeTable types = TheRun.UnkillableTypes();
        Run run = TheRun.Unstoppable(fieldSize: 4);
        RoundOrders orders = TheRun.Orders(types);
        int wave = TheRun.FullLeakCost(run.Costs, orders.Wave);

        Assert.Equal((23 * 10) + (17 * 15), wave);
        Assert.Equal(485, wave);

        // The pool is worth about three waves of average creep value: the third
        // concession is affordable and the fourth is the end of the run.
        Assert.Equal(1500, TheRuleset.Committed().HealthPoolSauce);

        var health = new List<int>();

        while (!run.IsOver)
        {
            RoundOutcome round = run.Advance(orders);
            Assert.Equal(wave, round.LeakCostTaken);
            health.Add(run.Health);
        }

        Assert.Equal(new[] { 1015, 530, 45, 0 }, health);
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
        // wave it faced cost. Ten opponents sending the 485 wave cost 485; a
        // field split between that wave and a 100 wave costs something strictly
        // between the two, which is what neither the sum, the largest nor the
        // smallest of them can be.
        //
        // OBSERVED: drop the division in Run.Advance and record the sums. The
        // uniform assertion goes red, 485 against 4850, and the unstoppable run
        // in the test above dies inside its first round instead of its fourth.
        //
        // OBSERVED: keep the largest of the K instead -- `taken = one *
        // field.Length > taken ? one * field.Length : taken;` in Run.Advance.
        // The lopsided assertion goes red at 485, outside the range 101 to 484:
        // a field is only ever as costly as its worst member, and the nine
        // others in it stop counting for anything.
        UnitTypeTable types = TheRun.UnkillableTypes();
        RoundOrders orders = TheRun.Orders(types);
        RoundOrders heavy = TheRun.Orders(types, 6, 6);
        RoundOrders light = TheRun.Orders(types, 6, 1);

        Assert.Equal(485, TheRun.FullLeakCost(TheRuleset.Costs(), heavy.Wave));
        Assert.Equal(100, TheRun.FullLeakCost(TheRuleset.Costs(), light.Wave));

        Assert.Equal(485, TheRun.Against(types, orders, heavy).LeakCostTaken);
        Assert.InRange(TheRun.Against(types, orders, heavy, light).LeakCostTaken, 101, 484);
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
        // Run.Advance. The sequence goes red as a staircase that only climbs,
        // [150, 160, 160, 160, 160, 160, 170, 180, 180, 180], which is a score
        // decided by a run's luckiest pairing.
        UnitTypeTable types = TheMatch.Types();
        RoundOrders orders = TheRun.Orders(types);
        RoundOrders thin = TheRun.Orders(types, 1, 6);
        RoundOrders whole = TheRun.Orders(types, 6, 6);

        int[] widening = Enumerable.Range(1, 10)
            .Select(fieldSize => TheRun.Against(types, orders, fieldSize, whole).LeakCostDealt)
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
        int weakest = TheRun.Against(types, orders, thin).LeakCostDealt;
        int mixed = TheRun.Against(types, orders, thin, whole).LeakCostDealt;
        int ceiling = TheRun.FullLeakCost(TheRuleset.Costs(), orders.Wave);

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
        Run run = Played(TheRun.Unstoppable(deathEndsTheRun: false, fieldSize: 1), TheRun.Orders(TheRun.UnkillableTypes()));

        Assert.Equal(10, run.Outcome.Rounds.Count);
        Assert.Equal(RunEnding.OutOfWaves, run.Ending);
        Assert.Equal(0, run.Health);
        Assert.Equal(3, run.Outcome.WavesSurvived);
        Assert.All(run.Outcome.Rounds, round => Assert.Equal(485, round.LeakCostTaken));
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
        Run run = Played(TheRun.Unstoppable(fieldSize: 1), TheRun.Orders(TheRun.UnkillableTypes()));

        Assert.Equal(0, run.Health);
        Assert.True(run.IsOver);
        Assert.Equal(4, run.Round);
        Assert.True(run.Waves > run.Round, "The run ran out of waves rather than out of health.");

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => run.Advance(TheRun.Orders(TheRun.UnkillableTypes())));

        Assert.Contains("This run is over", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_anywhere_in_a_run_can_put_health_back()
    {
        // Sauce cannot repair health, so the pool is a clock and nobody is sold
        // a way to stay in a run they are losing. The claim is structural: a
        // Repair(int) added later would look perfectly reasonable at the call
        // site that used it, so what is asserted is that Advance is the only
        // member that moves anything at all.
        //
        // OBSERVED: add an empty `public void Repair(int sauce)` to Run. The
        // first assertion goes red, ["Advance"] against ["Advance", "Repair"],
        // which is the whole of what this test is here to notice -- and it
        // notices a member that does not even do anything yet.
        string[] movers = typeof(Run)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Advance" }, movers);
        Assert.Null(typeof(Run).GetProperty("Health")!.SetMethod);
        Assert.Null(typeof(RunOutcome).GetProperty("HealthRemaining")!.SetMethod);

        // And across a run it only ever goes one way, however much the purse
        // grows alongside it.
        Run run = TheRun.Fresh(waves: 4, fieldSize: 3);
        RoundOrders orders = TheRun.Orders();
        int previous = run.Health;
        int purse = run.Purse.Sauce;

        while (!run.IsOver)
        {
            run.Advance(orders);

            Assert.True(
                run.Health <= previous,
                "Health went up from "
                + previous.ToString(CultureInfo.InvariantCulture)
                + " to "
                + run.Health.ToString(CultureInfo.InvariantCulture)
                + ".");

            Assert.True(run.Purse.Sauce > purse, "The wave paid the purse nothing at all.");

            previous = run.Health;
            purse = run.Purse.Sauce;
        }
    }

    [Fact]
    public void Runs_rank_by_waves_survived_then_health_and_the_offense_never_enters_the_placing()
    {
        // The graded pool is both the resource during the run and the order at
        // the end of it. What the offense earned is sauce, and it is on the
        // vector, and it is nowhere in the comparison.
        //
        // OBSERVED: compare LeakCostDealt between the waves and the health in
        // RunOutcome.CompareTo. The last pair of assertions goes red, 0 against
        // 1: two runs that survived the same waves on the same health are no
        // longer level, because one of them sent a better wave.
        int pool = TheRuleset.Committed().HealthPoolSauce;

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

        // Level on both, and one of them earned twenty-seven thousand sauce.
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
        // OBSERVED: fold the run against `_rules.HealthPoolSauce * 2` in
        // Run.Folded, so that the run's own health comes off a pool the vector
        // does not carry. The first assertion goes red, 2237 against 737 --
        // which is what one number kept in two places looks like the moment the
        // two of them disagree.
        Run run = Played(TheRun.Fresh(waves: 5, fieldSize: 4), TheRun.Orders());

        RunOutcome rebuilt = RunOutcome.Of(
            TheRuleset.Committed().HealthPoolSauce,
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
    public void A_rounds_field_is_drawn_from_the_seed_and_the_round_and_not_from_where_the_last_draw_left_off()
    {
        // The claim that makes a run reproducible from its record, and the one
        // an ambient stream fails: round three's field cannot depend on what
        // rounds one and two did. Two runs on one seed play two different
        // openings and then the same third round, and the third round has to
        // come back identical.
        //
        // OBSERVED: mix the previous round's leak cost into the field draw's
        // position in Run.FieldFor -- the shape a draw taken from wherever the
        // match stream left off has, since where that is depends on how many
        // shots were fired. The first assertion goes red, 320 against 352: two
        // runs meet different fields in a round they played identically.
        UnitTypeTable types = TheMatch.Types();
        RoundOrders third = TheRun.Orders(types);
        RoundOrders other = TheRun.Orders(types, 2, 2);

        Run one = TheRun.Fresh(waves: 3, fieldSize: 4);
        one.Advance(third);
        one.Advance(third);
        RoundOutcome fromOne = one.Advance(third);

        Run two = TheRun.Fresh(waves: 3, fieldSize: 4);
        two.Advance(other);
        two.Advance(other);
        RoundOutcome fromTwo = two.Advance(third);

        Assert.Equal(fromOne.LeakCostDealt, fromTwo.LeakCostDealt);
        Assert.Equal(fromOne.LeakCostTaken, fromTwo.LeakCostTaken);

        // The openings really were different, so the agreement above is about
        // the derivation rather than about two runs that played the same game.
        Assert.NotEqual(one.Outcome.Rounds[0].LeakCostTaken, two.Outcome.Rounds[0].LeakCostTaken);

        // Same seed, same run. A different seed, a different run.
        Assert.Equal(
            Played(TheRun.Fresh(waves: 2, fieldSize: 4), third).Outcome.LeakCostTaken,
            Played(TheRun.Fresh(waves: 2, fieldSize: 4), third).Outcome.LeakCostTaken);

        Assert.NotEqual(
            Played(TheRun.Fresh(waves: 2, fieldSize: 4), third).Outcome.LeakCostTaken,
            Played(TheRun.Fresh(2, 4, true, TheRun.Seed + 1), third).Outcome.LeakCostTaken);
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
        RoundOrders orders = TheRun.Orders();
        Run alive = Played(TheRun.Fresh(waves: 3, fieldSize: 2), orders);
        Run dead = Played(TheRun.Unstoppable(deathEndsTheRun: false, fieldSize: 1), TheRun.Orders(TheRun.UnkillableTypes()));

        Assert.Equal(3, alive.Sent.Count);
        Assert.All(alive.Sent, sent => Assert.Same(orders, sent));
        Assert.Equal(10, dead.Sent.Count);

        // And what came out is what a field is made of.
        FieldPool next = FieldPool.Of(alive.Sent);
        Assert.Equal(3, next.Size);
        Assert.Same(orders, next.At(0));
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
            TheRuleset.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 500"));

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
        int pool = TheRuleset.Committed().HealthPoolSauce;

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
        // range check. The refusal fires one layer later, from RoundOutcome,
        // naming a round that took -294967296 in leak cost -- so this goes red
        // on the message rather than on the throw. That is why the refusal is
        // asserted by name: a wrapped total should say which arithmetic wrapped
        // rather than surface as a round that gave health back.
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

        SimulationException thrown =
            Assert.Throws<SimulationException>(() => run.Advance(TheRun.Orders(types)));

        Assert.Contains("does not fit", thrown.Message, StringComparison.Ordinal);
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

    /// <summary>A run driven to its end on one round's orders every round.</summary>
    private static Run Played(Run run, RoundOrders orders)
    {
        while (!run.IsOver)
        {
            run.Advance(orders);
        }

        return run;
    }

    /// <summary>An outcome built from pairs rather than from a simulation.</summary>
    private static RunOutcome Outcome(int healthPoolSauce, params (int Dealt, int Taken)[] rounds) =>
        Outcome(healthPoolSauce, 10, rounds);

    private static RunOutcome Outcome(int healthPoolSauce, int waves, params (int Dealt, int Taken)[] rounds) =>
        RunOutcome.Of(
            healthPoolSauce,
            rounds.Select(round => new RoundOutcome(round.Dealt, round.Taken)).ToArray(),
            waves,
            deathEndsTheRun: true);
}
