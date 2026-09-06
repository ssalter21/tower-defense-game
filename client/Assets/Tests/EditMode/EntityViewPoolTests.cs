using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// Pooling by id-matching: an entity that vanished is an id that stopped
    /// appearing, and nothing else in the project has an opinion about it.
    /// </summary>
    /// <remarks>
    /// The property under test is an absence — there is no despawn message, no
    /// death callback and no second bookkeeping path — and an absence is
    /// awkward to assert. What these do instead is drive the pool the way a
    /// churning match drives it and check that the arithmetic of subtraction
    /// comes out right in the cases that would otherwise need special handling.
    /// </remarks>
    public class EntityViewPoolTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in _made)
            {
                if (made != null) UnityEngine.Object.DestroyImmediate(made);
            }

            _made.Clear();
        }

        private EntityViewPool<Transform> Pool()
        {
            return new EntityViewPool<Transform>(() =>
            {
                var host = new GameObject("view");
                _made.Add(host);

                return host.transform;
            });
        }

        /// <summary>Drives one frame: these ids exist, and nothing else does.</summary>
        private static void Sync(EntityViewPool<Transform> pool, params int[] ids)
        {
            pool.BeginSync();

            foreach (int id in ids)
            {
                pool.Claim(id);
            }

            pool.EndSync();
        }

        [Test]
        public void AnIdThatKeepsAppearingKeepsItsObject()
        {
            EntityViewPool<Transform> pool = Pool();

            Sync(pool, 1, 2, 3);
            Transform two = pool.Live[2];

            Sync(pool, 1, 2, 3);

            Assert.That(pool.Live[2], Is.SameAs(two), "an entity that never left was given a new object");
        }

        /// <summary>
        /// The whole mechanism, in one assertion: nobody said the entity died,
        /// and its object went back anyway.
        /// </summary>
        [Test]
        public void AnIdThatStopsAppearingIsReleasedWithoutBeingTold()
        {
            EntityViewPool<Transform> pool = Pool();

            Sync(pool, 1, 2, 3);
            Assert.That(pool.LiveCount, Is.EqualTo(3));

            Sync(pool, 1, 3);

            Assert.That(pool.LiveCount, Is.EqualTo(2));
            Assert.That(pool.Live.ContainsKey(2), Is.False);
            Assert.That(pool.IdleCount, Is.EqualTo(1));
        }

        [Test]
        public void AReleasedObjectIsReusedRatherThanRebuilt()
        {
            EntityViewPool<Transform> pool = Pool();

            Sync(pool, 1);
            Transform first = pool.Live[1];

            Sync(pool);
            Assert.That(pool.EverCreated, Is.EqualTo(1));

            Sync(pool, 7);

            Assert.That(pool.Live[7], Is.SameAs(first), "the idle object was not reused");
            Assert.That(pool.EverCreated, Is.EqualTo(1), "a second object was built while one sat idle");
        }

        /// <summary>
        /// The number that stops climbing. Over a match this is what says
        /// pooling is doing anything at all: it settles just above the busiest
        /// moment the match ever had, and nowhere near the number of entities
        /// the match contained.
        /// </summary>
        /// <remarks>
        /// <b>Four objects for three concurrent entities, and the extra one is
        /// inherent rather than waste.</b> Within a sync, claims come before
        /// releases — and they have to, because until every claim is in there
        /// is no way to know which ids stopped appearing. So on the frame where
        /// entity 4 arrives and entity 1 leaves, the arrival is served while
        /// entity 1's object is still live, and the pool builds one more. From
        /// then on it never builds another: the fourth object absorbs every
        /// subsequent rotation.
        /// </remarks>
        [Test]
        public void TheObjectCountSettlesJustAboveTheBusiestMoment()
        {
            EntityViewPool<Transform> pool = Pool();

            // Forty entities, never more than three at once -- the shape of a
            // wave walking past.
            for (int id = 1; id <= 40; id++)
            {
                Sync(pool, id, id + 1, id + 2);
            }

            Assert.That(pool.EverCreated, Is.EqualTo(4),
                "objects were built per entity, not per concurrent entity");
        }

        /// <summary>
        /// The property that actually matters, stated without depending on the
        /// exact steady-state number: it stops growing.
        /// </summary>
        [Test]
        public void TheObjectCountStopsGrowing()
        {
            EntityViewPool<Transform> pool = Pool();

            for (int id = 1; id <= 10; id++)
            {
                Sync(pool, id, id + 1, id + 2);
            }

            int settled = pool.EverCreated;

            for (int id = 11; id <= 500; id++)
            {
                Sync(pool, id, id + 1, id + 2);
            }

            Assert.That(pool.EverCreated, Is.EqualTo(settled),
                "the pool was still building objects five hundred entities in");
        }

        /// <summary>
        /// The hardest case in the whole contract, and it needs no code: a
        /// projectile whose target died mid-flight is an id that stopped
        /// appearing, exactly like a creep that walked off the end.
        /// </summary>
        [Test]
        public void AnEntityThatVanishesMidFlightIsNoDifferentFromAnyOther()
        {
            EntityViewPool<Transform> pool = Pool();

            Sync(pool, 100, 200);
            Transform shell = pool.Live[200];

            // The creep it was aimed at died, so the simulation dropped the
            // projectile. Nothing told the view.
            Sync(pool, 100);

            Assert.That(pool.Live.ContainsKey(200), Is.False);
            Assert.That(shell.gameObject.activeSelf, Is.False, "the object is still being drawn");
        }

        [Test]
        public void AnEmptySnapshotReleasesEverything()
        {
            EntityViewPool<Transform> pool = Pool();

            Sync(pool, 1, 2, 3, 4);
            Sync(pool);

            Assert.That(pool.LiveCount, Is.EqualTo(0));
            Assert.That(pool.IdleCount, Is.EqualTo(4));
        }

        [Test]
        public void ReleaseAllIsTheSameAsAnEmptySnapshot()
        {
            EntityViewPool<Transform> pool = Pool();

            Sync(pool, 1, 2, 3);
            pool.ReleaseAll();

            Assert.That(pool.LiveCount, Is.EqualTo(0));
            Assert.That(pool.IdleCount, Is.EqualTo(3));
        }

        /// <summary>
        /// Two entities sharing an id means one is invisible and the other is
        /// posed twice. Loud, because it is silent otherwise.
        /// </summary>
        [Test]
        public void ClaimingTheSameIdTwiceInOneSyncThrows()
        {
            EntityViewPool<Transform> pool = Pool();

            pool.BeginSync();
            pool.Claim(5);

            Assert.Throws<InvalidOperationException>(() => pool.Claim(5));
        }

        /// <summary>
        /// Without a sync there is no record of what was claimed, so an EndSync
        /// would release everything still alive. Refused rather than tolerated.
        /// </summary>
        [Test]
        public void ClaimingOutsideASyncThrows()
        {
            EntityViewPool<Transform> pool = Pool();

            Assert.Throws<InvalidOperationException>(() => pool.Claim(1));
        }

        [Test]
        public void ASyncThatNeverEndedIsRefusedRatherThanNested()
        {
            EntityViewPool<Transform> pool = Pool();

            pool.BeginSync();

            Assert.Throws<InvalidOperationException>(pool.BeginSync);
        }

        /// <summary>
        /// A body that changes what it is keeps its id and gives up its view.
        /// The Cursed Villager becomes the Werewolf, so a creep's variant is its
        /// unit type and its unit type can move — and this used to throw.
        /// </summary>
        [Test]
        public void AnIdThatComesBackAsAnotherVariantIsDrawnByAnotherView()
        {
            EntityViewPool<Transform> pool = Pool();

            pool.BeginSync();
            Transform villager = pool.Claim(1, variant: 47);
            pool.EndSync();

            pool.BeginSync();
            Transform werewolf = pool.Claim(1, variant: 48);
            pool.EndSync();

            Assert.That(werewolf, Is.Not.SameAs(villager), "the body is still being drawn as what it was");
            Assert.That(pool.LiveCount, Is.EqualTo(1), "one body is being drawn twice");
            Assert.That(pool.EverCreated, Is.EqualTo(2), "a Werewolf was drawn with a Villager's view");

            // The view it gave up is idle rather than lost, so the next body of
            // that row reuses it instead of building one.
            pool.BeginSync();
            Transform another = pool.Claim(2, variant: 47);
            pool.Claim(1, variant: 48);
            pool.EndSync();

            Assert.That(another, Is.SameAs(villager), "the view a transformed body gave up was thrown away");
            Assert.That(pool.EverCreated, Is.EqualTo(2));
        }

        /// <summary>
        /// And the view it gives up is on its stack in time for the rest of the
        /// same sync to take it, which is what a column of bodies transforming
        /// while another one spawns behind them looks like.
        /// </summary>
        [Test]
        public void TheViewAChangedBodyGivesUpIsAvailableInsideTheSameSync()
        {
            EntityViewPool<Transform> pool = Pool();

            pool.BeginSync();
            Transform villager = pool.Claim(1, variant: 47);
            pool.EndSync();

            pool.BeginSync();
            pool.Claim(1, variant: 48);
            Transform arriving = pool.Claim(2, variant: 47);
            pool.EndSync();

            Assert.That(arriving, Is.SameAs(villager), "the view was not free until the sync ended");
            Assert.That(pool.LiveCount, Is.EqualTo(2));
            Assert.That(pool.IdleCount, Is.EqualTo(0));
            Assert.That(pool.EverCreated, Is.EqualTo(2), "a third view was built for two bodies");
        }
    }
}
