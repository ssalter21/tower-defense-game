using System.Linq;
using NUnit.Framework;
using UnityEngine;
using View;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    /// bow is put on the Ranger's hand here and then the rig is posed, because
    /// "it is parented to something" and "it is in the hand through the whole
    /// draw" are different claims and only the second one is the pipeline.
    /// </summary>
    public class WeaponSocketTests
    {
        private const string RangerPath = ImportedArtTests.RangerPath;
        private const string BowPath = ImportedArtTests.BowPath;
        private const string RangedBankPath = ImportedArtTests.RangedBankPath;

        /// <summary>The wind-up clip: the hand travels furthest during this one.</summary>
        private const string DrawClip = "Ranged_Bow_Draw";

        private GameObject _ranger;

        [TearDown]
        public void TearDown()
        {
            if (_ranger != null) Object.DestroyImmediate(_ranger);
        }

#if UNITY_EDITOR
        private GameObject BuildArmedRanger(out GameObject bow, out Transform hand)
        {
            var rangerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RangerPath);
            Assert.IsNotNull(rangerPrefab, $"could not load {RangerPath}");

            var bowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BowPath);
            Assert.IsNotNull(bowPrefab, $"could not load {BowPath}");

            _ranger = Object.Instantiate(rangerPrefab);
            bow = WeaponSocket.Attach(_ranger, bowPrefab, WeaponSocket.BowHand);
            hand = WeaponSocket.FindBone(_ranger, WeaponSocket.BowHand);

            return _ranger;
        }

        [Test]
        public void TheBowParentsToTheLeftHandBone()
        {
            BuildArmedRanger(out GameObject bow, out Transform hand);

            Assert.IsNotNull(hand, $"no '{WeaponSocket.BowHand}' bone on the Ranger");
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
            BuildArmedRanger(out GameObject bow, out Transform _);

            // The measurement that settled this is on #44: on handslot.r the bow's
            // bounding box lands wholly inside the Ranger's own — an archer
            // holding nothing. Asserted as a string so a later "tidy-up" that
            // mirrors it back to the right hand fails here rather than in a
            // screenshot nobody takes.
            Assert.AreEqual("handslot.l", WeaponSocket.BowHand);
            Assert.AreEqual("handslot.l", bow.transform.parent.name);
            Assert.IsNotNull(WeaponSocket.FindBone(_ranger, "handslot.r"),
                "the rig has no handslot.r either — the bone naming is not what this assertion assumes");
        }

        [Test]
        public void TheBowRidesTheHandThroughTheWholeDraw()
        {
            BuildArmedRanger(out GameObject bow, out Transform hand);

            AnimationClip draw = AssetDatabase.LoadAllAssetsAtPath(RangedBankPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == DrawClip);
            Assert.IsNotNull(draw, $"no '{DrawClip}' clip in {RangedBankPath}");

            // Deliberately not `??`: Unity's fake-null overrides == but not the
            // null-coalescing operator.
            var animator = _ranger.GetComponent<Animator>();
            if (animator == null) animator = _ranger.AddComponent<Animator>();
            animator.applyRootMotion = false;

            var poser = _ranger.AddComponent<SimDrivenAnimator>();
            poser.Build(animator, draw);

            var handWorld = new Vector3[3];
            var bowWorld = new Vector3[3];
            float[] phases = { 0f, 0.5f, 1f };

            for (var i = 0; i < phases.Length; i++)
            {
                poser.SampleSingle(0, phases[i], draw.length);
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

            Debug.Log($"[weapon] bow travelled {travelled:F3} m across '{DrawClip}' on {WeaponSocket.BowHand}");
        }

        [Test]
        public void AttachingToABoneThatIsNotThereThrowsRatherThanHangingItOffTheRoot()
        {
            var rangerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RangerPath);
            var bowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BowPath);
            _ranger = Object.Instantiate(rangerPrefab);

            Assert.Throws<System.InvalidOperationException>(
                () => WeaponSocket.Attach(_ranger, bowPrefab, "handslot.left"));
        }
#endif
    }
}
