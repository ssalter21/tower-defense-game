using System.Globalization;
using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// The playfield drawn as a picture: one hexagon per cell, at the offsets the
/// odd-r grid actually puts them, tinted by tier and lettered with it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This exists so that a person editing <c>content/map.txt</c> can look at
/// what they typed.</b> The map is about to be drawn by hand -- a fold, with
/// three tiers and a corridor that has to stay one hex wide -- and the file is
/// the drawing surface. Without a picture the loop is edit, wait for somebody
/// else to describe the result, edit again; with one it is edit and look.
/// </para>
/// <para>
/// <b>It draws the PARSED map and never the text.</b> What comes out is
/// therefore a picture of what the loader read, corridor assertion and all: a
/// file that will not load produces no picture and the refusal instead, which
/// is the honest answer to "is this a map yet".
/// </para>
/// <para>
/// <b>Scalable vector graphics, because it costs no dependency.</b> A browser
/// opens it, it is text so a diff of one is readable, and nothing here has to
/// link an image library into a program whose other job is to be reproducible
/// on any machine. Every number is written under the invariant culture for the
/// same reason every other file this program writes is.
/// </para>
/// <para>
/// <b>The palette is a diagram's and not the game's.</b> Tiers read as three
/// steps of one shade so the fold is legible at a glance; nothing here is an
/// art decision, and the tile set the game is actually drawn with is somebody
/// else's to choose.
/// </para>
/// </remarks>
internal static class MapPicture
{
    /// <summary>The circumradius of one hex, in the picture's own units.</summary>
    private const double HexSize = 34;

    /// <summary>How far the drawing sits from the edge, leaving room for the labels.</summary>
    private const double Margin = 46;

    /// <summary>How tall the caption under the board is.</summary>
    private const double CaptionHeight = 96;

    /// <summary>The letter the lowest tier is written with, matching the file's.</summary>
    private const char FirstTier = 'a';

    /// <summary>Ground, one entry per tier, palest first.</summary>
    private static readonly string[] GroundByTier = { "#e4ead8", "#c3d1a8", "#9db477" };

    /// <summary>Corridor, one entry per tier, on the same ramp in a warmer hue.</summary>
    private static readonly string[] RouteByTier = { "#e6d3a8", "#cfb173", "#ad8b45" };

    private const string Ink = "#33312c";

    private const string Paper = "#faf8f2";

    private const string HexEdge = "#8d887b";

    private const string SpawnEdge = "#2f7d32";

    private const string ExitEdge = "#b4343f";

    /// <summary>The whole picture, as the bytes of an <c>.svg</c> file.</summary>
    public static string ToSvg(HexMap map)
    {
        double width = (HexWidth * (map.Width + 0.5)) + (Margin * 2);
        double height = (RowStep * (map.Height - 1)) + (HexSize * 2) + (Margin * 2) + CaptionHeight;

        var svg = new StringBuilder();

        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
            .Append(Number(width))
            .Append(' ')
            .Append(Number(height))
            .Append("\" width=\"")
            .Append(Number(width))
            .Append("\" height=\"")
            .Append(Number(height))
            .Append("\">\n");

        svg.Append("<rect x=\"0\" y=\"0\" width=\"")
            .Append(Number(width))
            .Append("\" height=\"")
            .Append(Number(height))
            .Append("\" fill=\"")
            .Append(Paper)
            .Append("\"/>\n");

        svg.Append("<g font-family=\"ui-monospace, Consolas, monospace\" text-anchor=\"middle\">\n");

        Rulers(svg, map);

        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                Cell(svg, map, column, row);
            }
        }

        Caption(svg, map, height);

        svg.Append("</g>\n</svg>\n");

        return svg.ToString();
    }

    /// <summary>
    /// What the shell prints after writing the file: the shape, the corridor and
    /// how many hexes stand at each tier.
    /// </summary>
    /// <remarks>
    /// The tier census is the half a picture is worst at. Counting nine hexes
    /// of tier <c>c</c> across a fold by eye is exactly the mistake an author
    /// makes at the end of a long edit, and it is one line here.
    /// </remarks>
    public static string Summary(HexMap map)
    {
        var text = new StringBuilder();

        text.Append("map        ")
            .Append(PlainText.Number(map.Width))
            .Append(" by ")
            .Append(PlainText.Number(map.Height))
            .Append(", corridor of ")
            .Append(PlainText.Number(map.Route.Count))
            .Append(" hexes from (")
            .Append(PlainText.Number(map.Spawn.Q))
            .Append(", ")
            .Append(PlainText.Number(map.Spawn.R))
            .Append(") to (")
            .Append(PlainText.Number(map.Exit.Q))
            .Append(", ")
            .Append(PlainText.Number(map.Exit.R))
            .Append(")\n");

        int[] census = Census(map);

        for (int tier = 0; tier < census.Length; tier++)
        {
            text.Append("tier ")
                .Append((char)(FirstTier + tier))
                .Append("     ")
                .Append(PlainText.Number(census[tier]))
                .Append(census[tier] == 1 ? " hex\n" : " hexes\n");
        }

        return text.ToString();
    }

    private static double HexWidth => Math.Sqrt(3) * HexSize;

    private static double RowStep => 1.5 * HexSize;

    /// <summary>How many hexes stand at each tier, lowest first.</summary>
    private static int[] Census(HexMap map)
    {
        var census = new int[HexMap.LevelCount];

        for (int row = 0; row < map.Height; row++)
        {
            for (int column = 0; column < map.Width; column++)
            {
                census[map.LevelAt(column, row)]++;
            }
        }

        return census;
    }

    /// <summary>
    /// The column numbers across the top and the row numbers down the side, in
    /// the coordinates a <c>place</c> command names -- so a cell can be read
    /// off the picture and typed straight into a script.
    /// </summary>
    private static void Rulers(StringBuilder svg, HexMap map)
    {
        for (int column = 0; column < map.Width; column++)
        {
            Text(svg, CentreX(column, 0), Margin - 18, PlainText.Number(column), 13, "#7c776c");
        }

        for (int row = 0; row < map.Height; row++)
        {
            Text(svg, Margin - 22, CentreY(row) + 5, PlainText.Number(row), 13, "#7c776c");
        }
    }

    /// <summary>One hex: its outline, and the tier letter inside it.</summary>
    private static void Cell(StringBuilder svg, HexMap map, int column, int row)
    {
        MapCell cell = map.CellAt(column, row);
        int level = map.LevelAt(column, row);
        double x = CentreX(column, row);
        double y = CentreY(row);

        svg.Append("<polygon points=\"").Append(Corners(x, y)).Append("\" fill=\"")
            .Append(cell == MapCell.Ground ? GroundByTier[level] : RouteByTier[level])
            .Append("\" stroke=\"")
            .Append(EdgeOf(cell))
            .Append("\" stroke-width=\"")
            .Append(cell == MapCell.Ground ? "1.5" : "2.5")
            .Append("\"/>\n");

        // The ends carry their own letter, because which way a wave walks is
        // the first thing anybody looks for and it is not a tier.
        if (cell == MapCell.Spawn || cell == MapCell.Exit)
        {
            Text(svg, x, y + 7, cell == MapCell.Spawn ? "S" : "E", 22, Ink);
            Text(svg, x, y + 24, TierLetter(level), 11, "#5d584e");
            return;
        }

        Text(svg, x, y + 6, TierLetter(level), 16, level == 0 ? "#8d887b" : Ink);
    }

    /// <summary>
    /// The line under the board: what the picture is of, and what it holds.
    /// </summary>
    private static void Caption(StringBuilder svg, HexMap map, double height)
    {
        double baseline = height - CaptionHeight + 34;
        int[] census = Census(map);
        var tiers = new StringBuilder();

        for (int tier = 0; tier < census.Length; tier++)
        {
            tiers.Append(tier == 0 ? string.Empty : "   ")
                .Append(TierLetter(tier))
                .Append(' ')
                .Append(PlainText.Number(census[tier]));
        }

        Left(
            svg,
            Margin,
            baseline,
            PlainText.Number(map.Width)
            + " by "
            + PlainText.Number(map.Height)
            + ", corridor of "
            + PlainText.Number(map.Route.Count)
            + " hexes, S to E",
            15,
            Ink);

        Left(svg, Margin, baseline + 24, "hexes per tier   " + tiers, 15, Ink);
        Left(
            svg,
            Margin,
            baseline + 48,
            "drawn from the parsed map, so this is what the loader read",
            13,
            "#7c776c");
    }

    private static string TierLetter(int level) => ((char)(FirstTier + level)).ToString();

    private static string EdgeOf(MapCell cell)
    {
        switch (cell)
        {
            case MapCell.Spawn:
                return SpawnEdge;

            case MapCell.Exit:
                return ExitEdge;

            default:
                return HexEdge;
        }
    }

    /// <summary>Where the hex at an offset column and row has its centre.</summary>
    /// <remarks>
    /// Odd-r: the odd rows are the shifted ones, half a hex right, which is the
    /// same statement <c>content/map.txt</c> makes by indenting them and the
    /// same one <see cref="Hex.FromOddRowOffset"/> makes in axial.
    /// </remarks>
    private static double CentreX(int column, int row) =>
        Margin + (HexWidth * (column + (row % 2 == 1 ? 0.5 : 0.0))) + (HexWidth / 2);

    private static double CentreY(int row) => Margin + (RowStep * row) + HexSize;

    private static string Corners(double x, double y)
    {
        var points = new StringBuilder();

        for (int corner = 0; corner < 6; corner++)
        {
            double angle = Math.PI / 180 * ((60 * corner) - 30);

            points.Append(corner == 0 ? string.Empty : " ")
                .Append(Number(x + (HexSize * Math.Cos(angle))))
                .Append(',')
                .Append(Number(y + (HexSize * Math.Sin(angle))));
        }

        return points.ToString();
    }

    private static void Text(StringBuilder svg, double x, double y, string what, int size, string fill) =>
        Place(svg, x, y, what, size, fill, "middle");

    private static void Left(StringBuilder svg, double x, double y, string what, int size, string fill) =>
        Place(svg, x, y, what, size, fill, "start");

    private static void Place(
        StringBuilder svg,
        double x,
        double y,
        string what,
        int size,
        string fill,
        string anchor)
    {
        svg.Append("<text x=\"").Append(Number(x))
            .Append("\" y=\"").Append(Number(y))
            .Append("\" font-size=\"").Append(PlainText.Number(size))
            .Append("\" fill=\"").Append(fill)
            .Append("\" text-anchor=\"").Append(anchor)
            .Append("\">").Append(what).Append("</text>\n");
    }

    /// <summary>
    /// One coordinate, to two places under the one culture this program writes
    /// numbers in.
    /// </summary>
    private static string Number(double value) =>
        Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
}
