using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Sim;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The run loop: build, commit, watch, and round again, then an end frame
    /// and a script on disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The whole loop is driven from a transcript, headless.</b> One line a
    /// round — what to build and how many creeps to send — pressed
    /// through the same screen a person presses: the palette selects, a click on
    /// a hex places, a box's list fills the wave, and the one button commits and
    /// then goes on. Nothing here reaches into <see cref="Run"/>.
    /// </para>
    /// <para>
    /// <b>The claim at the end is the one the whole architecture rests on.</b>
    /// The session compiles its decisions into a command script, that script is
    /// played into a run built fresh on the same seed and the same shape, and
    /// every round and the folded outcome are held against what the player was
    /// shown — <see cref="ProvedSession"/>. Then this fixture does to the file
    /// what <c>simcli record-run</c> and <c>simcli play-run</c> do to it:
    /// compiles it to a command file and plays those bytes into another fresh
    /// run, and holds the outcome line against the one the player saw. A
    /// playtest is a determinism test.
    /// </para>
    /// <para>
    /// <b>Frames are deliberately not waited on.</b> The loop is driven
    /// synchronously, so the watched match never advances a tick — what is being
    /// tested is the loop and not the clock, and a test that watched every match
    /// at one tick a frame would take an hour. <see cref="PlaybackTests"/> is
    /// where the clock is tested.
    /// </para>
    /// </remarks>
    public class RunLoopTests : ViewTest
    {
        /// <summary>
        /// One round to a line: what the round builds, on which cell, and
        /// how many creeps it sends. <c>-</c> builds nothing; a caret upgrades
        /// what is standing rather than placing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The labels are <c>content/units.txt</c>'s own, because that is what a
        /// transcript is: what was decided, in the vocabulary the decision is
        /// stored in. Which creeps go in the wave is not named — the cheapest
        /// the box offers goes in, so the transcript stays legible as the roster
        /// moves and every send is still an assertion that the box offered
        /// something.
        /// </para>
        /// <para>
        /// <b>It is the committed command file's defense, cell for cell.</b>
        /// <c>content/commands.txt</c> is the best arrangement in this
        /// repository at this ruleset, and a transcript that built somewhere
        /// else would be testing the loop against a run that dies sooner — which
        /// is the loop working and the fixture proving nothing. It is a
        /// different seed, so the numbers are not that file's; the shape of the
        /// defense is.
        /// </para>
        /// <para>
        /// <b>It is four rounds because four is what the run survives.</b> The
        /// opponent in the field buys its column again every round (#208) and a
        /// wall stops a roughly fixed number of bodies, so the fourth round
        /// spends the last of the health pool. A fifth line would put the loop
        /// in <see cref="RunMode.Over"/> with a decision still to make, which is
        /// the fixture asserting against a run nobody could play rather than a
        /// longer test.
        /// </para>
        /// </remarks>
        private const string Transcript =
            "archer 6 2 0\n"
            + "archer 7 4 0\n"
            + "archer 7 6 0\n"
            + "archer 4 4 2\n";

        /// <summary>How many rounds <see cref="Transcript"/> decides.</summary>
        private static int TranscriptRounds =>
            Transcript.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;

        /// <summary>
        /// What <c>simcli play-run</c> prints for the script this transcript
        /// writes, transcribed by hand from a real run of the shell.
        /// </summary>
        /// <remarks>
        /// A second copy of a number, and pinned — say exactly to what. Every
        /// other assertion in this file compares the client against itself,
        /// which is a check that cannot catch the two programs drifting apart.
        /// This one is the shell's own answer, taken out of the other process
        /// and written down, so a change that moves the client's run without
        /// moving the shell's turns this red on the push that moved it.
        /// </remarks>
        private const string TheShellPrinted =
            "outcome    3 waves survived, 0 of 800 health left, 0 dealt over 4 rounds, ended OutOfHealth";

        /// <summary>What a transcript line says instead of a tower to build.</summary>
        private const string Nothing = "-";

        /// <summary>What a transcript line puts in front of a rung to climb into.</summary>
        private const char Upgraded = '^';

        [Test]
        public void EveryWaveIsBuiltCommittedAndWatchedInOneScene()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Building), "A run opens on its first build phase.");
            Assert.That(root.MatchView, Is.Null, "And there is no match on screen until one is committed.");

            Play(root, loop);

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Over));
            Assert.That(loop.Rounds.Count, Is.EqualTo(TranscriptRounds), "Every round of it was played.");
            Assert.That(loop.Decisions.Count, Is.EqualTo(TranscriptRounds));
            Assert.That(root.Run.Round, Is.EqualTo(TranscriptRounds));
            Assert.That(root.Run.Ending, Is.EqualTo(RunEnding.OutOfHealth));
            Assert.That(root.Run.Board.Count, Is.GreaterThan(0), "The transcript stood towers.");
        }

        /// <summary>
        /// The session is proved and written, and the script it wrote is one
        /// <c>simcli play-run</c> plays back to the same outcome.
        /// </summary>
        [Test]
        public void TheSessionIsProvedAndWrittenAndReplaysToTheSameOutcome()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Play(root, loop);

            Assert.That(loop.Proved, Is.Not.Null);
            Assert.That(loop.Proved.Agreed, Is.True, loop.Proved.Disagreement);
            Assert.That(loop.Proved.RoundsProved, Is.EqualTo(TranscriptRounds));
            Assert.That(loop.ScriptPath, Is.Not.Null, "An agreeing session writes its script.");
            Assert.That(File.Exists(loop.ScriptPath), Is.True, loop.ScriptPath);

            string script = File.ReadAllText(loop.ScriptPath);

            Assert.That(script, Is.EqualTo(loop.Proved.Script), "What was written is what was proved.");

            // What the two command-file verbs do to this exact text: record-run
            // compiles it, having replayed it, and play-run plays the bytes into
            // a run built fresh on the same seed and the same shape. The outcome
            // line is the one play-run prints.
            IReadOnlyList<RecordCommand> commands = CommandScript.Parse(WrittenRun.FileName, script);
            (byte[] bytes, IReadOnlyList<RoundReport> compiled) =
                CommandStream.Recorded(root.RunOn(TheMatchOnScreen.Seed), commands);

            Assert.That(compiled.Count, Is.EqualTo(TranscriptRounds));

            CommandStream stream = CommandStream.FromBytes(WrittenRun.FileName, bytes);
            Run played = root.RunOn(TheMatchOnScreen.Seed);

            Assert.That(stream.Seed, Is.EqualTo(TheMatchOnScreen.Seed));

            stream.Replay(played);

            Assert.That(
                RunSummary.Outcome(played),
                Is.EqualTo(RunSummary.Outcome(root.Run)),
                "The command file this run wrote plays back to the run that wrote it.");

            // And it is this line, transcribed. The script this fixture writes
            // was put through the real verbs -- `simcli record-run --content
            // content --script <it> --seed 1` and then `simcli play-run
            // --commands <that>` -- and this is what the shell printed. Pinning
            // it is what makes the claim above about `play-run` a measurement
            // rather than a belief: everything up to here proves the client
            // agrees with itself, and only a number taken out of the other
            // program proves the two are one run.
            Assert.That(
                RunSummary.Outcome(root.Run),
                Is.EqualTo(TheShellPrinted),
                "The shell and the client no longer agree about this run. Regenerate the line by playing "
                + "the script this test writes through simcli record-run and play-run; a content change "
                + "that moved it moved both, and a change that moved only one is the bug this pins.");
        }

        /// <summary>
        /// <b>The build chrome comes down while a round is watched.</b> Until
        /// there were modes, a recorded match played underneath the palette and
        /// a composed tower could be stood on a hex a drawn tower already
        /// occupied. Two things drawing one board is what this removes.
        /// </summary>
        [Test]
        public void TheModesDoNotDrawOverEachOther()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Assert.That(root.Building, Is.Not.Null, "Build mode draws the composed board.");
            Assert.That(root.Palette, Is.Not.Null);
            Assert.That(root.Wave, Is.Not.Null);
            Assert.That(root.Pointer, Is.Not.Null);
            Assert.That(root.MatchView, Is.Null);
            Assert.That(root.Controls, Is.Null, "And there is nothing to scrub.");

            Compose(root, "soldier 7 0 1");
            loop.Press();

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Watching));
            Assert.That(root.Building, Is.Null, "Watch mode draws the match and nothing else.");
            Assert.That(root.Palette, Is.Null);
            Assert.That(root.Wave, Is.Null);
            Assert.That(root.Pointer, Is.Null);
            Assert.That(root.Composing, Is.Null);
            Assert.That(root.MatchView, Is.Not.Null);
            Assert.That(root.Controls, Is.Not.Null, "The playback controls come up with the match.");

            loop.Press();

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Building), "And back again for the next round.");
            Assert.That(root.MatchView, Is.Null);
            Assert.That(root.Controls, Is.Null);
            Assert.That(root.Building, Is.Not.Null);
            Assert.That(root.Composing.Wave, Is.EqualTo(2), "On the second round's phase.");
        }

        /// <summary>
        /// <b>Committing is the only thing on this screen that reaches the
        /// run.</b> Everything clicked composes a phase in a local; pressing the
        /// button is what hands it over.
        /// </summary>
        [Test]
        public void OnlyCommittingReachesTheRun()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());
            Run run = loop.Run;

            int opening = run.Purse.Gold;

            Compose(root, "soldier 7 0 2");

            Assert.That(root.Composing.Board.Count, Is.EqualTo(1), "The composed board has a tower on it.");
            Assert.That(run.Board.Count, Is.EqualTo(0), "The run's board has not moved.");
            Assert.That(run.Purse.Gold, Is.EqualTo(opening), "Nor has its purse.");
            Assert.That(run.Round, Is.EqualTo(0), "Nor has its round.");
            Assert.That(run.Sent, Is.Empty, "And nothing has been sent.");

            loop.Press();

            Assert.That(run.Round, Is.EqualTo(1), "Pressing the button is what moves it.");
            Assert.That(run.Board.Count, Is.EqualTo(1));
            Assert.That(run.Sent.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// The header carries three fields and the same three in both modes: the
        /// wave, the health and the gold. Neither of them is a forecast and
        /// neither names a slot count — #179 took the take and the slot bound
        /// out of the rules, which is why the purse is the figure a wave is
        /// composed against.
        /// </summary>
        [Test]
        public void TheHeaderSaysTheWaveTheHealthAndTheGoldInBothModes()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());
            RunHeader header = loop.Header;

            Assert.That(header.Wave.text, Is.EqualTo("Wave 1 of 10"));
            Assert.That(header.Health.text, Is.EqualTo("Health 800 of 800"));
            Assert.That(header.Gold.text, Is.EqualTo("100 gold"), "The purse a run opens on.");
            Assert.That(header.Action.text, Is.EqualTo(RunLoop.CommitLabel));
            Assert.That(header.Ending.text, Is.Empty, "No end frame while the run is going.");

            Compose(root, "soldier 7 0 1");
            header.Follow();

            Assert.That(
                header.Gold.text,
                Is.EqualTo("61 gold"),
                "A hundred, less a Soldier at 30 and the cheapest creep the box offered at 9. The gold on "
                + "the bar is what the composed round would leave, which is what a wave is composed against.");

            loop.Press();

            Assert.That(header.Wave.text, Is.EqualTo("Wave 1 of 10"), "The wave being watched is the one composed.");
            Assert.That(loop.Run.Purse.Gold, Is.Not.EqualTo(100), "The round was paid for and paid out.");
            Assert.That(
                header.Gold.text,
                Is.EqualTo(RosterNames.Gold(loop.Run.Purse.Gold)),
                "Watching shows the run's own purse and not a composed one.");
            Assert.That(header.Action.text, Is.EqualTo(RunLoop.GoOnLabel));

            loop.Press();

            Assert.That(header.Wave.text, Is.EqualTo("Wave 2 of 10"));
            Assert.That(header.Action.text, Is.EqualTo(RunLoop.CommitLabel));
        }

        /// <summary>
        /// The end frame says what the run came to and where the script went,
        /// and the button that moved the run on is gone.
        /// </summary>
        [Test]
        public void TheEndFrameSaysWhatTheRunCameTo()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Play(root, loop);
            loop.Header.Follow();

            Assert.That(loop.Header.Action.text, Is.Empty, "There is nothing left to press.");
            Assert.That(
                loop.Header.Ending.text,
                Does.Contain("10 waves survived, 326 of 800 health left, 0 dealt over 10 rounds"),
                "The fold the shell prints for this run, written out rather than re-derived from the "
                + "expression that produced it.");
            Assert.That(loop.Header.Ending.text, Does.Contain(loop.ScriptPath));
            Assert.That(loop.Header.Wave.text, Is.EqualTo("Wave 10 of 10"));

            // None of the record's vocabulary on screen. RunSummary.Outcome is
            // the terminal's line and names the ending by its enum member, which
            // is why the end frame is the fold's own sentence and not that one.
            Assert.That(loop.Header.Ending.text, Does.Not.Contain(RunEnding.OutOfWaves.ToString()));
            Assert.That(loop.Header.Ending.text, Does.Not.Contain("outcome    "));
        }

        /// <summary>
        /// <b>The committed round actually runs, and scrubs.</b> Every other
        /// test here drives the loop synchronously, so no tick ever happens —
        /// which proves the modes and proves nothing about the thing the player
        /// is watching. This one lets frames pass.
        /// </summary>
        /// <remarks>
        /// The seek back is ADR-0026 inside the loop: a watched round is
        /// re-simulated from tick zero on every seek, so scrubbing one is a
        /// fresh determinism check rather than a cache being read.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheWatchedRoundRunsAndScrubs()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Compose(root, "archer 6 2 2");
            loop.Press();

            Assert.That(root.Playback, Is.Not.Null, "Watching comes with the controls that drive it.");
            Assert.That(root.Playback.Tick, Is.EqualTo(0), "A committed round arrives on tick zero.");
            Assert.That(root.Playback.FinalTick, Is.GreaterThan(0), "And knows its length before the first frame.");

            // Until the clock has crossed a tick, bounded so a slow frame is a
            // longer test rather than a failing one.
            for (int frame = 0; frame < 600 && root.Playback.Tick == 0; frame++)
            {
                yield return null;
            }

            Assert.That(root.Playback.Tick, Is.GreaterThan(0), "Frames passing is what advances a watched round.");

            root.Playback.SeekToEnd();

            Assert.That(root.Playback.Tick, Is.EqualTo(root.Playback.FinalTick));
            Assert.That(root.MatchView.IsFinished, Is.True, "To the end resolves it.");

            root.Playback.SeekTo(0);

            Assert.That(root.Playback.Tick, Is.EqualTo(0), "And it scrubs back, by re-simulating.");
            Assert.That(root.MatchView.IsFinished, Is.False);

            loop.Press();

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Building), "A round that was watched is left, not waited on.");
        }

        /// <summary>
        /// A click on the header stops at the header. Which way up a screen
        /// point is read is the one thing here a reasonable person could get
        /// backwards, and getting it backwards is invisible: the button would
        /// still commit and the tower would <i>also</i> land on whatever hex was
        /// behind the bar.
        /// </summary>
        /// <remarks>
        /// The other bars are anchored to the bottom of the screen and this one
        /// is anchored to the top, so this pins the opposite end of the flip
        /// from <see cref="BuildingTests"/> and <see cref="WaveTests"/> — and
        /// both ends are asserted, because an assertion only that the middle of
        /// the screen is clear passes just as well when the panel was never laid
        /// out at all.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheHeaderSwallowsTheClicksThatLandOnIt()
        {
            MatchRoot root = Playfield();

            root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            yield return null;
            yield return null;

            // The bar is along the top of a panel laid out 1080 high and scaled
            // to the window's height, so it reaches down from the top of the
            // screen by its own height times that ratio.
            float scale = Screen.height / (float)RuntimePanel.ReferenceResolution.y;
            var onTheHeader = new Vector2(
                Screen.width * 0.5f, Screen.height - (RunHeader.BarHeight * 0.5f * scale));
            var overTheBoard = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Assert.That(root.Loop.Header.Covers(onTheHeader), Is.True, "A point on the header bar.");
            Assert.That(root.Loop.Header.Covers(overTheBoard), Is.False, "A point well below it.");

            root.Pointer.Shortcut(IndexInPalette(root, "soldier"));

            Assert.That(root.Pointer.Click(onTheHeader), Is.False);
            Assert.That(
                root.Composing.Phase.Actions.Count,
                Is.EqualTo(0),
                "A click on the header must not also land on the board behind it.");

            root.Pointer.Point(onTheHeader);

            Assert.That(root.Building.IsLit, Is.False);
        }

        /// <summary>
        /// <b>What you watch is your own towers.</b> You build a defence, you
        /// press Done, and the round that goes up is that defence against
        /// somebody else's wave. The other direction of the same round -- the
        /// wave you composed, walking into somebody else's towers -- is the
        /// screen's second view and is one press away.
        /// </summary>
        /// <remarks>
        /// #206. The screen showed the offence and only the offence, so the
        /// towers on it were a stranger's and the creeps were the player's own
        /// -- and a round that composed no wave drew an empty map. What pins it
        /// is the pieces rather than a label: the layout on screen is the one
        /// this round committed, object for object, and the wave walking is not
        /// the one it sent.
        /// </remarks>
        [Test]
        public void TheResultsScreenOpensOnYourDefenceAndSwitchesToYourOffence()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Assert.That(loop.Switch, Is.Not.Null, "The control is built with the run.");

            Compose(root, "soldier 7 0 1");
            loop.Press();

            RoundOrders sent = loop.Run.Sent[0];

            Assert.That(loop.WatchingAttack, Is.False, "A committed round opens on the defence.");
            Assert.That(
                root.MatchView.Match.Layout,
                Is.SameAs(sent.Defense),
                "The towers on screen are the ones this round built.");
            Assert.That(
                root.MatchView.Match.Layout.Count,
                Is.EqualTo(1),
                "Which is the one the transcript line stood.");
            Assert.That(
                root.MatchView.Match.Wave,
                Is.Not.SameAs(sent.Wave),
                "And the creeps walking at them are somebody else's.");

            ulong defending = root.MatchView.Match.Seed;

            Press(loop.Switch.Offence);

            Assert.That(loop.WatchingAttack, Is.True);
            Assert.That(
                root.MatchView.Match.Wave,
                Is.SameAs(sent.Wave),
                "The offence is the wave this round composed ...");
            Assert.That(
                root.MatchView.Match.Layout,
                Is.Not.SameAs(sent.Defense),
                "... against somebody else's towers.");
            Assert.That(
                root.MatchView.Match.Seed,
                Is.Not.EqualTo(defending),
                "Two fights and not one seen twice: the seed folds the direction.");

            Press(loop.Switch.Defence);

            Assert.That(loop.WatchingAttack, Is.False, "And back, as often as anybody likes.");
            Assert.That(root.MatchView.Match.Layout, Is.SameAs(sent.Defense));
            Assert.That(root.MatchView.Match.Seed, Is.EqualTo(defending));

            loop.Press();

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Building));
            Assert.That(loop.WatchingAttack, Is.False, "The next round opens on its defence too.");
        }

        /// <summary>
        /// <b>Switching is a view control and not a mode.</b> Both matches were
        /// resolved when the round was committed, so asking for the other one
        /// moves nothing: not the round, not the purse, not the health, and not
        /// the phase, because there is no phase being composed.
        /// </summary>
        [Test]
        public void SwitchingTheViewDoesNotMoveTheRun()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Compose(root, "soldier 7 0 2");
            loop.Press();

            int round = loop.Run.Round;
            int gold = loop.Run.Purse.Gold;
            int health = loop.Run.Health;
            int towers = loop.Run.Board.Count;
            int sent = loop.Run.Sent.Count;

            loop.Watch(attacking: true);
            loop.Watch(attacking: false);
            loop.Watch(attacking: true);

            Assert.That(loop.Run.Round, Is.EqualTo(round), "Advance is still the only thing that moves a run.");
            Assert.That(loop.Run.Purse.Gold, Is.EqualTo(gold));
            Assert.That(loop.Run.Health, Is.EqualTo(health));
            Assert.That(loop.Run.Board.Count, Is.EqualTo(towers));
            Assert.That(loop.Run.Sent.Count, Is.EqualTo(sent));

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Watching), "And it is not a mode of its own.");
            Assert.That(root.Composing, Is.Null, "Nothing is being composed behind it.");
            Assert.That(root.Building, Is.Null);
            Assert.That(root.Wave, Is.Null, "The wave bar is #197's and this does not touch it.");
            Assert.That(root.MatchView, Is.Not.Null, "There is exactly one match on screen ...");
            Assert.That(root.Controls, Is.Not.Null, "... and one scrub bar over it.");
        }

        /// <summary>
        /// <b>The number and the picture are the same match.</b> Health is spent
        /// on what got past this round's own towers, and those towers are what
        /// the screen opens on -- so the figure on the header and the fight
        /// under it finally agree. Watching the offence does not move it, because
        /// the offence is not what it is measured from.
        /// </summary>
        [Test]
        public void TheHeadersHealthMovesOnTheDefenceItIsShowing()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());
            RunHeader header = loop.Header;

            Assert.That(header.Health.text, Is.EqualTo("Health 800 of 800"));

            Compose(root, "soldier 7 0 1");
            loop.Press();

            int health = loop.Run.Health;

            Assert.That(health, Is.LessThan(800), "Something got past this round's defence.");
            Assert.That(header.Health.text, Is.EqualTo("Health " + health + " of 800"));
            Assert.That(
                root.MatchView.Match.Layout,
                Is.SameAs(loop.Run.Sent[0].Defense),
                "And the defence it was taken off is the one on screen.");

            Press(loop.Switch.Offence);
            header.Follow();

            Assert.That(
                loop.Run.Health,
                Is.EqualTo(health),
                "Watching the other direction does not spend health, because health is not what it scores.");
            Assert.That(header.Health.text, Is.EqualTo("Health " + health + " of 800"));
        }

        /// <summary>
        /// The control is up only while a round is being watched, it says the
        /// two views in words, and the one on screen is the one that is lit.
        /// Neither word is a type id or the record's vocabulary.
        /// </summary>
        /// <remarks>
        /// Pinned at both ends in every mode the loop has, because an assertion
        /// at one end alone passes just as well when the row was never laid out
        /// or the display was never written -- the same reason
        /// <see cref="RuntimePanel.Covers"/> is tested at both ends.
        /// </remarks>
        [Test]
        public void TheResultsSwitchIsUpOnlyWhileARoundIsWatched()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());
            ResultsSwitch control = loop.Switch;

            Assert.That(control.Defence.text, Is.EqualTo("Defence"));
            Assert.That(control.Offence.text, Is.EqualTo("Offence"));
            Assert.That(control.IsUp, Is.False, "There is nothing to switch between while a round is composed.");

            Compose(root, "soldier 7 0 1");
            loop.Press();

            Assert.That(control.IsUp, Is.True, "A watched round has two views and this is how you say which.");
            Assert.That(control.Defence.text, Is.EqualTo("Defence"), "The labels do not move with the view.");
            Assert.That(control.Offence.text, Is.EqualTo("Offence"));

            Color lit = control.Defence.style.backgroundColor.value;
            Color dim = control.Offence.style.backgroundColor.value;

            Assert.That(lit, Is.Not.EqualTo(dim), "The view on screen is the one that is lit.");

            Press(control.Offence);

            Assert.That(control.Offence.style.backgroundColor.value, Is.EqualTo(lit), "And it swaps with the view.");
            Assert.That(control.Defence.style.backgroundColor.value, Is.EqualTo(dim));

            loop.Press();

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Building));
            Assert.That(control.IsUp, Is.False, "And it goes down again with the match.");
        }

        /// <summary>
        /// <b>A session that does not agree writes nothing, and says why.</b> The
        /// simulation's half of that is <c>ProvedSession</c>; this is the
        /// client's half, which #198 shipped untested -- the branch that hands
        /// back no path and the two sentences a person is shown where there is
        /// no path to name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The disagreement is made rather than waited for. Two rounds are
        /// played through the screen and then handed back to the prover in the
        /// wrong order, so what the session claims it was shown at round one is
        /// what round two actually came to. That is a session that did not see
        /// what it played, which is the whole class of fault this step exists to
        /// catch, and it is arranged rather than hoped for -- a fresh run on a
        /// different seed was tried first and agreed, because the pool is one
        /// canned member and both runs met the same defence.
        /// </para>
        /// <para>
        /// The structural guarantee is what is asserted first: there is no
        /// script at all, so there is nothing this side could write even by
        /// ignoring the sentence.
        /// </para>
        /// </remarks>
        [Test]
        public void ADisagreeingSessionWritesNothingAndSaysSo()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Compose(root, "archer 6 2 2");
            loop.Press();
            loop.Press();

            Compose(root, "archer 7 4 0");
            loop.Press();
            loop.Press();

            Assert.That(
                loop.Rounds[0].ToString(),
                Is.Not.EqualTo(loop.Rounds[1].ToString()),
                "The two rounds have to differ for swapping them to be a disagreement.");

            var outOfOrder = new List<RoundReport> { loop.Rounds[1], loop.Rounds[0] };

            ProvedSession astray = ProvedSession.Of(
                loop.Decisions,
                outOfOrder,
                loop.Run,
                () => root.RunOn(TheMatchOnScreen.Seed));

            Assert.That(astray.Agreed, Is.False, "The fresh run did not say what this session claims it saw.");
            Assert.That(astray.Script, Is.Empty, "So it hands back no script at all.");
            Assert.That(astray.Disagreement, Is.Not.Null);

            string path = WrittenRun.Written(astray, Scratch());

            Assert.That(path, Is.Null, "And nothing lands on disk.");
            Assert.That(
                WrittenRun.Wording(astray, path),
                Is.EqualTo(astray.Disagreement),
                "What a person is shown is the prover's own sentence, which names the round the two runs "
                + "parted on. A screen that replaced it with 'could not save' would throw away the only "
                + "description of the bug there is.");
        }

        /// <summary>
        /// A session that committed no round decided nothing, so there is no
        /// script, nothing is written, and what is said is that rather than a
        /// disagreement.
        /// </summary>
        [Test]
        public void ASessionThatPlayedNoRoundWritesNothingAndSaysThat()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            ProvedSession nothing = ProvedSession.Of(
                new BuildPhase[0],
                new RoundReport[0],
                loop.Run,
                () => root.RunOn(TheMatchOnScreen.Seed));

            Assert.That(nothing.Agreed, Is.True, "Two runs that played nothing did not disagree.");
            Assert.That(nothing.Script, Is.Empty);

            string path = WrittenRun.Written(nothing, Scratch());

            Assert.That(path, Is.Null, "An empty script is not a file with nothing in it.");
            Assert.That(
                WrittenRun.Wording(nothing, path),
                Is.EqualTo("No round was played, so there is no script to write."));
        }

        // ---------------------------------------------------------------
        // Scaffolding
        // ---------------------------------------------------------------

        /// <summary>
        /// One button on the chrome, pressed.
        /// </summary>
        /// <remarks>
        /// A submit rather than a synthesised pointer press, because a pointer
        /// press is answered from the element's laid-out rectangle and nothing
        /// here has been through a layout pass -- a test that clicked at
        /// coordinates would be asserting on the panel's geometry by accident.
        /// Both routes end in the same <c>Clickable</c>.
        /// </remarks>
        private static void Press(Button button)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }
        }

        private MatchRoot Playfield() =>
            Spawn(SceneFraming.RootObjectName).AddComponent<MatchRoot>();

        /// <summary>
        /// Where a played session's script lands in a test. Not
        /// <c>Application.persistentDataPath</c>, which is where a person's run
        /// writes: a suite that wrote over it would delete the thing somebody
        /// was about to paste into <c>content/commands.txt</c>.
        /// </summary>
        private static string Scratch() =>
            Path.Combine(Application.temporaryCachePath, "run-loop-tests");

        /// <summary>Drives the whole transcript, one round to a line.</summary>
        private static void Play(MatchRoot root, RunLoop loop)
        {
            foreach (string line in Transcript.Split('\n'))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                Assert.That(loop.Mode, Is.EqualTo(RunMode.Building), "Round " + loop.Wave + " of the transcript.");
                Assert.That(loop.Header.Action.text, Is.EqualTo(RunLoop.CommitLabel));

                Compose(root, line);

                loop.Press();

                Assert.That(loop.Mode, Is.EqualTo(RunMode.Watching), "Committing puts the round on screen.");
                Assert.That(root.MatchView.IsRunning, Is.True, "And the match it resolved to is drawn.");

                loop.Press();
            }
        }

        /// <summary>
        /// One line of the transcript, pressed through the screen: the palette
        /// is selected from, a hex is clicked, and the wave's boxes are filled
        /// out of the lists they offer.
        /// </summary>
        private static void Compose(MatchRoot root, string line)
        {
            string[] words = line.Split(' ');

            Build(root, words[0], Number(words[1]), Number(words[2]));

            int creeps = Number(words[3]);

            // What the round walked in already holding. A creep is bought once
            // and attacks every round after (#207), so what this line names is
            // how many bodies the round ADDS and the row it adds them to is the
            // one the last round left.
            int carried = Bodies(root);

            for (int sent = 0; sent < creeps; sent++)
            {
                int box = root.Composing.Slots.Count;
                IReadOnlyList<UnitType> offered = root.Composing.Sendable(box);

                if (offered.Count > 0)
                {
                    root.Wave.Open(box);
                    root.Wave.Choose(offered[0]);

                    continue;
                }

                // Every creep the roster has is already in the row, which is
                // reachable now that a row keeps what it bought: a creep fills
                // at most one slot, so past that point another body is a higher
                // count on a box rather than a new one.
                Assert.That(
                    root.Composing.CanSendMore(0),
                    Is.True,
                    "Wave " + root.Composing.Wave + " can neither open a box nor raise one.");

                root.Wave.Open(0);
                root.Wave.More();
            }

            Assert.That(
                Bodies(root),
                Is.EqualTo(carried + creeps),
                "The wave was filled, on top of what the round carries.");
        }

        /// <summary>How many creeps the composed wave sends, over all its boxes.</summary>
        private static int Bodies(MatchRoot root)
        {
            int bodies = 0;

            for (int index = 0; index < root.Composing.Slots.Count; index++)
            {
                bodies += root.Composing.Slots[index].Count;
            }

            return bodies;
        }

        /// <summary>
        /// What one line of the transcript builds: a tower on a named cell, a
        /// rung climbed into on one, or nothing at all.
        /// </summary>
        /// <remarks>
        /// Both paths go through the pointer. Placing is a palette entry chosen
        /// by its shortcut and a click on a hex; upgrading is a click on a hex
        /// something already stands on, which opens the offer, and then the rung
        /// taken out of it. Every assertion here is really an assertion about
        /// prevention: a rung the offer did not carry, or a cell the round would
        /// not take, fails the line rather than quietly composing something
        /// else.
        /// </remarks>
        private static void Build(MatchRoot root, string what, int column, int row)
        {
            if (what == Nothing)
            {
                return;
            }

            int before = root.Composing.Board.Count;

            if (what[0] == Upgraded)
            {
                Climb(root, what.Substring(1), column, row);

                Assert.That(root.Composing.Board.Count, Is.EqualTo(before), "An upgrade replaces.");

                return;
            }

            root.Pointer.Shortcut(IndexInPalette(root, what));

            Assert.That(
                root.Pointer.Click(ScreenPointOf(root, column, row)),
                Is.True,
                "The click did not stand a " + what + " at " + column + ", " + row);

            Assert.That(root.Composing.Board.Count, Is.EqualTo(before + 1));
        }

        /// <summary>
        /// Climbs the tower on a cell into a named rung, the way a hand does:
        /// click the hex to open its offer, then take the rung out of it.
        /// </summary>
        private static void Climb(MatchRoot root, string label, int column, int row)
        {
            root.Pointer.Click(ScreenPointOf(root, column, row));

            Assert.That(
                root.Palette.IsOffering,
                Is.True,
                "Nothing at " + column + ", " + row + " offered a rung on wave " + root.Composing.Wave);

            foreach (UnitType rung in root.Composing.UpgradesOn(column, row))
            {
                if (rung.Label == label)
                {
                    root.Palette.Take(rung);

                    Assert.That(
                        root.Composing.StandingOn(column, row).Label,
                        Is.EqualTo(label),
                        "The rung was taken and nothing changed.");

                    return;
                }
            }

            Assert.Fail("The offer at " + column + ", " + row + " does not carry " + label);
        }

        /// <summary>
        /// Where a tower sits in the palette, by label. Fails where it is not
        /// listed, which is the palette refusing to offer something the rules
        /// would refuse.
        /// </summary>
        private static int IndexInPalette(MatchRoot root, string label)
        {
            for (int index = 0; index < root.Composing.Palette.Count; index++)
            {
                if (root.Composing.Palette[index].Label == label)
                {
                    return index;
                }
            }

            Assert.Fail("The palette does not offer " + label + " on wave " + root.Composing.Wave);

            return -1;
        }

        private static Vector2 ScreenPointOf(MatchRoot root, int column, int row) =>
            root.CameraRig.Camera.WorldToScreenPoint(HexGeometry.ToWorld(column, row));

        private static int Number(string word) =>
            int.Parse(word, System.Globalization.CultureInfo.InvariantCulture);
    }
}
