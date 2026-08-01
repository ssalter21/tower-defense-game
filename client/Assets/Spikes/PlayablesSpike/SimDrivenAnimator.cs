using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Spikes.Playables
{
    /// <summary>
    /// The production-shaped thing this spike is measuring: a view component that
    /// samples animation clips at a time the simulation dictates, with no playback
    /// head of its own.
    ///
    /// The whole point is that <see cref="Sample"/> is a pure function of its
    /// arguments. Call it with the same phase twice and you get the same pose;
    /// call it with a decreasing phase and the animation runs backwards. Nothing
    /// here accumulates, so nothing here can desync from the sim.
    /// </summary>
    public sealed class SimDrivenAnimator : MonoBehaviour
    {
        /// <summary>
        /// The two independent ways to stop the graph growing a playback head.
        /// Exposed so the spike can poison each one and prove it is load-bearing —
        /// a guard nobody has watched fail is not known to be doing anything.
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
