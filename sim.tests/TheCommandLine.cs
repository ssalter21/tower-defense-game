using System.Diagnostics;
using System.Text;

namespace Sim.Tests;

/// <summary>
/// The command line as a process: built once, invoked with arguments, and
/// answering with an exit code and whatever it printed.
/// </summary>
/// <remarks>
/// <para>
/// <b>A process and not a method call.</b> What the verbs are made of is
/// argument parsing and file IO, and neither of those exists when
/// <c>Program.Main</c> is called with an array from inside a test host: the
/// paths, the exit code and the two streams are the thing under test. It costs
/// a helper rather than a seam, because the gate already knows how to build a
/// project into scratch space.
/// </para>
/// <para>
/// The runner is built against the <b>committed</b> simulation, exactly as the
/// shell script builds it, so what these tests exercise is the image in the
/// repository.
/// </para>
/// </remarks>
public static class TheCommandLine
{
    private static readonly Lazy<string> LazyProgram = new(() =>
        Path.Combine(RepoLayout.Build(RepoLayout.CliProject, "simcli"), "Sim.Cli.dll"));

    /// <summary>The seed the committed command stream carries.</summary>
    public const ulong RunSeed = 20260807UL;

    /// <summary>The built runner, as a path <c>dotnet</c> will run.</summary>
    public static string Program => LazyProgram.Value;

    /// <summary>
    /// The content every run verb takes: the committed directory, out of which
    /// the runner takes all seven files by the names it declares.
    /// </summary>
    /// <remarks>
    /// <b>Which means the field file and never the wave file.</b> A run's own
    /// waves come off the command stream's build phases; the canned opponent
    /// each round is resolved against is <c>content/field.txt</c>, and
    /// <c>content/wave.txt</c> is a whole authored match that outspends anything
    /// a purse composes. A suite pointed at that one measures every verb here
    /// against an opponent no player could be -- and nothing about the numbers it
    /// produces looks wrong. Naming the seven here is what let that happen; the
    /// declaration is now the only place the pairing is written down.
    /// </remarks>
    public static string[] RunContent => new[] { "--content", RepoLayout.ContentDirectory };

    /// <summary>
    /// A directory of this test run's own, emptied first. Nothing here writes
    /// into <c>content/</c>: a check whose subject is the file it just wrote is
    /// a check that cannot fail.
    /// </summary>
    public static string Scratch(string label)
    {
        string path = Path.Combine(Path.GetTempPath(), "sim-cli-gate", label);

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }

        Directory.CreateDirectory(path);

        return path;
    }

    /// <summary>Runs the command line and hands back what it did.</summary>
    public static CommandLineResult Invoke(params string[] args) => Invoke((IEnumerable<string>)args);

    /// <summary>Runs the command line and hands back what it did.</summary>
    /// <remarks>
    /// Standard input is not redirected, because no verb reads it. The one that
    /// did was <c>play</c>, deleted with the shell specification it was built
    /// from; a harness that could still type at a verb would be a shape nothing
    /// in this program has.
    /// </remarks>
    public static CommandLineResult Invoke(IEnumerable<string> args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = RepoLayout.Root,
        };

        startInfo.ArgumentList.Add(Program);

        foreach (string argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // BOTH STREAMS ARE DRAINED AT ONCE, not one and then the other. A pipe
        // holds a few kilobytes; past that a write blocks until somebody reads.
        // Reading stdout to the end first therefore deadlocks against any verb
        // whose stderr is bigger than one pipeful -- the child waits for its
        // stderr to be read, the test waits for a stdout that will never close,
        // and neither ever moves.
        //
        // OBSERVED: read the two sequentially again and run this class. The
        // misspelled-option case below hangs indefinitely, because a usage
        // refusal prints the whole usage block to stderr and that block is now
        // over a pipeful long. It hangs rather than failing, which is the worst
        // shape a gate can take.
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the command line.");

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        return new CommandLineResult(process.ExitCode, output.Result, error.Result);
    }
}

/// <summary>What one invocation of the command line came to.</summary>
public sealed class CommandLineResult
{
    internal CommandLineResult(int exitCode, string output, string error)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
    }

    /// <summary>Zero if the verb happened, non-zero if it refused.</summary>
    public int ExitCode { get; }

    /// <summary>What it printed for a person to read.</summary>
    public string Output { get; }

    /// <summary>Its refusal, if it refused.</summary>
    public string Error { get; }

    /// <summary>The exit code, asserted, with everything it printed in the message.</summary>
    public CommandLineResult Succeeded()
    {
        Assert.True(
            ExitCode == 0,
            new StringBuilder()
                .Append("The command line exited ")
                .Append(ExitCode)
                .Append(" where it was expected to do the thing.\n")
                .Append(Error)
                .Append(Output)
                .ToString());

        return this;
    }
}
