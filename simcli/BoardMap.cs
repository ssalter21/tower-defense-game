using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// The playfield drawn as text: the grid in the coordinates a command names, a
/// letter on every hex a tower stands on, and a legend of what is standing
/// beside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Counting characters across a row gives the pair a <c>place</c> takes.</b>
/// The column number is written above its column and the row number down the
/// side, and the odd rows are pushed right exactly as <c>content/map.txt</c>
/// writes them -- the grid is odd-r offset, so the sideways step in the
/// corridor is a property of the coordinates rather than a drawing choice. A
/// legend row spells its cell as <c>column,row</c>, which is the order the two
/// operands of a <c>place</c> row are written in.
/// </para>
/// <para>
/// <b>A letter is the first letter of the type's label, upper case where
/// something upgraded into it.</b> The case is read off
/// <see cref="UpgradeLadder"/> rather than stored on a placement: a board
/// carries the type standing on a cell and nothing about how it got there, and
/// whether that type has an incoming edge is the whole of the question.
/// </para>
/// <para>
/// <b>A tower letter never covers a corridor character.</b> A place onto a
/// corridor cell is refused by <c>Footing.Possible</c> before a board ever sees
/// it, so the entrance and the exit are always drawn. What a roster could still
/// do is put an upper case <c>S</c> or <c>E</c> on a ground cell, and the
/// legend is what tells the two apart: the corridor's ends have no placement id
/// beside them.
/// </para>
/// <para>
/// <b>The column beside the grid is this drawing's, and a caller may put one
/// more panel in it.</b> Where that column begins depends on how wide the grid
/// is and how wide an id column is, both of which are worked out here, so a
/// caller that placed a panel of its own would be a second copy of this
/// drawing's geometry.
/// </para>
/// <para>
/// <b>Pure: a map, a board and a ladder in, a string out.</b> Nothing here
/// reads a console, holds state or knows a run has rounds, which is what lets a
/// test assert on the text and what lets the loop reprint the panel after every
/// word somebody types.
/// </para>
/// </remarks>
internal static class BoardMap
{
    /// <summary>
    /// The four characters <c>content/map.txt</c> writes its cells with.
    /// <see cref="HexMap"/> keeps its own copies of these private, so this is a
    /// second statement of one alphabet; what holds the two together is
    /// <c>BoardMapTests</c> comparing the rows of the drawing against the rows
    /// of the committed file.
    /// </summary>
    private const char GroundCharacter = '.';

    private const char RouteCharacter = '#';

    private const char SpawnCharacter = 'S';

    private const char ExitCharacter = 'E';

    /// <summary>
    /// How wide one hex is: its character, and the two spaces separating it from
    /// the hex to its left.
    /// </summary>
    private const int CellWidth = 3;

    /// <summary>How wide the row number down the side is.</summary>
    private const int RowLabelWidth = 2;

    /// <summary>What separates the row number from its row.</summary>
    private const int LabelGap = 2;

    /// <summary>
    /// How far right an odd row sits. Odd-r offset puts the odd rows half a hex
    /// right of the even ones, and a three-character cell does not halve, so it
    /// rounds up.
    /// </summary>
    private const int OddRowIndent = 2;

    /// <summary>What separates the widest row of the grid from the legend beside it.</summary>
    private const int LegendGap = 8;

    /// <summary>
    /// Which line of the output the legend opens on. Below the row of column
    /// numbers, so the word heading the legend cannot be read as one of them.
    /// </summary>
    private const int LegendTop = 2;

    /// <summary>Narrowest the legend's id column gets, however few digits the ids have.</summary>
    private const int MinimumIdWidth = 2;

    /// <summary>How wide the legend's type name column is.</summary>
    private const int NameWidth = 9;

    /// <summary>What the legend calls itself.</summary>
    private const string StandingLabel = "standing";

    /// <summary>What a board with nothing on it says in place of the legend.</summary>
    private const string NothingStanding = "nothing standing";

    /// <summary>How many blank lines separate the legend from a panel under it.</summary>
    private const int PanelGap = 1;

    private static readonly string[] NoPanel = new string[0];

    /// <summary>
    /// The grid and its legend, as one block of lines with no trailing newline
    /// and no trailing spaces on any line.
    /// </summary>
    public static string ToText(HexMap map, Board board, UpgradeLadder ladder) =>
        ToText(map, board, ladder, NoPanel, MinimumIdWidth);

    /// <summary>
    /// The same drawing with one more panel in the legend's column, a blank
    /// line under it.
    /// </summary>
    /// <param name="under">
    /// The panel: a line heading it, then rows each opening with an id
    /// right-aligned in its own column.
    /// </param>
    /// <param name="panelIdWidth">
    /// How wide that id column is. The last digit of an id lands a fixed
    /// distance right of the grid whatever the width, so the panel's ids and the
    /// legend's share a column even where the two are not the same width.
    /// </param>
    public static string ToText(
        HexMap map,
        Board board,
        UpgradeLadder ladder,
        IReadOnlyList<string> under,
        int panelIdWidth)
    {
        int idWidth = IdWidth(board);
        string[] grid = Grid(map, board, ladder);
        string[] legend = Legend(board, ladder, idWidth);
        int widest = TextPanel.Widest(grid);
        var lines = new List<string>(grid);

        TextPanel.Beside(lines, legend, LegendTop, Column(widest, idWidth));
        TextPanel.Beside(
            lines, under, LegendTop + legend.Length + PanelGap, Column(widest, panelIdWidth));

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Where a panel beside the grid begins: far enough right that the last
    /// digit of every id on it lands a fixed distance from the widest row of the
    /// grid, so the word heading the panel carries whatever the id column's
    /// width leaves over as leading space.
    /// </summary>
    private static int Column(int widest, int idWidth) => widest + LegendGap + 1 - idWidth;

    /// <summary>
    /// The column numbers, then one line per row: the row number, the odd row's
    /// half-cell step, and one character per hex.
    /// </summary>
    private static string[] Grid(HexMap map, Board board, UpgradeLadder ladder)
    {
        var lines = new string[map.Height + 1];
        var text = new StringBuilder();

        text.Append(' ', RowLabelWidth + LabelGap);

        for (int column = 0; column < map.Width; column++)
        {
            text.Append(PlainText.Number(column).PadLeft(CellWidth));
        }

        lines[0] = text.ToString();

        for (int row = 0; row < map.Height; row++)
        {
            text.Clear();
            text.Append(PlainText.Number(row).PadLeft(RowLabelWidth)).Append(' ', LabelGap);

            if (row % 2 == 1)
            {
                text.Append(' ', OddRowIndent);
            }

            for (int column = 0; column < map.Width; column++)
            {
                text.Append(' ', CellWidth - 1).Append(CharacterAt(map, board, ladder, column, row));
            }

            lines[row + 1] = text.ToString();
        }

        return lines;
    }

    /// <summary>
    /// The word heading the legend and one row per placement, in the order the
    /// run placed them -- which is ascending by the id each row carries.
    /// </summary>
    private static string[] Legend(Board board, UpgradeLadder ladder, int idWidth)
    {
        string heading = new string(' ', idWidth - 1);

        if (board.Count == 0)
        {
            return new[] { heading + NothingStanding };
        }

        var lines = new string[board.Count + 1];
        lines[0] = heading + StandingLabel;

        for (int index = 0; index < board.Count; index++)
        {
            Placement placement = board.Placements[index];

            lines[index + 1] = new StringBuilder()
                .Append(PlainText.Number(placement.Id).PadLeft(idWidth))
                .Append("  ")
                .Append(LetterFor(placement.Type, ladder))
                .Append("  ")
                .Append(placement.Type.Label.PadRight(NameWidth))
                .Append(PlainText.Number(placement.Column))
                .Append(',')
                .Append(PlainText.Number(placement.Row))
                .ToString();
        }

        return lines;
    }

    /// <summary>How wide the legend's id column is: enough for the longest id on the board.</summary>
    private static int IdWidth(Board board)
    {
        int width = MinimumIdWidth;

        for (int index = 0; index < board.Count; index++)
        {
            width = Math.Max(width, PlainText.Number(board.Placements[index].Id).Length);
        }

        return width;
    }

    /// <summary>What one hex is drawn as: the tower standing on it, or the map underneath.</summary>
    private static char CharacterAt(HexMap map, Board board, UpgradeLadder ladder, int column, int row)
    {
        for (int index = 0; index < board.Count; index++)
        {
            Placement placement = board.Placements[index];

            if (placement.Column == column && placement.Row == row)
            {
                return LetterFor(placement.Type, ladder);
            }
        }

        return CharacterOf(map.CellAt(column, row));
    }

    /// <summary>
    /// The type's initial, upper case where the ladder has an edge pointing at
    /// it and lower case where it is a root nothing upgrades into.
    /// </summary>
    private static char LetterFor(UnitType type, UpgradeLadder ladder)
    {
        char initial = type.Label[0];

        for (int index = 0; index < ladder.Count; index++)
        {
            if (ladder.Edges[index].To == type.Id)
            {
                return char.ToUpperInvariant(initial);
            }
        }

        return char.ToLowerInvariant(initial);
    }

    private static char CharacterOf(MapCell cell) => cell switch
    {
        MapCell.Route => RouteCharacter,
        MapCell.Spawn => SpawnCharacter,
        MapCell.Exit => ExitCharacter,
        _ => GroundCharacter,
    };
}
