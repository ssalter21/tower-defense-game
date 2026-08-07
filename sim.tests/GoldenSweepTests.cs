using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The committed balance report: what the sweep said about the roster the last
/// time somebody regenerated it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The golden trace's rule applied one level up again.</b> The trace pins a
/// match tick by tick and the run outcome pins a run round by round; this pins
/// a whole population of runs. Nobody knows a creep's win rate until a few
/// hundred runs of it have been played, so a retune that reorders the roster is
/// a diff here rather than an argument.
/// </para>
/// <para>
/// <b>Nothing here regenerates anything it checks.</b> The oracle is the
/// committed file and this suite only reads it --
/// <c>tools/run-sweep.ps1 -Regenerate</c> is the only writer, and
/// <c>-Verify</c> is what plays a fresh sweep and compares. What is checked
/// here is what a file cannot check about itself: that its rows are internally
/// consistent, that its columns line up, and that it names the rules it was
/// produced under.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class GoldenSweepTests
{
    /// <summary>Where the coverage and parameter rows keep their one number.</summary>
    private const int ValueColumn = 11;

    [Fact]
    public void Every_row_of_the_committed_report_has_the_columns_the_header_declares()
    {
        // A comma-separated file's one structural property, and the failure it
        // guards is the quiet one: a stray separator inside a cell shifts every
        // column from that row downwards, and the result reads as a balance
        // finding rather than as a broken file. The writer refuses such a cell
        // outright; this is the observation that it has never had to.
        //
        // OBSERVED: build a creep row's subject as the label plus ", " plus the
        // type id in SweepCsv. -Regenerate refuses by name -- "which carries a
        // separator, a quote or a line break" -- and writes nothing, so the
        // committed file cannot reach this state. Take the Cell guard out as
        // well and the file regenerates with fourteen columns on every creep
        // row, which is what this goes red on.
        string[] rows = Rows();
        int columns = rows[0].Split(',').Length;

        Assert.True(columns > 1, "The committed report's header row is not a header row: " + rows[0]);

        for (int index = 1; index < rows.Length; index++)
        {
            Assert.Equal(columns, rows[index].Split(',').Length);
        }
    }

    [Fact]
    public void The_committed_report_names_the_rules_it_was_produced_under()
    {
        // N and K are stamped in no record and the offering ratio is a dial the
        // harness turns, so two reports that differ only in what they were
        // played under look identical until the parameter rows. This is the
        // assertion that those rows are about the content in this repository
        // rather than about whatever was on the machine that wrote them.
        //
        // OBSERVED: move the interest rate in content/ruleset.txt from 10 to 11
        // and do not regenerate. The hash row goes red, the file's
        // B8D395FFBCA5BCCC against the EECBA54CEEFAF7A9 this build now parses,
        // and every number in the file stays perfectly self-consistent -- which
        // is exactly what a stale report looks like from every other angle.
        Ruleset rules = TheRuleset.Committed();

        Assert.Equal(rules.ContentHash.ToString(), Parameter("ruleset_hash"));
        Assert.Equal(Number(rules.OrdinaryOptionsPerRound), Parameter("ordinary_options"));
        Assert.Equal(Number(rules.GameChangersPerAnchor), Parameter("game_changers"));
        Assert.Equal(Number(rules.FreeSnapshotsPerRun), Parameter("free_snapshots"));
        Assert.Equal(Number(rules.SnapshotPriceSauce), Parameter("snapshot_price"));

        // And that it was swept in no-death mode, which is what makes every row
        // of it N rounds of data rather than however far a run got.
        Assert.Equal("no", Parameter("death_ends_the_run"));
    }

    [Fact]
    public void The_committed_report_says_how_far_it_reached()
    {
        // Story eighty, as an artefact. A sweep that sampled eight seeds a creep
        // says so in its own rows, so nobody three months from now reads a
        // sample as an enumeration -- and the roster row says whether the report
        // is about the whole table or a prefix of it.
        //
        // OBSERVED: run tools/run-sweep.ps1 -Regenerate with --most-creeps 3.
        // The creeps row goes red, "3,6,yes" against the "6,6,no" expected, and
        // nothing else in the file objects at all: it is a complete-looking
        // report about half a roster.
        Assert.Equal(
            Number(WalkersInTheCommittedRoster()) + "," + Number(WalkersInTheCommittedRoster()) + ",no",
            Coverage("creeps"));

        // The seed axis is a sample whatever its size, so it is bounded and its
        // population is not a number.
        Assert.Equal(Parameter("runs_per_creep") + ",,yes", Coverage("seeds"));
    }

    [Fact]
    public void The_ingredient_bins_of_the_committed_report_add_up_to_the_rows_above_them()
    {
        // The report carries two kinds of creep row -- a creep's whole
        // population and its runs split by how many ingredients they ended up
        // holding -- and the second is only readable if it partitions the first.
        // Checked on the committed artefact as well as on a fresh sweep, because
        // this is the file a person opens.
        //
        // OBSERVED: doctor the committed file, moving the grunt's bin-4 run
        // count from 5 to 4. This goes red, 7 binned against the 8 in the
        // population, and every other row of the file stays consistent.
        string[] rows = Rows();

        for (int index = 1; index < rows.Length; index++)
        {
            string[] cells = rows[index].Split(',');

            if (cells[0] != "creep" || cells[2] != "0")
            {
                continue;
            }

            int binned = 0;

            for (int other = 1; other < rows.Length; other++)
            {
                string[] cell = rows[other].Split(',');

                if (cell[0] == "creep" && cell[1] == cells[1] && cell[2] != "0")
                {
                    binned += int.Parse(cell[3], CultureInfo.InvariantCulture);
                }
            }

            Assert.Equal(int.Parse(cells[3], CultureInfo.InvariantCulture), binned);
        }
    }

    /// <summary>The committed report, split into rows with the trailing newline dropped.</summary>
    private static string[] Rows() =>
        File.ReadAllText(RepoLayout.SweepFile).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>The one number a parameter row carries.</summary>
    private static string Parameter(string name) => Cell("parameter", name, ValueColumn, 1);

    /// <summary>A coverage row's three cells: covered, of, and whether it is bounded.</summary>
    private static string Coverage(string axis) => Cell("coverage", axis, ValueColumn, 3);

    /// <summary>A run of cells off the one row of a kind with this subject.</summary>
    private static string Cell(string kind, string subject, int from, int count)
    {
        string[] rows = Rows();

        for (int index = 1; index < rows.Length; index++)
        {
            string[] cells = rows[index].Split(',');

            if (cells[0] == kind && cells[1] == subject)
            {
                return string.Join(",", cells.Skip(from).Take(count));
            }
        }

        throw new Xunit.Sdk.XunitException(
            "The committed report has no " + kind + " row for " + subject + ".");
    }

    /// <summary>How many rows of the committed roster walk, which is what a sweep can score.</summary>
    private static int WalkersInTheCommittedRoster()
    {
        UnitTypeTable types = TheMatch.Types();
        int walkers = 0;

        for (int index = 0; index < types.Count; index++)
        {
            if (types.Types[index].Role == UnitRole.Moving)
            {
                walkers++;
            }
        }

        return walkers;
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
