using Sim;

namespace Sim.Cli;

/// <summary>The line that says what a run came to.</summary>
/// <remarks>
/// <para>
/// <b>A fold over a run and nothing else</b> -- no record, no paths, no
/// arguments -- so it reads the same off a run replayed from a committed file
/// and off one somebody played a round at a time at a prompt. It sits here
/// rather than on either, because a line printed in two places has to have one
/// spelling: a terminal and <c>content/run-outcome.txt</c> that arranged the
/// same folds separately would drift, and the whole value of showing a player
/// their outcome is that it is the outcome the committed file would say.
/// </para>
/// <para>
/// <b>The shape line is deliberately not here.</b> N, K and the death flag are
/// arguments no record stamps, and what prints them is the committed outcome
/// file alone -- a prompt shows the wave count on every frame it draws. It
/// stays on <see cref="PlayedRun"/>, where its one caller is.
/// </para>
/// </remarks>
internal static class RunSummary
{
    /// <summary>What a person reads: the folds, and how the run stopped.</summary>
    public static string Outcome(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return "outcome    " + run.Outcome.ToString() + ", ended " + run.Outcome.Ending.ToString();
    }
}
