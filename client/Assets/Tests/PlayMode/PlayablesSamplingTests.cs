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
        private GameObject _root;
        private Transform _bone;
        private SimDrivenAnimator _view;

        /// <summary>A clip whose only channel is a straight line, so the correct pose at t is known exactly.</summary>
        private static AnimationClip LinearClip() => OracleClips.Load(OracleClips.Linear);

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
    }
}
