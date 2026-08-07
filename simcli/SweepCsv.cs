using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// A sweep's rows as a comma-separated file: the half of the harness that
/// writes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here computes anything.</b> The harness returns rows and this
/// turns them into text, which is the split the simulation's no-filesystem rule
/// forces and the reason the whole effort needs one behavioural seam. A number
/// that appears here and nowhere in <see cref="Sweep"/> would be a rule living
/// in the shell.
/// </para>
/// <para>
/// <b>One table, three kinds of row, and the kind is the first column.</b> A
/// spreadsheet filters on it and gets three clean tables; a reader that ignores
/// it still sees every number.
/// </para>
/// <list type="bullet">
/// <item><c>parameter</c> -- what the sweep was played under, one row a number,
/// so that two sweeps concatenated cannot be mistaken for one.</item>
/// <item><c>note</c> -- what a column does not mean, where reading it the
/// obvious way is wrong. There is one, and the remarks on <see cref="Notes"/>
/// say why it travels with the file rather than living beside it.</item>
/// <item><c>coverage</c> -- how far the sweep reached on each axis it could
/// have bounded. <b>Always present, bounded or not</b>, because a truncated
/// sweep that said nothing would read exactly like a complete one.</item>
/// <item><c>creep</c> -- the rows. One a creep over its whole population, then
/// one a creep per ingredient count that occurred.</item>
/// </list>
/// <para>
/// <b>No cell is ever quoted, and that is enforced rather than assumed.</b>
/// Every value written here is an integer formatted under the invariant culture
/// or a label off a content file, and a content file's parser refuses a comma on
/// a data line before it tokenises -- so a comma cannot reach a cell. If one
/// ever does, this refuses rather than writing a file whose columns have
/// silently shifted by one from some row downwards.
/// </para>
/// </remarks>
internal static class SweepCsv
{
    /// <summary>What a <c>yes</c>/<c>no</c> column says.</summary>
    private const string Yes = "yes";

    private const string No = "no";

    /// <summary>The empty cell: a column this kind of row has no number for.</summary>
    private static readonly string Blank = string.Empty;

    /// <summary>
    /// The columns, in order. <c>value</c>, <c>of</c> and <c>bounded</c> are the
    /// three the parameter and coverage rows use; every other column belongs to
    /// a creep row.
    /// </summary>
    private static readonly string[] Columns =
    {
        "kind",
        "subject",
        "ingredients",
        "runs",
        "rounds",
        "wins",
        "win_rate_bp",
        "dealt_sauce",
        "taken_sauce",
        "spent_sauce",
        "cost_efficiency_dealt_per_100_sauce",
        "value",
        "of",
        "bounded",
    };

    /// <summary>The whole file, header row first, ending in a newline.</summary>
    public static string Of(SweepReport report)
    {
        var text = new StringBuilder();

        Row(text, Columns);
        Notes(text);
        Parameters(text, report.Plan);

        for (int index = 0; index < report.Coverage.Count; index++)
        {
            Coverage(text, report.Coverage[index]);
        }

        for (int index = 0; index < report.Rows.Count; index++)
        {
            Creep(text, report.Rows[index]);
        }

        return text.ToString();
    }

    /// <summary>
    /// What a column does not mean, where reading it the obvious way is wrong.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is exactly one, and it earns its row. Under a one-for-one leak
    /// charge the price level cancels out of leak cost dealt over sauce spent
    /// exactly -- halve a creep's price and a purse buys twice as many while
    /// each leak charges half -- so the cost-efficiency column is the
    /// cost-weighted leak rate of what was sent and it cannot say a creep is
    /// overpriced. That is measured rather than argued, in
    /// docs/research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md.
    /// </para>
    /// <para>
    /// <b>It travels in the file rather than beside it</b> because this file is
    /// what somebody opens six months from now in a spreadsheet, and a column
    /// headed "cost efficiency" invites exactly the reading the research note
    /// exists to prevent. A caveat that lives in a document nobody opened is a
    /// caveat nobody read.
    /// </para>
    /// </remarks>
    private static void Notes(StringBuilder text) =>
        Note(
            text,
            "cost_efficiency_dealt_per_100_sauce",
            "a cost-weighted leak rate and never a price -- a leak charges what the creep cost one for one "
            + "so the price cancels out; see docs/adr/0041");

    private static void Note(StringBuilder text, string column, string what) =>
        Row(
            text,
            new[]
            {
                "note", column, Blank, Blank, Blank, Blank, Blank, Blank, Blank, Blank, Blank,
                what, Blank, Blank,
            });

    /// <summary>
    /// What the sweep was played under, one row a number.
    /// </summary>
    /// <remarks>
    /// The ruleset's content hash is on the list because N and K are stamped in
    /// no record and neither is a retuned dial: two sweeps that differ only in
    /// their offering ratio produce two files that look identical until this
    /// row, which is the whole reason a stored row needs the shape beside it.
    /// </remarks>
    private static void Parameters(StringBuilder text, SweepPlan plan)
    {
        Parameter(text, "waves", Number(plan.Waves));
        Parameter(text, "field_size", Number(plan.FieldSize));
        Parameter(text, "death_ends_the_run", plan.DeathEndsTheRun ? Yes : No);
        Parameter(text, "ordinary_options", Number(plan.Rules.OrdinaryOptionsPerRound));
        Parameter(text, "game_changers", Number(plan.Rules.GameChangersPerAnchor));
        Parameter(text, "free_snapshots", Number(plan.Rules.FreeSnapshotsPerRun));
        Parameter(text, "snapshot_price", Number(plan.Rules.SnapshotPriceSauce));
        Parameter(text, "first_seed", plan.FirstSeed.ToString(PlainText.Culture));
        Parameter(text, "runs_per_creep", Number(plan.RunsPerCreep));
        Parameter(text, "ruleset_hash", plan.Rules.ContentHash.ToString());
    }

    private static void Parameter(StringBuilder text, string name, string value) =>
        Row(
            text,
            new[]
            {
                "parameter", name, Blank, Blank, Blank, Blank, Blank, Blank, Blank, Blank, Blank,
                value, Blank, Blank,
            });

    private static void Coverage(StringBuilder text, CoverageBound bound) =>
        Row(
            text,
            new[]
            {
                "coverage",
                bound.Axis,
                Blank, Blank, Blank, Blank, Blank, Blank, Blank, Blank, Blank,
                Number(bound.Covered),
                bound.Available == CoverageBound.Unbounded ? Blank : Number(bound.Available),
                bound.IsBounded ? Yes : No,
            });

    private static void Creep(StringBuilder text, SweepRow row) =>
        Row(
            text,
            new[]
            {
                "creep",
                row.Label,
                Number(row.Ingredients),
                Number(row.Runs),
                Number(row.Rounds),
                Number(row.Wins),
                Number(row.WinRateBasisPoints),
                Number(row.LeakCostDealt),
                Number(row.LeakCostTaken),
                Number(row.SauceSpent),
                Number(row.DealtPerHundredSauce),
                Blank, Blank, Blank,
            });

    private static string Number(long value) => value.ToString(PlainText.Culture);

    /// <summary>One row, its cells checked and its newline appended.</summary>
    private static void Row(StringBuilder text, string[] cells)
    {
        for (int index = 0; index < cells.Length; index++)
        {
            if (index > 0)
            {
                text.Append(',');
            }

            text.Append(Cell(cells[index]));
        }

        text.Append('\n');
    }

    /// <summary>
    /// A cell, refused if it carries anything a reader would have to be told
    /// about.
    /// </summary>
    /// <remarks>
    /// A quoting rule is the alternative and it is the wrong one here: this file
    /// is read by a spreadsheet, by a diff and by whatever scores maps next, and
    /// the failure it is worth spending code on is the quiet one -- a stray
    /// separator shifting every column from one row downwards, which reads as a
    /// balance finding rather than as a broken file.
    /// </remarks>
    private static string Cell(string value)
    {
        if (value.IndexOf(',') < 0
            && value.IndexOf('"') < 0
            && value.IndexOf('\n') < 0
            && value.IndexOf('\r') < 0)
        {
            return value;
        }

        throw new IOException(
            "A sweep cell reads '"
            + value
            + "', which carries a separator, a quote or a line break. Every cell of this report is an "
            + "integer or a label off a content file, and a content file refuses both of those characters "
            + "on a data line -- so this is a cell that was assembled here rather than read, and writing "
            + "it would shift every column of every row below it by one.");
    }
}
