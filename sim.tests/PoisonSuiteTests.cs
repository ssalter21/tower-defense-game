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

    private static string Describe(IEnumerable<BanFinding> findings) =>
        string.Join(Environment.NewLine, findings.Select(finding => "  " + finding));
}
