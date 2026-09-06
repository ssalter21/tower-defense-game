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

        private SimDrivenAnimator _animator;

        private Material _skin;

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

        /// <summary>What it carries in <c>handslot.r</c>, or null.</summary>
        public GameObject RightHand { get; private set; }

        /// <summary>What it carries in <c>handslot.l</c>, or null.</summary>
        public GameObject LeftHand { get; private set; }

        /// <summary>
        /// What is on it, drawn — the wash and the bar. Shown by its own call
        /// rather than by <see cref="Pose"/>: where a creep stands and what its
        /// legs are doing come out of one part of the snapshot row and what is
        /// on it comes out of another, and neither answer is an input to the
        /// other.
        /// </summary>
        /// <remarks>
        /// <b>Made in <c>Build</c> and never in a field initializer.</b> It
        /// holds a <c>MaterialPropertyBlock</c>, which is a native object, and
        /// Unity refuses to make one of those from a MonoBehaviour's
        /// constructor — the throw names the game object and lands in whatever
        /// test happens to be running when the first creep is built, which is
        /// nowhere near the line that caused it.
        /// </remarks>
        public EffectMarks Marks { get; private set; }

        /// <summary>
        /// Builds the view: instantiates the model under this object at the
        /// size its unit type is drawn at, wires its two clips into a Playables
        /// graph with no playback head, and hangs the effect marks off it.
        /// </summary>
        /// <param name="healthSegment">
        /// The material the bar's health segment wears. Made once by whoever
        /// owns the match, because a material per creep is an asset instance
        /// per creep to destroy again.
        /// </param>
        /// <param name="shieldSegment">The material its pool segment wears.</param>
        public void Build(
            UnitArt art,
            AnimationClip walk,
            AnimationClip death,
            Material healthSegment,
            Material shieldSegment) =>
            BuildBody(art, walk, death, healthSegment, shieldSegment);

        /// <summary>
        /// The same, for a body that is a portrait rather than a creep in a
        /// match: a contact sheet or an art preview, where there is no snapshot
        /// behind the model and nothing will ever ask what is on it.
        /// </summary>
        /// <remarks>
        /// The marks are not built at all, rather than built out of materials
        /// nobody would destroy again: a sheet builds and throws away a body per
        /// row, so a material made here would be one leaked per row.
        /// <see cref="ShowEffects"/> refuses by name afterwards, which is what
        /// keeps this from being a quiet second mode of the same object.
        /// </remarks>
        public void Build(UnitArt art, AnimationClip walk, AnimationClip death) =>
            BuildBody(art, walk, death, healthSegment: null, shieldSegment: null);

        private void BuildBody(
            UnitArt art,
            AnimationClip walk,
            AnimationClip death,
            Material healthSegment,
            Material shieldSegment)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));
            if (walk == null) throw new ArgumentNullException(nameof(walk));
            if (death == null) throw new ArgumentNullException(nameof(death));

            Model = DrawnModel.Under(transform, art.Model, art.Scale);

            // Before the hands, because the row's atlas covers the body and a
            // prop wears its own pack's.
            _skin = DrawnModel.Wear(Model, art.Texture);

            // What it carries goes on before the graph is built, so it is in
            // hand for the first frame the creep is ever drawn in. A creep's
            // weapon is scenery -- nothing in the simulation swings it, because
            // a walker has no attack at all -- but it is what separates two
            // rows that share a skin. See docs/roster.md.
            if (art.RightHand != null)
            {
                RightHand = WeaponSocket.Attach(Model, art.RightHand, WeaponSocket.MeleeHand, art.RightHandTilt);
            }

            if (art.LeftHand != null)
            {
                LeftHand = WeaponSocket.Attach(Model, art.LeftHand, WeaponSocket.OffHand, art.LeftHandTilt);
            }

            // Generic transform curves and no avatar -- the path the Playables
            // validation proved. Binding is SimDrivenAnimator's business,
            // including the ban on a RuntimeAnimatorController, and the clip
            // lengths stay over there with the clips.
            _animator = SimDrivenAnimator.Bind(Model, walk, death);

            // After the graph, because the wash lands on every renderer under
            // the body and what it is holding is part of the body by now.
            Marks = new EffectMarks();
            Marks.Build(transform, Model, healthSegment, shieldSegment);
        }

        /// <summary>
        /// Destroys the material this made, because whoever made one destroys
        /// it. A creep wearing the atlas it imported with made none.
        /// </summary>
        /// <remarks>
        /// One per pooled view rather than one per creep: a view is built once
        /// for its variant and then handed out again for the length of the
        /// match.
        /// </remarks>
        private void OnDestroy()
        {
            DrawnModel.Discard(_skin);
            _skin = null;
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
                LastDeathTime = _animator.Pose(DeathSlot, Mathf.Clamp01(dyingFraction));
                LastWalkTime = 0f;
            }
            else
            {
                // Mathf.Repeat rather than a cast, because it is correct for
                // negative inputs: scrubbing back past the entrance has to give
                // a phase in [0,1) and not a mirrored one.
                float cycles = distanceHexes / MatchTuning.HexesPerWalkCycle;

                LastWalkTime = _animator.Pose(WalkSlot, Mathf.Repeat(cycles, 1f));
                LastDeathTime = 0f;
            }
        }
    }
}
