using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The balance report's column order, and the row that is filled in by naming
/// it.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the committed file cannot check about itself.</b>
/// <see cref="GoldenSweepTests"/> holds every row of the artefact to the width
/// its header declares, and a row of the right width with its values under the
/// wrong headings reads exactly the same to it. This is the other half: a value
/// lands under the column it was filed under and every column nobody named
/// stays empty.
/// </para>
/// <para>
/// <b>The separator refusal fires for nothing an invocation can do.</b> Every
/// cell the runner writes is an integer under the invariant culture or a label
/// off a content file, and a content file's parser refuses a comma on a data
/// line before it tokenises -- so driving the program cannot reach the guard,
/// and calling it is the only way to watch it work. That is why the writer's
/// column declaration is linked into this project as a source; see the
/// <c>Compile</c> item in <c>Sim.Tests.csproj</c>.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class SweepColumnsTests
{
    [Fact]
    public void A_row_carries_its_values_under_the_columns_they_were_filed_under()
    {
        // A coverage row is two headings and three numbers out of sixteen
        // columns, and what used to put them there was a hand-counted run of
        // blank cells in the writer. This is the property those runs were
        // spelling out, and the columns are named here out of order on purpose:
        // where a value lands is what it was filed under rather than when.
        //
        // OBSERVED: have CsvRow.With put its value in the first cell still
        // blank, which is what a row assembled in writing order comes to. The
        // header is unmoved -- it names every column, so it fills them all in
        // order either way -- and this goes red on the first cell, "yes" under
        // the heading that reads kind.
        IReadOnlyList<string> header = SweepColumns.Header().Cells;

        IReadOnlyList<string> row = new CsvRow()
            .With("bounded", "yes")
            .With("kind", "coverage")
            .Cells;

        for (int index = 0; index < header.Count; index++)
        {
            Assert.Equal(Expected(header[index]), row[index]);
        }
    }

    [Fact]
    public void A_cell_carrying_a_separator_is_refused_rather_than_quoted()
    {
        // The one failure worth spending code on here is the quiet one: a stray
        // separator shifts every column from that row downwards and the result
        // reads as a balance finding rather than as a broken file. So a cell
        // carrying one is refused outright, and so is a quote and a line break
        // of either spelling -- there is no quoting rule to fall back on.
        //
        // OBSERVED: quote the value instead of refusing it in CsvRow.Cell.
        // Every case here goes red having seen no exception at all, and a sweep
        // whose labels carried commas would write a file a spreadsheet opens
        // and a diff cannot read.
        foreach (string value in new[] { "minion, elite", "minion \"elite\"", "minion\nelite", "minion\relite" })
        {
            IOException refused = Assert.Throws<IOException>(() => new CsvRow().With("subject", value));

            Assert.Contains(value, refused.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_column_the_report_has_no_heading_for_is_refused()
    {
        // What naming a column buys over counting to one: a name nothing
        // declares is a mistake at the moment it is made rather than a value
        // written somewhere unintended.
        //
        // OBSERVED: return zero from SweepColumns.IndexOf for a name it did not
        // find. This goes red having seen no exception, and the misspelled
        // column silently overwrites the kind of every row that names it.
        ArgumentException refused =
            Assert.Throws<ArgumentException>(() => new CsvRow().With("win_rate", "8750"));

        Assert.Contains("no column called 'win_rate'", refused.Message, StringComparison.Ordinal);
        Assert.Contains("cost_efficiency_dealt_per_100_gold", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>What the row above is filled in with, by column.</summary>
    private static string Expected(string column) => column switch
    {
        "kind" => "coverage",
        "bounded" => "yes",
        _ => string.Empty,
    };
}
