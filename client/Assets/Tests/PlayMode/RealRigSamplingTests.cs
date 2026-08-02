using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using View;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests.PlayMode
{
    /// <summary>
    /// The other half of the Playables validation: the same purity claims, but on the
    /// real imported KayKit rig and a real imported clip rather than a synthetic one.
    ///
    /// The synthetic tests prove the API samples a curve purely. These prove the claim
    /// survives an asset-pack import — a skinned mesh, a bone hierarchy, and clips that
    /// arrived from an FBX authored in Blender.
    /// </summary>
    public class RealRigSamplingTests
    {
        private const string CharacterPath = "Assets/Art/Characters/Skeleton_Warrior.fbx";
        private const string MovementPath = "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx";
        private const string ClipName = "Walking_A";

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

#if UNITY_EDITOR
        private static AnimationClip LoadClip(string path, string name) =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == name);

        /// <summary>Poses the rig and snapshots every bone, so purity can be compared over the whole skeleton.</summary>
        private static Vector3[] Snapshot(IReadOnlyList<Transform> bones)
        {
            var pose = new Vector3[bones.Count * 2];
            for (var i = 0; i < bones.Count; i++)
            {
                pose[i * 2] = bones[i].localPosition;
                pose[i * 2 + 1] = bones[i].localRotation.eulerAngles;
            }
            return pose;
        }

        private (SimDrivenAnimator view, List<Transform> bones, float length) BuildRealRig()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            Assert.IsNotNull(prefab, $"could not load {CharacterPath}");

            var clip = LoadClip(MovementPath, ClipName);
            Assert.IsNotNull(clip, $"could not find clip '{ClipName}' in {MovementPath}");

            _instance = Object.Instantiate(prefab);
            // Deliberately not `??`: Unity's fake-null overrides == but not the null-coalescing
            // operator, so `GetComponent() ?? AddComponent()` hands back the fake-null.
            var animator = _instance.GetComponent<Animator>();
            if (animator == null) animator = _instance.AddComponent<Animator>();
            animator.applyRootMotion = false;

            var view = _instance.AddComponent<SimDrivenAnimator>();
            view.Build(animator, clip);

            var bones = _instance.GetComponentsInChildren<Transform>(true).ToList();
            return (view, bones, clip.length);
        }

        [Test]
        public void TheClipActuallyDrivesTheImportedRig()
        {
            var (view, bones, length) = BuildRealRig();

            view.SampleSingle(0, 0f, length);
            var atStart = Snapshot(bones);

            view.SampleSingle(0, 0.5f, length);
            var atMiddle = Snapshot(bones);

            var moved = atStart.Where((v, i) => (v - atMiddle[i]).sqrMagnitude > 1e-8f).Count();

            // Without this the purity tests below would pass trivially on a rig where
            // nothing is bound and every bone sits still.
            Assert.Greater(moved, 0,
                "no bone moved between phase 0 and 0.5 — the clip is not bound to this hierarchy at all");
            Debug.Log($"[realrig] bones={bones.Count} channels moved between phase 0 and 0.5: {moved}");
        }

        [Test]
        public void SamplingTheRealClip_IsPathIndependent()
        {
            var (view, bones, length) = BuildRealRig();

            foreach (var p in new[] { 0f, 0.1f, 0.25f, 0.4f }) view.SampleSingle(0, p, length);
            var fromBelow = Snapshot(bones);

            foreach (var p in new[] { 1f, 0.8f, 0.6f, 0.4f }) view.SampleSingle(0, p, length);
            var fromAbove = Snapshot(bones);

            for (var i = 0; i < fromBelow.Length; i++)
            {
                Assert.AreEqual(fromBelow[i].x, fromAbove[i].x, 1e-4f, $"channel {i} depends on approach direction");
                Assert.AreEqual(fromBelow[i].y, fromAbove[i].y, 1e-4f, $"channel {i} depends on approach direction");
                Assert.AreEqual(fromBelow[i].z, fromAbove[i].z, 1e-4f, $"channel {i} depends on approach direction");
            }
        }

        [Test]
        public void ScrubbingTheRealClipBackwards_RetracesTheSamePoses()
        {
            var (view, bones, length) = BuildRealRig();

            var phases = new[] { 0f, 0.2f, 0.4f, 0.6f, 0.8f };
            var forward = new List<Vector3[]>();
            foreach (var p in phases)
            {
                view.SampleSingle(0, p, length);
                forward.Add(Snapshot(bones));
            }

            // Walk the same phases in reverse; each pose must match what it was going forwards.
            for (var i = phases.Length - 1; i >= 0; i--)
            {
                view.SampleSingle(0, phases[i], length);
                var back = Snapshot(bones);
                for (var c = 0; c < back.Length; c++)
                {
                    Assert.AreEqual(forward[i][c].x, back[c].x, 1e-4f, $"phase {phases[i]} channel {c} differs on the way back");
                    Assert.AreEqual(forward[i][c].y, back[c].y, 1e-4f, $"phase {phases[i]} channel {c} differs on the way back");
                    Assert.AreEqual(forward[i][c].z, back[c].z, 1e-4f, $"phase {phases[i]} channel {c} differs on the way back");
                }
            }
        }

        [Test]
        public void RealClipsCarryNoRootMotion()
        {
            // Locomotion phase is driven from distance travelled in the sim, so any
            // clip-owned translation would be authoritative progress living in the view.
            foreach (var path in new[] { MovementPath, "Assets/Art/Animations/Rig_Medium_General.fbx" })
            {
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    Assert.IsFalse(clip.hasRootCurves, $"{clip.name} carries root curves");
                    Assert.IsFalse(clip.hasMotionCurves, $"{clip.name} carries motion curves");
                    Assert.IsFalse(clip.hasGenericRootTransform, $"{clip.name} carries a generic root transform");
                }
            }
        }
#endif
    }
}
