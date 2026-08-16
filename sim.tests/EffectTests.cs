namespace Sim.Tests;

/// <summary>
/// Timed effects: what a slow, a rally, a curse and a granted pool do to a
/// unit, how two of them resolve against each other, when they stop, and the
/// floor that stops one of them ending a match.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every row here is a fixture and none of them is content.</b> Naming and
/// statting a tower is a design decision and this ticket took none, so what is
/// proved below is that the schema and the tick loop carry each shape -- the
/// ids, labels and numbers are stand-ins and mean nothing outside this file.
/// The committed roster authors no bubble at all, so none of this is reachable
/// through <c>content/units.txt</c> and all of it would otherwise be untested.
/// </para>
/// <para>
/// <b>Half of it is about <see cref="Effects"/> alone and half about a
/// match.</b> The resolution rules -- strongest-wins, the refreshed timer, the
/// floor -- are properties of the model and are checked directly, because a
/// match can only ever show a handful of the orderings and none of the
/// adversarial magnitudes. The wiring is checked through a match, because a
/// model that is right and unread is the failure the whole
/// <c>#216</c>-to-<c>#217</c> boundary was drawn to avoid.
/// </para>
/// </remarks>
public class EffectTests
{
    /// <summary>Ten hexes of corridor with a tower cell under every one of them.</summary>
    private const string ACorridor = """
        S########E
        ..........

        aaaaaaaaaa
        aaaaaaaaaa
        """;

    /// <summary>
    /// Walkers that cannot die and towers whose whole job is the bubble they
    /// carry.
    /// </summary>
    /// <remarks>
    /// The health pools are enormous and the damage rolls are flat, so nothing
    /// below ever turns into a test about who died first: what is being
    /// measured is distance walked, shots fired and damage dealt, and each of
    /// those wants the other two held still. The towers reach four hexes, which
    /// is every cell of the corridor from where they stand.
    /// </remarks>
    private const string TheFixtures = """
        layout 3
        unit  1 walker   moving 1000000 28 0 0 0 0 0 0 none 0 4 30 none armoured 50 0 1 none none none 0 none 0 0
        unit  2 second   moving 1000000 28 0 0 0 0 0 0 none 0 4 30 none armoured 50 0 1 none none none 0 none 0 0
        unit  3 warden   moving 1000000 28 0 0 0 0 0 0 none 0 4 30 none armoured 50 0 1 2000 self friend 30 shield 50 90
        unit 10 frost    placed 0 0 4000 100000 0 0 1 1 hitscan 0 0 40 pierce none 0 0 1 0 target enemy 0 speed -35 60
        unit 11 endless  placed 0 0 4000 1 0 0 1 1 hitscan 0 0 40 pierce none 0 0 1 0 target enemy 0 speed -100 1000000
        unit 12 plain    placed 0 0 4000 30 0 0 100 100 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0
        unit 13 banner   placed 0 0 4000 30 0 0 100 100 hitscan 0 0 40 pierce none 0 0 1 3000 self friend 45 cooldown -50 90
        unit 14 curse    placed 0 0 4000 30 0 0 100 100 hitscan 0 0 40 pierce none 0 0 1 0 target enemy 0 armour -100 600
        unit 15 inward   placed 0 0 4000 30 0 0 100 100 hitscan 0 0 40 pierce none 0 0 1 3000 self friend 0 damage 0 0
        unit 16 outward  placed 0 0 4000 30 0 0 100 100 hitscan 0 0 40 pierce none 0 0 1 3000 self enemy 0 damage 0 0
        """;

    /// <summary>The walker the fixtures are authored around, in milli-hexes a tick.</summary>
    private const int WalkerSpeed = 28;

    /// <summary>What the frost tower takes off it, in percent.</summary>
    private const int FrostMagnitude = -35;

    /// <summary>How long the frost tower's slow lasts, in ticks.</summary>
    private const int FrostDuration = 60;

    private const ulong Seed = 20260816UL;

    [Fact]
    public void An_effect_applied_twice_refreshes_the_timer_and_does_not_stack_the_magnitude()
    {
        // The rule a Cryomancer standing beside a second Cryomancer is decided
        // by, and the rule docs/roster.md says is the intended answer. Two
        // landings of the same slow are one slow that lasts longer, and never a
        // slow of twice the size -- which is the failure a player can build any
        // number of copies of.
        //
        // OBSERVED: make Effects.Apply add the landed magnitude to the one in
        // the slot. The magnitude assertion goes red at -80 against -40 and the
        // walking-speed one with it; every expiry assertion below stays green,
        // which is exactly why the magnitude is asserted separately from the
        // timer.
        var effects = default(Effects);

        effects.Land(Slow(-40, 60), tick: 0, maxHp: 0);
        Assert.Equal(-40, effects.SpeedMagnitude);

        effects.Land(Slow(-40, 60), tick: 30, maxHp: 0);
        Assert.Equal(-40, effects.SpeedMagnitude);

        // The first landing alone would have run out at tick 60. The second
        // carried it to 90, and it is still on at 90 and off at 91.
        Assert.False(effects.Expire(60));
        Assert.Equal(-40, effects.SpeedMagnitude);

        Assert.False(effects.Expire(90));
        Assert.Equal(-40, effects.SpeedMagnitude);

        Assert.True(effects.Expire(91));
        Assert.Equal(0, effects.SpeedMagnitude);
    }

    [Theory]
    [InlineData(-40, -70, -70)]
    [InlineData(-70, -40, -70)]
    [InlineData(25, -25, -25)]
    [InlineData(-25, 25, -25)]
    [InlineData(80, -30, 80)]
    [InlineData(-30, 80, 80)]
    public void Two_sources_of_one_stat_resolve_strongest_wins_whichever_order_they_land_in(
        int first,
        int second,
        int expected)
    {
        // Strongest-wins, and the pairs are given both ways round on purpose:
        // what makes the rule deterministic is not that it picks a winner but
        // that it picks the same winner however the two arrived. The third pair
        // is the one an ordering bug hides in -- a curse and a blessing of the
        // same size, where "the last one wins" would answer differently for two
        // runs that differed only in which tower was built first.
        //
        // OBSERVED: make Stronger compare the magnitudes directly instead of
        // their distance from zero. The first four rows stay green and the last
        // two go red at -30 against 80, because a haste would then lose to any
        // slow at all.
        var landed = default(Effects);
        landed.Land(Slow(first, 60), tick: 0, maxHp: 0);
        landed.Land(Slow(second, 60), tick: 0, maxHp: 0);

        Assert.Equal(expected, landed.SpeedMagnitude);
    }

    [Fact]
    public void Landing_any_two_magnitudes_in_either_order_reaches_the_same_state()
    {
        // The whole of what "ordering is asserted canonical, not restored and
        // not incidental" buys here, asserted over a grid rather than over the
        // handful of pairs a match happens to produce. `Stronger` is a strict
        // total order on the integers and the surviving timer is a maximum, so
        // landing two effects is commutative -- which is the property replays
        // need, because two runs can differ in which of two towers fired first
        // without differing in anything a player could see.
        //
        // OBSERVED: drop the sign tiebreak from Stronger, so two magnitudes
        // equally far from zero compare equal. The pairs {-n, n} go red on the
        // first assertion, because the slot then keeps whichever landed first.
        int[] magnitudes = { -1000, -100, -75, -40, -25, -1, 1, 25, 40, 75, 100, 1000 };

        foreach (int first in magnitudes)
        {
            foreach (int second in magnitudes)
            {
                var forwards = default(Effects);
                forwards.Land(Slow(first, 60), tick: 0, maxHp: 0);
                forwards.Land(Slow(second, 90), tick: 0, maxHp: 0);

                var backwards = default(Effects);
                backwards.Land(Slow(second, 90), tick: 0, maxHp: 0);
                backwards.Land(Slow(first, 60), tick: 0, maxHp: 0);

                Assert.Equal(forwards.SpeedMagnitude, backwards.SpeedMagnitude);

                // And it is one of the two rather than some third number, which
                // is what separates strongest-wins from an average or a sum.
                Assert.Contains(forwards.SpeedMagnitude, new[] { first, second });

                // The timers agree too, which is what the maximum is for: a
                // slot that took "the last one wins" would pass the line above
                // and expire on different ticks for the two orders.
                foreach (int tick in new[] { 60, 90, 91 })
                {
                    Effects one = forwards;
                    Effects other = backwards;
                    one.Expire(tick);
                    other.Expire(tick);

                    Assert.Equal(one.SpeedMagnitude, other.SpeedMagnitude);
                }
            }
        }
    }

    [Fact]
    public void A_granted_pool_is_spent_raw_restored_by_the_next_pulse_and_never_stacked()
    {
        // The one payload that grants rather than displaces. Its magnitude is a
        // share of the health it stands in front of -- a pool has no rate of
        // its own for a percentage to be a percentage of -- so half of a
        // thousand-point body is five hundred points of pool.
        var effects = default(Effects);
        Bubble granting = Bubble.Of(
            2000,
            BubbleOrigin.Self,
            BubbleAffects.Friend,
            30,
            BubblePayload.Shield,
            50,
            90);

        effects.Land(granting, tick: 0, maxHp: 1000);
        Assert.Equal(500, effects.GrantedShield);

        // Spent raw, with overkill carrying through: a granted point is worth
        // exactly one point against every attack type there is, which is what
        // makes it a different lever from health rather than a bigger pool.
        Assert.Equal(0, effects.Spend(200));
        Assert.Equal(300, effects.GrantedShield);
        Assert.Equal(100, effects.Spend(400));
        Assert.Equal(0, effects.GrantedShield);

        // The next pulse restores it to what the effect grants and never past
        // it. A pulse that added to what was left would be a stack with extra
        // steps, and an aura is exactly the shape a player can build many of.
        effects.Land(granting, tick: 30, maxHp: 1000);
        Assert.Equal(500, effects.GrantedShield);

        effects.Land(granting, tick: 30, maxHp: 1000);
        Assert.Equal(500, effects.GrantedShield);

        // And what is left of it goes when the duration does, measured from the
        // pulse that last restored it.
        Assert.False(effects.Expire(120));
        Assert.Equal(500, effects.GrantedShield);

        Assert.False(effects.Expire(121));
        Assert.Equal(0, effects.GrantedShield);
    }

    [Fact]
    public void A_weaker_effect_does_not_extend_a_stronger_one_and_does_not_survive_it()
    {
        // The other half of one slot per stat. A weak long slow landing under a
        // strong short one is discarded outright: it does not stretch the
        // strong one's timer, and when the strong one runs out the creep is
        // back at its authored speed rather than down at the weak one. A queue
        // would be a stack wearing a different hat -- the total time a stat
        // spends displaced would grow with the number of sources.
        var effects = default(Effects);

        effects.Land(Slow(-70, 20), tick: 0, maxHp: 0);
        effects.Land(Slow(-20, 500), tick: 0, maxHp: 0);

        Assert.Equal(-70, effects.SpeedMagnitude);
        Assert.False(effects.Expire(20));
        Assert.True(effects.Expire(21));
        Assert.Equal(0, effects.SpeedMagnitude);
    }

    [Fact]
    public void An_effect_expires_exactly_on_its_duration_and_the_creep_returns_to_its_authored_speed()
    {
        // The claim in the tick loop rather than in the model: a slow landing
        // on the tick a tower fires is on for exactly its duration of ticks
        // after that one, and the tick after that the creep walks at the number
        // on its row again. Counted in raw Q32.32, so a step that came back
        // nearly right would still be wrong here.
        //
        // The frost tower's cooldown is a hundred thousand ticks, so it fires
        // once and everything below is one slow rather than a sequence of them.
        Match match = Corridor("tower 10 2 1", "order 0 1 1 0");

        long authored = Fix64.FromRatio(WalkerSpeed, 1000).Raw;
        long slowed = Fix64.FromRatio(Effects.ModifiedSpeed(WalkerSpeed, FrostMagnitude), 1000).Raw;

        Assert.NotEqual(authored, slowed);

        // Tick zero: the creep moves before the tower acts, so the first step
        // is the authored one and the slow lands after it.
        match.Advance(1);
        Assert.Equal(authored, Walked(match));

        // Then exactly sixty slowed steps.
        match.Advance(FrostDuration);
        Assert.Equal(authored + (FrostDuration * slowed), Walked(match));

        // And the sixty-first tick is authored again, which is what "expires
        // exactly on its duration" means.
        match.Advance(1);
        Assert.Equal(authored + (FrostDuration * slowed) + authored, Walked(match));
    }

    [Fact]
    public void The_speed_a_modifier_leaves_is_truncated_exactly_once()
    {
        // Two truncations compute a different function from the same algebra
        // written as one, which is the hazard DamageModel's remarks name for a
        // stat pipeline and the one a speed modifier shares. The percentage is
        // applied to the authored milli-hexes as one integer expression and the
        // result is converted into Q32.32 once.
        //
        // HAND COMPUTED. Twenty-eight thousandths of a hex at sixty-five
        // percent is 18.2 thousandths, which truncates to 18. Eighteen
        // thousandths in Q32.32 is 18 * 2^32 / 1000 = 77,309,411.328, which
        // truncates to 77309411.
        Assert.Equal(18, Effects.ModifiedSpeed(WalkerSpeed, FrostMagnitude));
        Assert.Equal(77309411L, Fix64.FromRatio(18, 1000).Raw);

        // The other spelling, for the same numbers: convert first and multiply
        // the truncated step by a truncated percentage. It is a different
        // number, which is the whole point -- both are defensible and only one
        // of them is what every stored record replays through.
        Fix64 twice = Fix64.FromRatio(WalkerSpeed, 1000) * Fix64.FromRatio(100 + FrostMagnitude, 100);

        Assert.NotEqual(77309411L, twice.Raw);

        // And that the tick loop walks at the first of them rather than the
        // second. The creep's distance is read off a snapshot, so this is the
        // number a view would draw and not a field a test reached into.
        Match match = Corridor("tower 10 2 1", "order 0 1 1 0");

        match.Advance(1);
        long afterOne = Walked(match);

        match.Advance(1);

        Assert.Equal(77309411L, Walked(match) - afterOne);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(28)]
    [InlineData(56)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void No_magnitude_authored_or_adversarial_drops_a_creep_below_a_tenth_of_its_speed(int authored)
    {
        // The floor binds every effect at once and is applied after the
        // modifier, so what has to hold is a property of the pair rather than
        // of any one number: whatever is asked for, what comes out is at least
        // a tenth of the authored speed and is never zero. The magnitudes below
        // run past anything a designer would write, because a safety rail is
        // only a rail if it holds at the edges.
        //
        // A tenth of nine thousandths of a hex is zero in integer arithmetic,
        // which is exactly the value the floor exists to make unreachable --
        // hence the second assertion and the nine in the list above.
        int floor = Effects.FloorSpeed(authored);

        Assert.True(floor >= 1, $"A speed of {authored} floors at {floor}, which is a creep that cannot move.");
        Assert.True(floor <= authored);

        // And it is the share the rule names rather than a number this file
        // agreed with once, so moving the constant moves what is asserted.
        Assert.Equal(
            Math.Max(1, (int)(((long)authored * Effects.SpeedFloorPercent) / 100)),
            floor);

        foreach (int magnitude in new[]
        {
            0, -1, -50, -99, -100, -101, -1000, -1000000, int.MinValue, 1, 100, 1000000, int.MaxValue,
        })
        {
            int walked = Effects.ModifiedSpeed(authored, magnitude);

            Assert.True(
                walked >= floor,
                $"A speed of {authored} at {magnitude}% came out at {walked}, under the floor of {floor}.");
        }
    }

    [Fact]
    public void A_permanent_maximum_slow_does_not_stop_the_match_ending()
    {
        // The reason the floor exists, run rather than argued. The tower fires
        // every tick and every shot takes a hundred percent off the creep's
        // speed for a million ticks, so there is no moment in this match when
        // anything is unslowed -- and the creeps still reach the exit, because
        // a hundred percent off lands on the floor rather than on nothing.
        //
        // OBSERVED: delete the floor from Effects.ModifiedSpeed. The creeps
        // stop moving, the match runs to the tick ceiling and Advance throws
        // "The match has run 120000 ticks without ending" -- which is the
        // failure this whole rail exists to make unreachable.
        Match match = Corridor("tower 11 2 1", "order 0 1 2 0");
        MatchResult result = match.Resolve();

        Assert.True(match.IsFinished);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Leaked);

        // At the floor -- two thousandths of a hex a tick against an authored
        // twenty-eight -- nine hexes of corridor take four and a half thousand
        // ticks. That is what a maximum slow costs and it is a long way inside
        // the ceiling, which is the margin the invariant at construction is
        // about.
        Assert.True(
            result.FinalTick > 4000 && result.FinalTick < 6000,
            $"A permanently floored wave finished on tick {result.FinalTick}.");
    }

    [Fact]
    public void A_wave_that_could_not_cross_at_the_floor_is_refused_when_the_match_is_built()
    {
        // The invariant stated as arithmetic rather than as care. The floor is
        // what makes a hung match unreachable, and this is where that is proved
        // for a particular map and a particular wave: a release so late that
        // even the last unit walking at its floor speed arrives past the
        // ceiling is refused at construction, where a wave that ran out of
        // ticks used to be a throw thousands of ticks later with nothing to
        // point at.
        UnitTypeTable types = UnitTypeTable.Parse("effects", TheFixtures);

        SimulationException thrown = Assert.Throws<SimulationException>(() => new Match(
            HexMap.Parse("effects map", ACorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("effects defense", "tower 12 2 1", types),
            WaveScript.Parse("effects wave", "order 119000 1 1 0", types),
            Seed));

        Assert.Contains("floor under every effect at once", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Overtaking_is_reported_correctly_for_a_creep_something_landed_on()
    {
        // Where a creep was a tick ago is arithmetic rather than memory: its
        // current distance less the step it took this tick. A modifier makes
        // that step a fact about the creep rather than about its wave order, so
        // a pass involving a slowed creep is only reported at all if the
        // subtraction uses the creep's own step.
        //
        // Two creeps released on the same tick, at the same speed. The tower
        // fires once, on tick zero, at the creep nearest the exit -- which is
        // the lower id, because they are level -- so the first is slowed and
        // the second walks past it on tick one.
        //
        // OBSERVED: point Match.StepThisTick back at the wave order's step. The
        // pass is not reported at all: the slowed creep's previous position
        // comes out too far back, the "and was not a tick ago" clause never
        // holds, and after tick one the two are never level again. Zero events
        // against one, which is a pass that happened and was never said.
        Match match = Corridor("tower 10 2 1", "order 0 1 1 0\norder 0 2 1 0");
        var log = new TheMatch.EventLog();

        match.Advance(200, log);

        int[] passes = Enumerable.Range(0, log.Count)
            .Where(index => log.Kinds[index] == "overtook")
            .ToArray();

        Assert.Single(passes);

        // Ids count from one and the towers take the front of the space, so the
        // one tower here is 1 and the two creeps are 2 and 3. The later-spawned
        // of a pair is always the passer.
        Assert.Equal(3, log.Subjects[passes[0]]);
        Assert.Equal(2, log.Amounts[passes[0]]);
    }

    [Fact]
    public void An_aura_pulses_on_its_own_clock_and_a_rallied_tower_fires_more_often()
    {
        // A bubble with a period is an aura and that is the whole of the
        // difference: the banner is the plain tower with six columns filled in,
        // and what it does with them is halve its own cooldown every forty-five
        // ticks for ninety. Both towers are otherwise the same row.
        //
        // The first shot is identical either way -- the aura lands at the end
        // of tick zero, after the tower has already committed to its wait --
        // which is what makes the difference below entirely about the shots
        // after it.
        //
        // Neither count is three hundred over a cooldown: both towers reach
        // four hexes and the corridor is ten, so each stops shooting when the
        // creep walks out of its coverage. That is the same cut-off for both
        // and it is the rallied tower that gets more shots in before it.
        int plain = ShotsIn(12, ticks: 300);
        int rallied = ShotsIn(13, ticks: 300);

        Assert.Equal(7, plain);
        Assert.Equal(13, rallied);
    }

    [Fact]
    public void A_curse_on_a_creeps_armour_lands_after_the_shot_that_carried_it_and_before_the_next()
    {
        // A bubble carrying a modifier does not carry the damage: the shot
        // lands where it was aimed exactly as an unadorned shot does, and the
        // bubble is a second thing that happens beside it. So the first landing
        // meets the armour on the row and every landing after it meets the
        // cursed number, which is what makes the two amounts different with a
        // damage roll that cannot vary.
        //
        // A hundred percent off fifty points of armour is no armour at all, so
        // the second shot is what the same roll does to a bare body.
        TheMatch.EventLog log = Fought("tower 14 2 1", "order 0 1 1 0", ticks: 100);
        int[] amounts = Enumerable.Range(0, log.Count)
            .Where(index => log.Kinds[index] == "damaged")
            .Select(index => log.Amounts[index])
            .ToArray();

        Assert.True(amounts.Length >= 3, $"Only {amounts.Length} landings; the fixture stopped shooting.");
        Assert.True(
            amounts[1] > amounts[0],
            $"An armour curse left the second landing at {amounts[1]} against a first of {amounts[0]}.");
        Assert.Equal(amounts[1], amounts[2]);
    }

    [Fact]
    public void A_walking_emitter_grants_a_pool_to_the_creeps_around_it()
    {
        // The Necromancer's shape: a bubble on itself, reaching its own side,
        // carrying a shield. It is the one payload that grants rather than
        // displaces, and the magnitude is a share of the health the pool stands
        // in front of -- there is no rate for a percentage to be a percentage
        // of, and a share of the recipient's own shield column would be nothing
        // at all on every walking row the roster has.
        //
        // Two matches, the same in every way but which row walks. The warden is
        // the plain walker with six columns filled in, its own bubble encloses
        // the hex it is standing on, and the pool it grants itself is spent
        // before its health is -- so it is further from dying after the same
        // number of identical shots.
        int alone = HealthLeftOf("order 0 1 1 0");
        int warded = HealthLeftOf("order 0 3 1 0");

        Assert.True(
            warded > alone,
            $"A creep beside a warden was left on {warded} health against {alone} without one.");
    }

    [Fact]
    public void A_radius_is_measured_in_hexes_so_it_reaches_the_neighbouring_leg_of_a_fold()
    {
        // Why an aura costs the tick loop a position at all. Route distance was
        // the free alternative -- a creep's distance is already a number on a
        // line -- and it was not taken, because a bubble measured along the
        // marching column stops at the fold in a corridor that doubles back:
        // the Necromancer would shield the creeps behind it and not the ones
        // standing a hex away on the next leg.
        //
        // The committed map is a corridor that folds, so it has pairs of cells
        // that are close on the board and a long walk apart along the route. A
        // sphere of the hex distance encloses such a pair; a bubble of the same
        // size measured along the marching column does not reach it at all,
        // because the walk between them is longer.
        HexMap map = TheMatch.Map();
        int folds = 0;

        for (int cell = 0; cell < map.Route.Count; cell++)
        {
            for (int other = cell + 1; other < map.Route.Count; other++)
            {
                int hexes = map.Route[cell].DistanceTo(map.Route[other]);

                if (hexes >= other - cell)
                {
                    continue;
                }

                folds++;

                Assert.True(
                    Reach.Encloses(
                        map.Route[cell],
                        map.LevelAt(map.Route[cell]),
                        hexes * 1000,
                        map.Route[other],
                        map.LevelAt(map.Route[other])),
                    $"Route cells {cell} and {other} are {hexes} hexes apart and a sphere of that size "
                    + "does not enclose them.");
            }
        }

        Assert.True(folds > 0, "The committed map has no fold in it, so this test proves nothing.");
    }

    [Fact]
    public void A_row_whose_bubble_carries_a_stat_no_longer_refuses_to_play()
    {
        // The line #216 drew and this ticket rubs out. A Cryomancer's exact
        // authored shape -- radius 0, origin target, affects enemy, payload
        // speed, a negative magnitude and a positive duration -- used to be
        // refused by name when a match was built out of it, because the columns
        // had landed and the machinery had not. It plays.
        Match match = Corridor("tower 10 2 1", "order 0 1 1 0");

        match.Advance(2);

        Assert.Equal(
            Fix64.FromRatio(WalkerSpeed, 1000).Raw
            + Fix64.FromRatio(Effects.ModifiedSpeed(WalkerSpeed, FrostMagnitude), 1000).Raw,
            Walked(match));
    }

    [Fact]
    public void A_duration_no_arithmetic_can_reach_does_not_wrap_into_an_effect_that_never_landed()
    {
        // A duration is bounded only by the range of the column it is authored
        // in, so tick + duration can leave an int -- and a wrapped sum comes
        // back negative, which reads as an effect that ran out before it
        // landed. The magnitude would then be on the creep for no ticks at all,
        // which is silently the opposite of what the number asked for.
        //
        // OBSERVED: take the saturating clause out of Effects.Land. The first
        // assertion below goes red immediately -- the slow is gone on the tick
        // after it landed -- and nothing else in this file notices, because
        // every other duration here is a number a person would write.
        var effects = default(Effects);

        effects.Land(Slow(-40, int.MaxValue), tick: 5, maxHp: 0);

        Assert.False(effects.Expire(6));
        Assert.Equal(-40, effects.SpeedMagnitude);

        Assert.False(effects.Expire(int.MaxValue));
        Assert.Equal(-40, effects.SpeedMagnitude);
    }

    [Fact]
    public void A_damage_bubble_pointed_at_the_emitters_own_side_lands_on_nothing()
    {
        // Which side a bubble reaches is a column, and a damage bubble is not
        // exempt from it. The two rows below differ in that column and in
        // nothing else: one spreads its roll over what walks, and the other
        // over what stands -- which in this loop is nothing that can be
        // damaged, so the roll goes nowhere.
        //
        // OBSERVED: delete the ReachesInto guard from Match.Land. The inward
        // tower damages creeps exactly as the outward one does, which is a
        // bubble landing on the side its own column says it does not reach.
        TheMatch.EventLog inward = Fought("tower 15 2 1", "order 0 1 1 0", ticks: 100);
        TheMatch.EventLog outward = Fought("tower 16 2 1", "order 0 1 1 0", ticks: 100);

        // Both fire, and the draw is taken either way: a shot that reaches
        // nothing still costs the stream a number, which is what makes the
        // stream's position a count of the shots fired.
        Assert.Equal(outward.CountOf("fired"), inward.CountOf("fired"));
        Assert.True(inward.CountOf("fired") > 0);

        Assert.Equal(0, inward.CountOf("damaged"));
        Assert.True(outward.CountOf("damaged") > 0);
    }

    /// <summary>That defense and that wave, on the ten-hex corridor.</summary>
    private static Match Corridor(string defense, string wave)
    {
        UnitTypeTable types = UnitTypeTable.Parse("effects", TheFixtures);

        return new Match(
            HexMap.Parse("effects map", ACorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("effects defense", defense, types),
            WaveScript.Parse("effects wave", wave, types),
            Seed);
    }

    /// <summary>How far the first creep on the map has walked, in raw Q32.32.</summary>
    private static long Walked(Match match) => match.PullSnapshot().Creeps[0].DistanceAlongPath.Raw;

    /// <summary>Everything a match of that shape said happened.</summary>
    private static TheMatch.EventLog Fought(string defense, string wave, int ticks)
    {
        var log = new TheMatch.EventLog();
        Corridor(defense, wave).Advance(ticks, log);

        return log;
    }

    /// <summary>How many shots one tower of that type gets away in that many ticks.</summary>
    private static int ShotsIn(int towerType, int ticks) =>
        Fought("tower " + towerType + " 2 1", "order 0 1 1 0", ticks).CountOf("fired");

    /// <summary>
    /// What the first creep of that wave has left after a plain tower has been
    /// shooting at it for a while.
    /// </summary>
    private static int HealthLeftOf(string wave)
    {
        Match match = Corridor("tower 12 2 1", wave);
        match.Advance(200);

        return match.PullSnapshot().Creeps[0].Hp;
    }

    // A bubble carrying a slow, which is what most of the tests above land.
    // These go through Bubble rather than through a loose parameter list for
    // the reason Effects.Land's own remarks give: handing a checker the
    // columns one at a time is a chance to pass the wrong one, and a test that
    // can reach a shape no row can author is testing something the simulation
    // will never be asked to do.
    private static Bubble Slow(int magnitude, int durationTicks) =>
        Bubble.Of(
            0,
            BubbleOrigin.Target,
            BubbleAffects.Enemy,
            0,
            BubblePayload.Speed,
            magnitude,
            durationTicks);
}
