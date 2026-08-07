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
    public void The_full_parse_and_hash_is_identical_under_a_hostile_culture(string name)
    {
        string units = File.ReadAllText(RepoLayout.UnitsFile);
        string wave = File.ReadAllText(RepoLayout.WaveFile);
        string map = File.ReadAllText(RepoLayout.MapFile);

        UnitTypeTable invariantTypes = UnitTypeTable.Parse(units);
        WaveScript invariantWave = WaveScript.Parse(wave, invariantTypes);
        HexMap invariantMap = HexMap.Parse(map);

        UnitTypeTable hostileTypes;
        WaveScript hostileWave;
        HexMap hostileMap;

        using (Hostile(name))
        {
            hostileTypes = UnitTypeTable.Parse(units);
            hostileWave = WaveScript.Parse(wave, hostileTypes);
            hostileMap = HexMap.Parse(map);
        }

        Assert.Equal(invariantTypes.ContentHash, hostileTypes.ContentHash);
        Assert.Equal(invariantMap.MapHash, hostileMap.MapHash);
        Assert.Equal(invariantWave.TotalUnits, hostileWave.TotalUnits);

        for (int index = 0; index < invariantTypes.Count; index++)
        {
            Assert.Equal(invariantTypes.Types[index].Id, hostileTypes.Types[index].Id);
            Assert.Equal(invariantTypes.Types[index].MaxHp, hostileTypes.Types[index].MaxHp);
            Assert.Equal(invariantTypes.Types[index].Delivery, hostileTypes.Types[index].Delivery);
            Assert.Equal(invariantTypes.Types[index].Role, hostileTypes.Types[index].Role);
        }

        for (int index = 0; index < invariantMap.Route.Count; index++)
        {
            Assert.Equal(invariantMap.Route[index], hostileMap.Route[index]);
        }
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
        Assert.Equal(invariant.InterestCapSauce, hostile.InterestCapSauce);
        Assert.Equal(invariant.IncomeBasePerWave, hostile.IncomeBasePerWave);
        Assert.Equal(invariant.HealthPoolSauce, hostile.HealthPoolSauce);
        Assert.Equal(invariant.StartingWaveSlots, hostile.StartingWaveSlots);
        Assert.Equal(invariant.WaveSlotsPerAnchor, hostile.WaveSlotsPerAnchor);
        Assert.Equal(invariant.OrdinaryOptionsPerRound, hostile.OrdinaryOptionsPerRound);
        Assert.Equal(invariant.GameChangersPerAnchor, hostile.GameChangersPerAnchor);
        Assert.Equal(invariant.FreeSnapshotsPerRun, hostile.FreeSnapshotsPerRun);
        Assert.Equal(invariant.SnapshotPriceSauce, hostile.SnapshotPriceSauce);

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
    public void The_whole_schedule_parses_and_hashes_identically_under_a_hostile_culture(string name)
    {
        // Every column the shape is made of, read under a culture chosen to
        // break the parse. Five of them are integers a framework parser would
        // consult a culture about -- the anchor's wave, its tier, the counter's
        // type id, the wave that counter is purchasable from, and the bonus
        // against a tag -- and the sixth is a keyword a culture could case-fold
        // differently.
        string text = File.ReadAllText(RepoLayout.ScheduleFile);
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        AnchorSchedule invariant = AnchorSchedule.Parse(text, types);
        AnchorSchedule hostile;

        using (Hostile(name))
        {
            hostile = AnchorSchedule.Parse(text, types);
        }

        Assert.Equal(invariant.ContentHash, hostile.ContentHash);
        Assert.Equal(invariant.Anchors.Count, hostile.Anchors.Count);
        Assert.Equal(invariant.GameChangers.Count, hostile.GameChangers.Count);

        for (int index = 0; index < invariant.Anchors.Count; index++)
        {
            Assert.Equal(invariant.Anchors[index].Wave, hostile.Anchors[index].Wave);
            Assert.Equal(invariant.Anchors[index].Tier, hostile.Anchors[index].Tier);
            Assert.Equal(
                invariant.Anchors[index].OpensTheSteepCounter,
                hostile.Anchors[index].OpensTheSteepCounter);
            Assert.Equal(invariant.Anchors[index].CounterTypeId, hostile.Anchors[index].CounterTypeId);
            Assert.Equal(invariant.Anchors[index].CounterFromWave, hostile.Anchors[index].CounterFromWave);
        }

        for (int index = 0; index < invariant.GameChangers.Count; index++)
        {
            Assert.Equal(invariant.GameChangers[index].Id, hostile.GameChangers[index].Id);
            Assert.Equal(invariant.GameChangers[index].Tier, hostile.GameChangers[index].Tier);
            Assert.Equal(invariant.GameChangers[index].TypeId, hostile.GameChangers[index].TypeId);
            Assert.Equal(invariant.GameChangers[index].BonusVsTag, hostile.GameChangers[index].BonusVsTag);
        }
    }

    [Theory]
    [InlineData(Turkish, "anchor 6 2 steep 3 5", "anchor 6 2 steep 3 5.0")]
    [InlineData(Turkish, "changer 3 late-a 2 1 400", "changer 3 late-a 2 1 4.00")]
    [InlineData(CommaDecimal, "anchor 6 2 steep 3 5", "anchor 6 2 steep 3 5,0")]
    [InlineData(CommaDecimal, "changer 3 late-a 2 1 400", "changer 3 late-a 2 1 4,00")]
    public void A_fraction_in_a_schedule_column_is_refused_under_a_hostile_culture(
        string name,
        string authored,
        string planted)
    {
        // The wave a counter is purchasable from and the bonus against a tag
        // are the two numbers a designer is most likely to reach for a fraction
        // in. Under a comma-decimal culture the second spelling is the natural
        // one, which is why both characters are refused rather than one.
        //
        // OBSERVED: stop refusing '.' and ',' in DataText.Fields and let
        // DataText.Integer skip them the way int.Parse with AllowThousands
        // does. The two bonus rows go red having caught nothing, and a counter
        // authored as 4,00 loads as four hundred. The two wave rows stay green
        // because 5,0 read that way is fifty, which the counter rule refuses
        // for being after the anchor -- caught, but for the wrong reason.
        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => AnchorSchedule.Parse(
                    TheSchedule.Planted(authored, planted),
                    UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile))));
        }
    }

    [Theory]
    [InlineData(Turkish)]
    [InlineData(CommaDecimal)]
    public void The_steep_keyword_in_the_wrong_case_is_refused_under_a_hostile_culture(string name)
    {
        // The schedule's one keyword column, matched ordinally like every other
        // one. A case-insensitive comparison would consult a culture, and the
        // same bytes would then be a shape on one machine and a load error on
        // another.
        //
        // OBSERVED: compare with StringComparison.CurrentCultureIgnoreCase in
        // DataText.Keyword. Both rows go red having caught nothing -- "steep"
        // carries none of the letters Turkish casing moves, so this one is the
        // plain case-folding bug rather than the dotless-i one, and a shape
        // written in the wrong case loads under both cultures.
        using (Hostile(name))
        {
            Assert.Throws<ContentException>(
                () => AnchorSchedule.Parse(
                    TheSchedule.Planted("anchor 6 2 steep 3 5", "anchor 6 2 STEEP 3 5"),
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
                () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10", "interest 10,5")));

            Assert.Throws<ContentException>(
                () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "armour 1 100", "armour 1.5 100")));

            // The interest cap is the column this layout added, and a ceiling
            // is exactly the shape a designer reaches for a fraction in.
            Assert.Throws<ContentException>(
                () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 0,5")));

            Assert.Throws<ContentException>(
                () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 1.5")));
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
                    TheRuleset.Replace(TheRuleset.Minimal, "matrix magic", "matrix MAGIC")));
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
