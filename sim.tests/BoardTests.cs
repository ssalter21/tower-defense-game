namespace Sim.Tests;

/// <summary>
/// The board: what a run holds, and the one place placement order becomes
/// canonical order.
/// </summary>
/// <remarks>
/// Nothing is wired to this yet. What is asserted here is the type's own
/// arithmetic -- the ordinals, the derivation, and the two positions an empty
/// board has to be legal in -- because every later ticket in this effort reads
/// them rather than restating them.
/// </remarks>
public class BoardTests
{
    /// <summary>The committed bolt: hitscan, three hexes.</summary>
    private static UnitType Bolt(UnitTypeTable types) => types.ById(3);

    /// <summary>The committed mortar: projectile, four hexes.</summary>
    private static UnitType Mortar(UnitTypeTable types) => types.ById(4);

    [Fact]
    public void A_placement_carries_the_ordinal_of_the_place_that_made_it()
    {
        UnitTypeTable types = TheMatch.Types();

        Board board = Board.Empty
            .Place(Bolt(types), 6, 4)
            .Place(Mortar(types), 3, 2)
            .Place(Bolt(types), 10, 8);

        // Placement order, not canonical order: row 4 was built first and is
        // still first here.
        Assert.Equal(3, board.Count);
        Assert.Equal(new[] { 1, 2, 3 }, board.Placements.Select(placement => placement.Id));
        Assert.Equal(new[] { 4, 2, 8 }, board.Placements.Select(placement => placement.Row));
    }

    [Fact]
    public void An_upgrade_keeps_the_id_and_swaps_the_type()
    {
        UnitTypeTable types = TheMatch.Types();

        Board board = Board.Empty
            .Place(Bolt(types), 6, 4)
            .Place(Bolt(types), 3, 2)
            .Upgrade(Mortar(types), 6, 4)
            .Place(Bolt(types), 10, 8);

        // The counter counts places. Were the upgrade minting an id, the third
        // place would be 4 and the placement it climbed would have lost its
        // name.
        Assert.Equal(3, board.Count);
        Assert.Equal(new[] { 1, 2, 3 }, board.Placements.Select(placement => placement.Id));
        Assert.Equal(1, board.Placements[0].Id);
        Assert.Equal(4, board.Placements[0].Type.Id);
        Assert.Equal(6, board.Placements[0].Column);
        Assert.Equal(4, board.Placements[0].Row);
    }

    [Fact]
    public void A_derived_layout_ascends_by_row_and_then_by_column()
    {
        UnitTypeTable types = TheMatch.Types();

        TowerLayout layout = Board.Empty
            .Place(Bolt(types), 10, 8)
            .Place(Bolt(types), 12, 4)
            .Place(Mortar(types), 9, 0)
            .Place(Bolt(types), 6, 4)
            .Layout();

        Assert.Equal(new[] { 0, 4, 4, 8 }, layout.Towers.Select(tower => tower.Row));
        Assert.Equal(new[] { 9, 6, 12, 10 }, layout.Towers.Select(tower => tower.Column));
    }

    [Fact]
    public void The_same_two_placements_in_either_sequence_derive_the_same_layout()
    {
        // The sort is the seam, and this is what it buys: two boards that are
        // different runs -- the ordinals differ -- are one position.
        UnitTypeTable types = TheMatch.Types();

        Board built = Board.Empty.Place(Bolt(types), 6, 4).Place(Mortar(types), 3, 2);
        Board other = Board.Empty.Place(Mortar(types), 3, 2).Place(Bolt(types), 6, 4);

        Assert.NotEqual(built.Placements[0].Id, other.Placements.First(placement => placement.Column == 6).Id);
        Assert.Equal(Spelling(built.Layout()), Spelling(other.Layout()));
    }

    [Fact]
    public void The_committed_defense_built_backwards_derives_the_committed_layout()
    {
        // The strongest form of the claim: a board whose placement order is the
        // reverse of canonical order derives the authored defense exactly, and
        // the match behind it is the committed run down to the state hash.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout authored = TheMatch.Layout(types);
        Board board = Board.Empty;

        for (int index = authored.Count - 1; index >= 0; index--)
        {
            PlacedTower tower = authored.Towers[index];
            board = board.Place(tower.Type, tower.Column, tower.Row);
        }

        TowerLayout derived = board.Layout();

        Assert.Equal(Spelling(authored), Spelling(derived));

        MatchResult played = new Match(
            TheMatch.Map(), TheRuleset.Committed(), derived, TheMatch.Wave(types), TheMatch.Seed).Resolve();
        MatchResult committed = TheMatch.Fresh().Resolve();

        Assert.Equal(committed.Leaked, played.Leaked);
        Assert.Equal(committed.FinalTick, played.FinalTick);
        Assert.Equal(committed.RollingStateHash, played.RollingStateHash);
    }

    [Fact]
    public void An_empty_board_derives_an_empty_layout()
    {
        Assert.Equal(0, Board.Empty.Count);
        Assert.Empty(Board.Empty.Placements);
        Assert.Equal(0, Board.Empty.Layout().Count);
        Assert.Empty(Board.Empty.Layout().Towers);
    }

    [Fact]
    public void A_match_against_an_empty_board_resolves()
    {
        // A run starts with the purse and nothing on the map. Standing nothing
        // at wave 1 is a position -- the whole wave walks through unopposed --
        // and not a match that refuses to load.
        UnitTypeTable types = TheMatch.Types();

        MatchResult result = new Match(
            TheMatch.Map(),
            TheRuleset.Committed(),
            Board.Empty.Layout(),
            TheMatch.Wave(types),
            TheMatch.Seed).Resolve();

        Assert.True(result.Total > 0);
        Assert.Equal(result.Total, result.Leaked);
    }

    [Fact]
    public void A_defense_file_with_no_towers_in_it_still_refuses_to_load()
    {
        // The other half of the sentence above. An empty board is a decision; an
        // empty defense file is one somebody forgot to finish, and that rule
        // stays exactly where it is.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TowerLayout.Parse("# nothing but a comment", TheMatch.Types()));

        Assert.Contains("no towers in it at all", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_second_placement_on_one_cell_is_refused()
    {
        UnitTypeTable types = TheMatch.Types();
        Board board = Board.Empty.Place(Bolt(types), 6, 4);

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => board.Place(Mortar(types), 6, 4));

        Assert.Contains("One cell holds one placement", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_upgrade_of_a_cell_nothing_stands_on_is_refused()
    {
        UnitTypeTable types = TheMatch.Types();
        Board board = Board.Empty.Place(Bolt(types), 6, 4);

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => board.Upgrade(Mortar(types), 6, 5));

        Assert.Contains("where nothing stands", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A layout written out, so two of them can be compared as one value.</summary>
    private static string[] Spelling(TowerLayout layout) =>
        layout.Towers.Select(tower => $"{tower.Type.Id} {tower.Column} {tower.Row} {tower.Hex}").ToArray();
}
