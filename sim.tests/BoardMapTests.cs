using System.Globalization;
using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The playfield drawn as text, against the committed map and the boards the
/// committed run builds on it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The whole text is asserted, not a property of it.</b> What can be wrong
/// with a drawing is where its characters are: a row indented by the wrong
/// amount, a legend column that drifted, a letter one hex left of the cell it
/// stands on. None of those is visible to an assertion that counts lines or
/// looks for a substring, and all of them are visible in a diff of the block.
/// </para>
/// <para>
/// <b>The map is the committed one and the boards are the committed run's.</b>
/// The three archers below are the placements <c>content/commands.txt</c> makes
/// in waves one to three, and the ranger is its wave-five upgrade -- so the
/// coordinates in the legend can be read against the <c>place</c> rows of that
/// file, which is the claim the drawing exists to make.
/// </para>
/// <para>
/// <b>Each block was watched failing under a deliberately wrong drawing</b>,
/// and the wrong drawing is written above it so the observation can be
/// repeated.
/// </para>
/// </remarks>
public class BoardMapTests
{
    /// <summary>The archer, which is the root of the committed ladder's one edge.</summary>
    private const int ArcherId = 3;

    /// <summary>The ranger, which is what that edge points at.</summary>
    private const int RangerId = 14;



    [Fact]
    public void The_map_draws_what_the_committed_run_has_standing_at_wave_four()
    {
        UnitTypeTable types = Types();
        UnitType archer = types.ById(ArcherId);

        Board board = Board.Empty
            .Place(archer, 6, 2)
            .Place(archer, 7, 4)
            .Place(archer, 7, 6);

        // Columns across the top, rows down the side, and the odd rows pushed
        // half a cell right exactly as content/map.txt writes them -- so
        // counting characters across row 2 to the 'a' gives 6, which is the
        // column its `place 1 3 6 2` row names. The legend spells the same
        // pair in the same order that row does, which is what makes "6,2" here
        // and 6 2 there one fact rather than two: swap the two operands
        // anywhere and this block reads 2,6.
        //
        // OBSERVED: set BoardMap.OddRowIndent to zero. The odd rows come back
        // flush with the even ones, the corridor stops stepping sideways at its
        // turns, and a player counting across row 3 to the descent reads column
        // 3 for a hex the map puts at column 2 of a shifted row.
        Assert.Equal(
            """
                  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18
             0    .  .  .  .  S  .  .  .  .  .  .  .  .  .  .  .  E  .  .
             1      .  .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  #  .  .        standing
             2    .  .  .  #  #  .  a  .  .  .  .  .  .  .  .  .  #  .  .          1  a  archer   6,2
             3      .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .        2  a  archer   7,4
             4    .  .  .  #  #  #  #  a  .  .  .  .  .  .  .  .  #  .  .          3  a  archer   7,6
             5      .  .  .  .  .  .  #  .  .  .  #  #  #  #  #  #  .  .  .
             6    .  .  .  .  .  .  #  a  .  .  #  .  .  .  .  .  .  .  .
             7      .  .  .  .  .  .  #  .  .  .  #  #  #  #  #  #  .  .  .
             8    .  .  .  .  #  #  #  .  .  .  .  .  .  .  .  .  #  .  .
             9      .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
            10    .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
            11      .  .  .  #  #  #  #  #  #  #  #  #  #  #  #  #  .  .  .
            12    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
            """,
            BoardMap.ToText(Map(), board, Ladder(types)));
    }

    [Fact]
    public void An_upgraded_tower_draws_upper_case_and_keeps_the_placement_id_it_had()
    {
        UnitTypeTable types = Types();
        UnitType archer = types.ById(ArcherId);

        Board board = Board.Empty
            .Place(archer, 6, 2)
            .Place(archer, 7, 4)
            .Place(archer, 7, 6)
            .Place(archer, 4, 5)
            .Upgrade(types.ById(RangerId), 6, 2);

        // The ranger is still placement 1 on the cell placement 1 was made on,
        // and the only thing the upgrade changed about the drawing is the
        // letter and its case.
        //
        // OBSERVED: have BoardMap.LetterFor lower-case an initial it found an
        // incoming edge for. The ranger draws 'r' on the grid and in the
        // legend, and the one thing on the board that cost a round's whole
        // purse looks exactly like the three that did not.
        Assert.Equal(
            """
                  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18
             0    .  .  .  .  S  .  .  .  .  .  .  .  .  .  .  .  E  .  .
             1      .  .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  #  .  .        standing
             2    .  .  .  #  #  .  R  .  .  .  .  .  .  .  .  .  #  .  .          1  R  ranger   6,2
             3      .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .        2  a  archer   7,4
             4    .  .  .  #  #  #  #  a  .  .  .  .  .  .  .  .  #  .  .          3  a  archer   7,6
             5      .  .  .  .  a  .  #  .  .  .  #  #  #  #  #  #  .  .  .        4  a  archer   4,5
             6    .  .  .  .  .  .  #  a  .  .  #  .  .  .  .  .  .  .  .
             7      .  .  .  .  .  .  #  .  .  .  #  #  #  #  #  #  .  .  .
             8    .  .  .  .  #  #  #  .  .  .  .  .  .  .  .  .  #  .  .
             9      .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
            10    .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
            11      .  .  .  #  #  #  #  #  #  #  #  #  #  #  #  #  .  .  .
            12    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
            """,
            BoardMap.ToText(Map(), board, Ladder(types)));
    }

    [Fact]
    public void A_board_with_nothing_on_it_draws_the_map_and_says_so()
    {
        // A heading over no rows reads as a legend that was cut off, which is
        // the one thing a player opening wave one would see.
        //
        // OBSERVED: return the heading and no rows from BoardMap.Legend for a
        // board with nothing on it. The word "standing" sits beside row 1 with
        // nothing under it, and the first frame of every run looks like a
        // drawing that ran out of output.
        Assert.Equal(
            """
                  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18
             0    .  .  .  .  S  .  .  .  .  .  .  .  .  .  .  .  E  .  .
             1      .  .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  #  .  .        nothing standing
             2    .  .  .  #  #  .  .  .  .  .  .  .  .  .  .  .  #  .  .
             3      .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
             4    .  .  .  #  #  #  #  .  .  .  .  .  .  .  .  .  #  .  .
             5      .  .  .  .  .  .  #  .  .  .  #  #  #  #  #  #  .  .  .
             6    .  .  .  .  .  .  #  .  .  .  #  .  .  .  .  .  .  .  .
             7      .  .  .  .  .  .  #  .  .  .  #  #  #  #  #  #  .  .  .
             8    .  .  .  .  #  #  #  .  .  .  .  .  .  .  .  .  #  .  .
             9      .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
            10    .  .  .  #  .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
            11      .  .  .  #  #  #  #  #  #  #  #  #  #  #  #  #  .  .  .
            12    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
            """,
            BoardMap.ToText(Map(), Board.Empty, Ladder(Types())));
    }

    [Fact]
    public void Every_hex_is_drawn_as_the_character_the_committed_map_writes_it_with()
    {
        // The drawing states the map's four characters a second time, because
        // HexMap keeps the copies it parses with private. This is what holds
        // the two statements together, and it anchors on the authored file
        // rather than on the parser: take the spacing out of a drawn row and
        // what is left is the row number followed by that line of
        // content/map.txt.
        //
        // OBSERVED: change BoardMap.RouteCharacter to '='. This goes red naming
        // the first corridor row and the characters it expected. It is also
        // the one assertion here that stays red once the three blocks above
        // have been refreshed by pasting the drawing's new output, which is how
        // a hand-typed expectation gets updated and how a wrong alphabet gets
        // pasted into one.
        string[] drawn = BoardMap.ToText(Map(), Board.Empty, Ladder(Types())).Split('\n');
        string[] rows = MapRows();

        Assert.Equal(rows.Length + 1, drawn.Length);

        for (int row = 0; row < rows.Length; row++)
        {
            // A prefix rather than the whole line, because the legend is beside
            // one of these rows and is not part of the grid.
            Assert.StartsWith(
                row.ToString(CultureInfo.InvariantCulture) + rows[row],
                drawn[row + 1].Replace(" ", string.Empty, StringComparison.Ordinal),
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The committed map's terrain grid, one string per row, with the comment
    /// block it opens with taken off and the level grid under it left where it
    /// is.
    /// </summary>
    /// <remarks>
    /// <b>The first block and only the first.</b> A map file is two grids -- the
    /// terrain and the tier every hex of it stands at -- and what this drawing
    /// puts on a cell is the terrain character. The level block is a second
    /// block of the same shape, so a reader that took every row would find
    /// twice as many as the drawing has and fail counting rather than
    /// comparing.
    ///
    /// The rows are trimmed because the file indents its odd rows: odd-r offset
    /// shifts them half a cell, the file says so in whitespace so that what is
    /// typed looks like the board, and the drawing says so with
    /// <c>BoardMap.OddRowIndent</c>. Both are decoration over the same columns,
    /// which is exactly what this comparison is about.
    /// </remarks>
    private static string[] MapRows()
    {
        var rows = new List<string>();

        foreach (string line in File.ReadAllLines(RepoLayout.MapFile))
        {
            string row = line.Trim();

            if (row.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (row.Length == 0)
            {
                if (rows.Count > 0)
                {
                    break;
                }

                continue;
            }

            rows.Add(row);
        }

        return rows.ToArray();
    }

    private static UnitTypeTable Types() => UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

    private static UpgradeLadder Ladder(UnitTypeTable types) =>
        UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);

    private static HexMap Map() => HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));
}
