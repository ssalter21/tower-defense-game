namespace Sim.Tests;

/// <summary>
/// The two shot shapes and the second health pool, in a match: how many numbers
/// an attack takes off the dice, how many bodies each shape lands on, and what a
/// shield does to a hit on its way to health.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every row here is a fixture and none of them is content.</b> Naming and
/// statting a tower is a design decision and #216 took none, so what is proved
/// below is that the schema and the tick loop carry each shape -- the ids,
/// labels and numbers are stand-ins and mean nothing outside this file. The
/// committed roster authors one shot, no shield and no bubble on every row, so
/// none of this is reachable through <c>content/units.txt</c> and all of it
/// would otherwise be untested.
/// </para>
/// <para>
/// <b>The draw count is asserted by reconstructing the stream, not by counting
/// events.</b> A test that counted <c>fired</c> events would pass a build that
/// drew twice per shot. Drawing independently from the same seed and requiring
/// every landing to be that number resolved through the ruleset walks the two
/// sequences out of step the moment a draw is added or skipped -- which is the
/// same technique <see cref="MatchTests"/> uses on the committed match, pointed
/// at the shapes that committed match has none of.
/// </para>
/// </remarks>
public class ShotShapeTests
{
    /// <summary>
    /// Five hexes of corridor with a tower cell under the middle of it. Every
    /// route cell is within three hexes of that cell, which is what lets one
    /// bubble centred there enclose the whole route.
    /// </summary>
    private const string AShortCorridor = """
        S###E
        .....

        aaaaa
        aaaaa
        """;

    /// <summary>
    /// Three walkers that are the same walker three times, and the towers each
    /// test needs. Three rows rather than one order of three because a wave
    /// releases one unit per order every forty-five ticks: three orders on tick
    /// zero put three bodies on the same hex, which is what a shape that hits
    /// more than one body has to be measured against.
    /// </summary>
    /// <remarks>
    /// The towers are 4000 milli-hexes so every route cell is covered, and they
    /// wind up and recover in nothing so that one attack fits inside a handful
    /// of ticks with the next one thirty ticks away.
    /// </remarks>
    private const string TheFixtures = """
        layout 3
        unit  1 alpha   moving 5000 10 0 0 0 0 0 0 none 0 4 30 none armoured 0 0 1 none none none 0 none 0 0
        unit  2 beta    moving 5000 10 0 0 0 0 0 0 none 0 4 30 none armoured 0 0 1 none none none 0 none 0 0
        unit  3 gamma   moving 5000 10 0 0 0 0 0 0 none 0 4 30 none armoured 0 0 1 none none none 0 none 0 0
        unit  4 warded  moving 5000 10 0 0 0 0 0 0 none 0 4 30 none armoured 0 40 1 none none none 0 none 0 0
        unit  5 sealed  moving 5000 10 0 0 0 0 0 0 none 0 4 30 none armoured 0 200 1 none none none 0 none 0 0
        unit 10 single  placed 0 0 4000 30 0 0 90 150 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0
        unit 11 volley  placed 0 0 4000 30 0 0 90 150 hitscan 0 0 120 pierce none 0 0 3 none none none 0 none 0 0
        unit 12 sweep   placed 0 0 4000 30 0 0 90 150 hitscan 0 0 40 pierce none 0 0 1 3000 self enemy 0 damage 0 0
        unit 13 mortar  placed 0 0 4000 30 0 0 90 150 projectile 5 0 40 pierce none 0 0 1 3000 target enemy 0 damage 0 0
        unit 14 flat    placed 0 0 4000 30 0 0 100 100 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0
        unit 15 frost   placed 0 0 4000 30 0 0 90 150 hitscan 0 0 40 pierce none 0 0 1 0 target enemy 0 speed -40 120
        unit 16 banner  placed 0 0 4000 30 0 0 90 150 hitscan 0 0 40 pierce none 0 0 1 3000 self friend 45 cooldown -20 60
        """;

    /// <summary>Three bodies released on tick zero, one per order.</summary>
    private const string ThreeAbreast = """
        order 0 1 1 0
        order 0 2 1 0
        order 0 3 1 0
        """;

    private const ulong Seed = 20260816UL;

    /// <summary>
    /// How far the match is run. The towers wind up in nothing and recover in
    /// nothing, so the first attack lands on tick one and the next is thirty
    /// ticks behind it: five ticks is exactly one attack.
    /// </summary>
    private const int OneAttack = 5;

    [Fact]
    public void A_targets_of_n_fires_n_shots_at_n_creeps_and_draws_n_rolls()
    {
        // Half of the determinism contract. Three shots, three creeps, three
        // draws off the one stream in the order the shots were released.
        //
        // OBSERVED: draw once in Fire and hand the same number to all three
        // shots. The first landing still matches, because the first draw is the
        // same draw; the second goes red, 63 against the 84 the second
        // reconstructed roll resolves to. That is the failure this reconstructs
        // the stream to catch, and a test that counted events would be green
        // for it.
        TheMatch.EventLog log = Played(11, ThreeAbreast);

        Assert.Equal(3, log.CountOf("fired"));
        Assert.Equal(3, log.CountOf("damaged"));

        // Three different creeps, not one creep three times. The rule takes
        // them nearest-to-exit first and these three are level, so it settles
        // on the lowest id -- and having taken one it does not take it again.
        Assert.Equal(3, log.Subjects.Where((_, index) => log.Kinds[index] == "damaged").Distinct().Count());

        AssertLandingsAreTheStream(log, 11, 3);
    }

    [Fact]
    public void A_bubble_is_one_shot_and_one_roll_however_many_bodies_it_lands_on()
    {
        // The other half. One shot, one draw, three bodies -- at full damage on
        // every one of them, with no falloff.
        //
        // OBSERVED: draw inside the loop over the creeps instead of once before
        // it. The fired count stays at one and the damaged count stays at
        // three, so nothing about the shape looks wrong; the three amounts stop
        // agreeing and the reconstruction goes red on the second body. A bubble
        // that rolls per body is a bubble whose cost in dice depends on how many
        // creeps happen to be standing there, and every stored record made
        // under it replays to a different match.
        TheMatch.EventLog log = Played(12, ThreeAbreast);

        Assert.Equal(1, log.CountOf("fired"));
        Assert.Equal(3, log.CountOf("damaged"));

        int[] amounts = Amounts(log);

        Assert.Equal(amounts[0], amounts[1]);
        Assert.Equal(amounts[0], amounts[2]);

        AssertLandingsAreTheStream(log, 12, 1);
    }

    [Fact]
    public void A_single_shot_is_the_same_call_with_room_for_one_answer()
    {
        // The control both tests above are read against: the same corridor, the
        // same three bodies and a row that authors neither shape hits exactly
        // one of them, once, for one draw.
        TheMatch.EventLog log = Played(10, ThreeAbreast);

        Assert.Equal(1, log.CountOf("fired"));
        Assert.Equal(1, log.CountOf("damaged"));

        AssertLandingsAreTheStream(log, 10, 1);
    }

    [Fact]
    public void A_blast_centred_on_the_target_arrives_where_the_shot_did()
    {
        // A mortar: one projectile at one creep, and the roll it was carrying
        // lands on everything within a radius of where it arrived. The draw
        // happens when the shot is released and not when it lands, which is
        // what the flight ticks between the two are here to separate.
        TheMatch.EventLog log = Played(13, ThreeAbreast, ticks: 10);

        Assert.Equal(1, log.CountOf("fired"));
        Assert.Equal(3, log.CountOf("damaged"));

        int[] amounts = Amounts(log);

        Assert.Equal(amounts[0], amounts[1]);
        Assert.Equal(amounts[0], amounts[2]);
    }

    [Fact]
    public void A_shield_absorbs_before_armour_is_consulted_and_overkill_reaches_health()
    {
        // The claim, as the one arithmetic that separates the two orderings. A
        // flat hundred-damage pierce shot against an Armoured body with forty
        // points of shield:
        //
        //   shield first, raw    (100 - 40) * 70 / 100 = 42
        //   armour first         100 * 70 / 100 - 40   = 30
        //
        // and the control with no shield at all is 70. Asserting 42 is
        // asserting the order; asserting it is not 70 is asserting the shield
        // did anything at all.
        //
        // OBSERVED: move the Absorbed call below Resolved in Match.Damage. This
        // goes red, 30 against 42, and nothing else in the suite notices --
        // because no committed row carries a shield.
        Assert.Equal(70, Amounts(Played(14, "order 0 1 1 0"))[0]);
        Assert.Equal(42, Amounts(Played(14, "order 0 4 1 0"))[0]);

        // And a shield the roll cannot get through stops the hit dead: no
        // damage event at all, because nothing reached health. The floor is
        // under hits that resolve through the matrix, and this one never
        // reached it -- a floor applied here would leak a point of health past
        // a pool that stopped the shot.
        TheMatch.EventLog stopped = Played(14, "order 0 5 1 0");

        Assert.Equal(1, stopped.CountOf("fired"));
        Assert.Equal(0, stopped.CountOf("damaged"));
    }

    [Fact]
    public void A_shield_is_spent_rather_than_refreshed_and_the_next_hit_goes_further()
    {
        // It does not regenerate, so the second shot meets a smaller pool than
        // the first and the third meets none. Two hundred points against a flat
        // hundred: the first two are swallowed whole and the third lands at
        // full strength.
        //
        // Three attacks rather than one, so the match has to be run past the
        // cooldown twice: they land on ticks 1, 32 and 63.
        TheMatch.EventLog log = Played(14, "order 0 5 1 0", ticks: 70);

        Assert.Equal(3, log.CountOf("fired"));
        Assert.Equal(1, log.CountOf("damaged"));
        Assert.Equal(70, Amounts(log)[0]);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    public void A_bubble_this_build_does_not_resolve_refuses_when_a_match_is_built_from_it(int typeId)
    {
        // A slow and an aura: both author perfectly, both fold into the content
        // hash, and neither plays. The refusal is at construction and by name,
        // because the alternative is a Cryomancer standing on the board firing
        // and slowing nothing with nothing anywhere saying so.
        //
        // This is the line #216 drew and #217 rubs out: the columns parse and
        // carry, and per-creep timed effect state is not half-built here.
        UnitTypeTable types = UnitTypeTable.Parse("shot shapes", TheFixtures);

        SimulationException thrown = Assert.Throws<SimulationException>(() => new Match(
            HexMap.Parse("shot shapes map", AShortCorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("shot shapes defense", "tower " + typeId + " 2 1", types),
            WaveScript.Parse("shot shapes wave", ThreeAbreast, types),
            Seed));

        Assert.Contains("one bubble shape", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// One tower of that type under the middle of the corridor, that wave, and
    /// everything it said happened.
    /// </summary>
    private static TheMatch.EventLog Played(int towerType, string wave, int ticks = OneAttack)
    {
        UnitTypeTable types = UnitTypeTable.Parse("shot shapes", TheFixtures);

        var match = new Match(
            HexMap.Parse("shot shapes map", AShortCorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("shot shapes defense", "tower " + towerType + " 2 1", types),
            WaveScript.Parse("shot shapes wave", wave, types),
            Seed);

        var log = new TheMatch.EventLog();
        match.Advance(ticks, log);

        return log;
    }

    /// <summary>What every landing in a log took off a health pool, in order.</summary>
    private static int[] Amounts(TheMatch.EventLog log) =>
        Enumerable.Range(0, log.Count)
            .Where(index => log.Kinds[index] == "damaged")
            .Select(index => log.Amounts[index])
            .ToArray();

    /// <summary>
    /// That the landings in this log are the first <paramref name="draws"/>
    /// numbers of the seeded stream, resolved through the ruleset. A draw added
    /// or skipped anywhere walks the two sequences apart and never recovers.
    /// </summary>
    private static void AssertLandingsAreTheStream(TheMatch.EventLog log, int towerType, int draws)
    {
        UnitType tower = UnitTypeTable.Parse("shot shapes", TheFixtures).ById(towerType);
        var dice = new Pcg32(Seed);
        var expected = new List<int>();

        for (int draw = 0; draw < draws; draw++)
        {
            int roll = dice.NextInRange(tower.DamageMin, tower.DamageMax + 1);

            expected.Add(DamageModel.Dealt(
                TheRuleset.Committed(),
                roll,
                0,
                tower.AttackType,
                ArmourType.Armoured,
                0));
        }

        int[] landed = Amounts(log);

        for (int index = 0; index < landed.Length; index++)
        {
            Assert.Contains(landed[index], expected);
        }

        // And in order where there is an order to be in: n shots land in the
        // order they were drawn, which is what makes an added draw visible on
        // the shot after it rather than only in the multiset.
        if (draws == landed.Length)
        {
            Assert.Equal(expected, landed);
        }
    }
}
