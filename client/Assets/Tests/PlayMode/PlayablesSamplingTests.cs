using System.Collections;
using NUnit.Framework;
using View;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// Validates the bet made by the sim-to-view contract (#8): that Unity animation
    /// can be driven from simulation time via the Playables API, with no view-side
    /// playback head that could desync from the sim.
    ///
    /// Every test here uses a synthetic clip with an exact analytic oracle
    /// (bone x == 10 * t), so "is the pose correct" is an assertion rather than a
    /// judgement. No art asset is involved: the questions these tests answer are
    /// about the graph, not about any particular model.
    ///
    /// The clips are loaded rather than built here — see <see cref="OracleClips"/>
    /// for why, which is that building one is an editor-only trick that fails
    /// silently everywhere else.
    /// </summary>
    public class PlayablesSamplingTests
    {
        private const float ClipLength = OracleClips.Length;
        private const float TravelPerSecond = OracleClips.Travel;

        private GameObject _root;
        private Transform _bone;
        private SimDrivenAnimator _view;

        /// <summary>A clip whose only channel is a straight line, so the correct pose at t is known exactly.</summary>
        private static AnimationClip LinearClip() => OracleClips.Load(OracleClips.Linear);

        /// <summary>The two ends a mixer blend is measured between.</summary>
        private static AnimationClip ConstantAtZero() => OracleClips.Load(OracleClips.ConstantZero);

        private static AnimationClip ConstantAtTravel() => OracleClips.Load(OracleClips.ConstantTravel);

        private void BuildRig(params AnimationClip[] clips)
        {
            _root = new GameObject("Rig");
            var animator = _root.AddComponent<Animator>();
            animator.applyRootMotion = false;

            var bone = new GameObject(OracleClips.BoneName);
            bone.transform.SetParent(_root.transform, false);
            _bone = bone.transform;

            _view = _root.AddComponent<SimDrivenAnimator>();
            _view.Build(animator, clips);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private float X => _bone.localPosition.x;

        // ------------------------------------------------------------------
        // Q1: can a clip be sampled at an arbitrary time?
        // ------------------------------------------------------------------

        [Test]
        public void SamplingAtArbitraryTime_MatchesAnalyticOracle()
        {
            BuildRig(LinearClip());

            foreach (var phase in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                _view.Pose(0, phase);
                Assert.AreEqual(phase * TravelPerSecond, X, 1e-4f,
                    $"pose at phase {phase} does not match the oracle");
            }
        }

        [Test]
        public void SamplingOutOfOrder_IsPathIndependent()
        {
            BuildRig(LinearClip());

            // Arrive at 0.3 from below.
            foreach (var p in new[] { 0f, 0.1f, 0.2f, 0.3f }) _view.Pose(0, p);
            var fromBelow = X;

            // Arrive at the same 0.3 from above.
            foreach (var p in new[] { 1f, 0.9f, 0.6f, 0.3f }) _view.Pose(0, p);
            var fromAbove = X;

            // Arrive at it cold, with a fresh graph.
            TearDown();
            BuildRig(LinearClip());
            _view.Pose(0, 0.3f);
            var cold = X;

            Assert.AreEqual(fromBelow, fromAbove,
                "the pose at t depends on which times were sampled before it — there is a playback head");
            Assert.AreEqual(fromBelow, cold,
                "the pose at t on a warm graph differs from a cold one — there is residual state");
        }

        [Test]
        public void ScrubbingBackwards_MovesThePoseBackwards()
        {
            BuildRig(LinearClip());

            var previous = float.MaxValue;
            for (var phase = 1f; phase >= 0f; phase -= 0.05f)
            {
                _view.Pose(0, phase);
                Assert.Less(X, previous, $"pose did not move backwards at phase {phase}");
                previous = X;
            }
        }

        [Test]
        public void RandomAccessFuzz_AlwaysMatchesTheOracle()
        {
            BuildRig(LinearClip());

            var rng = new System.Random(12345);
            for (var i = 0; i < 500; i++)
            {
                var phase = (float)rng.NextDouble();
                _view.Pose(0, phase);
                Assert.AreEqual(phase * TravelPerSecond, X, 1e-4f,
                    $"random-access sample {i} at phase {phase} drifted from the oracle");
            }
        }

        // ------------------------------------------------------------------
        // The accumulator ban: is there a playback head anywhere?
        // ------------------------------------------------------------------

        [Test]
        public void RepeatedEvaluationWithoutSettingTime_DoesNotAdvance()
        {
            BuildRig(LinearClip());
            _view.Pose(0, 0.5f);
            var settled = X;

            for (var i = 0; i < 100; i++)
            {
                _view.Pose(0, 0.5f);
            }

            Assert.AreEqual(settled, X, "the pose drifted under repeated evaluation — something is accumulating");
        }

        [UnityTest]
        public IEnumerator PoseDoesNotDriftAcrossRealFrames()
        {
            BuildRig(LinearClip());
            _view.Pose(0, 0.5f);
            var settled = X;

            // Let Unity tick for real. Nothing should move the rig, because the
            // graph is in Manual mode and only a Pose call evaluates it.
            for (var i = 0; i < 30; i++)
            {
                yield return null;
            }

            Assert.AreEqual(settled, X, 1e-6f,
                "the pose moved while Unity ticked frames — the graph has its own playback head");
        }

        // ------------------------------------------------------------------
        // Q2: blending and switching without an Animator Controller
        // ------------------------------------------------------------------

        [Test]
        public void MixerWeights_BlendLinearlyBetweenClips()
        {
            BuildRig(ConstantAtZero(), ConstantAtTravel());

            foreach (var w in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
            {
                _view.PoseBlend(0, 0f, 1, 0f, w);
                Assert.AreEqual(w * TravelPerSecond, X, 1e-4f,
                    $"blend at weight {w} is not the linear combination of the two clips");
            }
        }

        [Test]
        public void SwitchingClipsViaWeights_IsAlsoPathIndependent()
        {
            BuildRig(ConstantAtZero(), ConstantAtTravel());

            // Walk the weight up, then back down to the same place.
            foreach (var w in new[] { 0f, 0.5f, 1f, 0.5f }) _view.PoseBlend(0, 0f, 1, 0f, w);
            var afterRoundTrip = X;

            _view.PoseBlend(0, 0f, 1, 0f, 0.5f);
            Assert.AreEqual(afterRoundTrip, X, "switching left residue behind in the mixer");
        }

        [Test]
        public void ClipsInOneGraph_HaveIndependentTimes()
        {
            // walk on slot 0, "hit" on slot 1 — the two clips must not share a head.
            BuildRig(LinearClip(), LinearClip());

            // Both slots given a time, then all the weight put on one and all of
            // it on the other. A slot reading the wrong clip's head would show
            // up as the other slot's answer.
            _view.PoseBlend(0, 0.2f, 1, 0.9f, 0f);
            var slot0Only = X;

            _view.PoseBlend(0, 0.2f, 1, 0.9f, 1f);
            var slot1Only = X;

            Assert.AreEqual(2f, slot0Only, 1e-4f, "slot 0 did not read its own time");
            Assert.AreEqual(9f, slot1Only, 1e-4f, "slot 1 did not read its own time");
        }

        // ------------------------------------------------------------------
        // Q4: root motion — the API the import check will use
        // ------------------------------------------------------------------

        [Test]
        public void RootMotionCurves_AreDetectableOnAClip()
        {
            var clip = LinearClip();

            // These are the flags the asset-import half of this ticket inspects on
            // the real FBX clips. On a synthetic clip that animates a child bone
            // only, all three must be false.
            Assert.IsFalse(clip.hasRootCurves, "synthetic clip unexpectedly has root curves");
            Assert.IsFalse(clip.hasMotionCurves, "synthetic clip unexpectedly has motion curves");
            Assert.IsFalse(clip.hasGenericRootTransform, "synthetic clip unexpectedly has a generic root transform");
        }
    }
}
