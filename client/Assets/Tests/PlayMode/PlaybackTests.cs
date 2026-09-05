using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using UnityEngine;
using UnityEngine.UIElements;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The playback controller's <c>Advance</c> and <c>SeekTo</c> — the one new
    /// seam this effort adds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These test only the claims that can actually fail.</b> Nothing in the
    /// build gate can reach them, because they are view-side; and nothing here
    /// asserts that seeking to tick N matches a fresh run to tick N, because
    /// seek <i>is</i> a fresh run to tick N. That comparison is a tautology, it
    /// was deleted on purpose, and the thing that replaced it is the re-sim
    /// budget in the build gate — which is allowed to go red, and is the only
    /// legitimate reason anyone would revisit the no-cache decision.
    /// </para>
    /// <para>
    /// What is left is four claims with a failure mode each: that the re-run
    /// ticks' events never arrive, that a seek clears the one remaining thing
    /// that owns a clock — asserted once on the tracer count and once on the
    /// ring a bubble leaves — and that the pool releases by subtraction without
    /// anybody clearing it.
    /// </para>
    /// </remarks>
    public class PlaybackTests : ViewTest
    {
        /// <summary>
        /// Far enough in that the match is busy: creeps walking, towers firing,
        /// shells in the air. Well short of the end, so a seek here is a seek
        /// into the middle of something rather than onto an empty playfield.
        /// </summary>
        /// <remarks>
        /// Multiplied by three when the clock slowed by three on 8 August 2026.
        /// This is a window measured in ticks and what it is really asking for
        /// is a stretch of match, so a dilation that left it alone would have
        /// shrunk it to a third of the match it was written to sample -- and it
        /// said so: at six hundred ticks the busy-enough assertion below went
        /// red at 68 events against the hundred it wants.
        /// </remarks>
        private const int BusyTick = 1800;

        private PlaybackController Playback() => new PlaybackController(Begin());

        // ---------------------------------------------------------------
        // The three claims
        // ---------------------------------------------------------------

        /// <summary>
        /// Events emitted during a seek's re-simulation are discarded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Asserted on the count of events the decorations were ever
        /// <i>told</i> about rather than on the effects left standing
        /// afterwards, and the difference is the whole test. An implementation
        /// that ran the eighteen hundred ticks with the event sink attached and then
        /// tidied up would leave nothing on screen either — and would still
        /// have built and thrown away every tracer, flash and spark of the
        /// match, which on a seek to the end is the frame-long detonation this
        /// claim exists to prevent.
        /// </para>
        /// <para>
        /// The playing half is not decoration on the test: without it, a seek
        /// that quietly failed to advance at all would pass.
        /// </para>
        /// </remarks>
        [Test]
        public void EventsEmittedDuringASeeksReSimulationAreDiscarded()
        {
            // Played, the same ticks say a great deal.
            PlaybackController played = Playback();
            RunUntil(played.View, () => played.View.Current.Tick >= BusyTick);

            Assert.That(played.View.Current.Tick, Is.EqualTo(BusyTick),
                "the match ended before it got busy enough to say anything");
            Assert.That(played.View.Decorations.EventsHeard, Is.GreaterThan(100),
                "the match said almost nothing in eighteen hundred ticks, so this proves nothing");

            // Sought, they say nothing at all.
            PlaybackController sought = Playback();
            int heardBefore = sought.View.Decorations.EventsHeard;

            sought.SeekTo(BusyTick);

            Assert.That(sought.View.Current.Tick, Is.EqualTo(BusyTick),
                "the seek did not land on the tick it was asked for");

            Assert.That(sought.View.Decorations.EventsHeard, Is.EqualTo(heardBefore),
                "the ticks the seek re-ran emitted their events into the decorations, so seeking to the "
                + "end would detonate the whole match's effects in one frame");

            Assert.That(sought.View.Decorations.ActiveCount, Is.Zero,
                "a seek from a standing start put an effect on screen");
        }

        /// <summary>
        /// Effects clear on an explicit seek — and only on an explicit seek.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Both halves matter. Clearing is what a seek owes: effects are the
        /// only thing left in this client that owns a clock, so anything still
        /// fading after a seek belongs to a tick that has not happened yet.
        /// <b>Not</b> clearing is what a fast-forward owes, and it is why the
        /// discontinuity is signalled rather than inferred — a fast-forward
        /// crosses many ticks in one frame, which is exactly what a tick-delta
        /// heuristic would mistake for a seek.
        /// </para>
        /// <para>
        /// Audio would clear here too, alongside the effects, for the same
        /// reason: it is the other thing that owns a clock. This client has
        /// none yet, so there is nothing to assert about it and nothing was
        /// invented to have something.
        /// </para>
        /// </remarks>
        [Test]
        public void EffectsClearOnAnExplicitSeekAndNotOnAFastForward()
        {
            PlaybackController playback = Playback();
            MatchView view = playback.View;

            // Far enough in that a good deal has been drawn. The count is what
            // makes the fast-forward half of this test mean anything, and it
            // only means something while it is large next to what one frame can
            // draw from scratch: tracers arrive on a tower's cooldown, so a
            // frame's worth of ticks is worth about one of them.
            RunUntil(view, () => view.Current.Tick >= BusyTick);

            int tickBefore = view.Current.Tick;
            int drawnBefore = view.Decorations.TracersDrawn;

            Assert.That(drawnBefore, Is.GreaterThan(10),
                "hardly any tracer was drawn in eighteen hundred ticks, so a count that did not drop proves "
                + "nothing about whether something cleared");

            // A fast-forward crosses far more than one tick in a frame and is
            // not a discontinuity. Clear puts this count back to zero and one
            // frame cannot climb back past where it was, so a count that did
            // not drop is a clear that did not happen.
            playback.Speed = PlaybackController.FastestSpeed;
            playback.Advance(1f);

            Assert.That(view.Current.Tick, Is.GreaterThan(tickBefore + PlaybackController.MaxTicksPerFrame),
                "the fast-forward covered fewer ticks than one frame at normal speed would");

            Assert.That(view.Decorations.TracersDrawn, Is.GreaterThanOrEqualTo(drawnBefore),
                "a fast-forward was mistaken for a seek and cleared the effects");

            // And the seek, signalled, clears — with something on screen to
            // clear, which after a fast-forward is not automatic.
            RunUntil(view, () => view.Decorations.ActiveCount > 0);

            Assert.That(view.Decorations.ActiveCount, Is.GreaterThan(0),
                "nothing was on screen, so there was never anything to clear");

            playback.SeekTo(view.Current.Tick);

            Assert.That(view.Decorations.ActiveCount, Is.Zero,
                "an effect survived a seek, so it belongs to a tick that has not happened yet");

            Assert.That(view.Decorations.TracersDrawn, Is.Zero,
                "the drawn count survived the clear");
        }

        /// <summary>
        /// A bubble's ring does not survive a seek and is not drawn twice by
        /// one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The same two claims the tracer count makes, on the decoration a
        /// blast and an aura leave: the count goes back to zero because a ring
        /// still on screen belongs to a tick that has not happened yet, and it
        /// does not climb again, because the ticks the seek re-runs are
        /// re-simulated with no sink attached and say nothing at all.
        /// </para>
        /// <para>
        /// The rings are handed over by hand because no shipped row authors a
        /// bubble, so no match played from <c>content/units.txt</c> would ever
        /// fire one. What that costs is nothing: what is under test is the
        /// seek, and a ring put on screen deliberately is a harder starting
        /// position than one that happened to be there.
        /// </para>
        /// </remarks>
        [Test]
        public void ABubblesRingDoesNotSurviveASeekOrArriveTwiceFromOne()
        {
            PlaybackController playback = Playback();
            MatchView view = playback.View;

            RunUntil(view, () => view.Current.Tick >= BusyTick);

            Assert.That(view.Current.Tick, Is.EqualTo(BusyTick),
                "the match ended before it got busy enough for a seek to re-run anything");

            int towerId = view.Current.Towers[0].Id;

            view.Decorations.AuraPulsed(towerId, 3000, BubblePayload.Cooldown);
            view.Decorations.BlastLanded(towerId, 3000, BubblePayload.Damage);

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(2),
                "the rings were never drawn, so a count that dropped would prove nothing");

            int heardBefore = view.Decorations.EventsHeard;

            // Backwards, so the seek re-simulates eighteen hundred ticks.
            playback.SeekTo(BusyTick);

            Assert.That(view.Decorations.RingsDrawn, Is.Zero,
                "a ring survived a seek, so it belongs to a tick that has not happened yet");

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heardBefore),
                "the re-run ticks emitted their events into the decorations");
        }

        /// <summary>
        /// The id-matched pool releases objects whose ids left the snapshot —
        /// on a seek as on any other frame, and with nobody telling it a seek
        /// happened.
        /// </summary>
        /// <remarks>
        /// The second assertion is the one about the design rather than about
        /// the behaviour: <c>EverCreated</c> not moving is what says the pool
        /// was left alone. A seek that cleared the pool would look identical on
        /// screen and would have thrown away every object it is about to need
        /// again — which is the second bookkeeping path the whole
        /// subtraction-only rule exists to avoid.
        /// </remarks>
        [Test]
        public void SeekingReleasesTheObjectsWhoseIdsLeftTheSnapshot()
        {
            PlaybackController playback = Playback();
            MatchView view = playback.View;

            RunUntil(view, () => view.Creeps.LiveCount > 3);

            int liveBefore = view.Creeps.LiveCount;
            int builtBefore = view.Creeps.EverCreated;

            Assert.That(liveBefore, Is.GreaterThan(3), "the match never got busy enough to prove anything");

            // Back to the beginning, where almost nothing has been released yet.
            playback.SeekTo(0);

            var expected = new HashSet<int>(view.Current.Creeps.Select(creep => creep.Id));
            var drawn = new HashSet<int>(view.Creeps.Live.Keys);

            Assert.That(drawn.Count, Is.LessThan(liveBefore),
                "the seek went backwards and nothing left the playfield");

            Assert.That(drawn.SetEquals(expected), Is.True,
                $"drawing {drawn.Count} creeps for the {expected.Count} in the snapshot after a seek");

            Assert.That(view.Creeps.EverCreated, Is.EqualTo(builtBefore),
                "the seek built new view objects, so something cleared the pool rather than letting "
                + "per-frame id-matching release by subtraction");

            Assert.That(view.Creeps.IdleCount, Is.EqualTo(builtBefore - drawn.Count),
                "the released objects did not go back in the pool");
        }

        // ---------------------------------------------------------------
        // The controls
        // ---------------------------------------------------------------

        /// <summary>
        /// Fast-forward covers proportionally more ticks in the same wall-clock
        /// time, which is the whole of what it is: the speed multiplies the
        /// clock and nothing else.
        /// </summary>
        /// <remarks>
        /// That this speeds the walk cycle up needs no separate assertion and
        /// could not have one that meant anything — locomotion phase is a pure
        /// function of distance travelled, and more ticks is more distance.
        /// It is row five of the sit-down table because the way it fails is by
        /// looking wrong, not by computing wrong.
        /// </remarks>
        [Test]
        public void FastForwardCoversProportionallyMoreTicks()
        {
            const float Frame = 1f / 60f;
            const int Frames = 60;

            int TicksInOneSecondAt(float speed)
            {
                var playback = new PlaybackController(Begin()) { Speed = speed };

                for (int frame = 0; frame < Frames; frame++)
                {
                    playback.Advance(Frame);
                }

                return playback.Tick;
            }

            int atOne = TicksInOneSecondAt(1f);
            int atFour = TicksInOneSecondAt(4f);

            // Measured against the tick rate rather than against each other,
            // because sixty additions of a sixtieth do not come to exactly one
            // and comparing two drifted numbers doubles the slack needed.
            Assert.That(atOne, Is.EqualTo(Sim.Match.TicksPerSecond).Within(2),
                $"a second of wall clock at normal speed covered {atOne} ticks");

            Assert.That(atFour, Is.EqualTo(4 * Sim.Match.TicksPerSecond).Within(2),
                $"four times speed covered {atFour} ticks where normal speed covered {atOne}");
        }

        /// <summary>
        /// The controls are built with something to drag, something to press
        /// and something to read, and dragging the slider seeks.
        /// </summary>
        /// <remarks>
        /// Every one of these is a way to ship a build whose playback control
        /// is invisible or inert while every other test in this suite stays
        /// green: a slider whose range was never set does not move, a panel
        /// with no theme has no font and no slider track to drag, and elements
        /// that never reached a panel are drawn by nothing and clicked by
        /// nothing.
        /// </remarks>
        [Test]
        public void TheControlsCanBeDraggedPressedAndRead()
        {
            GameObject host = Spawn("ControlsTest");

            PlaybackController playback = new PlaybackController(TheMatchOnScreen.Begin(host));
            PlaybackControls controls = PlaybackControls.Build(host.transform, playback);

            Assert.That(controls.Scrubber.lowValue, Is.Zero, "the scrub bar does not start at tick zero");
            Assert.That(controls.Scrubber.highValue, Is.EqualTo(playback.FinalTick),
                "the scrub bar's end is not the end of the match");
            Assert.That(playback.FinalTick, Is.GreaterThan(60 * Sim.Match.TicksPerSecond),
                "the match resolved far too early to be the one this project is written about");

            Assert.That(controls.Document.panelSettings.themeStyleSheet, Is.Not.Null,
                "a panel with no theme style sheet has no font and no slider track");
            Assert.That(controls.Readout.panel, Is.Not.Null,
                "the bar never reached a panel, so nothing draws it and nothing can click it");
            Assert.That(host.GetComponentsInChildren<Canvas>(includeInactive: true), Is.Empty,
                "the controls built a uGUI canvas, so the scene is running two UI systems again");
            Assert.That(controls.Buttons.Count, Is.GreaterThan(2),
                "fast-forward and jump-to-the-end are buttons, and pause is the third");

            // Dragging is a value change, and a value change is a seek — one
            // that pauses, because a drag and a clock pulling the same slider
            // in the same frame is a jitter nobody can debug afterwards.
            controls.Scrubber.value = BusyTick;

            Assert.That(playback.Tick, Is.EqualTo(BusyTick), "moving the scrub bar did not seek the match");
            Assert.That(playback.IsPaused, Is.True, "the drag left the match playing out from under it");

            // Pressing play, the bar follows the match instead of driving it.
            Press(controls.Buttons[0]);

            Assert.That(playback.IsPaused, Is.False, "the first button does not start and stop the match");

            playback.Advance(1f);
            controls.Follow();

            Assert.That(playback.Tick, Is.GreaterThan(BusyTick), "the match did not advance after play");
            Assert.That(controls.Scrubber.value, Is.EqualTo(playback.Tick),
                "the scrub bar did not follow the match");

            // And the speed button walks the speeds and comes back round. The
            // wrap is the half worth asserting: a button that only ever climbs
            // leaves the only way back to normal speed a restart.
            for (int press = 1; press < PlaybackControls.Speeds.Length; press++)
            {
                Press(controls.Buttons[1]);

                Assert.That(playback.Speed, Is.EqualTo(PlaybackControls.Speeds[press]),
                    $"press {press} of the speed button did not reach {PlaybackControls.Speeds[press]}x");
            }

            Press(controls.Buttons[1]);

            Assert.That(playback.Speed, Is.EqualTo(PlaybackControls.Speeds[0]),
                "the speed button climbed off the end instead of coming back round to normal");
        }

        /// <summary>
        /// Presses a button the way a keyboard or a gamepad does.
        /// </summary>
        /// <remarks>
        /// A submit rather than a synthesised pointer press, because a pointer
        /// press is answered from the element's laid-out rectangle and nothing
        /// here has been through a layout pass — a test that clicked at
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
    }
}

