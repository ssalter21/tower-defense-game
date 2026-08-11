using Sim;

namespace Sim.Cli;

/// <summary>
/// The two lines that name a run to a person: the shape it was played at, and
/// what it came to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Both are folds over a run and nothing else</b> -- no record, no paths, no
/// arguments -- so they read the same off a run replayed from a committed file
/// and off one somebody played a round at a time at a prompt. They sit here
/// rather than on either of those, because a line printed in two places has to
/// have one spelling: a terminal and a committed outcome file that arranged the
/// same folds separately would drift, and the whole value of showing a player
/// their outcome is that it is the outcome the committed file would say.
/// </para>
/// <para>
/// <b>The shape is here for the opposite reason to the outcome.</b> What a run
/// came to is intrinsic to it; N, K and the death flag are arguments no record
/// stamps, so the same decisions played against a wider field are a legal run
/// and a different set of numbers. Printing the shape is what puts that where a
/// diff can see it.
/// </para>
/// </remarks>
internal static class RunSummary
{
    /// <summary>The shape the run was played at: N, K, and whether death ends it.</summary>
    public static string Shape(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return "shape      "
            + run.Waves.ToString(PlainText.Culture)
            + " waves, a field of "
            + run.FieldSize.ToString(PlainText.Culture)
            + (run.DeathEndsTheRun ? ", death ends the run" : ", death does not end the run");
    }

    /// <summary>What a person reads: the folds, and how the run stopped.</summary>
    public static string Outcome(Run run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return "outcome    " + run.Outcome.ToString() + ", ended " + run.Outcome.Ending.ToString();
    }
}
