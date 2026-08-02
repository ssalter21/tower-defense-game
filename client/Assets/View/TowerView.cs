using System;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// One tower: where it stands, which way it is pointing, and — if it is the
    /// kind with a rig — what it is doing with its bow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Towers are not pooled, because they never churn.</b> The defense is
    /// one array read once and never written to; six towers exist from the
    /// first tick to the last. Pooling them would be a mechanism with nothing
    /// to do, and the id-matching the creeps and projectiles need is only worth
    /// having where things actually appear and vanish.
    /// </para>
    /// <para>
    /// <b>The two kinds are deliberately different, and the contrast is the
    /// test.</b> The hitscan tower is a static building with no rig and no
    /// clips: its shot puts nothing at all in the snapshot and exists only as
    /// an event and a tracer the view draws and forgets. The projectile tower
    /// is a skinned character that draws a bow, and its shot is a real snapshot
    /// entity that can be scrubbed backwards through. Same seam, opposite
    /// treatments — if both were drawn the same way the seam would not be being
    /// tested by anything.
    /// </para>
    /// <para>
    /// <b>Facing snaps, and that is not a shortcut.</b> Turning smoothly means
    /// interpolating from wherever the tower was pointing last frame, and where
    /// it was pointing last frame is view-side state the simulation has no
    /// opinion about. Scrub to tick 900 from the left and from the right and a
    /// smoothed tower is pointing two different ways at the same tick, which is
    /// precisely the class of disagreement this architecture exists to make
    /// impossible. So the rotation is recomputed from the snapshot every frame
    /// and nothing carries over.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TowerView : MonoBehaviour
    {
        /// <summary>The mixer slot the idle clip is connected to.</summary>
        public const int IdleSlot = 0;

        /// <summary>The mixer slot the windup clip is connected to.</summary>
        public const int WindupSlot = 1;

        /// <summary>The mixer slot the backswing clip is connected to.</summary>
        public const int BackswingSlot = 2;

        private readonly double[] _times = new double[3];

        private readonly float[] _weights = new float[3];

        private readonly float[] _lengths = new float[3];

        private SimDrivenAnimator _animator;

        private Quaternion _restingRotation = Quaternion.identity;

        /// <summary>Its one-based place in the defense — the id the snapshot uses.</summary>
        public int Id { get; private set; }

        /// <summary>What kind of tower it is.</summary>
        public UnitType Type { get; private set; }

        /// <summary>The instantiated model.</summary>
        public GameObject Model { get; private set; }

        /// <summary>The weapon, on a hitscan tower's <c>null</c>.</summary>
        public GameObject Weapon { get; private set; }

        /// <summary>True when this tower has a rig and three clips.</summary>
        public bool IsAnimated => _animator != null;

        /// <summary>Where its shots leave from — what a tracer is drawn out of.</summary>
        public Vector3 Muzzle => transform.position + (Vector3.up * MatchTuning.TowerMuzzleHeight);

        /// <summary>What the last <see cref="Pose"/> call drew. For tests.</summary>
        public TowerState LastState { get; private set; }

        /// <summary>The clip time last sampled, in seconds. For tests.</summary>
        public float LastClipTime { get; private set; }

        /// <summary>Which slot was last weighted. For tests.</summary>
        public int LastSlot { get; private set; }

        /// <summary>
        /// The static building. No rig, no clips, nothing to sample — and
        /// nothing in the snapshot for its shots either.
        /// </summary>
        public void BuildStatic(int id, UnitType type, GameObject model, Quaternion resting)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Id = id;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _restingRotation = resting;

            Model = Instantiate(model, transform, false);
            Model.name = model.name;
            Model.transform.localPosition = Vector3.zero;

            // The model's own local ROTATION is left exactly as the importer
            // produced it. Forcing it to identity looks tidy and tips over any
            // model whose FBX root carries an axis-conversion rotation -- which
            // is how the hitscan tower came to be lying on its side on the road,
            // while the characters, whose roots happen to be identity, stood up
            // perfectly and hid the bug.

            transform.rotation = resting;
        }

        /// <summary>
        /// The skinned character, its weapon and its three clips — one per
        /// simulation state.
        /// </summary>
        public void BuildAnimated(
            int id,
            UnitType type,
            GameObject model,
            GameObject weapon,
            AnimationClip idle,
            AnimationClip windup,
            AnimationClip backswing,
            Quaternion resting)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (weapon == null) throw new ArgumentNullException(nameof(weapon));
            if (idle == null) throw new ArgumentNullException(nameof(idle));
            if (windup == null) throw new ArgumentNullException(nameof(windup));
            if (backswing == null) throw new ArgumentNullException(nameof(backswing));

            Id = id;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _restingRotation = resting;

            Model = Instantiate(model, transform, false);
            Model.name = model.name;
            Model.transform.localPosition = Vector3.zero;

            // The model's own local ROTATION is left exactly as the importer
            // produced it. Forcing it to identity looks tidy and tips over any
            // model whose FBX root carries an axis-conversion rotation -- which
            // is how the hitscan tower came to be lying on its side on the road,
            // while the characters, whose roots happen to be identity, stood up
            // perfectly and hid the bug.

            // The weapon goes on the bone before the graph is built, so the
            // first pose the tower is ever drawn in already has it in hand.
            Weapon = WeaponSocket.Attach(Model, weapon, WeaponSocket.BowHand);

            Animator animator = Model.GetComponent<Animator>();

            if (animator == null)
            {
                animator = Model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;

            _lengths[IdleSlot] = idle.length;
            _lengths[WindupSlot] = windup.length;
            _lengths[BackswingSlot] = backswing.length;

            _animator = gameObject.AddComponent<SimDrivenAnimator>();
            _animator.Build(animator, idle, windup, backswing);

            transform.rotation = resting;
        }

        /// <summary>
        /// Points the tower and poses it from one tick of simulation state.
        /// </summary>
        /// <param name="state">Idle, winding up, or recovering.</param>
        /// <param name="ticksInState">How long it has been in that state.</param>
        /// <param name="targetPosition">
        /// Where its target is right now, or null when it has none. Taken from
        /// the snapshot being drawn, so a tower tracks a moving creep for free.
        /// </param>
        public void Pose(TowerState state, int ticksInState, Vector3? targetPosition)
        {
            LastState = state;

            transform.rotation = targetPosition.HasValue
                ? FacingToward(targetPosition.Value)
                : _restingRotation;

            if (_animator == null)
            {
                // A static building has nothing to sample. Its whole
                // contribution to the picture is standing where it was put.
                return;
            }

            int slot = SlotFor(state);
            float time = ClipTimeFor(state, ticksInState, slot);

            LastSlot = slot;
            LastClipTime = time;

            for (int index = 0; index < _times.Length; index++)
            {
                _times[index] = index == slot ? time : 0.0;
                _weights[index] = index == slot ? 1f : 0f;
            }

            _animator.Sample(_times, _weights);
        }

        /// <summary>
        /// The rotation that looks at a point, kept level. Towers stand on flat
        /// ground and a tower tilting to track a creep at its feet reads as a
        /// bug rather than as aiming.
        /// </summary>
        private Quaternion FacingToward(Vector3 target)
        {
            Vector3 flat = target - transform.position;
            flat.y = 0f;

            return flat.sqrMagnitude < 1e-6f
                ? _restingRotation
                : Quaternion.LookRotation(flat.normalized, Vector3.up);
        }

        private static int SlotFor(TowerState state)
        {
            switch (state)
            {
                case TowerState.Windup: return WindupSlot;
                case TowerState.Backswing: return BackswingSlot;
                default: return IdleSlot;
            }
        }

        /// <summary>
        /// Where in its clip the tower is, derived from simulation ticks and
        /// nothing else.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Windup and backswing are stretched to fit the simulation's
        /// durations</b>, which is what makes the draw finish exactly as the
        /// shot is released and the release finish exactly as the tower goes
        /// idle. Playing the clips at their authored speed instead would put
        /// the animation and the firing on two different clocks — the bow would
        /// still be drawing when the arrow left, or would sit fully drawn
        /// waiting — and that is the "neither without the other" the acceptance
        /// criteria are about.
        /// </para>
        /// <para>
        /// Idle is the exception, because the simulation gives it no duration:
        /// a tower is idle until something walks into range. It wraps on
        /// <c>ticksInState</c>, which is still simulation state and still runs
        /// backwards under a scrub — it is a loop driven by the simulation's
        /// clock, not a playback head running on the view's.
        /// </para>
        /// </remarks>
        private float ClipTimeFor(TowerState state, int ticksInState, int slot)
        {
            float length = _lengths[slot];

            if (state == TowerState.Idle)
            {
                float seconds = ticksInState / (float)Match.TicksPerSecond;

                return length <= 0f ? 0f : Mathf.Repeat(seconds, length);
            }

            int duration = state == TowerState.Windup ? Type.WindupTicks : Type.BackswingTicks;

            if (duration <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01(ticksInState / (float)duration) * length;
        }
    }
}
