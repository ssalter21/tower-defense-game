namespace Sim.Cli;

/// <summary>
/// The columns of the sweep report, in the order they are written.
/// </summary>
/// <remarks>
/// <para>
/// <b>The order is declared once and every row is keyed on it.</b> A row that
/// counted its way to a column would be a second copy of this list, and the
/// failure that has is the quiet one: it compiles, it writes a file of the right
/// width, and it puts a number under the wrong heading. Adding a column is a row
/// here.
/// </para>
/// <para>
/// <b>The gate tests compile this very file.</b> <c>sim.tests</c> links it as a
/// source rather than restating the order -- see the <c>Compile</c> item in
/// <c>Sim.Tests.csproj</c> -- because a test that knows where a column sits by
/// counting is another copy of the list, and the test project cannot reference
/// the runner: it exercises the command line as a process.
/// </para>
/// </remarks>
internal static class SweepColumns
{
    /// <summary>
    /// The columns, in order. <c>value</c>, <c>of</c> and <c>bounded</c> are the
    /// three the parameter and coverage rows use; every other column belongs to
    /// a creep row.
    /// </summary>
    private static readonly string[] Declared =
    {
        "kind",
        "subject",
        "ingredients",
        "runs",
        "rounds",
        "wins",
        "win_rate_bp",
        "dealt_gold",
        "taken_gold",
        "spent_gold",
        "defense_gold",
        "unspent_gold",
        "cost_efficiency_dealt_per_100_gold",
        "income_base_gold",
        "bonus_gold",
        "value",
        "of",
        "bounded",
    };

    /// <summary>How many columns a row of the report has.</summary>
    public static int Count => Declared.Length;

    /// <summary>The header: a row whose every cell is its own column's name.</summary>
    public static CsvRow Header()
    {
        var header = new CsvRow();

        for (int index = 0; index < Declared.Length; index++)
        {
            header.With(Declared[index], Declared[index]);
        }

        return header;
    }

    /// <summary>Where a column sits in a row, or a refusal naming the ones there are.</summary>
    public static int IndexOf(string column)
    {
        for (int index = 0; index < Declared.Length; index++)
        {
            if (Declared[index] == column)
            {
                return index;
            }
        }

        throw new ArgumentException(
            "The sweep report has no column called '"
            + column
            + "'. Its columns are: "
            + string.Join(", ", Declared)
            + ".");
    }
}

/// <summary>
/// One row of the sweep report, filled in a column at a time.
/// </summary>
/// <remarks>
/// <para>
/// <b>A column nobody names stays blank.</b> That is what the three kinds of row
/// are: a parameter row is two headings and a number, and the run of empty cells
/// that leaves in the file is the file's shape rather than something a writer
/// counts out.
/// </para>
/// <para>
/// <b>Nothing here computes a cell.</b> A row is handed text and files it under
/// a heading; every number in the report is the harness's. See
/// <see cref="SweepCsv"/>.
/// </para>
/// </remarks>
internal sealed class CsvRow
{
    private readonly string[] _cells;

    internal CsvRow()
    {
        _cells = new string[SweepColumns.Count];

        for (int index = 0; index < _cells.Length; index++)
        {
            _cells[index] = string.Empty;
        }
    }

    /// <summary>The cells, in the declared order, blank wherever no column was named.</summary>
    public IReadOnlyList<string> Cells => _cells;

    /// <summary>The row as one line of the file, without its newline.</summary>
    public string Line => string.Join(",", _cells);

    /// <summary>Files one value under one column, and hands the row back to be filled in further.</summary>
    public CsvRow With(string column, string value)
    {
        _cells[SweepColumns.IndexOf(column)] = Cell(value);

        return this;
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
