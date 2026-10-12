using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using View;
using UnityEngine;

namespace Tests.PlayMode
{
    /// <summary>
    /// The Playables validation on the real imported KayKit rig and a real imported
    /// clip rather than a synthetic one: the clip actually binds to and moves the
    /// skeleton an asset-pack import produced.
    ///
    /// What the import <i>settings</i> are is a different question, and it is asserted
    /// in the edit-mode suite where the importer can actually be asked. This is about
    /// the pose, and a pose is a runtime fact.
    /// </summary>
    public class RealRigSamplingTests
    {
        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null) Object.DestroyImmediate(_instance);
        }

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

        private (SimDrivenAnimator view, List<Transform> bones) BuildRealRig()
        {
            MatchArt art = MatchArtSource.Load();

            // A walking body's rig, taken off the unit table rather than named
            // here: the models are per unit type now, and which row happens to
            // be first is not this file's business. Every one of them is on
            // Rig_Medium, so any would do.
            _instance = Object.Instantiate(art.ModelFor(FirstCreep().Id));

            SimDrivenAnimator view = SimDrivenAnimator.Bind(_instance, art.CreepWalkClip);
            var bones = _instance.GetComponentsInChildren<Transform>(true).ToList();

            return (view, bones);
        }

        /// <summary>The first walking row in the shipped unit table.</summary>
        private static UnitType FirstCreep() =>
            StreamingContent.ReadUnitTypes().Types.First(t => t.Role == UnitRole.Moving);

        [Test]
        public void TheClipActuallyDrivesTheImportedRig()
        {
            var (view, bones) = BuildRealRig();

            view.Pose(0, 0f);
            var atStart = Snapshot(bones);

            view.Pose(0, 0.5f);
            var atMiddle = Snapshot(bones);

            var moved = atStart.Where((v, i) => (v - atMiddle[i]).sqrMagnitude > 1e-8f).Count();

            // A rig where nothing is bound draws every body frozen in its bind pose.
            Assert.Greater(moved, 0,
                "no bone moved between phase 0 and 0.5 — the clip is not bound to this hierarchy at all");
            Debug.Log($"[realrig] bones={bones.Count} channels moved between phase 0 and 0.5: {moved}");
        }
    }
}
