using System.Text.RegularExpressions;

namespace Sim.Tests;

/// <summary>
/// The one rule in this ticket that has to be checked in source, because it
/// leaves no residue in either artefact.
/// </summary>
/// <remarks>
/// <para>
/// <c>#if DEBUG</c> and its relatives are resolved by the compiler before any
/// metadata exists. Code inside a branch that was not taken is simply not
/// there afterwards, so the IL scan -- which is otherwise the stronger
/// mechanism, since it checks the image that ships rather than the text
/// someone last read -- is structurally incapable of seeing this one. There is
/// nothing to find.
/// </para>
/// <para>
/// The rule matters because the simulation's invariants are unconditional
/// throws. The corridor is one hex wide, towers are sorted by coordinate,
/// orders are unique on <c>(tick, type)</c>, three header copies agree: every
/// one of those is a load-time assertion, and every one of them compiled out
/// would mean the shipping build is the one <b>without</b> the loud-failure
/// architecture that everything else in this design rests on.
/// </para>
/// </remarks>
public class PreprocessorSourceTests
{
    /// <summary>
    /// Conditional-compilation directives. <c>#region</c>, <c>#nullable</c>,
    /// <c>#pragma</c> and <c>#line</c> are deliberately absent: none of them
    /// can make code exist in one configuration and not another, which is the
    /// property being banned.
    /// </summary>
    private static readonly Regex Directive = new(
        @"^\s*#\s*(if|ifdef|ifndef|elif|else|endif|define|undef)\b",
        RegexOptions.Compiled);

    [Fact]
    public void No_simulation_source_file_compiles_conditionally()
    {
        var offences = new List<string>();

        foreach (string file in SimulationSources())
        {
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                if (Directive.IsMatch(lines[index]))
                {
                    offences.Add($"{Path.GetRelativePath(RepoLayout.Root, file)}({index + 1}): {lines[index].Trim()}");
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "Conditional compilation inside the simulation. An invariant that can vanish from a "
            + "configuration is an unenforced rule that has been promoted to a believed one."
            + Environment.NewLine
            + string.Join(Environment.NewLine, offences));
    }

    [Fact]
    public void The_check_is_looking_at_the_simulation_and_not_at_nothing()
    {
        // A file list that silently came back empty would make the test above
        // green forever. Two independent things have to hold: there are source
        // files, and the ones this ticket added are among them.
        string[] sources = SimulationSources().ToArray();

        Assert.NotEmpty(sources);
        Assert.Contains(sources, path => Path.GetFileName(path) == "Fix64.cs");
        Assert.Contains(sources, path => Path.GetFileName(path) == "Pcg32.cs");
        Assert.Contains(sources, path => Path.GetFileName(path) == "DataText.cs");
        Assert.Contains(sources, path => Path.GetFileName(path) == "HexMap.cs");
    }

    [Theory]
    [InlineData("#if DEBUG")]
    [InlineData("    #if DEBUG")]
    [InlineData("#  if NET6_0_OR_GREATER")]
    [InlineData("#else")]
    [InlineData("#endif")]
    [InlineData("#define TRACE_TICKS")]
    [InlineData("#undef TRACE_TICKS")]
    [InlineData("#elif UNITY_EDITOR")]
    public void The_matcher_recognises_every_form_it_bans(string line)
    {
        Assert.Matches(Directive, line);
    }

    [Theory]
    [InlineData("#region arithmetic")]
    [InlineData("#nullable enable")]
    [InlineData("#pragma warning disable CS0219")]
    [InlineData("// #if DEBUG in a comment is prose, not compilation")]
    [InlineData("int different = 1;")]
    public void The_matcher_leaves_alone_what_it_does_not_ban(string line)
    {
        Assert.DoesNotMatch(Directive, line);
    }

    private static IEnumerable<string> SimulationSources() =>
        Directory
            .EnumerateFiles(RepoLayout.SimDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
}
