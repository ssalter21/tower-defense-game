using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace View
{
    /// <summary>
    /// The view component that samples animation clips at a time the simulation
    /// dictates, with no playback head of its own.
    ///
    /// The whole point is that <see cref="Pose(int, float)"/> is a pure function
    /// of its arguments. Call it with the same phase twice and you get the same
    /// pose; call it with a decreasing phase and the animation runs backwards.
    /// Nothing here accumulates, so nothing here can desync from the sim.
    ///
    /// This is why there is no <c>RuntimeAnimatorController</c> anywhere near it.
    /// A state-machine animator is a playback head that advances in wall-clock
    /// time — exactly the view-side accumulator the architecture forbids — and it
    /// is banned outright rather than configured carefully. <see cref="Bind"/> is
    /// where that ban is enforced, and it is the only place a rig is wired up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The interface is a slot and a phase, and it allocates nothing.</b> It
    /// used to be two arrays — one of clip times, one of mixer weights — and
    /// every per-frame caller had to cache both buffers and write its own
    /// zero-every-other-slot loop, because a convenience that allocated two
    /// arrays per call could not be used by anything drawn every frame. Two
    /// callers grew that loop independently and a third owned a copy of the
    /// clip-length table to feed it. All of it was this component's protocol
    /// leaking out through an interface too shallow to hold it.
    /// </para>
    /// <para>
    /// <b>Clip lengths live here.</b> This component holds the clips, so it is
    /// the one thing that cannot be wrong about how long they are. A caller
    /// wanting a phase converts to one from its own units — distance travelled,
    /// ticks in a state — and never from a second copy of the length.
    /// </para>
    /// </remarks>
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
        private float[] _lengths;

        /// <summary>
        /// Binds a rig and returns the sampler on it. <b>The one place a rig is
        /// wired up</b>, used by every caller that poses one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Three things have to be true before a graph can drive a rig, and the
        /// third is the one that matters: the object needs an
        /// <see cref="Animator"/> to output through, root motion has to be off,
        /// and the animator must carry <b>no</b>
        /// <c>RuntimeAnimatorController</c> — the banned playback head. That
        /// preamble was written out three times and had already drifted: two
        /// copies nulled the controller and the third did not, so the contact
        /// sheets were being rendered through a rig configured differently from
        /// the one the game draws. It is written once now, and the guard applies
        /// wherever a rig is bound.
        /// </para>
        /// <para>
        /// The sampler component goes on <paramref name="rig"/> itself, beside
        /// the animator it drives, so the two cannot be separated by a caller
        /// that parented one of them somewhere else.
        /// </para>
        /// </remarks>
        /// <param name="rig">The instantiated model whose bones the clips drive.</param>
        /// <param name="clips">One clip per slot; slots are addressed by index.</param>
        public static SimDrivenAnimator Bind(GameObject rig, params AnimationClip[] clips)
            => Bind(rig, HeadGuard.Both, clips);

        /// <summary>
        /// The same binding with a chosen subset of the head-guards. Only the
        /// poison suite passes anything other than <see cref="HeadGuard.Both"/>.
        /// </summary>
        public static SimDrivenAnimator Bind(GameObject rig, HeadGuard guard, params AnimationClip[] clips)
        {
            if (rig == null) throw new ArgumentNullException(nameof(rig));

            // Deliberately not `??`: Unity's fake-null overrides == but not the
            // null-coalescing operator, so `GetComponent() ?? AddComponent()`
            // hands back the fake-null.
            Animator animator = rig.GetComponent<Animator>();

            if (animator == null)
            {
                animator = rig.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;

            var sampler = rig.AddComponent<SimDrivenAnimator>();
            sampler.Build(animator, guard, clips);

            return sampler;
        }

        /// <summary>
        /// Builds the graph on an animator the caller already has. One clip per
        /// slot; slots are addressed by index.
        /// </summary>
        /// <remarks>
        /// <see cref="Bind"/> is what production code calls — this is the
        /// primitive underneath it, for a caller holding an animator it
        /// configured itself. A caller reaching for this instead of
        /// <see cref="Bind"/> is opting out of the head-guard on the animator,
        /// and is on its own about the controller.
        /// </remarks>
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
            _lengths = new float[clips.Length];

            for (var i = 0; i < clips.Length; i++)
            {
                _clips[i] = AnimationClipPlayable.Create(_graph, clips[i]);
                _lengths[i] = clips[i].length;

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
        /// How long the clip in <paramref name="slot"/> runs, in seconds.
        /// </summary>
        /// <remarks>
        /// For the caller that has to wrap in clip time rather than in phase — a
        /// tower idling for however many ticks nothing walks into range, which
        /// loops on the clip's own length and has no other duration to divide
        /// by. Everything else converts to a phase from its own units and never
        /// needs this.
        /// </remarks>
        public float ClipLength(int slot) => _lengths[slot];

        /// <summary>
        /// Poses the rig from one clip, at <paramref name="phase"/> of its
        /// length, and returns the clip time that came to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every other slot is zeroed — weight and time both — so the pose is a
        /// function of these two arguments and of nothing left behind by the
        /// call before it. Pure: same slot, same phase, same pose.
        /// </para>
        /// <para>
        /// <b>The phase is taken as given.</b> Wrapping a walk cycle and clamping
        /// a death are different decisions belonging to different callers, and a
        /// component that quietly picked one would be making a domain choice
        /// about an animation it knows nothing about. A phase outside [0,1]
        /// samples outside the clip, which is what Unity does with it and what
        /// the caller asked for.
        /// </para>
        /// </remarks>
        /// <returns>The absolute clip time sampled, in seconds.</returns>
        public float Pose(int slot, float phase)
        {
            float time = phase * _lengths[slot];

            for (var i = 0; i < _clips.Length; i++)
            {
                bool chosen = i == slot;

                _clips[i].SetTime(chosen ? time : 0.0);
                _mixer.SetInputWeight(i, chosen ? 1f : 0f);
            }

            Evaluate();

            return time;
        }

        /// <summary>
        /// Poses the rig from two clips at once, <paramref name="blend"/> of the
        /// way from <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <remarks>
        /// The multi-slot case, which the mixer is there for: two clips with
        /// their own times and complementary weights, every other slot zeroed.
        /// Nothing in the match blends today — the simulation says which state a
        /// thing is in and there is no crossfade between states — so this exists
        /// for the sampling tests that prove the mixer blends linearly and keeps
        /// the two clips' times independent, which is the claim the whole
        /// no-playback-head design rests on.
        /// </remarks>
        /// <param name="blend">Weight of <paramref name="to"/>; <c>from</c> gets the rest.</param>
        public void PoseBlend(int from, float fromPhase, int to, float toPhase, float blend)
        {
            double fromTime = fromPhase * _lengths[from];
            double toTime = toPhase * _lengths[to];

            for (var i = 0; i < _clips.Length; i++)
            {
                double time = i == from ? fromTime : i == to ? toTime : 0.0;
                float weight = i == from ? 1f - blend : i == to ? blend : 0f;

                _clips[i].SetTime(time);
                _mixer.SetInputWeight(i, weight);
            }

            Evaluate();
        }

        /// <summary>Zero delta: evaluate the pose at the times just set, advance nothing.</summary>
        private void Evaluate() => _graph.Evaluate(0f);

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }
    }
}
