using Sim;

namespace Sim.Cli;

/// <summary>
/// The headless runner. Give it a replay bundle and it plays the whole match
/// with nobody watching, or a command file and it plays the whole run, then
/// says what happened.
/// </summary>
/// <remarks>
/// <para>
/// <b>A static entry point, reachable from a shell and from nothing else.</b>
/// No editor has to be running, no plug-in installed and no project open: the
/// working agreements in CLAUDE.md ask for exactly this, because a session is
/// the one thing a fresh clone, a continuous-integration runner and an
/// overnight agent all lack. <c>tools/run-headless-match.ps1</c> is the shell
/// end of it.
/// </para>
/// <para>
/// <b>There is no headless mode here, because there is no mode.</b> This
/// program pulls no snapshots, and that is the entire difference between it and
/// the view -- an absent call, not a branch. It is the same
/// <see cref="Match"/> surface, the same assembly and the same tick loop, which
/// is why the engine-side parity run can diff its trace against this one's and
/// have the comparison mean something.
/// </para>
/// </remarks>
public static class Program
{
    private const string TraceFileName = "golden-trace.txt";

    private const string LandmarkFileName = "landmarks.txt";

    /// <summary>The map handle is a <c>u16</c> in the record, and this is that.</summary>
    private const int MaximumMapHandle = 65535;

    /// <summary>
    /// The longest run a command file can describe. A build phase stores its
    /// wave as a <c>u16</c>, so a run past this has rounds no decision can be
    /// stored for. The floor is one rather than zero: a run whose wave cap is
    /// lifted is bounded by its health alone, which is a sweep's loop rather
    /// than a run somebody plays from a file.
    /// </summary>
    private const int MaximumWaves = 65535;

    /// <summary>
    /// The widest field a round may be resolved against. There is no rule
    /// putting a ceiling here; a bound there is stops a mistyped argument
    /// asking for a billion matches and getting them.
    /// </summary>
    private const int MaximumFieldSize = 65535;

    /// <summary>
    /// The most seeds one creep may be played on. There is no rule putting a
    /// ceiling here; a bound there is stops a mistyped argument asking for a
    /// billion runs and getting them.
    /// </summary>
    private const int MaximumRunsPerCreep = 100000;

    /// <summary>The most rows of a roster a sweep may be bounded to. The unit table is a u16 id space.</summary>
    private const int MaximumCreeps = 65535;

    /// <summary>
    /// The six content files a run is built from, and the three arguments that
    /// say how long it lasts, how wide its field is and whether death ends it.
    /// Every run verb takes all nine, so the list is written once.
    /// </summary>
    private static readonly string[] RunOptions =
        { "map", "units", "rules", "schedule", "defense", "wave", "waves", "field-size", "no-death" };

    /// <summary>
    /// The options across every verb that are switches rather than pairs. Named
    /// here rather than beside each verb, because a switch that is a pair on one
    /// verb and a switch on another is an argument nobody can spell twice.
    /// </summary>
    private static readonly string[] Switches = { "no-death" };

    private const string RunContentUsage =
        "--map <file> --units <file> --rules <file> --schedule <file> --defense <file> --wave <file>";

    private const string RunShapeUsage = "[--waves <number>] [--field-size <number>] [--no-death]";

    private static readonly string[] Usage =
    {
        "Sim.Cli -- one match or one run, played headless.",
        string.Empty,
        "  run    --bundle <file> --units <file> --rules <file> [--out <directory>]",
        string.Empty,
        "         Plays the bundle to the end and prints the result and the landmarks.",
        "         With --out, writes " + TraceFileName + " and " + LandmarkFileName + " there.",
        string.Empty,
        "  record --map <file> --units <file> --rules <file> --defense <file>",
        "         --wave <file> --seed <number> --out <file> [--map-handle <number>]",
        string.Empty,
        "         Records the content as one self-contained replay bundle, having",
        "         first read it back and played it. Nothing is written if it will",
        "         not replay.",
        string.Empty,
        "         --map-handle says which map the defense claims to be on, for",
        "         looking one up. It is not what pins the geometry -- the map hash",
        "         is -- so leaving it out records a defense that does not say.",
        string.Empty,
        "  play-run   --commands <file> [--out <file>]",
        "             " + RunContentUsage,
        "             " + RunShapeUsage,
        string.Empty,
        "         Plays a command file to the end and prints the run's outcome.",
        "         The seed comes off the record, so the run played is the run the",
        "         decisions were made in. With --out, writes the outcome there.",
        string.Empty,
        "  record-run --script <file> --seed <number> --out <file>",
        "             " + RunContentUsage,
        "             " + RunShapeUsage,
        string.Empty,
        "         Compiles an authored command script into a command file, having",
        "         first read the bytes back and played them to the end. Nothing is",
        "         written if it will not replay.",
        string.Empty,
        "  offerings  --seed <number>",
        "             " + RunContentUsage,
        "             " + RunShapeUsage,
        string.Empty,
        "         Prints every wave's public menu for that seed. A take names a",
        "         kind and an id off one of these, so this is what a command",
        "         script is written from.",
        string.Empty,
        "  sweep      --seed <number> [--runs <number>] [--out <file>]",
        "             " + RunContentUsage,
        "             " + RunShapeUsage,
        "             [--ordinary-options <number>] [--game-changers <number>]",
        "             [--free-snapshots <number>] [--snapshot-price <number>]",
        "             [--most-creeps <number>]",
        string.Empty,
        "         Plays a population of runs per creep and writes the balance",
        "         report as a comma-separated file -- to --out, or to standard",
        "         output where there is no --out. --runs is how many seeds each",
        "         creep is played on and --most-creeps bounds the roster; both",
        "         bounds are reported in the file's own coverage rows.",
        string.Empty,
        "         The four dials retune the ruleset for the sweep alone. Left",
        "         out, each is whatever --rules already says.",
        string.Empty,
        "         --waves and --field-size are N and K: how many waves the run",
        "         lasts and how many opponents each round is resolved against.",
        "         --no-death keeps a run going after its health reaches zero, so",
        "         that a sweep gets N rounds of data out of every row.",
    };

    /// <summary>The entry point. Zero if the run happened, non-zero if it did not.</summary>
    public static int Main(string[] args)
    {
        try
        {
            return Dispatch(args);
        }
        catch (UsageException usage)
        {
            return Refuse(usage.Message, withUsage: true);
        }
        catch (ContentException content)
        {
            return Refuse(content.Message, withUsage: false);
        }
        catch (RecordException record)
        {
            return Refuse(record.Message, withUsage: false);
        }
        catch (RetiredRecordException retired)
        {
            return Refuse(retired.Message, withUsage: false);
        }
        catch (SimulationException simulation)
        {
            return Refuse(simulation.Message, withUsage: false);
        }
        catch (IOException file)
        {
            return Refuse(file.Message, withUsage: false);
        }
        catch (UnauthorizedAccessException file)
        {
            return Refuse(file.Message, withUsage: false);
        }
    }

    private static int Dispatch(string[] args)
    {
        if (args.Length == 0)
        {
            throw new UsageException("No verb. This program does one of six things.");
        }

        switch (args[0])
        {
            case "run":
                return Run(Arguments.Parse("run", args, 1, new[] { "bundle", "units", "rules", "out" }));

            case "record":
                return Record(Arguments.Parse(
                    "record",
                    args,
                    1,
                    new[] { "map", "units", "rules", "defense", "wave", "seed", "out", "map-handle" }));

            case "play-run":
                return PlayRun(RunVerb("play-run", args, "commands", "out"));

            case "record-run":
                return RecordRun(RunVerb("record-run", args, "script", "seed", "out"));

            case "offerings":
                return ShowOfferings(RunVerb("offerings", args, "seed"));

            case "sweep":
                return RunSweep(RunVerb(
                    "sweep",
                    args,
                    "seed",
                    "runs",
                    "out",
                    "ordinary-options",
                    "game-changers",
                    "free-snapshots",
                    "snapshot-price",
                    "most-creeps"));

            default:
                throw new UsageException($"'{args[0]}' is not a verb this program has.");
        }
    }

    /// <summary>A run verb's command line: the options every one of them takes, plus its own.</summary>
    private static Arguments RunVerb(string verb, string[] args, params string[] extra) =>
        Arguments.Parse(verb, args, 1, RunOptions.Concat(extra).ToArray(), Switches);

    /// <summary>
    /// Plays a committed bundle and writes down what happened.
    /// </summary>
    private static int Run(Arguments arguments)
    {
        // The caller opens the file. The simulation receives bytes and text,
        // never a path -- it cannot open anything, and the build gate scans the
        // compiled image to keep it that way.
        byte[] bundle = File.ReadAllBytes(arguments.Required("bundle"));
        string units = File.ReadAllText(arguments.Required("units"));
        string rules = File.ReadAllText(arguments.Required("rules"));

        HeadlessRun run = HeadlessRun.Of(bundle, units, rules);

        Report(run);

        string? directory = arguments.Optional("out");

        if (directory is null)
        {
            return 0;
        }

        Write(Path.Combine(directory, TraceFileName), run.TraceFile());
        Write(Path.Combine(directory, LandmarkFileName), run.LandmarkFile());

        return 0;
    }

    /// <summary>
    /// Records the content as a bundle, having proved it replays.
    /// </summary>
    private static int Record(Arguments arguments)
    {
        (byte[] bytes, HeadlessRun proof) = Recording.Of(
            File.ReadAllText(arguments.Required("map")),
            File.ReadAllText(arguments.Required("units")),
            File.ReadAllText(arguments.Required("rules")),
            File.ReadAllText(arguments.Required("defense")),
            File.ReadAllText(arguments.Required("wave")),
            arguments.RequiredUnsigned("seed"),
            arguments.Optional("map-handle", GhostRecord.NoMapHandle, 0, MaximumMapHandle));

        Report(proof);
        WriteRecord(arguments, bytes);

        return 0;
    }

    /// <summary>
    /// Plays a committed command file and writes down what the run came to.
    /// </summary>
    private static int PlayRun(Arguments arguments)
    {
        string path = arguments.Required("commands");

        // The record's name in any message is the file's name and never its
        // path: the simulation is handed bytes and a label, and a label it
        // could open would be the seam the IL scan exists to keep shut.
        PlayedRun run = PlayedRun.Of(
            Path.GetFileName(path),
            File.ReadAllBytes(path),
            ContentOf(arguments),
            ShapeOf(arguments));

        Report(run);

        string? outcome = arguments.Optional("out");

        if (outcome is null)
        {
            return 0;
        }

        Write(outcome, run.OutcomeFile());

        return 0;
    }

    /// <summary>
    /// Compiles an authored command script into a command file, having proved
    /// it replays.
    /// </summary>
    private static int RecordRun(Arguments arguments)
    {
        string script = arguments.Required("script");

        (byte[] bytes, PlayedRun proof) = PlayedRun.Recorded(
            Path.GetFileName(script),
            File.ReadAllText(script),
            ContentOf(arguments),
            arguments.RequiredUnsigned("seed"),
            ShapeOf(arguments));

        Report(proof);
        WriteRecord(arguments, bytes);

        return 0;
    }

    /// <summary>
    /// Prints every wave's public menu, which is what a command script's takes
    /// are written off.
    /// </summary>
    private static int ShowOfferings(Arguments arguments)
    {
        Sim.Run run = ContentOf(arguments).Fresh(arguments.RequiredUnsigned("seed"), ShapeOf(arguments));

        Console.Out.Write(Offerings.ToText(run));

        return 0;
    }

    /// <summary>
    /// Plays a population of runs per creep and writes the balance report.
    /// </summary>
    /// <remarks>
    /// <b>The harness computes and this writes.</b> Every number below comes off
    /// <see cref="SweepReport"/>; the only decisions made here are which file the
    /// text lands in and what an absent dial means, which is "whatever the
    /// ruleset already says".
    /// </remarks>
    private static int RunSweep(Arguments arguments)
    {
        SweepPlan plan = ContentOf(arguments).Sweep(
            ShapeOf(arguments),
            arguments.RequiredUnsigned("seed"),
            arguments.Optional("runs", SweepPlan.DefaultRunsPerCreep, 1, MaximumRunsPerCreep),
            Dial(arguments, "ordinary-options"),
            Dial(arguments, "game-changers"),
            Dial(arguments, "free-snapshots"),
            Dial(arguments, "snapshot-price"),
            arguments.Optional("most-creeps", SweepPlan.WholeRoster, SweepPlan.WholeRoster, MaximumCreeps));

        SweepReport report = Sim.Sweep.Of(plan);
        string csv = SweepCsv.Of(report);
        string? path = arguments.Optional("out");

        if (path is null)
        {
            Console.Out.Write(csv);

            return 0;
        }

        Write(path, csv);
        Console.Out.Write("swept      " + report.ToString() + "\n");

        for (int index = 0; index < report.Coverage.Count; index++)
        {
            Console.Out.Write("coverage   " + report.Coverage[index].ToString() + "\n");
        }

        return 0;
    }

    /// <summary>
    /// One of the ruleset's retunable numbers, or the value that says the
    /// ruleset's own answer stands.
    /// </summary>
    private static int Dial(Arguments arguments, string name) =>
        arguments.Optional(name, SweepPlan.AsAuthored, 0, int.MaxValue);

    /// <summary>
    /// N, K and the death flag, read the same way for every verb that plays a
    /// run. None of them has a record to come off, so all three are arguments
    /// with the library's own defaults behind them.
    /// </summary>
    private static RunShape ShapeOf(Arguments arguments) =>
        new RunShape(
            arguments.Optional("waves", Sim.Run.DefaultWaves, 1, MaximumWaves),
            arguments.Optional("field-size", Sim.Run.DefaultFieldSize, 1, MaximumFieldSize),
            !arguments.Given("no-death"));

    /// <summary>The six files every run verb is handed, read here and parsed there.</summary>
    private static RunContent ContentOf(Arguments arguments) =>
        RunContent.Of(
            File.ReadAllText(arguments.Required("map")),
            File.ReadAllText(arguments.Required("units")),
            File.ReadAllText(arguments.Required("rules")),
            File.ReadAllText(arguments.Required("schedule")),
            File.ReadAllText(arguments.Required("defense")),
            File.ReadAllText(arguments.Required("wave")));

    /// <summary>The stream's stamps, the shape it was played at, and the vector.</summary>
    private static void Report(PlayedRun run)
    {
        Console.Out.Write(
            run.Stream.ToString()
            + "\n"
            + run.ShapeLine()
            + "\n"
            + run.Summary()
            + "\n"
            + run.Rounds()
            + "\n");
    }

    /// <summary>
    /// A record, written where <c>--out</c> says and said to have been written.
    /// </summary>
    /// <remarks>
    /// The sentence names the size and says the bytes were proved, because both
    /// record verbs return bytes that have already been read back, gated and
    /// played to the end -- a line saying only that a file appeared would read
    /// the same for a writer that had done none of it.
    /// </remarks>
    private static void WriteRecord(Arguments arguments, byte[] bytes)
    {
        string path = arguments.Required("out");

        MakeRoomFor(path);
        File.WriteAllBytes(path, bytes);
        Console.Out.Write(
            "wrote      "
            + path
            + " ("
            + bytes.Length.ToString(PlainText.Culture)
            + " bytes, read back and replayed before writing)\n");
    }

    /// <summary>The result triple, the final hash, and the landmark table.</summary>
    private static void Report(HeadlessRun run)
    {
        Console.Out.Write(
            run.Bundle.Header.ToString()
            + "\nseed       "
            + run.Bundle.Seed.ToString(PlainText.Culture)
            + "\n"
            + run.DefenseLine()
            + "\nwave       "
            + run.Bundle.WaveId.ToString()
            + "\n"
            + run.Summary()
            + "\n"
            + run.Landmarks.ToText()
            + "\n");
    }

    /// <summary>A generated text file, in the directory it asked for.</summary>
    private static void Write(string path, string text)
    {
        MakeRoomFor(path);
        File.WriteAllText(path, text, PlainText.Utf8);
        Console.Out.Write("wrote      " + path + "\n");
    }

    /// <summary>The directory a file is about to be written into.</summary>
    private static void MakeRoomFor(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static int Refuse(string message, bool withUsage)
    {
        Console.Error.Write(message + "\n");

        if (withUsage)
        {
            Console.Error.Write("\n" + string.Join("\n", Usage) + "\n");
        }

        return 1;
    }
}
