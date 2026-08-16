namespace Sim.Tests;

/// <summary>
/// The map: the grid, the corridor assertion, the map hash, and the odd-r
/// conversion the whole thing is computed in.
/// </summary>
public class HexMapTests
{
    /// <summary>
    /// A three-cell corridor with room around it, and the level grid every map
    /// carries under its terrain. Small enough to reason about by hand, which
    /// is what makes the malformed variants below trustworthy.
    /// </summary>
    /// <remarks>
    /// Written out in full, both blocks, because this is the fixture the byte
    /// order is asserted against and the second plane is half of what those
    /// bytes are. The grids below use <see cref="TheGrid.OnTheFlat"/> instead:
    /// what each of them is about is the shape of a corridor, and a block of
    /// letters under one would bury it.
    /// </remarks>
    private const string Straight = """
        .....
        .S#E.
        .....

        aaaaa
        aaaaa
        aaaaa
        """;

    [Fact]
    public void The_committed_map_loads_and_its_corridor_is_one_hex_wide()
    {
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

        Assert.Equal(15, map.Width);
        Assert.Equal(9, map.Height);
        Assert.Equal(47, map.Route.Count);
        Assert.Equal(Hex.FromOddRowOffset(1, 1), map.Spawn);
        Assert.Equal(Hex.FromOddRowOffset(1, 7), map.Exit);
    }

    [Fact]
    public void Every_step_of_the_route_is_adjacent_to_the_one_before_it()
    {
        // The corridor assertion says the walk exists. This says the walk is a
        // walk: consecutive route entries are one hex apart, so distance along
        // the path is a count of steps and nothing downstream needs to search.
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

        for (int index = 1; index < map.Route.Count; index++)
        {
            Assert.Equal(1, map.Route[index - 1].DistanceTo(map.Route[index]));
        }
    }

    [Fact]
    public void The_route_visits_no_hex_twice()
    {
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

        Assert.Equal(map.Route.Count, map.Route.Distinct().Count());
    }

    [Fact]
    public void A_branching_corridor_refuses_to_load()
    {
        // The T junction: the middle cell of the top run also has a neighbour
        // below it, so it has three corridor neighbours where a corridor one
        // hex wide allows two.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .......
            .S###E.
            ...#...
            ...#...
            """)));

        Assert.Contains("branches", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_two_hex_wide_stretch_refuses_to_load()
    {
        // Nothing here is a junction to look at; it is simply too wide. The
        // degree test catches it for the same reason it catches the T, which is
        // why "one hex wide" and "never branches" are one check rather than two.
        Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            ......
            .S##..
            .###..
            ...E..
            """)));
    }

    [Fact]
    public void A_dead_end_that_is_not_an_end_refuses_to_load()
    {
        // A second stretch of corridor, off on its own. Nothing about it is a
        // branch, but it has two ends and the map only marks two ends in total.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .......
            .S#E...
            .......
            .......
            .......
            .##....
            .......
            """)));

        Assert.Contains("dead end", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ring_that_the_route_never_reaches_refuses_to_load()
    {
        // Three mutually adjacent hexes. Every one of them has exactly two
        // corridor neighbours, so the degree test is satisfied and so is the
        // dead-end test -- a ring has no ends. Only the walk catches it: it
        // reaches the exit having visited three of the six corridor cells, and
        // the other three are a second thing calling itself the route.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .......
            .S#E...
            .......
            .......
            .......
            .##....
            ..#....
            .......
            """)));

        Assert.Contains("visits only", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_map_with_no_entrance_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .....
            .##E.
            .....
            """)));
    }

    [Fact]
    public void A_map_with_two_exits_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .....
            .S#E.
            ...E.
            """)));
    }

    [Fact]
    public void A_ragged_grid_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .....
            .S#E
            .....
            """)));
    }

    [Fact]
    public void An_unknown_character_refuses_to_load_and_a_digit_is_one()
    {
        // The map file holds no numbers at all, which is how it stays outside
        // the decimal-point question entirely rather than needing its own
        // version of that rule.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            .....
            .S#E.
            ..7..
            """)));

        Assert.Contains("'7'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, thrown.Line);
    }

    [Fact]
    public void A_comment_names_the_right_line_afterwards()
    {
        // The grid does not start at line one once the map carries a legend, so
        // a message that counted grid rows would point a person at the wrong
        // line of their editor.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse(TheGrid.OnTheFlat("""
            // a legend
            // spanning two lines

            .....
            .S#E.
            ..7..
            """)));

        Assert.Equal(6, thrown.Line);
    }

    [Fact]
    public void Reformatting_the_map_file_does_not_move_the_map_hash()
    {
        string original = File.ReadAllText(RepoLayout.MapFile);
        Hash64 hash = HexMap.Parse(original).MapHash;

        Assert.Equal(hash, HexMap.Parse(original.Replace("\r\n", "\n")).MapHash);
        Assert.Equal(hash, HexMap.Parse(original.Replace("\n", "\r\n")).MapHash);
        Assert.Equal(hash, HexMap.Parse(original + "\n\n").MapHash);
        Assert.Equal(hash, HexMap.Parse("// rewritten legend\n\n" + StripComments(original)).MapHash);
    }

    [Fact]
    public void Nudging_one_hex_moves_the_map_hash()
    {
        // The reason the hash is over the parsed grid: a map that arrived from
        // somewhere else, or was generated, or was edited by hand, is pinned by
        // what it means rather than by how it was written down.
        HexMap moved = HexMap.Parse(TheGrid.OnTheFlat("""
            .....
            .S#E.
            .....
            """));

        HexMap elsewhere = HexMap.Parse(TheGrid.OnTheFlat("""
            .....
            .....
            .S#E.
            """));

        Assert.NotEqual(moved.MapHash, elsewhere.MapHash);
    }

    [Fact]
    public void A_grid_of_the_same_cells_at_a_different_shape_hashes_differently()
    {
        Assert.NotEqual(
            HexMap.Parse(TheGrid.OnTheFlat("S#E\n...\n")).MapHash,
            HexMap.Parse(TheGrid.OnTheFlat("S#E...")).MapHash);
    }

    [Fact]
    public void The_cell_bytes_are_the_grid_row_major()
    {
        HexMap map = HexMap.Parse(Straight);
        byte[] bytes = map.ToCellBytes();

        Assert.Equal(15, bytes.Length);
        Assert.Equal((byte)MapCell.Spawn, bytes[6]);
        Assert.Equal((byte)MapCell.Route, bytes[7]);
        Assert.Equal((byte)MapCell.Exit, bytes[8]);
    }

    [Fact]
    public void A_map_that_stops_after_its_terrain_is_refused_and_the_message_names_what_is_missing()
    {
        // The refusal that used to be the other way round: a second block was
        // the fault and is now the requirement. A person who has just typed a
        // grid and pressed save is the one who sees this, so the message says
        // what to type rather than that something is wrong.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse("""
            .....
            .S#E.
            .....
            """));

        Assert.Contains("level grid", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("missing", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_third_block_of_rows_refuses_to_load()
    {
        // What the old refusal was actually protecting -- a stray edit, or a
        // second map nothing will ever read -- moved along by one block rather
        // than going away.
        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aaaaa
            aaaaa

            bbbbb
            bbbbb
            bbbbb
            """));

        Assert.Contains("third block", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_level_grid_of_another_shape_refuses_to_load()
    {
        // Both halves of "the same shape": too few rows, and a row too narrow.
        // Either one leaves a hex whose height nothing states, and there is no
        // height a reader could supply for it.
        Assert.Throws<ContentException>(() => HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aaaaa
            """));

        ContentException narrow = Assert.Throws<ContentException>(() => HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aaaa
            aaaaa
            """));

        Assert.Equal(6, narrow.Line);
    }

    [Fact]
    public void A_level_outside_the_three_tiers_refuses_to_load_and_a_digit_is_one()
    {
        // The level grid holds no numbers either, which is the half of that
        // rule a second block could quietly have lost. 'd' is the other half:
        // there are three tiers and a fourth is not flattened onto the third.
        ContentException tier = Assert.Throws<ContentException>(() => HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aadaa
            aaaaa
            """));

        Assert.Contains("'d'", tier.Message, StringComparison.Ordinal);
        Assert.Equal(6, tier.Line);

        ContentException digit = Assert.Throws<ContentException>(() => HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aa1aa
            aaaaa
            """));

        Assert.Contains("'1'", digit.Message, StringComparison.Ordinal);
        Assert.Contains("this file holds no numbers", digit.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Raising_one_hex_a_tier_moves_the_map_hash()
    {
        // The property the whole layer rests on: a map is a seed, and two maps
        // with the same corridor at different heights are two maps. Without
        // this, a defense recorded on a fold would replay on the flat with
        // every gate agreeing.
        //
        // OBSERVED: delete the level loop from HexMap.Fold. Both maps come back
        // 3C99A5296C6A1B60 and the assertion goes red -- which is the shape of
        // a hash that pins terrain and calls it geometry.
        HexMap flat = HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aaaaa
            aaaaa
            """);

        HexMap raised = HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            aabaa
            aaaaa
            """);

        Assert.Equal(flat.ToCellBytes(), raised.ToCellBytes());
        Assert.NotEqual(flat.MapHash, raised.MapHash);
    }

    [Fact]
    public void The_level_bytes_are_the_second_grid_row_major()
    {
        HexMap map = HexMap.Parse("""
            .....
            .S#E.
            .....

            aaaaa
            abcaa
            aaaaa
            """);

        byte[] levels = map.ToLevelBytes();

        Assert.Equal(15, levels.Length);
        Assert.Equal(0, levels[5]);
        Assert.Equal(1, levels[6]);
        Assert.Equal(2, levels[7]);

        Assert.Equal(1, map.LevelAt(1, 1));
        Assert.Equal(2, map.LevelAt(2, 1));
        Assert.Equal(0, map.LevelAt(2, 2));
    }

    [Fact]
    public void The_committed_map_stands_entirely_on_the_ground_tier()
    {
        // The map this skeleton has always been played on is flat, and it stays
        // flat until somebody draws a folded one. A test rather than a comment,
        // because every number priced against this board was measured on the
        // flat and a tier appearing here would move all of them at once.
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                Assert.Equal(0, map.LevelAt(column, row));
            }
        }
    }

    [Fact]
    public void Indenting_a_row_moves_nothing()
    {
        // Odd rows are indented in the committed file so that what is typed
        // looks like the odd-r board it produces. The indent is decoration, and
        // a hash that moved with it would retire every stored record for a
        // whitespace edit.
        HexMap map = HexMap.Parse(Straight);

        Assert.Equal(
            map.MapHash,
            HexMap.Parse("""
                .....
                 .S#E.
                .....

                aaaaa
                 aaaaa
                aaaaa
                """).MapHash);
    }

    [Theory]
    // Odd-r: the ODD rows are the shifted ones, so row 0 and row 1 share a
    // column origin and row 2 has slid one column of q to the left.
    [InlineData(0, 0, 0, 0)]
    [InlineData(3, 0, 3, 0)]
    [InlineData(3, 1, 3, 1)]
    [InlineData(3, 2, 2, 2)]
    [InlineData(3, 3, 2, 3)]
    [InlineData(3, 4, 1, 4)]
    public void Odd_row_offset_converts_to_axial_the_one_canonical_way(int column, int row, int q, int r)
    {
        Hex hex = Hex.FromOddRowOffset(column, row);

        Assert.Equal(q, hex.Q);
        Assert.Equal(r, hex.R);
    }

    [Fact]
    public void The_offset_conversion_round_trips()
    {
        for (int row = 0; row < 12; row++)
        {
            for (int column = 0; column < 12; column++)
            {
                Hex.ToOddRowOffset(Hex.FromOddRowOffset(column, row), out int back, out int backRow);

                Assert.Equal(column, back);
                Assert.Equal(row, backRow);
            }
        }
    }

    [Fact]
    public void The_cube_coordinate_is_derived_and_sums_to_zero()
    {
        // Nothing stores it, which is the point: two fields that must agree are
        // two fields that can disagree.
        for (int r = -8; r <= 8; r++)
        {
            for (int q = -8; q <= 8; q++)
            {
                var hex = new Hex(q, r);

                Assert.Equal(0, hex.CubeX + hex.CubeY + hex.CubeZ);
            }
        }
    }

    [Fact]
    public void Every_neighbour_is_one_step_away_and_there_are_six_of_them()
    {
        var hex = new Hex(4, -2);
        var seen = new List<Hex>();

        for (int direction = 0; direction < Hex.DirectionCount; direction++)
        {
            Hex neighbour = hex.Neighbour(direction);

            Assert.Equal(1, hex.DistanceTo(neighbour));
            seen.Add(neighbour);
        }

        Assert.Equal(6, seen.Distinct().Count());
    }

    [Fact]
    public void A_hex_that_does_not_fit_in_two_shorts_refuses_to_exist()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Hex(40000, 0));
    }

    private static string StripComments(string text) =>
        string.Join(
            "\n",
            text.Split('\n').Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal)));
}
