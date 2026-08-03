using System.Collections;
using NUnit.Framework;
using View;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// The permanent positive control for <see cref="PlayablesSamplingTests"/>.
    ///
    /// Those tests pass. This file exists to prove they pass for a reason: it
    /// rebuilds the same subject with each head-guard removed and shows the pose
    /// then *does* drift under real frames. A guard nobody has watched fail is
    /// not known to be doing anything, and a green test that cannot go red is the
    /// species this map has already killed twice.
    ///
    /// It travelled out of the spike with the component and stays permanent: the
    /// day someone deletes a guard as redundant, <see cref="NoGuards_ThePoseDriftsOnItsOwn"/>
    /// is the row that still knows what the guards were for.
    /// </summary>
    public class PlayableHeadPoisonTests
    {
        private GameObject _root;
        private Transform _bone;
        private SimDrivenAnimator _view;

        private static AnimationClip LinearClip() => OracleClips.Load(OracleClips.Linear);

        private void BuildRig(SimDrivenAnimator.HeadGuard guard)
        {
            _root = new GameObject("Rig");
            var animator = _root.AddComponent<Animator>();
            animator.applyRootMotion = false;

            var bone = new GameObject(OracleClips.BoneName);
            bone.transform.SetParent(_root.transform, false);
            _bone = bone.transform;

            _view = _root.AddComponent<SimDrivenAnimator>();
            _view.Build(animator, guard, LinearClip());
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        /// <summary>Poses at the halfway point, lets Unity tick, returns how far the pose moved on its own.</summary>
        private IEnumerator MeasureDrift(SimDrivenAnimator.HeadGuard guard, System.Action<float> report)
        {
            BuildRig(guard);
            _view.Pose(0, 0.5f);
            var settled = _bone.localPosition.x;

            for (var i = 0; i < 30; i++) yield return null;

            var drift = Mathf.Abs(_bone.localPosition.x - settled);
            Debug.Log($"[poison] guard={guard} settled={settled:0.0000} now={_bone.localPosition.x:0.0000} drift={drift:0.0000}");
            report(drift);
        }

        [UnityTest]
        public IEnumerator NoGuards_ThePoseDriftsOnItsOwn()
        {
            var drift = -1f;
            yield return MeasureDrift(SimDrivenAnimator.HeadGuard.None, d => drift = d);

            // This is the failure the whole Playables decision is meant to avoid:
            // Unity ticking the clip forward behind the view's back.
            Assert.Greater(drift, 0.01f,
                "the unguarded graph did NOT drift — the sampling tests are not proving anything, " +
                "because the thing they guard against does not happen here anyway");
        }

        [UnityTest]
        public IEnumerator ManualUpdateAlone_HoldsThePose()
        {
            var drift = -1f;
            yield return MeasureDrift(SimDrivenAnimator.HeadGuard.ManualUpdate, d => drift = d);
            Assert.Less(drift, 1e-4f, "manual update mode alone did not hold the pose");
        }

        [UnityTest]
        public IEnumerator ZeroSpeedAlone_HoldsThePose()
        {
            var drift = -1f;
            yield return MeasureDrift(SimDrivenAnimator.HeadGuard.ZeroSpeed, d => drift = d);
            Assert.Less(drift, 1e-4f, "zero clip speed alone did not hold the pose");
        }

        [UnityTest]
        public IEnumerator BothGuards_HoldThePose()
        {
            var drift = -1f;
            yield return MeasureDrift(SimDrivenAnimator.HeadGuard.Both, d => drift = d);
            Assert.Less(drift, 1e-4f, "the shipping configuration did not hold the pose");
        }
    }
}
