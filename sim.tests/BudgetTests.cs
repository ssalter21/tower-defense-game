using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace Sim.Tests;

/// <summary>
/// The re-simulation budget: the number that is allowed to go red instead of
/// somebody adding the snapshot cache.
/// </summary>
/// <remarks>
/// <para>
/// Seeking re-simulates from tick zero rather than reading a cache, which makes
/// every scrub of the slider a free determinism check. The only reason anyone
/// would ever add the cache back is re-simulation getting slow -- so this
/// budget going red <i>is</i> that signal, arriving with a number in hand
/// rather than as a feeling that the editor is sluggish.
/// </para>
/// <para>
/// <b>The budget is a multiple of a reference workload, not a number of
/// milliseconds</b>, and the reason is written in this repository's history.
/// Until 6 Aug 2026 it was a hard-coded ten milliseconds, described here as
/// "roughly ten times what the match actually costs". That sentence was false
/// when measured: the match costs 2.75 ms on the machine the number was
/// calibrated on, so ten milliseconds was 3.6x, not 10x -- and 3.6x is not
/// enough margin to cover the difference between a laptop and a shared runner.
/// </para>
/// <para>
/// What that cost, exactly: <c>main</c> was red from 3 to 6 Aug 2026 on a merge
/// that changed three Markdown files and nothing else. <c>sim/</c>,
/// <c>content/</c> and the committed <c>Sim.dll</c> were byte-identical across
/// the red commit and the green one that followed it, so the tick loop provably
/// had not changed; the same test measured 13.51 ms on one <c>ubuntu-latest</c>
/// runner and passed on another. A second flake on 3 Aug measured 10.88 ms.
/// Two failures in forty-four runs, neither of them a regression, and a
/// failure message that said "seeking has become expensive enough for somebody
/// to want the snapshot cache back" every time.
/// </para>
/// <para>
/// So the check now times <see cref="ReferenceWorkload"/> on the same machine
/// in the same run and asserts the match costs less than
/// <see cref="BudgetMultiple"/> times it. A runner that is uniformly slower
/// slows both and the ratio does not move; a tick loop that got slower moves
/// the ratio and nothing else does. The multiple is the margin the old number
/// really had -- 3.6x, deliberately not widened, because the point of this
/// change is to stop measuring the machine, not to loosen the gate.
/// </para>
/// <para>
/// <b>Measured, rather than argued.</b> The same commit was run five times on
/// <c>ubuntu-latest</c> on 6 Aug 2026, changing nothing between attempts:
/// </para>
/// <list type="table">
///   <item><description>match 1.60 ms, reference 2.15 ms, ratio 0.74</description></item>
///   <item><description>match 7.83 ms, reference 7.76 ms, ratio 1.01</description></item>
///   <item><description>match 11.76 ms, reference 8.92 ms, ratio 1.32</description></item>
///   <item><description>match 7.80 ms, reference 8.05 ms, ratio 0.97</description></item>
///   <item><description>match 7.86 ms, reference 7.85 ms, ratio 1.00</description></item>
/// </list>
/// <para>
/// The match time spans <b>7.4x</b> across those five runs, on identical bytes.
/// The ratio spans 1.8x. That gap is the entire case for this design, and the
/// third row is the case made concrete: 11.76 ms is over the old ten-millisecond
/// ceiling, so that run would have been red, reported as the tick loop having
/// got slower, on a commit whose simulation nobody had touched. Two of the
/// other rows sat at 7.8 ms -- 78% of the old budget spent doing nothing wrong.
/// Note also that the fastest runner beat the calibration laptop outright:
/// <c>ubuntu-latest</c> is not reliably slower than a laptop, it is reliably
/// <i>inconsistent</i>, which is the thing a fixed millisecond number cannot be
/// written against.
/// </para>
/// <para>
/// The ratio is much more stable than the clock, not perfectly stable: under
/// load the match slowed somewhat more than the reference did (row three), so
/// the two do not track exactly. 3.6x leaves 2.7x of headroom over the worst
/// ratio yet seen, which is margin for that drift rather than margin for a
/// regression.
/// </para>
/// <para>
/// The measurements are <b>interleaved</b> rather than run in two blocks: one
/// match, then one reference, twelve times. A runner that gets busy halfway
/// through would otherwise land entirely on one of the two and show up as a
/// ratio change, which is the exact failure this design exists to remove.
/// </para>
/// <para>
/// It measures the <b>median</b> of several runs rather than the best of them.
/// A timing test whose number can be spoiled by one unrelated process is a test
/// that gets muted, so one run on its own will not do -- but the fastest of
/// twelve is the wrong correction, because it lets a single lucky run hide a
/// regression the other eleven can all see. The median needs half the runs to
/// be inside the budget, which survives a noisy neighbour and still goes red
/// when the tick loop has genuinely got slower.
/// </para>
/// <para>
/// <b>Both numbers are reported on success as well as on failure.</b> The old
/// test printed its measurement only when it failed, which is why nobody --
/// including the ticket that reported the flake -- could say what the margin
/// actually was on any runner. A budget whose headroom is invisible until the
/// day it runs out is a budget nobody can tune.
/// </para>
/// </remarks>
public class BudgetTests
{
    /// <summary>
    /// How many times the match may cost what the reference workload costs.
    /// See the remarks: this is the margin the ten-millisecond number really
    /// had on the machine it was written on, carried over unchanged.
    /// </summary>
    private const double BudgetMultiple = 3.6;

    private const int Runs = 12;

    private readonly ITestOutputHelper output;

    public BudgetTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void Re_simulating_to_the_end_of_the_match_stays_inside_the_budget()
    {
        // Warm both first. The first run of each pays for the JIT, and the
        // budget is about the twentieth scrub of the slider rather than the
        // first.
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, TheMatch.Fresh().Resolve().FinalTick);
        Assert.Equal(ReferenceWorkload.Checksum, ReferenceWorkload.Churn(ReferenceWorkload.Iterations));

        double[] matchMilliseconds = new double[Runs];
        double[] referenceMilliseconds = new double[Runs];

        for (int run = 0; run < Runs; run++)
        {
            Match match = TheMatch.Fresh();
            var matchWatch = Stopwatch.StartNew();
            MatchResult result = match.Resolve();
            matchWatch.Stop();

            var referenceWatch = Stopwatch.StartNew();
            long checksum = ReferenceWorkload.Churn(ReferenceWorkload.Iterations);
            referenceWatch.Stop();

            Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
            Assert.Equal(ReferenceWorkload.Checksum, checksum);

            matchMilliseconds[run] = matchWatch.Elapsed.TotalMilliseconds;
            referenceMilliseconds[run] = referenceWatch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(matchMilliseconds);
        Array.Sort(referenceMilliseconds);

        // With an even number of runs there are two middles; take the slower of
        // them for the match and the faster for the reference, so every tie is
        // broken against the simulation rather than for it.
        double matchMedian = matchMilliseconds[Runs / 2];
        double referenceMedian = referenceMilliseconds[(Runs / 2) - 1];

        double ratio = matchMedian / referenceMedian;
        double budget = referenceMedian * BudgetMultiple;

        output.WriteLine(
            $"match {matchMedian:0.00} ms, reference {referenceMedian:0.00} ms, ratio {ratio:0.00} "
            + $"of a permitted {BudgetMultiple:0.0} (budget {budget:0.00} ms on this machine)");

        // The same predicate the theories above pin the behaviour of, so what
        // is proven is what runs.
        Assert.True(
            IsInsideBudget(matchMedian, referenceMedian),
            $"Re-simulating the match took {matchMedian:0.00} ms at the median of {Runs} runs, which is "
            + $"{ratio:0.00} times the {referenceMedian:0.00} ms the reference workload took on this same "
            + $"machine in this same run, and the budget is {BudgetMultiple:0.0} times. This is the "
            + "signal that seeking has become expensive enough for somebody to want the snapshot "
            + "cache back. Make the tick loop cheaper rather than raising this number -- and note "
            + "that a slow runner cannot produce this failure, because a slow runner slows the "
            + "reference too.");
    }

    [Fact]
    public void The_reference_workload_is_the_one_the_budget_was_calibrated_against()
    {
        // The budget is a multiple of this workload, so a change to the
        // workload silently rescales the gate. Pinning the checksum makes that
        // a red test with a reason rather than a threshold that quietly means
        // something else. It also catches the optimiser deleting the loop,
        // which would otherwise calibrate the budget against zero.
        Assert.Equal(ReferenceWorkload.Checksum, ReferenceWorkload.Churn(ReferenceWorkload.Iterations));
    }

    [Fact]
    public void The_reference_workload_does_not_touch_the_simulation()
    {
        // The property the whole design rests on, and the one that is easiest
        // to lose by accident: calibrating against Fix64 would slow the
        // reference and the match together and keep the ratio flat through the
        // exact regression this gate exists to catch. Checked in source rather
        // than believed, because the failure is silent -- the test still runs,
        // still passes, and no longer means anything.
        var offences = new List<string>();
        string[] simulationTypes = typeof(Match).Assembly.GetExportedTypes()
            .Select(type => type.Name)
            .Distinct()
            .ToArray();

        foreach ((int number, string code) in CodeLinesOf(ReferenceWorkloadSource))
        {
            foreach (string name in simulationTypes)
            {
                if (Regex.IsMatch(code, $@"\b{Regex.Escape(name)}\b"))
                {
                    offences.Add($"ReferenceWorkload.cs({number}): {name} in {code.Trim()}");
                }
            }
        }

        Assert.True(
            offences.Count == 0,
            "The reference workload references the simulation. A budget calibrated against the code "
            + "it is measuring cannot go red, because a slowdown moves both sides of the ratio."
            + Environment.NewLine
            + string.Join(Environment.NewLine, offences));
    }

    [Fact]
    public void The_independence_check_is_looking_at_the_workload_and_not_at_nothing()
    {
        // A source path that silently stopped resolving would make the test
        // above green forever. Both halves have to hold: the file is there, and
        // the simulation type list it is checked against is not empty.
        Assert.True(File.Exists(ReferenceWorkloadSource));
        Assert.NotEmpty(CodeLinesOf(ReferenceWorkloadSource));
        Assert.Contains(typeof(Match).Assembly.GetExportedTypes(), type => type.Name == "Fix64");
        Assert.Contains(CodeLinesOf(ReferenceWorkloadSource), line => line.Code.Contains("Golden"));
    }

    [Theory]
    [InlineData("// Fix64 in a comment is prose, not a dependency", "")]
    [InlineData("/// <summary>Fix64</summary>", "")]
    [InlineData("long high = value >> 32; // Fix64", "long high = value >> 32;")]
    [InlineData("long accumulator = 1L;", "long accumulator = 1L;")]
    public void The_comment_stripper_keeps_code_and_drops_prose(string line, string expected)
    {
        // The stripper is what stops the check above firing on its own
        // docstring, which names Fix64 four times on purpose.
        Assert.Equal(expected, StripComment(line).Trim());
    }

    private static string ReferenceWorkloadSource =>
        Path.Combine(RepoLayout.Root, "sim.tests", "ReferenceWorkload.cs");

    /// <summary>
    /// The lines of a source file with their comments removed, and the blank
    /// results dropped. Line comments only, which is all this file has and all
    /// the check needs -- a block comment would leave a residue here and is
    /// worth a failing test rather than a silently weaker one.
    /// </summary>
    private static (int Number, string Code)[] CodeLinesOf(string path) =>
        File.ReadAllLines(path)
            .Select((line, index) => (Number: index + 1, Code: StripComment(line)))
            .Where(line => line.Code.Trim().Length > 0)
            .Where(line => !line.Code.TrimStart().StartsWith("namespace", StringComparison.Ordinal))
            .ToArray();

    private static string StripComment(string line)
    {
        int marker = line.IndexOf("//", StringComparison.Ordinal);
        return marker < 0 ? line : line[..marker];
    }

    [Theory]
    // A machine that is uniformly slower: both numbers scale together and the
    // verdict does not move. These are the runs that used to go red -- 13.51 ms
    // and 10.88 ms against a ten-millisecond ceiling, on a tick loop that was
    // byte-identical to the one that passed.
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(4.0)]
    [InlineData(8.0)]
    [InlineData(20.0)]
    public void A_uniformly_slower_machine_does_not_spend_the_budget(double slowdown)
    {
        // Calibration-machine numbers, 6 Aug 2026: 2.75 ms of match against
        // 2.77 ms of reference.
        Assert.True(IsInsideBudget(2.75 * slowdown, 2.77 * slowdown));
    }

    [Theory]
    // A tick loop that genuinely got slower, on machines of every speed. The
    // gate has to fire on all of them, which is the property a hard-coded
    // millisecond ceiling could not have.
    [InlineData(1.0)]
    [InlineData(4.0)]
    [InlineData(20.0)]
    public void A_slower_tick_loop_spends_the_budget_on_any_machine(double slowdown)
    {
        Assert.False(IsInsideBudget(2.75 * 4 * slowdown, 2.77 * slowdown));
    }

    [Fact]
    public void The_budget_is_spent_where_the_multiple_says()
    {
        // Just inside and just outside, so the multiple means what it is
        // written as rather than approximately that.
        //
        // The exact boundary is deliberately not asserted. `BudgetMultiple *
        // reference / reference` is not `BudgetMultiple` in binary floating
        // point -- for 3.6 and 2.77 it comes back 3.5999999999999996 -- so a
        // test of the equality case would be pinning the rounding of one
        // multiply and not the behaviour of the gate. A budget whose verdict
        // hinged on the last bit of a double would be the wrong design anyway.
        Assert.True(IsInsideBudget((BudgetMultiple - 0.01) * 2.77, 2.77));
        Assert.False(IsInsideBudget((BudgetMultiple + 0.01) * 2.77, 2.77));
    }

    private static bool IsInsideBudget(double matchMilliseconds, double referenceMilliseconds) =>
        matchMilliseconds / referenceMilliseconds < BudgetMultiple;

    [Fact]
    public void The_budget_is_measured_against_a_match_that_actually_happened()
    {
        // A budget met by a match that resolves in three ticks is not a budget.
        MatchResult result = TheMatch.Fresh().Resolve();

        Assert.True(result.FinalTick > 60 * Match.TicksPerSecond);
        Assert.True(result.Leaked > 0);
        Assert.True(result.Leaked < result.Total);
    }
}
