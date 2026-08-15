using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A session held up against a fresh run of the script it wrote: the claim that
    /// the run somebody played and the record of it are one run, and the refusal to
    /// keep anything where they are not.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The decisions are compiled into a command script, that script is played into
    /// a run built fresh on the same seed and the same shape, and every round it
    /// reports and the outcome it folds to are held against what the player was
    /// shown. Why the step exists and what it costs are ADR-0050.
    /// </para>
    /// <para>
    /// <b>The proving is here and the writing is the caller's.</b> A session that
    /// agreed comes back holding its script; what a shell does with it is open a
    /// file, and what a client does with it is whatever the engine's own storage
    /// is. <c>System.IO</c> is a banned namespace in this assembly, so the split
    /// is enforced by the IL scan rather than remembered -- and the half that
    /// matters, the claim itself, is reachable from anywhere a run is played
    /// rather than from a shell alone.
    /// </para>
    /// <para>
    /// <b>A session that did not agree hands back no script.</b> Not the script
    /// beside the sentence saying not to keep it -- nothing at all, so a caller
    /// that ignores <see cref="Agreed"/> has nothing to write down. The decision
    /// to keep a session stays this type's rather than becoming a convention in
    /// whichever caller happens to hold one.
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
    /// prompt composed something the record cannot carry, which is the caller's
    /// fault. The sentence a disagreement prints says so.
    /// </para>
    /// </remarks>
    public sealed class ProvedSession
    {
        /// <summary>What the script is called in anything the record says about it. Never a path.</summary>
        private const string Source = "the script this session played";

        /// <summary>What a disagreement opens with.</summary>
        private const string Disagrees = "The script this session wrote does not play back as the session played.";

        /// <summary>What a script the record or the run turned down opens with.</summary>
        private const string Refused = "The script this session wrote does not play back at all.";

        /// <summary>What either of them closes with.</summary>
        private const string Bug =
            "This is a bug in playing a run a round at a time rather than a decision anybody made badly: the "
            + "run somebody played and the script it compiles to have to be the same run, and proving that "
            + "before anything is kept is the whole of what this step is for.";

        /// <summary>What stands in front of each side of a disagreement, one to a line.</summary>
        private const string Shown = "    played    ";

        private const string Replayed = "    replayed  ";

        private ProvedSession(string script, int rounds, string? disagreement)
        {
            Script = script;
            RoundsProved = rounds;
            Disagreement = disagreement;
        }

        /// <summary>
        /// The session's decisions as a command script, and nothing at all where
        /// it decided nothing, where the record would not carry what it decided,
        /// or where the fresh run did not play it back as the session played it.
        /// </summary>
        public string Script { get; }

        /// <summary>
        /// How many rounds that script carries, for whatever says it was kept.
        /// Named apart from the round reports a session hands in, which are a
        /// list under the same word everywhere else.
        /// </summary>
        public int RoundsProved { get; }

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
        /// <param name="decisions">The phase each played round was played from, in wave order.</param>
        /// <param name="shown">What each of those rounds came to, as the player was told it.</param>
        /// <param name="played">The run the session moved, as it stands afterwards.</param>
        /// <param name="afresh">
        /// A run on the same seed and the same shape with nothing played into it,
        /// built here rather than handed in because recording a stream refuses a run
        /// that has already resolved a round.
        /// </param>
        public static ProvedSession Of(
            IReadOnlyList<BuildPhase> decisions,
            IReadOnlyList<RoundReport> shown,
            Run played,
            Func<Run> afresh)
        {
            if (decisions is null)
            {
                throw new ArgumentNullException(nameof(decisions));
            }

            if (shown is null)
            {
                throw new ArgumentNullException(nameof(shown));
            }

            if (played is null)
            {
                throw new ArgumentNullException(nameof(played));
            }

            if (afresh is null)
            {
                throw new ArgumentNullException(nameof(afresh));
            }

            try
            {
                string script = PlayedScript.Of(decisions);

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

                string? disagreement = Disagreed(shown, replayed, played, fresh);

                // A script only leaves here where the fresh run played it back
                // as the session played it. That was the write's guarantee while
                // the write was this type's only exit; with the file gone to the
                // caller it has to be the script's own, or a caller that ignored
                // the sentence would have a legible script to keep.
                return disagreement is null
                    ? new ProvedSession(script, decisions.Count, null)
                    : new ProvedSession(string.Empty, 0, disagreement);
            }
            catch (Exception thrown) when (thrown is ContentException || thrown is RecordException
                || thrown is SimulationException)
            {
                return new ProvedSession(string.Empty, 0, Refused + "\n\n  " + thrown.Message + "\n\n" + Bug);
            }
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
            int paired = shown.Count < replayed.Count ? shown.Count : replayed.Count;

            for (int index = 0; index < paired; index++)
            {
                string was = shown[index].ToString();
                string again = replayed[index].ToString();

                if (was != again)
                {
                    return Says("wave " + Number(index + 1), was, again);
                }
            }

            if (shown.Count != replayed.Count)
            {
                return Says("how many rounds", Number(shown.Count), Number(replayed.Count));
            }

            string ended = RunSummary.Outcome(played);
            string endedAgain = RunSummary.Outcome(fresh);

            return ended == endedAgain ? null : Says("the run", ended, endedAgain);
        }

        /// <summary>One thing the session and the fresh run do not say the same way, with both sides of it.</summary>
        private static string Says(string what, string shown, string replayed) =>
            Disagrees + "\n\n  " + what + ":\n" + Shown + shown + "\n" + Replayed + replayed + "\n\n" + Bug;

        /// <summary>One integer, under the one culture this assembly formats with.</summary>
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
