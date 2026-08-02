using Sim.Tests.Scan;

namespace Sim.Tests;

/// <summary>
/// The positive control. A test that cannot fail is not a test, so every
/// clause of the scan is watched failing on a violation planted for it.
/// </summary>
/// <remarks>
/// <para>
/// The assertions are per row and per clause rather than "the poison assembly
/// has some findings", and that is the whole design. A single aggregate
/// assertion is satisfied by one working clause, so six broken ones would hide
/// behind it and the scan would report a confident green over an assembly full
/// of floats. Each row below is required to fire, and required to fire
/// <b>from its own poison type</b>, so a row cannot be credited to a violation
/// planted for a different row.
/// </para>
/// </remarks>
public class PoisonSuiteTests
{
    private static readonly Lazy<IReadOnlyList<BanFinding>> LazyFindings =
        new(() => IlScan.Scan(RepoLayout.PoisonAssembly));

    private static IReadOnlyList<BanFinding> Findings => LazyFindings.Value;

    [Theory]
    [InlineData(BanRow.Floats, "PoisonFloats")]
    [InlineData(BanRow.Math, "PoisonMath")]
    [InlineData(BanRow.HashedCollections, "PoisonHashedCollections")]
    [InlineData(BanRow.UnstableSorts, "PoisonUnstableSort")]
    [InlineData(BanRow.AmbientTimeAndRandomness, "PoisonAmbient")]
    [InlineData(BanRow.Threading, "PoisonThreading")]
    [InlineData(BanRow.ConditionalDiagnostics, "PoisonConditionalDiagnostics")]
    [InlineData(BanRow.AmbientIo, "PoisonAmbientIo")]
    public void Every_banned_row_fires_from_its_own_deliberate_violation(BanRow row, string poisonType)
    {
        BanFinding[] forRow = Findings.Where(finding => finding.Row == row).ToArray();

        Assert.True(
            forRow.Length > 0,
            $"The scan found nothing for {row}, so that clause is not enforcing anything. "
            + $"All findings:{Environment.NewLine}{Describe(Findings)}");

        Assert.True(
            forRow.Any(finding => finding.Site.Contains(poisonType, StringComparison.Ordinal)),
            $"{row} fired, but not from {poisonType} -- so it was credited to some other type's "
            + $"violation and its own clause is unproven. Findings for {row}:{Environment.NewLine}"
            + Describe(forRow));
    }

    [Theory]
    [InlineData(BanTable.ClauseFloatSignature)]
    [InlineData(BanTable.ClauseFloatLocal)]
    [InlineData(BanTable.ClauseFloatInstruction)]
    [InlineData(BanTable.ClauseBannedType)]
    [InlineData(BanTable.ClauseBannedMember)]
    public void Every_clause_of_the_scan_fires(string clause)
    {
        Assert.True(
            Findings.Any(finding => finding.Clause == clause),
            $"No finding came from the {clause} clause, so that piece of the scanner is dead code. "
            + $"All findings:{Environment.NewLine}{Describe(Findings)}");
    }

    [Fact]
    public void Floating_point_is_caught_in_all_three_places_it_can_hide()
    {
        // Signature, local slot and instruction stream are three separate
        // pieces of scanning code, and only the first is visible to anything
        // that reads source. This asserts all three fire on the one poisoned
        // method, so none of them can be quietly broken.
        BanFinding[] floats = Findings
            .Where(finding => finding.Row == BanRow.Floats
                && finding.Site.Contains("PoisonFloats", StringComparison.Ordinal))
            .ToArray();

        Assert.Contains(floats, finding => finding.Clause == BanTable.ClauseFloatSignature);
        Assert.Contains(floats, finding => finding.Clause == BanTable.ClauseFloatLocal);
        Assert.Contains(floats, finding => finding.Clause == BanTable.ClauseFloatInstruction);
    }

    [Fact]
    public void All_eight_rows_are_accounted_for()
    {
        BanRow[] fired = Findings.Select(finding => finding.Row).Distinct().OrderBy(row => row).ToArray();
        BanRow[] all = Enum.GetValues<BanRow>().OrderBy(row => row).ToArray();

        Assert.Equal(all, fired);
    }

    [Fact]
    public void The_conditional_diagnostics_row_is_only_catchable_because_the_build_is_debug()
    {
        // This is the load-bearing side effect of committing Debug, asserted
        // rather than believed. Debug.Assert is [Conditional("DEBUG")], so a
        // Release image contains no trace of it -- meaning that if Release
        // were the committed configuration, this row would be a clause that
        // can never fire and the ban would be enforced by nothing at all.
        string releaseOutput = RepoLayout.Build(RepoLayout.PoisonProject, "poison-release", "Release");
        IReadOnlyList<BanFinding> release = IlScan.Scan(Path.Combine(releaseOutput, "Sim.Poison.dll"));

        Assert.DoesNotContain(release, finding => finding.Row == BanRow.ConditionalDiagnostics);
        Assert.Contains(Findings, finding => finding.Row == BanRow.ConditionalDiagnostics);
    }

    [Fact]
    public void The_poison_project_is_referenced_by_nothing()
    {
        string[] projects = Directory
            .EnumerateFiles(RepoLayout.Root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "Library" + Path.DirectorySeparatorChar))
            .Where(path => Path.GetFileName(path) != "Sim.Poison.csproj")
            .ToArray();

        foreach (string project in projects)
        {
            Assert.DoesNotContain("Sim.Poison", File.ReadAllText(project), StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string Describe(IEnumerable<BanFinding> findings) =>
        string.Join(Environment.NewLine, findings.Select(finding => "  " + finding));
}
