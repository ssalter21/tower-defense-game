using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace View
{
    /// <summary>
    /// The view component that samples animation clips at a time the simulation
    /// dictates, with no playback head of its own.
    ///
    /// The whole point is that <see cref="Sample"/> is a pure function of its
    /// arguments. Call it with the same phase twice and you get the same pose;
    /// call it with a decreasing phase and the animation runs backwards. Nothing
    /// here accumulates, so nothing here can desync from the sim.
    ///
    /// This is why there is no <c>RuntimeAnimatorController</c> anywhere near it.
    /// A state-machine animator is a playback head that advances in wall-clock
    /// time — exactly the view-side accumulator the architecture forbids — and it
    /// is banned outright rather than configured carefully.
    /// </summary>
    public sealed class SimDrivenAnimator : MonoBehaviour
    {
        /// <summary>
        /// The two independent ways to stop the graph growing a playback head.
        /// Each was measured to be independently sufficient; both are kept
        /// precisely because they fail independently.
        ///
        /// This is exposed rather than hard-coded so the poison suite can rebuild
        /// the subject with each guard removed and watch the pose drift. A guard
        /// nobody has watched fail is not known to be doing anything. Production
        /// code never passes anything but <see cref="HeadGuard.Both"/> — that is
        /// what the <see cref="Build(Animator, AnimationClip[])"/> overload is.
        /// </summary>
        [System.Flags]
        public enum HeadGuard
        {
            None = 0,
            /// <summary>Unity never ticks the graph; only an explicit Evaluate does.</summary>
            ManualUpdate = 1,
            /// <summary>The clip cannot advance even if something evaluates with a delta.</summary>
            ZeroSpeed = 2,
            Both = ManualUpdate | ZeroSpeed
        }

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable[] _clips;

        /// <summary>Builds the graph. One clip per slot; slots are addressed by index.</summary>
        public void Build(Animator animator, params AnimationClip[] clips)
            => Build(animator, HeadGuard.Both, clips);

        /// <summary>
        /// The same graph with a chosen subset of the head-guards. Only the poison
        /// suite calls this with anything other than <see cref="HeadGuard.Both"/>.
        /// </summary>
        public void Build(Animator animator, HeadGuard guard, params AnimationClip[] clips)
        {
            _graph = PlayableGraph.Create(name + ".anim");

            if ((guard & HeadGuard.ManualUpdate) != 0)
            {
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            }

            _mixer = AnimationMixerPlayable.Create(_graph, clips.Length);
            _clips = new AnimationClipPlayable[clips.Length];

            for (var i = 0; i < clips.Length; i++)
            {
                _clips[i] = AnimationClipPlayable.Create(_graph, clips[i]);

                if ((guard & HeadGuard.ZeroSpeed) != 0)
                {
                    _clips[i].SetSpeed(0.0);
                }

                _graph.Connect(_clips[i], 0, _mixer, i);
            }

            var output = AnimationPlayableOutput.Create(_graph, "pose", animator);
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }

        /// <summary>
        /// Poses the rig. <paramref name="times"/> are absolute clip times in seconds,
        /// <paramref name="weights"/> are the mixer weights. Pure: same input, same pose.
        /// </summary>
        public void Sample(double[] times, float[] weights)
        {
            for (var i = 0; i < _clips.Length; i++)
            {
                _clips[i].SetTime(times[i]);
                _mixer.SetInputWeight(i, weights[i]);
            }

            // Zero delta: evaluate the pose at the times just set, advance nothing.
            _graph.Evaluate(0f);
        }

        /// <summary>Convenience for the single-clip case, phase in [0,1] of the clip's length.</summary>
        public void SampleSingle(int slot, float phase, float clipLength)
        {
            var times = new double[_clips.Length];
            var weights = new float[_clips.Length];
            times[slot] = phase * clipLength;
            weights[slot] = 1f;
            Sample(times, weights);
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}
