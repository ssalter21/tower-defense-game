using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    public class MatchViewTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

#if UNITY_EDITOR
        /// <summary>A match, drawn, with nobody watching it.</summary>
        private MatchView Begin()
        {
            var host = new GameObject("MatchViewTest");
            _spawned.Add(host);

            return TheMatchOnScreen.Begin(host);
        }

        /// <summary>
        /// Steps the match, drawing every tick, until <paramref name="stop"/>
        /// says so or the match ends.
        /// </summary>
        private static void RunUntil(MatchView view, System.Func<bool> stop) =>
            TheMatchOnScreen.RunUntil(view, stop);

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
        /// </remarks>
        [Test]
        public void ObjectsArePooledAcrossTheWholeMatch()
        {
            MatchView view = Begin();
            int mostAtOnce = 0;
            int halfway = 0;

            RunUntil(view, () =>
            {
                mostAtOnce = Mathf.Max(mostAtOnce, view.Creeps.LiveCount);

                if (view.Current.Tick == 1000)
                {
                    halfway = view.Creeps.EverCreated;
                }

                return false;
            });

            int total = StreamingContent.ReadWave(StreamingContent.ReadUnitTypes()).TotalUnits;

            Assert.That(mostAtOnce, Is.GreaterThan(1), "the match never had two creeps on it at once");

            Assert.That(view.Creeps.EverCreated, Is.LessThanOrEqualTo(mostAtOnce + 1),
                "more objects were built than were ever alive at once, so something is not being reused");

            Assert.That(view.Creeps.EverCreated, Is.LessThan(total),
                $"{view.Creeps.EverCreated} objects for {total} creeps is one per creep, not a pool");

            Assert.That(view.Creeps.EverCreated, Is.EqualTo(halfway),
                "the pool was still building objects in the second half of the match");
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

            RunUntil(view, () => view.Creeps.LiveCount > 0);

            foreach (TowerView tower in view.Towers.Values)
            {
                AssertAuthoredRotation(tower.Model, ModelPathOf(tower));
            }

            CreepView creep = view.Creeps.Live.Values.First();
            AssertAuthoredRotation(creep.Model, "Assets/Art/Characters/Skeleton_Warrior.fbx");
        }

        private static string ModelPathOf(TowerView tower) =>
            tower.Type.Delivery == Delivery.Projectile
                ? "Assets/Art/Characters/Ranger.fbx"
                : "Assets/Art/Buildings/building_tower_A_blue.fbx";

        /// <summary>
        /// The instantiated model carries the same local rotation the imported
        /// asset does — measured off the asset rather than written down here,
        /// so the assertion cannot disagree with the import it describes.
        /// </summary>
        private static void AssertAuthoredRotation(GameObject instance, string assetPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            Assert.IsNotNull(prefab, $"nothing imported at {assetPath}");

            Assert.That(
                Quaternion.Angle(instance.transform.localRotation, prefab.transform.localRotation),
                Is.LessThan(0.01f),
                $"{assetPath} is being drawn rotated {Quaternion.Angle(instance.transform.localRotation, prefab.transform.localRotation):F1} "
                + "degrees away from how it was imported — a model whose root rotation was overwritten "
                + "lies on its side and nothing else in this suite notices");
        }

        [Test]
        public void TheTwoKindsOfTowerAreBuiltDifferently()
        {
            MatchView view = Begin();

            Assert.That(view.Towers.Count, Is.EqualTo(6), "the defense has six towers");

            foreach (TowerView tower in view.Towers.Values)
            {
                if (tower.Type.Delivery == Delivery.Projectile)
                {
                    Assert.That(tower.IsAnimated, Is.True, "the projectile tower is a skinned character");
                    Assert.That(tower.Weapon, Is.Not.Null, "it draws a bow, so it has to be holding one");
                    Assert.That(
                        tower.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                        Is.Not.Empty);
                }
                else
                {
                    Assert.That(tower.IsAnimated, Is.False, "the hitscan tower is a static building");
                    Assert.That(
                        tower.Model.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                        Is.Empty,
                        "the building arrived skinned, so both halves of the pipeline are the same half");
                }
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

                    // Recomputed from the target's position in this snapshot,
                    // and from nothing else at all.
                    Vector3 origin = ProjectileView.OriginFor(targetAt, view.Route.TangentAt(distanceAlong));

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

                    if (closing.TryGetValue(shell.Id, out float was))
                    {
                        Assert.That(gap, Is.LessThan(was),
                            $"shell {shell.Id} did not close on its target this tick");
                    }

                    closing[shell.Id] = gap;

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
        /// Nothing in the match turns to face the camera, so yawing through all
        /// six snaps is a real check rather than a formality.
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
#endif
    }
}
