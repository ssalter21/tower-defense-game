namespace Sim.Tests;

/// <summary>
/// The authored defense, and the three questions that can only be asked once it
/// is standing on a map.
/// </summary>
public class DefenseTests
{
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
        Assert.Equal(4, layout.Towers[0].Type.Id);
        Assert.Equal(9, layout.Towers[0].Column);
        Assert.Equal(0, layout.Towers[0].Row);
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
        // Column 3, row 1 is corridor on the committed map. A tower there would
        // be a wall, and a wall is how mazing gets in.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TowerCoverage.For(TheMatch.Map(), TowerLayout.Parse("tower 3 3 1", TheMatch.Types())));

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
        HexMap small = HexMap.Parse("""
            SE...
            .....
            .....
            .....
            .....
            .....
            """);

        ContentException thrown = Assert.Throws<ContentException>(
            () => TowerCoverage.For(small, TowerLayout.Parse("tower 3 4 5", TheMatch.Types())));

        Assert.Contains("cannot reach", thrown.Message, StringComparison.Ordinal);
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
