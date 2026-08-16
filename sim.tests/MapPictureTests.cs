using System.Globalization;
using System.Text.RegularExpressions;
using Sim.Cli;

// Sim.Match is the simulation's match, and it is what `Match` means inside
// Sim.Tests. The regular-expression one needs a name of its own here.
using RegexMatch = System.Text.RegularExpressions.Match;

namespace Sim.Tests;

/// <summary>
/// The playfield drawn as a picture: one hexagon per cell, where the odd-r grid
/// puts them, lettered with the tier each hex stands at.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is asserted is the geometry and the alphabet, not the taste.</b> The
/// palette, the font size and the caption are a diagram's choices and a test
/// that pinned them would go red on every one of them. Where a hexagon is, and
/// which letter is inside it, are claims about the map -- and both are exactly
/// the sort of claim that can be quietly wrong in a drawing nobody diffs.
/// </para>
/// <para>
/// <b>The half-cell step is measured against the picture's own hexes</b> rather
/// than against a number restated here. A width written into this file would
/// agree with itself after somebody resized the drawing, which is the failure
/// an assertion about a step is supposed to catch.
/// </para>
/// </remarks>
public class MapPictureTests
{
    /// <summary>A fold, so that all three tiers are drawn and counted.</summary>
    private const string Folded = """
        .....
        .S#E.
        .....

        aabbc
        abbcc
        aaabb
        """;

    [Fact]
    public void The_picture_draws_one_hexagon_for_every_hex_of_the_committed_map()
    {
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

        Assert.Equal(map.Width * map.Height, FirstCornerXs(map).Count);
    }

    [Fact]
    public void An_odd_row_steps_exactly_half_a_hex_right_of_an_even_one()
    {
        // Odd-r offset, which is the same statement content/map.txt makes by
        // indenting its odd rows and the same one Hex.FromOddRowOffset makes in
        // axial. A drawing that put the shift on the even rows would look
        // plausible and would disagree with every coordinate a place command
        // names.
        //
        // OBSERVED: change MapPicture.CentreX to shift `row % 2 == 0`. The step
        // comes back negative half a hex and this goes red; the picture still
        // looks like a hex grid, which is the whole problem.
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));
        List<double> corners = FirstCornerXs(map);

        double hex = corners[1] - corners[0];
        double step = corners[map.Width] - corners[0];

        Assert.Equal(hex / 2, step, 2);
    }

    [Fact]
    public void Every_hex_is_lettered_with_the_tier_the_map_file_writes_it_with()
    {
        // The drawing states the level alphabet a second time, because HexMap
        // keeps the letters it parses with private -- exactly as BoardMap
        // states the four terrain characters a second time. This is what holds
        // the two statements together: the letters drawn are counted against
        // the levels the parser read, so a drawing that started its tiers at
        // some other letter has none of them.
        HexMap map = HexMap.Parse("folded", Folded);
        string svg = MapPicture.ToSvg(map);

        for (char tier = 'a'; tier <= 'c'; tier++)
        {
            int authored = Folded.Count(character => character == tier);

            Assert.True(authored > 0, "The fixture has no '" + tier + "' in it to count.");
            Assert.Equal(authored, Letters(svg, tier));
        }
    }

    [Fact]
    public void The_summary_counts_the_hexes_standing_at_each_tier()
    {
        // The half a picture is worst at: counting nine hexes of one tier
        // across a fold by eye is exactly the mistake an author makes at the
        // end of a long edit.
        string summary = MapPicture.Summary(HexMap.Parse("folded", Folded));

        Assert.Contains("corridor of 3 hexes", summary, StringComparison.Ordinal);
        Assert.Contains("tier a     6 hexes", summary, StringComparison.Ordinal);
        Assert.Contains("tier b     6 hexes", summary, StringComparison.Ordinal);
        Assert.Contains("tier c     3 hexes", summary, StringComparison.Ordinal);
    }

    /// <summary>
    /// The x of the first corner of every hexagon, in the order they are drawn
    /// -- which is row-major, so index <c>width</c> is the first hex of row
    /// one. Every corner sits a fixed distance from its own centre, so a
    /// difference between two of these is a difference between two centres.
    /// </summary>
    private static List<double> FirstCornerXs(HexMap map)
    {
        var found = new List<double>();

        foreach (RegexMatch polygon in Regex.Matches(MapPicture.ToSvg(map), "<polygon points=\"([^ \"]+)"))
        {
            found.Add(double.Parse(
                polygon.Groups[1].Value.Split(',')[0],
                CultureInfo.InvariantCulture));
        }

        return found;
    }

    /// <summary>How many text elements in the drawing are exactly that letter.</summary>
    private static int Letters(string svg, char letter) =>
        Regex.Matches(svg, ">" + letter + "</text>").Count;
}
