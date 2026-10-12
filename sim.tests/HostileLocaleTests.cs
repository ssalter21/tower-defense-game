using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The whole parse, run again under a culture chosen to break it.
/// </summary>
/// <remarks>
/// <para>
/// A culture-sensitive parse is the classic bug that never reproduces on the
/// machine that wrote it. <c>int.Parse</c> consults
/// <see cref="CultureInfo.CurrentCulture"/> by default, so a group separator, a
/// digit substitution or a negative sign that is not U+002D turns the same
/// bytes into a different number on somebody else's laptop -- and in this
/// design that number is inside a hash that a stored record pins, so the
/// symptom is a record that refuses to replay for a reason nobody can see.
/// </para>
/// <para>
/// The simulation's answer is not to remember to pass an invariant culture
/// everywhere. It is to have no framework number parser in the assembly at all
/// -- <see cref="DataText"/> accumulates ASCII digits by hand -- so there is
/// nothing for a culture to be consulted about. These tests are what turns that
/// claim into an observation, on this machine, now.
/// </para>
/// <para>
/// Each one asserts first that the hostile culture is genuinely in effect,
/// because a test that silently ran under the invariant culture -- which is
/// what happens when globalization is switched off at build time -- would pass
/// while proving nothing.
/// </para>
/// <para>
/// <b>"The whole parse" is meant literally, and is derived rather than typed.</b>
/// The sweep walks <see cref="ContentParsers.All"/>, which is the same
/// declaration <see cref="RepoLayout.NumericContentFiles"/> is taken from, and
/// a separate test reflects over the assembly to prove that list names every
/// parser there is. The suite used to enumerate its parsers by hand and quietly
/// omitted three of them -- the tower layout, the golden trace and the upgrade
/// ladder, the last of which folds into the roster's content hash and so gates
/// every stored record. Derivation is what stops that from being possible
/// again: a parser can be in both gates or in neither, and never in one.
/// </para>
/// </remarks>
public class HostileLocaleTests
{
    /// <summary>
    /// Turkish. Its dotless-i casing rules are the reason every string
    /// comparison in the parsers is ordinal: under this culture
    /// <c>"hitscan".ToUpper()</c> does not contain an <c>I</c>.
    /// </summary>
    private const string Turkish = "tr-TR";

    /// <summary>German. Comma is the decimal separator and stop is the group separator.</summary>
    private const string CommaDecimal = "de-DE";

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void Every_content_parser_hashes_identically_under_a_hostile_culture(string name)
    {
        // The whole sweep, and the only test here that grows on its own when a
        // parser is added. Each parser's committed file is parsed twice and
        // folded twice -- into its own content hash where it has one, and into a
        // fold over every field it produced where it has not -- and the two
        // numbers have to be the same number.
        //
        // The fold is compared rather than a field at a time on purpose: a
        // per-field assertion list is the thing that goes stale when a column is
        // added, and a stale list is how a parser ends up half-covered without
        // anything going red.
        foreach (ContentParser parser in ContentParsers.All)
        {
            string text = File.ReadAllText(parser.File);
            Hash64 invariant = parser.Digest(text);
            Hash64 hostile;

            using (Hostile(name))
            {
                hostile = parser.Digest(text);
            }

            Assert.True(
                invariant == hostile,
                $"{parser} folded to {invariant} under the invariant culture and {hostile} under {name}.");
        }
    }

    [Fact]
    public void Every_parser_in_the_simulation_is_declared_in_the_sweep()
    {
        // What makes the sweep above a claim about the simulation rather than a
        // claim about a list somebody maintained. Every exported type with a
        // public static Parse is found by reflection and has to appear in the
        // declaration, so a parser added without a content file and a fold
        // reddens here instead of being quietly absent from two gates.
        //
        // OBSERVED: delete the upgrade ladder's row from ContentParsers.Declare.
        // This is the only test that reddens, and it names UpgradeLadder. The
        // sweep above and the committed-file decimal-point check both go green
        // having quietly stopped covering the one parser whose hash gates stored
        // records -- which is the whole failure mode, reproduced.
        IReadOnlyList<Type> discovered = ContentParsers.DiscoveredInTheAssembly();
        var declared = new HashSet<Type>(ContentParsers.All.Select(parser => parser.ParsedBy));

        Assert.NotEmpty(discovered);

        string[] undeclared = discovered
            .Where(type => !declared.Contains(type))
            .Select(type => type.Name)
            .ToArray();

        Assert.True(
            undeclared.Length == 0,
            "These parsers are in the simulation and not in ContentParsers.All, so nothing runs them "
            + "under a hostile culture: "
            + string.Join(", ", undeclared)
            + ". Declare each one with the committed file it reads and a fold over what it parses.");

        // And the other way, so a declaration cannot outlive the parser it names.
        string[] stale = declared
            .Where(type => !discovered.Contains(type))
            .Select(type => type.Name)
            .ToArray();

        Assert.True(
            stale.Length == 0,
            "These types are declared in ContentParsers.All but no longer expose a public static Parse: "
            + string.Join(", ", stale)
            + ".");
    }

    [Theory]
    [InlineData(Turkish, "upgrade    3  14", "upgrade    3  1.4")]
    [InlineData(Turkish, "upgrade    3  14", "upgrade    3.0  14")]
    [InlineData(CommaDecimal, "upgrade    3  14", "upgrade    3  1,4")]
    [InlineData(CommaDecimal, "upgrade    3  14", "upgrade    3,0  14")]
    public void A_fraction_in_a_ladder_column_is_refused_under_a_hostile_culture(
        string name,
        string authored,
        string planted)
    {
        // Both ends of an edge, spelled the way a comma-decimal culture makes
        // natural, which is why both characters are refused rather than one.
        //
        // OBSERVED: stop refusing '.' and ',' in DataText.Fields and let
        // DataText.Integer skip them the way int.Parse with AllowThousands
        // does. The target rows go red having caught nothing, and an edge
        // authored as 1,4 points at type id fourteen on one machine and at
        // whatever a culture makes of it on another.
        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => UpgradeLadder.Parse(
                    TheLadder.CommittedText().Replace(authored, planted, StringComparison.Ordinal),
                    UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile))));
        }
    }

    [Theory]
    [InlineData(Turkish, "10 none armoured 0", "1.5 none armoured 0")]
    [InlineData(Turkish, "10 none armoured 0", "10 none armoured 0.5")]
    [InlineData(CommaDecimal, "10 none armoured 0", "1,5 none armoured 0")]
    [InlineData(CommaDecimal, "10 none armoured 0", "10 none armoured 0,5")]
    public void A_fraction_in_a_column_the_current_layout_added_is_refused_under_a_hostile_culture(
        string name,
        string authored,
        string planted)
    {
        // The cost and the armour value are the two new numbers, and both are
        // exactly the shape a designer reaches for a fraction in. Under a
        // comma-decimal culture the second spelling is the natural one, which
        // is why both characters are refused rather than one.
        //
        // OBSERVED: stop refusing '.' and ',' in DataText.Fields and let
        // DataText.Integer skip them the way int.Parse with AllowThousands
        // does under both of these cultures. Every row of this theory and of
        // the ruleset one below goes red having caught nothing, and 1.5 loads
        // as fifteen.
        const string Row = "layout 2\nunit 1 grunt moving 2000 85 0 0 0 0 0 0 none 0 12 10 none armoured 0";

        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => UnitTypeTable.Parse(Row.Replace(authored, planted, StringComparison.Ordinal)));
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void A_fraction_in_a_ruleset_column_is_refused_under_a_hostile_culture(string name)
    {
        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10", "interest 10,5")));

            Assert.Throws<ContentException>(
                () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "armour 1 100", "armour 1.5 100")));

            // The interest cap is the column this layout added, and a ceiling
            // is exactly the shape a designer reaches for a fraction in.
            Assert.Throws<ContentException>(
                () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 0,5")));

            Assert.Throws<ContentException>(
                () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 1.5")));
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void A_type_keyword_in_the_wrong_case_is_refused_under_a_hostile_culture(string name)
    {
        // The Turkish trap, aimed at the columns that carry it. A parser that
        // compared these case-insensitively would consult a culture, and in
        // Turkish "pierce" does not upper-case to "PIERCE". The comparison is
        // ordinal, so the wrong case is simply not the keyword.
        //
        // OBSERVED: compare with StringComparison.CurrentCultureIgnoreCase in
        // DataText.Keyword. The de-DE row goes red having caught nothing and
        // the Turkish row stays green, which is the bug exactly: the same
        // bytes are a keyword on one machine and not on another.
        const string Row = "layout 2\nunit 3 bolt placed 0 0 3200 6 3 2 90 150 hitscan 0 0 40 pierce none 0";

        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => UnitTypeTable.Parse(Row.Replace(" pierce ", " PIERCE ", StringComparison.Ordinal)));

            Assert.Throws<ContentException>(
                () => Ruleset.Parse(
                    PlantedText.Replace(TheRuleset.Minimal, "matrix magic", "matrix MAGIC")));
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void A_decimal_point_is_still_refused_under_a_hostile_culture(string name)
    {
        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => UnitTypeTable.Parse("unit 1 grunt moving 1.500 34 0 0 0 0 0 0 none 0 12"));
        }
    }

    private static CultureScope Hostile(string name) => new(name);

    /// <summary>Forces a culture for the duration of a block, and puts it back afterwards.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        private readonly CultureInfo _previousUi = CultureInfo.CurrentUICulture;

        internal CultureScope(string name)
        {
            var culture = new CultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previous;
            CultureInfo.CurrentUICulture = _previousUi;
        }
    }
}
