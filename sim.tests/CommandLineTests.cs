using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The run verbs of the command line, exercised as a process.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is the wiring and not the rules.</b> Every rule these
/// verbs reach lives in the simulation and is tested there, exhaustively, in
/// <see cref="CommandStreamTests"/> and <see cref="BuildPhaseTests"/>. What is
/// only true out here is that the arguments reach the right parsers, the files
/// are read and written where they were asked for, and an exit code says what
/// happened -- so each verb gets one end-to-end pass and one that is refused,
/// and no more.
/// </para>
/// <para>
/// The other end-to-end half is <c>tools/run-headless-match.ps1 -Verify</c>,
/// which plays the committed record through the actual command line and
/// compares what it printed against the committed outcome. The gate runs both.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class CommandLineTests
{
    /// <summary>The line an outcome file's closing block opens on.</summary>
    private const string BoardLabel = "the board at the end";

    /// <summary>The seed the committed run was decided on, as an argument.</summary>
    private static string Seed =>
        TheCommandLine.RunSeed.ToString(System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void The_play_run_verb_plays_a_command_file_and_reports_the_outcome()
    {
        // OBSERVED: have PlayRun write to the path plus ".elsewhere". The file
        // assertion goes red naming the path nothing landed at, and the three
        // printed assertions stay green -- which is what a verb that says the
        // right thing and writes somewhere else looks like from a shell.
        //
        // OBSERVED: build the run on the match's seed instead of the stream's --
        // pass 20260801 to content.Fresh. The verb exits 1 rather than printing
        // anything, "A command stream stores the run seeded 20260807 and it was
        // handed the run seeded 20260801", and the succeeded-assertion carries
        // that refusal into the message.
        //
        // OBSERVED, on the two spellings of a round: give Report(PlayedRun) a
        // round line of its own -- drop the standing count out of what it
        // prints and leave PlayedRun.OutcomeFile alone. The round loop goes red
        // on wave one, quoting the line the file carries, which is what a
        // terminal and a committed file that have grown apart look like from a
        // shell.
        string scratch = TheCommandLine.Scratch("play-run");
        string outcome = Path.Combine(scratch, "run-outcome.txt");

        CommandLineResult played = TheCommandLine.Invoke(
            new[] { "play-run", "--commands", RepoLayout.CommandFile, "--out", outcome }
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains(
            "seed " + Seed,
            played.Output,
            StringComparison.Ordinal);

        Assert.Contains("outcome    ", played.Output, StringComparison.Ordinal);

        Assert.True(File.Exists(outcome), outcome + " was asked for and nothing landed there.");
        Assert.Equal(File.ReadAllText(RepoLayout.RunOutcomeFile), File.ReadAllText(outcome));

        // The round lines a person watched are the round lines that were
        // committed, which is what one code path behind both of them buys.
        // The count is asserted because a loop over nothing agrees with
        // everything.
        //
        // OBSERVED, on the count: filter the file on "round " instead of
        // "wave ". Nothing matches, the loop below passes over an empty array,
        // and this goes red -- 0 against 4 -- rather than the pass a filter
        // that had stopped selecting anything would otherwise be.
        //
        // The count comes off the record rather than off N: the committed run
        // ends on its health, so it is as many rounds as it has decisions and
        // fewer than its wave cap.
        string[] rounds = File.ReadAllLines(outcome)
            .Where(line => line.StartsWith("wave ", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            CommandStream.FromBytes(File.ReadAllBytes(RepoLayout.CommandFile)).Count,
            rounds.Length);

        foreach (string round in rounds)
        {
            Assert.Contains(round, played.Output, StringComparison.Ordinal);
        }

        // The block a person watched is the block that was committed, the blank
        // line above it included, and it is there once.
        //
        // OBSERVED, on the count: append run.Run.Board.ToReportText() to what
        // Report writes. The Contains stays green -- a second copy of a block
        // still contains the first -- and this goes red, 2 against 1.
        Assert.Contains(BoardBlock(outcome), played.Output, StringComparison.Ordinal);
        Assert.Equal(1, Occurrences(played.Output, BoardLabel));
    }

    [Fact]
    public void The_record_run_verb_compiles_a_script_into_a_command_file()
    {
        // OBSERVED: build the run on seed + 1 in PlayedRun.Recorded. The verb
        // still exits 0 -- these decisions happen to be legal against that
        // seed's menus too, which is exactly why the exit code is not the
        // assertion -- and the seed goes red, 20260807 against 20260808, taking
        // the byte comparison with it.
        string scratch = TheCommandLine.Scratch("record-run");
        string written = Path.Combine(scratch, "run.commands");

        CommandLineResult wrote = TheCommandLine.Invoke(
            new[]
            {
                "record-run",
                "--script", RepoLayout.CommandScriptFile,
                "--seed", Seed,
                "--out", written,
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        // Read by the library rather than eyeballed: what the verb has to have
        // produced is a command stream, and the seed it was given is the one
        // the record carries.
        CommandStream recorded = CommandStream.FromBytes(File.ReadAllBytes(written));

        Assert.Equal(TheCommandLine.RunSeed, recorded.Seed);
        Assert.Equal(File.ReadAllBytes(RepoLayout.CommandFile), File.ReadAllBytes(written));

        // Recording plays the run to the end to prove it, and a recorder that
        // reported less than a replay of the same decisions would be two verbs
        // with one run between them.
        //
        // OBSERVED: delete the Report(proof) call from RecordRun, which is what
        // a recorder that only wrote a file would be. This goes red and the
        // play-run test stays green, which is the pair that would otherwise
        // leave one verb reporting less than the other.
        Assert.Contains(BoardBlock(RepoLayout.RunOutcomeFile), wrote.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void A_script_that_will_not_replay_writes_no_command_file_at_all()
    {
        // The bytes are read back, taken through the replay gate and played to
        // the end before anything is written, so a run nobody could have played
        // never becomes a file somebody finds out about later.
        //
        // OBSERVED: take the FromBytes(bytes).Replay(...) line out of
        // CommandStream.Recorded, which is the whole of what "proved" means.
        // The verb exits 0, writes a perfectly readable command stream for a
        // wave nobody could have paid for, and this is the only test in the
        // file that goes red -- a stored run that refuses the first time
        // anybody plays it.
        string scratch = TheCommandLine.Scratch("record-run-refused");
        string script = Path.Combine(scratch, "commands.txt");
        string written = Path.Combine(scratch, "run.commands");

        // A hundred minions out of an opening purse of a hundred gold, which is
        // ten times what the round holds. The script parses perfectly; what
        // refuses it is the playing.
        File.WriteAllText(script, "build 1 1 100\n");

        CommandLineResult refused = TheCommandLine.Invoke(
            new[]
            {
                "record-run",
                "--script", script,
                "--seed", Seed,
                "--out", written,
            }.Concat(TheCommandLine.RunContent));

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains(
            "There is no credit in this economy",
            refused.Error,
            StringComparison.Ordinal);

        Assert.False(File.Exists(written), written + " was written for a run that cannot be played.");
    }

    [Fact]
    public void A_run_against_an_empty_pool_is_the_committed_run_and_a_seeded_one_names_who_it_met()
    {
        // The three claims a folder of opponents makes from a shell, in the
        // order they have to hold: an empty folder is the run this program has
        // always played, byte for byte; storing a run puts its rounds where the
        // next one draws from; and drawing from a seeded folder names the ids
        // it met and how many the canned field stood in for.
        //
        // OBSERVED: consume one draw off the stream for a stage that stored
        // nobody -- start FieldFor's top-up loop at zero rather than at `met`.
        // The first assertion goes red on a whole outcome file, which is what
        // retiring content/run-outcome.txt without meaning to looks like.
        string scratch = TheCommandLine.Scratch("play-run-pool");
        string pool = Path.Combine(scratch, "pool");
        string empty = Path.Combine(scratch, "empty.txt");
        string seeded = Path.Combine(scratch, "seeded.txt");
        string[] against = { "--pool", pool };

        CommandLineResult unseeded = TheCommandLine.Invoke(
            new[] { "play-run", "--commands", RepoLayout.CommandFile, "--out", empty }
                .Concat(against)
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains("pool       0 stored rounds", unseeded.Output, StringComparison.Ordinal);
        Assert.Equal(File.ReadAllText(RepoLayout.RunOutcomeFile), File.ReadAllText(empty));

        // And what that run stored is what the next one meets. The first round
        // built a wall and sent nothing, so it is not a stored round and says
        // so rather than landing as a file the folder would refuse.
        CommandLineResult stored = TheCommandLine.Invoke(
            new[] { "play-run", "--commands", RepoLayout.CommandFile, "--store" }
                .Concat(against)
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains("not stored round 1", stored.Output, StringComparison.Ordinal);
        Assert.Contains("read back before writing", stored.Output, StringComparison.Ordinal);

        // A file the reader cannot use, dropped in beside them: it is named and
        // skipped, and the run still finishes. A folder accumulates for as long
        // as anybody plays, so a stale record in one must not stop a run.
        File.WriteAllBytes(Path.Combine(pool, "0000000000000000.round"), new byte[] { 1, 2, 3, 4 });

        CommandLineResult met = TheCommandLine.Invoke(
            new[] { "play-run", "--commands", RepoLayout.CommandFile, "--out", seeded }
                .Concat(against)
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains("refused    0000000000000000", met.Output, StringComparison.Ordinal);
        Assert.Contains("1 refused", met.Output, StringComparison.Ordinal);

        // The outcome names who each round met and how many of the ten it could
        // not fill, and the two together are the field's width.
        string[] rounds = File.ReadAllLines(seeded)
            .Where(line => line.StartsWith("wave ", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(rounds);
        Assert.All(rounds, round => Assert.Contains(" canned", round, StringComparison.Ordinal));
        Assert.Contains(
            rounds,
            round => round.Contains(" stored and ", StringComparison.Ordinal)
                && !round.Contains("0 stored and ", StringComparison.Ordinal));

        // Replaying the same commands against the same folder draws the same
        // ten, which is what a derived draw buys.
        string again = Path.Combine(scratch, "again.txt");

        TheCommandLine.Invoke(
            new[] { "play-run", "--commands", RepoLayout.CommandFile, "--out", again }
                .Concat(against)
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Equal(File.ReadAllText(seeded), File.ReadAllText(again));
        Assert.NotEqual(File.ReadAllText(empty), File.ReadAllText(seeded));
    }

    [Fact]
    public void The_ladder_verb_reads_the_committed_pair_and_exits_zero()
    {
        // Two files and not seven, which is the shape of the verb: a ladder is
        // read against the roster and against nothing else.
        //
        // The edge count is read off the same parser the verb uses rather than
        // pinned, because the ladder is expected to grow one row at a time and a
        // pinned count would go red on every legitimate authoring. What this
        // holds the verb to is printing one line per committed edge and exiting
        // zero -- which over a ladder with no edges is no output and still a zero,
        // and is still the statement that the file was opened and accepted rather
        // than skipped.
        CommandLineResult listed = TheCommandLine.Invoke(
            "ladder",
            "--units", RepoLayout.UnitsFile,
            "--upgrades", RepoLayout.UpgradesFile)
            .Succeeded();

        Assert.Equal(EdgeCount(), listed.Output.Split("edge   ").Length - 1);
    }

    [Fact]
    public void The_ladder_verb_refuses_a_ladder_it_cannot_read()
    {
        // The verb enforces nothing about a ladder's DESIGN and everything about
        // its structure: it exits zero over a fault, and non-zero over a file it
        // could not parse. Those are two different questions and this is the one
        // a wrong exit code would hide.
        string scratch = TheCommandLine.Scratch("ladder");
        string broken = Path.Combine(scratch, "upgrades.txt");

        File.WriteAllText(broken, "layout 1\nupgrade 4 3\n");

        CommandLineResult refused = TheCommandLine.Invoke(
            "ladder",
            "--units", RepoLayout.UnitsFile,
            "--upgrades", broken);

        Assert.NotEqual(0, refused.ExitCode);
        Assert.Contains("has to exceed its source", refused.Error, StringComparison.Ordinal);
    }

    /// <summary>How many edges the committed ladder has, read the same way the verb reads it.</summary>
    private static int EdgeCount()
    {
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        return UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types).Count;
    }

    [Fact]
    public void The_sweep_verb_writes_the_report_the_harness_computed()
    {
        // The wiring, end to end: six content files and a shape in, a
        // comma-separated file out, with the rows the library folded and the
        // coverage it reached. The numbers are the harness's and are tested in
        // SweepTests; what is only true out here is that they reach a file.
        //
        // A small sweep on purpose -- two creeps, three seeds, three waves and a
        // field of two -- because what is under test is the wiring and a wide
        // one would spend half a minute proving it.
        //
        // OBSERVED: pass SweepPlan.WholeRoster in place of the --most-creeps
        // argument in Program.RunSweep. The coverage assertion goes red, having
        // found no "coverage,creeps,...,2,6,yes" row among the six-of-six the
        // file now carries -- a truncation argument that reaches nothing, and a
        // file that correctly says so.
        string scratch = TheCommandLine.Scratch("sweep");
        string report = Path.Combine(scratch, "sweep.csv");

        TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "3",
                "--waves", "3",
                "--field-size", "2",
                "--most-creeps", "2",
                "--no-death",
                "--out", report,
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        string[] rows = File.ReadAllText(report).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // The two rows are spelled by naming their columns, off the writer's own
        // declaration, rather than as a literal with its blank cells counted out
        // here -- so a column added to the report is an edit to that list and
        // not to this.
        Assert.Equal(SweepColumns.Header().Line, rows[0]);

        Assert.Contains(
            new CsvRow()
                .With("kind", "coverage")
                .With("subject", "creeps")
                .With("value", "2")
                .With("of", "5")
                .With("bounded", "yes")
                .Line,
            rows,
            StringComparer.Ordinal);

        Assert.Contains(
            new CsvRow()
                .With("kind", "parameter")
                .With("subject", "death_ends_the_run")
                .With("value", "no")
                .Line,
            rows,
            StringComparer.Ordinal);

        // Two creeps against three walls, so six creep rows. It was two whole-
        // population rows plus a bin or more under each until #179 deleted the
        // ingredients axis, and two flat until #242 made the wall an axis --
        // every creep meets every wall, so the rows are the product and a
        // seventh would be a cell nobody asked to score.
        Assert.Equal(6, rows.Count(row => row.StartsWith("creep,", StringComparison.Ordinal)));

        // And the file says how many walls that was, on a coverage row of its
        // own. A report that carried the wall column without this could be
        // filtered down to one wall and read as complete.
        Assert.Contains(
            new CsvRow()
                .With("kind", "coverage")
                .With("subject", "walls")
                .With("value", "3")
                .With("of", "3")
                .With("bounded", "no")
                .Line,
            rows,
            StringComparer.Ordinal);
    }

    [Fact]
    public void The_sweep_verb_refuses_a_wall_that_is_not_an_attack_type()
    {
        // The whole report hangs off this argument, and the failure of a
        // defaulted one is the quiet kind: a complete and correct-looking file
        // scored against walls nobody asked for. A misspelling is how somebody
        // comparing two reports ends up comparing one against itself.
        //
        // OBSERVED: fall back to the roster's own types where a word does not
        // parse. Both invocations below succeed and write fifteen rows, and the
        // one that asked for two walls gets three.
        CommandLineResult unknown = TheCommandLine.Invoke(
            new[] { "sweep", "--seed", "1", "--walls", "peirce" }.Concat(TheCommandLine.RunContent));

        Assert.NotEqual(0, unknown.ExitCode);
        Assert.Contains("peirce", unknown.Error + unknown.Output, StringComparison.Ordinal);

        // 'any' is the absence of a restriction rather than a fourth type, so a
        // file listing it beside pierce would carry two rows a reader would
        // compare where one is the other's superset.
        CommandLineResult mixed = TheCommandLine.Invoke(
            new[] { "sweep", "--seed", "1", "--walls", "any,pierce" }.Concat(TheCommandLine.RunContent));

        Assert.NotEqual(0, mixed.ExitCode);

        // And a wall named twice is the same runs reported again under a
        // heading nothing tells from the first.
        CommandLineResult twice = TheCommandLine.Invoke(
            new[] { "sweep", "--seed", "1", "--walls", "pierce,pierce" }.Concat(TheCommandLine.RunContent));

        Assert.NotEqual(0, twice.ExitCode);
    }

    [Fact]
    public void The_sweep_verb_takes_one_unrestricted_wall_where_it_is_asked_for_one()
    {
        // The way back to the report this file was before #242: whatever the
        // defending bot buys, unrestricted. It is a legitimate question -- it
        // is the wall a run actually meets -- and it is named rather than left
        // blank so that a one-wall report and a three-wall one cannot be
        // mistaken for each other by a spreadsheet.
        //
        // OBSERVED: leave the wall column blank for the unrestricted wall. The
        // rows still parse and the file still reads, and every row of it says
        // nothing about what it was played against.
        CommandLineResult swept = TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "1",
                "--waves", "2",
                "--field-size", "1",
                "--most-creeps", "1",
                "--no-death",
                "--walls", "any",
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains("\ncreep,minion,any,1,", swept.Output, StringComparison.Ordinal);
        Assert.Equal(
            1,
            swept.Output
                .Split('\n')
                .Count(row => row.StartsWith("creep,", StringComparison.Ordinal)));
    }

    [Fact]
    public void The_sweep_verb_prints_the_report_where_there_is_no_out()
    {
        // A sweep is a mode and a spreadsheet rather than a project, so the
        // shortest useful invocation of it pipes. With --out the program says
        // what it wrote and where; without one, the file itself is what comes
        // back and nothing else is on the stream to spoil it.
        //
        // OBSERVED: write the "swept" summary to standard output ahead of the
        // report in Program.RunSweep. The header assertion goes red on the
        // string it starts with, which is what a tool that cannot be piped looks
        // like -- every number in the file is still correct and the first line
        // of it is prose.
        CommandLineResult swept = TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "2",
                "--waves", "2",
                "--field-size", "1",
                "--most-creeps", "1",
                "--no-death",
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.StartsWith("kind,subject,", swept.Output, StringComparison.Ordinal);

        // The wall is the third cell since #242, and naming it here is the
        // point: a row that said only "minion" would be one of three with
        // different numbers under the same heading.
        Assert.Contains("\ncreep,minion,pierce,2,", swept.Output, StringComparison.Ordinal);
        Assert.Contains("\ncreep,minion,magic,2,", swept.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void The_sweep_verb_writes_a_row_for_every_run_where_it_is_asked_to()
    {
        // The other table the file can carry: one row a run, under a kind of
        // its own, so a spreadsheet filters on the first column and gets the
        // distribution behind the fold. The numbers are the harness's and are
        // tested in SweepTests; what is only true out here is that the flag
        // reaches the plan and the rows reach the file.
        //
        // OBSERVED: hand SweepPlan a keepsEveryRun of false in RunContent.Sweep
        // and ignore the argument. The run rows go to none and this goes red on
        // the count, which is the shape a flag that parses and does nothing
        // takes -- the file is well-formed and the mode is simply absent.
        string scratch = TheCommandLine.Scratch("sweep-per-run");
        string report = Path.Combine(scratch, "sweep.csv");

        TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "3",
                "--waves", "3",
                "--field-size", "2",
                "--most-creeps", "2",
                "--no-death",
                "--per-run",
                "--out", report,
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        string[] rows = File.ReadAllText(report).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Two creeps over three seeds against three walls, so eighteen runs and
        // the six folded rows they add up to. Both tables are in one file
        // because the alternative is two files that can be separated from each
        // other.
        Assert.Equal(18, rows.Count(row => row.StartsWith("run,", StringComparison.Ordinal)));
        Assert.Equal(6, rows.Count(row => row.StartsWith("creep,", StringComparison.Ordinal)));

        // Every one of them names the seed it was played on, which is what
        // makes a row out on the tail something to replay rather than to
        // squint at.
        int seed = SweepColumns.IndexOf("seed");

        foreach (string row in rows.Where(row => row.StartsWith("run,", StringComparison.Ordinal)))
        {
            Assert.NotEqual(string.Empty, row.Split(',')[seed]);
        }

        // And the flag is what produces them: the same sweep without it writes
        // the folded table alone.
        CommandLineResult folded = TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "3",
                "--waves", "3",
                "--field-size", "2",
                "--most-creeps", "2",
                "--no-death",
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.DoesNotContain("\nrun,", folded.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void The_sweep_verb_plays_the_policy_it_was_named_and_writes_down_which()
    {
        // Comparing two strategies is what the plan's policy parameter exists
        // for, and a verb that took no name would leave it reachable only by
        // editing C#. The all-in player spends its whole purse on the wave, so
        // what separates the two reports is the defense column: one builds and
        // the other does not.
        //
        // OBSERVED: resolve every name to EvenShareBot.Decide in Program's
        // policy lookup. The parameter row still reads all-in -- the shell
        // knows what it was asked for -- and the defense assertion goes red
        // with a board built under a player that builds nothing, which is a
        // report naming a strategy it was not played under.
        string scratch = TheCommandLine.Scratch("sweep-policy");
        string report = Path.Combine(scratch, "sweep.csv");

        TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "2",
                "--waves", "3",
                "--field-size", "2",
                "--most-creeps", "2",
                "--no-death",
                "--policy", "all-in",
                "--out", report,
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        string[] rows = File.ReadAllText(report).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains(
            new CsvRow()
                .With("kind", "parameter")
                .With("subject", "policy")
                .With("value", "all-in")
                .Line,
            rows,
            StringComparer.Ordinal);

        int defense = SweepColumns.IndexOf("defense_gold");
        string[] creeps = rows.Where(row => row.StartsWith("creep,", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(creeps);

        foreach (string row in creeps)
        {
            Assert.Equal("0", row.Split(',')[defense]);
        }

        // And the default player is the even-share bot, which does build --
        // so the column above is about the policy that was named rather than
        // about a number this shape of sweep never moves.
        CommandLineResult shared = TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "2",
                "--waves", "3",
                "--field-size", "2",
                "--most-creeps", "2",
                "--no-death",
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains(
            new CsvRow()
                .With("kind", "parameter")
                .With("subject", "policy")
                .With("value", "even-share")
                .Line,
            shared.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries),
            StringComparer.Ordinal);

        Assert.Contains(
            shared.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(row => row.StartsWith("creep,", StringComparison.Ordinal)),
            row => row.Split(',')[defense] != "0");
    }

    [Fact]
    public void A_sweep_asked_for_a_player_this_program_does_not_have_is_refused_by_name()
    {
        // A misspelled policy is the quiet failure this closes: falling back to
        // the default would produce a complete, correct-looking report about a
        // player nobody asked for, and the name is not on any row the reader
        // would think to check.
        //
        // OBSERVED: fall back to EvenShareBot.Decide for an unrecognised name.
        // The exit code goes to 0 and this goes red on it, having swept the
        // whole roster under the wrong player without a word.
        CommandLineResult refused = TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--policy", "greedy",
            }.Concat(TheCommandLine.RunContent));

        Assert.NotEqual(0, refused.ExitCode);
        Assert.Contains("greedy", refused.Error, StringComparison.Ordinal);
        Assert.Contains("even-share", refused.Error, StringComparison.Ordinal);
        Assert.Contains("all-in", refused.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sweep_handed_the_match_wave_as_its_field_is_refused_by_name()
    {
        // The mistake this closes is the silent one, and it is the mistake three
        // shells and this suite were all making: content/wave.txt parses as a
        // field perfectly, and a sweep against it loses every row and separates
        // no creep from any other. The report that falls out reads exactly like
        // a real one -- same columns, same coverage rows, every number
        // self-consistent -- so nothing about it says which file it was about.
        //
        // What makes the two tellable apart is structural rather than a budget:
        // a field member stands in for a stored round, a stored round is a build
        // phase's output, and a build phase composes what is sent rather than
        // when. The wave file's second order releases on tick 750, which is the
        // one thing a stored round can never do.
        //
        // OBSERVED: take the tick loop out of RunContent.Field. The exit code
        // goes to 0 and this goes red on it, the sweep having run to completion
        // and written its report -- the minion row reading 0 dealt against 219
        // taken and no win. At the committed shape the same build writes a whole
        // report in which not one of the five creeps wins a single run: 824 gold
        // dealt against 8211 taken, and a zero in the win rate and the bonus of
        // all twenty-two rows, with every other column perfectly self-consistent.
        string scratch = TheCommandLine.Scratch("sweep-wrong-field");

        CommandLineResult refused = TheCommandLine.Invoke(
            new[]
            {
                "sweep",
                "--seed", "20260807",
                "--runs", "1",
                "--waves", "2",
                "--field-size", "1",
                "--most-creeps", "1",
                "--no-death",
                "--out", Path.Combine(scratch, "sweep.csv"),
                "--map", RepoLayout.MapFile,
                "--units", RepoLayout.UnitsFile,
                "--upgrades", RepoLayout.UpgradesFile,
                "--rules", RepoLayout.RulesetFile,
                "--defense", RepoLayout.DefenseFile,
                "--field", RepoLayout.WaveFile,
            });

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("releases on tick 750", refused.Error, StringComparison.Ordinal);
        Assert.False(
            File.Exists(Path.Combine(scratch, "sweep.csv")),
            "A report was written for a sweep against an opponent no player could be.");
    }

    [Fact]
    public void A_file_named_outright_stands_in_for_the_one_the_content_directory_holds()
    {
        // --content is a directory and the six files inside it are found by
        // the names the runner declares; naming one outright replaces that file
        // and leaves the other five where they were.
        //
        // What proves the override reached the reader rather than being ignored
        // is the refusal: the file named here is the authored match, whose
        // second order releases on tick 750, and no stored round does that.
        //
        // OBSERVED: look in the directory before the option in Program.TextOf.
        // The verb exits 0 and prints a run's menus against content/field.txt --
        // an argument that named a file nobody opened, which is the failure the
        // whole of Arguments exists to prevent.
        CommandLineResult refused = TheCommandLine.Invoke(
            "play-run",
            "--commands", RepoLayout.CommandFile,
            "--content", RepoLayout.ContentDirectory,
            "--field", RepoLayout.WaveFile);

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("releases on tick 750", refused.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_content_file_the_runner_declares_is_one_a_run_verb_opens()
    {
        // The declaration drives the option list and the usage block by being
        // read, and it drives the reader by somebody having written the line.
        // This is what holds the third to the first two: a run verb is played
        // against a directory with one declared file missing from it, once per
        // file, and it has to refuse naming the file that is not there.
        //
        // A row added to RunContentFiles and not opened by Program.ContentOf is
        // an option that parses, appears in the usage block and reaches nothing
        // -- so this walks the declaration rather than a list of its own.
        //
        // OBSERVED: drop the --schedule line from Program.ContentOf. The verb
        // runs to completion against a directory with no schedule.txt in it and
        // this goes red naming that file, while every other test in this class
        // stays green -- content nobody reads, offered by name.
        string scratch = TheCommandLine.Scratch("content-directory");

        foreach (ContentFile file in RunContentFiles.All)
        {
            File.Copy(RepoLayout.InContent(file), Path.Combine(scratch, file.FileName));
        }

        foreach (ContentFile withheld in RunContentFiles.All)
        {
            string path = Path.Combine(scratch, withheld.FileName);
            string held = File.ReadAllText(path);

            File.Delete(path);

            CommandLineResult refused = TheCommandLine.Invoke(
                "play-run", "--commands", RepoLayout.CommandFile, "--content", scratch);

            File.WriteAllText(path, held);

            Assert.True(
                refused.ExitCode != 0,
                "A run verb played a run with no " + withheld.FileName + " in front of it.");

            Assert.Contains(withheld.FileName, refused.Error, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_run_verb_given_neither_the_file_nor_a_directory_names_both_ways_to_give_it()
    {
        // Nothing is assumed about where content lives: a verb with no --map and
        // no --content refuses, and says which file it wanted and both ways of
        // handing it over. A default path here would play a run against content
        // nobody named and print a confident answer about a different game.
        //
        // OBSERVED: fall back to Path.Combine("content", file.FileName) instead
        // of throwing. The exit code goes to 0 wherever the program happens to
        // have been started from, and to 1 with a file-not-found somewhere else
        // -- a verb whose behaviour is the shell's working directory.
        CommandLineResult refused = TheCommandLine.Invoke(
            "play-run", "--commands", RepoLayout.CommandFile);

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("'play-run' needs --map", refused.Error, StringComparison.Ordinal);
        Assert.Contains("holding map.txt with --content", refused.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void The_death_flag_is_a_switch_and_it_reaches_the_run()
    {
        // Death is a flag rather than a rule so that a harness can ask for a
        // round of data per wave instead of a short row wherever a build failed.
        // Until this switch existed no shell could play a no-death run at all.
        //
        // The shape line is what says so, and it is printed into the outcome
        // file as well -- which is where a diff sees that two runs of the same
        // record were played under different rules.
        //
        // OBSERVED: read the flag as arguments.Given("no-death") rather than its
        // negation in Program.ShapeOf. The first assertion goes red, having
        // found the other's sentence -- and content/run-outcome.txt would
        // regenerate saying death does not end a run that death ends.
        string[] content = new[] { "play-run", "--commands", RepoLayout.CommandFile }
            .Concat(TheCommandLine.RunContent)
            .ToArray();

        Assert.Contains(
            ", death ends the run",
            TheCommandLine.Invoke(content).Succeeded().Output,
            StringComparison.Ordinal);

        Assert.Contains(
            ", death does not end the run",
            TheCommandLine.Invoke(content.Append("--no-death")).Succeeded().Output,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_switch_given_a_value_is_refused_rather_than_swallowing_the_next_option()
    {
        // A switch takes no value, so the thing after it is the next option and
        // not its argument. The failure being engineered out is the quiet one: a
        // parser that consumed one would swallow --commands the first time
        // somebody wrote --no-death in front of it, and complain about a missing
        // argument rather than about the switch.
        //
        // OBSERVED: empty Program.Switches. The exit code stays 1 and the
        // message assertion goes red, because what comes back names --commands
        // rather than --nonsense: the switch ate the option after it and the
        // program reported that option's absence. The death-flag test above goes
        // red with it, refusing a run nobody mistyped.
        CommandLineResult refused = TheCommandLine.Invoke(
            new[] { "play-run", "--no-death", "--commands", RepoLayout.CommandFile, "--nonsense", "1" }
                .Concat(TheCommandLine.RunContent));

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("'--nonsense' is not an option of 'play-run'", refused.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void A_misspelled_option_on_a_run_verb_is_refused_rather_than_defaulted()
    {
        // The property Arguments exists for, asserted on the verbs that were
        // just added to it: a --schedul that silently became a default would
        // play a run against a shape nobody named and print a confident answer
        // about a different game.
        //
        // OBSERVED: add "upgrade" to the run verbs' allowed list. The exit code
        // stays 1 and the message assertion goes red, because what comes back is
        // "'play-run' needs --map, and it was not given" -- a typo presenting as
        // a different argument being the problem, which is the whole distance
        // between being told what to fix and being sent looking.
        CommandLineResult refused = TheCommandLine.Invoke(
            "play-run", "--commands", RepoLayout.CommandFile, "--upgrade", RepoLayout.UpgradesFile);

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("'--upgrade' is not an option of 'play-run'", refused.Error, StringComparison.Ordinal);
    }

    /// <summary>
    /// The block an outcome file ends on, and the blank line above it, read off
    /// the file rather than spelled here -- so that what a verb printed is held
    /// against a committed copy of itself and not against a second copy of the
    /// format, and so that where the block sits is asserted and not only what
    /// is in it.
    /// </summary>
    private static string BoardBlock(string path)
    {
        string[] lines = File.ReadAllText(path).TrimEnd('\n').Split('\n');
        int opens = Array.FindIndex(lines, line => line.Contains(BoardLabel, StringComparison.Ordinal));

        Assert.True(opens >= 1, path + " ends on no board block at all.");

        return string.Join("\n", lines.Skip(opens - 1));
    }

    /// <summary>How many times a phrase appears in what a verb printed.</summary>
    private static int Occurrences(string text, string phrase)
    {
        int found = 0;

        for (int at = text.IndexOf(phrase, StringComparison.Ordinal);
            at >= 0;
            at = text.IndexOf(phrase, at + phrase.Length, StringComparison.Ordinal))
        {
            found++;
        }

        return found;
    }
}
