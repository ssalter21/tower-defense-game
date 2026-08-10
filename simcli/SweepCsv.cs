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
/// obvious way is wrong. One a column, in the order the columns are declared,
/// and the remarks on <see cref="Notes"/> say why they travel with the file
/// rather than living beside it.</item>
/// <item><c>coverage</c> -- how far the sweep reached on each axis it could
/// have bounded. <b>Always present, bounded or not</b>, because a truncated
/// sweep that said nothing would read exactly like a complete one.</item>
/// <item><c>creep</c> -- the rows. One a creep over its whole population, then
/// one a creep per ingredient count that occurred.</item>
/// </list>
/// <para>
/// <b>Rows are assembled by naming their columns.</b> The order lives in
/// <see cref="SweepColumns"/>, a row fills in the columns it has something for,
/// and every column it does not name comes out blank -- so no writer below
/// counts cells to reach a heading.
/// </para>
/// <para>
/// <b>No cell is ever quoted, and that is enforced rather than assumed.</b>
/// Every value written here is an integer formatted under the invariant culture
/// or a label off a content file, and a content file's parser refuses a comma on
/// a data line before it tokenises -- so a comma cannot reach a cell. If one
/// ever does, <see cref="CsvRow"/> refuses rather than writing a file whose
/// columns have silently shifted by one from some row downwards.
/// </para>
/// </remarks>
internal static class SweepCsv
{
    /// <summary>What a <c>yes</c>/<c>no</c> column says.</summary>
    private const string Yes = "yes";

    private const string No = "no";

    /// <summary>The whole file, header row first, ending in a newline.</summary>
    public static string Of(SweepReport report)
    {
        var text = new StringBuilder();

        Row(text, SweepColumns.Header());
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
    /// <b>They travel in the file rather than beside it</b> because this file is
    /// what somebody opens six months from now in a spreadsheet, and both of the
    /// columns below invite exactly the reading the note exists to prevent. A
    /// caveat that lives in a document nobody opened is a caveat nobody read.
    /// </para>
    /// <para>
    /// The defense column is the gold a scripted bot put on the board -- see
    /// <see cref="CoverThenUpgradeBot"/> -- so what every row of this report was
    /// played against is one simple rule rather than a person, and a row is a
    /// statement about a game rather than about skilled play.
    /// </para>
    /// <para>
    /// Under a one-for-one leak charge the price level cancels out of leak cost
    /// dealt over gold spent exactly -- halve a creep's price and a purse buys
    /// twice as many while each leak charges half -- so the cost-efficiency
    /// column is the cost-weighted leak rate of what was sent and it cannot say
    /// a creep is overpriced. That is measured rather than argued, in
    /// docs/research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md.
    /// </para>
    /// <para>
    /// <b>The sentences carry no comma</b>, because a cell that did would be
    /// refused by <see cref="CsvRow"/> rather than quoted.
    /// </para>
    /// </remarks>
    private static void Notes(StringBuilder text)
    {
        Note(
            text,
            "defense_gold",
            "the defense these rows were played against was built by a deliberately simple bot -- cheapest "
            + "tower that covers unshot route then upgrade the oldest -- so a row describes a game and "
            + "never skilled play; see #145");

        Note(
            text,
            "cost_efficiency_dealt_per_100_gold",
            "a cost-weighted leak rate and never a price -- a leak charges what the creep cost one for one "
            + "so the price cancels out; see docs/adr/0041");
    }

    private static void Note(StringBuilder text, string column, string what) =>
        Row(
            text,
            new CsvRow()
                .With("kind", "note")
                .With("subject", column)
                .With("value", what));

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
        Parameter(text, "snapshot_price", Number(plan.Rules.SnapshotPriceGold));
        Parameter(text, "first_seed", plan.FirstSeed.ToString(PlainText.Culture));
        Parameter(text, "runs_per_creep", Number(plan.RunsPerCreep));
        Parameter(text, "ruleset_hash", plan.Rules.ContentHash.ToString());
    }

    private static void Parameter(StringBuilder text, string name, string value) =>
        Row(
            text,
            new CsvRow()
                .With("kind", "parameter")
                .With("subject", name)
                .With("value", value));

    /// <summary>
    /// One axis and what the sweep covered of it. The population is named only
    /// where it is a number: an axis nothing enumerates leaves that column
    /// blank by not filling it in.
    /// </summary>
    private static void Coverage(StringBuilder text, CoverageBound bound)
    {
        CsvRow row = new CsvRow()
            .With("kind", "coverage")
            .With("subject", bound.Axis)
            .With("value", Number(bound.Covered))
            .With("bounded", bound.IsBounded ? Yes : No);

        if (bound.Available != CoverageBound.Unbounded)
        {
            row.With("of", Number(bound.Available));
        }

        Row(text, row);
    }

    private static void Creep(StringBuilder text, SweepRow row) =>
        Row(
            text,
            new CsvRow()
                .With("kind", "creep")
                .With("subject", row.Label)
                .With("ingredients", Number(row.Ingredients))
                .With("runs", Number(row.Runs))
                .With("rounds", Number(row.Rounds))
                .With("wins", Number(row.Wins))
                .With("win_rate_bp", Number(row.WinRateBasisPoints))
                .With("dealt_gold", Number(row.LeakCostDealt))
                .With("taken_gold", Number(row.LeakCostTaken))
                .With("spent_gold", Number(row.GoldSpent))
                .With("defense_gold", Number(row.DefenseGold))
                .With("unspent_gold", Number(row.UnspentGold))
                .With("cost_efficiency_dealt_per_100_gold", Number(row.DealtPerHundredGold))
                .With("income_base_gold", Number(row.IncomeBaseGold))
                .With("bonus_gold", Number(row.BonusGold)));

    private static string Number(long value) => value.ToString(PlainText.Culture);

    /// <summary>One row, and the newline that ends it.</summary>
    private static void Row(StringBuilder text, CsvRow row) => text.Append(row.Line).Append('\n');
}
