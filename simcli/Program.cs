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

    /// <summary>What <see cref="AllInBot"/> is called on the command line.</summary>
    private const string AllIn = "all-in";

    /// <summary>
    /// The directory a run's content is taken out of where a file is not named
    /// outright. Every file of <see cref="RunContentFiles"/> is looked for in it
    /// under the name that declaration gives.
    /// </summary>
    /// <remarks>
    /// A directory somebody named is not a default: nothing is assumed about
    /// where content lives, and a file missing from the directory refuses by the
    /// path it looked at. What it buys is that the seven names are written in one
    /// declaration rather than in every invocation and every shell script that
    /// makes one.
    /// </remarks>
    private const string ContentDirectoryOption = "content";

    /// <summary>The left margin a verb's continuation lines sit on.</summary>
    private const string UsageIndent = "             ";

    /// <summary>The left margin the usage block's prose sits on.</summary>
    private const string ProseIndent = "         ";

    /// <summary>
    /// Every option a run verb takes: the content files, the directory they can
    /// be found in, and the three arguments that say how long the run lasts, how
    /// wide its field is and whether death ends it.
    /// </summary>
    /// <remarks>
    /// <b>The content half is read off <see cref="RunContentFiles"/></b> rather
    /// than typed out, so an option offered here is an option something opens.
    /// A list written by hand can carry a file no verb reads and omit one every
    /// verb needs, and both of those compile.
    /// </remarks>
    private static readonly string[] RunOptions = RunContentFiles.All
        .Select(file => file.Option)
        .Concat(new[] { ContentDirectoryOption, "waves", "field-size", "no-death" })
        .ToArray();

    /// <summary>
    /// The options across every verb that are switches rather than pairs. Named
    /// here rather than beside each verb, because a switch that is a pair on one
    /// verb and a switch on another is an argument nobody can spell twice.
    /// </summary>
    private static readonly string[] Switches = { "no-death", "per-run" };

    /// <summary>
    /// The content every run verb takes, as an invocation writes it: a directory
    /// holding the seven, or each of them named outright.
    /// </summary>
    private static readonly string RunContentUsage =
        "--" + ContentDirectoryOption + " <directory>, or each file named outright:\n"
        + UsageIndent
        + Wrapped(RunContentFiles.All.Select(file => "--" + file.Option + " <file>"));

    private const string RunShapeUsage = "[--waves <number>] [--field-size <number>] [--no-death]";

    private static readonly string[] Usage =
    {
        "Sim.Cli -- one match or one run, played headless.",
        string.Empty,
        "  run    --bundle <file> --units <file> --upgrades <file> --rules <file>",
        "         [--out <directory>]",
        string.Empty,
        "         Plays the bundle to the end and prints the result and the landmarks.",
        "         With --out, writes " + TraceFileName + " and " + LandmarkFileName + " there.",
        string.Empty,
        "  restage --bundle <file> --units <file> --upgrades <file> --rules <file>",
        "          [--out <directory>]",
        string.Empty,
        "         The same, with the simulation version and content hash gates set",
        "         aside, and every line it writes labelled as a restaging. The",
        "         outcome is what that defense and that wave do today; it is not",
        "         that record's result, and this verb exists so that asking the",
        "         question cannot be confused with replaying.",
        string.Empty,
        "  record --map <file> --units <file> --upgrades <file> --rules <file>",
        "         --defense <file> --wave <file> --seed <number> --out <file>",
        "         [--map-handle <number>]",
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
        "  ladder     --units <file> --upgrades <file>",
        string.Empty,
        "         Prints which unit follows which, with the price of each tier,",
        "         then what a walk over the whole ladder had to say about it --",
        "         its roots, its leaves and any upgrade that is not dearer than",
        "         what it replaces, then any fault.",
        string.Empty,
        "         It always exits zero, faults included. What fails a build on a",
        "         fault is a test; this reads a roster and enforces nothing.",
        string.Empty,
        "  draw-map   --map <file> --out <file>",
        string.Empty,
        "         Draws the map as a picture -- one hexagon per cell at the",
        "         offsets the odd-r grid puts them, tinted and lettered by tier",
        "         -- and writes it as scalable vector graphics, which a browser",
        "         opens and a diff can be read. It prints the shape, the",
        "         corridor and how many hexes stand at each tier.",
        string.Empty,
        "         The picture is of the PARSED map, so a file that will not load",
        "         produces the refusal and no picture at all.",
        string.Empty,
        "  sweep      --seed <number> [--runs <number>] [--out <file>]",
        "             " + RunContentUsage,
        "             " + RunShapeUsage,
        "             [--free-snapshots <number>] [--snapshot-price <number>]",
        "             [--most-creeps <number>] [--policy <name>] [--per-run]",
        string.Empty,
        "         Plays a population of runs per creep and writes the balance",
        "         report as a comma-separated file -- to --out, or to standard",
        "         output where there is no --out. --runs is how many seeds each",
        "         creep is played on and --most-creeps bounds the roster; both",
        "         bounds are reported in the file's own coverage rows.",
        string.Empty,
        "         --policy names the scripted player: " + SweepPlan.EvenShare + ", which",
        "         spends half of every purse on the board and half on the wave,",
        "         or " + AllIn + ", which builds nothing and sends the lot. Two reports",
        "         under the two of them are what says what the defensive half of",
        "         a round is worth. The name is a row of the file.",
        string.Empty,
        "         --per-run writes a row for every run under the folded ones,",
        "         each naming the seed it was played on -- the distribution the",
        "         fold is a summary of. It is off by default because the row",
        "         count is the roster times the sample.",
        string.Empty,
        "         The four dials retune the ruleset for the sweep alone. Left",
        "         out, each is whatever --rules already says.",
        string.Empty,
        "         --waves and --field-size are N and K: how many waves the run",
        "         lasts and how many opponents each round is resolved against.",
        "         --no-death keeps a run going after its health reaches zero, so",
        "         that a sweep gets N rounds of data out of every row.",
        string.Empty,
        "  Where a run's content comes from",
        string.Empty,
        ProseIndent + "--content names a directory and takes the files above out of",
        ProseIndent + "it by name:",
        ProseIndent + Wrapped(RunContentFiles.All.Select(file => file.FileName), ProseIndent),
        string.Empty,
        ProseIndent + "Naming a file outright overrides the directory for that one",
        ProseIndent + "file, so scoring another board is one argument rather than a",
        ProseIndent + "whole set. Every file is required either way: an optional",
        ProseIndent + "content file is a default, and a default is a number nobody",
        ProseIndent + "authored folded into a content hash as though somebody had.",
        string.Empty,
        "  The two files that hold orders, and why they are two",
        string.Empty,
        "         --field is the canned opponent, and every verb that plays a run",
        "         takes it: play-run, record-run and sweep. It is one",
        "         round's worth of orders standing behind --defense, drawn with",
        "         replacement to make the field of K a round is resolved against.",
        "         A build phase composes what is sent rather than when, so every",
        "         order of one releases on tick 0 and a file whose orders arrive",
        "         over time is refused here rather than swept against.",
        string.Empty,
        "         --wave is a whole authored match, released over time, and",
        "         'record' is the only verb that takes it. A run's own waves come",
        "         from the build phases on the command stream and are read from no",
        "         file at all.",
        string.Empty,
        "         --defense is the OPPONENTS' defense and never this run's. It is",
        "         the wall every member of the canned field stands behind. A run",
        "         opens on an empty board and stands whatever its own build",
        "         phases put on the map, so nothing hands it a defense at all.",
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
            throw new UsageException("No verb. This program does one of eight things.");
        }

        switch (args[0])
        {
            case "run":
                return Run(Arguments.Parse(
                    "run",
                    args,
                    1,
                    new[] { "bundle", "units", "upgrades", "rules", "out" }));

            case "restage":
                return Restage(Arguments.Parse(
                    "restage",
                    args,
                    1,
                    new[] { "bundle", "units", "upgrades", "rules", "out" }));

            case "record":
                return Record(Arguments.Parse(
                    "record",
                    args,
                    1,
                    new[] { "map", "units", "upgrades", "rules", "defense", "wave", "seed", "out", "map-handle" }));

            case "play-run":
                return PlayRun(RunVerb("play-run", args, "commands", "out"));

            case "record-run":
                return RecordRun(RunVerb("record-run", args, "script", "seed", "out"));

            // Two files and not a RunVerb: a ladder is read against the roster
            // and against nothing else, so asking for a map, a schedule, a
            // defense and a wave to print one would be six arguments nobody's
            // answer depends on.
            case "ladder":
                return ShowLadder(Arguments.Parse("ladder", args, 1, new[] { "units", "upgrades" }));

            // One file in and one file out, for the same reason the ladder
            // takes two: a map is read against nothing at all, so asking for a
            // roster and a ruleset to draw one would be five arguments nobody's
            // picture depends on.
            case "draw-map":
                return DrawMap(Arguments.Parse("draw-map", args, 1, new[] { "map", "out" }));

            case "sweep":
                return RunSweep(RunVerb(
                    "sweep",
                    args,
                    "seed",
                    "runs",
                    "out",
                    "free-snapshots",
                    "snapshot-price",
                    "most-creeps",
                    "policy",
                    "per-run"));

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
    private static int Run(Arguments arguments) => Play(arguments, HeadlessRun.Of);

    /// <summary>
    /// Runs a committed bundle's defense and wave under today's rules, and says
    /// in what it writes that this is not that record's result.
    /// </summary>
    /// <remarks>
    /// The verb <c>content/golden/</c> is checked with. Those bundles are kept
    /// forever and cannot be made again, so a simulation version bump would
    /// otherwise retire the only evidence that each retired reader branch still
    /// reads. See <see cref="HeadlessRun.Restaged"/>.
    /// </remarks>
    private static int Restage(Arguments arguments) => Play(arguments, HeadlessRun.Restaged);

    /// <summary>
    /// The two bundle verbs, which differ in one call and in nothing else. Both
    /// read the same three files, print the same report and write the same two
    /// generated files, so the difference between them stays visible as the one
    /// thing it is.
    /// </summary>
    private static int Play(Arguments arguments, Func<byte[], string, string, string, HeadlessRun> play)
    {
        // The caller opens the file. The simulation receives bytes and text,
        // never a path -- it cannot open anything, and the build gate scans the
        // compiled image to keep it that way.
        byte[] bundle = File.ReadAllBytes(arguments.Required("bundle"));
        string units = File.ReadAllText(arguments.Required("units"));
        string upgrades = File.ReadAllText(arguments.Required("upgrades"));
        string rules = File.ReadAllText(arguments.Required("rules"));

        HeadlessRun run = play(bundle, units, upgrades, rules);

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
            File.ReadAllText(arguments.Required("upgrades")),
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
    /// Prints the upgrade ladder, and exits zero whatever it found.
    /// </summary>
    /// <remarks>
    /// Faults included. What fails a build on one is a test in <c>sim.tests</c>;
    /// this verb is for reading a roster, and a second enforcer here would be one
    /// rule with two homes.
    /// </remarks>
    private static int ShowLadder(Arguments arguments)
    {
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(arguments.Required("units")));
        UpgradeLadder ladder = UpgradeLadder.Parse(
            File.ReadAllText(arguments.Required("upgrades")),
            types);

        Console.Out.Write(Ladder.ToText(types, ladder));

        return 0;
    }

    /// <summary>
    /// Draws a map file as a picture, and says what the loader made of it.
    /// </summary>
    /// <remarks>
    /// <b>The picture is of the parsed map, so a file that will not load
    /// produces the refusal instead of a drawing.</b> That is the whole value
    /// of routing this through the program rather than colouring the characters
    /// in place: what comes out is what the simulation read, corridor assertion
    /// and all, and a second reader would eventually disagree with the first
    /// about exactly the maps somebody is in the middle of editing.
    /// </remarks>
    private static int DrawMap(Arguments arguments)
    {
        HexMap map = HexMap.Parse("map", File.ReadAllText(arguments.Required("map")));

        Console.Out.Write(MapPicture.Summary(map));
        Write(arguments.Required("out"), MapPicture.ToSvg(map));

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
        string name = arguments.Optional("policy") ?? SweepPlan.EvenShare;

        SweepPlan plan = ContentOf(arguments).Sweep(
            ShapeOf(arguments),
            arguments.RequiredUnsigned("seed"),
            arguments.Optional("runs", SweepPlan.DefaultRunsPerCreep, 1, MaximumRunsPerCreep),
            Dial(arguments, "free-snapshots"),
            Dial(arguments, "snapshot-price"),
            arguments.Optional("most-creeps", SweepPlan.WholeRoster, SweepPlan.WholeRoster, MaximumCreeps),
            PolicyOf(name),
            arguments.Given("per-run"),
            name);

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
    /// The scripted player a name asks for, or a refusal naming the ones this
    /// program has.
    /// </summary>
    /// <remarks>
    /// <b>An unrecognised name is refused rather than defaulted.</b> Falling
    /// back would produce a complete and correct-looking report about a player
    /// nobody asked for, and a misspelling is exactly how somebody comparing two
    /// strategies ends up comparing one against itself.
    /// </remarks>
    private static BuildPolicy PolicyOf(string name) =>
        name switch
        {
            SweepPlan.EvenShare => EvenShareBot.Decide,
            AllIn => AllInBot.Decide,
            _ => throw new UsageException(
                $"'{name}' is not a player this program has. It sweeps under "
                + $"{SweepPlan.EvenShare}, which spends half of every purse on the board and half on the "
                + $"wave, or {AllIn}, which builds nothing and sends the lot."),
        };

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

    /// <summary>
    /// The seven files every run verb is handed, opened here and parsed there.
    /// </summary>
    /// <remarks>
    /// The order is the one <see cref="RunContent.Of"/> layers the content in,
    /// and each file is named by its row of <see cref="RunContentFiles"/> rather
    /// than by a string spelled here -- so an option this reads is an option the
    /// verb takes and a file the layout tests look for.
    /// </remarks>
    private static RunContent ContentOf(Arguments arguments) =>
        RunContent.Of(
            TextOf(arguments, RunContentFiles.Map),
            TextOf(arguments, RunContentFiles.Units),
            TextOf(arguments, RunContentFiles.Upgrades),
            TextOf(arguments, RunContentFiles.Rules),
            TextOf(arguments, RunContentFiles.Defense),
            TextOf(arguments, RunContentFiles.Field));

    /// <summary>
    /// One content file's text: out of the path its own option named, or out of
    /// the directory <c>--content</c> named, under the name it is declared with.
    /// </summary>
    /// <remarks>
    /// The option wins where both are given, so pointing a run at another board
    /// is one argument beside the directory rather than seven instead of it. The
    /// simulation is handed what comes back and never the path it came from,
    /// which is the seam ADR-0018 draws and the build gate scans for.
    /// </remarks>
    private static string TextOf(Arguments arguments, ContentFile file)
    {
        string? named = arguments.Optional(file.Option);

        if (named is not null)
        {
            return File.ReadAllText(named);
        }

        string? directory = arguments.Optional(ContentDirectoryOption);

        if (directory is not null)
        {
            return File.ReadAllText(Path.Combine(directory, file.FileName));
        }

        throw new UsageException(
            $"'{arguments.Verb}' needs --{file.Option}, and it was not given. Name the file, or name a "
            + $"directory holding {file.FileName} with --{ContentDirectoryOption}.");
    }

    /// <summary>
    /// Words laid out one line under another, so that a list the usage block
    /// prints grows a line rather than running off the terminal.
    /// </summary>
    private static string Wrapped(IEnumerable<string> words, string indent = UsageIndent, int width = 64)
    {
        var lines = new List<string>();
        string line = string.Empty;

        foreach (string word in words)
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line);
                line = string.Empty;
            }

            line = line.Length == 0 ? word : line + " " + word;
        }

        lines.Add(line);

        return string.Join("\n" + indent, lines);
    }

    /// <summary>
    /// The stream's stamps, the shape it was played at, the vector, and the
    /// board the run ended on.
    /// </summary>
    /// <remarks>
    /// Both run verbs print through here, so a recording and a replay say the
    /// same things about the same run in the same order.
    /// </remarks>
    private static void Report(PlayedRun run)
    {
        Console.Out.Write(
            run.Stream.ToString()
            + "\n"
            + run.ShapeLine()
            + "\n"
            + run.Summary()
            + "\n"
            + run.RoundsAndBoard()
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

        PlainText.RoomFor(path);
        File.WriteAllBytes(path, bytes);
        Console.Out.Write(
            "wrote      "
            + path
            + " ("
            + bytes.Length.ToString(PlainText.Culture)
            + " bytes, read back and replayed before writing)\n");
    }

    /// <summary>The result triple, the final hash, and the landmark table.</summary>
    /// <remarks>
    /// A restaging says so on its own line, above everything the run produced.
    /// The label is emitted by <see cref="Sim.Restaging"/> itself rather than
    /// composed here, so nothing that prints one of these can present the
    /// outcome as the record's own by forgetting to.
    /// </remarks>
    private static void Report(HeadlessRun run)
    {
        if (run.Restaging is not null)
        {
            Console.Out.Write("restaged   " + run.Restaging.ToString() + "\n");
        }

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
            + run.Landmarks.ToReportText()
            + "\n");
    }

    /// <summary>A generated text file, in the directory it asked for.</summary>
    private static void Write(string path, string text)
    {
        PlainText.Written(path, text);
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
