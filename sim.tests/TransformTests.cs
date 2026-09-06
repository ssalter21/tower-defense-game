namespace Sim.Tests;

/// <summary>
/// A creep becoming another row mid-lane: what triggers it, what order it
/// happens in against the damage that triggered it, what carries over, and what
/// the table refuses.
/// </summary>
/// <remarks>
/// <para>
/// <b>The committed pair is asserted against <c>content/units.txt</c> and every
/// other row here is a fixture.</b> The Cursed Villager becoming the Werewolf is
/// content and is signed in <c>docs/roster.md</c>; the ids, labels and numbers
/// below mean nothing outside this file and exist because the committed roster
/// authors exactly one transformation, which cannot show a shield swallowing a
/// roll, a chain, or a successor whose numbers are identical to its
/// predecessor's.
/// </para>
/// <para>
/// The corridor and the tower are ShotShapeTests' shape and for its reason: five
/// hexes, every cell covered, and a tower that winds up and recovers in nothing
/// so one attack fits in a handful of ticks.
/// </para>
/// </remarks>
public class TransformTests
{
    private const string AShortCorridor = """
        S###E
        .....

        aaaaa
        aaaaa
        """;

    /// <summary>
    /// Six walkers and two turrets. <c>larva</c> and <c>lone</c> are the same
    /// row twice and differ only in whether it names a successor, which is what
    /// makes the pair a controlled comparison; <c>twin</c> and <c>clone</c> are
    /// the same row twice again, so that a body turning from one into the other
    /// changes no folded number except which row it is.
    /// </summary>
    private const string TheFixtures = """
        layout 4
        unit  1 larva  moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0    1 none none none 0 none 0 0 2
        unit  2 imago  moving 9000 25 0    0  0 0 0    0    none    0 4 30 none   swift    20 0    1 none none none 0 none 0 0 none
        unit  3 warded moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  9000 1 none none none 0 none 0 0 2
        unit  4 lone   moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0    1 none none none 0 none 0 0 none
        unit  5 twin   moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0    1 none none none 0 none 0 0 6
        unit  6 clone  moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0    1 none none none 0 none 0 0 none
        unit 11 cannon placed 0    0  4000 30 0 0 600  600  hitscan 0 0 40 pierce none     0  0    1 none none none 0 none 0 0 none
        unit 12 pin    placed 0    0  4000 30 0 0 90   150  hitscan 0 0 40 pierce none     0  0    1 none none none 0 none 0 0 none
        """;

    /// <summary>The row that names a successor, and the same row naming none.</summary>
    private const string TwinBecomesClone =
        "unit  5 twin   moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0    1 none none none 0 none 0 0 6";

    private const string TwinBecomesNobody =
        "unit  5 twin   moving 400  10 0    0  0 0 0    0    none    0 4 30 none   armoured 0  0    1 none none none 0 none 0 0 none";

    private const ulong Seed = 20260906UL;

    /// <summary>One attack: the turret fires on tick one and again thirty later.</summary>
    private const int OneAttack = 5;

    /// <summary>The turret that rolls an ordinary shot, and the one that rolls a lethal flat 600.</summary>
    private const int Pin = 12;

    private const int Cannon = 11;

    /// <summary>The Cursed Villager and the Werewolf, by the ids docs/roster.md signs.</summary>
    private const int CursedVillager = 47;

    private const int Werewolf = 48;

    [Fact]
    public void The_cursed_villager_becomes_the_werewolf_on_the_first_damage_it_takes()
    {
        // The signed mechanic, on the committed roster and against the committed
        // defense. One Villager walks into four archers and two mages, and the
        // first roll that reaches its health hands the rest of the corridor a
        // Werewolf wearing the same entity id.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(Werewolf, types.ById(CursedVillager).Becomes?.Id);

        var match = new Match(
            TheMatch.Map(),
            TheRuleset.Committed(),
            TheMatch.Layout(types),
            WaveScript.Parse("one villager", "order 0 " + CursedVillager + " 1 0", types),
            TheMatch.Seed);

        var log = new TheMatch.EventLog();
        int before = 0;
        int after = 0;

        while (!match.IsFinished && log.CountOf("became") == 0)
        {
            before = TypeDrawn(match);
            log.EnteringTick(match.Tick + 1);
            match.Advance(1, log);
            after = TypeDrawn(match);
        }

        // It happened, once, and it named the row the snapshot is now reporting.
        int[] changes = log.IndicesOf("became");

        Assert.Single(changes);
        Assert.Equal(Werewolf, log.Amounts[changes[0]]);
        Assert.Equal(CursedVillager, before);
        Assert.Equal(Werewolf, after);

        // On the tick the first damage landed, and not before it and not after.
        Assert.Equal(1, log.CountOf("damaged"));
        Assert.Equal(log.Ticks[log.IndicesOf("damaged")[0]], log.Ticks[changes[0]]);

        // And the change is reported before the damage it resolved ahead of, so
        // a reader of the stream sees the body change and then take the hit.
        Assert.True(changes[0] < log.IndicesOf("damaged")[0]);
    }

    [Fact]
    public void The_werewolf_enters_on_its_own_full_pool_and_the_villager_never_loses_a_point()
    {
        // What carries over, on the committed pair: the share of the pool is
        // whatever the body had, and under this trigger that is all of it --
        // nothing has come off health when the change resolves. So the Werewolf
        // enters on 2600 and the roll then comes off THAT, which is the
        // arithmetic docs/roster.md signs in as many words.
        UnitTypeTable types = TheMatch.Types();
        UnitType werewolf = types.ById(Werewolf);

        var match = new Match(
            TheMatch.Map(),
            TheRuleset.Committed(),
            TheMatch.Layout(types),
            WaveScript.Parse("one villager", "order 0 " + CursedVillager + " 1 0", types),
            TheMatch.Seed);

        var log = new TheMatch.EventLog();

        while (!match.IsFinished && log.CountOf("became") == 0)
        {
            log.EnteringTick(match.Tick + 1);
            match.Advance(1, log);
        }

        CreepSnapshot body = match.PullSnapshot().Creeps.Single();
        int dealt = log.Amounts[log.IndicesOf("damaged")[0]];

        Assert.Equal(Werewolf, body.TypeId);
        Assert.Equal(werewolf.MaxHp - dealt, body.Hp);

        // The Villager's own 1800 is untouched by the hit that ended it, which
        // is the half of the reading the price rule cannot see: what a defense
        // spends on this body is one roll plus the Werewolf's whole pool.
        Assert.True(dealt < types.ById(CursedVillager).MaxHp);
    }

    [Fact]
    public void A_hit_that_would_kill_the_row_outright_leaves_the_row_it_becomes_standing()
    {
        // "Cannot be one-shot", and it is arithmetic rather than a clamp: the
        // row that named a successor is already gone when the death check runs,
        // so no hit of any size can kill it. The control is the same row with
        // the successor struck out, dying to the identical shot.
        //
        // OBSERVED: move the change to after the death check in Match.Damage.
        // The control stays green -- it never had a successor -- and this goes
        // red with a corpse: 600 through the matrix is more than four hundred
        // health, so the body dies as a larva and the imago never exists.
        TheMatch.EventLog changed = Played(Cannon, "order 0 1 1 0");
        TheMatch.EventLog control = Played(Cannon, "order 0 4 1 0");

        Assert.Equal(1, control.CountOf("damaged"));
        Assert.Equal(1, control.CountOf("died"));
        Assert.Equal(0, control.CountOf("became"));

        Assert.Equal(1, changed.CountOf("damaged"));
        Assert.Equal(0, changed.CountOf("died"));
        Assert.Equal(1, changed.CountOf("became"));
    }

    [Fact]
    public void The_roll_lands_on_the_row_the_body_became_and_not_on_the_row_it_was()
    {
        // The consequence of resolving the change ahead of the damage, and the
        // one a reader has to be told: the shot was aimed at an armoured body
        // and lands on a swift one. Both readings are computed here rather than
        // written down, so the assertion is about which row the matrix was asked
        // about and not about the matrix.
        Ruleset rules = TheRuleset.Committed();
        UnitTypeTable types = UnitTypeTable.Parse("transform fixtures", TheFixtures);
        UnitType turret = types.ById(Cannon);
        UnitType larva = types.ById(1);
        UnitType imago = types.ById(2);

        int asItWas = DamageModel.Dealt(
            rules, turret.DamageMin, 0, turret.AttackType, larva.ArmourType, larva.Armour);

        int asItBecame = DamageModel.Dealt(
            rules, turret.DamageMin, 0, turret.AttackType, imago.ArmourType, imago.Armour);

        Assert.NotEqual(asItWas, asItBecame);

        TheMatch.EventLog log = Played(Cannon, "order 0 1 1 0");

        Assert.Equal(asItBecame, log.Amounts[log.IndicesOf("damaged")[0]]);
    }

    [Fact]
    public void A_shield_that_swallows_a_roll_whole_is_not_damage_taken_and_changes_nothing()
    {
        // The trigger is damage REACHING HEALTH. A pool in front of it absorbs
        // raw and before the matrix, and a roll it eats whole is a shot that
        // took nothing off anything -- so the body is still what it was, no
        // event is emitted, and the next roll to get through is the first one.
        //
        // OBSERVED: move the change above Absorbed in Match.Damage. This goes
        // red with a transformation nobody could see the cause of: a body that
        // took no damage at all changes row, and a shield stops being a delay
        // and starts being a trigger.
        TheMatch.EventLog log = Played(Cannon, "order 0 3 1 0");

        Assert.Equal(1, log.CountOf("fired"));
        Assert.Equal(0, log.CountOf("damaged"));
        Assert.Equal(0, log.CountOf("became"));
    }

    [Fact]
    public void The_body_keeps_its_id_its_lane_and_its_place_on_the_route()
    {
        // The same body, which is what makes this a transformation rather than a
        // death and a spawn: everything aimed at it is still aimed at it, and
        // nothing about where it is moves. Its speed does move, because it walks
        // at the new row's speed from the tick it becomes it.
        UnitTypeTable types = UnitTypeTable.Parse("transform fixtures", TheFixtures);
        Match match = Built(TheFixtures, "order 0 1 1 0");

        var log = new TheMatch.EventLog();
        CreepSnapshot before = default;

        while (!match.IsFinished && log.CountOf("became") == 0)
        {
            before = match.PullSnapshot().Creeps.SingleOrDefault();
            log.EnteringTick(match.Tick + 1);
            match.Advance(1, log);
        }

        CreepSnapshot after = match.PullSnapshot().Creeps.Single();

        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.LateralOffset, after.LateralOffset);
        Assert.Equal(CreepState.Walking, after.State);
        Assert.True(after.DistanceAlongPath >= before.DistanceAlongPath);

        // And it is now the other row in every way, not only in what it draws
        // as: the pool it is standing on is the pool that row authors, less the
        // roll that arrived after the change.
        Assert.Equal(2, after.TypeId);
        Assert.Equal(
            types.ById(2).MaxHp - log.Amounts[log.IndicesOf("damaged")[0]],
            after.Hp);
    }

    [Fact]
    public void The_rolling_hash_covers_which_row_a_body_is()
    {
        // Two matches whose every folded number agrees and whose bodies are not
        // the same row. `twin` and `clone` carry identical numbers, so a body
        // that becomes the second has the same health, the same step, the same
        // phase and the same distance as one that does not -- and the ONLY thing
        // the fold could tell them apart by is the type id.
        //
        // OBSERVED: take `.Add(creep.Type.Id)` back out of Match.Fold's
        // per-creep loop. This goes red with the two hashes equal, and the
        // golden trace stops being able to tell a run where a body transformed
        // from a run where it did not.
        Assert.NotEqual(
            HashOf(TheFixtures),
            HashOf(TheFixtures.Replace(TwinBecomesClone, TwinBecomesNobody)));
    }

    [Fact]
    public void Exactly_one_committed_row_names_a_successor_and_it_is_the_villager()
    {
        // The roster's own claim, measured. A second transforming pair is a
        // design decision docs/roster.md would have to sign, so a row that
        // gained one quietly goes red here.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(
            new[] { "cursed-villager becomes werewolf" },
            types.Types
                .Where(row => row.Becomes is not null)
                .Select(row => row.Label + " becomes " + row.Becomes!.Label)
                .ToArray());
    }

    [Fact]
    public void A_row_that_becomes_something_no_row_authored_refuses_by_name()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("transform fixtures", TheFixtures.Replace(" 0 0 2\n", " 0 0 99\n")));

        Assert.Contains("becomes type 99", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_becomes_itself_refuses_by_name()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("transform fixtures", TheFixtures.Replace(" 0 0 2\n", " 0 0 1\n")));

        Assert.Contains("becomes itself", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_becomes_a_tower_refuses_by_name()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("transform fixtures", TheFixtures.Replace(" 0 0 2\n", " 0 0 12\n")));

        Assert.Contains("stands where it was put", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_stands_where_it_was_put_may_not_become_anything()
    {
        // The refusal from the other side: nothing that stands is ever damaged
        // here, so the moment the change is triggered on never arrives and the
        // column would be read by nothing at all.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(
                "transform fixtures",
                "layout 4\n"
                + "unit  1 walker moving 400 10 0    0  0 0 0   0   none    0 4 30 none   armoured 0 0 1 none none none 0 none 0 0 none\n"
                + "unit 12 pin    placed 0   0  4000 30 0 0 90  150 hitscan 0 0 40 pierce none     0 0 1 none none none 0 none 0 0 1\n"));

        Assert.Contains("names a row it becomes", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_chain_refuses_by_name()
    {
        // A body changes row once. A chain would make the trigger every hit
        // that lands rather than the first one, and would make the slowest speed
        // a match has to terminate at a walk over a graph rather than a
        // comparison of two rows.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(
                "transform fixtures",
                TheFixtures.Replace("armoured 0  0    1 none none none 0 none 0 0 6", "armoured 0  0    1 none none none 0 none 0 0 4")
                    .Replace("swift    20 0    1 none none none 0 none 0 0 none", "swift    20 0    1 none none none 0 none 0 0 6")));

        Assert.Contains("becomes something in its turn", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_match_refuses_a_successor_that_would_never_reach_the_exit()
    {
        // The termination invariant, which a transformation walks straight
        // through unless the bound is taken against both rows: a body released
        // at a legal speed that turns into one crawling at nothing is a match
        // that runs to the ceiling and throws thousands of ticks after the
        // mistake.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Built(
                TheFixtures.Replace("unit  2 imago  moving 9000 25", "unit  2 imago  moving 9000 0 "),
                "order 0 1 1 0"));

        Assert.Contains("which has no speed", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>What the one creep on the map is drawn as, or zero if it is gone.</summary>
    private static int TypeDrawn(Match match)
    {
        IReadOnlyList<CreepSnapshot> creeps = match.PullSnapshot().Creeps;

        return creeps.Count == 0 ? 0 : creeps[0].TypeId;
    }

    /// <summary>
    /// The corridor, one turret on it, and a wave, out of one roster. The
    /// turret defaults to the one that rolls an ordinary shot, because most of
    /// these want a body hit rather than a body hit hard.
    /// </summary>
    private static Match Built(string units, string wave, int towerType = Pin)
    {
        UnitTypeTable types = UnitTypeTable.Parse("transform fixtures", units);

        return new Match(
            HexMap.Parse("transform map", AShortCorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("transform defense", "tower " + towerType + " 2 1", types),
            WaveScript.Parse("transform wave", wave, types),
            Seed);
    }

    private static TheMatch.EventLog Played(int towerType, string wave) =>
        TheMatch.Watched(Built(TheFixtures, wave, towerType), OneAttack);

    /// <summary>What the rolling hash is after one attack on a column of twins.</summary>
    private static Hash64 HashOf(string units)
    {
        Match match = Built(units, "order 0 5 1 0");

        match.Advance(OneAttack);

        return match.StateHash;
    }
}
