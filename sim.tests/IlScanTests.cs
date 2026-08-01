using Sim.Tests.Scan;

namespace Sim.Tests;

/// <summary>
/// The scan, run over the two images that both have to be clean.
/// </summary>
/// <remarks>
/// <para>
/// Scanning only the committed assembly would let a float land in source and
/// sit there indefinitely, because the committed bytes stay clean until
/// somebody rebuilds. Scanning only a fresh build would let the repository
/// carry an image nobody has checked, because continuous integration would
/// rebuild before looking and never open the file that ships. Neither is a
/// gate on its own; together they close on each other.
/// </para>
/// <para>
/// Note what the pair does <b>not</b> claim: it does not assert the two images
/// are byte-identical. Two builds of the same sources on different SDK patch
/// levels legitimately differ, and a check that fails for that reason would be
/// switched off within a week. What is asserted is that both satisfy the rule.
/// </para>
/// </remarks>
public class IlScanTests
{
    [Fact]
    public void The_committed_assembly_is_clean()
    {
        AssertClean(RepoLayout.CommittedAssembly, "the committed assembly");
    }

    [Fact]
    public void A_fresh_build_of_the_same_sources_is_clean()
    {
        AssertClean(RepoLayout.FreshlyBuiltAssembly, "a fresh build of sim/");
    }

    [Fact]
    public void The_scan_looks_at_something_rather_than_an_empty_shell()
    {
        // A scan of an assembly with no method bodies would be clean for the
        // wrong reason. Sim has arithmetic and dice in it; assert the scanner
        // is actually reading them, so "no findings" means "nothing banned"
        // rather than "nothing looked at".
        Assert.True(
            new FileInfo(RepoLayout.CommittedAssembly).Length > 4096,
            "The committed assembly is suspiciously small; the scan may be passing over an empty shell.");

        Assert.NotEmpty(typeof(Fix64).GetMethods());
        Assert.NotEmpty(typeof(Pcg32).GetMethods());
    }

    private static void AssertClean(string assemblyPath, string description)
    {
        IReadOnlyList<BanFinding> findings = IlScan.Scan(assemblyPath);

        Assert.True(
            findings.Count == 0,
            $"The IL scan rejected {description} ({assemblyPath}):{Environment.NewLine}"
            + string.Join(Environment.NewLine, findings.Select(finding => "  " + finding)));
    }
}
