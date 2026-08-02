using System.Diagnostics;

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
/// Ten milliseconds is roughly ten times what the match actually costs, which
/// is deliberate on both sides: loose enough that a loaded continuous
/// integration runner does not make it flaky, and still far tighter than
/// anything a human would notice while dragging a slider.
/// </para>
/// <para>
/// It measures the <b>median</b> of several runs rather than the best of them.
/// A timing test whose number can be spoiled by one unrelated process is a test
/// that gets muted, so one run on its own will not do -- but the fastest of
/// twelve is the wrong correction. The ten milliseconds is already ten times the
/// estimate precisely so a loaded machine does not make this flaky; taking the
/// best as well stacks a second margin on top of that one, and lets a single
/// lucky run hide a regression the other eleven can all see. The median needs
/// half the runs to be inside the budget, which survives a noisy neighbour and
/// still goes red when the tick loop has genuinely got slower.
/// </para>
/// </remarks>
public class BudgetTests
{
    private const int BudgetMilliseconds = 10;

    private const int Runs = 12;

    [Fact]
    public void Re_simulating_to_the_end_of_the_match_stays_inside_the_budget()
    {
        // Warm first. The first run pays for the JIT, and the budget is about
        // the twentieth scrub of the slider rather than the first.
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, TheMatch.Fresh().Resolve().FinalTick);

        double[] milliseconds = new double[Runs];

        for (int run = 0; run < Runs; run++)
        {
            Match match = TheMatch.Fresh();
            var watch = Stopwatch.StartNew();
            MatchResult result = match.Resolve();
            watch.Stop();

            Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
            milliseconds[run] = watch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(milliseconds);

        // With an even number of runs there are two middles; take the slower of
        // them, so the tie is broken against the simulation rather than for it.
        double median = milliseconds[Runs / 2];

        Assert.True(
            median < BudgetMilliseconds,
            $"Re-simulating the match took {median:0.00} ms at the median of {Runs} runs, and the budget "
            + $"is {BudgetMilliseconds} ms. This is the signal that seeking has become expensive enough "
            + "for somebody to want the snapshot cache back. Make the tick loop cheaper rather than "
            + "raising this number.");
    }

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
