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
        /// Every model is placed with the rotation its importer gave it, and
        /// stands the way the artist drew it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The regression guard for a real bug, and one no other test could
        /// have caught. The view used to force each instantiated model's local
        /// rotation to identity — which looks like tidying up and is actually
        /// throwing away the axis-conversion rotation an FBX root can carry.
        /// The characters' roots happen to be identity, so they stood up
        /// perfectly and hid it; the hitscan tower's is not, and it spent the
        /// whole match lying on its side on the road.
        /// </para>
        /// <para>
        /// Nothing failed. Every assertion in this file was green, the atlas
        /// bound, the mesh was there and the tower fired on schedule. It was
        /// visible only by looking at a frame, which is what the frames are
        /// for — and this test is what stops it needing to be noticed twice.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryModelStandsTheWayItWasImported()
        {
            MatchView view = Begin();
            MatchArt art = TheMatchOnScreen.Art();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            foreach (TowerView tower in view.Towers.Values)
            {
                AssertAuthoredRotation(tower.Model, art.ModelFor(tower.Type.Id));
            }

            foreach (CreepSnapshot creep in view.Current.Creeps)
            {
                AssertAuthoredRotation(view.Creeps.Live[creep.Id].Model, art.ModelFor(creep.TypeId));
            }
        }

        /// <summary>
        /// The instantiated model carries the same local rotation the imported
        /// asset does — measured off the asset rather than written down here,
        /// so the assertion cannot disagree with the import it describes.
        /// </summary>
        private static void AssertAuthoredRotation(GameObject instance, GameObject authored)
        {
            Assert.IsNotNull(authored, "the art source handed over no model to compare against");

            Assert.That(
                Quaternion.Angle(instance.transform.localRotation, authored.transform.localRotation),
                Is.LessThan(0.01f),
                $"{authored.name} is being drawn rotated {Quaternion.Angle(instance.transform.localRotation, authored.transform.localRotation):F1} "
                + "degrees away from how it was imported — a model whose root rotation was overwritten "
                + "lies on its side and nothing else in this suite notices");
        }

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
        /// The mage does not draw a bow, and the archer and the ranger do.
        /// </summary>
        /// <remarks>
        /// Named units on purpose. The test above proves the view agrees with
        /// the art table; this one proves the art table itself is not the thing
        /// that is wrong, which is exactly how the original defect survived —
        /// every layer faithfully carried out an assignment nobody had checked.
        /// <para>
        /// What the mage holds instead is the open spellbook
        /// <c>docs/roster.md</c> signs for that row — "the mage, book in hand".
        /// The staff is on the Sorcerer, which is the rung above him.
        /// </para>
        /// </remarks>
        [Test]
        public void TheBowIsHeldByTheArchersAndNotByTheMage()
        {
            MatchArt art = TheMatchOnScreen.Art();

            const int Archer = 3;
            const int Mage = 4;
            const int Soldier = 11;
            const int Ranger = 14;

            foreach (int id in new[] { Archer, Ranger })
            {
                Assert.That(art.ArtFor(id).LeftHand, Is.Not.Null, "unit " + id + " draws no bow");
                Assert.That(art.ArtFor(id).LeftHand.name, Does.Contain("bow").IgnoreCase,
                    "unit " + id + " holds '" + art.ArtFor(id).LeftHand.name + "' rather than a bow");
            }

            Assert.That(art.ArtFor(Mage).RightHand, Is.Not.Null, "the mage holds nothing");
            Assert.That(art.ArtFor(Mage).RightHand.name, Does.Contain("spellbook").IgnoreCase,
                "the mage holds '" + art.ArtFor(Mage).RightHand.name + "' rather than a spellbook");
            Assert.That(art.ArtFor(Mage).LeftHand, Is.Null, "the mage still carries something off-hand");

            Assert.That(art.ArtFor(Soldier).RightHand, Is.Not.Null, "the soldier holds nothing");
            Assert.That(art.ArtFor(Soldier).RightHand.name, Does.Contain("sword").IgnoreCase,
                "the soldier holds '" + art.ArtFor(Soldier).RightHand.name + "' rather than a sword");
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
        /// Checked by type rather than by looking, because the components that
        /// billboard do so silently and by default: a line renderer faces the
        /// camera unless told otherwise, and so does a particle system, and a
        /// sprite is a flat card by definition.
        /// </remarks>
        [Test]
        public void NothingInTheMatchTurnsToFaceTheCamera()
        {
            MatchView view = Begin();

            RunUntil(view, () => view.Current.Tick > 400);

            Assert.That(view.GetComponentsInChildren<LineRenderer>(true), Is.Empty,
                "a line renderer billboards to the camera unless told not to");
            Assert.That(view.GetComponentsInChildren<TrailRenderer>(true), Is.Empty);
            Assert.That(view.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty,
                "a sprite is a flat card");
            Assert.That(view.GetComponentsInChildren<ParticleSystem>(true), Is.Empty,
                "default particles are camera-facing billboards");
            Assert.That(view.GetComponentsInChildren<Canvas>(true), Is.Empty);

            foreach (Renderer renderer in view.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(
                    renderer is MeshRenderer || renderer is SkinnedMeshRenderer,
                    Is.True,
                    $"{renderer.name} is a {renderer.GetType().Name}, which is not real geometry");
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
        /// Every rung of the Knight, Barbarian, Paladin and Engineer lines
        /// flashes at the point on its own art its shot leaves from, and sparks
        /// on the body its shot lands on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The anchor is asserted here as the place the effect went, not as
        /// a field on a row.</b> That a row names a point on its own art rather
        /// than a height above its root is <c>ImportedArtTests</c>'s claim and
        /// is checked over the whole shipped table; what is checked here is
        /// that the flash the event stream draws is at that point, on twelve
        /// rows standing on a real board — which is the half of it that a
        /// correct anchor and a decoration reading somewhere else would still
        /// pass.
        /// </para>
        /// <para>
        /// <b>The decorations are cleared between rows so that each assertion
        /// is about one flash.</b> Twelve flashes drawn together would have to
        /// be matched back to their towers by position, which is the thing
        /// under test.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryRowOnTheseFourLinesFiresFromItsAnchorAndSparksOnWhatItHits()
        {
            MatchView view = BeginWithTheFourLines();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            foreach (TowerView tower in view.Towers.Values)
            {
                view.Decorations.Clear();
                view.Decorations.TowerFired(tower.Id, creepId);

                Assert.That(view.Decorations.FlashesDrawn, Is.EqualTo(1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) fired and nothing left it");

                Assert.That(tower.AnchorTransform, Is.Not.Null,
                    $"unit {tower.Type.Id} ({tower.Type.Label}) has no anchor, so its flash is at a "
                    + "height above its own root whatever it is holding");

                Transform flash = Pieces(view, "MuzzleFlash").Single();

                Assert.That(Vector3.Distance(flash.position, tower.Muzzle), Is.LessThan(1e-3f),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) fires from {tower.Muzzle} and its flash "
                    + $"is at {flash.position}");

                view.Decorations.CreepDamaged(creepId, 10);

                Assert.That(view.Decorations.SparksDrawn, Is.EqualTo(1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) landed damage and nothing landed with it");

                Transform spark = Pieces(view, "Spark").Single();

                Assert.That(
                    Vector3.Distance(spark.position, walks + (Vector3.up * MatchTuning.HitSparkHeight)),
                    Is.LessThan(1e-3f),
                    "the landing is not on the creep the shot landed on");
            }

            Assert.That(view.Towers.Count, Is.EqualTo(12), "the four lines are twelve rows");
        }

        /// <summary>
        /// Each of the four capstones draws the shape its own row names rather
        /// than the disc every bubble shared, at the size the bubble reached,
        /// and keeps that size for the whole of its life.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The events are handed over by hand, as the ring's are.</b> What is
        /// under test is the decoration and the binding behind it, and the
        /// decoration is reached through the interface the simulation reaches it
        /// through. <b>Each radius is read off the row that authors it</b>
        /// rather than written out here, so this measures the view against
        /// whatever <c>content/units.txt</c> says rather than against a fourth
        /// copy of four numbers.
        /// </para>
        /// <para>
        /// <b>The Mortar's is the one that names no row.</b> Its blast is
        /// centred on the body its shell arrived at, so the event carries a
        /// creep id and the shooter is not in it at all — the burst is what a
        /// blast that arrived on a body draws, and it is asserted here from a
        /// creep id for that reason rather than from the Mortar's own.
        /// </para>
        /// <para>
        /// OBSERVED: let the signatures age like a spark. Every assertion above
        /// the last block stays green and each shape reports a reach its bubble
        /// did not have on every tick but the first — which is the bug #253
        /// found in the ring and fixed with a flag that a new shape can simply
        /// not set.
        /// </para>
        /// </remarks>
        [Test]
        public void EachCapstoneDrawsItsOwnSignatureAtTheSizeItReached()
        {
            MatchView view = BeginWithTheFourLines();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            view.Decorations.Clear();

            // The Shield Wall: a ring on the ground at the edge of the slow.
            TowerView shieldWall = Standing(view, 16);
            int shieldWallRadius = Reaches(shieldWall);

            view.Decorations.AuraPulsed(shieldWall.Id, shieldWallRadius, BubblePayload.Speed);

            Assert.That(view.Decorations.SlowRingsDrawn, Is.EqualTo(1),
                "the Shield Wall pulsed and left no ring");

            Transform slow = Pieces(view, "SlowRing").Single();

            Assert.That(
                Vector3.Distance(
                    slow.position,
                    shieldWall.transform.position + (Vector3.up * MatchTuning.FloorClearance)),
                Is.LessThan(1e-3f),
                "the slow ring is not under the tower that pulsed it");

            Assert.That(slow.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(shieldWallRadius)).Within(1e-4f));

            // The Slam: cracks out from under the man who swung.
            TowerView slam = Standing(view, 19);
            int slamRadius = Reaches(slam);

            view.Decorations.BlastLanded(slam.Id, slamRadius, BubblePayload.Damage);

            Assert.That(view.Decorations.ShocksDrawn, Is.EqualTo(1),
                "the Slam swung and the ground did not move");

            Transform shock = Pieces(view, "GroundShock").Single();

            Assert.That(
                Vector3.Distance(
                    shock.position,
                    slam.transform.position + (Vector3.up * MatchTuning.FloorClearance)),
                Is.LessThan(1e-3f),
                "the shock is not under the tower that swung");

            Assert.That(shock.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(slamRadius)).Within(1e-4f));

            // The Blessing: one ring over each tower it reached, which is
            // itself and the Templar two hexes away and nothing else.
            TowerView blessing = Standing(view, 22);
            TowerView templar = Standing(view, 21);
            TowerView paladin = Standing(view, 20);

            view.Decorations.AuraPulsed(blessing.Id, Reaches(blessing), BubblePayload.Cooldown);

            Assert.That(view.Decorations.GlowsDrawn, Is.EqualTo(2),
                "the Blessing reached itself and the Templar, and drew something else");

            Vector3[] glows = Pieces(view, "TowerGlow").Select(glow => glow.position).ToArray();
            Vector3 overhead = Vector3.up * MatchTuning.BlessingGlowHeight;

            Assert.That(
                glows.Min(at => Vector3.Distance(at, blessing.transform.position + overhead)),
                Is.LessThan(1e-3f),
                "the tower doing the blessing is not wearing one");

            Assert.That(
                glows.Min(at => Vector3.Distance(at, templar.transform.position + overhead)),
                Is.LessThan(1e-3f),
                "the Templar is inside the aura and is not wearing one");

            Assert.That(
                glows.Min(at => Vector3.Distance(at, paladin.transform.position + overhead)),
                Is.GreaterThan(1f),
                "the Paladin is six hexes away and is wearing one anyway");

            // The Mortar: a burst on the body the shell arrived at.
            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            int mortarRadius = Reaches(Standing(view, 37));

            view.Decorations.BlastLanded(creepId, mortarRadius, BubblePayload.Damage);

            Assert.That(view.Decorations.BurstsDrawn, Is.EqualTo(1),
                "a blast arrived on a body and nothing burst");

            Transform burst = Pieces(view, "MortarBurst").Single();

            Assert.That(
                Vector3.Distance(burst.position, walks + (Vector3.up * MatchTuning.HitSparkHeight)),
                Is.LessThan(1e-3f),
                "the burst is not on the body the shell arrived at");

            Assert.That(burst.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(mortarRadius)).Within(1e-4f));

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(0),
                "a capstone fell back to the plain disc, so its binding was not read");

            // And every one of them holds the size it reported. A shape that
            // stands for a radius saying a smaller radius each tick is the bug
            // the ring already had.
            Transform[] shapes = { slow, shock, burst };
            Vector3[] drawnAt = shapes.Select(shape => shape.localScale).ToArray();

            for (var tick = 1; tick < MatchTuning.GroundShockTicks; tick++)
            {
                view.Decorations.AgeOneTick();

                for (var index = 0; index < shapes.Length; index++)
                {
                    Assert.That(shapes[index].localScale, Is.EqualTo(drawnAt[index]),
                        $"{shapes[index].name} is {shapes[index].localScale.x} metres across {tick} "
                        + $"ticks after a bubble {drawnAt[index].x} metres across went off");
                }
            }
        }

        /// <summary>
        /// A seek re-runs the ticks it passes over in silence, so nothing it
        /// passes over is drawn a second time.
        /// </summary>
        /// <remarks>
        /// The counter that has to answer for it is
        /// <see cref="MatchDecorations.EventsHeard"/>, which a clear does not
        /// reset: a seek that heard the re-run ticks' events and then tidied up
        /// after itself would be indistinguishable from one that never heard
        /// them, from every counter that does.
        /// </remarks>
        [Test]
        public void ASeekDrawsNoSignatureASecondTime()
        {
            MatchView view = BeginWithTheFourLines();

            RunUntil(view, () => view.Decorations.SlowRingsDrawn > 0 && view.Decorations.GlowsDrawn > 0);

            Assert.That(view.Decorations.SlowRingsDrawn, Is.GreaterThan(0),
                "no aura pulsed in the whole match, so this proves nothing");

            int tick = view.Current.Tick;
            int heard = view.Decorations.EventsHeard;

            view.ReSimulateTo(tick + 90);
            view.ReSimulateTo(tick);

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heard),
                "a seek heard the events of the ticks it re-ran, so every effect between here and the "
                + "start was drawn again");

            Assert.That(view.Decorations.SlowRingsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.GlowsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.ShocksDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.BurstsDrawn, Is.EqualTo(0));

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "an effect from before the seek is still on screen, and the tick it belongs to is now "
                + "in the future");
        }

        /// <summary>
        /// Every rung of the Archer and Rogue lines flashes at the point on its
        /// own art its shot leaves from, and sparks on the body its shot lands
        /// on.
        /// </summary>
        /// <remarks>
        /// The same claim <see cref="EveryRowOnTheseFourLinesFiresFromItsAnchorAndSparksOnWhatItHits"/>
        /// makes about the impact and melee rows, on the six rows that throw
        /// and shoot. It is asserted a second time rather than folded into one
        /// test over eighteen rows because the two defenses cannot stand on the
        /// board together: eighteen towers next to a corridor of fifty-one
        /// cells is more than the cells beside it, and a row that could not be
        /// placed would be a row this quietly stopped covering.
        /// </remarks>
        [Test]
        public void EveryRowOnThePierceLinesFiresFromItsAnchorAndSparksOnWhatItHits()
        {
            MatchView view = BeginWithThePierceLines();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            foreach (TowerView tower in view.Towers.Values)
            {
                view.Decorations.Clear();
                view.Decorations.TowerFired(tower.Id, creepId);

                Assert.That(view.Decorations.FlashesDrawn, Is.EqualTo(1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) fired and nothing left it");

                Assert.That(tower.AnchorTransform, Is.Not.Null,
                    $"unit {tower.Type.Id} ({tower.Type.Label}) has no anchor, so its flash is at a "
                    + "height above its own root whatever it is holding");

                Transform flash = Pieces(view, "MuzzleFlash").Single();

                Assert.That(Vector3.Distance(flash.position, tower.Muzzle), Is.LessThan(1e-3f),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) fires from {tower.Muzzle} and its flash "
                    + $"is at {flash.position}");

                view.Decorations.CreepDamaged(creepId, 10);

                Assert.That(view.Decorations.SparksDrawn, Is.EqualTo(1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) landed damage and nothing landed with it");

                Transform spark = Pieces(view, "Spark").Single();

                Assert.That(
                    Vector3.Distance(spark.position, walks + (Vector3.up * MatchTuning.HitSparkHeight)),
                    Is.LessThan(1e-3f),
                    "the landing is not on the creep the shot landed on");
            }

            Assert.That(view.Towers.Count, Is.EqualTo(6), "the two pierce lines are six rows");
        }

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
        /// A seek re-runs the ticks it passes over in silence, so no shot it
        /// passes over is drawn a second time.
        /// </summary>
        /// <remarks>
        /// <see cref="ASeekDrawsNoSignatureASecondTime"/>'s claim, on the two
        /// shapes an event stream reaches through a shot rather than through a
        /// bubble. The counter that answers for it is the same one:
        /// <see cref="MatchDecorations.EventsHeard"/>, which a clear does not
        /// reset.
        /// </remarks>
        [Test]
        public void ASeekDrawsNoPierceShotASecondTime()
        {
            MatchView view = BeginWithThePierceLines();

            RunUntil(view, () => view.Decorations.LongShotsDrawn > 0 && view.Decorations.KnivesDrawn > 0);

            Assert.That(view.Decorations.LongShotsDrawn, Is.GreaterThan(0),
                "the Overwatch never fired in the whole match, so this proves nothing");

            Assert.That(view.Decorations.KnivesDrawn, Is.GreaterThan(0),
                "no knife was ever thrown in the whole match, so this proves nothing");

            int tick = view.Current.Tick;
            int heard = view.Decorations.EventsHeard;

            view.ReSimulateTo(tick + 90);
            view.ReSimulateTo(tick);

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heard),
                "a seek heard the events of the ticks it re-ran, so every shot between here and the "
                + "start was drawn again");

            Assert.That(view.Decorations.LongShotsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.KnivesDrawn, Is.EqualTo(0));

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "an effect from before the seek is still on screen, and the tick it belongs to is now "
                + "in the future");
        }

        /// <summary>
        /// Every rung of the Mage, Cleric and Druid lines flashes at the point
        /// on its own art its shot leaves from, sparks on the body its shot
        /// lands on, and — where it is hitscan — puts a bolt in the air out of
        /// the same point.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The same claim the two tests above make about the impact, melee and
        /// pierce rows, on the nine that cast. It is asserted a third time
        /// rather than folded in for the reason there are three: twenty-one
        /// towers next to a corridor of fifty-one cells is more than the cells
        /// beside it, and a row that could not be placed would be a row this
        /// quietly stopped covering.
        /// </para>
        /// <para>
        /// <b>Six of the nine draw a bolt and three draw the shared tracer, and
        /// that split is the delivery column rather than a choice made here.</b>
        /// The Cleric and Druid lines are hitscan, so the only thing crossing
        /// to the body is whatever the decoration draws. The Mage line is
        /// projectile: its shell is a real entity in the snapshot flying that
        /// same line over thirty-three ticks, and a bolt drawn beside it would
        /// be a second thing in the air saying what the shell already says.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryRowOnTheMagicLinesFiresFromItsAnchorAndSparksOnWhatItHits()
        {
            MatchView view = BeginWithTheMagicLines();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            foreach (TowerView tower in view.Towers.Values)
            {
                view.Decorations.Clear();
                view.Decorations.TowerFired(tower.Id, creepId);

                Assert.That(view.Decorations.FlashesDrawn, Is.EqualTo(1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) fired and nothing left it");

                Assert.That(tower.AnchorTransform, Is.Not.Null,
                    $"unit {tower.Type.Id} ({tower.Type.Label}) has no anchor, so its flash is at a "
                    + "height above its own root whatever it is holding");

                Transform flash = Pieces(view, "MuzzleFlash").Single();

                Assert.That(Vector3.Distance(flash.position, tower.Muzzle), Is.LessThan(1e-3f),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) fires from {tower.Muzzle} and its flash "
                    + $"is at {flash.position}");

                bool hitscan = tower.Type.Delivery == Delivery.Hitscan;

                Assert.That(view.Decorations.BoltsDrawn, Is.EqualTo(hitscan ? 1 : 0),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) is {tower.Type.Delivery} and drew "
                    + $"{view.Decorations.BoltsDrawn} bolts");

                Assert.That(view.Decorations.TracersDrawn, Is.EqualTo(hitscan ? 0 : 1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) is {tower.Type.Delivery} and drew "
                    + $"{view.Decorations.TracersDrawn} of the tracer every row shares");

                if (hitscan)
                {
                    Transform bolt = Pieces(view, "MagicBolt").Single();

                    Assert.That(Vector3.Distance(bolt.position, tower.Muzzle), Is.LessThan(1e-3f),
                        $"unit {tower.Type.Id} ({tower.Type.Label})'s bolt starts at {bolt.position} "
                        + $"and its tome or staff tip is at {tower.Muzzle}");
                }

                view.Decorations.CreepDamaged(creepId, 10);

                Assert.That(view.Decorations.SparksDrawn, Is.EqualTo(1),
                    $"unit {tower.Type.Id} ({tower.Type.Label}) landed damage and nothing landed with it");

                Transform spark = Pieces(view, "Spark").Single();

                Assert.That(
                    Vector3.Distance(spark.position, walks + (Vector3.up * MatchTuning.HitSparkHeight)),
                    Is.LessThan(1e-3f),
                    "the landing is not on the creep the shot landed on");
            }

            Assert.That(view.Towers.Count, Is.EqualTo(9), "the three magic lines are nine rows");
        }

        /// <summary>
        /// The Consecration lays light on the ground out to the edge of its
        /// aura, the Overgrowth puts roots under every body it is holding, and
        /// the Unravel strips the hex its bolt landed on — while the Mage's
        /// splash, which is a damage blast on a body like the Mortar's, still
        /// wears the Mortar's burst.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The last of those is open question 8 asserted rather than fixed.</b>
        /// A blast centred on its target names the body the shot arrived at and
        /// the shooter is deliberately not in the event, so the only handle on
        /// it is the payload — which tells the Unravel's armour blast from the
        /// Mortar's damage blast and cannot tell the Mage's damage blast from
        /// the Mortar's at all. The burst on the Mage's splash is asserted here
        /// so that the day something does tell them apart, this is the test
        /// that says so.
        /// </para>
        /// <para>
        /// <b>The Overgrowth's roots are counted against the bodies on the
        /// board and not against a radius.</b> That aura reaches sixty hexes
        /// and the board is nineteen across, so every creep in the snapshot is
        /// inside it — which is what "the whole board slows while he stands"
        /// means, drawn.
        /// </para>
        /// </remarks>
        [Test]
        public void EachMagicCapstoneDrawsItsOwnSignature()
        {
            MatchView view = BeginWithTheMagicLines();

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            view.Decorations.Clear();

            // The Consecration: light filling the ground the font has claimed.
            TowerView consecration = Standing(view, 25);
            int consecrationRadius = Reaches(consecration);

            view.Decorations.AuraPulsed(consecration.Id, consecrationRadius, BubblePayload.Armour);

            Assert.That(view.Decorations.LightsDrawn, Is.EqualTo(1),
                "the Consecration pulsed and the ground stayed dark");

            Transform light = Pieces(view, "ConsecrationLight").Single();

            Assert.That(
                Vector3.Distance(
                    light.position,
                    consecration.transform.position + (Vector3.up * MatchTuning.FloorClearance)),
                Is.LessThan(1e-3f),
                "the light is not under the tower that pulsed it");

            Assert.That(light.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(consecrationRadius)).Within(1e-4f));

            // The Overgrowth: roots under every body the aura is holding, which
            // at sixty hexes is every body on the board.
            TowerView overgrowth = Standing(view, 30);

            view.Decorations.AuraPulsed(overgrowth.Id, Reaches(overgrowth), BubblePayload.Speed);

            Assert.That(view.Decorations.RootsDrawn, Is.EqualTo(view.Current.Creeps.Count),
                "the Overgrowth slows the whole board and the roots reached a different number of "
                + "bodies than are standing on it");

            Assert.That(view.Decorations.RootsDrawn, Is.GreaterThan(0),
                "no body was on the board, so this proves nothing");

            Vector3[] roots = Pieces(view, "OvergrowthRoots").Select(patch => patch.position).ToArray();

            foreach (int walking in view.Current.Creeps.Select(creep => creep.Id))
            {
                Vector3 under = view.Creeps.Live[walking].transform.position
                    + (Vector3.up * MatchTuning.FloorClearance);

                Assert.That(roots.Min(at => Vector3.Distance(at, under)), Is.LessThan(1e-3f),
                    $"creep {walking} is inside the aura and has no roots under it");
            }

            Assert.That(
                Pieces(view, "OvergrowthRoots").First().localScale.x,
                Is.EqualTo(MatchTuning.OvergrowthRootPatchDiameter).Within(1e-4f),
                "a patch of roots is scaled by the reach of an aura that covers ten boards");

            // The Unravel: the hex his bolt landed on, stripped. The event
            // names the body and never him, so what picks this shape is the
            // armour payload.
            int creepId = view.Current.Creeps[0].Id;
            Vector3 walks = view.Creeps.Live[creepId].transform.position;

            int unravelRadius = Reaches(Standing(view, 27));

            view.Decorations.BlastLanded(creepId, unravelRadius, BubblePayload.Armour);

            Assert.That(view.Decorations.StripsDrawn, Is.EqualTo(1),
                "an armour blast arrived on a body and nothing came off the hex");

            Transform strip = Pieces(view, "ArmourStrip").Single();

            Assert.That(
                Vector3.Distance(
                    strip.position, walks + (Vector3.up * MatchTuning.FloorClearance)),
                Is.LessThan(1e-3f),
                "the strip is not on the hex the bolt arrived at");

            Assert.That(strip.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(unravelRadius)).Within(1e-4f));

            // And the Mage's splash, which is the open question: a damage blast
            // on a body, indistinguishable from the Mortar's shell landing.
            view.Decorations.BlastLanded(creepId, Reaches(Standing(view, 4)), BubblePayload.Damage);

            Assert.That(view.Decorations.BurstsDrawn, Is.EqualTo(1),
                "the Mage's splash stopped drawing the Mortar's burst, which would mean something now "
                + "tells a damage blast on a body from another one — open question 8 in docs/roster.md");

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(0),
                "a capstone fell back to the plain disc, so its binding was not read");

            // And every one of them holds the size it reported.
            Transform[] shapes = { light, strip };
            Vector3[] drawnAt = shapes.Select(shape => shape.localScale).ToArray();

            for (var tick = 1; tick < MatchTuning.ArmourStripTicks; tick++)
            {
                view.Decorations.AgeOneTick();

                for (var index = 0; index < shapes.Length; index++)
                {
                    Assert.That(shapes[index].localScale, Is.EqualTo(drawnAt[index]),
                        $"{shapes[index].name} is {shapes[index].localScale.x} metres across {tick} "
                        + $"ticks after a bubble {drawnAt[index].x} metres across went off");
                }
            }
        }

        /// <summary>
        /// A seek re-runs the ticks it passes over in silence, so no bolt, no
        /// light and no root it passes over is drawn a second time.
        /// </summary>
        /// <remarks>
        /// The same claim the two seek tests above make, on the shapes this
        /// file's magic rows draw. <see cref="MatchDecorations.EventsHeard"/>
        /// is the counter that has to answer for it, because a clear does not
        /// reset it.
        /// </remarks>
        [Test]
        public void ASeekDrawsNoMagicSignatureASecondTime()
        {
            MatchView view = BeginWithTheMagicLines();

            RunUntil(view, () => view.Decorations.BoltsDrawn > 0 && view.Decorations.RootsDrawn > 0);

            Assert.That(view.Decorations.BoltsDrawn, Is.GreaterThan(0),
                "no magic row fired in the whole match, so this proves nothing");

            Assert.That(view.Decorations.LightsDrawn, Is.GreaterThan(0),
                "the Consecration never pulsed in the whole match, so this proves nothing");

            int tick = view.Current.Tick;
            int heard = view.Decorations.EventsHeard;

            view.ReSimulateTo(tick + 90);
            view.ReSimulateTo(tick);

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heard),
                "a seek heard the events of the ticks it re-ran, so every effect between here and the "
                + "start was drawn again");

            Assert.That(view.Decorations.BoltsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.LightsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.RootsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.StripsDrawn, Is.EqualTo(0));

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "an effect from before the seek is still on screen, and the tick it belongs to is now "
                + "in the future");
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
        /// A creep something has landed on wears it: a wash while a modifier is
        /// in force, and a bar while a pool stands in front of its health.
        /// </summary>
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
            Assert.That(
                view.Creeps.Live[slowed.Id].Marks.Wash,
                Is.EqualTo(MatchTuning.SpeedEffectTint),
                "a creep the snapshot says is slowed is drawn in its own colour");

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

            int tick = view.Current.Tick;
            int slowed = view.Current.Creeps.First(creep => creep.SpeedMagnitude != 0).Id;

            view.ReSimulateTo(tick + 60);
            view.ReSimulateTo(tick);

            Assert.That(
                view.Current.Creeps.Any(creep => creep.Id == slowed && creep.SpeedMagnitude != 0),
                Is.True,
                "the same tick played again is a different match");

            Assert.That(
                view.Creeps.Live[slowed].Marks.Wash,
                Is.EqualTo(MatchTuning.SpeedEffectTint),
                "a creep scrubbed back onto the tick it was slowed on is drawn as though it were not");
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
                    Assert.That(drawn.Marks.Wash, Is.Null, "a creep with nothing on it was washed");
                    Assert.That(drawn.Marks.Bar.gameObject.activeSelf, Is.False,
                        "a creep with no pool wears a bar");
                }

                return false;
            });

            Assert.That(view.Current.Tick, Is.GreaterThan(1000), "hardly any of the match was watched");
        }

        /// <summary>
        /// Each of the four creep auras draws its own shape, at the size the
        /// pulse reached and on the thing that pulse is about.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every one of them is centred on the body and not on what the body
        /// is holding.</b> #266 asked for a shape leaving the staff, the
        /// scythe, the broom or the axe; a walking row carries no effect anchor
        /// at all — <c>ImportedArtTests.EveryTowerFiresFromAPointOnItsOwnArt</c>
        /// asserts that it carries none, because nothing would ever resolve one
        /// — so an aura leaves the creep, which is where the emitter id the
        /// event carries resolves to.
        /// </para>
        /// <para>
        /// <b>Driven by calling the event rather than by waiting for the
        /// match to produce one</b>, the same way the tower capstones above are
        /// driven: what is under test is what the view draws when an aura
        /// pulses, and the tick a given creep first pulses on is the
        /// simulation's business. <see cref="ASeekDrawsNoCreepAuraASecondTime"/>
        /// is the one that waits for real pulses.
        /// </para>
        /// </remarks>
        [Test]
        public void EachCreepAuraDrawsItsOwnSignature()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            MatchView view = BeginWithTheCreepAuras();

            RunUntil(view, () => Walkers(view, SkeletonMage).Any() && Walkers(view, Witch).Any());

            view.Decorations.Clear();

            // The Skeleton Mage: a ring over the head of every body its haste
            // reached, itself included.
            int mage = Walkers(view, SkeletonMage).First();
            int hasteRadius = Pulses(types, SkeletonMage);
            Vector3[] hastened = Bodies(view, mage, hasteRadius);

            view.Decorations.AuraPulsed(mage, hasteRadius, BubblePayload.Speed);

            Assert.That(view.Decorations.HasteRingsDrawn, Is.EqualTo(hastened.Length),
                "the haste reached a different number of bodies than are standing inside it");

            Assert.That(view.Decorations.HasteRingsDrawn, Is.GreaterThan(0),
                "the emitter is inside its own aura, so this can never be none");

            Transform[] rings = Pieces(view, "HasteRing").ToArray();

            foreach (Vector3 body in hastened)
            {
                Vector3 over = body + (Vector3.up * MatchTuning.HasteRingHeight);

                Assert.That(rings.Min(ring => Vector3.Distance(ring.position, over)), Is.LessThan(1e-3f),
                    "a body inside the haste has no ring over its head");
            }

            Assert.That(rings[0].localScale.x, Is.EqualTo(MatchTuning.HasteRingDiameter).Within(1e-4f),
                "the ring over a hastened body is scaled by the reach of the aura rather than by itself");

            // The Necromancer: a cage over the ground its ward covered, as tall
            // as it is wide.
            int necromancer = Walkers(view, Necromancer).First();
            int wardRadius = Pulses(types, Necromancer);

            view.Decorations.AuraPulsed(necromancer, wardRadius, BubblePayload.Shield);

            Assert.That(view.Decorations.WardDomesDrawn, Is.EqualTo(1),
                "the Necromancer warded and nothing stood over it");

            Transform dome = Pieces(view, "WardDome").Single();

            Assert.That(
                Vector3.Distance(
                    dome.position,
                    view.Creeps.Live[necromancer].transform.position
                        + (Vector3.up * MatchTuning.FloorClearance)),
                Is.LessThan(1e-3f),
                "the cage is not over the body that warded");

            Assert.That(dome.localScale,
                Is.EqualTo(Vector3.one * (2f * SimUnits.MetresFromMilliHex(wardRadius))).Within(1e-4f),
                "the cage reports a radius in all three directions, so it is scaled in all three");

            // The Witch: plates on the ground out to the edge of the hex ward.
            int witch = Walkers(view, Witch).First();
            int hexRadius = Pulses(types, Witch);

            view.Decorations.AuraPulsed(witch, hexRadius, BubblePayload.Armour);

            Assert.That(view.Decorations.HexPlatesDrawn, Is.EqualTo(1),
                "the Witch pulsed and the ground stayed bare");

            Transform plates = Pieces(view, "HexPlates").Single();

            Assert.That(plates.localScale.x,
                Is.EqualTo(2f * SimUnits.MetresFromMilliHex(hexRadius)).Within(1e-4f));

            // The Frost Wight: a crown at the feet of every tower it reached.
            // The one aura on the roster that reaches the other side, so this
            // is the one shape a creep draws on a tower.
            RunUntil(view, () =>
                Walkers(view, FrostWight).Any(wight => Standings(view, wight, FrostReach(types)).Any()));

            int frostRadius = Pulses(types, FrostWight);
            int frozen = Walkers(view, FrostWight)
                .First(wight => Standings(view, wight, FrostReach(types)).Any());

            Vector3[] towers = Standings(view, frozen, FrostReach(types)).ToArray();

            view.Decorations.AuraPulsed(frozen, frostRadius, BubblePayload.Cooldown);

            Assert.That(view.Decorations.FrostSpikesDrawn, Is.EqualTo(towers.Length),
                "the frost reached a different number of towers than are standing inside it");

            Transform[] crowns = Pieces(view, "FrostSpikes").ToArray();

            foreach (Vector3 tower in towers)
            {
                Vector3 at = tower + (Vector3.up * MatchTuning.FloorClearance);

                Assert.That(crowns.Min(crown => Vector3.Distance(crown.position, at)), Is.LessThan(1e-3f),
                    "a tower inside the frostbite has no crown at its feet");
            }

            Assert.That(crowns[0].localScale.x,
                Is.EqualTo(MatchTuning.FrostCrownDiameter).Within(1e-4f),
                "the crown at a frozen tower's feet is scaled by the reach of the aura");

            Assert.That(view.Decorations.RingsDrawn, Is.EqualTo(0),
                "a creep aura fell back to the plain disc, so its binding was not read");

            // And every one of them holds the size it reported. All four stand
            // for a distance or for a body caught, and neither may close down
            // over its life.
            Transform[] shapes = { rings[0], dome, plates, crowns[0] };
            Vector3[] drawnAt = shapes.Select(shape => shape.localScale).ToArray();

            for (var tick = 1; tick < MatchTuning.WardDomeTicks; tick++)
            {
                view.Decorations.AgeOneTick();

                for (var index = 0; index < shapes.Length; index++)
                {
                    Assert.That(shapes[index].localScale, Is.EqualTo(drawnAt[index]),
                        $"{shapes[index].name} is {shapes[index].localScale.x} metres across {tick} "
                        + $"ticks after a pulse {drawnAt[index].x} metres across went out");
                }
            }
        }

        /// <summary>
        /// A seek re-runs the ticks it passes over in silence, so no ring, no
        /// cage, no plate and no crown it passes over is drawn a second time.
        /// </summary>
        /// <remarks>
        /// The same claim the seek tests above make, on the shapes the walking
        /// rows draw — and the one test here that waits for the simulation to
        /// pulse rather than calling the event itself, so it is also what says
        /// these four rows pulse at all.
        /// </remarks>
        [Test]
        public void ASeekDrawsNoCreepAuraASecondTime()
        {
            MatchView view = BeginWithTheCreepAuras();

            RunUntil(view, () =>
                view.Decorations.HasteRingsDrawn > 0
                && view.Decorations.WardDomesDrawn > 0
                && view.Decorations.HexPlatesDrawn > 0
                && view.Decorations.FrostSpikesDrawn > 0);

            Assert.That(view.Decorations.HasteRingsDrawn, Is.GreaterThan(0),
                "the Skeleton Mage never hastened anything in the whole match, so this proves nothing");

            Assert.That(view.Decorations.WardDomesDrawn, Is.GreaterThan(0),
                "the Necromancer never warded in the whole match, so this proves nothing");

            Assert.That(view.Decorations.HexPlatesDrawn, Is.GreaterThan(0),
                "the Witch never pulsed in the whole match, so this proves nothing");

            Assert.That(view.Decorations.FrostSpikesDrawn, Is.GreaterThan(0),
                "no tower was ever frostbitten in the whole match, so this proves nothing");

            int tick = view.Current.Tick;
            int heard = view.Decorations.EventsHeard;

            view.ReSimulateTo(tick + 90);
            view.ReSimulateTo(tick);

            Assert.That(view.Decorations.EventsHeard, Is.EqualTo(heard),
                "a seek heard the events of the ticks it re-ran, so every effect between here and the "
                + "start was drawn again");

            Assert.That(view.Decorations.HasteRingsDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.WardDomesDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.HexPlatesDrawn, Is.EqualTo(0));
            Assert.That(view.Decorations.FrostSpikesDrawn, Is.EqualTo(0));

            Assert.That(view.Decorations.ActiveCount, Is.EqualTo(0),
                "an effect from before the seek is still on screen, and the tick it belongs to is now "
                + "in the future");
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

                // A pool washes nothing, and the wash on one of these bodies is
                // never the pool. It is often not null: the Skeleton Mage walks
                // in this wave too, and a hastened body is washed in the same
                // colour a slowed one is -- which is the placeholder saying
                // that the sign of a speed modifier is not distinguished.
                Assert.That(
                    marks.Wash == null
                    || carrying.SpeedMagnitude != 0
                    || carrying.ArmourMagnitude != 0,
                    Is.True,
                    $"unit {unitId} is washed while nothing has moved its speed or its armour, so the "
                    + "wash is being driven by the pool");

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

        /// <summary>What the Frost Wight's frostbite carries, in metres.</summary>
        private static float FrostReach(UnitTypeTable types) =>
            SimUnits.MetresFromMilliHex(Pulses(types, FrostWight));

        /// <summary>
        /// Where every body within <paramref name="radiusMilliHex"/> of the
        /// creep with this id is drawn.
        /// </summary>
        /// <remarks>
        /// Measured flat against where the bodies are drawn, which is what the
        /// view measures a pulse's reach against — the simulation reads the same
        /// radius as a sphere, and nothing that draws a bubble asks how tall the
        /// ground is.
        /// </remarks>
        private static Vector3[] Bodies(MatchView view, int emitterId, int radiusMilliHex) =>
            Within(
                view.Creeps.Live.Values.Select(body => body.transform.position),
                view.Creeps.Live[emitterId].transform.position,
                SimUnits.MetresFromMilliHex(radiusMilliHex));

        /// <summary>
        /// Where every tower within <paramref name="metres"/> of the creep with
        /// this id stands.
        /// </summary>
        private static Vector3[] Standings(MatchView view, int emitterId, float metres) =>
            Within(
                view.Towers.Values.Select(tower => tower.transform.position),
                view.Creeps.Live[emitterId].transform.position,
                metres);

        /// <summary>
        /// Which of <paramref name="places"/> are within
        /// <paramref name="metres"/> of <paramref name="centre"/>.
        /// </summary>
        private static Vector3[] Within(IEnumerable<Vector3> places, Vector3 centre, float metres) =>
            places.Where(at => (at - centre).sqrMagnitude <= metres * metres).ToArray();

        /// <summary>
        /// Every bubble ring standing under the view, found by the name the
        /// pool gives them.
        /// </summary>
        private static IEnumerable<Transform> Rings(MatchView view) =>
            view.GetComponentsInChildren<Transform>().Where(child => child.name == "BubbleRing");
    }
}
