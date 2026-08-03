using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// Rows four and five of the sit-down, as assertions: scrubbing backwards
    /// walks the legs backwards, and fast-forward cycles them with the ground
    /// rather than with the clock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These exist because the two rows turned out not to be answerable by
    /// eye.</b> The sit-down was run on the build cut for #48 and every other
    /// row was called: the floor, the atlases, the corridor, the orphaned
    /// shell, the effects, the tower firing, the death clip, the overtake, the
    /// six camera snaps. Four and five came back "hard to tell" — which is the
    /// honest answer, because both ask whether something already in motion is
    /// moving at the right rate, and a walk cycle at thirty hertz does not hold
    /// still to be judged. <c>docs/sit-down.md</c> says what to do about that:
    /// "Anything it catches that <i>can</i> be caught by an assertion should
    /// leave behind an assertion."
    /// </para>
    /// <para>
    /// <b>What was already covered, and is deliberately not re-tested here.</b>
    /// The animation component is proven at its own seam and on the real rig —
    /// <see cref="PlayablesSamplingTests.PoseDoesNotDriftAcrossRealFrames"/>,
    /// <see cref="PlayablesSamplingTests.ScrubbingBackwards_MovesThePoseBackwards"/>,
    /// <see cref="RealRigSamplingTests.ScrubbingTheRealClipBackwards_RetracesTheSamePoses"/>
    /// — and <see cref="PlayableHeadPoisonTests"/> rebuilds it with each guard
    /// removed to show those pass for a reason. A clip that is told a time
    /// poses at that time, forwards or backwards, and never advances on its
    /// own. None of that is in question.
    /// </para>
    /// <para>
    /// <b>What was never covered is the wiring</b>, and it is the whole of what
    /// rows four and five look at: that <see cref="MatchView"/> hands
    /// <see cref="CreepView"/> the distance out of the snapshot, and nothing
    /// else. Every test above builds its own rig and calls
    /// <c>Sample</c> directly, so not one of them would notice
    /// <c>DrawCreeps</c> being rewired to elapsed time. That is the failure the
    /// two rows were watching for — a creep gliding backwards while its feet
    /// walk forwards — and it is what these two assert against, through a real
    /// match, through the real playback seam.
    /// </para>
    /// <para>
    /// <b>The invariant both rest on: the walk phase is what the snapshot's
    /// distance says it is.</b> Not "close to", not "consistent with the last
    /// frame" — equal to <c>Repeat(distance / HexesPerWalkCycle) * clipLength</c>,
    /// which is a quantity the view cannot arrive at by accumulating anything.
    /// An implementation keeping its own playback head can pass a test that
    /// only checks the legs are moving. It cannot pass this one.
    /// </para>
    /// </remarks>
    public class LocomotionTests : ViewTest
    {
        /// <summary>
        /// Deep into the match, where the wave is spread along the corridor
        /// rather than bunched at the entrance — so there are several walking
        /// creeps at different distances and the choice between them is not
        /// between near-identical ones.
        /// </summary>
        private const int SpreadTick = 900;

        /// <summary>Slack for one float divide and one <c>Repeat</c>.</summary>
        private const float Tolerance = 1e-4f;

        private float WalkClipLength => TheMatchOnScreen.Art().CreepWalkClip.length;

        /// <summary>
        /// The walk phase the snapshot demands of a creep, in clip seconds.
        /// </summary>
        /// <remarks>
        /// Deliberately computed from the snapshot the test read, not from
        /// anything the view kept. If these two ever have to be reconciled by
        /// remembering what happened last frame, the architecture this project
        /// is built on has already gone.
        /// </remarks>
        private float PhaseOf(float distanceHexes) =>
            Mathf.Repeat(distanceHexes / MatchTuning.HexesPerWalkCycle, 1f) * WalkClipLength;

        private float PhaseDemandedBy(CreepSnapshot creep) =>
            PhaseOf(SimUnits.ToFloat(creep.DistanceAlongPath));

        private static float DistanceOf(CreepSnapshot creep) =>
            SimUnits.ToFloat(creep.DistanceAlongPath);

        /// <summary>
        /// Whether a creep crossed a cycle boundary between two distances — in
        /// which case its phase wrapped, and comparing the two as plain numbers
        /// reads the wrap as a direction.
        /// </summary>
        private static bool Wrapped(float from, float to) =>
            !Mathf.Approximately(
                Mathf.Floor(from / MatchTuning.HexesPerWalkCycle),
                Mathf.Floor(to / MatchTuning.HexesPerWalkCycle));

        private static Dictionary<int, CreepSnapshot> WalkingAt(MatchView view) =>
            view.Current.Creeps
                .Where(c => c.State == CreepState.Walking)
                .ToDictionary(c => c.Id, c => c);

        /// <summary>
        /// Every walking creep is posed at the phase its own distance implies —
        /// checked over the whole playfield rather than on the one creep a test
        /// picked, so a view that got one right by luck is not enough.
        /// </summary>
        private void AssertLegsAreWhereTheGroundPutThem(MatchView view, string when)
        {
            Assert.That(
                view.Previous, Is.Null,
                "This assertion pins the phase to one snapshot exactly, which is only right on a tick "
                + "the view landed on squarely — after a seek. Mid-tick the view is blending two "
                + "snapshots and the honest check is the bracketed one.");

            IReadOnlyDictionary<int, CreepView> live = view.Creeps.Live;

            var walking = WalkingAt(view);

            Assert.That(
                walking, Is.Not.Empty,
                "No creep is walking " + when + ", so this tick proves nothing about legs. "
                + "Pick a tick where the match is busy.");

            foreach (KeyValuePair<int, CreepSnapshot> pair in walking)
            {
                Assert.That(
                    live.ContainsKey(pair.Key), Is.True,
                    "Creep " + pair.Key + " is in the snapshot " + when + " but has no view.");

                Assert.That(
                    live[pair.Key].LastWalkTime,
                    Is.EqualTo(PhaseDemandedBy(pair.Value)).Within(Tolerance),
                    "Creep " + pair.Key + " is posed at a walk phase its distance along the corridor does "
                    + "not account for, " + when + ". The phase IS the distance; if these disagree "
                    + "something is driving the legs from a clock, and scrubbing will moonwalk.");
            }
        }

        /// <summary>
        /// Mid-tick, every walking creep is posed at a phase belonging to some
        /// point between the tick it came from and the tick it is going to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The bracket rather than a point, because a frame arriving part-way
        /// through a tick blends the two snapshots — and that blend is the
        /// reason the match looks smooth at thirty hertz. Pinning the phase to
        /// the newer snapshot would be asserting the interpolation away.
        /// </para>
        /// <para>
        /// It is still the claim that matters. The bracket is one tick of
        /// travel wide, and a walk phase driven by <c>Time.deltaTime</c> has no
        /// reason to land inside it — which is what the poison run showed.
        /// </para>
        /// </remarks>
        private void AssertLegsStayBracketedByTheGround(MatchView view, string when)
        {
            Assert.That(
                view.Previous, Is.Not.Null,
                "Nothing has been stepped, so there is no tick-to-tick bracket to check " + when + ".");

            IReadOnlyDictionary<int, CreepView> live = view.Creeps.Live;

            Dictionary<int, CreepSnapshot> before = view.Previous.Creeps
                .Where(c => c.State == CreepState.Walking)
                .ToDictionary(c => c.Id, c => c);

            int checkable = 0;

            foreach (CreepSnapshot creep in view.Current.Creeps.Where(c => c.State == CreepState.Walking))
            {
                if (!before.TryGetValue(creep.Id, out CreepSnapshot was)) continue;

                float from = DistanceOf(was);
                float to = DistanceOf(creep);

                // Nothing to bracket if it did not move, and nothing readable
                // if the phase wrapped on the way.
                if (to <= from || Wrapped(from, to)) continue;

                Assert.That(
                    live[creep.Id].LastWalkTime,
                    Is.InRange(PhaseOf(from) - Tolerance, PhaseOf(to) + Tolerance),
                    "Creep " + creep.Id + " is posed at a walk phase outside the ground it covered this "
                    + "tick, " + when + ". Its feet are running on something other than the corridor, "
                    + "which is the skate row five was squinting at.");

                checkable++;
            }

            Assert.That(
                checkable, Is.GreaterThan(0),
                "No creep walked a readable distance " + when + ", so this proves nothing about legs.");
        }

        // ---------------------------------------------------------------
        // Row 4 — scrub backwards
        // ---------------------------------------------------------------

        /// <summary>
        /// Seeking backwards moves the legs backwards, and leaves every creep
        /// posed at the phase its new distance implies.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The strict half of this — one creep's phase being lower a tick
        /// earlier — is asserted only on a creep that did not cross a cycle
        /// boundary between the two ticks, because the phase wraps and a
        /// wrapped comparison would be reading noise. Which creep that is
        /// depends on the match, so it is found rather than named: the pool of
        /// candidates is every creep walking at both ticks, and the assertion
        /// is the same one whichever comes back.
        /// </para>
        /// <para>
        /// This is the row that would have caught the towers-on-their-side
        /// class of bug for animation. It goes red if <c>DrawCreeps</c> is
        /// rewired to elapsed time, if <c>SeekTo</c> stops re-simulating from
        /// the beginning, or if anything downstream starts remembering where
        /// the creep was.
        /// </para>
        /// </remarks>
        [Test]
        public void ScrubbingBackwardsWalksTheLegsBackwards()
        {
            var playback = new PlaybackController(Begin());
            MatchView view = playback.View;

            playback.SeekTo(SpreadTick);
            AssertLegsAreWhereTheGroundPutThem(view, "at tick " + SpreadTick);

            var ahead = WalkingAt(view);
            var aheadPhase = ahead.ToDictionary(
                p => p.Key, p => view.Creeps.Live[p.Key].LastWalkTime);

            playback.SeekTo(SpreadTick - 1);
            AssertLegsAreWhereTheGroundPutThem(view, "a tick earlier");

            var behind = WalkingAt(view);

            // A creep walking at both ticks, which moved, and which did not
            // cross a cycle boundary while doing it -- the only creeps whose
            // phase can be compared as a plain number.
            int id = behind.Keys
                .Where(k => ahead.ContainsKey(k))
                .FirstOrDefault(k =>
                {
                    float back = SimUnits.ToFloat(behind[k].DistanceAlongPath);
                    float fore = SimUnits.ToFloat(ahead[k].DistanceAlongPath);

                    return fore > back
                        && Mathf.Floor(back / MatchTuning.HexesPerWalkCycle)
                            == Mathf.Floor(fore / MatchTuning.HexesPerWalkCycle);
                });

            Assert.That(
                id, Is.Not.EqualTo(0),
                "No creep walked forward across tick " + SpreadTick + " without wrapping its walk cycle, "
                + "so there is nothing here whose phase can be compared without reading a wrap as a "
                + "direction. Move the tick.");

            Assert.That(
                SimUnits.ToFloat(behind[id].DistanceAlongPath),
                Is.LessThan(SimUnits.ToFloat(ahead[id].DistanceAlongPath)),
                "Seeking backwards did not put creep " + id + " further back along the corridor.");

            Assert.That(
                view.Creeps.Live[id].LastWalkTime,
                Is.LessThan(aheadPhase[id]),
                "Creep " + id + " stands further back than it did, and its legs are further THROUGH the "
                + "walk cycle than they were. That is the moonwalk row four exists to catch: the ground "
                + "went backwards and the feet did not.");
        }

        // ---------------------------------------------------------------
        // Row 5 — fast-forward
        // ---------------------------------------------------------------

        /// <summary>
        /// At eight times speed the match covers eight times the ticks, and the
        /// legs are still posed by the ground rather than by the wall clock.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Asserted on ticks advanced rather than on distance travelled,
        /// because a creep is free to die or leak inside the longer window and
        /// distance would then be measuring the match rather than the clock.
        /// Ticks are what speed multiplies; everything downstream is a function
        /// of ticks, which is the reason fast-forward is correct here or
        /// nowhere.
        /// </para>
        /// <para>
        /// The second assertion is the one that fails on a skating creep. A
        /// view whose legs run on <c>Time.deltaTime</c> passes the first —
        /// the simulation would still advance eight times as far — and lands
        /// its feet a factor of eight out, which is exactly what row five was
        /// squinting at.
        /// </para>
        /// </remarks>
        [Test]
        public void FastForwardCyclesTheLegsWithTheGroundAndNotTheClock()
        {
            const float Frame = 1f / 60f;
            const int Frames = 30;
            const int Start = 600;

            var playback = new PlaybackController(Begin());
            MatchView view = playback.View;

            playback.SeekTo(Start);
            for (int i = 0; i < Frames; i++) playback.Advance(Frame);

            int atNormalSpeed = view.Current.Tick - Start;
            AssertLegsStayBracketedByTheGround(view, "after half a second at 1x");

            playback.SeekTo(Start);
            playback.Speed = PlaybackController.FastestSpeed;
            for (int i = 0; i < Frames; i++) playback.Advance(Frame);

            int atEightTimes = view.Current.Tick - Start;

            Assert.That(
                atNormalSpeed, Is.GreaterThan(0),
                "Half a second of frames advanced no ticks at all, so neither speed proves anything.");

            Assert.That(
                atEightTimes,
                Is.EqualTo(atNormalSpeed * (int)PlaybackController.FastestSpeed).Within(1),
                "Eight times speed did not cover eight times the ticks in the same wall clock.");

            AssertLegsStayBracketedByTheGround(view, "after half a second at 8x");
        }
    }
}
