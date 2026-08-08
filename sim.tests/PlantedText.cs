namespace Sim.Tests;

/// <summary>
/// Deliberately wrong content, made by swapping one run of characters in a
/// well-formed file for another.
/// </summary>
/// <remarks>
/// One home for the swap, used by every fixture that plants text --
/// <see cref="TheRuleset"/>, <see cref="TheSchedule"/>, <see cref="TheSweep"/>.
/// The swap asserts that the text it is looking for is there, because a planted
/// substring that matched nothing is a test comparing a file against itself.
/// </remarks>
public static class PlantedText
{
    /// <summary>One substring swapped for another, exactly once.</summary>
    public static string Replace(string text, string what, string with)
    {
        Assert.Contains(what, text, StringComparison.Ordinal);

        return text.Replace(what, with, StringComparison.Ordinal);
    }
}
