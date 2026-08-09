using System.Reflection;

namespace Sim.Tests;

/// <summary>
/// The match: its one public surface, the contract it hands a view, and the
/// handful of things about the skeleton's match that are supposed to be true.
/// </summary>
public class MatchTests
{
    /// <summary>
    /// Every usage scenario, as arguments rather than as code paths. Each one
    /// is a number of ticks per call, whether anybody pulls a snapshot, and
    /// whether anybody is listening.
    /// </summary>
    public static TheoryData<string, int, bool, bool> Scenarios => new()
    {
        { "normal playback", 1, true, true },
        { "fast-forward", 8, true, true },
        { "seek", 500, false, false },
        { "instant-resolve", int.MaxValue, false, false },
        { "the command line", 1, false, true },
        { "the parity run", 1, true, false },
        { "a server re-validating", int.MaxValue, false, true },
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void Every_usage_scenario_is_the_same_call_with_different_arguments(
        string scenario,
        int ticksPerCall,
        bool pullSnapshots,
        bool emitEvents)
    {
        // If any of these needed its own code path the surface would be wrong.
        // They do not: the differences are an integer, a method nobody calls,
        // and an argument left null.
        Match match = TheMatch.Fresh();
        var events = emitEvents ? new TheMatch.EventLog() : null;

        while (!match.IsFinished)
        {
            match.Advance(ticksPerCall, events);

            if (pullSnapshots)
            {
                match.PullSnapshot();
            }
        }

        MatchResult result = match.Result();

        Assert.Equal(TheMatch.LeakedInTheCommittedRun, result.Leaked);
        Assert.Equal(40, result.Total);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
        Assert.Equal(TheMatch.Fresh().Resolve().RollingStateHash, result.RollingStateHash);
        Assert.NotEqual(string.Empty, scenario);
    }

    [Fact]
    public void Pulling_a_snapshot_every_tick_changes_nothing_about_the_match()
    {
        // Instant-resolve is not a mode. It is this, with the pull removed.
        Match watched = TheMatch.Fresh();
        Match headless = TheMatch.Fresh();

        while (!watched.IsFinished)
        {
            watched.Advance(1);
            watched.PullSnapshot();
        }

        headless.Resolve();

        Assert.Equal(headless.Result().RollingStateHash, watched.Result().RollingStateHash);
        Assert.Equal(headless.Result().FinalTick, watched.Result().FinalTick);
    }

    [Fact]
    public void Advancing_in_one_call_or_a_thousand_reaches_the_same_state()
    {
        // A seek is a fresh match run forward to the tick asked for, and this is
        // the claim that makes that legitimate: how the ticks were grouped into
        // calls is not a simulation input.
        Match batched = TheMatch.Fresh();
        Match stepped = TheMatch.Fresh();

        batched.Advance(700);

        for (int tick = 0; tick < 700; tick++)
        {
            stepped.Advance(1);
        }

        Assert.Equal(stepped.StateHash, batched.StateHash);
        Assert.Equal(stepped.Tick, batched.Tick);

        Snapshot one = batched.PullSnapshot();
        Snapshot other = stepped.PullSnapshot();

        Assert.Equal(other.Creeps.Count, one.Creeps.Count);

        for (int index = 0; index < one.Creeps.Count; index++)
        {
            Assert.Equal(other.Creeps[index].Id, one.Creeps[index].Id);
            Assert.Equal(other.Creeps[index].DistanceAlongPath, one.Creeps[index].DistanceAlongPath);
            Assert.Equal(other.Creeps[index].Hp, one.Creeps[index].Hp);
        }
    }

    [Fact]
    public void The_match_is_tuned_to_a_partial_break_over_about_three_minutes()
    {
        // A defense that holds and a defense that collapses are both useless as
        // signals. A partial break means the leak count is a number a person
        // can watch move.
        //
        // The band is a quarter to a half of the wave, which is the target the
        // roster was signed against. Seventeen of forty is where it lands.
        //
        // OBSERVED: divide every order tick in content/wave.txt by three, which
        // is what leaving that file alone through the clock dilation would have
        // meant. The leak goes red at 25 of 40 -- the wave arriving three times
        // faster than the towers now fire is most of what a leak rate is.
        MatchResult result = TheMatch.Fresh().Resolve();
        int seconds = result.FinalTick / Match.TicksPerSecond;

        Assert.Equal(40, result.Total);
        Assert.InRange(result.Leaked, 10, 20);
        Assert.InRange(seconds, 150, 240);
    }

    [Fact]
    public void Every_walking_row_returns_a_comparable_share_of_its_gold_against_the_committed_defense()
    {
        // The roster's own tuning claim, measured rather than asserted. A leak
        // charges health equal to what the creep cost to send, so what a column
        // returns is its leak rate and cost cancels out of it entirely -- which
        // makes survivability the only thing pricing can be wrong about, and
        // makes a row that never leaks a row nobody would ever take.
        //
        // Four hundred gold of one creep against the committed defense, per
        // row. The band is deliberately wide: it is the claim that no row is
        // dead and none is free money, not a pin on numbers a sweep is meant to
        // move.
        //
        // OBSERVED: put the Skeleton Scout at 500 health and 3 gold. It goes
        // red -- "skeleton-scout returned 0 percent of the gold a column of 133
        // cost" -- because five hundred effective health is under what this
        // defense deals a creep while it crosses, so every one of them dies and
        // a whole row of the menu is a dead option that still reads like a
        // choice.
        UnitTypeTable types = TheMatch.Types();
        Ruleset rules = TheRuleset.Committed();
        TowerLayout defense = TheMatch.Layout(types);

        foreach (UnitType creep in types.Types.Where(row => row.Role == UnitRole.Moving))
        {
            int count = 400 / creep.Cost;
            var match = new Match(
                TheMatch.Map(),
                rules,
                defense,
                WaveScript.Parse("order 0 " + creep.Id + " " + count + " 0", types),
                TheMatch.Seed);

            MatchResult result = match.Resolve();
            int returned = result.Leaked * 100 / count;

            Assert.True(
                returned >= 60 && returned <= 95,
                creep.Label
                + " returned "
                + returned
                + " percent of the gold a column of "
                + count
                + " cost, against a roster band of 60 to 95.");
        }
    }

    [Fact]
    public void A_hitscan_shot_puts_nothing_in_the_snapshot_and_a_projectile_shot_puts_an_entity_in_it()
    {
        // The deliberate asymmetry, and the contrast is the test. Both towers
        // fire; only one of them is ever in a picture.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);
        // The two the committed defense actually stands, rather than the two the
        // roster happens to carry: the roster has a sniper and a sieger on it as
        // well, and neither of them is on this board.
        int hitscanType = layout.Towers.Select(tower => tower.Type)
            .Distinct()
            .Single(type => type.Delivery == Delivery.Hitscan)
            .Id;

        int projectileType = layout.Towers.Select(tower => tower.Type)
            .Distinct()
            .Single(type => type.Delivery == Delivery.Projectile)
            .Id;

        Match match = TheMatch.Fresh();
        var events = new TheMatch.EventLog();
        var projectileTypesSeen = new List<int>();
        int hitscanShots = 0;
        int projectileShots = 0;

        while (!match.IsFinished)
        {
            int before = events.Count;
            match.Advance(1, events);

            for (int index = before; index < events.Count; index++)
            {
                if (events.Kinds[index] != "fired")
                {
                    continue;
                }

                int firedType = layout.Towers[events.Subjects[index] - 1].Type.Id;

                if (firedType == hitscanType)
                {
                    hitscanShots++;
                }
                else
                {
                    projectileShots++;
                }
            }

            foreach (ProjectileSnapshot projectile in match.PullSnapshot().Projectiles)
            {
                projectileTypesSeen.Add(projectile.TypeId);
            }
        }

        Assert.True(hitscanShots > 0, "No hitscan tower ever fired, so the asymmetry was not exercised.");
        Assert.True(projectileShots > 0, "No projectile tower ever fired.");
        Assert.NotEmpty(projectileTypesSeen);
        Assert.All(projectileTypesSeen, typeId => Assert.Equal(projectileType, typeId));
        Assert.DoesNotContain(hitscanType, projectileTypesSeen);
    }

    [Fact]
    public void A_projectile_whose_target_dies_mid_flight_stops_appearing()
    {
        // The hardest case in the contract. It needs no special handling: the
        // target lookup does not find anybody, so there is nothing to keep
        // flying, and nothing to linger.
        Match match = TheMatch.Fresh();
        var previousIds = new List<int>();
        var previousFlight = new List<int>();
        var previousDuration = new List<int>();
        int orphaned = 0;
        int landed = 0;

        while (!match.IsFinished)
        {
            match.Advance(1);
            Snapshot snapshot = match.PullSnapshot();
            var ids = snapshot.Projectiles.Select(projectile => projectile.Id).ToList();

            for (int index = 0; index < previousIds.Count; index++)
            {
                if (ids.Contains(previousIds[index]))
                {
                    continue;
                }

                if (previousFlight[index] + 1 >= previousDuration[index])
                {
                    landed++;
                }
                else
                {
                    orphaned++;
                }
            }

            previousIds = ids;
            previousFlight = snapshot.Projectiles.Select(projectile => projectile.TicksInFlight).ToList();
            previousDuration = snapshot.Projectiles.Select(projectile => projectile.FlightDurationTicks).ToList();
        }

        Assert.True(orphaned > 0, "No projectile was ever orphaned, so the hardest case went untested.");
        Assert.True(landed > 0, "No projectile ever landed, so orphaning is not the only thing happening.");
        Assert.Empty(match.PullSnapshot().Projectiles);
    }

    [Fact]
    public void Dying_lasts_the_number_of_ticks_the_type_says()
    {
        // A real simulation state with an integer duration, so seeking into a
        // death shows a death -- rather than the view owning a corpse the
        // simulation has forgotten about.
        UnitTypeTable types = TheMatch.Types();
        int dyingTicks = types.ById(1).DyingTicks;
        Assert.True(dyingTicks > 0);

        Match match = TheMatch.Fresh();
        var longestSeen = new Dictionary<int, int>();

        while (!match.IsFinished)
        {
            match.Advance(1);

            foreach (CreepSnapshot creep in match.PullSnapshot().Creeps)
            {
                if (creep.State != CreepState.Dying)
                {
                    continue;
                }

                Assert.Equal(0, creep.Hp);
                Assert.InRange(creep.TicksInState, 0, dyingTicks - 1);
                longestSeen[creep.Id] = creep.TicksInState;
            }
        }

        Assert.NotEmpty(longestSeen);
        Assert.Contains(longestSeen, entry => entry.Value == dyingTicks - 1);
    }

    [Fact]
    public void A_later_fast_group_catches_an_earlier_slow_one()
    {
        // Ids ascend with spawn order, so a creep with a higher id further along
        // the corridor than one with a lower id is an overtake -- which is what
        // makes unit ordering observable rather than theoretical.
        Match match = TheMatch.Fresh();
        int firstOvertake = -1;

        while (!match.IsFinished && firstOvertake < 0)
        {
            match.Advance(1);
            Snapshot snapshot = match.PullSnapshot();

            for (int index = 1; index < snapshot.Creeps.Count; index++)
            {
                Assert.True(snapshot.Creeps[index].Id > snapshot.Creeps[index - 1].Id);

                if (snapshot.Creeps[index].DistanceAlongPath > snapshot.Creeps[index - 1].DistanceAlongPath)
                {
                    firstOvertake = snapshot.Tick;
                }
            }
        }

        Assert.True(firstOvertake > 0, "Nothing ever overtook anything.");
    }

    [Fact]
    public void Two_towers_can_commit_to_one_creep_on_one_tick()
    {
        // Overkill and iteration order, made real by the ranges overlapping.
        Match match = TheMatch.Fresh();
        int shared = 0;

        while (!match.IsFinished)
        {
            match.Advance(1);

            int[] aimed = match.PullSnapshot().Towers
                .Where(tower => tower.State == TowerState.Windup && tower.TargetId != 0)
                .Select(tower => tower.TargetId)
                .ToArray();

            shared += aimed.Length - aimed.Distinct().Count();
        }

        Assert.True(shared > 0, "No two towers ever wound up at the same creep, so overkill never happened.");
    }

    [Fact]
    public void A_tower_shoots_the_creep_furthest_along_and_breaks_a_tie_on_the_lower_id()
    {
        // Target selection against an analytic oracle rather than by eye: the
        // rule is "furthest along the corridor, lowest id if level", so the
        // snapshot alone says who each tower should have picked.
        //
        // The tie half is the point, and it is fought on a wave built here
        // rather than on the committed one. That is a correction. The committed
        // wave does still put creeps at the same distance -- fifty-one times
        // over the match -- but a tie only tests anything if it lands on the
        // tick a tower acquires, and whether those two schedules coincide is
        // luck. It held until the clock dilation of 8 August 2026 moved
        // content/wave.txt and cut tower acquisitions to a third of what they
        // were, at which point the coincidence stopped happening and this
        // assertion went red having quietly tested nothing about ties for as
        // long as the coincidence had lasted.
        //
        // The wave below cannot stop tying. The Minion and the Skeleton walk at
        // exactly the same speed under the signed roster -- 28 each -- so two
        // orders released on the same tick stay level for the whole crossing
        // and every acquisition in range is a tie. "Closest to the exit" on its
        // own is not a rule that can be replayed, because it leaves the answer
        // to whichever of them happened to be looked at first.
        //
        // OBSERVED: drop the lower-id clause from target selection in
        // Match, leaving "furthest along". The tie run goes red naming the
        // creep it picked instead; the committed run below stays green, which
        // is exactly why the tie half could not be left to it.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);

        Assert.Equal(types.ById(12).SpeedMilliHexPerTick, types.ById(1).SpeedMilliHexPerTick);

        CheckTargeting(
            new Match(
                TheMatch.Map(),
                TheRuleset.Committed(),
                layout,
                WaveScript.Parse("order 0 1 12 0\norder 0 12 12 0", types),
                TheMatch.Seed),
            requireTies: true);

        CheckTargeting(TheMatch.Fresh(), requireTies: false);
    }

    [Fact]
    public void A_creep_first_appears_at_the_entrance_having_walked_nowhere()
    {
        // The wave releases at the end of a tick, after everything that moves
        // has moved and after the towers have chosen -- the phase order in
        // Match.Step. This is the half of that a picture can see, and it is
        // why targeting on a spawn tick is asked of the rule directly in
        // TargetingTests rather than of a snapshot here: the creep standing at
        // the entrance in this tick's picture is one the towers did not see.
        // Every one of the forty is caught on the tick it was released.
        //
        // OBSERVED: move the Release call in Match.Step above MoveCreeps. It
        // goes red at 1 of 40 -- everything released inside a tick now walks a
        // step in that same tick, so only the one the constructor released is
        // ever seen standing at the entrance.
        Match match = TheMatch.Fresh();
        Snapshot snapshot = match.PullSnapshot();
        int released = 0;

        while (true)
        {
            foreach (CreepSnapshot creep in snapshot.Creeps)
            {
                if (creep.State != CreepState.Walking || creep.TicksInState != 0)
                {
                    continue;
                }

                Assert.Equal(Fix64.Zero, creep.DistanceAlongPath);
                released++;
            }

            if (match.IsFinished)
            {
                break;
            }

            match.Advance(1);
            snapshot = match.PullSnapshot();
        }

        Assert.Equal(40, released);
    }

    /// <summary>
    /// Walks a match and holds every target acquisition against the analytic
    /// rule: furthest along the corridor, lowest id where two are level.
    /// </summary>
    /// <remarks>
    /// The intervals come off the match rather than from a second table built
    /// beside it, so what the acquisition is held against is the table the
    /// acquisition went through.
    /// </remarks>
    private static void CheckTargeting(Match match, bool requireTies)
    {
        int acquisitions = 0;
        int ties = 0;

        while (!match.IsFinished)
        {
            match.Advance(1);
            Snapshot snapshot = match.PullSnapshot();

            // A creep that spawned or died this tick was a different creep
            // when the towers looked, so those ticks are not ones the
            // snapshot can be an oracle for. TargetingTests asks the rule
            // about both of them directly, which is the only way they get
            // covered at all: measured over both waves below, not one of the
            // 622 acquisitions this walk checks lands on a spawn tick, so a
            // refinement here that recovered those ticks would be an
            // assertion about nothing.
            bool settled = snapshot.Creeps.All(creep => creep.TicksInState > 0);

            if (!settled)
            {
                continue;
            }

            foreach (TowerSnapshot tower in snapshot.Towers)
            {
                if (tower.State != TowerState.Windup || tower.TicksInState != 0)
                {
                    continue;
                }

                CreepSnapshot[] reachable = snapshot.Creeps
                    .Where(creep => creep.State == CreepState.Walking)
                    .Where(creep => match.Coverage.Covers(tower.Id - 1, creep.DistanceAlongPath))
                    .ToArray();

                Assert.NotEmpty(reachable);

                Fix64 furthest = reachable.Max(creep => creep.DistanceAlongPath);
                CreepSnapshot[] level = reachable
                    .Where(creep => creep.DistanceAlongPath == furthest)
                    .ToArray();

                Assert.Equal(level.Min(creep => creep.Id), tower.TargetId);

                acquisitions++;

                if (level.Length > 1)
                {
                    ties++;
                }
            }
        }

        Assert.True(acquisitions > 100, $"Only {acquisitions} target acquisitions were checked.");

        if (requireTies)
        {
            Assert.True(ties > 0, "No tower ever had to break a tie, so the tiebreak rule went untested.");
        }
    }

    [Fact]
    public void The_defense_is_one_array_that_never_changes()
    {
        // Towers are invulnerable and static for the whole match, so the only
        // thing that can vary about the tower half of a snapshot is what each
        // one is doing.
        Match match = TheMatch.Fresh();
        int[] ids = match.PullSnapshot().Towers.Select(tower => tower.Id).ToArray();

        Assert.Equal(6, ids.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, ids);

        while (!match.IsFinished)
        {
            match.Advance(97);
            Assert.Equal(ids, match.PullSnapshot().Towers.Select(tower => tower.Id).ToArray());
        }

        // No health, and nothing that could hold a position: both are static
        // data the view loaded once, and the snapshot carries only what moves.
        Assert.Equal(new[] { "Id", "State", "TargetId", "TicksInState" }, FieldsOf<TowerSnapshot>());
    }

    [Fact]
    public void A_creep_is_a_distance_and_an_offset_and_never_a_point()
    {
        // Free 2D never enters the simulation. There is no field here that
        // could hold one, which is what makes that permanent rather than a
        // habit -- and the same is true of what a projectile is aimed at.
        Assert.Equal(
            new[] { "DistanceAlongPath", "Hp", "Id", "LateralOffset", "State", "TicksInState", "TypeId" },
            FieldsOf<CreepSnapshot>());

        Assert.Equal(new[] { "Id", "Kind" }, FieldsOf<TargetRef>());

        Assert.Equal(
            new[] { "FlightDurationTicks", "Id", "Target", "TicksInFlight", "TypeId" },
            FieldsOf<ProjectileSnapshot>());
    }

    /// <summary>
    /// What one of these carries, by name. Instance members only: a static like
    /// <see cref="TargetRef.None"/> is a way of writing a value down, not a
    /// field the thing is made of.
    /// </summary>
    private static string[] FieldsOf<T>() =>
        typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    [Fact]
    public void Creeps_are_given_a_lateral_offset_so_a_pass_is_visible()
    {
        // Two creeps at the same distance is the moment an overtake happens.
        // Without an offset they would be the same point, and the pass would be
        // invisible on screen while being perfectly real in the simulation.
        Match match = TheMatch.Fresh();
        var offsets = new List<Fix64>();

        while (!match.IsFinished && offsets.Distinct().Count() < 2)
        {
            match.Advance(1);
            offsets.AddRange(match.PullSnapshot().Creeps.Select(creep => creep.LateralOffset));
        }

        Assert.True(offsets.Distinct().Count() > 1, "Every creep walks down the exact centre line.");
    }

    [Fact]
    public void The_damage_roll_is_the_only_randomness_and_it_is_one_draw_per_shot()
    {
        // The highest-frequency call site in the match, doubling as the most
        // sensitive detector of a unit-ordering desync. This reconstructs the
        // stream independently: draw once per shot, in the order the shots were
        // fired, and every hitscan shot's damage has to be that number resolved
        // through the ruleset. A second draw anywhere, or a draw skipped, walks
        // the two sequences out of step immediately.
        //
        // What lands is the roll through the matrix, and the wave sends two
        // armour types, so a shot resolves to one of exactly two numbers -- the
        // cell against Armoured or the cell against Swift. Both are computed
        // from the same draw, which is what makes this an assertion about the
        // stream rather than about the target; the exact amount against a known
        // target is DamageWiringTests' claim rather than this one's.
        //
        // OBSERVED: draw a second time inside Match.Fire -- roll the damage,
        // then roll it again and use the second number. This goes red on the
        // first hitscan landing, 102 against the [81, 163] the reconstructed
        // roll resolves to, and the reconstruction never recovers.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);
        Ruleset rules = TheRuleset.Committed();

        var events = new TheMatch.EventLog();
        TheMatch.Fresh().Resolve(events);

        var dice = new Pcg32(TheMatch.Seed);
        int shots = 0;
        int checkedLandings = 0;

        for (int index = 0; index < events.Count; index++)
        {
            if (events.Kinds[index] != "fired")
            {
                continue;
            }

            UnitType type = layout.Towers[events.Subjects[index] - 1].Type;
            int expected = dice.NextInRange(type.DamageMin, type.DamageMax + 1);
            shots++;

            // A hitscan shot lands inside the same call that fired it, so its
            // damage event is the very next thing in the stream. A projectile's
            // is not, which is the whole difference between them.
            bool landedImmediately = index + 1 < events.Count && events.Kinds[index + 1] == "damaged";

            if (type.Delivery == Delivery.Hitscan && landedImmediately)
            {
                Assert.Contains(
                    events.Amounts[index + 1],
                    new[]
                    {
                        DamageModel.Dealt(rules, expected, 0, type.AttackType, ArmourType.Armoured, 0),
                        DamageModel.Dealt(rules, expected, 0, type.AttackType, ArmourType.Swift, 0),
                    });

                checkedLandings++;
            }
        }

        Assert.True(shots > 100, "Too few shots for this to have proved anything.");
        Assert.True(checkedLandings > 50, "Too few hitscan shots landed for this to have proved anything.");
    }

    [Fact]
    public void Events_are_emitted_only_if_somebody_asked_for_them()
    {
        // Which is also how a seek discards them: the re-simulation passes
        // nothing, so there is no discarding code and nothing to forget.
        var events = new TheMatch.EventLog();
        MatchResult heard = TheMatch.Fresh().Resolve(events);
        MatchResult silent = TheMatch.Fresh().Resolve();

        Assert.Equal(silent.RollingStateHash, heard.RollingStateHash);
        Assert.True(events.Count > 0);
        Assert.Equal(TheMatch.LeakedInTheCommittedRun, events.CountOf("leaked"));
        Assert.Equal(40 - TheMatch.LeakedInTheCommittedRun, events.CountOf("died"));
    }

    [Fact]
    public void Every_event_is_purely_decorative_by_the_shape_of_the_interface()
    {
        // The rule that prevents the bug where a projectile becomes a particle
        // it cannot scrub. Nothing here carries a position, a duration or a
        // handle -- there is nothing to hold on to, so an effect built from one
        // cannot own a lifetime the simulation does not.
        MethodInfo[] methods = typeof(IMatchEvents).GetMethods();

        Assert.Equal(
            new[]
            {
                "CreepDamaged",
                "CreepDied",
                "CreepLeaked",
                "CreepOvertook",
                "ProjectileOrphaned",
                "TowerFired",
            },
            methods.Select(method => method.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());

        foreach (MethodInfo method in methods)
        {
            Assert.Equal(typeof(void), method.ReturnType);
            Assert.All(method.GetParameters(), parameter => Assert.Equal(typeof(int), parameter.ParameterType));
        }
    }

    [Fact]
    public void A_result_before_the_end_of_the_match_throws()
    {
        Match match = TheMatch.Fresh();
        match.Advance(200);

        SimulationException thrown = Assert.Throws<SimulationException>(() => match.Result());
        Assert.Contains("not over", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Advancing_a_finished_match_does_nothing_at_all()
    {
        // So fast-forwarding past the end is the same call as any other.
        Match match = TheMatch.Fresh();
        match.Resolve();

        Hash64 hash = match.StateHash;
        Assert.Equal(0, match.Advance(1000));
        Assert.Equal(hash, match.StateHash);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, match.Tick);
    }

    [Fact]
    public void Running_backwards_is_not_a_negative_number_of_ticks()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TheMatch.Fresh().Advance(-1));
    }
}
