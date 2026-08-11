using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>How a run being played at a prompt stopped.</summary>
internal enum Ended
{
    /// <summary>
    /// The run ended on its own terms: its last wave resolved, or health
    /// reached zero in a run death ends. <see cref="Run.Ending"/> says which.
    /// </summary>
    Over = 0,

    /// <summary><c>quit</c>: the run was left early, and the round it was in was not played.</summary>
    Quit,

    /// <summary>
    /// The reader ran out of lines, which is what a transcript that stops
    /// before the run does.
    /// </summary>
    OutOfLines,
}

/// <summary>What a session at the prompt played: the decisions, the rounds they came to, and how it stopped.</summary>
/// <remarks>
/// The decisions are handed back because they are what a command script is
/// written from: a run played at a prompt and not written down is an experiment
/// nobody can repeat. The rounds come back beside them rather than being
/// re-derived, for the reason <see cref="RoundReport"/> exists -- a round
/// settles its pair, its cost and its payment once, while it is being played.
/// </remarks>
internal sealed class Played
{
    public Played(
        IReadOnlyList<BuildPhase> decisions,
        IReadOnlyList<RoundReport> rounds,
        Ended ending)
    {
        Decisions = decisions;
        Rounds = rounds;
        Ending = ending;
    }

    /// <summary>The phase each played round was played from, in wave order.</summary>
    public IReadOnlyList<BuildPhase> Decisions { get; }

    /// <summary>What each of those rounds came to. One per decision, in the same order.</summary>
    public IReadOnlyList<RoundReport> Rounds { get; }

    /// <summary>How the session stopped.</summary>
    public Ended Ending { get; }
}

/// <summary>
/// A whole run played round by round at a prompt: the lifecycle around
/// <see cref="BuildPrompt.Compose"/>'s one round.
/// </summary>
/// <remarks>
/// <para>
/// <b>Composing is not this, and committing is not composing.</b> A round is
/// composed with nothing moved -- see <see cref="BuildPrompt"/> -- and this is
/// the only place <see cref="Run.Advance"/> is called on what came back. The two
/// being separate is what lets a round be priced a word at a time without the
/// run ever holding half of one.
/// </para>
/// <para>
/// <b>The round line is <see cref="RoundReport.ToString"/> and nothing
/// else.</b> The words a player is shown when a round resolves are the words
/// <c>content/run-outcome.txt</c> carries for that round, so what somebody read
/// at the prompt can be found in the committed file by searching for it. A
/// second arrangement of the same numbers would be a second thing to keep
/// current.
/// </para>
/// <para>
/// <b>Three things end the loop, and only one of them is a word.</b> The run
/// ending is <see cref="Run.IsOver"/>, which is a fold over the outcome and
/// already knows whether this run's shape lets death end it -- so a run that
/// dies with death switched off carries on playing here for the same reason it
/// does everywhere else, without this loop knowing the rule. The other two are
/// <c>quit</c> and a reader with no lines left.
/// </para>
/// <para>
/// <b>A wave nobody can afford is refused at the word that made it
/// unaffordable.</b> Composing prices the whole decision after every word and
/// keeps only what resolved, against exactly the offering, unlocks, purse,
/// costs, roster, map and board <see cref="BuildPhase.Resolve"/> is given here
/// -- so the reprint after every <c>send</c> is what stops a phase whose towers
/// ate its wave arriving as a surprise at commit with a round's typing behind
/// it.
/// </para>
/// <para>
/// <b>A commit that is refused anyway is caught, printed and carried on
/// from.</b> <see cref="Run.Advance"/> is a wider surface than the resolving
/// composing did: past the decision it plays the round's matches, measures the
/// field on first use and folds the outcome, and each of those refuses in its
/// own right -- a leak priced beyond what a purse can hold is refused there and
/// nowhere composing can see it. So the refusal is printed like any other and
/// the round is composed again <i>from the phase it already had</i>, which is
/// the only way <c>docs/playing-a-run-from-a-shell.md</c> §3's "a refusal is
/// caught and reprinted, never thrown out of the loop" can hold at the one
/// point a round is committed.
/// </para>
/// <para>
/// <b>The end prints the run's own outcome and the run's own board.</b> Both
/// come from where the committed outcome file gets them -- see
/// <see cref="RunSummary.Outcome"/> and <see cref="Board.ToReportText"/> --
/// rather than from a summary written for a terminal.
/// </para>
/// </remarks>
internal static class RunPrompt
{
    /// <summary>What is said in front of the wave a session stopped short of.</summary>
    private const string QuitAt = "Quit at wave ";

    private const string RanOutAt = "The lines ran out at wave ";

    /// <summary>
    /// What follows either, said once: the round that was open when the session
    /// stopped is not a round the run played.
    /// </summary>
    private const string NotPlayed =
        ", which is a round nobody played. What follows is the run as the last committed round left it.";

    /// <summary>
    /// Plays a run from where it stands to wherever it stops, taking each round's
    /// decision from the reader and printing the frames, the refusals, the round
    /// lines and the ending to the writer.
    /// </summary>
    /// <param name="run">The run to play. Advanced once per committed round.</param>
    /// <param name="ladder">Which unit follows which, which is how the map cases its letters.</param>
    /// <param name="reader">Where the words come from.</param>
    /// <param name="writer">Where everything a session prints goes.</param>
    public static Played Play(Run run, UpgradeLadder ladder, TextReader reader, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(ladder);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(writer);

        var decisions = new List<BuildPhase>();
        var rounds = new List<RoundReport>();
        Ended ending = Ended.Over;

        // What the round about to be composed is already holding: nothing, or
        // the decision a refused commit handed back.
        BuildPhase? holding = null;

        while (!run.IsOver)
        {
            Composed composed = BuildPrompt.Compose(run, ladder, reader, writer, holding);

            if (composed.Stopped != Stopped.Done)
            {
                ending = Left(composed.Stopped);
                break;
            }

            // Composing refuses `done` before a take, so a round that came back
            // done came back with the phase that take is composed around.
            BuildPhase phase = composed.Phase!;
            RoundReport round;

            try
            {
                round = run.Advance(phase);
            }
            catch (SimulationException refused)
            {
                PlainText.Say(writer, refused.Message);
                holding = phase;
                continue;
            }

            holding = null;
            decisions.Add(phase);
            rounds.Add(round);

            PlainText.Say(writer, round.ToString());
        }

        PlainText.Say(writer, Closing(run, ending));

        return new Played(decisions, rounds, ending);
    }

    /// <summary>How a round that did not finish with <c>done</c> ended the run.</summary>
    /// <remarks>
    /// A switch rather than a test against <c>quit</c>, on the arrangement
    /// <see cref="BuildPrompt.Compose"/> uses for the same reason: a word added
    /// to <see cref="Stopped"/> without a case here would otherwise be read as
    /// a transcript that ran out, which is an ending nobody chose said in a
    /// sentence that is not true.
    /// </remarks>
    private static Ended Left(Stopped stopped) => stopped switch
    {
        Stopped.Quit => Ended.Quit,
        Stopped.OutOfLines => Ended.OutOfLines,
        _ => throw new InvalidOperationException(
            "A round came back stopped by "
            + stopped
            + ", which this loop has no ending for. Every member of Stopped other than Done ends a "
            + "session, so one added to that list without a case here is a run that would stop for a "
            + "reason its own report does not carry."),
    };

    /// <summary>
    /// What a run says when it stops: why it stopped where it did if it stopped
    /// short, then its outcome and the board it ended on.
    /// </summary>
    /// <remarks>
    /// A run that reached its own end says nothing extra, because
    /// <see cref="RunSummary.Outcome"/> already names which of the two ways it
    /// was. Stopping short is the case the outcome cannot say on its own: the
    /// fold reads <c>Unfinished</c> either way, and which wave it was abandoned
    /// on is a fact about the session rather than about the run.
    /// </remarks>
    private static string Closing(Run run, Ended ending)
    {
        var text = new StringBuilder();

        if (ending != Ended.Over)
        {
            text.Append(ending == Ended.Quit ? QuitAt : RanOutAt)
                .Append(Number(run.Round + 1))
                .Append(" of ")
                .Append(Number(run.Waves))
                .Append(NotPlayed)
                .Append("\n\n");
        }

        return text.Append(RunSummary.Outcome(run))
            .Append("\n\n")
            .Append(run.Board.ToReportText())
            .ToString();
    }

    private static string Number(int value) => value.ToString(PlainText.Culture);
}
