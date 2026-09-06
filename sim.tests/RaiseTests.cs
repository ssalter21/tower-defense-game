namespace Sim.Tests;

/// <summary>
/// A creep putting a creep on the corridor: when it fires, where the body
/// arrives, what the body is worth, what it costs the dice, and what the table
/// refuses.
/// </summary>
/// <remarks>
/// <para>
/// <b>The committed spawner is asserted against <c>content/units.txt</c> and
/// every other row here is a fixture.</b> The Necromancer raising the Minion is
/// content and is signed in <c>docs/roster.md</c>; the ids, labels and numbers
/// below mean nothing outside this file and exist because the committed roster
/// authors exactly one raise, on a cadence of a hundred and fifty ticks, which
/// is too slow to show a raise landing between two shots.
/// </para>
/// <para>
/// The corridor and the turret are ShotShapeTests' shape and for its reason:
/// five hexes, every cell covered, and a tower that winds up and recovers in
/// nothing so one attack fits in a handful of ticks.
/// </para>
/// </remarks>
public class RaiseTests
{
    private const string AShortCorridor = """
        S###E
        .....

        aaaaa
        aaaaa
        """;

    /// <summary>
    /// A caller and what it calls, plus the rows the refusals need.
    /// <c>caller</c> is the row the four spellings below re-author in place, so
    /// that two rosters can differ in the raise and in nothing else -- one id
    /// space, one set of numbers, one column moved. <c>frail</c> is a caller the
    /// cannon can kill, with a long death to raise through.
    /// </summary>
    private const string TheFixtures = """
        layout 5
        unit  1 caller moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none 2    5
        unit  2 imp    moving 400  10 0    0  0 0 0    0    none    0 4 12 none   armoured 0  0 1 none none none 0 none 0 0 none none 0
        unit  3 quiet  moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none none 0
        unit  6 frail  moving 500  10 0    0  0 0 0    0    none    0 30 12 none  armoured 0  0 1 none none none 0 none 0 0 none 2    4
        unit 11 cannon placed 0    0  4000 30 0 0 600  600  hitscan 0 0 40 pierce none     0  0 1 none none none 0 none 0 0 none none 0
        unit 12 pin    placed 0    0  4000 9  0 0 90   150  hitscan 0 0 40 pierce none     0  0 1 none none none 0 none 0 0 none none 0
        """;

    /// <summary>The caller as the fixture above spells it, and three re-authorings of it.</summary>
    private const string CallerRaisesEveryFive =
        "unit  1 caller moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none 2    5";

    private const string CallerRaisesNobody =
        "unit  1 caller moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none none 0";

    /// <summary>Two cadences a tick apart, both longer than any match here runs.</summary>
    private const string CallerRaisesEventually =
        "unit  1 caller moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none 2    900";

    private const string CallerRaisesATickLater =
        "unit  1 caller moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none 2    901";

    private const ulong Seed = 20260906UL;

    /// <summary>The turret that rolls an ordinary shot, and the one that rolls a lethal flat 600.</summary>
    private const int Pin = 12;

    private const int Cannon = 11;

    /// <summary>The Necromancer and the Minion, by the ids docs/roster.md signs.</summary>
    private const int Necromancer = 38;

    private const int Minion = 1;

    /// <summary>The cadence docs/roster.md signs, in ticks.</summary>
    private const int EveryHundredAndFifty = 150;

    [Fact]
    public void The_necromancer_raises_a_minion_beside_itself_on_the_signed_cadence()
    {
        // The signed mechanic, on the committed roster and against the committed
        // defense. One Necromancer walks into four archers and two mages and
        // hands the corridor a Minion every hundred and fifty ticks for as long
        // as it is alive and walking.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(Minion, types.ById(Necromancer).Raises?.Id);
        Assert.Equal(EveryHundredAndFifty, types.ById(Necromancer).RaisePeriodTicks);

        Match match = OneNecromancer(types);
        var log = new TheMatch.EventLog();

        while (!match.IsFinished)
        {
            log.EnteringTick(match.Tick + 1);
            match.Advance(1, log);
        }

        int[] raises = log.IndicesOf("raised");

        Assert.Equal(11, raises.Length);

        // The first one a whole period after the body arrived, and every one
        // after it a period apart. The Necromancer is released on tick zero, so
        // the cadence is measured from there.
        Assert.Equal(EveryHundredAndFifty, log.Ticks[raises[0]]);

        for (int index = 1; index < raises.Length; index++)
        {
            Assert.Equal(
                EveryHundredAndFifty,
                log.Ticks[raises[index]] - log.Ticks[raises[index - 1]]);
        }

        // One raiser, and every body it named is a Minion the wave never sent.
        Assert.Single(log.Subjects.Where((_, index) => log.Kinds[index] == "raised").Distinct());
    }

    [Fact]
    public void The_cadence_is_counted_from_the_tick_the_body_arrived()
    {
        // Not from the tick the wave opened. A body released a spawn interval
        // into the match waits the same whole period the first of its column
        // did, which is what makes "every 150 ticks" a fact about the body.
        //
        // OBSERVED: run Match.Raise after Release rather than before it. The
        // body released on tick zero still raises on tick five, and this one
        // raises on 49 instead of 50 -- one tick early, because the raise phase
        // counted it down on the tick it arrived.
        Assert.Equal(5, FirstRaiseTick("order 0 1 1 0"));
        Assert.Equal(50, FirstRaiseTick("order 45 1 1 0"));
    }

    [Fact]
    public void A_raised_body_arrives_beside_its_raiser_on_a_full_pool_and_in_its_own_lane()
    {
        // Where it is: the raiser's own distance along the route, which is what
        // "beside itself" is, and the next lane offset in the cycle so the two
        // are not the same point. What it is: a body that spawned, on the whole
        // of its own pool.
        UnitTypeTable types = TheMatch.Types();
        Match match = OneNecromancer(types);

        CreepSnapshot raiser = default;
        var log = new TheMatch.EventLog();

        while (!match.IsFinished && log.CountOf("raised") == 0)
        {
            raiser = match.PullSnapshot().Creeps.Single();
            log.EnteringTick(match.Tick + 1);
            match.Advance(1, log);
        }

        IReadOnlyList<CreepSnapshot> creeps = match.PullSnapshot().Creeps;
        CreepSnapshot risen = creeps.Single(creep => creep.TypeId == Minion);

        Assert.Equal(2, creeps.Count);
        Assert.Equal(types.ById(Minion).MaxHp, risen.Hp);
        Assert.Equal(CreepState.Walking, risen.State);

        // It has not moved yet, so it is exactly where the raiser was standing
        // at the end of the tick it was raised on -- which is one step past
        // where the raiser was in the picture before it.
        Assert.Equal(creeps.Single(creep => creep.TypeId == Necromancer).DistanceAlongPath, risen.DistanceAlongPath);
        Assert.True(risen.DistanceAlongPath > raiser.DistanceAlongPath);
        Assert.NotEqual(raiser.LateralOffset, risen.LateralOffset);

        // And it is a new entity rather than the raiser renamed.
        Assert.NotEqual(raiser.Id, risen.Id);
        Assert.Equal(risen.Id, log.Amounts[log.IndicesOf("raised")[0]]);
        Assert.Equal(raiser.Id, log.Subjects[log.IndicesOf("raised")[0]]);
    }

    [Fact]
    public void A_raised_body_enters_behind_everything_standing_and_loses_every_tie()
    {
        // Where it goes in the array, which is the decision ADR-0060 records.
        // Ids are handed out in arrival order and the array is kept in ascending
        // id, so a raised body is behind everything already on the corridor --
        // and the target-selection tiebreak picks the lower id, so a raised body
        // level with its raiser is never the one acquired.
        Match match = Built(TheFixtures, "order 0 1 1 0");
        var log = new TheMatch.EventLog();

        while (!match.IsFinished && log.CountOf("raised") == 0)
        {
            log.EnteringTick(match.Tick + 1);
            match.Advance(1, log);
        }

        IReadOnlyList<CreepSnapshot> creeps = match.PullSnapshot().Creeps;

        Assert.Equal(2, creeps.Count);
        Assert.True(creeps[1].Id > creeps[0].Id);
        Assert.Equal(1, creeps[0].TypeId);
        Assert.Equal(2, creeps[1].TypeId);

        // The two are at the same distance on the tick of the raise, which is
        // the tie, and the turret shoots the raiser.
        Assert.Equal(creeps[0].DistanceAlongPath, creeps[1].DistanceAlongPath);

        TheMatch.EventLog after = TheMatch.Watched(match, 12);

        Assert.Equal(creeps[0].Id, after.Subjects[after.IndicesOf("damaged")[0]]);
    }

    [Fact]
    public void The_dice_stream_is_untouched_by_a_raise()
    {
        // A raise puts a body on the board and draws nothing to do it: where it
        // goes is the raiser's distance and the next offset in the cycle, both
        // determined. So the landings of a match full of raises are still the
        // first n numbers of the seeded stream in order, and the stream's
        // position is still a running count of the shots fired.
        //
        // OBSERVED: take one number off the stream in Match.Raise -- a lateral
        // offset drawn instead of cycled would be the realistic way in. The
        // rolls walk one place along on the shot after the first raise and never
        // recover, and every stored record made under it replays to a different
        // match.
        Match match = Built(TheFixtures, "order 0 1 1 0");
        TheMatch.EventLog log = TheMatch.Watched(match, 60);

        Assert.True(log.CountOf("raised") >= 4, "no raise happened, so this proves nothing");

        UnitType turret = UnitTypeTable.Parse("raise fixtures", TheFixtures).ById(Pin);
        var dice = new Pcg32(Seed);
        int[] landed = log.IndicesOf("damaged").Select(index => log.Amounts[index]).ToArray();

        Assert.NotEmpty(landed);

        for (int index = 0; index < landed.Length; index++)
        {
            int roll = dice.NextInRange(turret.DamageMin, turret.DamageMax + 1);

            Assert.Equal(
                DamageModel.Dealt(
                    TheRuleset.Committed(),
                    roll,
                    0,
                    turret.AttackType,
                    ArmourType.Armoured,
                    0),
                landed[index]);
        }

        // And the count of draws is the count of shots, which is what makes the
        // reconstruction above a statement about the stream rather than about
        // these particular numbers.
        Assert.Equal(log.CountOf("fired"), landed.Length);
    }

    [Fact]
    public void The_rolling_hash_covers_the_clock_a_body_raises_on()
    {
        // Two matches whose every body agrees and whose raise clocks do not.
        // The two rosters are the same row -- same id, same pool, same speed --
        // authored with cadences a tick apart, and both are longer than these
        // sixty ticks, so neither ever raises. Nothing is on the corridor that
        // is not on the corridor in the other run, nothing carries a different
        // number, and the ONLY thing the fold could tell them apart by is how
        // long the body has left before it would have raised.
        //
        // OBSERVED: take `creep.RaiseIn` back out of Match.Fold's per-creep
        // loop. This goes red with the two hashes equal, and a run that is one
        // tick from putting a body on the board hashes the same as one that is
        // two.
        Assert.NotEqual(
            HashOf(Reauthored(CallerRaisesEventually)),
            HashOf(Reauthored(CallerRaisesATickLater)));

        // And the bodies themselves, which is the ordinary case and the same
        // controlled pair: one roster raises, the other is that roster with the
        // column struck out, and the run is fifteen bodies apart by the end.
        Assert.NotEqual(HashOf(TheFixtures), HashOf(Reauthored(CallerRaisesNobody)));
    }

    [Fact]
    public void A_leak_of_a_raised_body_is_charged_at_the_price_of_the_row_it_is()
    {
        // The pricing decision ADR-0060 records. A raised body takes as much
        // health off a defense as a bought one, so its leak is charged -- and at
        // the price of the row that was raised, because that is what leaked. It
        // is counted apart from the order's own leaks, because the order's type
        // is a different price.
        UnitTypeTable types = TheMatch.Types();
        Match match = OneNecromancer(types);

        match.Resolve();

        Assert.Equal(1, match.LeakedByOrder.Single());
        Assert.Equal(11, match.LeakedRaisedByOrder.Single());
        Assert.Equal(12, match.Leaked);

        // Twelve gold of bodies for one twenty-one gold body: the eleven Minions
        // are 110 gold of health a defense has to spend that nobody paid to
        // send. That gap is the finding and is not closed here.
        CostTable costs = CostTable.From(TheRuleset.Committed(), types);

        Assert.Equal(
            costs.PriceOf(Purchase.Unit(Necromancer)) + (11 * costs.PriceOf(Purchase.Unit(Minion))),
            costs.PriceOf(Purchase.Unit(Necromancer), match.LeakedByOrder.Single())
            + costs.PriceOf(Purchase.Unit(Minion), match.LeakedRaisedByOrder.Single()));
    }

    [Fact]
    public void A_body_that_stops_walking_stops_raising()
    {
        // The raise phase runs after the dead have been cleared away and reads
        // only walking bodies, so killing a spawner stops the raises on the tick
        // it dies rather than at the end of its corpse.
        //
        // OBSERVED: drop the phase check in Match.Raise. A dying body goes on
        // raising for the whole of its death, which makes a spawner's total a
        // function of how long its row spends falling over.
        TheMatch.EventLog log = TheMatch.Watched(Built(TheFixtures, "order 0 6 1 0", Cannon), 70);
        int[] raises = log.IndicesOf("raised");

        Assert.True(raises.Length > 0, "it never raised at all, so this proves nothing");

        // The one body that raised, and the tick it died on. The cannon goes on
        // to kill what it raised, so the raiser is named rather than the first
        // death taken.
        int raiser = log.Subjects[raises[0]];
        int death = log.Ticks[
            log.IndicesOf("died").Single(index => log.Subjects[index] == raiser)];

        // Its corpse stands for thirty ticks, which is long enough for seven
        // more raises to come due on this row's cadence.
        Assert.True(log.Ticks[^1] > death + 20, "the log stops before the corpse does");

        foreach (int index in raises)
        {
            Assert.True(
                log.Ticks[index] <= death,
                "a body was raised on tick " + log.Ticks[index] + ", after its raiser died on " + death);
        }
    }

    [Fact]
    public void Exactly_one_committed_row_raises_and_it_is_the_necromancer()
    {
        // The roster's own claim, measured. A second spawner is a design
        // decision docs/roster.md would have to sign, so a row that gained one
        // quietly goes red here.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(
            new[] { "necromancer raises minion every 150" },
            types.Types
                .Where(row => row.Raises is not null)
                .Select(row => row.Label + " raises " + row.Raises!.Label + " every " + row.RaisePeriodTicks)
                .ToArray());
    }

    [Fact]
    public void A_row_that_raises_something_no_row_authored_refuses_by_name()
    {
        Assert.Contains(
            "raises type 99",
            Refused(TheFixtures.Replace("none 2    5", "none 99   5")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_raises_itself_refuses_by_name()
    {
        // A body putting a copy of itself on the board every so often is a
        // population that doubles on a clock, and no arithmetic bounds it.
        Assert.Contains(
            "raises itself",
            Refused(TheFixtures.Replace("none 2    5", "none 1    5")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_raises_a_tower_refuses_by_name()
    {
        // Named, because the other half of this rule is refused with the same
        // words from the other side: the row is quoted back, so the refusal says
        // which of the two rows is the one that stands.
        Assert.Contains(
            "raises pin (#12), which stands where it was put",
            Refused(TheFixtures.Replace("none 2    5", "none 12   5")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_stands_where_it_was_put_may_not_raise_anything()
    {
        // The refusal from the other side. A raise puts a body on the corridor
        // where the body that raised it is standing, and nothing that stands is
        // on the corridor at all.
        Assert.Contains(
            "names a row it raises",
            Refused(TheFixtures.Replace("0 40 pierce none     0  0 1 none none none 0 none 0 0 none none 0", "0 40 pierce none     0  0 1 none none none 0 none 0 0 none 2    5")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_raises_a_body_nothing_can_damage_refuses_by_name()
    {
        // A body with no pool walks to the exit whatever stands in front of it,
        // so a spawner making them is a leak nothing can answer.
        Assert.Contains(
            "has no health pool",
            Refused(
                "layout 5\n"
                + "unit 1 caller moving 9000 10 0 0 0 0 0 0 none 0 4 30 none armoured 0 0 1 none none none 0 none 0 0 none 2 5\n"
                + "unit 2 ghost  moving 0    10 0 0 0 0 0 0 none 0 4 0  none none     0 0 1 none none none 0 none 0 0 none none 0\n").Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_raised_row_that_raises_in_its_turn_refuses_by_name()
    {
        // What a raise puts on the board raises nothing. A second generation is
        // a population no arithmetic bounds, so a match holding one could not be
        // proved to end -- which is the whole reason the termination bound below
        // is arithmetic.
        Assert.Contains(
            "raises in its turn",
            Refused(TheFixtures.Replace("unit  2 imp    moving 400  10 0    0  0 0 0    0    none    0 4 12 none   armoured 0  0 1 none none none 0 none 0 0 none none 0", "unit  2 imp    moving 400  10 0    0  0 0 0    0    none    0 4 12 none   armoured 0  0 1 none none none 0 none 0 0 none 3    5")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_becomes_a_row_that_raises_refuses_by_name()
    {
        // The same second generation by a longer route, and the clause that
        // makes one wave order name one raised row: a lineage carrying a raise
        // on both halves would put two different rows on the corridor out of one
        // order, and a leak of either could not be priced from the order it
        // descends from.
        Assert.Contains(
            "which raises",
            Refused(TheFixtures.Replace("unit  3 quiet  moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 none none 0", "unit  3 quiet  moving 9000 10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0 1 none none none 0 none 0 0 1    none 0")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_raise_with_no_cadence_and_a_cadence_with_no_raise_are_both_refused()
    {
        // Both directions, because both are a column read by nothing that would
        // still move the content hash -- which is the rule every other unread
        // column in this file is refused by.
        Assert.Contains(
            "every nothing ticks",
            Refused(TheFixtures.Replace("none 2    5", "none 2    0")).Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "raises nothing and carries a raise period",
            Refused(TheFixtures.Replace("none 2    5", "none none 5")).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_match_refuses_a_raised_row_that_would_never_reach_the_exit()
    {
        // The termination invariant, extended. How many a spawner raises is
        // bounded by the board and deliberately not by arithmetic; when the last
        // of them gets to the exit has to be arithmetic, or a match holding one
        // could not be proved to end.
        //
        // OBSERVED: drop RequireWhatItRaisesArrives. This goes green having
        // caught nothing, and the match instead runs to the tick ceiling and
        // throws thousands of ticks after the mistake.
        // The raiser itself arrives inside the ceiling and what it raises does
        // not, which is the case only the second bound catches: the wave is
        // released late enough that one crossing fits and two do not.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Built(TheFixtures, "order 113000 1 1 0"));

        Assert.Contains("raises imp (#2)", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("at or past the ceiling", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_committed_wave_raises_nothing_and_the_committed_match_is_untouched()
    {
        // What the mechanic costs the run everything else in this repository is
        // measured against: nothing. The committed wave sends Minions and
        // Skeleton Scouts and neither raises, so the leak count, the length and
        // every landmark are where they were -- and the rolling hash moves
        // anyway, which is what the SimulationVersion bump is for.
        UnitTypeTable types = TheMatch.Types();

        Assert.All(
            TheMatch.Wave(types).Orders,
            order => Assert.Null(order.Type.Raises));

        MatchResult result = TheMatch.Fresh().Resolve();

        Assert.Equal(TheMatch.LeakedInTheCommittedRun, result.Leaked);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
    }

    /// <summary>One Necromancer, alone, against the committed defense.</summary>
    private static Match OneNecromancer(UnitTypeTable types) =>
        new(
            TheMatch.Map(),
            TheRuleset.Committed(),
            TheMatch.Layout(types),
            WaveScript.Parse("one necromancer", "order 0 " + Necromancer + " 1 0", types),
            TheMatch.Seed);

    /// <summary>
    /// The corridor, one turret on it, and a wave, out of one roster. The
    /// turret defaults to the one that rolls an ordinary shot, because most of
    /// these want a body hit rather than a body hit hard.
    /// </summary>
    private static Match Built(string units, string wave, int towerType = Pin)
    {
        UnitTypeTable types = UnitTypeTable.Parse("raise fixtures", units);

        return new Match(
            HexMap.Parse("raise map", AShortCorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("raise defense", "tower " + towerType + " 2 1", types),
            WaveScript.Parse("raise wave", wave, types),
            Seed);
    }

    /// <summary>The tick the first raise of that wave landed on.</summary>
    private static int FirstRaiseTick(string wave)
    {
        TheMatch.EventLog log = TheMatch.Watched(Built(TheFixtures, wave), 60);
        int[] raises = log.IndicesOf("raised");

        Assert.NotEmpty(raises);

        return log.Ticks[raises[0]];
    }

    /// <summary>The fixtures with the caller row spelled another way.</summary>
    private static string Reauthored(string caller) =>
        TheFixtures.Replace(CallerRaisesEveryFive, caller, StringComparison.Ordinal);

    /// <summary>What the rolling hash is after sixty ticks of one caller walking.</summary>
    private static Hash64 HashOf(string units)
    {
        Match match = Built(units, "order 0 1 1 0");

        match.Advance(60);

        return match.StateHash;
    }

    /// <summary>The refusal a roster produces, or a failure saying it produced none.</summary>
    private static ContentException Refused(string units) =>
        Assert.Throws<ContentException>(() => UnitTypeTable.Parse("raise fixtures", units));
}
