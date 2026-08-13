namespace Sim.Tests;

/// <summary>
/// The committed upgrade ladder, and the smallest well-formed one text can be
/// planted into.
/// </summary>
/// <remarks>
/// <para>
/// The tests open the file and hand the simulation text, exactly as
/// <see cref="TheRuleset"/> and <see cref="TheMatch"/> do: the simulation never
/// learns a path exists.
/// </para>
/// <para>
/// This stands where <c>TheSchedule</c> stood. A run is built against a ladder
/// now rather than against an anchor schedule, because the ladder is the one
/// prerequisite left: a unit that is some edge's target is refused to
/// <c>place</c> and reached by <c>upgrade</c>.
/// </para>
/// </remarks>
public static class TheLadder
{
    /// <summary>
    /// A ladder with no edges in it. Legal, and the shortest thing a fixture
    /// that does not care about the ladder can be handed -- nothing is any
    /// edge's target, so every placed row may be placed.
    /// </summary>
    public const string Empty = "layout 1";

    /// <summary>The committed ladder, against the roster its ids name rows of.</summary>
    public static UpgradeLadder Committed(UnitTypeTable? types = null)
    {
        UnitTypeTable table = types ?? TheMatch.Types();

        return UpgradeLadder.Parse(CommittedText(), table);
    }

    /// <summary>The committed ladder's text, for a test that plants into it.</summary>
    public static string CommittedText() =>
        System.IO.File.ReadAllText(RepoLayout.UpgradesFile);

    /// <summary>A ladder out of text the caller composed, against a roster.</summary>
    public static UpgradeLadder Of(string text, UnitTypeTable? types = null) =>
        UpgradeLadder.Parse(text, types ?? TheMatch.Types());

    /// <summary>
    /// The committed ladder with its one edge pointed somewhere else: a
    /// different ladder, of the same shape, that a stored record was not
    /// recorded against.
    /// </summary>
    /// <remarks>
    /// The mage rather than the ranger. Both are placed rows, so the ladder is
    /// still well-formed, and the run it makes refuses a different placement
    /// from the one the committed ladder refuses -- which is what a stamp on
    /// this file has to catch.
    /// </remarks>
    public static UpgradeLadder Reshaped(UnitTypeTable? types = null) =>
        Of(
            CommittedText().Replace("upgrade    3  14", "upgrade    3   4", StringComparison.Ordinal),
            types);
}
