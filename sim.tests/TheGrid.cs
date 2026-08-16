namespace Sim.Tests;

/// <summary>
/// Map text written for a test: a terrain grid, and the level grid every map
/// carries under it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A map written here is on the flat unless the test is about the
/// levels.</b> A test asking whether a corridor branches has nothing to say
/// about how high its hexes stand, and a second block written out under every
/// one of those grids would bury the shape the test is actually about. So the
/// terrain is written and the ground tier is appended to it.
/// </para>
/// <para>
/// <b>What comes back is map text and not a map.</b> It goes through the real
/// parser, two blocks and all, so nothing here is a second reading of the
/// format -- which is the trap a fixture that built cells directly would fall
/// into. A ragged terrain stays ragged, because the level rows are cut to the
/// rows they are under rather than to a width this decided.
/// </para>
/// </remarks>
public static class TheGrid
{
    /// <summary>The lowest tier, which is what a map with no fold in it is all of.</summary>
    public const char GroundTier = 'a';

    /// <summary>The terrain, with every hex of it standing on the ground tier.</summary>
    public static string OnTheFlat(string terrain) => AtTier(terrain, GroundTier);

    /// <summary>The terrain, with every hex of it standing on one named tier.</summary>
    public static string AtTier(string terrain, char tier)
    {
        string[] rows = terrain
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(row => row.Trim())
            .Where(row => row.Length > 0 && !row.StartsWith("//", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(rows);

        return terrain
            + "\n\n"
            + string.Join("\n", rows.Select(row => new string(tier, row.Length)));
    }
}
