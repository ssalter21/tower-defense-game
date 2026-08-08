namespace Sim.Tests;

/// <summary>
/// The committed anchor schedule, and the smallest well-formed one text can be
/// planted into.
/// </summary>
/// <remarks>
/// The tests open the file and hand the simulation text, exactly as
/// <see cref="TheRuleset"/> and <see cref="TheMatch"/> do: the simulation never
/// learns a path exists.
/// </remarks>
public static class TheSchedule
{
    /// <summary>
    /// A complete shape with two anchors and two tier pools, laid out so that a
    /// test can plant text into any single rule without disturbing another.
    /// Unit 3 is the committed table's placed bolt; units 1 and 2 are its two
    /// walkers.
    /// </summary>
    public const string Minimal = """
        anchor 3 1 plain 3 1
        anchor 6 2 steep 3 5
        changer 1 early-a 1 1 0
        changer 2 early-b 1 2 0
        changer 3 late-a 2 1 400
        changer 4 late-b 2 2 400
        """;

    /// <summary>The committed file, as text.</summary>
    public static string CommittedText() => File.ReadAllText(RepoLayout.ScheduleFile);

    /// <summary>The committed file, parsed against the committed unit table.</summary>
    public static AnchorSchedule Committed(UnitTypeTable? types = null) =>
        AnchorSchedule.Parse(CommittedText(), types ?? TheMatch.Types());

    /// <summary>
    /// The committed shape with its second anchor moved from wave six to wave
    /// five, and nothing else touched. A rotation away from the committed one.
    /// </summary>
    public static AnchorSchedule Reshaped(UnitTypeTable? types = null) =>
        AnchorSchedule.Parse(
            PlantedText.Replace(CommittedText(), "anchor        6     2", "anchor        5     2"),
            types ?? TheMatch.Types());

    /// <summary><see cref="Minimal"/>, parsed against the committed unit table.</summary>
    public static AnchorSchedule Small() => AnchorSchedule.Parse(Minimal, TheMatch.Types());

    /// <summary>Any schedule text, parsed against the committed unit table.</summary>
    public static AnchorSchedule Of(string text) => AnchorSchedule.Parse(text, TheMatch.Types());

    /// <summary><see cref="Minimal"/> with one substring swapped for another.</summary>
    public static string Planted(string what, string with) => PlantedText.Replace(Minimal, what, with);

    /// <summary>
    /// The slot width of every wave of a run this long, derived from a shape and
    /// the ruleset's widening step.
    /// </summary>
    public static int[] Widths(AnchorSchedule schedule, int waves)
    {
        Ruleset rules = TheRuleset.Committed();
        var widths = new int[waves];

        for (int wave = 1; wave <= waves; wave++)
        {
            widths[wave - 1] = schedule.WaveSlotsAt(rules, wave);
        }

        return widths;
    }
}
