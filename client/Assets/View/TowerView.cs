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
    /// test.</b> A hitscan tower is posed by nothing: its shot puts nothing at
    /// all in the snapshot and exists only as an event and a tracer the view
    /// draws and forgets, so it stands as its model was imported. The
    /// projectile tower is bound to three clips and draws a bow, and its shot
    /// is a real snapshot entity that can be scrubbed backwards through. Same
    /// seam, opposite treatments — if both were drawn the same way the seam
    /// would not be being tested by anything.
    /// </para>
    /// <para>
    /// <b>Which model it wears is not that distinction.</b> The model and the
    /// scale come from <see cref="MatchArt"/> keyed by the unit type's id, so
    /// the Soldier, the Archer, the Ranger and the Mage are four bodies on the
    /// board rather than two deliveries. What the delivery still decides is
    /// whether there is anything to pose.
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

        private SimDrivenAnimator _animator;

        private Material _skin;

        private AnchoredPoint _effectAnchor;

        private Vector3 _besideOffset;

        private Quaternion _besideRotation = Quaternion.identity;

        private Quaternion _restingRotation = Quaternion.identity;

        /// <summary>Its one-based place in the defense — the id the snapshot uses.</summary>
        public int Id { get; private set; }

        /// <summary>What kind of tower it is.</summary>
        public UnitType Type { get; private set; }

        /// <summary>The instantiated model.</summary>
        public GameObject Model { get; private set; }

        /// <summary>What it holds in <c>handslot.r</c>, or null.</summary>
        public GameObject RightHand { get; private set; }

        /// <summary>What it holds in <c>handslot.l</c>, or null.</summary>
        public GameObject LeftHand { get; private set; }

        /// <summary>What stands on the ground beside it, or null.</summary>
        public GameObject Beside { get; private set; }

        /// <summary>
        /// What this tower's own effects are drawn as — its bubble, or its
        /// shot — off its own row's art.
        /// </summary>
        /// <remarks>
        /// Read where the event stream is handled, because an event names an
        /// entity and the view has to turn that id into the row that emitted
        /// it. Kept here rather than looked up out of <see cref="MatchArt"/>
        /// per event, for the same reason the anchor is resolved once: an event
        /// is a tick-loop caller.
        /// </remarks>
        public EffectSignature Signature { get; private set; }

        /// <summary>True when this tower has a rig and three clips.</summary>
        public bool IsAnimated => _animator != null;

        /// <summary>The transform its shots leave from, or null if it has no anchor.</summary>
        public Transform AnchorTransform => _effectAnchor.At;

        /// <summary>
        /// Where its shots leave from — what a tracer is drawn out of and where
        /// the muzzle flash sits.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The anchor was found on this tower's own model, on what it holds or
        /// on what stands beside it when the view was built, so this reads the
        /// current pose of a bone, a staff tip or a turret barrel — and the
        /// flash on a held prop moves with the arm. A tower with no anchor
        /// — a row drawn as the stand-in, which is a mannequin with no staff tip
        /// to name — falls back to
        /// <see cref="MatchTuning.TowerMuzzleHeight"/> above its base.
        /// </para>
        /// <para>
        /// <b>Decorations only.</b> This is read where the event stream is
        /// handled, and a projectile may not use it: see
        /// <see cref="EffectAnchor"/> for why a shell's origin stays derived
        /// from its target.
        /// </para>
        /// </remarks>
        public Vector3 Muzzle => _effectAnchor.IsSet
            ? _effectAnchor.Position
            : transform.position + (Vector3.up * MatchTuning.TowerMuzzleHeight);

        /// <summary>What the last <see cref="Pose"/> call drew. For tests.</summary>
        public TowerState LastState { get; private set; }

        /// <summary>The clip time last sampled, in seconds. For tests.</summary>
        public float LastClipTime { get; private set; }

        /// <summary>Which slot was last weighted. For tests.</summary>
        public int LastSlot { get; private set; }

        /// <summary>
        /// The unposed tower. No clips, nothing to sample — and nothing in the
        /// snapshot for its shots either.
        /// </summary>
        public void BuildStatic(int id, UnitType type, UnitArt art, Quaternion resting)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));

            Id = id;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Signature = art.Signature;
            _restingRotation = resting;

            Model = DrawnModel.Under(transform, art.Model, art.Scale);

            // Before the hands, because the row's atlas covers the body and a
            // prop wears its own pack's.
            _skin = DrawnModel.Wear(Model, art.Texture);

            Hold(art);
            Place(art.Beside);

            _effectAnchor = art.EffectAnchor.ResolveOn(gameObject);

            transform.rotation = resting;

            Stand();
        }

        /// <summary>
        /// The skinned character, what it holds and its three clips — one per
        /// simulation state.
        /// </summary>
        /// <remarks>
        /// The clips come off the unit's own art rather than off a set shared
        /// by every tower, because a mage casting and an archer drawing are
        /// different actions on the same three states. See <see cref="MatchArt"/>.
        /// </remarks>
        public void BuildAnimated(int id, UnitType type, UnitArt art, Quaternion resting)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));

            if (!art.IsPosed)
            {
                throw new ArgumentException(
                    "Unit " + art.UnitId + " has no clips, so there is nothing to pose it with. A tower "
                    + "with all three clips is animated and one with none is static; call BuildStatic.",
                    nameof(art));
            }

            Id = id;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Signature = art.Signature;
            _restingRotation = resting;

            Model = DrawnModel.Under(transform, art.Model, art.Scale);

            // Before the hands, because the row's atlas covers the body and a
            // prop wears its own pack's.
            _skin = DrawnModel.Wear(Model, art.Texture);

            // What it holds goes on the bones before the graph is built, so the
            // first pose the tower is ever drawn in already has it in hand.
            Hold(art);
            Place(art.Beside);

            // After the hands and the beside prop, because an anchor may name a
            // node inside either, and once — the lookup is by name and this is
            // the last moment the hierarchy changes shape.
            _effectAnchor = art.EffectAnchor.ResolveOn(gameObject);

            // Binding is SimDrivenAnimator's business, including the ban on a
            // RuntimeAnimatorController, and the clip lengths stay over there
            // with the clips.
            _animator = SimDrivenAnimator.Bind(
                Model, art.IdleClip, art.WindupClip, art.BackswingClip);

            transform.rotation = resting;

            Stand();
        }

        /// <summary>
        /// Destroys the material this made, because whoever made one destroys
        /// it. A tower wearing the atlas it imported with made none.
        /// </summary>
        private void OnDestroy()
        {
            DrawnModel.Discard(_skin);
            _skin = null;
        }

        /// <summary>Puts whatever the art names into whichever hands it names.</summary>
        private void Hold(UnitArt art)
        {
            if (art.RightHand != null)
            {
                RightHand = WeaponSocket.Attach(Model, art.RightHand, WeaponSocket.MeleeHand, art.RightHandTilt);
            }

            if (art.LeftHand != null)
            {
                LeftHand = WeaponSocket.Attach(Model, art.LeftHand, WeaponSocket.OffHand, art.LeftHandTilt);
            }
        }

        /// <summary>
        /// Draws whatever the art stands beside the tower, as a sibling of the
        /// model rather than as a child of a bone.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Beside the model and not under it.</b> The body is instantiated
        /// at the row's own scale and then painted with the row's own atlas, and
        /// a prop parented under it would inherit both — which for a scenery
        /// asset off another pack means the wrong size in the wrong swatches.
        /// </para>
        /// <para>
        /// <b>A prop with no size is refused here and named.</b> Zero is what an
        /// unfilled serialized field holds, so it is what a model dropped into
        /// the inspector slot arrives with and what a row inherits if a table
        /// ever stops writing the size — and a prop drawn at no size at all is a
        /// prop nobody can see missing.
        /// </para>
        /// </remarks>
        private void Place(BesideProp prop)
        {
            if (!prop.IsSet)
            {
                return;
            }

            if (prop.Scale <= 0f)
            {
                throw new InvalidOperationException(
                    "Unit " + Id + " stands " + prop.Model.name + " beside it at a size of " + prop.Scale
                    + ". A beside prop carries its own size because the packs are per pack, and zero is "
                    + "what an unwritten field holds — so this is a row that named a prop and never said "
                    + "how big.");
            }

            Beside = DrawnModel.Under(transform, prop.Model, prop.Scale);
            _besideOffset = prop.Offset;

            // Composed with the resting facing in Stand rather than overwritten.
            _besideRotation = Beside.transform.localRotation;
        }

        /// <summary>
        /// Puts the beside prop back where it stands, whichever way the tower
        /// has since turned.
        /// </summary>
        /// <remarks>
        /// <b>A tower turns and the thing on the ground beside it does not.</b>
        /// The whole view rotates to aim — that is what <see cref="Pose"/> does
        /// — so a prop left at a fixed local offset would swing through the
        /// neighbouring tiles every time a creep walked past. Its world pose is
        /// written from the resting facing instead, which is the frame the
        /// offset is measured in and the one the tile was chosen in.
        /// </remarks>
        private void Stand()
        {
            if (Beside == null)
            {
                return;
            }

            Beside.transform.SetPositionAndRotation(
                transform.position + (_restingRotation * _besideOffset),
                _restingRotation * _besideRotation);
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

            // After the turn and before the early return, because a tower with
            // no clips still turns to track a creep and would still drag its
            // prop round with it.
            Stand();

            if (_animator == null)
            {
                // A tower with no clips has nothing to sample. Its whole
                // contribution to the picture is standing where it was put.
                return;
            }

            int slot = SlotFor(state);

            LastSlot = slot;

            // Idle is the one state the simulation gives no duration to, so it
            // is the one that loops on its clip's own length rather than being
            // stretched to fit a number of ticks. See StretchedPhase.
            LastClipTime = state == TowerState.Idle
                ? _animator.PoseLooping(slot, ticksInState / (float)Match.TicksPerSecond)
                : _animator.Pose(slot, StretchedPhase(state, ticksInState));
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
        /// How far through its clip a winding-up or recovering tower is, in
        /// [0,1], derived from simulation ticks and nothing else.
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
        /// Idle is not here, because the simulation gives it no duration to be
        /// stretched to: a tower is idle until something walks into range, so
        /// the only length its phase can be measured against is the clip's own.
        /// That wrap belongs to whatever holds the clip, which is why
        /// <see cref="SimDrivenAnimator.PoseLooping"/> takes seconds and this
        /// takes ticks. Both are simulation state and both run backwards under a
        /// scrub; neither is a playback head on the view's clock.
        /// </para>
        /// </remarks>
        private float StretchedPhase(TowerState state, int ticksInState)
        {
            int duration = state == TowerState.Windup ? Type.WindupTicks : Type.BackswingTicks;

            return duration <= 0 ? 0f : Mathf.Clamp01(ticksInState / (float)duration);
        }
    }
}
