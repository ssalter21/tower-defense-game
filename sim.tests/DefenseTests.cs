namespace Sim.Tests;

/// <summary>
/// The authored defense, and the three questions that can only be asked once it
/// is standing on a map.
/// </summary>
public class DefenseTests
{
    /// <summary>
    /// A map with somewhere to stand that nothing walks past. The committed
    /// corridor snakes through nearly the whole grid, so every cell of it is in
    /// range of some part of the route -- which is a fact about that map and
    /// not a reason to leave the reach question unobserved.
    /// </summary>
    private static readonly HexMap RoomToMissTheRoute = HexMap.Parse(TheGrid.OnTheFlat("""
        SE...
        .....
        .....
        .....
        .....
        .....
        """));

    private const string ThreeGoodTypes = """
        unit 1 grunt  moving 200 85 0 0 0 0 0 0 none 0 12
        unit 3 bolt   placed 0 0 3200 6 3 2 9 15 hitscan 0 0
        unit 4 mortar placed 0 0 4600 18 7 5 21 34 projectile 11 0
        """;

    [Fact]
    public void The_committed_defense_parses()
    {
        TowerLayout layout = TheMatch.Layout(TheMatch.Types());

        Assert.Equal(6, layout.Count);
        Assert.Equal(3, layout.Towers[0].Type.Id);
        Assert.Equal(4, layout.Towers[0].Column);
        Assert.Equal(3, layout.Towers[0].Row);
    }

    [Fact]
    public void Towers_out_of_canonical_order_refuse_to_load()
    {
        // Asserted, not sorted. Sorting would leave two identical defenses with
        // two different sets of bytes -- and it would also quietly change which
        // tower gets to fire first, which decides who lands a killing shot.
        ContentException thrown = Assert.Throws<ContentException>(() => TowerLayout.Parse("""
            tower 3 6 4
            tower 3 3 2
            """, UnitTypeTable.Parse(ThreeGoodTypes)));

        Assert.Contains("canonical order", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(2, thrown.Line);
    }

    [Fact]
    public void Two_towers_on_one_cell_refuse_to_load()
    {
        Assert.Throws<ContentException>(() => TowerLayout.Parse("""
            tower 3 3 2
            tower 4 3 2
            """, UnitTypeTable.Parse(ThreeGoodTypes)));
    }

    [Fact]
    public void A_defense_that_places_a_creep_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => TowerLayout.Parse("tower 1 3 2", UnitTypeTable.Parse(ThreeGoodTypes)));

        Assert.Contains("moving unit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_type_id_the_table_does_not_define_refuses_to_load()
    {
        Assert.Throws<ContentException>(
            () => TowerLayout.Parse("tower 9 3 2", UnitTypeTable.Parse(ThreeGoodTypes)));
    }

    [Fact]
    public void A_tower_standing_in_the_corridor_refuses_to_load()
    {
        // Column 4, row 1 is corridor on the committed map. A tower there would
        // be a wall, and a wall is how mazing gets in.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TowerCoverage.For(TheMatch.Map(), TowerLayout.Parse("tower 3 4 1", TheMatch.Types())));

        Assert.Contains("corridor cell", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tower_off_the_edge_of_the_map_refuses_to_load()
    {
        Assert.Throws<ContentException>(
            () => TowerCoverage.For(TheMatch.Map(), TowerLayout.Parse("tower 3 40 40", TheMatch.Types())));
    }

    [Fact]
    public void A_tower_that_cannot_reach_the_route_refuses_to_load()
    {
        // This is what a mistyped coordinate looks like. Without the check it
        // presents as a tower that never fires, which is indistinguishable from
        // a balance problem and gets tuned around instead of fixed.
        //
        // It takes a map of its own to demonstrate: the committed corridor
        // snakes through nearly the whole grid, so there is nowhere on it far
        // enough from the route to be out of range of all of it -- which is a
        // fact about that map and not a reason to leave the check unobserved.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TowerCoverage.For(RoomToMissTheRoute, TowerLayout.Parse("tower 3 4 5", TheMatch.Types())));

        Assert.Contains("cannot reach", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, thrown.Line);
    }

    [Fact]
    public void A_cell_answers_all_three_questions_with_no_file_and_no_line_involved()
    {
        UnitTypeTable types = TheMatch.Types();
        HexMap map = TheMatch.Map();
        UnitType bolt = types.ById(3);
        PlacedTower standing = TheMatch.Layout(types).Towers[0];

        // The same three questions the authored path asks, asked about a bare
        // column and row. Nothing here has a source file to point into, and the
        // answers do not invent one -- what comes back is a clause, and each
        // caller supplies its own subject and its own exception.
        Footing offMap = Footing.Of(map, bolt, 40, 40);
        Footing corridor = Footing.Of(map, bolt, 4, 1);
        Footing sound = Footing.Of(map, standing.Type, standing.Column, standing.Row);

        Assert.False(offMap.Possible);
        Assert.Contains("off a", offMap.Fault, StringComparison.Ordinal);

        Assert.False(corridor.Possible);
        Assert.Contains("corridor cell", corridor.Fault, StringComparison.Ordinal);

        Assert.True(sound.Sound);
        Assert.Equal(string.Empty, sound.Fault);
    }

    [Fact]
    public void Reaching_nothing_is_a_separate_answer_from_standing_nowhere()
    {
        // The split the whole predicate exists for. Column 4, row 5 is on the
        // grid and is ground -- a possible position -- and no part of the route
        // is within a bolt's three hexes of it. A caller reading a file refuses
        // that; a caller reading a player's decision may not.
        Footing footing = Footing.Of(RoomToMissTheRoute, TheMatch.Types().ById(3), 4, 5);

        Assert.True(footing.Possible);
        Assert.False(footing.ReachesRoute);
        Assert.False(footing.Sound);
        Assert.Contains("cannot reach", footing.Fault, StringComparison.Ordinal);
    }

    [Fact]
    public void A_defense_a_run_built_may_stand_somewhere_that_reaches_nothing()
    {
        // The same cell the authored file was refused for, arriving off a board
        // instead. A placement made at wave 4 was never in a file, so there is
        // no line to blame and no typo to suspect -- the player built somewhere
        // useless, which is a bad decision rather than an illegal one.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout built = Board.Empty
            .Place(types.ById(3), 4, 5)
            .Place(types.ById(3), 1, 1)
            .Layout();

        var coverage = TowerCoverage.For(RoomToMissTheRoute, built);

        Assert.Equal(0, coverage.IntervalCount(1));
        Assert.False(coverage.Covers(1, Fix64.Zero));
        Assert.False(coverage.Overlaps(0, 1));

        // And the placement beside it is unaffected: reaching nothing is one
        // tower's problem and not the defense's.
        Assert.True(coverage.IntervalCount(0) > 0);
    }

    [Fact]
    public void A_defense_a_run_built_still_cannot_stand_off_the_map_or_in_the_corridor()
    {
        UnitTypeTable types = TheMatch.Types();

        // Zero, because a placement carries no line. The refusal survives the
        // absence of one; only the reason changes.
        ContentException corridor = Assert.Throws<ContentException>(
            () => TowerCoverage.For(TheMatch.Map(), Board.Empty.Place(types.ById(3), 4, 1).Layout()));

        Assert.Contains("corridor cell", corridor.Message, StringComparison.Ordinal);
        Assert.Equal(0, corridor.Line);

        ContentException offMap = Assert.Throws<ContentException>(
            () => TowerCoverage.For(TheMatch.Map(), Board.Empty.Place(types.ById(3), 40, 40).Layout()));

        Assert.Contains("off a", offMap.Message, StringComparison.Ordinal);
        Assert.Equal(0, offMap.Line);
    }

    [Fact]
    public void Every_tower_of_the_committed_defense_reaches_the_route()
    {
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);
        var coverage = TowerCoverage.For(TheMatch.Map(), layout);

        for (int tower = 0; tower < layout.Count; tower++)
        {
            Assert.True(coverage.IntervalCount(tower) > 0, $"Tower {tower + 1} reaches nothing.");

            for (int interval = 0; interval < coverage.IntervalCount(tower); interval++)
            {
                Assert.True(coverage.IntervalStart(tower, interval) <= coverage.IntervalEnd(tower, interval));
            }
        }
    }

    [Fact]
    public void Every_tower_shares_a_stretch_of_corridor_with_another()
    {
        // The overlap is the point. A defense whose towers each had the corridor
        // to themselves would never put two of them on one creep on one tick, so
        // overkill and iteration order would stay hypothetical -- and this is
        // the assertion that stops that being true by accident after a retune.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);
        var coverage = TowerCoverage.For(TheMatch.Map(), layout);

        for (int tower = 0; tower < layout.Count; tower++)
        {
            bool shares = false;

            for (int other = 0; other < layout.Count; other++)
            {
                if (other != tower && coverage.Overlaps(tower, other))
                {
                    shares = true;
                }
            }

            Assert.True(shares, $"Tower {tower + 1} has its stretch of corridor entirely to itself.");
        }
    }

    [Fact]
    public void A_range_question_is_an_interval_test_and_the_intervals_are_where_the_route_is()
    {
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);
        var coverage = TowerCoverage.For(TheMatch.Map(), layout);

        // Whatever the first tower's first stretch is, a point inside it is in
        // range and a point a whole hex before it is not. Two comparisons on a
        // line; nothing here has a position in a plane.
        Fix64 start = coverage.IntervalStart(0, 0);
        Assert.True(coverage.Covers(0, start));
        Assert.True(coverage.Covers(0, coverage.IntervalEnd(0, 0)));
        Assert.False(coverage.Covers(0, start - Fix64.One));
    }
}
