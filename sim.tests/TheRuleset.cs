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
        health 800
        snapshot 10 25
        """;

    /// <summary>The committed file, as text.</summary>
    public static string CommittedText() => File.ReadAllText(RepoLayout.RulesetFile);

    /// <summary>The committed file, parsed.</summary>
    public static Ruleset Committed() => Ruleset.Parse(CommittedText());

    /// <summary>
    /// The committed ruleset with one number moved and nothing else touched.
    /// </summary>
    /// <remarks>
    /// The income base, because it is a single number on its own row that no
    /// load-time constraint couples to another: every record stamped with the
    /// old ruleset is retired by it and nothing else about a run moves.
    /// </remarks>
    public static Ruleset Retuned() =>
        Ruleset.Parse(PlantedText.Replace(CommittedText(), "income        168", "income        169"));

    /// <summary>
    /// The committed ruleset with the armour denominator moved by one and
    /// nothing else touched.
    /// </summary>
    /// <remarks>
    /// <see cref="Retuned"/> moves the income base, which no match can see: it
    /// pays a purse, and a purse belongs to a run. This one moves a number the
    /// fused damage expression divides by on every landing, whatever the
    /// creep's armour, so a match run against it comes to a different rolling
    /// state hash. That difference is the reason a bundle stamps its ruleset,
    /// and a gate test that used a number no match reads would be proving the
    /// stamp against a change that could not have hurt anybody.
    /// </remarks>
    public static Ruleset RetunedDamage() =>
        Ruleset.Parse(PlantedText.Replace(
            CommittedText(),
            "armour          1          100",
            "armour          1          101"));

    /// <summary>Every rule the committed file states, once each, in the order it states them.</summary>
    public static TheoryData<string> EveryRule() => Cases(Keywords());

    /// <summary>Every rule the committed file states on exactly one row.</summary>
    public static TheoryData<string> EveryRuleStatedOnce() =>
        Cases(Keywords().Where(keyword => !IsRepeated(keyword)));

    /// <summary>
    /// Every number the committed file holds on a rule it states once: the
    /// keyword of the row it is on, and which of that row's columns it is.
    /// </summary>
    /// <remarks>
    /// This is the set of ruleset fields the simulation declares, read back off
    /// the file that declaration parses. Every declared field is one column of
    /// one required row -- a file missing a row is refused, and so is a row
    /// carrying the wrong number of columns -- so a field somebody adds to the
    /// simulation turns up here on its own.
    /// </remarks>
    public static TheoryData<string, int> EveryNumber()
    {
        var numbers = new TheoryData<string, int>();

        foreach (string[] fields in DataRows(CommittedText()))
        {
            if (IsRepeated(fields[0]))
            {
                continue;
            }

            for (int column = 1; column < fields.Length; column++)
            {
                numbers.Add(fields[0], column);
            }
        }

        return numbers;
    }

    /// <summary>
    /// The committed file with one of its numbers moved by one and nothing else
    /// touched.
    /// </summary>
    /// <remarks>
    /// Up where the column allows it and down otherwise. Every column has a
    /// declared range and a number authored at one end of one can only be moved
    /// the other way; a move the file refuses would let a caller pass on the
    /// refusal instead of on the hash.
    /// </remarks>
    public static string MovedNumber(string keyword, int column)
    {
        string up = MovedBy(keyword, column, 1);

        return Loads(up) ? up : MovedBy(keyword, column, -1);
    }

    /// <summary>The committed ruleset as a different file and the same rules.</summary>
    public static string ReformattedText() => Reauthoring.Reauthored(CommittedText());

    /// <summary>That file, parsed.</summary>
    public static Ruleset Reformatted() => Ruleset.Parse(ReformattedText());

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
        PlantedText.Replace(
            PlantedText.Replace(
                PlantedText.Replace(
                    Minimal,
                    "matrix pierce 140 70 100",
                    Row("pierce", first, second, third)),
                "matrix impact 70 100 140",
                Row("impact", second, third, first)),
            "matrix magic 100 140 70",
            Row("magic", third, first, second));

    /// <summary>The one row of <see cref="Minimal"/> that states this rule.</summary>
    public static string MinimalRow(string keyword) =>
        Assert.Single(
            DataRows(Minimal)
                .Where(fields => fields[0] == keyword)
                .Select(fields => string.Join(" ", fields)));

    /// <summary><see cref="Minimal"/> with every row starting with this keyword taken out.</summary>
    public static string Without(string keyword)
    {
        string[] lines = Minimal.Split('\n');
        var kept = lines.Where(line => !line.TrimStart().StartsWith(keyword + " ", StringComparison.Ordinal));

        Assert.NotEqual(lines.Length, kept.Count());

        return string.Join("\n", kept);
    }

    /// <summary>The keywords the committed file's data rows open with, once each, in file order.</summary>
    private static IEnumerable<string> Keywords() =>
        DataRows(CommittedText()).Select(fields => fields[0]).Distinct(StringComparer.Ordinal);

    /// <summary>The committed file with one number of one row shifted, asserted to have hit one row.</summary>
    private static string MovedBy(string keyword, int column, int step)
    {
        string[] lines = CommittedText().Split('\n');
        int moved = 0;

        for (int index = 0; index < lines.Length; index++)
        {
            string[] fields = Split(lines[index]);

            if (fields.Length == 0 || fields[0] != keyword)
            {
                continue;
            }

            fields[column] = (int.Parse(fields[column], CultureInfo.InvariantCulture) + step)
                .ToString(CultureInfo.InvariantCulture);
            lines[index] = string.Join(" ", fields);
            moved++;
        }

        Assert.Equal(1, moved);

        return string.Join("\n", lines);
    }

    /// <summary>Whether the ruleset parses at all, as opposed to what it parses to.</summary>
    private static bool Loads(string text)
    {
        try
        {
            Ruleset.Parse(text);

            return true;
        }
        catch (ContentException)
        {
            return false;
        }
    }

    /// <summary>
    /// True for a rule the committed file states on more than one row: the
    /// damage matrix, which is three rows of a Latin square, and the performance
    /// bands, which are however many ascending rows the file states. Those two
    /// describe a shape; every other rule is a keyword and one number per
    /// column, stated exactly once.
    /// </summary>
    private static bool IsRepeated(string keyword) =>
        DataRows(CommittedText()).Count(fields => fields[0] == keyword) > 1;

    /// <summary>One theory case per keyword.</summary>
    private static TheoryData<string> Cases(IEnumerable<string> keywords)
    {
        var cases = new TheoryData<string>();

        foreach (string keyword in keywords)
        {
            cases.Add(keyword);
        }

        return cases;
    }

    /// <summary>
    /// The data rows of a file, split into fields: every line that is neither
    /// blank nor a comment. The same walk the simulation does, done again here
    /// because a test may not reach inside it.
    /// </summary>
    private static IEnumerable<string[]> DataRows(string text) =>
        text.Split('\n').Select(Split).Where(fields => fields.Length > 0);

    private static string[] Split(string line) =>
        line.TrimStart().StartsWith('#')
            ? Array.Empty<string>()
            : line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

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
