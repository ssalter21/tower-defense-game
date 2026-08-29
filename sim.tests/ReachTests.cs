namespace Sim.Tests;

/// <summary>
/// What a level does to a range: the signed difference a shot is measured
/// under, the sphere a radius is, the floor under both, and the two things
/// elevation deliberately does not touch.
/// </summary>
/// <remarks>
/// <para>
/// <b>The arithmetic is asserted at the boundary and nowhere else.</b> A block
/// of height is two levels and is worth half a hex, and distances are whole
/// hexes, so the only ranges where half a hex is visible at all are the ones
/// whose boundary the half hex crosses. Every range constant below is chosen
/// for that and is written down beside the crossing it makes: a test at a range
/// where the half hex changes no answer would pass under a rule that ignored
/// height entirely.
/// </para>
/// <para>
/// <b>The levels here are even numbers because a block is two of them.</b>
/// When a level was a whole block these fixtures read a, b, c; they now read
/// a, c, e, and every assertion in this file is measuring exactly what it
/// measured before. An odd level is half a block and worth a quarter hex,
/// which is a quantity no boundary in this file crosses -- that is deliberate,
/// and it is why the half step could be added without any of these numbers
/// moving.
/// </para>
/// <para>
/// <b>The committed map is on the flat, so none of this is observable on
/// it.</b> Every map here is a fixture written for the claim it is under. The
/// folded board is drawn by hand later, and a rule that waited for it would
/// arrive untested.
/// </para>
/// </remarks>
public class ReachTests
{
    /// <summary>
    /// A straight nine-hex corridor with a room under it, and one hex of that
    /// corridor standing two blocks up: the ridge.
    /// </summary>
    /// <remarks>
    /// The tower stands at column 4 of row 2, which is two hexes from route
    /// cells 3, 4 and 5 and three from everything else -- so at a range of 2600
    /// its reach on the flat is exactly those three cells, in one run. Raising
    /// the middle of the three by two blocks costs a full hex, which takes it to
    /// 3000 and out, and leaves the two either side of it untouched. The hole
    /// is in the middle of a run of route rather than at its end, which is what
    /// makes it a second interval rather than a shorter first one.
    /// </remarks>
    private const string RidgeAcrossTheCorridor = """
        S#######E
        .........
        .........

        aaaaeaaaa
        aaaaaaaaa
        aaaaaaaaa
        """;

    /// <summary>The same board with the ridge flattened, and nothing else changed.</summary>
    private const string TheSameBoardOnTheFlat = """
        S#######E
        .........
        .........

        aaaaaaaaa
        aaaaaaaaa
        aaaaaaaaa
        """;

    /// <summary>
    /// The same board again as a staircase: the corridor climbs two whole blocks
    /// on the way to the exit.
    /// </summary>
    private const string TheSameBoardAsAStaircase = """
        S#######E
        .........
        .........

        aaaccceee
        aaaccceee
        aaaccceee
        """;

    /// <summary>
    /// A corridor with a tower cell directly beside it, two blocks down. Adjacent
    /// and below is the case the floor exists for: without it a soldier here
    /// has an effective range of half a hex and cannot hit the creep he is
    /// touching.
    /// </summary>
    private const string ACellBesideTheCorridorAndBelowIt = """
        S##E
        ....

        eeee
        aaaa
        """;

    /// <summary>
    /// A tower at 2600 milli-hexes -- two hexes and a bit -- and a walker to
    /// send past it. The range is what the ridge fixture is measured against
    /// and is a fixture number rather than a roster one.
    /// </summary>
    private const string AWalkerAndATowerAt2600 = """
        unit 1 walker moving 100 27 0 0 0 0 0 0 none 0 4
        unit 3 bolt   placed 0 0 2600 6 3 2 9 15 hitscan 0 0
        """;

    /// <summary>A tower at exactly one hex, which is the shortest range the roster authors.</summary>
    private const string AWalkerAndATowerAtOneHex = """
        unit 1 walker moving 100 27 0 0 0 0 0 0 none 0 4
        unit 3 bolt   placed 0 0 1000 6 3 2 9 15 hitscan 0 0
        """;

    private const string ThreeWalkers = "order 0 1 3 0";

    /// <summary>
    /// How long the ridge match is watched for. Short of the tick the three
    /// walkers reach the exit on, so the loop that watches the coverage is
    /// bounded by the count rather than by the match ending under it.
    /// </summary>
    private const int TicksTheMatchOutlasts = 200;

    private static readonly Hex Origin = new Hex(0, 0);

    [Fact]
    public void A_tower_one_block_above_its_target_reaches_half_a_hex_further()
    {
        // Two and a half hexes: three hexes is out on the flat and the half hex
        // bought by shooting down one block is exactly what closes it. At a
        // range of 2000 or 3000 the same shot answers the same either way, and
        // the test would pass under a rule that never read a level.
        const int TwoAndAHalfHexes = 2500;

        Assert.False(Reach.Shoots(Origin, 0, TwoAndAHalfHexes, Away(3), 0));
        Assert.True(Reach.Shoots(Origin, 2, TwoAndAHalfHexes, Away(3), 0));

        // And the half step in between buys a quarter hex, which does not close
        // it. This is the assertion that says a level is half of what it was:
        // under the old rule the line below reached, and the one above it is
        // the height that reaches now.
        Assert.False(Reach.Shoots(Origin, 1, TwoAndAHalfHexes, Away(3), 0));

        // And it is a difference and not a bonus for standing high. The same
        // tower on the top of the board shooting a target up there with it
        // reaches exactly as far as it did on the ground, which is the claim a
        // flat per-level bonus would fail.
        for (int hexes = 0; hexes <= 5; hexes++)
        {
            Assert.Equal(
                Reach.Shoots(Origin, 0, TwoAndAHalfHexes, Away(hexes), 0),
                Reach.Shoots(Origin, 4, TwoAndAHalfHexes, Away(hexes), 4));
        }
    }

    [Fact]
    public void A_tower_one_block_below_its_target_reaches_half_a_hex_less()
    {
        // Two hexes exactly: the target two hexes away is in range on the flat
        // and the half hex charged for shooting up one tier is what takes it
        // out. Two tiers up charges a whole hex, which leaves this tower
        // reaching one -- and the last line is the arithmetic saying so rather
        // than the floor, which is under it either way.
        const int TwoHexes = 2000;

        Assert.True(Reach.Shoots(Origin, 0, TwoHexes, Away(2), 0));
        Assert.False(Reach.Shoots(Origin, 0, TwoHexes, Away(2), 2));
        Assert.False(Reach.Shoots(Origin, 0, TwoHexes, Away(2), 4));
        Assert.True(Reach.Shoots(Origin, 0, TwoHexes, Away(1), 4));

        // Half a block up costs a quarter hex, and a quarter hex is enough to
        // put a shot that landed exactly on the boundary outside it.
        Assert.False(Reach.Shoots(Origin, 0, TwoHexes, Away(2), 1));
    }

    [Fact]
    public void A_radius_is_a_sphere_and_height_costs_it_in_both_directions()
    {
        // The difference between the two rules, stated as the one case that
        // separates them: a bubble centred a tier above a cell two hexes away
        // does not reach it, and neither does one centred a tier below, while a
        // shot taken from the tier above reaches it comfortably. A radius that
        // shared the shot's signed term would blanket the board from a cliff.
        const int TwoHexes = 2000;

        Assert.True(Reach.Encloses(Origin, 2, TwoHexes, Away(2), 2));
        Assert.False(Reach.Encloses(Origin, 2, TwoHexes, Away(2), 0));
        Assert.False(Reach.Encloses(Origin, 2, TwoHexes, Away(2), 4));

        Assert.True(Reach.Shoots(Origin, 2, TwoHexes, Away(2), 0));
    }

    [Fact]
    public void Any_reach_at_all_reaches_the_six_hexes_touching_it_whatever_the_levels_do()
    {
        // The floor, over every neighbour and every pair of tiers there is. One
        // thousandth of a hex is the smallest range that is a range at all, so
        // every one of these but the downhill shots is the floor answering: on
        // the arithmetic alone a hex level with this one already costs a
        // thousand times the range, and each tier climbed costs half that
        // again.
        //
        // The second hex out is what says the floor is the hexes touching it
        // and not a rule that swallowed the range column. It is out at every
        // pair of levels but one, downhill included, because a refund is a
        // quarter hex a level and it is two hexes away.
        //
        // THE ONE PAIR IS THE CEILING, AND IT IS NEW. Nine levels is four
        // blocks, so the deepest drop on the board refunds two whole hexes and
        // a shot with any range at all reaches the second hex out on the refund
        // alone. Under three levels the most a refund could ever buy was one
        // hex and this held for every pair. It is asserted below rather than
        // stepped around: the ceiling on what height is worth is now two hexes,
        // and that is a fact about the board somebody should have to delete a
        // line to change.
        const int TheDeepestDrop = HexMap.LevelCount - 1;
        for (int direction = 0; direction < Hex.DirectionCount; direction++)
        {
            Hex neighbour = Origin.Neighbour(direction);
            Hex beyond = neighbour.Neighbour(direction);

            for (int standing = 0; standing < HexMap.LevelCount; standing++)
            {
                for (int target = 0; target < HexMap.LevelCount; target++)
                {
                    Assert.True(Reach.Shoots(Origin, standing, 1, neighbour, target));
                    Assert.True(Reach.Encloses(Origin, standing, 1, neighbour, target));

                    if (standing - target < TheDeepestDrop)
                    {
                        Assert.False(Reach.Shoots(Origin, standing, 1, beyond, target));
                    }

                    // The sphere never refunds, so the ceiling does not apply
                    // to it and the second hex is out at every pair.
                    Assert.False(Reach.Encloses(Origin, standing, 1, beyond, target));

                    // No range is not a short range. A creep carries zero in the
                    // range column and reaching its neighbours would make every
                    // walking row a tower.
                    Assert.False(Reach.Shoots(Origin, standing, 0, neighbour, target));
                    Assert.False(Reach.Encloses(Origin, standing, 0, neighbour, target));
                }
            }

            // The ceiling itself, named. Shooting from the top of the board at
            // the bottom of it refunds exactly two hexes, which is the most
            // height can ever be worth here.
            Assert.True(Reach.Shoots(Origin, TheDeepestDrop, 1, beyond, 0));

            // And zero reaches nothing at all rather than reaching only itself,
            // which the flat arithmetic used to grant it.
            Assert.False(Reach.Shoots(Origin, 0, 0, Origin, 0));
            Assert.False(Reach.Encloses(Origin, 0, 0, Origin, 0));
        }
    }

    [Fact]
    public void A_tower_below_the_corridor_beside_it_still_covers_the_hex_it_is_touching()
    {
        // The floor through the whole stack rather than through the arithmetic:
        // a one-hex tower on the ground under a corridor two blocks above
        // it. The climb costs a whole hex, so the signed difference alone would
        // leave this tower unable to reach the route at all -- which would be
        // refused at load, in a file, as a mistyped coordinate.
        UnitTypeTable types = UnitTypeTable.Parse("reach units", AWalkerAndATowerAtOneHex);
        HexMap map = HexMap.Parse("reach map", ACellBesideTheCorridorAndBelowIt);

        Footing footing = Footing.Of(map, types.ById(3), 1, 1);

        Assert.True(footing.Sound);

        TowerCoverage coverage = TowerCoverage.For(map, TowerLayout.Parse("tower 3 1 1", types));

        Assert.True(coverage.Covers(0, Fix64.FromInt(1)));
    }

    [Fact]
    public void A_ridge_splits_a_towers_coverage_into_two_intervals_and_both_are_covered()
    {
        UnitTypeTable types = UnitTypeTable.Parse("reach units", AWalkerAndATowerAt2600);
        string defense = "tower 3 4 2";

        // On the flat the three cells within two hexes are consecutive route,
        // so they are one run.
        TowerCoverage flat = TowerCoverage.For(
            HexMap.Parse("flat map", TheSameBoardOnTheFlat),
            TowerLayout.Parse("reach defense", defense, types));

        Assert.Equal(1, flat.IntervalCount(0));
        Assert.Equal(Fix64.FromInt(3), flat.IntervalStart(0, 0));
        Assert.Equal(Fix64.FromInt(5), flat.IntervalEnd(0, 0));

        // Raise the middle one two blocks and the run is cut in half, with the
        // cells either side of the ridge untouched.
        TowerCoverage ridged = TowerCoverage.For(
            HexMap.Parse("ridge map", RidgeAcrossTheCorridor),
            TowerLayout.Parse("reach defense", defense, types));

        Assert.Equal(2, ridged.IntervalCount(0));
        Assert.Equal(Fix64.FromInt(3), ridged.IntervalStart(0, 0));
        Assert.Equal(Fix64.FromInt(3), ridged.IntervalEnd(0, 0));
        Assert.Equal(Fix64.FromInt(5), ridged.IntervalStart(0, 1));
        Assert.Equal(Fix64.FromInt(5), ridged.IntervalEnd(0, 1));

        Assert.True(ridged.Covers(0, Fix64.FromInt(3)));
        Assert.False(ridged.Covers(0, Fix64.FromInt(4)));
        Assert.True(ridged.Covers(0, Fix64.FromInt(5)));
    }

    [Fact]
    public void A_ridged_towers_coverage_is_computed_at_load_and_does_not_move_while_the_match_runs()
    {
        // The property the whole design was priced on: a level is read per
        // route cell when the coverage is built, and the tick loop is handed
        // intervals of distance it never recomputes. What a tick asks is
        // Covers(tower, distance) -- one number on a line -- and the answer to
        // it is the same on tick 400 as it was before the first tick ran.
        UnitTypeTable types = UnitTypeTable.Parse("reach units", AWalkerAndATowerAt2600);
        var match = new Match(
            HexMap.Parse("ridge map", RidgeAcrossTheCorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("reach defense", "tower 3 4 2", types),
            WaveScript.Parse("reach wave", ThreeWalkers, types),
            20260816UL);

        string[] atLoad = Spelling(match.Coverage);

        for (int tick = 0; tick < TicksTheMatchOutlasts && !match.IsFinished; tick++)
        {
            match.Advance(1);

            Assert.Equal(atLoad, Spelling(match.Coverage));
            Assert.False(match.Coverage.Covers(0, Fix64.FromInt(4)));
        }

        // The loop is bounded by the match ending as well as by the count, so
        // this is what says the whole of it actually ran. A match that finished
        // early would leave the assertions above unreached and the test green,
        // which is the shape of a coverage test that stopped covering.
        Assert.False(match.IsFinished);
        Assert.Equal(2, match.Coverage.IntervalCount(0));
    }

    [Fact]
    public void A_creep_walks_a_staircase_in_exactly_the_time_it_walks_the_flat()
    {
        // Considered and rejected: creeps do not slow going uphill. The two
        // boards below are the same corridor and the same wave with nothing
        // standing on either, and the only difference between them is that one
        // of them climbs from the ground tier to the top one. A rule that
        // charged for the climb would show up here as a later final tick.
        UnitTypeTable types = UnitTypeTable.Parse("reach units", AWalkerAndATowerAt2600);
        WaveScript wave = WaveScript.Parse("reach wave", ThreeWalkers, types);

        MatchResult flat = PlayedOut(TheSameBoardOnTheFlat, types, wave);
        MatchResult climbed = PlayedOut(TheSameBoardAsAStaircase, types, wave);

        Assert.Equal(flat.FinalTick, climbed.FinalTick);
        Assert.Equal(flat.Leaked, climbed.Leaked);
        Assert.Equal(flat.Total, climbed.Total);
    }

    /// <summary>A hex some whole number of hexes away from <see cref="Origin"/>.</summary>
    private static Hex Away(int hexes) => new Hex(hexes, 0);

    /// <summary>
    /// A coverage written out, so two of them can be compared as one value.
    /// Everything the tick loop can ask it, and nothing else.
    /// </summary>
    private static string[] Spelling(TowerCoverage coverage) =>
        Enumerable
            .Range(0, coverage.TowerCount)
            .SelectMany(tower => Enumerable
                .Range(0, coverage.IntervalCount(tower))
                .Select(index =>
                    $"{tower} {coverage.IntervalStart(tower, index)} {coverage.IntervalEnd(tower, index)}"))
            .ToArray();

    /// <summary>The wave sent down a board with nothing built on it, to the end.</summary>
    private static MatchResult PlayedOut(string map, UnitTypeTable types, WaveScript wave)
    {
        var match = new Match(
            HexMap.Parse("reach map", map),
            TheRuleset.Committed(),
            Board.Empty.Layout(),
            wave,
            20260816UL);

        while (!match.IsFinished)
        {
            match.Advance(1);
        }

        return match.Result();
    }
}
