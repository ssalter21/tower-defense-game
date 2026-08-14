using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>What the screen is doing: composing a round, watching one, or finished.</summary>
    public enum RunMode
    {
        /// <summary>A round is being composed. No match is drawn.</summary>
        Building = 0,

        /// <summary>The round that was just committed is being watched.</summary>
        Watching = 1,

        /// <summary>The run has stopped and its last frame is up.</summary>
        Over = 2,
    }

    /// <summary>
    /// The run loop: build, commit, watch, and round again, ten times, then an
    /// end frame — and the session written down on the way out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One scene and two modes.</b> Nothing here loads a scene. Committing
    /// takes the build chrome down and puts a match up; going on takes the
    /// match down and puts a fresh round's chrome up. The header
    /// (<see cref="RunHeader"/>) is built once and outlives both, which is why
    /// it does not move when the mode does.
    /// </para>
    /// <para>
    /// <b>Committing is the only thing on this screen that reaches the
    /// run.</b> Everything the player clicks composes a <see cref="BuildPhase"/>
    /// in a local — that is <see cref="ComposedRound"/>, ADR-0051 — and the
    /// phase reaches the simulation through <see cref="Run.Advance"/> and by no
    /// other route. What is watched afterwards is asked for with
    /// <see cref="Run.MatchAt"/> rather than kept from the resolving, so the run
    /// never carries a match it might not draw.
    /// </para>
    /// <para>
    /// <b>The commit is not a gate.</b> By the time the button is pressed the
    /// phase already resolves, because a phase that did not was never
    /// composable. So the refusal from <see cref="Run.Advance"/> is not caught:
    /// one arriving here means an affordance offered something the rules turn
    /// down, which is a defect in the view and wants a stack trace. ADR-0051
    /// makes that assertion the reason prevention is safe to rely on.
    /// </para>
    /// <para>
    /// <b>The field is a number and the player watches one member of it.</b> A
    /// round resolves against K opponents; the rest of them are the band that
    /// feeds the purse, and drawing one of them is what a round looks like. See
    /// <see cref="WatchedOpponent"/>.
    /// </para>
    /// <para>
    /// <b>Two screens, and the second one has two views.</b> Building is a
    /// single joint screen: the towers and the wave are composed together and
    /// committed together. Watching is the Offence and Defence Results Screen,
    /// and the pairing it draws is resolved in both directions - so the same
    /// round can be watched as your towers against their wave or as your wave
    /// against their defence, and neither is a fresh simulation. It opens on the
    /// defence, which is the loop the game is about: you build towers and you
    /// watch those towers work. <see cref="ResultsSwitch"/> is the control, and
    /// <see cref="Watch"/> is what it presses.
    /// </para>
    /// <para>
    /// <b>The session is proved before it is kept.</b> At the end the decisions
    /// are compiled into a command script, played into a run built fresh on the
    /// same seed and the same shape, and held round for round against what the
    /// player was shown — <see cref="ProvedSession"/>. A session that disagrees
    /// hands back no script, so nothing is written. A playtest is a determinism
    /// test.
    /// </para>
    /// <para>
    /// <b>A run does not survive quitting.</b> There is no save and no resume:
    /// the only thing that outlives the session is the script, and that is a
    /// record of what was played rather than a place to carry on from.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RunLoop : MonoBehaviour
    {
        /// <summary>
        /// Which member of the field is drawn.
        /// </summary>
        /// <remarks>
        /// The first, always, because a round has to be watched against
        /// somebody and every choice among K identically-derived pairings is
        /// arbitrary. It is not <c>fieldSize: 1</c>: the round still resolves
        /// against the whole field, and the other K-1 pairings are what the
        /// performance band — and therefore the purse — is measured from.
        /// </remarks>
        public const int WatchedOpponent = 0;

        /// <summary>What the button says while a round is being composed.</summary>
        public const string CommitLabel = "Done";

        /// <summary>What it says while a round is being watched.</summary>
        public const string GoOnLabel = "Next wave";

        /// <summary>
        /// Which direction a committed round opens on: the defence, always.
        /// </summary>
        /// <remarks>
        /// The core viewing loop, and the direction the header's health is spent
        /// on - so the number on the bar and the picture under it are the same
        /// match. #206: it was the other one, and a player watched their own
        /// wave walk into a stranger's towers with none of theirs on the board.
        /// </remarks>
        public const bool OpensAttacking = false;

        private readonly List<BuildPhase> _decisions = new List<BuildPhase>();

        private readonly List<RoundReport> _rounds = new List<RoundReport>();

        private MatchRoot _root;

        private MatchArt _art;

        private Func<Run> _afresh;

        private string _directory;

        /// <summary>The run every round of the session is played into.</summary>
        public Run Run { get; private set; }

        /// <summary>What the screen is doing.</summary>
        public RunMode Mode { get; private set; }

        /// <summary>The header. Built once, up in every mode.</summary>
        public RunHeader Header { get; private set; }

        /// <summary>
        /// The results screen's two views and the control between them. Built
        /// once, drawn only while a round is being watched.
        /// </summary>
        public ResultsSwitch Switch { get; private set; }

        /// <summary>
        /// Whether the round on screen is the offence - this round's wave
        /// against an opponent's defence - rather than the defence.
        /// </summary>
        /// <remarks>
        /// False every time a round is committed and every time one is left, so
        /// a run is a sequence of defences unless somebody asks otherwise. It
        /// means nothing outside <see cref="RunMode.Watching"/>: there is no
        /// match on screen for it to be a direction of.
        /// </remarks>
        public bool WatchingAttack { get; private set; }

        /// <summary>
        /// Which wave is on screen: the one being composed, or the one being
        /// watched. They are the same number either side of a commit, which is
        /// what lets the header carry one field for both.
        /// </summary>
        public int Wave => Mode == RunMode.Building ? Run.Round + 1 : Run.Round;

        /// <summary>
        /// What there is to spend: what the composed round would leave while one
        /// is being composed, and what the run holds otherwise.
        /// </summary>
        /// <remarks>
        /// The composed figure and not the run's, because the purse is the only
        /// limit on a wave and a player composing one needs the number to move
        /// as they spend it. Neither is a forecast: both are settled arithmetic
        /// over what has already been decided.
        /// </remarks>
        public int Gold =>
            Mode == RunMode.Building && _root.Composing != null
                ? _root.Composing.Gold
                : Run.Purse.Gold;

        /// <summary>What the button says, or nothing where there is no button.</summary>
        public string ActionLabel
        {
            get
            {
                switch (Mode)
                {
                    case RunMode.Building: return CommitLabel;
                    case RunMode.Watching: return GoOnLabel;
                    default: return string.Empty;
                }
            }
        }

        /// <summary>Every phase the session committed, in wave order.</summary>
        public IReadOnlyList<BuildPhase> Decisions => _decisions;

        /// <summary>What each of those rounds came to, as the player was shown it.</summary>
        public IReadOnlyList<RoundReport> Rounds => _rounds;

        /// <summary>
        /// The session held against a fresh run of the script it wrote, once the
        /// run is over. Null before that.
        /// </summary>
        public ProvedSession Proved { get; private set; }

        /// <summary>
        /// Where the script was written, or null where nothing was written —
        /// which is every case in which the session was not proved.
        /// </summary>
        public string ScriptPath { get; private set; }

        /// <summary>
        /// The run's last frame, in words, or nothing while it is still going.
        /// </summary>
        /// <remarks>
        /// <see cref="RunOutcome.ToString"/> and not
        /// <see cref="RunSummary.Outcome"/>: the summary line is prefixed and
        /// suffixed for a terminal and names the ending by its enum member,
        /// which is the record's vocabulary rather than a person's. What is left
        /// is the fold itself, which is already a sentence.
        /// </remarks>
        public string EndingText { get; private set; } = string.Empty;

        /// <summary>
        /// Stands the loop up on <paramref name="root"/> and opens the first
        /// round's build phase.
        /// </summary>
        /// <param name="root">The scene root, which draws whatever the mode asks for.</param>
        /// <param name="art">What a tower and a creep are drawn with.</param>
        /// <param name="afresh">
        /// A run on the same seed and the same shape with nothing played into
        /// it. Called once here for the run the session plays and once more at
        /// the end for the run its script is proved against — recording a stream
        /// refuses a run that has already resolved a round, so the second one
        /// cannot be the first.
        /// </param>
        /// <param name="directory">Where an agreeing session's script is written.</param>
        public static RunLoop Build(MatchRoot root, MatchArt art, Func<Run> afresh, string directory)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (art == null) throw new ArgumentNullException(nameof(art));
            if (afresh == null) throw new ArgumentNullException(nameof(afresh));
            if (directory == null) throw new ArgumentNullException(nameof(directory));

            var host = new GameObject("RunLoop");
            host.transform.SetParent(root.transform, worldPositionStays: false);

            var loop = host.AddComponent<RunLoop>();

            loop._root = root;
            loop._art = art;
            loop._afresh = afresh;
            loop._directory = directory;
            loop.Run = afresh();
            loop.Header = RunHeader.Build(root.transform, loop);
            loop.Switch = ResultsSwitch.Build(root.transform, loop);

            loop.OpenBuildPhase();

            // Last, as it is everywhere else here: the mode names what is on
            // screen, so it is written once whatever it names is actually up.
            loop.Mode = RunMode.Building;

            loop.Header.Follow();
            loop.Switch.Follow();

            return loop;
        }

        /// <summary>
        /// The one button, whatever mode it is in. Composing commits; watching
        /// goes on; a finished run does nothing, and the button is not drawn.
        /// </summary>
        public void Press()
        {
            switch (Mode)
            {
                case RunMode.Building:
                    Commit();

                    break;

                case RunMode.Watching:
                    GoOn();

                    break;
            }
        }

        /// <summary>
        /// Commits the composed round — the towers and the wave together — and
        /// puts the match it resolved to on screen.
        /// </summary>
        /// <remarks>
        /// The order is the whole of the mode switch: the run moves first, so a
        /// refusal leaves the build chrome exactly as it was; then the build
        /// chrome comes down, which is what stops a composed tower being drawn
        /// on the same hex as a watched one; then the match goes up.
        /// </remarks>
        public void Commit()
        {
            if (Mode != RunMode.Building)
            {
                return;
            }

            if (_root.Composing == null)
            {
                throw new InvalidOperationException(
                    "The loop is in build mode with no round being composed, so there is nothing to "
                    + "commit. Something took the build chrome down without changing the mode.");
            }

            BuildPhase phase = _root.Composing.Phase;
            RoundReport report = Run.Advance(phase);

            _decisions.Add(phase);
            _rounds.Add(report);

            _root.EndBuilding();

            // Written before the match goes up, because it is what says which of
            // the round's two matches that is. A round always opens on the
            // defence.
            WatchingAttack = OpensAttacking;

            _root.BeginWatching(
                Run.MatchAt(Run.Round - 1, WatchedOpponent, WatchingAttack),
                Run.Types,
                _art);

            // Last, so that anything above throwing leaves the loop saying what
            // is actually on screen rather than naming a mode nothing drew.
            Mode = RunMode.Watching;

            Header.Follow();
            Switch.Follow();
        }

        /// <summary>
        /// Draws the other direction of the round already on screen: the wave
        /// this round sent, or the towers it built.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A view control and not a mode.</b> Both matches were resolved when
        /// the round was committed - a wave is scored against every opponent and
        /// the defence is scored against the same ones - so this asks
        /// <see cref="Run.MatchAt"/> for a copy of a fight that is over. The run
        /// does not move, the purse does not move, the health does not move, and
        /// there is no phase being composed to disturb. It is the same
        /// re-simulation a scrub already does, over the other pairing of one
        /// round.
        /// </para>
        /// <para>
        /// <b>The match comes down before the next one goes up</b>, for the
        /// reason a root refuses a second one: the scrub bar is wired to the
        /// view it was built for, so one left standing over a replaced view is a
        /// bar that moves and changes nothing on screen.
        /// </para>
        /// </remarks>
        /// <param name="attacking">
        /// True for the offence - this round's wave against an opponent's
        /// defence. False for the defence, which is what a round opens on.
        /// </param>
        public void Watch(bool attacking)
        {
            if (Mode != RunMode.Watching || attacking == WatchingAttack)
            {
                return;
            }

            WatchingAttack = attacking;

            _root.EndMatch();
            _root.BeginWatching(
                Run.MatchAt(Run.Round - 1, WatchedOpponent, attacking),
                Run.Types,
                _art);

            Switch.Follow();
        }

        /// <summary>
        /// Leaves the watched match: on to the next round's build phase, or to
        /// the end frame where the run has stopped.
        /// </summary>
        public void GoOn()
        {
            if (Mode != RunMode.Watching)
            {
                return;
            }

            _root.EndMatch();

            // The next round opens on its defence whatever this one was left
            // showing, so switching is a decision about one round rather than a
            // setting that follows the run.
            WatchingAttack = OpensAttacking;

            if (Run.IsOver)
            {
                Finish();
            }
            else
            {
                OpenBuildPhase();

                Mode = RunMode.Building;
            }

            Header.Follow();
            Switch.Follow();
        }

        private void OpenBuildPhase() =>
            _root.BeginBuilding(ComposedRound.For(Run), _art, Header == null ? null : Header.Document);

        /// <summary>
        /// The end of the run: what it came to, and the session proved and
        /// written.
        /// </summary>
        /// <remarks>
        /// The write is the client's and the proving is the simulation's:
        /// <c>System.IO</c> is a banned namespace in <c>sim</c>, so a session
        /// that agreed hands back a script and where it lands is whoever is
        /// holding it. A disagreement hands back no script at all, so there is
        /// nothing here that could write one by ignoring the sentence.
        /// </remarks>
        private void Finish()
        {
            Mode = RunMode.Over;
            Proved = ProvedSession.Of(_decisions, _rounds, Run, _afresh);
            ScriptPath = WrittenRun.Written(Proved, _directory);
            EndingText = Run.Outcome.ToString() + "\n\n" + WrittenRun.Wording(Proved, ScriptPath);
        }
    }
}
