using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The committed ruleset, and the smallest well-formed one text can be planted
/// into.
/// </summary>
/// <remarks>
/// The tests open the file and hand the simulation text, exactly as
/// <see cref="TheMatch"/> does and for the same reason: the simulation never
/// learns a path exists.
/// </remarks>
public static class TheRuleset
{
    /// <summary>
    /// A complete ruleset with one row of each kind, laid out so that a test
    /// can plant text into any single rule without disturbing another.
    /// </summary>
    public const string Minimal = """
        matrix pierce 140 70 100
        matrix impact 70 100 140
        matrix magic 100 140 70
        armour 1 100
        floor 1
        interest 10 0
        income 100
        purse 100
        band 0 0
        band 50 5
        health 1500
        slots 2 1
        offering 3 3
        snapshot 10 25
        """;

    /// <summary>The committed file, as text.</summary>
    public static string CommittedText() => File.ReadAllText(RepoLayout.RulesetFile);

    /// <summary>The committed file, parsed.</summary>
    public static Ruleset Committed() => Ruleset.Parse(CommittedText());

    /// <summary>
    /// The committed ruleset and the committed unit table, priced together:
    /// every unit's cost column and every line item that is not a unit, in the
    /// one table they share.
    /// </summary>
    public static CostTable Costs() => CostTable.From(Committed(), TheMatch.Types());

    /// <summary>
    /// <see cref="Minimal"/> with the matrix rebuilt from three cells, cycled
    /// so that every row and every column is a permutation of them. Pierce
    /// against Swift is always the first of the three, which is what lets a
    /// sweep move one multiplier at a time.
    /// </summary>
    public static string WithCells(int first, int second, int third) =>
        Replace(
            Replace(
                Replace(Minimal, "matrix pierce 140 70 100", Row("pierce", first, second, third)),
                "matrix impact 70 100 140",
                Row("impact", second, third, first)),
            "matrix magic 100 140 70",
            Row("magic", third, first, second));

    /// <summary><see cref="Minimal"/> with one substring swapped for another, exactly once.</summary>
    public static string Replace(string text, string what, string with)
    {
        Assert.Contains(what, text, StringComparison.Ordinal);

        return text.Replace(what, with, StringComparison.Ordinal);
    }

    /// <summary><see cref="Minimal"/> with every row starting with this keyword taken out.</summary>
    public static string Without(string keyword)
    {
        string[] lines = Minimal.Split('\n');
        var kept = lines.Where(line => !line.TrimStart().StartsWith(keyword + " ", StringComparison.Ordinal));

        Assert.NotEqual(lines.Length, kept.Count());

        return string.Join("\n", kept);
    }

    private static string Row(string attack, int swift, int armoured, int arcane) =>
        "matrix "
        + attack
        + " "
        + swift.ToString(CultureInfo.InvariantCulture)
        + " "
        + armoured.ToString(CultureInfo.InvariantCulture)
        + " "
        + arcane.ToString(CultureInfo.InvariantCulture);
}
