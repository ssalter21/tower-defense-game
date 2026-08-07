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
    /// The six content files a run is built from, and the two numbers that say
    /// how long it lasts and how wide its field is. Every run verb takes all
    /// eight, so the list is written once.
    /// </summary>
    private static readonly string[] RunOptions =
        { "map", "units", "rules", "schedule", "defense", "wave", "waves", "field-size" };

    private const string RunContentUsage =
        "--map <file> --units <file> --rules <file> --schedule <file> --defense <file> --wave <file>";

    private const string RunShapeUsage = "[--waves <number>] [--field-size <number>]";

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
        "         --waves and --field-size are N and K: how many waves the run",
        "         lasts and how many opponents each round is resolved against.",
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
            throw new UsageException("No verb. This program does one of five things.");
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
                return PlayRun(Arguments.Parse("play-run", args, 1, With("commands", "out")));

            case "record-run":
                return RecordRun(Arguments.Parse("record-run", args, 1, With("script", "seed", "out")));

            case "offerings":
                return ShowOfferings(Arguments.Parse("offerings", args, 1, With("seed")));

            default:
                throw new UsageException($"'{args[0]}' is not a verb this program has.");
        }
    }

    /// <summary>The options every run verb takes, plus the ones only this verb does.</summary>
    private static string[] With(params string[] extra) => RunOptions.Concat(extra).ToArray();

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

        Directory.CreateDirectory(directory);
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

        string path = arguments.Required("out");

        MakeRoomFor(path);
        File.WriteAllBytes(path, bytes);

        Report(proof);
        Console.Out.Write(
            "wrote      "
            + path
            + " ("
            + bytes.Length.ToString(PlainText.Culture)
            + " bytes, read back and replayed before writing)\n");

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
            arguments.Optional("waves", Sim.Run.DefaultWaves, 1, MaximumWaves),
            arguments.Optional("field-size", Sim.Run.DefaultFieldSize, 1, MaximumFieldSize));

        Report(run);

        string? outcome = arguments.Optional("out");

        if (outcome is null)
        {
            return 0;
        }

        MakeRoomFor(outcome);
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
            arguments.Optional("waves", Sim.Run.DefaultWaves, 1, MaximumWaves),
            arguments.Optional("field-size", Sim.Run.DefaultFieldSize, 1, MaximumFieldSize));

        string path = arguments.Required("out");

        MakeRoomFor(path);
        File.WriteAllBytes(path, bytes);

        Report(proof);
        Console.Out.Write(
            "wrote      "
            + path
            + " ("
            + bytes.Length.ToString(PlainText.Culture)
            + " bytes, read back and replayed before writing)\n");

        return 0;
    }

    /// <summary>
    /// Prints every wave's public menu, which is what a command script's takes
    /// are written off.
    /// </summary>
    private static int ShowOfferings(Arguments arguments)
    {
        Sim.Run run = ContentOf(arguments).Fresh(
            arguments.RequiredUnsigned("seed"),
            arguments.Optional("waves", Sim.Run.DefaultWaves, 1, MaximumWaves),
            arguments.Optional("field-size", Sim.Run.DefaultFieldSize, 1, MaximumFieldSize));

        Console.Out.Write(Offerings.ToText(run));

        return 0;
    }

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

    /// <summary>The directory a file is about to be written into.</summary>
    private static void MakeRoomFor(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }
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

    private static void Write(string path, string text)
    {
        File.WriteAllText(path, text, PlainText.Utf8);
        Console.Out.Write("wrote      " + path + "\n");
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
