using System;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// One creep on the playfield: where it stands, which way it faces, and
    /// what its legs are doing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every one of those three is a pure function of the snapshot.</b>
    /// Nothing here remembers where the creep was, how long it has been
    /// walking, or whether it has started dying yet. Call
    /// <see cref="Pose(Vector3, Quaternion, float, CreepState, float)"/> twice
    /// with the same arguments and you get the same picture; call it with
    /// decreasing values and the creep walks backwards, feet and all.
    /// </para>
    /// <para>
    /// <b>The walk cycle is driven by distance travelled, not by elapsed
    /// time</b>, and that one substitution buys three things at once that
    /// otherwise cost three mechanisms. Feet match ground speed automatically,
    /// because the phase <i>is</i> the distance. Fast-forward is correct for
    /// free, because covering ground twice as fast cycles the legs twice as
    /// fast without anybody scaling a playback rate. And scrubbing backwards
    /// walks the legs backwards, because a decreasing distance is a decreasing
    /// phase — which is row four of the sit-down landmark table, and the one
    /// place a human can catch the whole architecture being wrong.
    /// </para>
    /// <para>
    /// <b>Dying is the simulation's business.</b> The clip is played across
    /// exactly the tick duration the simulation gave the <c>Dying</c> state, so
    /// the corpse leaves the screen on the tick the simulation stopped
    /// reporting it. The view never owns a corpse: it has no timer to keep one
    /// alive with, and when the id stops appearing the pool takes the object
    /// back whether the clip had finished or not.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CreepView : MonoBehaviour
    {
        /// <summary>The mixer slot the walk clip is connected to.</summary>
        public const int WalkSlot = 0;

        /// <summary>The mixer slot the death clip is connected to.</summary>
        public const int DeathSlot = 1;

        private readonly double[] _times = new double[2];

        private readonly float[] _weights = new float[2];

        private SimDrivenAnimator _animator;

        private float _walkLength;

        private float _deathLength;

        /// <summary>The instantiated model, once built.</summary>
        public GameObject Model { get; private set; }

        /// <summary>What the last <see cref="Pose"/> call drew. For tests.</summary>
        public CreepState LastState { get; private set; }

        /// <summary>
        /// The clip time the walk slot was last sampled at, in seconds. For
        /// tests, and for the one landmark row a human reads.
        /// </summary>
        public float LastWalkTime { get; private set; }

        /// <summary>The clip time the death slot was last sampled at, in seconds.</summary>
        public float LastDeathTime { get; private set; }

        /// <summary>
        /// Builds the view: instantiates the model under this object and wires
        /// its two clips into a Playables graph with no playback head.
        /// </summary>
        public void Build(GameObject model, AnimationClip walk, AnimationClip death)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (walk == null) throw new ArgumentNullException(nameof(walk));
            if (death == null) throw new ArgumentNullException(nameof(death));

            Model = Instantiate(model, transform, false);
            Model.name = model.name;
            Model.transform.localPosition = Vector3.zero;

            // The model's own local ROTATION is left exactly as the importer
            // produced it. Forcing it to identity looks tidy and tips over any
            // model whose FBX root carries an axis-conversion rotation -- which
            // is how the hitscan tower came to be lying on its side on the road,
            // while the characters, whose roots happen to be identity, stood up
            // perfectly and hid the bug.

            // Generic transform curves and no avatar -- the path the Playables
            // validation proved. An Animator is still the component a graph
            // outputs through; what matters is that it carries no
            // RuntimeAnimatorController, which is the banned playback head.
            Animator animator = Model.GetComponent<Animator>();

            if (animator == null)
            {
                animator = Model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;

            _walkLength = walk.length;
            _deathLength = death.length;

            _animator = gameObject.AddComponent<SimDrivenAnimator>();
            _animator.Build(animator, walk, death);
        }

        /// <summary>
        /// Puts the creep where the snapshot says it is and poses it
        /// accordingly.
        /// </summary>
        /// <param name="position">Where it stands, in world space.</param>
        /// <param name="facing">Which way it looks.</param>
        /// <param name="distanceHexes">
        /// How far along the corridor it has travelled. Drives the walk phase,
        /// and nothing else does.
        /// </param>
        /// <param name="state">Walking or dying.</param>
        /// <param name="dyingFraction">
        /// How far through the <c>Dying</c> state it is, as
        /// <c>ticksInState / dyingTicks</c>. Ignored while walking.
        /// </param>
        public void Pose(
            Vector3 position,
            Quaternion facing,
            float distanceHexes,
            CreepState state,
            float dyingFraction)
        {
            transform.SetPositionAndRotation(
                position + (Vector3.up * MatchTuning.CreepGroundOffset),
                facing);

            LastState = state;

            if (state == CreepState.Dying)
            {
                // The whole clip, stretched across however many ticks the
                // simulation said dying takes. Clamped rather than wrapped: a
                // death that overran its budget should hold on its last frame,
                // not start again.
                LastDeathTime = Mathf.Clamp01(dyingFraction) * _deathLength;
                LastWalkTime = 0f;

                _times[WalkSlot] = 0.0;
                _times[DeathSlot] = LastDeathTime;
                _weights[WalkSlot] = 0f;
                _weights[DeathSlot] = 1f;
            }
            else
            {
                // Mathf.Repeat rather than a cast, because it is correct for
                // negative inputs: scrubbing back past the entrance has to give
                // a phase in [0,1) and not a mirrored one.
                float cycles = distanceHexes / MatchTuning.HexesPerWalkCycle;

                LastWalkTime = Mathf.Repeat(cycles, 1f) * _walkLength;
                LastDeathTime = 0f;

                _times[WalkSlot] = LastWalkTime;
                _times[DeathSlot] = 0.0;
                _weights[WalkSlot] = 1f;
                _weights[DeathSlot] = 0f;
            }

            _animator.Sample(_times, _weights);
        }
    }
}
