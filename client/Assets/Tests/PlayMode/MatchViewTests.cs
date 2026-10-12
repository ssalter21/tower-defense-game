using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The match on screen, drawn only from snapshots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These drive a real match with the real art and check the properties the
    /// architecture is for, rather than checking that pixels landed. The
    /// distinction matters: a screenshot comparison was rejected on this
    /// project after two frames whose bones were definitively swapped rendered
    /// pixel-identical, so what is asserted here is always a number the view
    /// computed and never a picture it produced.
    /// </para>
    /// <para>
    /// The eyeball half of this ticket is not here and is not supposed to be —
    /// it is the sit-down landmark table, where every row names a tick and says
    /// what broken looks like.
    /// </para>
    /// </remarks>
    public class MatchViewTests : ViewTest
    {
        // ---------------------------------------------------------------
        // Pulling and matching
        // ---------------------------------------------------------------

        [Test]
        public void TheViewHoldsTheLastTwoSnapshotsAndNoMore()
        {
            MatchView view = Begin();

            Assert.IsNotNull(view.Current, "the first snapshot was never pulled");
            Assert.IsNull(view.Previous, "there is no snapshot before the first one");

            Snapshot first = view.Current;
            view.StepOneTick();

            Assert.That(view.Previous, Is.SameAs(first));
            Assert.That(view.Current.Tick, Is.EqualTo(first.Tick + 1));

            Snapshot second = view.Current;
            view.StepOneTick();

            Assert.That(view.Previous, Is.SameAs(second), "the view kept the wrong pair");
        }

        /// <summary>
        /// Every creep in the snapshot is drawn, and nothing else is. The whole
        /// of "matched by id", checked every tick of a real match.
        /// </summary>
        [Test]
        public void WhatIsDrawnIsExactlyWhatIsInTheSnapshot()
        {
            MatchView view = Begin();
            int ticks = 0;

            RunUntil(view, () =>
            {
                ticks++;

                var expected = new HashSet<int>(view.Current.Creeps.Select(c => c.Id));
                var drawn = new HashSet<int>(view.Creeps.Live.Keys);

                Assert.That(drawn.SetEquals(expected), Is.True,
                    $"tick {view.Current.Tick}: drawing {drawn.Count} creeps for {expected.Count} in the snapshot");

                return false;
            });

            Assert.That(ticks, Is.GreaterThan(1000), "the match ended far too early to have proved anything");
            Assert.That(view.Creeps.LiveCount, Is.EqualTo(0), "the match ended with creeps still on screen");
        }

        /// <summary>
        /// The number that says pooling is doing something: objects are built
        /// for the busiest moment of the match, not for every creep in it.
        /// </summary>
        /// <remarks>
        /// The steady state sits a little above the busiest moment rather than
        /// exactly on it, because within a sync every claim comes before any
        /// release — until the claims are in, nothing knows which ids stopped
        /// appearing. What is asserted is the property that matters: objects
        /// are built for the busiest moment, not for every creep in the wave.
        /// Every bound here is taken off the match this run actually played, so
        /// a content change that moves where the busiest moment falls does not
        /// redden a claim about pooling.
        ///
        /// <b>The busiest moment is counted per unit type, because that is what
        /// the pool holds.</b> A creep view is built around its type's model, so
        /// an idle Skeleton cannot be lent to a Minion and the two settle at
        /// their own steady states independently. Counting the wave as one
        /// population would assert a bound the pool never promised, and it
        /// would be a bound that tightens every time a wave sends one more kind
        /// of body.
        /// </remarks>
        [Test]
        public void ObjectsArePooledAcrossTheWholeMatch()
        {
            MatchView view = Begin();
            int mostAtOnce = 0;
            int created = 0;
            int lastBuiltOnTick = 0;
            var mostAtOnceOfType = new Dictionary<int, int>();
            var onThisTick = new Dictionary<int, int>();

            RunUntil(view, () =>
            {
                mostAtOnce = Mathf.Max(mostAtOnce, view.Creeps.LiveCount);

                onThisTick.Clear();

                foreach (CreepSnapshot creep in view.Current.Creeps)
                {
                    onThisTick.TryGetValue(creep.TypeId, out int alive);
                    onThisTick[creep.TypeId] = alive + 1;
                }

                foreach (KeyValuePair<int, int> alive in onThisTick)
                {
                    mostAtOnceOfType.TryGetValue(alive.Key, out int most);
                    mostAtOnceOfType[alive.Key] = Mathf.Max(most, alive.Value);
                }

                // WHEN the pool last grew, rather than what it had grown to by
                // some named tick. The named tick was 1000, which stopped being
                // in the second half of anything when the release cadence was
                // dilated on 8 August 2026 and the busiest moment moved past it:
                // the assertion went red at 14 against 12 without a single
                // object having failed to be reused. A tick number written here
                // is a claim about the shape of the committed match, and this
                // test has no business making one.
                if (view.Creeps.EverCreated > created)
                {
                    created = view.Creeps.EverCreated;
                    lastBuiltOnTick = view.Current.Tick;
                }

                return false;
            });

            int total = StreamingContent.ReadWave(StreamingContent.ReadUnitTypes()).TotalUnits;

            Assert.That(mostAtOnce, Is.GreaterThan(1), "the match never had two creeps on it at once");

            // One steady state per unit type, each of them the type's own
            // busiest moment plus the one object a claim-before-release costs.
            int ceiling = mostAtOnceOfType.Values.Sum() + mostAtOnceOfType.Count;

            Assert.That(view.Creeps.EverCreated, Is.LessThanOrEqualTo(ceiling),
                "more objects were built than were ever alive at once, so something is not being reused");

            Assert.That(view.Creeps.EverCreated, Is.LessThan(total),
                $"{view.Creeps.EverCreated} objects for {total} creeps is one per creep, not a pool");

            Assert.That(lastBuiltOnTick, Is.LessThan(view.Current.Tick / 2),
                $"the pool built its last object on tick {lastBuiltOnTick} of {view.Current.Tick}, which is "
                + "the second half of the match");
        }

        // ---------------------------------------------------------------
        // Creeps
        // ---------------------------------------------------------------

        /// <summary>
        /// Locomotion phase comes from distance travelled, and from nothing
        /// else. Same distance, same pose — however much time passed.
        /// </summary>
        [Test]
        public void TheWalkCycleIsAPureFunctionOfDistance()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            CreepView creep = view.Creeps.Live.Values.First();

            creep.Pose(Vector3.zero, Quaternion.identity, 3.25f, CreepState.Walking, 0f);
            float first = creep.LastWalkTime;

            // Somewhere else entirely, then back. Elapsed time has moved on; the
            // distance has not.
            creep.Pose(Vector3.zero, Quaternion.identity, 11.9f, CreepState.Walking, 0f);
            creep.Pose(Vector3.zero, Quaternion.identity, 3.25f, CreepState.Walking, 0f);

            Assert.That(creep.LastWalkTime, Is.EqualTo(first),
                "the same distance gave two different poses, so something is accumulating");
        }

        /// <summary>
        /// Scrubbing backwards walks the legs backwards. This is the one place
        /// the whole architecture can be caught wrong by a human, and it is row
        /// four of the landmark table.
        /// </summary>
        [Test]
        public void WalkingBackwardsRunsTheClipBackwards()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            CreepView creep = view.Creeps.Live.Values.First();

            // A quarter of a cycle at a time, staying inside one cycle so the
            // comparison is not confused by the wrap.
            float quarter = MatchTuning.HexesPerWalkCycle * 0.25f;

            creep.Pose(Vector3.zero, Quaternion.identity, quarter * 3f, CreepState.Walking, 0f);
            float late = creep.LastWalkTime;

            creep.Pose(Vector3.zero, Quaternion.identity, quarter * 2f, CreepState.Walking, 0f);
            float middle = creep.LastWalkTime;

            creep.Pose(Vector3.zero, Quaternion.identity, quarter, CreepState.Walking, 0f);
            float early = creep.LastWalkTime;

            Assert.That(middle, Is.LessThan(late), "the clip did not run backwards");
            Assert.That(early, Is.LessThan(middle), "the clip did not run backwards");
        }

        /// <summary>
        /// A creep in <c>Dying</c> plays the death clip across exactly the ticks
        /// the simulation gave it, and then stops appearing. The view never
        /// owns the corpse.
        /// </summary>
        [Test]
        public void ADeathLastsExactlyAsLongAsTheSimulationSaidAndNotOneTickMore()
        {
            MatchView view = Begin();

            int dyingId = 0;
            int dyingTicks = 0;
            var seen = new List<int>();

            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            RunUntil(view, () =>
            {
                if (dyingId == 0)
                {
                    foreach (CreepSnapshot creep in view.Current.Creeps)
                    {
                        if (creep.State == CreepState.Dying && creep.TicksInState == 0)
                        {
                            dyingId = creep.Id;
                            dyingTicks = types.ById(creep.TypeId).DyingTicks;

                            break;
                        }
                    }

                    return false;
                }

                CreepSnapshot? still = null;

                foreach (CreepSnapshot creep in view.Current.Creeps)
                {
                    if (creep.Id == dyingId) still = creep;
                }

                if (still.HasValue)
                {
                    Assert.That(still.Value.State, Is.EqualTo(CreepState.Dying),
                        "a creep came back to life");

                    seen.Add(still.Value.TicksInState);

                    Assert.That(view.Creeps.Live.ContainsKey(dyingId), Is.True,
                        "a dying creep is in the snapshot and is not being drawn");

                    return false;
                }

                // It has stopped appearing. Its object must already be back in
                // the pool -- nothing told the view it died.
                Assert.That(view.Creeps.Live.ContainsKey(dyingId), Is.False,
                    "the view is still drawing a corpse the simulation has forgotten");

                return true;
            });

            Assert.That(dyingId, Is.GreaterThan(0), "no creep died in the whole match");
            Assert.That(dyingTicks, Is.GreaterThan(0));
            Assert.That(seen.Count, Is.EqualTo(dyingTicks - 1),
                $"the death was drawn for {seen.Count + 1} ticks and the simulation gave it {dyingTicks}");
        }

        /// <summary>
        /// Two creeps overtaking, and the objects drawing them never swap.
        /// </summary>
        /// <remarks>
        /// Draw order is stable because each entity's object is bound to its id
        /// for as long as that id keeps appearing, and nothing re-sorts. Two
        /// creeps swapping places in the world is therefore two transforms
        /// moving, which is what an overtake should be.
        /// </remarks>
        [Test]
        public void AnOvertakeMovesTheCreepsAndNotTheObjectsDrawingThem()
        {
            MatchView view = Begin();

            var bound = new Dictionary<int, CreepView>();
            bool sawAnOvertake = false;

            RunUntil(view, () =>
            {
                foreach (KeyValuePair<int, CreepView> live in view.Creeps.Live)
                {
                    if (bound.TryGetValue(live.Key, out CreepView was))
                    {
                        Assert.That(live.Value, Is.SameAs(was),
                            $"creep {live.Key} changed object mid-life, so draw order is not stable");
                    }
                    else
                    {
                        bound[live.Key] = live.Value;
                    }
                }

                // An overtake: a higher id further along than a lower one. Ids
                // ascend with spawn order, so this is a later creep in front of
                // an earlier one.
                List<CreepSnapshot> walking = view.Current.Creeps
                    .Where(c => c.State == CreepState.Walking)
                    .ToList();

                for (int i = 0; i < walking.Count && !sawAnOvertake; i++)
                {
                    for (int j = 0; j < walking.Count; j++)
                    {
                        if (walking[i].Id > walking[j].Id
                            && walking[i].DistanceAlongPath > walking[j].DistanceAlongPath)
                        {
                            sawAnOvertake = true;

                            break;
                        }
                    }
                }

                return false;
            });

            Assert.That(sawAnOvertake, Is.True,
                "no creep ever passed another, so unit ordering was never observable");
        }

        // ---------------------------------------------------------------
        // Towers and projectiles — the deliberate asymmetry
        // ---------------------------------------------------------------

        /// <summary>
        /// Every tower holds what its own art says it holds, and is posed with
        /// its own clips.
        /// </summary>
        /// <remarks>
        /// This used to assert the opposite and pass: towers split on
        /// <c>Delivery</c>, so the mage — the only projectile row — was the one
        /// animated and holding a weapon, and the archer and the ranger stood
        /// still with empty hands. The weapon and the clips are per unit now, so
        /// the question is no longer "which kind of tower is this" but "what did
        /// its art say", and the answer is checked against the art rather than
        /// against a rule about damage.
        /// </remarks>
        [Test]
        public void EveryTowerHoldsWhatItsArtSaysAndIsPosedWithItsOwnClips()
        {
            MatchView view = Begin();
            MatchArt art = TheMatchOnScreen.Art();

            Assert.That(view.Towers.Count, Is.EqualTo(6), "the defense has six towers");

            foreach (TowerView tower in view.Towers.Values)
            {
                UnitArt expected = art.ArtFor(tower.Type.Id);

                Assert.That(tower.IsAnimated, Is.EqualTo(expected.IsPosed),
                    tower.Type.Label + " is posed when its art carries no clips, or the other way round");

                Assert.That(
                    tower.RightHand == null, Is.EqualTo(expected.RightHand == null),
                    tower.Type.Label + "'s right hand disagrees with its art");

                Assert.That(
                    tower.LeftHand == null, Is.EqualTo(expected.LeftHand == null),
                    tower.Type.Label + "'s left hand disagrees with its art");

                Assert.That(
                    tower.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                    Is.Not.Empty,
                    tower.Type.Label + " is not a skinned character");
            }
        }

        /// <summary>
        /// The asymmetry, asserted over a whole match: a hitscan tower's shot
        /// puts nothing at all in the snapshot, and a projectile tower's puts a
        /// real entity there.
        /// </summary>
        [Test]
        public void OnlyTheProjectileTowerEverPutsAnythingInTheSnapshot()
        {
            MatchView view = Begin();

            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            var everSeen = new HashSet<int>();
            int mostShellsAtOnce = 0;

            RunUntil(view, () =>
            {
                foreach (ProjectileSnapshot shell in view.Current.Projectiles)
                {
                    everSeen.Add(shell.TypeId);
                }

                mostShellsAtOnce = Mathf.Max(mostShellsAtOnce, view.Current.Projectiles.Count);

                return false;
            });

            Assert.That(mostShellsAtOnce, Is.GreaterThan(0), "no projectile was ever fired");

            foreach (int typeId in everSeen)
            {
                Assert.That(types.ById(typeId).Delivery, Is.EqualTo(Delivery.Projectile),
                    $"unit type {typeId} is not a projectile tower and it put an entity in the snapshot");
            }
        }

        /// <summary>
        /// The hardest case in the contract: a shell whose target dies
        /// mid-flight stops being drawn, and nothing anywhere handled it.
        /// </summary>
        [Test]
        public void AShellWhoseTargetDiesMidFlightSimplyStopsBeingDrawn()
        {
            MatchView view = Begin();

            var inFlight = new Dictionary<int, ProjectileSnapshot>();
            bool sawAnOrphan = false;

            RunUntil(view, () =>
            {
                var present = new Dictionary<int, ProjectileSnapshot>();

                foreach (ProjectileSnapshot shell in view.Current.Projectiles)
                {
                    present[shell.Id] = shell;
                }

                foreach (KeyValuePair<int, ProjectileSnapshot> was in inFlight)
                {
                    if (present.ContainsKey(was.Key))
                    {
                        continue;
                    }

                    // It left the snapshot. If it had not finished its flight,
                    // it was orphaned rather than landed.
                    if (was.Value.TicksInFlight < was.Value.FlightDurationTicks - 1)
                    {
                        sawAnOrphan = true;
                    }

                    Assert.That(view.Projectiles.Live.ContainsKey(was.Key), Is.False,
                        $"shell {was.Key} left the snapshot and is still being drawn");
                }

                inFlight.Clear();

                foreach (KeyValuePair<int, ProjectileSnapshot> shell in present)
                {
                    inFlight[shell.Key] = shell.Value;
                }

                return false;
            });

            Assert.That(sawAnOrphan, Is.True,
                "no shell ever lost its target, so the case this contract exists for was never exercised");
        }

        /// <summary>
        /// A shell is drawn as a function of where its target is <i>now</i>, so
        /// a target that walks on is tracked with no homing code anywhere.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Asserted by recomputing the shell's position from the target's
        /// position in the snapshot being drawn, and requiring the two to
        /// agree exactly. That is the homing claim stated directly: if the view
        /// were flying the shell at where the target <i>was</i>, or at a
        /// remembered muzzle, the recomputation would not match.
        /// </para>
        /// <para>
        /// The shell also has to close on its target every tick. Note that the
        /// last tick a shell appears in the snapshot is the tick <i>before</i>
        /// it lands — at ten elevenths of its flight — so "nearly there" is
        /// most of a hex out, and a test that demanded it be on top of its
        /// target would be asserting against a frame that is never drawn.
        /// </para>
        /// </remarks>
        [Test]
        public void AShellIsDrawnFromWhereItsTargetIsNow()
        {
            MatchView view = Begin();

            var closing = new Dictionary<int, float>();
            var aimedAlong = new Dictionary<int, Vector3>();
            int checkedTicks = 0;
            bool watchedOneAllTheWayIn = false;

            RunUntil(view, () =>
            {
                foreach (ProjectileSnapshot shell in view.Current.Projectiles)
                {
                    if (shell.Target.Kind != TargetKind.Creep
                        || !view.Projectiles.Live.TryGetValue(shell.Id, out ProjectileView drawn))
                    {
                        continue;
                    }

                    CreepSnapshot? target = null;

                    foreach (CreepSnapshot creep in view.Current.Creeps)
                    {
                        if (creep.Id == shell.Target.Id) target = creep;
                    }

                    if (!target.HasValue)
                    {
                        continue;
                    }

                    float distanceAlong = SimUnits.ToFloat(target.Value.DistanceAlongPath);

                    Vector3 targetAt = view.Route.PointAt(
                        distanceAlong,
                        SimUnits.ToFloat(target.Value.LateralOffset));

                    Vector3 tangent = view.Route.TangentAt(distanceAlong);

                    // Recomputed from the target's position in this snapshot,
                    // and from nothing else at all.
                    Vector3 origin = ProjectileView.OriginFor(targetAt, tangent);

                    float travelled = shell.FlightDurationTicks <= 0
                        ? 1f
                        : Mathf.Clamp01(shell.TicksInFlight / (float)shell.FlightDurationTicks);

                    Vector3 expected = Vector3.Lerp(origin, targetAt, travelled);
                    expected.y += MatchTuning.ProjectileArcBulge * 4f * travelled * (1f - travelled);

                    Assert.That(Vector3.Distance(drawn.LastPosition, expected), Is.LessThan(1e-3f),
                        $"shell {shell.Id} is not being drawn from where its target is now");

                    checkedTicks++;

                    // And it closes, every tick, on a target that is walking.
                    float gap = Vector3.Distance(drawn.LastPosition, targetAt);

                    // Except across a tilt change, and that exception is the
                    // board having ramps on it. The aim line is rebuilt every
                    // tick as target + apex - tangent * lead, so when a creep
                    // steps onto a ramp the tangent pitches up, the origin
                    // swings, and the gap can widen for exactly that one tick
                    // without the shell having stopped closing on anything. The
                    // corridor tilts at three places and nowhere else, so this
                    // skips three ticks per flight at most.
                    bool tiltHeld = !aimedAlong.TryGetValue(shell.Id, out Vector3 before)
                        || Vector3.Distance(before, tangent) < 1e-4f;

                    if (tiltHeld && closing.TryGetValue(shell.Id, out float was))
                    {
                        Assert.That(gap, Is.LessThan(was),
                            $"shell {shell.Id} did not close on its target this tick");
                    }

                    closing[shell.Id] = gap;
                    aimedAlong[shell.Id] = tangent;

                    if (shell.TicksInFlight == shell.FlightDurationTicks - 1)
                    {
                        watchedOneAllTheWayIn = true;

                        // Most of the way there: the remaining eleventh of a
                        // flight that starts five and a half metres up.
                        Assert.That(gap, Is.LessThan(SimUnits.MetresPerHex),
                            "a shell one tick from landing is more than a hex from its target");
                    }
                }

                return false;
            });

            Assert.That(checkedTicks, Is.GreaterThan(50), "hardly any shell was ever watched in flight");
            Assert.That(watchedOneAllTheWayIn, Is.True, "no shell was ever watched all the way in");
        }

        /// <summary>
        /// The bow is drawn in step with actually firing: the windup clip runs
        /// across the windup and the release clip across the backswing, neither
        /// without the other.
        /// </summary>
        [Test]
        public void TheTowerPlaysItsAttackClipInStepWithFiring()
        {
            MatchView view = Begin();

            var sawState = new HashSet<TowerState>();
            bool sawAFullWindup = false;

            RunUntil(view, () =>
            {
                foreach (TowerSnapshot snapshot in view.Current.Towers)
                {
                    TowerView tower = view.Towers[snapshot.Id];

                    if (!tower.IsAnimated)
                    {
                        continue;
                    }

                    sawState.Add(snapshot.State);

                    Assert.That(tower.LastState, Is.EqualTo(snapshot.State),
                        "the tower is posed in a state the snapshot does not report");

                    int expected = snapshot.State == TowerState.Windup
                        ? TowerView.WindupSlot
                        : snapshot.State == TowerState.Backswing
                            ? TowerView.BackswingSlot
                            : TowerView.IdleSlot;

                    Assert.That(tower.LastSlot, Is.EqualTo(expected),
                        $"tower {snapshot.Id} is in {snapshot.State} and playing slot {tower.LastSlot}");

                    // The last tick of the windup is the tick the shot is
                    // released, so the draw has to be finished by then.
                    if (snapshot.State == TowerState.Windup
                        && snapshot.TicksInState == tower.Type.WindupTicks - 1)
                    {
                        sawAFullWindup = true;
                    }
                }

                return false;
            });

            Assert.That(sawState, Does.Contain(TowerState.Windup));
            Assert.That(sawState, Does.Contain(TowerState.Backswing));
            Assert.That(sawState, Does.Contain(TowerState.Idle));
            Assert.That(sawAFullWindup, Is.True, "no shot was ever watched all the way through its windup");
        }

        [Test]
        public void ATowerWithATargetTurnsToFaceIt()
        {
            MatchView view = Begin();

            bool checkedOne = false;

            RunUntil(view, () =>
            {
                foreach (TowerSnapshot snapshot in view.Current.Towers)
                {
                    if (snapshot.TargetId == 0)
                    {
                        continue;
                    }

                    TowerView tower = view.Towers[snapshot.Id];

                    if (!tower.IsAnimated
                        || !view.Creeps.Live.ContainsKey(snapshot.TargetId))
                    {
                        continue;
                    }

                    CreepView target = view.Creeps.Live[snapshot.TargetId];

                    Vector3 toward = target.transform.position - tower.transform.position;
                    toward.y = 0f;

                    if (toward.sqrMagnitude < 1e-4f)
                    {
                        continue;
                    }

                    float angle = Vector3.Angle(tower.transform.forward, toward.normalized);

                    Assert.That(angle, Is.LessThan(1f),
                        $"tower {snapshot.Id} is {angle:F1} degrees off its target");

                    checkedOne = true;
                }

                return false;
            });

            Assert.That(checkedOne, Is.True, "no tower ever acquired a target");
        }

        // ---------------------------------------------------------------
        // Still true afterwards
        // ---------------------------------------------------------------

        /// <summary>
        /// Nothing in the match turns to face the camera, so orbiting to look
        /// at a thing from another side is a real check rather than a formality.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The rule is a consequence of the camera and not a taste.</b>
        /// Issue #3 answered the playfield and the camera separately and made
        /// the camera a free orbit, which the docs of the day recorded as
        /// making "no billboards, no flat cards, no painted-on shadows" a
        /// mandatory art rule — <c>docs/vision.md</c> §6 still carries it. A
        /// billboard is a card that rotates to keep facing the viewer, so an
        /// orbiting player either watches it spin or, if it is pinned, watches
        /// it vanish edge-on. Neither survives.
        /// </para>
        /// <para>
        /// <b>What is banned is billboarding, not any particular component,
        /// and this test used to confuse the two.</b> Until 7 Sep 2026 it
        /// asserted no <c>ParticleSystem</c> existed at all, which is stricter
        /// than the rule: a particle system whose renderer is in
        /// <see cref="ParticleSystemRenderMode.Mesh"/> emits real geometry per
        /// particle and faces nothing. That ban was reading out of the vision a
        /// sentence the vision does not contain, and it stood between the
        /// effects work and the only tool built for it. Particles are allowed
        /// here now **on the condition that they do not billboard**, which is
        /// the actual rule, checked.
        /// </para>
        /// <para>
        /// Checked by type and by render mode rather than by looking, because
        /// the components that billboard do so silently and by default: a line
        /// renderer faces the camera unless told otherwise, a particle system
        /// defaults to <see cref="ParticleSystemRenderMode.Billboard"/>, and a
        /// sprite is a flat card by definition. <c>CameraRigTests</c> holds the
        /// behavioural half — orbit the rig and assert nothing moved — which is
        /// what catches a billboard this list has not thought of.
        /// </para>
        /// </remarks>
        [Test]
        public void NothingInTheMatchTurnsToFaceTheCamera()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Current.Tick > 400);

            AssertNothingBillboards(view);
        }

        /// <summary>
        /// The rule above, over whatever is under <paramref name="root"/>, so
        /// that the test which proves it still bites can point it at a match it
        /// has deliberately spoiled.
        /// </summary>
        private static void AssertNothingBillboards(Component root)
        {
            Assert.That(root.GetComponentsInChildren<LineRenderer>(true), Is.Empty,
                "a line renderer billboards to the camera unless told not to");
            Assert.That(root.GetComponentsInChildren<TrailRenderer>(true), Is.Empty);
            Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty,
                "a sprite is a flat card");
            Assert.That(root.GetComponentsInChildren<Canvas>(true), Is.Empty);

            foreach (ParticleSystemRenderer particles
                in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Assert.That(
                    particles.renderMode,
                    Is.EqualTo(ParticleSystemRenderMode.Mesh),
                    $"{particles.name} draws its particles as {particles.renderMode}, and every mode "
                    + "but Mesh is a camera-facing card. Give it a mesh, or draw the effect some "
                    + "other way.");

                Assert.That(
                    particles.alignment,
                    Is.Not.EqualTo(ParticleSystemRenderSpace.View)
                        .And.Not.EqualTo(ParticleSystemRenderSpace.Facing),
                    $"{particles.name} draws real meshes and then turns them to face the camera, "
                    + "which is a billboard with extra steps");
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(
                    renderer is MeshRenderer
                        || renderer is SkinnedMeshRenderer
                        || renderer is ParticleSystemRenderer,
                    Is.True,
                    $"{renderer.name} is a {renderer.GetType().Name}, which is not real geometry");
            }
        }

        /// <summary>
        /// A particle system drawing camera-facing cards is refused by name,
        /// which is the half of the rule above that nothing in the match
        /// currently exercises.
        /// </summary>
        /// <remarks>
        /// <b>Without this the relaxation is untested.</b> The match ships no
        /// particle system at all, so every assertion in
        /// <see cref="NothingInTheMatchTurnsToFaceTheCamera"/> about render
        /// modes passes over an empty list — which is exactly how a guard that
        /// has stopped guarding looks. This stands one up, points it at the
        /// camera, and checks the rule bites.
        /// </remarks>
        [Test]
        public void ABillboardingParticleSystemIsStillRefused()
        {
            MatchView view = Begin();

            var host = new GameObject("Sparks");
            host.transform.SetParent(view.transform, worldPositionStays: false);

            ParticleSystemRenderer particles = host.AddComponent<ParticleSystem>()
                .GetComponent<ParticleSystemRenderer>();

            try
            {
                particles.renderMode = ParticleSystemRenderMode.Billboard;

                Assert.That(
                    () => AssertNothingBillboards(view),
                    Throws.InstanceOf<AssertionException>(),
                    "a particle system drawing camera-facing cards was allowed into the match");

                // And the mesh mode the relaxation exists for is accepted, so
                // this test says where the line is rather than only that there
                // is one.
                particles.renderMode = ParticleSystemRenderMode.Mesh;
                particles.alignment = ParticleSystemRenderSpace.World;

                Assert.That(
                    () => AssertNothingBillboards(view),
                    Throws.Nothing,
                    "a particle system drawing real meshes in world space was refused, which is the "
                    + "one thing the rule is supposed to permit");

                // Real meshes turned to face the camera are a billboard with
                // extra steps, and are refused on the second assertion rather
                // than the first.
                particles.alignment = ParticleSystemRenderSpace.Facing;

                Assert.That(
                    () => AssertNothingBillboards(view),
                    Throws.InstanceOf<AssertionException>(),
                    "meshes aligned to face the camera were allowed");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        /// <summary>
        /// No animator anywhere carries a controller. A state-machine animator
        /// is a playback head that advances in wall-clock time, which is the
        /// view-side accumulator the whole architecture forbids.
        /// </summary>
        [Test]
        public void NoAnimatorOwnsAPlaybackHead()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Creeps.LiveCount > 2);

            Animator[] animators = view.GetComponentsInChildren<Animator>(true);

            Assert.That(animators, Is.Not.Empty, "nothing is animated at all");

            foreach (Animator animator in animators)
            {
                Assert.That(animator.runtimeAnimatorController, Is.Null,
                    $"{animator.name} carries a controller, which is a playback head that can disagree "
                    + "with the simulation");

                Assert.That(animator.applyRootMotion, Is.False,
                    $"{animator.name} applies root motion, so something other than distance travelled "
                    + "is moving it");
            }
        }

        // ---------------------------------------------------------------
        // Events
        // ---------------------------------------------------------------

        /// <summary>
        /// Deleting every effect mid-match changes nothing about where anything
        /// is. That is what "events drive only decoration" means, and it is
        /// checkable rather than a claim.
        /// </summary>
        [Test]
        public void ClearingEveryEffectChangesNothingAboutTheMatch()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Current.Tick > 500 && view.Creeps.LiveCount > 1);

            var before = view.Creeps.Live.ToDictionary(e => e.Key, e => e.Value.transform.position);

            view.ClearDecorations();
            view.Draw(1f);

            foreach (KeyValuePair<int, Vector3> was in before)
            {
                Assert.That(view.Creeps.Live[was.Key].transform.position, Is.EqualTo(was.Value),
                    $"creep {was.Key} moved when the decorations were cleared, so something decorative "
                    + "is load-bearing");
            }
        }

        [Test]
        public void EffectsAreDrawnAndThenForgotten()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Decorations.TracersDrawn > 0);

            Assert.That(view.Decorations.TracersDrawn, Is.GreaterThan(0),
                "the hitscan towers fired and drew no tracer");
            Assert.That(view.Decorations.ActiveCount, Is.GreaterThan(0));

            // Effects age on the tick, so ticking is what retires them. Nothing
            // here touches a wall clock.
            int longest = Mathf.Max(
                MatchTuning.TracerTicks,
                Mathf.Max(MatchTuning.MuzzleFlashTicks, MatchTuning.HitSparkTicks));

            for (int tick = 0; tick <= longest; tick++)
            {
                view.Decorations.AgeOneTick();
            }

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "an effect outlived its lifetime, so something is holding on to it");
        }

        /// <summary>
        /// A bubble whose row names no signature of its own leaves the plain
        /// disc on the ground under whatever it was centred on, as wide as the
        /// bubble reached — and a blast that arrived on a body bursts on the
        /// body instead.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The events are handed over by hand, and they have to be.</b> No
        /// row of <c>content/units.txt</c> authors a bubble, so a match played
        /// from the shipped content never fires one — the same reason the
        /// simulation's own bubble tests are written against fixture rows. What
        /// is under test here is the decoration, and the decoration is reached
        /// through the interface the simulation would reach it through.
        /// </para>
        /// <para>
        /// <b>The position is asserted against the tower, because a tower's is
        /// exact.</b> A creep's ring lands on the position the creep was last
        /// drawn at, which is a lerp and would have to be re-derived here to be
        /// compared; that half is asserted as "under the creep and not under
        /// the tower", which is the claim that would break if the two lookups
        /// were crossed.
        /// </para>
        /// </remarks>
        [Test]
        public void ABubbleLeavesARingUnderWhatItWasCentredOnAtTheSizeItReached()
        {
            const int RadiusMilliHex = 3000;

            MatchView view = Begin();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            int towerId = view.Current.Towers[0].Id;
            Vector3 stands = view.Towers[towerId].transform.position;

            view.Decorations.AuraPulsed(towerId, RadiusMilliHex, BubblePayload.Cooldown);

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(1),
                "an aura pulsed and left nothing on the board");

            Transform ring = Rings(view).Single();

            Assert.That(
                Vector3.Distance(ring.position, stands + (Vector3.up * MatchTuning.FloorClearance)),
                Is.LessThan(1e-3f),
                $"the ring is at {ring.position} and the tower that pulsed is at {stands}");

            // Sized to the radius, in the one place a simulation number becomes
            // a view number. A ring that ignored the radius would look right on
            // every bubble that happened to be three hexes wide.
            Assert.That(ring.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(RadiusMilliHex)).Within(1e-4f));
            Assert.That(ring.localScale.z, Is.EqualTo(ring.localScale.x));

            // And it keeps that size for the whole of its life, which is the
            // one place it parts company with a spark. A spark's size is how
            // loud it is and closing it down is it going away; a ring's size is
            // the entire message, so a ring that shrank would report a reach
            // the bubble did not have on every tick but its first.
            //
            // OBSERVED: age every effect alike. This goes red on the first
            // tick, at seven eighths of the width it was drawn at, and nothing
            // else in the suite notices — the ring is still drawn, still
            // pooled, still cleared by a seek, and it is telling the truth for
            // one frame in eight.
            float drawnAt = ring.localScale.x;

            for (int tick = 1; tick < MatchTuning.BubbleRingTicks; tick++)
            {
                view.Decorations.AgeOneTick();

                Assert.That(ring.localScale.x, Is.EqualTo(drawnAt).Within(1e-4f),
                    $"the ring is {ring.localScale.x} metres across {tick} ticks after a bubble "
                    + $"{drawnAt} metres across went off");
            }

            // And a blast that arrived on a body is a burst on the body rather
            // than this disc. The event names the creep the shot reached and
            // never the tower that fired it, so there is no row to read a look
            // off — see MatchDecorations.BlastLanded.
            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            view.Decorations.BlastLanded(creepId, RadiusMilliHex, BubblePayload.Damage);

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(1),
                "a blast that arrived on a body drew the disc a self-centred bubble draws");

            Assert.That(view.Decorations.BurstsDrawn, Is.EqualTo(1),
                "a blast arrived on a body and nothing burst on it");

            float nearest = Pieces(view, "MortarBurst")
                .Min(shards => Vector3.Distance(
                    shards.position, walks + (Vector3.up * MatchTuning.HitSparkHeight)));

            Assert.That(nearest, Is.LessThan(1e-3f),
                "nothing burst on the creep the blast landed on");

            // A bubble that reached only its centre is a shape of no size, and a
            // centre the view is not holding has nowhere to be. Neither draws,
            // and neither is an error.
            view.Decorations.BlastLanded(creepId, 0, BubblePayload.Speed);
            view.Decorations.AuraPulsed(int.MaxValue, RadiusMilliHex, BubblePayload.Shield);

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(1),
                "a bubble with no radius, or centred on nothing the view is holding, drew a ring anyway");

            Assert.That(view.Decorations.BurstsDrawn, Is.EqualTo(1),
                "a bubble with no radius burst anyway");
        }

        /// <summary>
        /// The defenses and the wave that between them put every row a
        /// decoration draws on a real board.
        /// </summary>
        /// <remarks>
        /// Several rather than one because the twenty-seven tower rows cannot
        /// stand beside the corridor together, and a row that could not be
        /// placed would be a row these tests quietly stopped covering.
        /// </remarks>
        public enum Lines
        {
            Four,
            Pierce,
            Magic,
            CreepAuras,
        }

        /// <summary>What a signature is drawn under or on.</summary>
        public enum Centre
        {
            /// <summary>The tower of that row, which emitted it.</summary>
            Tower,

            /// <summary>The body the row's shot arrived at; the event names no shooter.</summary>
            BodyHit,

            /// <summary>A walking body of that row, which emitted it.</summary>
            Walker,
        }

        private MatchView BeginWith(Lines lines) => lines switch
        {
            Lines.Four => BeginWithTheFourLines(),
            Lines.Pierce => BeginWithThePierceLines(),
            Lines.Magic => BeginWithTheMagicLines(),
            _ => BeginWithTheCreepAuras(),
        };

        /// <summary>
        /// Every tower row flashes at the point on its own art its shot leaves
        /// from and sparks on the body its shot lands on — and a hitscan magic
        /// row puts a bolt in the air out of that same point instead of the
        /// tracer every other row shares.
        /// </summary>
        /// <remarks>
        /// <b>The decorations are cleared between rows so that each assertion
        /// is about one flash.</b> Many flashes drawn together would have to be
        /// matched back to their towers by position, which is the thing under
        /// test.
        /// </remarks>
        [TestCase(Lines.Four, 12)]
        [TestCase(Lines.Pierce, 6)]
        [TestCase(Lines.Magic, 9)]
        public void EveryTowerRowFiresFromItsAnchorAndSparksOnWhatItHits(Lines lines, int rows)
        {
            MatchView view = BeginWith(lines);

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            foreach (TowerView tower in view.Towers.Values)
            {
                string row = $"unit {tower.Type.Id} ({tower.Type.Label})";

                view.Decorations.Clear();
                view.Decorations.TowerFired(tower.Id, creepId);

                Assert.That(view.Decorations.FlashesDrawn, Is.EqualTo(1), row + " fired and nothing left it");

                Assert.That(tower.AnchorTransform, Is.Not.Null,
                    row + " has no anchor, so its flash is at a height above its own root whatever it is holding");

                Transform flash = Pieces(view, "MuzzleFlash").Single();

                Assert.That(Vector3.Distance(flash.position, tower.Muzzle), Is.LessThan(1e-3f),
                    $"{row} fires from {tower.Muzzle} and its flash is at {flash.position}");

                if (lines == Lines.Magic)
                {
                    bool hitscan = tower.Type.Delivery == Delivery.Hitscan;

                    Assert.That(view.Decorations.BoltsDrawn, Is.EqualTo(hitscan ? 1 : 0),
                        $"{row} is {tower.Type.Delivery} and drew {view.Decorations.BoltsDrawn} bolts");

                    Assert.That(view.Decorations.TracersDrawn, Is.EqualTo(hitscan ? 0 : 1),
                        $"{row} is {tower.Type.Delivery} and drew {view.Decorations.TracersDrawn} of the "
                        + "tracer every row shares");

                    if (hitscan)
                    {
                        Assert.That(
                            Vector3.Distance(Pieces(view, "MagicBolt").Single().position, tower.Muzzle),
                            Is.LessThan(1e-3f),
                            row + "'s bolt does not start at its tome or staff tip");
                    }
                }

                view.Decorations.CreepDamaged(creepId, 10);

                Assert.That(view.Decorations.SparksDrawn, Is.EqualTo(1),
                    row + " landed damage and nothing landed with it");

                Assert.That(
                    Vector3.Distance(
                        Pieces(view, "Spark").Single().position,
                        walks + (Vector3.up * MatchTuning.HitSparkHeight)),
                    Is.LessThan(1e-3f),
                    "the landing is not on the creep the shot landed on");
            }

            Assert.That(view.Towers.Count, Is.EqualTo(rows), "a row of this defense is not standing");
        }

        /// <summary>
        /// Every row that authors a bubble draws the shape its own row names
        /// rather than the plain disc, under or on what it was centred on, as
        /// wide as the bubble reached — and keeps that size for the whole of its
        /// life, because the size is the message.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The events are handed over by hand.</b> What is under test is the
        /// decoration and the binding behind it, reached through the interface
        /// the simulation reaches it through; <see cref="ASeekDrawsNoEffectASecondTime"/>
        /// is the one that waits for real pulses. <b>Each radius is read off the
        /// row that authors it</b>, so this measures the view against whatever
        /// <c>content/units.txt</c> says.
        /// </para>
        /// <para>
        /// <b>The Overgrowth draws nothing, on purpose.</b> Its reach is sixty
        /// hexes, so a circle would wash the whole board flat — see
        /// docs/decision-log.md. Its row passes no piece name.
        /// </para>
        /// </remarks>
        [TestCaseSource(nameof(Signatures))]
        public void EachBubbleRowDrawsItsOwnSignatureAtTheSizeItReached(
            string label, Lines lines, int unitId, Centre centre, bool aura, BubblePayload payload, string piece)
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            MatchView view = BeginWith(lines);

            RunUntil(view, () => centre == Centre.Walker
                ? Walkers(view, unitId).Any(walker => ReachesSomething(view, types, unitId, walker))
                : view.Creeps.LiveCount > 0);

            int centreId;
            int radius;

            switch (centre)
            {
                case Centre.Tower:
                    TowerView tower = Standing(view, unitId);
                    centreId = tower.Id;
                    radius = Reaches(tower);
                    break;
                case Centre.BodyHit:
                    centreId = view.Current.Creeps[0].Id;
                    radius = Reaches(Standing(view, unitId));
                    break;
                default:
                    centreId = Walkers(view, unitId).First(walker => ReachesSomething(view, types, unitId, walker));
                    radius = Pulses(types, unitId);
                    break;
            }

            Vector3 at = centre == Centre.Tower
                ? view.Towers[centreId].transform.position
                : view.Creeps.Live[centreId].transform.position;

            view.Decorations.Clear();
            int heard = view.Decorations.EventsHeard;

            if (aura)
            {
                view.Decorations.AuraPulsed(centreId, radius, payload);
            }
            else
            {
                view.Decorations.BlastLanded(centreId, radius, payload);
            }

            Assert.That(view.Decorations.EventsHeard, Is.GreaterThan(heard),
                "the event never arrived, so whatever was drawn proves nothing");

            if (piece == null)
            {
                Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0), label + " pulsed and drew something");

                return;
            }

            Transform shape = Pieces(view, piece).Single();
            float height = piece == "MortarBurst" ? MatchTuning.HitSparkHeight : MatchTuning.FloorClearance;

            Assert.That(Vector3.Distance(shape.position, at + (Vector3.up * height)), Is.LessThan(1e-3f),
                $"{label}'s {piece} is at {shape.position} and what it was centred on is at {at}");

            Assert.That(shape.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(radius)).Within(1e-4f),
                $"{label}'s {piece} is not as wide as the bubble reached");

            float reach = SimUnits.MetresFromMilliHex(radius);
            Vector3[] covered = Covered(view, unitId, at, reach);

            if (covered != null)
            {
                Assert.That(covered, Is.Not.Empty, label + " reached nothing, so this proves nothing");

                foreach (Vector3 inside in covered)
                {
                    Assert.That(Flat(inside - at), Is.LessThan(reach + 1e-3f),
                        $"something {label} reached is outside the circle drawn for it");
                }
            }

            if (unitId == Blessing)
            {
                Assert.That(Flat(Standing(view, Paladin).transform.position - at), Is.GreaterThan(reach),
                    "the Paladin is six hexes away and the Blessing's circle covers it anyway");
            }

            if (piece == "WardDome")
            {
                Assert.That(shape.localScale.y, Is.LessThan(shape.localScale.x),
                    "the ward is drawn standing up, and every aura lies flat on the floor now");
            }

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(0),
                label + " fell back to the plain disc, so its binding was not read");

            // A shape that stands for a radius may not report a smaller one as
            // it ages — the bug #253 found in the ring.
            Vector3 drawnAt = shape.localScale;

            for (var tick = 1; tick < 120 && shape.gameObject.activeSelf; tick++)
            {
                view.Decorations.AgeOneTick();

                if (shape.gameObject.activeSelf)
                {
                    Assert.That(shape.localScale, Is.EqualTo(drawnAt),
                        $"{label}'s {piece} is {shape.localScale.x} metres across {tick} ticks after a "
                        + $"bubble {drawnAt.x} metres across went off");
                }
            }
        }

        private static IEnumerable<TestCaseData> Signatures()
        {
            yield return new TestCaseData("Shield Wall", Lines.Four, 16, Centre.Tower, true, BubblePayload.Speed, "SlowRing");
            yield return new TestCaseData("Slam", Lines.Four, 19, Centre.Tower, false, BubblePayload.Damage, "GroundShock");
            yield return new TestCaseData("Blessing", Lines.Four, 22, Centre.Tower, true, BubblePayload.Cooldown, "TowerGlow");
            yield return new TestCaseData("Mortar", Lines.Four, 37, Centre.BodyHit, false, BubblePayload.Damage, "MortarBurst");
            yield return new TestCaseData("Mage splash", Lines.Magic, 4, Centre.BodyHit, false, BubblePayload.Damage, "MortarBurst");
            yield return new TestCaseData("Consecration", Lines.Magic, 25, Centre.Tower, true, BubblePayload.Armour, "ConsecrationLight");
            yield return new TestCaseData("Unravel", Lines.Magic, 27, Centre.BodyHit, false, BubblePayload.Armour, "ArmourStrip");
            yield return new TestCaseData("Overgrowth", Lines.Magic, 30, Centre.Tower, true, BubblePayload.Speed, null);
            yield return new TestCaseData("Skeleton Mage", Lines.CreepAuras, SkeletonMage, Centre.Walker, true, BubblePayload.Speed, "HasteRing");
            yield return new TestCaseData("Necromancer", Lines.CreepAuras, Necromancer, Centre.Walker, true, BubblePayload.Shield, "WardDome");
            yield return new TestCaseData("Witch", Lines.CreepAuras, Witch, Centre.Walker, true, BubblePayload.Armour, "HexPlates");
            yield return new TestCaseData("Frost Wight", Lines.CreepAuras, FrostWight, Centre.Walker, true, BubblePayload.Cooldown, "FrostSpikes");
        }

        private const int Paladin = 20;
        private const int Templar = 21;
        private const int Blessing = 22;

        /// <summary>
        /// What a row's circle has to be seen to cover: the Templar two hexes
        /// from the Blessing, every body the haste reached, every tower the
        /// frostbite reached; null for a row that makes no such claim.
        /// </summary>
        private static Vector3[] Covered(MatchView view, int unitId, Vector3 at, float reach) => unitId switch
        {
            Blessing => new[] { Standing(view, Templar).transform.position },
            SkeletonMage => Within(view.Creeps.Live.Values.Select(body => body.transform.position), at, reach),
            FrostWight => Within(view.Towers.Values.Select(tower => tower.transform.position), at, reach),
            _ => null,
        };

        /// <summary>
        /// Whether this walker's aura would reach anything it has to be seen to
        /// cover — the Frost Wight needs a tower beside it, and nothing else waits.
        /// </summary>
        private static bool ReachesSomething(MatchView view, UnitTypeTable types, int unitId, int walker) =>
            unitId != FrostWight
            || Covered(
                view,
                unitId,
                view.Creeps.Live[walker].transform.position,
                SimUnits.MetresFromMilliHex(Pulses(types, unitId))).Length > 0;

        private static Vector3[] Within(IEnumerable<Vector3> places, Vector3 centre, float metres) =>
            places.Where(place => (place - centre).sqrMagnitude <= metres * metres).ToArray();

        /// <summary>
        /// The distance between two places on the floor, height ignored, because
        /// every aura's circle lies on the floor and knows nothing about how
        /// tall the ground is.
        /// </summary>
        private static float Flat(Vector3 between) =>
            new Vector2(between.x, between.z).magnitude;

        /// <summary>
        /// A seek re-runs the ticks it passes over in silence, so nothing it
        /// passes over is drawn a second time — on every defense whose rows
        /// draw a signature, and waiting for the simulation to fire each one
        /// for real, so it is also what says those rows fire at all.
        /// </summary>
        /// <remarks>
        /// The counter that has to answer for it is
        /// <see cref="MatchDecorations.EventsHeard"/>, which a clear does not
        /// reset: a seek that heard the re-run ticks' events and then tidied up
        /// after itself would be indistinguishable from one that never heard
        /// them, from every counter that does.
        /// </remarks>
        [TestCase(Lines.Four)]
        [TestCase(Lines.Pierce)]
        [TestCase(Lines.Magic)]
        [TestCase(Lines.CreepAuras)]
        public void ASeekDrawsNoEffectASecondTime(Lines lines)
        {
            MatchView view = BeginWith(lines);
            System.Func<MatchView, int>[] signatures = SignaturesDrawn(lines);

            RunUntil(view, () => signatures.All(drawn => drawn(view) > 0));

            Assert.That(signatures.Select(drawn => drawn(view)), Has.All.GreaterThan(0),
                "a signature this defense draws never went off in the whole match, so this proves nothing");

            int tick = view.Current.Tick;
            int heard = view.Decorations.EventsHeard;

            view.ReSimulateTo(tick + 90);
            view.ReSimulateTo(tick);

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heard),
                "a seek heard the events of the ticks it re-ran, so every effect between here and the "
                + "start was drawn again");

            Assert.That(signatures.Select(drawn => drawn(view)), Has.All.EqualTo(0));

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "an effect from before the seek is still on screen, and the tick it belongs to is now "
                + "in the future");
        }

        /// <summary>The counters of the signatures each defense's rows draw.</summary>
        private static System.Func<MatchView, int>[] SignaturesDrawn(Lines lines) => lines switch
        {
            Lines.Four => new System.Func<MatchView, int>[]
            {
                view => view.Decorations.SlowRingsDrawn,
                view => view.Decorations.GlowsDrawn,
            },
            Lines.Pierce => new System.Func<MatchView, int>[]
            {
                view => view.Decorations.LongShotsDrawn,
                view => view.Decorations.KnivesDrawn,
            },
            Lines.Magic => new System.Func<MatchView, int>[]
            {
                view => view.Decorations.BoltsDrawn,
                view => view.Decorations.LightsDrawn,
            },
            _ => new System.Func<MatchView, int>[]
            {
                view => view.Decorations.HasteRingsDrawn,
                view => view.Decorations.WardDomesDrawn,
                view => view.Decorations.HexPlatesDrawn,
                view => view.Decorations.FrostSpikesDrawn,
            },
        };

        /// <summary>
        /// The Overwatch's shot is one bar the whole length of the leg it
        /// crossed and holds that length; the Fan of Knives' throw is three
        /// knives leaving its hand and arriving on three bodies. Every other
        /// row on those two lines draws the tracer they share.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Neither of these capstones authors a bubble, so neither is
        /// reached the way the four before them were.</b> The Shield Wall, the
        /// Slam and the Blessing are found through a blast or an aura naming
        /// the row; <c>content/units.txt</c> gives rows 31 and 34 no bubble at
        /// all — the Overwatch is a long single shot and the Fan of Knives is a
        /// <c>targets</c> of three — so what names the row here is the shot,
        /// which carries the tower that fired it.
        /// </para>
        /// <para>
        /// <b>Three shots and not one.</b> A <c>targets</c> of three fires
        /// three shots at three bodies with three rolls, which is three
        /// <c>TowerFired</c> events on one tick, so three knives is what the
        /// event stream produces rather than something this decoration has to
        /// know to fan out.
        /// </para>
        /// <para>
        /// OBSERVED: let the long shot age like a tracer. Every assertion above
        /// the last block stays green and a bar standing for eight hexes
        /// reports a shorter leg on every tick but its first — the bug #253
        /// found in the ring, in the one shape here whose length is a distance.
        /// </para>
        /// </remarks>
        [Test]
        public void EachPierceCapstoneDrawsItsOwnShot()
        {
            MatchView view = BeginWithThePierceLines();

            RunUntil(view, () => view.Creeps.LiveCount >= 3);

            int[] bodies = view.Current.Creeps.Take(3).Select(creep => creep.Id).ToArray();

            Assert.That(bodies.Length, Is.EqualTo(3),
                "fewer than three bodies are walking, so the throw has nothing to fan out over");

            Vector3[] at = bodies
                .Select(id => view.Creeps.Live[id].transform.position
                    + (Vector3.up * MatchTuning.HitSparkHeight))
                .ToArray();

            // The Archer, which is the bottom of one of these two lines and
            // carries no signature: the tracer every hitscan row has always
            // drawn, and neither capstone's shape.
            view.Decorations.Clear();
            view.Decorations.TowerFired(Standing(view, 3).Id, bodies[0]);

            Assert.That(view.Decorations.TracersDrawn, Is.EqualTo(1),
                "the Archer fired and drew no tracer");

            Assert.That(view.Decorations.LongShotsDrawn + view.Decorations.KnivesDrawn, Is.EqualTo(0),
                "a rung below the capstone is drawing the capstone's shape, so the binding is on the "
                + "line rather than on the row");

            // The Overwatch: one bar from the crossbow to the body.
            view.Decorations.Clear();

            TowerView overwatch = Standing(view, 31);

            view.Decorations.TowerFired(overwatch.Id, bodies[0]);

            Assert.That(view.Decorations.LongShotsDrawn, Is.EqualTo(1),
                "the Overwatch fired and drew no shot of its own");

            Assert.That(view.Decorations.TracersDrawn, Is.EqualTo(0),
                "the Overwatch fell back to the shared tracer, so its binding was not read");

            Transform shot = Pieces(view, "LongShot").Single();
            float leg = Vector3.Distance(overwatch.Muzzle, at[0]);

            Assert.That(
                Vector3.Distance(shot.position, (overwatch.Muzzle + at[0]) * 0.5f),
                Is.LessThan(1e-3f),
                "the shot is not drawn between the crossbow and the body it was aimed at");

            Assert.That(shot.localScale.z, Is.EqualTo(leg).Within(1e-3f),
                $"the shot crossed {leg} metres and is {shot.localScale.z} long");

            Assert.That(shot.localScale.x, Is.EqualTo(MatchTuning.LongShotThickness).Within(1e-4f));

            Assert.That(
                shot.localScale.x, Is.GreaterThan(MatchTuning.TracerThickness),
                "the Overwatch's shot is no heavier than the tracer every other row draws, so nothing "
                + "on the board tells them apart");

            // The Fan of Knives: one throw, three shots, three knives.
            view.Decorations.Clear();

            TowerView fanOfKnives = Standing(view, 34);

            foreach (int body in bodies)
            {
                view.Decorations.TowerFired(fanOfKnives.Id, body);
            }

            Assert.That(view.Decorations.KnivesDrawn, Is.EqualTo(3),
                "three shots left the Fan of Knives and three knives did not");

            Assert.That(view.Decorations.TracersDrawn, Is.EqualTo(0),
                "the Fan of Knives fell back to the shared tracer, so its binding was not read");

            Transform[] knives = Pieces(view, "ThrownKnife").ToArray();

            Assert.That(knives.Length, Is.EqualTo(3));

            foreach (Transform knife in knives)
            {
                Assert.That(Vector3.Distance(knife.position, fanOfKnives.Muzzle), Is.LessThan(1e-3f),
                    "a knife did not leave the hand it was thrown from");
            }

            // And it crosses to the body over its life rather than being a
            // line drawn between the two.
            for (var tick = 1; tick < MatchTuning.KnifeFlightTicks; tick++)
            {
                view.Decorations.AgeOneTick();
            }

            foreach (Vector3 body in at)
            {
                Assert.That(
                    knives.Min(knife => Vector3.Distance(knife.position, body)),
                    Is.LessThan(1e-3f),
                    $"no knife arrived at the body at {body}");
            }

            // The long shot held its length for the whole of its life. A bar
            // that says how far a shot went may not say a shorter distance
            // every tick, which is the ring's own bug in another shape.
            view.Decorations.Clear();
            view.Decorations.TowerFired(overwatch.Id, bodies[0]);

            shot = Pieces(view, "LongShot").Single();
            Vector3 drawnAt = shot.localScale;

            for (var tick = 1; tick < MatchTuning.LongShotTicks; tick++)
            {
                view.Decorations.AgeOneTick();

                Assert.That(shot.localScale, Is.EqualTo(drawnAt),
                    $"the shot is {shot.localScale.z} metres long {tick} ticks after crossing "
                    + $"{drawnAt.z} metres");
            }
        }

        /// <summary>
        /// Decoration does not pile up over a whole match. The count on screen
        /// stays bounded by what the last few ticks produced, because aging
        /// happens where the simulation advances rather than where somebody
        /// remembered to call it.
        /// </summary>
        /// <remarks>
        /// This is the regression guard for a real bug: with wall-clock
        /// lifetimes, a view stepped a tick at a time never aged anything, and
        /// a capture of tick 700 came out with every tracer the match had ever
        /// fired still drawn across the playfield.
        /// </remarks>
        [Test]
        public void DecorationDoesNotAccumulateOverAMatch()
        {
            MatchView view = Begin();
            int most = 0;

            RunUntil(view, () =>
            {
                most = Mathf.Max(most, view.Decorations.ActiveCount);

                return false;
            });

            Assert.That(view.Decorations.TracersDrawn, Is.GreaterThan(20),
                "hardly anything was ever drawn, so this proves little");

            Assert.That(most, Is.LessThan(40),
                $"{most} effects were on screen at once — decoration is accumulating rather than ageing");
        }

        // ---------------------------------------------------------------
        // What is on a unit
        // ---------------------------------------------------------------

        /// <summary>
        /// A creep something has landed on wears it: a bar while a pool stands
        /// in front of its health.
        /// </summary>
        /// <remarks>
        /// <b>A modifier in force is drawn nowhere on the body.</b> A wash of
        /// colour used to say so and Sam took it off on 7 Sep 2026 — what an
        /// aura is doing is read off the translucent circle it lays on the
        /// floor. See <c>docs/decision-log.md</c>.
        /// </remarks>
        /// <remarks>
        /// <b>Played against a fixture roster, and it has to be.</b> Every row
        /// of <c>content/units.txt</c> authors no bubble at all, so a match of
        /// the shipped content never slows or shields anything — the same
        /// reason the simulation's own effect tests are written against fixture
        /// rows, and the reason the frame capture takes a table of its own.
        /// The board, the tower cell and the wave shape are real; the two rows
        /// standing on them are stand-ins and mean nothing outside this file.
        /// </remarks>
        [Test]
        public void ACreepWearsWhatTheSnapshotSaysIsOnIt()
        {
            MatchView view = BeginWithEffects();

            RunUntil(view, () => view.Current.Creeps.Any(creep => creep.SpeedMagnitude != 0));

            CreepSnapshot slowed = view.Current.Creeps.First(creep => creep.SpeedMagnitude != 0);

            Assert.That(slowed.SpeedMagnitude, Is.Negative, "the fixture archer's bubble is a slow");

            // And the bar is the pool, as a share of the health pool its row
            // authored. Asserted against the number in the snapshot rather than
            // against a width somebody wrote down, so a bar drawn at a fixed
            // size would go red here and nowhere else.
            CreepSnapshot shielded = view.Current.Creeps.First(creep => creep.Shield > 0);
            EffectMarks marks = view.Creeps.Live[shielded.Id].Marks;
            float pool = shielded.Shield / (float)FixtureMaxHp;

            Assert.That(marks.Bar.gameObject.activeSelf, Is.True, "a creep with a pool wears no bar");
            Assert.That(
                marks.ShieldSegment.localScale.x,
                Is.EqualTo(MatchTuning.UnitBarLength * pool).Within(1e-4f),
                $"{shielded.Shield} of a {FixtureMaxHp} pool is drawn {marks.ShieldSegment.localScale.x} "
                + "metres wide");

            // And it is a SECOND segment: the health one is the same share of
            // the same pool, and the shield starts where it ends. Both are
            // shares of the authored health, so the two together run past a
            // whole bar on a creep that has not been hurt yet — which is
            // deliberate, and one of the things the placeholder is for.
            float health = shielded.Hp / (float)FixtureMaxHp;

            Assert.That(
                marks.HealthSegment.localScale.x,
                Is.EqualTo(MatchTuning.UnitBarLength * health).Within(1e-4f));

            Assert.That(
                marks.ShieldSegment.localPosition.x - (marks.ShieldSegment.localScale.x / 2f),
                Is.EqualTo(marks.HealthSegment.localPosition.x + (marks.HealthSegment.localScale.x / 2f))
                    .Within(1e-4f),
                "the pool segment does not start where the health segment ends");

            // The bar lies along the world axis whatever the creep is facing,
            // because the body turns to follow the corridor and a bar that
            // swung with it would be reporting the route rather than the pool.
            Assert.That(Quaternion.Angle(marks.Bar.rotation, Quaternion.identity), Is.LessThan(1e-3f));
        }

        /// <summary>
        /// The marks are still right after a seek, which is the whole reason
        /// they are snapshot fields and not events.
        /// </summary>
        /// <remarks>
        /// <b>This is the assertion ADR-0007's new section is about.</b> A seek
        /// re-simulates and subscribes nobody, so the events of the re-run ticks
        /// are never built — an "a slow landed" event would be heard once, on
        /// the tick it landed, and a creep scrubbed back across that tick would
        /// be slowed in the simulation and undecorated on screen.
        ///
        /// OBSERVED: drive the wash off <c>MatchDecorations</c> instead. Every
        /// assertion above stays green, because a match played forwards hears
        /// every event exactly once; this goes red the moment the bar is
        /// dragged.
        /// </remarks>
        [Test]
        public void WhatIsOnACreepSurvivesASeek()
        {
            MatchView view = BeginWithEffects();

            RunUntil(view, () => view.Current.Creeps.Any(creep => creep.SpeedMagnitude != 0));

            RunUntil(view, () => view.Current.Creeps.Any(creep => creep.Shield > 0));

            int tick = view.Current.Tick;
            int slowed = view.Current.Creeps.First(creep => creep.SpeedMagnitude != 0).Id;
            CreepSnapshot pooled = view.Current.Creeps.First(creep => creep.Shield > 0);

            view.ReSimulateTo(tick + 60);
            view.ReSimulateTo(tick);

            Assert.That(
                view.Current.Creeps.Any(creep => creep.Id == slowed && creep.SpeedMagnitude != 0),
                Is.True,
                "the same tick played again is a different match");

            // The bar is what carries the claim now that no modifier is drawn
            // on the body at all: it is a pure function of the snapshot, so a
            // scrub back onto this tick has to put it back exactly.
            Assert.That(
                view.Creeps.Live[pooled.Id].Marks.ShieldSegment.localScale.x,
                Is.EqualTo(MatchTuning.UnitBarLength * (pooled.Shield / (float)FixtureMaxHp))
                    .Within(1e-4f),
                "a creep scrubbed back onto the tick it carried a pool on is drawn without it");
        }

        /// <summary>
        /// A body that becomes another row is drawn as that row from the tick it
        /// becomes it, and a scrub across that tick draws the right body on both
        /// sides of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the fourth acceptance criterion of #267 and it is
        /// satisfied by a field rather than by a message.</b> Which row a creep
        /// is is in the snapshot, so a seek — which re-simulates and subscribes
        /// nobody — puts the body back to whatever the row was at that tick with
        /// no listener involved. An event saying "it transformed" would be
        /// discarded by the re-run and the body would be scrubbed back wearing
        /// the wrong skin; see ADR-0007 and ADR-0059.
        /// </para>
        /// <para>
        /// <b>The two models are nearly the same picture and the assertions are
        /// chosen for it.</b> The Cursed Villager and the Werewolf are the same
        /// figure at the same height in the same clothes — the wolf head and the
        /// axe are the whole of what tells them apart — so what is asserted is
        /// the model asset's own name and the hand the axe is in, both read off
        /// the art rather than written down here.
        /// </para>
        /// </remarks>
        [Test]
        public void ACreepDrawsTheRowItBecameAndASeekDrawsTheRightBodyEitherSide()
        {
            MatchArt art = TheMatchOnScreen.Art();
            MatchView view = BeginWithTheTransformingPair();

            RunUntil(view, () => view.Current.Creeps.Any(creep => creep.TypeId == Werewolf));

            CreepSnapshot changed = view.Current.Creeps.First(creep => creep.TypeId == Werewolf);
            int body = changed.Id;
            int tick = view.Current.Tick;

            Assert.That(tick, Is.GreaterThan(1), "nothing had time to be hit");
            AssertDrawnAs(view, body, art, Werewolf);

            // One tick earlier the same id is the Villager, which is what makes
            // this a transformation rather than a death and a spawn.
            view.ReSimulateTo(tick - 1);

            Assert.That(
                view.Current.Creeps.Single(creep => creep.Id == body).TypeId,
                Is.EqualTo(CursedVillager),
                "the body was already the Werewolf a tick before it became one");

            AssertDrawnAs(view, body, art, CursedVillager);

            // Forward across the change, and back over it again. Neither
            // direction is special: the pool is told what exists and works the
            // rest out by subtraction.
            view.ReSimulateTo(tick + 30);
            AssertDrawnAs(view, body, art, Werewolf);

            view.ReSimulateTo(tick - 1);
            AssertDrawnAs(view, body, art, CursedVillager);

            view.ReSimulateTo(tick);
            AssertDrawnAs(view, body, art, Werewolf);
        }

        /// <summary>
        /// A body raised mid-lane is drawn from the tick it arrives, and a seek
        /// back across that tick takes it off screen again.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The arrival needs no event, and that is what this measures.</b> A
        /// raised creep is an entity in the snapshot, so the pool claims a view
        /// for it by subtraction the moment it appears -- and a seek
        /// re-simulates with nobody subscribed, so a scrub back to a tick before
        /// the raise has to put the view away again with no decoration having
        /// been heard.
        /// </para>
        /// <para>
        /// The Necromancer and the Minion are shipped rows and not a fixture,
        /// for the reason the transforming pair is: what the recorded wave lacks
        /// is not the authoring but the sending.
        /// </para>
        /// </remarks>
        [Test]
        public void ARaisedBodyIsDrawnOnItsTickAndASeekTakesItBackOffScreen()
        {
            MatchArt art = TheMatchOnScreen.Art();
            MatchView view = BeginWithTheSpawner();

            RunUntil(view, () => view.Current.Creeps.Any(creep => creep.TypeId == Minion));

            CreepSnapshot risen = view.Current.Creeps.First(creep => creep.TypeId == Minion);
            int body = risen.Id;
            int tick = view.Current.Tick;

            Assert.That(tick, Is.GreaterThan(1), "nothing had time to be raised");
            Assert.That(
                view.Current.Creeps.Any(creep => creep.TypeId == Necromancer),
                Is.True,
                "the raiser is gone, so this is not a raise");

            AssertDrawnAs(view, body, art, Minion);

            // One tick earlier it does not exist, so nothing is drawn for it --
            // which is the pool being told what exists rather than being told
            // what changed.
            view.ReSimulateTo(tick - 1);

            Assert.That(
                view.Current.Creeps.Any(creep => creep.Id == body),
                Is.False,
                "the body was already on the corridor a tick before it was raised");

            Assert.That(
                view.Creeps.Live.ContainsKey(body),
                Is.False,
                "a view is still being drawn for a body that does not exist yet");

            // Forward across the raise and back over it again. Neither
            // direction is special.
            view.ReSimulateTo(tick + 30);
            AssertDrawnAs(view, body, art, Minion);

            view.ReSimulateTo(tick - 1);
            Assert.That(view.Creeps.Live.ContainsKey(body), Is.False);

            view.ReSimulateTo(tick);
            AssertDrawnAs(view, body, art, Minion);
        }

        /// <summary>
        /// A kill that pays moves the gold the match is carrying on the tick it
        /// happens, and a seek back across that tick moves it back.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The gain is a number on the match and not a decoration, and that
        /// is what this measures.</b> A seek re-simulates from tick zero with
        /// nobody subscribed, so what the view is holding either side of the
        /// tick is whatever the re-simulation arrived at -- which is the only
        /// reason a scrub across income can be right without anything being
        /// replayed. See
        /// <c>docs/adr/0026-seeking-re-simulates-rather-than-caching.md</c>.
        /// </para>
        /// <para>
        /// <b>And the decoration draws nothing</b>, for the reason
        /// <c>CreepTransformed</c> and <c>CreepRaised</c> draw nothing: a coin,
        /// a floating number or a flash on the purse is an art decision. The
        /// event is heard and no object appears, which is asserted here rather
        /// than described.
        /// </para>
        /// <para>
        /// The Grave Robber is a shipped row and not a fixture, for the reason
        /// the transforming pair is: what the recorded wave lacks is not the
        /// authoring but the sending. It takes the twelve rows of
        /// <see cref="FourLinesDefense"/> to kill one inside a match -- the
        /// recorded six leave most of a column standing -- so this is the
        /// defense those frames are of, with a short column walking into it.
        /// </para>
        /// </remarks>
        [Test]
        public void AKillThatPaysMovesTheGoldOnItsTickAndASeekMovesItBack()
        {
            MatchView view = BeginWithTheGraveRobbers();

            RunUntil(view, () => view.Match.Bounty > 0);

            int tick = view.Current.Tick;
            int paid = view.Match.Bounty;

            Assert.That(paid, Is.EqualTo(GraveRobberPays), "the first kill paid something else");
            Assert.That(tick, Is.GreaterThan(1), "nothing had time to be killed");

            // A tick earlier nothing had been killed, so the match is carrying
            // nothing -- and the view got there by re-simulating rather than by
            // undoing anything.
            view.ReSimulateTo(tick - 1);

            Assert.That(view.Match.Bounty, Is.Zero, "the gold survived a seek back across the kill");

            // Forward across it and back over it again. Neither direction is
            // special, and the number is the same one both times.
            view.ReSimulateTo(tick);

            Assert.That(view.Match.Bounty, Is.EqualTo(paid));

            view.ReSimulateTo(tick - 1);

            Assert.That(view.Match.Bounty, Is.Zero);

            view.ReSimulateTo(tick);

            Assert.That(view.Match.Bounty, Is.EqualTo(paid));

            // And the payment puts nothing on screen. Called straight, as the
            // bubble events above are, because what is being asserted is what
            // the sink does rather than when the simulation calls it.
            view.Decorations.Clear();

            int heard = view.Decorations.EventsHeard;

            view.Decorations.BountyPaid(view.Current.Creeps[0].Id, GraveRobberPays);

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heard + 1), "the sink did not hear it");
            Assert.That(
                view.Decorations.ActiveCount,
                Is.Zero,
                "the payment drew something, and no shape for it has been signed");
        }

        /// <summary>
        /// That the body with this id is drawn as the row with that id: the
        /// model asset the row's art names, and whatever that row puts in its
        /// hands.
        /// </summary>
        private static void AssertDrawnAs(MatchView view, int body, MatchArt art, int unitId)
        {
            CreepView drawn = view.Creeps.Live[body];
            UnitArt authored = art.ArtFor(unitId);

            Assert.That(
                drawn.Model.name,
                Is.EqualTo(authored.Model.name),
                $"the body is drawn as {drawn.Model.name} where unit {unitId} is {authored.Model.name}");

            Assert.That(
                drawn.RightHand == null,
                Is.EqualTo(authored.RightHand == null),
                $"unit {unitId} is drawn holding the wrong thing");
        }

        /// <summary>
        /// Nothing in the recorded match is ever marked, so the marks cost a
        /// match with no effects in it exactly nothing on screen.
        /// </summary>
        /// <remarks>
        /// The roster does author bubbles — four creep auras and eight tower
        /// ones — but the recorded wave releases Minions and Skeleton Scouts
        /// against Archers and Mages, and not one of those four rows carries
        /// one that lasts.
        /// </remarks>
        [Test]
        public void ACreepCarryingNothingWearsNothing()
        {
            MatchView view = Begin();

            RunUntil(view, () =>
            {
                foreach (CreepView drawn in view.Creeps.Live.Values)
                {
                    Assert.That(drawn.Marks.Bar.gameObject.activeSelf, Is.False,
                        "a creep with no pool wears a bar");
                }

                return false;
            });

            Assert.That(view.Current.Tick, Is.GreaterThan(1000), "hardly any of the match was watched");
        }

        /// <summary>
        /// The two rows that carry a pool of their own wear it from the first
        /// tick they are drawn, out of the snapshot and with no event anywhere.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is what the Vampire's blood and the Grave Robber's pack are
        /// drawn as, and nothing was added to draw it.</b> A pool is a
        /// <c>CreepSnapshot</c> field rather than a moment, which is why it
        /// survives a scrub, and <see cref="EffectMarks"/> already draws it as
        /// the second segment of the bar over the body. Those two rows author
        /// theirs in <c>content/units.txt</c> rather than being granted one, so
        /// unlike the Necromancer's ward there is no event on the wire at any
        /// tick — a decoration of any kind would have nothing to fire off, and a
        /// second shape saying "there is a pool here" is what this asserts is
        /// absent.
        /// </para>
        /// </remarks>
        [Test]
        public void TheTwoRowsWithAPoolOfTheirOwnWearItAndDrawNoEffect()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            MatchView view = BeginWithTheCreepAuras();

            RunUntil(view, () => Walkers(view, Vampire).Any() && Walkers(view, GraveRobber).Any());

            foreach (int unitId in new[] { Vampire, GraveRobber })
            {
                CreepSnapshot carrying = view.Current.Creeps.First(creep => creep.TypeId == unitId);
                EffectMarks marks = view.Creeps.Live[carrying.Id].Marks;

                // The row's own pool is the floor and not the number: the
                // Necromancer walks in this wave too, and its ward grants a
                // quarter of a body's health on top of whatever that body
                // brought. One field carries both, summed, which is what makes
                // the granted one survive a scrub as well.
                Assert.That(carrying.Shield, Is.GreaterThanOrEqualTo(types.ById(unitId).Shield),
                    $"unit {unitId} walks on with less pool than its own row authored");

                Assert.That(marks.Bar.gameObject.activeSelf, Is.True,
                    $"unit {unitId} carries a pool and wears no bar");

                Assert.That(
                    marks.ShieldSegment.localScale.x,
                    Is.EqualTo(
                            MatchTuning.UnitBarLength
                            * (carrying.Shield / (float)types.ById(unitId).MaxHp))
                        .Within(1e-4f),
                    $"unit {unitId}'s pool is not drawn as a share of the health it stands in front of");

                Assert.That(types.ById(unitId).Bubble.Present, Is.False,
                    $"unit {unitId} authors a bubble, so its pool is no longer the case this is about");
            }

            // And drawing those bodies again puts nothing on screen. A pool is
            // snapshot state rather than a moment: there is no event behind it
            // to draw a shape from, so a second thing saying "there is a pool
            // here" could only be invented, and nothing here invents one.
            view.Decorations.Clear();

            for (var again = 0; again < 5; again++)
            {
                view.Draw(1f);
            }

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "drawing a frame of bodies carrying pools put an effect on screen, and no tick was "
                + "advanced for one to have arrived on");
        }

        /// <summary>
        /// The health pool both fixture rows below author, which the bar's two
        /// segments are shares of.
        /// </summary>
        private const int FixtureMaxHp = 1550;

        /// <summary>
        /// Two rows with the shipped ids, so the art binds and the real board
        /// takes them: a Minion granting a pool to whatever walks beside it, and
        /// an Archer whose shot slows what it hits.
        /// </summary>
        private const string EffectFixtures =
            "layout 3\n"
            + "unit 1 minion moving 1550 28 0 0 0 0 0 0 none 0 36 10 none armoured 0 0 1 "
            + "2000 self friend 30 shield 40 90\n"
            + "unit 3 archer placed 0 0 3200 18 9 6 90 150 hitscan 0 0 40 pierce none 0 0 1 "
            + "0 target enemy 0 speed -40 90\n";

        /// <summary>One Archer, on a cell the recorded defense puts one on.</summary>
        private const string EffectDefense = "tower 3 4 3";

        /// <summary>A column of Minions, released together so they stand in each other's spheres.</summary>
        private const string EffectWave = "order 0 1 10 0";

        /// <summary>
        /// The real board and the real camera, playing the two fixture rows
        /// above.
        /// </summary>
        private MatchView BeginWithEffects()
        {
            UnitTypeTable types = UnitTypeTable.Parse("effect fixtures", EffectFixtures);

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("effect defense", EffectDefense, types),
                WaveScript.Parse("effect wave", EffectWave, types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>
        /// The three rungs of each of the four impact and melee lines, standing
        /// on cells of the real board, in the order the loader wants them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every cell here is next to the corridor, and it has to be.</b>
        /// Nine of these twelve rows reach one hex, where the recorded
        /// defense's archers and mortars reach three and four — so the cells
        /// that defense stands on would be refused at load for a row whose
        /// range cannot touch the route at all.
        /// </para>
        /// <para>
        /// <b>The Blessing and the Templar are one hex apart on purpose and
        /// everything else is at least three away.</b> The Blessing's aura
        /// carries two hexes, so a layout where the nearest tower sat exactly
        /// on the edge would make "how many towers did the glow reach" a
        /// question about the last bit of a float.
        /// </para>
        /// <para>
        /// <b><c>docs/frames/four-lines.txt</c> is the same twelve rows on the
        /// same cells</b>, so the frames of these lines are frames of what this
        /// asserts about. Two copies rather than one, because a play-mode test
        /// cannot read a file that is not beside the player — the same reason
        /// the fixture roster above is written out here.
        /// </para>
        /// </remarks>
        private const string FourLinesDefense =
            "tower 11  3  0\n"
            + "tower 15  5  0\n"
            + "tower 16  2  2\n"
            + "tower 17  5  2\n"
            + "tower 18  2  4\n"
            + "tower 19  7  4\n"
            + "tower 20  5  6\n"
            + "tower 22 11  6\n"
            + "tower 21 12  6\n"
            + "tower 35  3  8\n"
            + "tower 36  7  8\n"
            + "tower 37  2 10\n";

        /// <summary>
        /// The real board, the real roster and the real wave, with the twelve
        /// rows of the Knight, Barbarian, Paladin and Engineer lines standing on
        /// it instead of the recorded six.
        /// </summary>
        /// <remarks>
        /// The roster is the shipped one and not a fixture, which is the whole
        /// difference from <see cref="BeginWithEffects"/>: these four capstones
        /// author real bubbles in <c>content/units.txt</c>, so what is under
        /// test is the rows as they ship rather than rows invented to have
        /// something to draw.
        /// </remarks>
        private MatchView BeginWithTheFourLines()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("four lines", FourLinesDefense, types),
                StreamingContent.ReadWave(types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>
        /// The three rungs of each of the two pierce lines, standing on cells
        /// of the real board, in the order the loader wants them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The six are bunched round the entrance on purpose.</b> The
        /// shortest reach here is the Rogue line's two hexes, and what these
        /// tests need is bodies inside every one of the six at the same tick —
        /// three of them at once, for the throw that goes to three bodies. The
        /// stretch of corridor leaving the entrance is where the wave is
        /// densest, so it is the one stretch that answers that.
        /// </para>
        /// <para>
        /// <b><c>docs/frames/pierce-lines.txt</c> is the same six rows on the
        /// same cells</b>, so the frames of these lines are frames of what this
        /// asserts about — two copies for the reason
        /// <see cref="FourLinesDefense"/> has two.
        /// </para>
        /// </remarks>
        private const string PierceLinesDefense =
            "tower  3  3  0\n"
            + "tower 14  5  0\n"
            + "tower 31  2  2\n"
            + "tower 32  5  2\n"
            + "tower 33  2  4\n"
            + "tower 34  7  4\n";

        /// <summary>
        /// The real board, the real roster and the real wave, with the six rows
        /// of the Archer and Rogue lines standing on it instead of the recorded
        /// six.
        /// </summary>
        private MatchView BeginWithThePierceLines()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("pierce lines", PierceLinesDefense, types),
                StreamingContent.ReadWave(types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>
        /// The three rungs of each of the three magic lines that are not the
        /// Paladin's, standing on cells of the real board, in the order the
        /// loader wants them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The cells are <see cref="FourLinesDefense"/>'s first nine.</b>
        /// Every row here reaches at least three hexes where nine of those
        /// twelve reach one, so a cell that holds a Soldier holds any of these
        /// — and reusing them means the three defenses differ only in what is
        /// standing rather than in where.
        /// </para>
        /// <para>
        /// <b><c>docs/frames/magic-lines.txt</c> is the same nine rows on the
        /// same cells</b>, so the frames of these lines are frames of what this
        /// asserts about — two copies for the reason
        /// <see cref="FourLinesDefense"/> has two.
        /// </para>
        /// </remarks>
        private const string MagicLinesDefense =
            "tower  4  3  0\n"
            + "tower 23  5  0\n"
            + "tower 24  2  2\n"
            + "tower 25  5  2\n"
            + "tower 26  2  4\n"
            + "tower 27  7  4\n"
            + "tower 28  5  6\n"
            + "tower 29 11  6\n"
            + "tower 30 12  6";

        /// <summary>
        /// The real board, the real roster and the real wave, with the nine
        /// rows of the Mage, Cleric and Druid lines standing on it instead of
        /// the recorded six.
        /// </summary>
        private MatchView BeginWithTheMagicLines()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("magic lines", MagicLinesDefense, types),
                StreamingContent.ReadWave(types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>The tower on the board drawn as the row with this id.</summary>
        private static TowerView Standing(MatchView view, int unitId) =>
            view.Towers.Values.Single(tower => tower.Type.Id == unitId);

        /// <summary>
        /// How far one tower's own bubble reaches, in thousandths of a hex, off
        /// the row that authors it.
        /// </summary>
        /// <remarks>
        /// Read rather than written out, so a reach that moves in
        /// <c>content/units.txt</c> moves here too. A row authoring none is
        /// refused rather than quietly asserted about, because a bubble of no
        /// radius draws nothing and every assertion downstream of it would pass
        /// by drawing nothing at all.
        /// </remarks>
        private static int Reaches(TowerView tower)
        {
            int radius = tower.Type.Bubble.RadiusMilliHex;

            Assert.That(radius, Is.GreaterThan(0),
                $"unit {tower.Type.Id} ({tower.Type.Label}) authors no bubble to draw a signature for");

            return radius;
        }

        /// <summary>
        /// Every effect object of one kind that is on screen, found by the name
        /// its pool gives it.
        /// </summary>
        private static IEnumerable<Transform> Pieces(MatchView view, string named) =>
            view.GetComponentsInChildren<Transform>()
                .Where(child => child.name == named && child.gameObject.activeSelf);


        /// <summary>The Skeleton Mage, whose haste is the first creep aura.</summary>
        private const int SkeletonMage = 7;

        /// <summary>The Necromancer, whose ward grants a pool and whose raise puts a Minion down.</summary>
        private const int Necromancer = 38;

        /// <summary>The Frost Wight, whose frostbite is the one aura reaching towers.</summary>
        private const int FrostWight = 41;

        /// <summary>The Vampire, which authors a pool of its own.</summary>
        private const int Vampire = 43;

        /// <summary>The Witch, whose hex ward puts armour on what walks beside it.</summary>
        private const int Witch = 44;

        /// <summary>The Grave Robber, which authors the other pool.</summary>
        private const int GraveRobber = 49;

        /// <summary>The Cursed Villager, the one row that names a row it becomes.</summary>
        private const int CursedVillager = 47;

        /// <summary>The Werewolf, which is what it becomes.</summary>
        private const int Werewolf = 48;

        /// <summary>
        /// One Archer on a cell of the recorded defense, and a short column of
        /// Cursed Villagers walking into it.
        /// </summary>
        /// <remarks>
        /// The shipped rows and not a fixture, for the reason
        /// <see cref="CreepAuraWave"/> is: what the recorded wave lacks is not
        /// the authoring but the sending, so a row invented here would go stale
        /// the day <c>content/units.txt</c> moved.
        /// </remarks>
        private const string TransformDefense = "tower 3 4 3";

        private const string TransformWave = "order 0 47 3 0";

        /// <summary>The Minion, which the Necromancer raises.</summary>
        private const int Minion = 1;

        /// <summary>What docs/roster.md signs the Grave Robber pays for being killed.</summary>
        private const int GraveRobberPays = 12;

        /// <summary>
        /// A short column of Grave Robbers, walking into the twelve rows of
        /// <see cref="FourLinesDefense"/>. Three rather than one so that the
        /// wall has something to kill while the first is still crossing.
        /// </summary>
        private const string BountyWave = "order 0 49 3 0";

        /// <summary>The real board and the real roster, with the row that pays walking it.</summary>
        private MatchView BeginWithTheGraveRobbers()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("bounty defense", FourLinesDefense, types),
                WaveScript.Parse("bounty wave", BountyWave, types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>
        /// One Archer on a cell of the recorded defense, and one Necromancer
        /// walking past it. One rather than a column, so the Minions on screen
        /// are unambiguously the ones it raised.
        /// </summary>
        private const string SpawnerDefense = "tower 3 4 3";

        private const string SpawnerWave = "order 0 38 1 0";

        /// <summary>The real board and the real roster, with the spawner walking it.</summary>
        private MatchView BeginWithTheSpawner()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("spawner defense", SpawnerDefense, types),
                WaveScript.Parse("spawner wave", SpawnerWave, types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>The real board and the real roster, with the pair walking it.</summary>
        private MatchView BeginWithTheTransformingPair()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("transform defense", TransformDefense, types),
                WaveScript.Parse("transform wave", TransformWave, types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>
        /// Two Archers beside the corridor, so the Frost Wight has something on
        /// the tower side to freeze.
        /// </summary>
        /// <remarks>
        /// The cells are two of <see cref="FourLinesDefense"/>'s, which are next
        /// to the corridor because nine of those twelve rows reach one hex. An
        /// Archer reaches three and would stand anywhere; what these tests need
        /// is a tower inside the two hexes a creep aura carries, which is a much
        /// tighter requirement than a tower that can shoot.
        /// </remarks>
        private const string CreepAuraDefense =
            "tower 3  3  0\n"
            + "tower 3  5  0";

        /// <summary>
        /// A column of each of the six rows this ticket is about, released
        /// together.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A wave and not a roster, which is the whole point of it.</b> Every
        /// row here is the shipped row with the aura or the pool
        /// <c>content/units.txt</c> authors on it; what the recorded wave lacks
        /// is not the authoring but the sending, because it releases Minions and
        /// Skeleton Scouts and neither carries either. So nothing here is a
        /// fixture row invented to have something to draw, and nothing here goes
        /// stale when the roster moves.
        /// </para>
        /// <para>
        /// <b>Together, so they stand in each other's spheres.</b> Three of the
        /// four auras reach friends within two hexes, and a column released on
        /// its own would be one body pulsing at nobody. Orders ascend by tick
        /// and then by type id, which is asserted at load rather than sorted.
        /// </para>
        /// <para>
        /// <b><c>docs/frames/creep-auras.txt</c> is the same six orders</b>, so
        /// the frames of these rows are frames of what this asserts about — two
        /// copies for the reason <see cref="FourLinesDefense"/> has two.
        /// </para>
        /// </remarks>
        private const string CreepAuraWave =
            "order 0  7 6 0\n"
            + "order 0 38 6 0\n"
            + "order 0 41 4 0\n"
            + "order 0 43 4 0\n"
            + "order 0 44 6 0\n"
            + "order 0 49 4 0";

        /// <summary>
        /// The real board, the real roster and the recorded seed, with the six
        /// rows that carry an aura or a pool walking it.
        /// </summary>
        private MatchView BeginWithTheCreepAuras()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                StreamingContent.ReadMap(),
                StreamingContent.ReadRuleset(),
                types,
                TowerLayout.Parse("creep aura defense", CreepAuraDefense, types),
                WaveScript.Parse("creep aura wave", CreepAuraWave, types),
                TheMatchOnScreen.Seed);
        }

        /// <summary>The ids of the bodies on the board drawn as the row with this id.</summary>
        private static IEnumerable<int> Walkers(MatchView view, int unitId) =>
            view.Current.Creeps.Where(creep => creep.TypeId == unitId).Select(creep => creep.Id);

        /// <summary>
        /// How far one walking row's aura reaches, in thousandths of a hex, off
        /// the row that authors it.
        /// </summary>
        /// <remarks>
        /// Read rather than written out, for the reason
        /// <see cref="Reaches"/> reads a tower's: a reach that moves in
        /// <c>content/units.txt</c> moves here too, and a row authoring none is
        /// refused rather than quietly asserted about.
        /// </remarks>
        private static int Pulses(UnitTypeTable types, int unitId)
        {
            int radius = types.ById(unitId).Bubble.RadiusMilliHex;

            Assert.That(radius, Is.GreaterThan(0),
                $"unit {unitId} authors no aura to draw a signature for");

            return radius;
        }

        /// <summary>
        /// Every bubble ring standing under the view, found by the name the
        /// pool gives them.
        /// </summary>
        private static IEnumerable<Transform> Rings(MatchView view) =>
            view.GetComponentsInChildren<Transform>().Where(child => child.name == "BubbleRing");
    }
}
