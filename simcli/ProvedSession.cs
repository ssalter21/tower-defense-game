using Sim;

namespace Sim.Cli;

/// <summary>
/// A session held up against a fresh run of the script it wrote: the claim that
/// the run somebody played and the record of it are one run, and the refusal to
/// write anything down where they are not.
/// </summary>
/// <remarks>
/// <para>
/// The decisions are compiled into a command script, that script is played into
/// a run built fresh on the same seed and the same shape, and every round it
/// reports and the outcome it folds to are held against what the player was
/// shown. Only a session that agreed writes anything at all, and the write
/// happens here rather than at the verb. Why the step exists and what it costs
/// are ADR-0050.
/// </para>
/// <para>
/// <b>The rounds arrive as data, and the second run arrives as a way to build
/// one.</b> Every round compared is one the session handed back rather than any
/// re-derived in here -- a run compared against itself is a check that cannot
/// fail -- and the fresh run is built in here because
/// <see cref="CommandStream.Recorded"/> needs one nothing has been played into.
/// The outcome is the exception and it is folded off the run the session moved,
/// because <see cref="RunSummary.Outcome"/> is the one place that line is
/// spelled and a second spelling of it here would be the thing to keep current.
/// </para>
/// <para>
/// <b>A script that will not compile or will not replay is reported as a
/// disagreement, not raised at the player.</b> The record refuses a decision it
/// cannot store, the grammar refuses a script it cannot read back, and replaying
/// refuses a decision the run will not take; each of them arriving here says the
/// prompt composed something the record cannot carry, which is this program's
/// fault. The sentence a disagreement prints says so.
/// </para>
/// </remarks>
internal sealed class ProvedSession
{
    /// <summary>What the script is called in anything the record says about it. Never a path.</summary>
    private const string Source = "the script this session played";

    /// <summary>What a disagreement opens with.</summary>
    private const string Disagrees = "The script this session wrote does not play back as the session played.";

    /// <summary>What a script the record or the run turned down opens with.</summary>
    private const string Refused = "The script this session wrote does not play back at all.";

    /// <summary>What either of them closes with.</summary>
    private const string Bug =
        "This is a bug in playing a run at a prompt rather than a decision anybody made badly: the run "
        + "somebody played and the script it compiles to have to be the same run, and proving that "
        + "before anything reaches a disk is the whole of what this step is for.";

    /// <summary>What a session that committed no round is told.</summary>
    private const string NothingPlayed =
        "No round was played, so there is no script to write and nothing for a second run to disagree "
        + "with. Nothing was written to ";

    /// <summary>What stands in front of each side of a disagreement, one to a line.</summary>
    private const string Shown = "    played    ";

    private const string Replayed = "    replayed  ";

    /// <summary>How many rounds the script carries, for the line that says it was written.</summary>
    private readonly int _rounds;

    private ProvedSession(string script, int rounds, string? disagreement)
    {
        Script = script;
        _rounds = rounds;
        Disagreement = disagreement;
    }

    /// <summary>
    /// The session's decisions as a command script, or nothing where it decided
    /// nothing or where the record would not carry what it decided.
    /// </summary>
    public string Script { get; }

    /// <summary>
    /// What the fresh run said that the session did not, spelled for a person,
    /// or nothing where the two agreed.
    /// </summary>
    public string? Disagreement { get; }

    /// <summary>Whether the fresh run played the session's script as the session played it.</summary>
    public bool Agreed => Disagreement is null;

    /// <summary>
    /// Compiles what a session decided, plays it into a fresh run and holds
    /// every round and the outcome against what the player was shown.
    /// </summary>
    /// <param name="session">The decisions and the rounds they came to, as the prompt handed them back.</param>
    /// <param name="played">The run the session moved, as it stands afterwards.</param>
    /// <param name="afresh">
    /// A run on the same seed and the same shape with nothing played into it,
    /// built here rather than handed in because recording a stream refuses a run
    /// that has already resolved a round.
    /// </param>
    public static ProvedSession Of(Played session, Run played, Func<Run> afresh)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(played);
        ArgumentNullException.ThrowIfNull(afresh);

        try
        {
            string script = PlayedScript.Of(session.Decisions);

            // A session that committed no round decided nothing, and this
            // grammar has no row for that: there is no script to play into a
            // second run and nothing two runs could disagree about.
            if (script.Length == 0)
            {
                return new ProvedSession(script, 0, null);
            }

            Run fresh = afresh();
            IReadOnlyList<RoundReport> replayed = CommandStream
                .Recorded(fresh, CommandScript.Parse(Source, script))
                .Rounds;

            return new ProvedSession(
                script,
                session.Decisions.Count,
                Disagreed(session.Rounds, replayed, played, fresh));
        }
        catch (Exception thrown) when (thrown is ContentException or RecordException or SimulationException)
        {
            return new ProvedSession(string.Empty, 0, Refused + "\n\n  " + thrown.Message + "\n\n" + Bug);
        }
    }

    /// <summary>
    /// Writes the script where the fresh run agreed with the session, and says
    /// what did not where it did not.
    /// </summary>
    /// <param name="path">Where an agreeing session's script goes, and the only file this writes.</param>
    /// <param name="writer">Where the line about it goes.</param>
    /// <returns>False where the session was refused, which is a verb's exit code.</returns>
    public bool Written(string path, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(writer);

        if (Disagreement is not null)
        {
            PlainText.Say(writer, Disagreement + "\n\nNothing was written to " + path + ".");

            return false;
        }

        if (Script.Length == 0)
        {
            PlainText.Say(writer, NothingPlayed + path + ".");

            return true;
        }

        PlainText.Written(path, Script);
        PlainText.Say(
            writer,
            "wrote      "
            + path
            + " ("
            + PlainText.Number(_rounds)
            + " rounds, played into a fresh run and matched round for round before writing)");

        return true;
    }

    /// <summary>
    /// What the two runs do not say the same way, or nothing where they say
    /// everything the same way.
    /// </summary>
    /// <remarks>
    /// The rounds are walked before the counts are compared, so the first round
    /// they differ on is what a person is shown even where one run stopped
    /// somewhere the other did not. The outcome is last because it is a fold
    /// over the rounds: a run that disagrees about both is a run that disagreed
    /// about a round first.
    /// </remarks>
    private static string? Disagreed(
        IReadOnlyList<RoundReport> shown,
        IReadOnlyList<RoundReport> replayed,
        Run played,
        Run fresh)
    {
        for (int index = 0; index < Math.Min(shown.Count, replayed.Count); index++)
        {
            string was = shown[index].ToString();
            string again = replayed[index].ToString();

            if (was != again)
            {
                return Says("wave " + PlainText.Number(index + 1), was, again);
            }
        }

        if (shown.Count != replayed.Count)
        {
            return Says("how many rounds", PlainText.Number(shown.Count), PlainText.Number(replayed.Count));
        }

        string ended = RunSummary.Outcome(played);
        string endedAgain = RunSummary.Outcome(fresh);

        return ended == endedAgain ? null : Says("the run", ended, endedAgain);
    }

    /// <summary>One thing the session and the fresh run do not say the same way, with both sides of it.</summary>
    private static string Says(string what, string shown, string replayed) =>
        Disagrees + "\n\n  " + what + ":\n" + Shown + shown + "\n" + Replayed + replayed + "\n\n" + Bug;
}
