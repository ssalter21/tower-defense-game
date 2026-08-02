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
