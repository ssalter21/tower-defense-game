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
/// <b>One table, four kinds of row, and the kind is the first column.</b> A
/// spreadsheet filters on it and gets four clean tables; a reader that ignores
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
/// <item><c>run</c> -- the population itself, one row a run, present only where
/// the sweep was asked for it. Same headings as the creep row it belongs to, so
/// grouping these lands on that one.</item>
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

        // The runs go under the rows they were folded into rather than
        // interleaved with them, so a reader who wants the folded table alone
        // has it whole before the long tail starts.
        for (int index = 0; index < report.EveryRun.Count; index++)
        {
            RunRow(text, report.EveryRun[index]);
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
            "wall",
            "what the OPPONENTS' towers were made of -- every creep meets every wall and reports a row "
            + "against each of them so two rows are comparable when this column matches and not otherwise; "
            + "the matrix is authored so that no attack type is globally better -- pierce takes 140% off a "
            + "swift body where magic takes 140% off an armoured one -- so a zero in this file is a hard "
            + "counter rather than a weak creep; see docs/adr/0058 and #242");

        Note(
            text,
            "defense_gold",
            "the defense these rows were played against was built by a deliberately simple bot -- the tower "
            + "that covers the most unshot route per gold and then whichever upgrade or second tower scores "
            + "the most damage over the route per gold above what already stands there -- so a row describes "
            + "a game and never skilled play; see #163 and #236 -- and since #242 that bot is restricted to "
            + "one attack type per wall so it buys the best tower of that type rather than the best tower");

        Note(
            text,
            "cost_efficiency_dealt_per_100_gold",
            "a cost-weighted leak rate and never a price -- a leak charges what the creep cost one for one "
            + "so the price cancels out; see docs/adr/0041");

        Note(
            text,
            "dealt_gold",
            "a slot's position is the order its creeps walk out in since #191 -- and a row here fills one "
            + "slot a round because a row is about one creep -- so nothing in this report varies with the "
            + "arrangement of a wave and no ordering question can be answered from it");
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
    /// The player is on it for the same reason and it is the strongest case of
    /// all -- two reports played by different strategies share every other
    /// parameter, and every number below them differs.
    /// </remarks>
    private static void Parameters(StringBuilder text, SweepPlan plan)
    {
        Parameter(text, "waves", Number(plan.Waves));
        Parameter(text, "field_size", Number(plan.FieldSize));
        Parameter(text, "death_ends_the_run", plan.DeathEndsTheRun ? Yes : No);
        Parameter(text, "free_snapshots", Number(plan.Rules.FreeSnapshotsPerRun));
        Parameter(text, "snapshot_price", Number(plan.Rules.SnapshotPriceGold));
        Parameter(text, "first_seed", plan.FirstSeed.ToString(PlainText.Culture));
        Parameter(text, "runs_per_creep", Number(plan.RunsPerCreep));
        Parameter(text, "policy", plan.PolicyName);

        // The walls, named in the order the rows are written, so a reader who
        // filters the file down to one wall can still see which others were
        // played -- and a report swept against one wall cannot be mistaken for
        // this one.
        Parameter(text, "walls", Walls(plan));

        Parameter(text, "ruleset_hash", plan.Rules.ContentHash.ToString());
    }

    /// <summary>
    /// The wall names, separated by a character a cell may carry.
    /// </summary>
    /// <remarks>
    /// A space and not a comma: a comma would shift every column of every row
    /// below this one, and <see cref="CsvRow"/> refuses one rather than quoting
    /// it. There is a coverage row saying how many there were, so this is the
    /// naming and not the count.
    /// </remarks>
    private static string Walls(SweepPlan plan)
    {
        var names = new string[plan.Walls.Count];

        for (int index = 0; index < plan.Walls.Count; index++)
        {
            names[index] = plan.Walls[index].Name;
        }

        return string.Join(" ", names);
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

    /// <summary>
    /// The columns a population fills whatever its depth, filled in.
    /// </summary>
    /// <remarks>
    /// <b>One writer for both kinds of row, because they are one population
    /// counted twice.</b> A creep row is the fold and a run row is a population
    /// of one, so the file's promise is that grouping the second lands on the
    /// first -- and two copies of this list are two chances for one of them to
    /// gain a column the other does not have. What each kind adds on top of
    /// these is its own: the two rates for a fold, the seed for a run.
    /// </remarks>
    private static CsvRow Population(
        string kind,
        string label,
        string wall,
        long runs,
        long rounds,
        long wins,
        long dealt,
        long taken,
        long spent,
        long defense,
        long unspent,
        long incomeBase,
        long bonus) =>
        new CsvRow()
            .With("kind", kind)
            .With("subject", label)
            .With("wall", wall)
            .With("runs", Number(runs))
            .With("rounds", Number(rounds))
            .With("wins", Number(wins))
            .With("dealt_gold", Number(dealt))
            .With("taken_gold", Number(taken))
            .With("spent_gold", Number(spent))
            .With("defense_gold", Number(defense))
            .With("unspent_gold", Number(unspent))
            .With("income_base_gold", Number(incomeBase))
            .With("bonus_gold", Number(bonus));

    /// <summary>One creep over its whole population of runs, and the two rates that fold gives.</summary>
    private static void Creep(StringBuilder text, SweepRow row) =>
        Row(
            text,
            Population(
                "creep",
                row.Label,
                row.Wall,
                runs: row.Runs,
                rounds: row.Rounds,
                wins: row.Wins,
                dealt: row.LeakCostDealt,
                taken: row.LeakCostTaken,
                spent: row.GoldSpent,
                defense: row.DefenseGold,
                unspent: row.UnspentGold,
                incomeBase: row.IncomeBaseGold,
                bonus: row.BonusGold)
                .With("win_rate_bp", Number(row.WinRateBasisPoints))
                .With("cost_efficiency_dealt_per_100_gold", Number(row.DealtPerHundredGold)));

    /// <summary>
    /// One run, under the same headings as the row it was folded into.
    /// </summary>
    /// <remarks>
    /// A population of one: it reports one run and either one win or none. The
    /// two rate columns stay blank -- see <see cref="SweepColumns"/> -- and the
    /// seed is the column only this kind of row fills.
    /// </remarks>
    private static void RunRow(StringBuilder text, SweepRunRow row) =>
        Row(
            text,
            Population(
                "run",
                row.Label,
                row.Wall,
                runs: 1,
                rounds: row.Rounds,
                wins: row.Won ? 1 : 0,
                dealt: row.LeakCostDealt,
                taken: row.LeakCostTaken,
                spent: row.GoldSpent,
                defense: row.DefenseGold,
                unspent: row.UnspentGold,
                incomeBase: row.IncomeBaseGold,
                bonus: row.BonusGold)
                .With("seed", row.Seed.ToString(PlainText.Culture)));

    private static string Number(long value) => value.ToString(PlainText.Culture);

    /// <summary>One row, and the newline that ends it.</summary>
    private static void Row(StringBuilder text, CsvRow row) => text.Append(row.Line).Append('\n');
}
