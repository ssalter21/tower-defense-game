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
        string scratch = TheCommandLine.Scratch("play-run");
        string outcome = Path.Combine(scratch, "run-outcome.txt");

        CommandLineResult played = TheCommandLine.Invoke(
            new[] { "play-run", "--commands", RepoLayout.CommandFile, "--out", outcome }
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains(
            "seed " + TheCommandLine.RunSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            played.Output,
            StringComparison.Ordinal);

        Assert.Contains("outcome    ", played.Output, StringComparison.Ordinal);
        Assert.Contains("wave 10: take ", played.Output, StringComparison.Ordinal);

        Assert.True(File.Exists(outcome), outcome + " was asked for and nothing landed there.");
        Assert.Equal(File.ReadAllText(RepoLayout.RunOutcomeFile), File.ReadAllText(outcome));
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

        TheCommandLine.Invoke(
            new[]
            {
                "record-run",
                "--script", RepoLayout.CommandScriptFile,
                "--seed", TheCommandLine.RunSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--out", written,
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        // Read by the library rather than eyeballed: what the verb has to have
        // produced is a command stream, and the seed it was given is the one
        // the record carries.
        CommandStream recorded = CommandStream.FromBytes(File.ReadAllBytes(written));

        Assert.Equal(TheCommandLine.RunSeed, recorded.Seed);
        Assert.Equal(File.ReadAllBytes(RepoLayout.CommandFile), File.ReadAllBytes(written));
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
        // take nobody was offered, and this is the only test in the file that
        // goes red -- a stored run that refuses the first time anybody plays it.
        string scratch = TheCommandLine.Scratch("record-run-refused");
        string script = Path.Combine(scratch, "commands.txt");
        string written = Path.Combine(scratch, "run.commands");

        // Wave one's menu on this seed carries ordinary options 5, 8 and 6.
        File.WriteAllText(script, "build 1 ordinary 3\n");

        CommandLineResult refused = TheCommandLine.Invoke(
            new[]
            {
                "record-run",
                "--script", script,
                "--seed", TheCommandLine.RunSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "--out", written,
            }.Concat(TheCommandLine.RunContent));

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains(
            "which that round's offering does not carry",
            refused.Error,
            StringComparison.Ordinal);

        Assert.False(File.Exists(written), written + " was written for a run that cannot be played.");
    }

    [Fact]
    public void The_offerings_verb_prints_the_menu_a_script_is_written_from()
    {
        // What makes a command file authorable: a take names a kind and an id
        // off a menu drawn from the run's seed, and nobody can write one for a
        // seed they have not been shown.
        //
        // The game changer is named beside its word rather than the word being
        // looked for on its own: every listing has both halves of a vocabulary
        // in it, so "the string 'changer' appears somewhere" is true however the
        // two are wired together.
        //
        // OBSERVED: reverse CommandScript's TakeKinds. WordFor starts answering
        // with the other half's word, and this goes red -- the anchor's swift
        // column is listed as an ordinary option, and a row copied off the
        // listing takes the wrong half of the menu or nothing at all.
        //
        // OBSERVED, on the two wave assertions: walk Run.DefaultWaves in
        // Offerings.ToText instead of run.Waves. The last assertion goes red at
        // position 629, having found "wave   4" -- a listing that ignores the
        // length of the run it was asked about, and quietly shows menus for
        // rounds nobody will play.
        CommandLineResult listed = TheCommandLine.Invoke(
            new[] { "offerings", "--seed", "20260807", "--waves", "3" }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Contains("wave   1, 2 slots\n", listed.Output, StringComparison.Ordinal);
        Assert.Contains("wave   3, 3 slots, an anchor\n", listed.Output, StringComparison.Ordinal);
        Assert.Contains("changer     1  swift-column", listed.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("wave   4", listed.Output, StringComparison.Ordinal);
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

        Assert.Equal(
            "kind,subject,ingredients,runs,rounds,wins,win_rate_bp",
            string.Join(",", rows[0].Split(',').Take(7)));
        Assert.Contains("coverage,creeps,,,,,,,,,,2,6,yes", rows, StringComparer.Ordinal);
        Assert.Contains("parameter,death_ends_the_run,,,,,,,,,,no,,", rows, StringComparer.Ordinal);

        // Two creeps scored, so two whole-population rows and at least one bin
        // under each of them.
        Assert.Equal(2, rows.Count(row => row.StartsWith("creep,", StringComparison.Ordinal)
            && row.Split(',')[2] == "0"));

        Assert.True(
            rows.Count(row => row.StartsWith("creep,", StringComparison.Ordinal)) > 2,
            "The report carries no ingredient bins at all:\n" + string.Join("\n", rows));
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
        Assert.Contains("\ncreep,grunt,0,2,", swept.Output, StringComparison.Ordinal);
    }

    [Fact]
    public void The_death_flag_is_a_switch_and_it_reaches_the_run()
    {
        // Story twenty-three, from a shell: death is a flag rather than a rule
        // so that a harness can ask for N rounds of data out of every run. Until
        // this verb carried it no shell could play a no-death run at all.
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
        // OBSERVED: add "schedul" to the run verbs' allowed list. The exit code
        // stays 1 and the message assertion goes red, because what comes back is
        // "'play-run' needs --map, and it was not given" -- a typo presenting as
        // a different argument being the problem, which is the whole distance
        // between being told what to fix and being sent looking.
        CommandLineResult refused = TheCommandLine.Invoke(
            "play-run", "--commands", RepoLayout.CommandFile, "--schedul", RepoLayout.ScheduleFile);

        Assert.Equal(1, refused.ExitCode);
        Assert.Contains("'--schedul' is not an option of 'play-run'", refused.Error, StringComparison.Ordinal);
    }
}
