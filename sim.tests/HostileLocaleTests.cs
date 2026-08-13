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
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void Every_column_the_current_layout_added_parses_identically_under_a_hostile_culture(string name)
    {
        // The four columns the unit schema gained: a cost, two keywords matched
        // ordinally and an armour value. Two of them are numbers a culture
        // could reinterpret and two are words a culture could case-fold
        // differently -- in Turkish, "pierce" does not round-trip through an
        // upper-casing, which is why the keyword match is ordinal.
        string units = File.ReadAllText(RepoLayout.UnitsFile);
        UnitTypeTable invariant = UnitTypeTable.Parse(units);
        UnitTypeTable hostile;

        using (Hostile(name))
        {
            hostile = UnitTypeTable.Parse(units);
        }

        Assert.Equal(invariant.ContentHash, hostile.ContentHash);
        Assert.Equal(invariant.Layout, hostile.Layout);

        for (int index = 0; index < invariant.Count; index++)
        {
            Assert.Equal(invariant.Types[index].Cost, hostile.Types[index].Cost);
            Assert.Equal(invariant.Types[index].AttackType, hostile.Types[index].AttackType);
            Assert.Equal(invariant.Types[index].ArmourType, hostile.Types[index].ArmourType);
            Assert.Equal(invariant.Types[index].Armour, hostile.Types[index].Armour);
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void The_whole_ruleset_parses_and_hashes_identically_under_a_hostile_culture(string name)
    {
        // Every number the rules are made of, read under a culture chosen to
        // break the parse. The matrix cells, the armour expression, the floor,
        // the interest rate and its ceiling, the income base, the bands, the
        // health pool, the slot widths, the offering and the snapshot price are
        // all integers a framework parser would consult a culture about.
        string text = File.ReadAllText(RepoLayout.RulesetFile);
        Ruleset invariant = Ruleset.Parse(text);
        Ruleset hostile;

        using (Hostile(name))
        {
            hostile = Ruleset.Parse(text);
        }

        Assert.Equal(invariant.ContentHash, hostile.ContentHash);
        Assert.Equal(invariant.Matrix.Cells, hostile.Matrix.Cells);
        Assert.Equal(invariant.ArmourPercentPerPoint, hostile.ArmourPercentPerPoint);
        Assert.Equal(invariant.ArmourDenominator, hostile.ArmourDenominator);
        Assert.Equal(invariant.DamageFloor, hostile.DamageFloor);
        Assert.Equal(invariant.InterestPercentPerWave, hostile.InterestPercentPerWave);
        Assert.Equal(invariant.InterestCapGold, hostile.InterestCapGold);
        Assert.Equal(invariant.IncomeBasePerWave, hostile.IncomeBasePerWave);
        Assert.Equal(invariant.HealthPoolGold, hostile.HealthPoolGold);
        Assert.Equal(invariant.FreeSnapshotsPerRun, hostile.FreeSnapshotsPerRun);
        Assert.Equal(invariant.SnapshotPriceGold, hostile.SnapshotPriceGold);

        Assert.Equal(invariant.Bands.Count, hostile.Bands.Count);

        for (int index = 0; index < invariant.Bands.Count; index++)
        {
            Assert.Equal(invariant.Bands[index].PercentileThreshold, hostile.Bands[index].PercentileThreshold);
            Assert.Equal(invariant.Bands[index].BonusPercentOfBase, hostile.Bands[index].BonusPercentOfBase);
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void The_whole_command_script_parses_identically_under_a_hostile_culture(string name)
    {
        // Four numeric columns a framework parser would consult a culture about
        // -- the wave, the take id, and a type id and a count per slot -- and a
        // fifth that is a keyword a culture could case-fold differently.
        //
        // The decisions are compared rather than a hash, because a script has
        // no hash of its own: what it becomes is a record, and a record's
        // stamps are hashes of the tables rather than of the decisions. The
        // printed form is compared beside the values because that is what a
        // committed outcome file is made of, and a number formatted under a
        // culture would move that file without moving the run.
        string text = File.ReadAllText(RepoLayout.CommandScriptFile);
        IReadOnlyList<RecordCommand> invariant = CommandScript.Parse(text);
        IReadOnlyList<RecordCommand> hostile;

        using (Hostile(name))
        {
            hostile = CommandScript.Parse(text);
        }

        Assert.Equal(invariant.Count, hostile.Count);

        for (int index = 0; index < invariant.Count; index++)
        {
            Assert.Equal(invariant[index], hostile[index]);
            Assert.Equal(invariant[index].ToString(), hostile[index].ToString());
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void The_whole_ladder_parses_and_hashes_identically_under_a_hostile_culture(string name)
    {
        // Every column an edge is made of, read under a culture chosen to break
        // the parse. Both are integers a framework parser would consult a
        // culture about -- the source id and the target id -- and the layout
        // line above them is a third.
        //
        // This stands where the anchor schedule's pair stood. It matters more
        // than it did: #179 handed the ladder to the simulation, so an edge
        // that parsed differently under one culture is a run that refuses a
        // different placement on one machine than on another.
        string text = File.ReadAllText(RepoLayout.UpgradesFile);
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        UpgradeLadder invariant = UpgradeLadder.Parse(text, types);
        UpgradeLadder hostile;

        using (Hostile(name))
        {
            hostile = UpgradeLadder.Parse(text, types);
        }

        Assert.Equal(invariant.ContentHash, hostile.ContentHash);
        Assert.Equal(invariant.Layout, hostile.Layout);
        Assert.Equal(invariant.Edges.Count, hostile.Edges.Count);

        for (int index = 0; index < invariant.Edges.Count; index++)
        {
            Assert.Equal(invariant.Edges[index].From, hostile.Edges[index].From);
            Assert.Equal(invariant.Edges[index].To, hostile.Edges[index].To);
        }
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
    public void The_whole_canned_field_parses_identically_under_a_hostile_culture(string name)
    {
        // The wave the sweep's field sends, read under a culture chosen to break
        // the parse. Four numeric columns a framework parser would consult a
        // culture about -- the tick, the type id, the count and the corridor --
        // in a file every row of the balance report is measured against.
        string text = File.ReadAllText(RepoLayout.FieldFile);
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        WaveScript invariant = WaveScript.Parse(text, types);
        WaveScript hostile;

        using (Hostile(name))
        {
            hostile = WaveScript.Parse(text, types);
        }

        Assert.Equal(invariant.Count, hostile.Count);
        Assert.Equal(invariant.TotalUnits, hostile.TotalUnits);

        for (int index = 0; index < invariant.Count; index++)
        {
            Assert.Equal(invariant.Orders[index].TickOffset, hostile.Orders[index].TickOffset);
            Assert.Equal(invariant.Orders[index].TypeId, hostile.Orders[index].TypeId);
            Assert.Equal(invariant.Orders[index].Count, hostile.Orders[index].Count);
        }
    }

    [Fact]
    public void Not_one_number_in_the_committed_report_carries_a_group_separator()
    {
        // A COMMA DECIMAL SEPARATOR AGAINST A COMMA-DELIMITED FILE is the trap
        // this repository tests for, and the balance report is the first file it
        // has produced where the two meet. Under de-DE a framework formatter
        // renders 15836 as "15.836" and, with a group separator configured the
        // other way, as "15,836" -- which is a cell that has quietly become two
        // and every column from there rightwards shifted by one.
        //
        // What the writer does instead is format under the invariant culture and
        // refuse any cell carrying a separator at all. This is that claim as an
        // observation about the committed file: every cell is digits, a word, or
        // empty, and nothing in it is a formatted number.
        //
        // OBSERVED: format the report's numbers as "N0" in SweepCsv.Number,
        // which is the grouped spelling any culture's own formatter reaches for.
        // tools/run-sweep.ps1 -Regenerate refuses by name on the first
        // four-figure cell -- "A sweep cell reads '2,500'" -- and writes nothing
        // at all, which is why the committed file cannot arrive in that state.
        //
        // OBSERVED: doctor the committed file instead, so that a cell that got
        // past a writer is what is under test. "2500" spelled "2.500" reddens
        // the decimal-point assertion; spelled "2,500" it reddens the column
        // count, 15 against the header's 14 -- which is the same number written
        // by de-DE and by en-US, and the whole reason both are refused.
        string[] rows = File.ReadAllText(RepoLayout.SweepFile)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (int index = 0; index < rows.Length; index++)
        {
            string[] cells = rows[index].Split(',');

            Assert.Equal(rows[0].Split(',').Length, cells.Length);

            for (int cell = 0; cell < cells.Length; cell++)
            {
                Assert.DoesNotContain('.', cells[cell]);
                Assert.DoesNotContain('"', cells[cell]);
            }
        }
    }

    [Fact]
    public void The_turkish_culture_is_really_in_effect_and_really_is_hostile()
    {
        // Without this the test above could be passing because globalization is
        // switched off in this build, which would make it a check that cannot
        // fail. The Turkish trap is specifically what would bite a parser that
        // compared keywords case-insensitively: "hitscan" uppercases to
        // "H\u0130TSCAN" here, with a dotted capital that is not 'I'.
        using (Hostile(Turkish))
        {
            Assert.Equal("H\u0130TSCAN", "hitscan".ToUpper(CultureInfo.CurrentCulture));
            Assert.NotEqual("HITSCAN", "hitscan".ToUpper(CultureInfo.CurrentCulture));
        }

        Assert.Equal("HITSCAN", "hitscan".ToUpper(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void The_comma_decimal_culture_is_really_in_effect_and_really_is_hostile()
    {
        using (Hostile(CommaDecimal))
        {
            Assert.Equal(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            // The bug this culture would cause, demonstrated on the framework
            // parser the simulation deliberately does not use: 1.500 is one and
            // a half in one place and fifteen hundred in another, from the same
            // bytes.
            Assert.Equal(1500, int.Parse("1.500", NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture));
            Assert.Throws<FormatException>(
                () => int.Parse("1.500", NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture));
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
