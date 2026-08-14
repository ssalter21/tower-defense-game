using System.Linq;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The weapon half of the asset pipeline, which is the half a single drag of
    /// a single file never exercises.
    ///
    /// A character is one import and a building is one import. A weapon is two
    /// imports that have to agree about a bone name, a local transform and an
    /// atlas — and the wrong answer to any of the three still instantiates,
    /// still renders and still passes every other test in this project. So the
    /// bow is put on the hand of the tower that fires one, and then the rig is
    /// posed, because "it is parented to something" and "it is in the hand
    /// through the whole draw" are different claims and only the second one is
    /// the pipeline.
    /// </summary>
    public class WeaponSocketTests
    {
        private GameObject _holder;

        [TearDown]
        public void TearDown()
        {
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        private GameObject BuildArmedTower(out GameObject bow, out Transform hand)
        {
            MatchArt art = TheMatchOnScreen.Art();

            _holder = Object.Instantiate(BowHolderModel());
            bow = WeaponSocket.Attach(_holder, art.BowModel, WeaponSocket.BowHand);
            hand = WeaponSocket.FindBone(_holder, WeaponSocket.BowHand);

            return _holder;
        }

        /// <summary>
        /// The model of the one tower that actually holds a bow: the projectile
        /// one. Found through the unit table rather than named, because models
        /// are per unit type and the row that draws a bow is a content fact.
        /// </summary>
        private static GameObject BowHolderModel()
        {
            UnitType holder = StreamingContent.ReadUnitTypes()
                .Types
                .First(t => t.Delivery == Delivery.Projectile);

            return TheMatchOnScreen.Art().ModelFor(holder.Id);
        }

        [Test]
        public void TheBowParentsToTheLeftHandBone()
        {
            BuildArmedTower(out GameObject bow, out Transform hand);

            Assert.IsNotNull(hand, $"no '{WeaponSocket.BowHand}' bone on {_holder.name}");
            Assert.AreSame(hand, bow.transform.parent,
                $"the bow's parent is '{bow.transform.parent?.name}', not '{WeaponSocket.BowHand}'");

            // Zero offset is the contract, not an incidental starting value: the
            // pack authors the slot bone to be exactly where the held thing goes.
            Assert.AreEqual(Vector3.zero, bow.transform.localPosition);
            Assert.Less(Quaternion.Angle(Quaternion.identity, bow.transform.localRotation), 1e-3f,
                "the bow was rolled relative to the slot bone");
            Assert.AreEqual(Vector3.one, bow.transform.localScale);
        }

        [Test]
        public void TheBowIsOnTheLeftHandSlot_NotTheRightOne()
        {
            BuildArmedTower(out GameObject bow, out Transform _);

            // The measurement that settled this is on #44: on handslot.r the bow's
            // bounding box lands wholly inside the character's own — an archer
            // holding nothing. Asserted as a string so a later "tidy-up" that
            // mirrors it back to the right hand fails here rather than in a
            // screenshot nobody takes.
            Assert.AreEqual("handslot.l", WeaponSocket.BowHand);
            Assert.AreEqual("handslot.l", bow.transform.parent.name);
            Assert.IsNotNull(WeaponSocket.FindBone(_holder, "handslot.r"),
                "the rig has no handslot.r either — the bone naming is not what this assertion assumes");
        }

        [Test]
        public void TheBowRidesTheHandThroughTheWholeDraw()
        {
            BuildArmedTower(out GameObject bow, out Transform hand);

            // The wind-up clip: the hand travels furthest during this one.
            AnimationClip draw = TheMatchOnScreen.Art().TowerWindupClip;

            SimDrivenAnimator poser = SimDrivenAnimator.Bind(_holder, draw);

            var handWorld = new Vector3[3];
            var bowWorld = new Vector3[3];
            float[] phases = { 0f, 0.5f, 1f };

            for (var i = 0; i < phases.Length; i++)
            {
                poser.Pose(0, phases[i]);
                handWorld[i] = hand.position;
                bowWorld[i] = bow.transform.position;

                Assert.AreEqual(handWorld[i].x, bowWorld[i].x, 1e-4f, $"bow left the hand in x at phase {phases[i]}");
                Assert.AreEqual(handWorld[i].y, bowWorld[i].y, 1e-4f, $"bow left the hand in y at phase {phases[i]}");
                Assert.AreEqual(handWorld[i].z, bowWorld[i].z, 1e-4f, $"bow left the hand in z at phase {phases[i]}");
            }

            // Without this the three assertions above would pass on a rig that
            // never moved, and on a bow parented to the root object instead of
            // the hand — the exact failure this test exists for.
            float travelled = (bowWorld[0] - bowWorld[2]).magnitude;
            Assert.Greater(travelled, 0.05f,
                "the bow did not move at all across the draw — it is not riding a bone that the clip animates");

            Debug.Log($"[weapon] bow travelled {travelled:F3} m across '{draw.name}' on {WeaponSocket.BowHand}");
        }

        [Test]
        public void AttachingToABoneThatIsNotThereThrowsRatherThanHangingItOffTheRoot()
        {
            MatchArt art = TheMatchOnScreen.Art();
            _holder = Object.Instantiate(BowHolderModel());

            Assert.Throws<System.InvalidOperationException>(
                () => WeaponSocket.Attach(_holder, art.BowModel, "handslot.left"));
        }
    }
}
