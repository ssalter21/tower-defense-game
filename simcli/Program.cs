using Sim;

namespace Sim.Cli;

/// <summary>
/// The headless runner. Give it a replay bundle and it plays the whole match
/// with nobody watching, then says what happened.
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

    private static readonly string[] Usage =
    {
        "Sim.Cli -- one match, played headless.",
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
            throw new UsageException("No verb. This program does one of two things.");
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

            default:
                throw new UsageException($"'{args[0]}' is not a verb this program has.");
        }
    }

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
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

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
