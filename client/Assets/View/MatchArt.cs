using System;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// One unit type's model and the size it is drawn at.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The scale is here and not in <c>content/units.txt</c>.</b> Visual size
    /// is a view fact under ADR-0007, and a column in the content tables would
    /// make every art tweak a format version and a re-recording of every stored
    /// match. The two numbers it takes are on <see cref="MatchArt"/>.
    /// </para>
    /// <para>
    /// <b>Size says which side a unit is on and nothing else.</b> A tier is
    /// told apart by <see cref="Texture"/>, by what the unit holds, or by
    /// being a different model — never by being bigger. So two rows sharing a
    /// model share a scale, and what separates them is one of the other three.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class UnitArt
    {
        [SerializeField]
        [Tooltip("The id in content/units.txt. One global space; never an index.")]
        private int unitId;

        [SerializeField]
        [Tooltip("The model this unit type is drawn with.")]
        private GameObject model;

        [SerializeField]
        [Tooltip("Multiplied into the imported model's own scale.")]
        private float scale;

        [SerializeField]
        [Tooltip("Drawn over the model's own atlas. Null for a unit wearing the one it imported with.")]
        private Texture2D texture;

        [SerializeField]
        [Tooltip("Hung off handslot.r. Null for a unit that carries nothing there.")]
        private GameObject rightHand;

        [SerializeField]
        [Tooltip("Hung off handslot.l. Null for a unit that carries nothing there.")]
        private GameObject leftHand;

        [SerializeField]
        [Tooltip("Euler degrees applied to the right-hand item, on top of the bone. Usually zero.")]
        private Vector3 rightHandTilt;

        [SerializeField]
        [Tooltip("Euler degrees applied to the left-hand item, on top of the bone. Usually zero.")]
        private Vector3 leftHandTilt;

        [SerializeField]
        [Tooltip("Played while this tower is Idle. Null on a creep.")]
        private AnimationClip idleClip;

        [SerializeField]
        [Tooltip("Played across this tower's Windup. Null on a creep.")]
        private AnimationClip windupClip;

        [SerializeField]
        [Tooltip("Played across this tower's Backswing. Null on a creep.")]
        private AnimationClip backswingClip;

        [SerializeField]
        [Tooltip("Where this unit's flashes and tracers leave its art from. Empty for a unit that never fires.")]
        private EffectAnchor effectAnchor;

        /// <summary>A unit that stands there and holds nothing.</summary>
        public static UnitArt Of(int unitId, GameObject model, float scale) =>
            new UnitArt { unitId = unitId, model = model, scale = scale };

        /// <summary>
        /// A unit that holds something. Either hand may be null; a unit holding
        /// nothing in either is <see cref="Of(int, GameObject, float)"/>.
        /// </summary>
        public static UnitArt Holding(
            int unitId, GameObject model, float scale, GameObject rightHand, GameObject leftHand) =>
            Armed(unitId, model, scale, rightHand, leftHand, null, null, null);

        /// <summary>
        /// A tower: what it holds, and the three clips it is posed with.
        /// </summary>
        /// <remarks>
        /// The clips are per unit and not per project because a mage casting
        /// and an archer drawing are different actions on the same three
        /// simulation states. They were one shared set until the weapons became
        /// per unit, and that shared set is what put a bow in the mage's hands.
        /// </remarks>
        public static UnitArt Armed(
            int unitId,
            GameObject model,
            float scale,
            GameObject rightHand,
            GameObject leftHand,
            AnimationClip idle,
            AnimationClip windup,
            AnimationClip backswing,
            Vector3 rightHandTilt = default,
            Vector3 leftHandTilt = default,
            EffectAnchor effectAnchor = default,
            Texture2D texture = null) =>
            new UnitArt
            {
                unitId = unitId,
                model = model,
                scale = scale,
                texture = texture,
                rightHand = rightHand,
                leftHand = leftHand,
                idleClip = idle,
                windupClip = windup,
                backswingClip = backswing,
                rightHandTilt = rightHandTilt,
                leftHandTilt = leftHandTilt,
                effectAnchor = effectAnchor,
            };

        /// <summary>The row in <c>content/units.txt</c> this stands for.</summary>
        public int UnitId => unitId;

        /// <summary>The model.</summary>
        public GameObject Model => model;

        /// <summary>How much bigger or smaller than the imported model this draws.</summary>
        public float Scale => scale;

        /// <summary>
        /// The atlas this row is drawn in, or null for the one the model
        /// imported wearing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The packs ship several atlases per character and the rows pick
        /// between them, so two rows on one model are two colours rather than
        /// two sizes. Which atlas goes on which row is signed in
        /// <c>docs/roster.md</c> and nothing chooses one here.
        /// </para>
        /// <para>
        /// It covers the body and not what the body is holding. A prop is its
        /// own import off its own pack's atlas — the Adventurers quiver is
        /// authored on the rogue's — so a character atlas painted over it
        /// would draw the prop in swatches meant for a torso.
        /// </para>
        /// </remarks>
        public Texture2D Texture => texture;

        /// <summary>What goes on <c>handslot.r</c>, or null.</summary>
        public GameObject RightHand => rightHand;

        /// <summary>What goes on <c>handslot.l</c>, or null.</summary>
        public GameObject LeftHand => leftHand;

        /// <summary>
        /// How the right-hand item is turned relative to the bone. Zero for
        /// everything the pack authored for that hand.
        /// </summary>
        public Quaternion RightHandTilt => Quaternion.Euler(rightHandTilt);

        /// <summary>
        /// How the left-hand item is turned relative to the bone.
        /// </summary>
        /// <remarks>
        /// Not zero for the bow. Every weapon in this pack is authored for the
        /// right hand, which is the melee hand; the bow is the only thing that
        /// goes in the left, and at the bone's own rotation it comes out with
        /// its belly curving into the archer and its string facing the target —
        /// backwards, and visibly so. A half turn about the vertical is the
        /// correction, and it is written down per unit rather than baked into
        /// the socket because the next off-hand item is a shield, which needs
        /// none.
        /// </remarks>
        public Quaternion LeftHandTilt => Quaternion.Euler(leftHandTilt);

        /// <summary>The clip for <see cref="Sim.TowerState.Idle"/>, or null on a creep.</summary>
        public AnimationClip IdleClip => idleClip;

        /// <summary>The clip for <see cref="Sim.TowerState.Windup"/>, or null on a creep.</summary>
        public AnimationClip WindupClip => windupClip;

        /// <summary>The clip for <see cref="Sim.TowerState.Backswing"/>, or null on a creep.</summary>
        public AnimationClip BackswingClip => backswingClip;

        /// <summary>
        /// Where this unit's flashes and tracers leave its art from — a bone,
        /// or a point on what it is holding. Empty for a unit that never fires.
        /// </summary>
        /// <remarks>
        /// Per unit for the same reason the weapon is: a staff tip and a bow
        /// grip are not the same place on the same rig, and a project-wide
        /// height above the root is the thing this replaces. It travels with
        /// the model and the hands because it names a part of them.
        /// </remarks>
        public EffectAnchor EffectAnchor => effectAnchor;

        /// <summary>
        /// True when both halves are filled in. A zero scale is as incomplete as
        /// a null model: it draws the unit at no size at all, which on screen is
        /// a unit that never appeared.
        /// </summary>
        public bool IsComplete => model != null && scale > 0f;

        /// <summary>
        /// True when this unit carries all three clips, which is what makes it
        /// a posed tower rather than a thing standing where it was put.
        /// </summary>
        /// <remarks>
        /// All three or none. Two clips and a null is a wiring mistake that
        /// would otherwise reach the animator as a bind-pose freeze in one
        /// state only, which is the hardest kind of animation bug to see.
        /// </remarks>
        public bool IsPosed => idleClip != null && windupClip != null && backswingClip != null;
    }

    /// <summary>
    /// The models and clips the match is drawn with — every one of them chosen
    /// by the developer, and none of them chosen here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This type picks nothing.</b> It is a set of serialized references
    /// filled in by <c>MatchSceneBuilder</c> from paths recorded on issue #44
    /// and in <c>docs/roster.md</c>, where each choice was put to the developer
    /// and answered before anything was imported. Art and animation are never
    /// decided unattended on this project, and a field with a sensible-looking
    /// default would be exactly that decision made quietly. So there are no
    /// defaults: an unfilled field throws by name.
    /// </para>
    /// <para>
    /// <b>Models, weapons and tower clips are all per unit type.</b> The lookup
    /// is by the id in <c>content/units.txt</c>, so the Necromancer and the
    /// Skeleton Warrior are two different bodies on the board rather than two
    /// rows that happen to draw the same. What is still shared is the creep
    /// animation — one walk and one death for all of them — because all nine
    /// models are on <c>Rig_Medium</c> and a clip from any bank drives any of
    /// them.
    /// </para>
    /// <para>
    /// <b>The tower's weapon and clips were shared once, and that was the bug.</b>
    /// One bow and one set of three clips hung off <c>Delivery</c> rather than
    /// off the unit, so the mage — the only projectile row in
    /// <c>content/units.txt</c> — drew the bow, while the archer and the ranger,
    /// which are hitscan, drew nothing at all. A unit's weapon and the clips
    /// animated for that weapon are one choice, so they are made together, per
    /// unit, on <see cref="UnitArt"/>.
    /// </para>
    /// <para>
    /// <b>Serialized references rather than a runtime lookup.</b>
    /// <c>Resources.Load</c> and <c>AssetDatabase</c> are both wrong here —
    /// the first needs a magic folder and silently returns null when the path
    /// drifts, the second does not exist in a player at all. A serialized
    /// reference is checked when the scene is built, survives into the build,
    /// and shows up in a diff of the generated scene when it changes.
    /// </para>
    /// <para>
    /// The tower's three clips are mapped one per simulation state, which is
    /// what makes "plays its attack clip in step with actually firing" a
    /// property of the mapping rather than something to keep in sync. There is
    /// no fourth clip and no blend between them: the simulation says which state
    /// the tower is in and for how long, and that is the whole of the animation
    /// logic.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class MatchArt
    {
        /// <summary>The size a tower is drawn at — the baseline everything else is read against.</summary>
        public const float TowerScale = 1f;

        /// <summary>
        /// The size every creep is drawn at, so a body is unmistakably smaller
        /// than the thing shooting at it from any camera angle.
        /// </summary>
        public const float CreepScale = 0.5f;

        [Header("Per unit type")]
        [SerializeField]
        [Tooltip("One entry per row in content/units.txt: the model it draws as, and how big.")]
        private List<UnitArt> units = new List<UnitArt>();

        [Header("Creep clips")]
        [SerializeField]
        [Tooltip("Walking_A. Sampled at a phase derived from distance travelled, never from elapsed time.")]
        private AnimationClip creepWalkClip;

        [SerializeField]
        [Tooltip("Death_A. Played across exactly the tick duration the simulation gave the Dying state.")]
        private AnimationClip creepDeathClip;

        /// <summary>
        /// A bundle built in memory, for a caller that already has the assets
        /// in hand.
        /// </summary>
        /// <remarks>
        /// The scene builder does not use this — it fills in the serialized
        /// copy on the root object, because that is what has to survive into a
        /// build. This is for a caller holding the assets and wanting a match
        /// drawn with them, which is a test and is also anything that ever
        /// draws a second match with different art.
        /// </remarks>
        public static MatchArt Of(
            IEnumerable<UnitArt> units,
            AnimationClip walk,
            AnimationClip death) =>
            new MatchArt
            {
                units = new List<UnitArt>(units ?? throw new ArgumentNullException(nameof(units))),
                creepWalkClip = walk,
                creepDeathClip = death,
            };

        /// <summary>Every unit type that has art, in the order it was wired.</summary>
        public IReadOnlyList<UnitArt> Units => units;

        /// <summary>The walk cycle.</summary>
        public AnimationClip CreepWalkClip => Required(creepWalkClip, nameof(creepWalkClip));

        /// <summary>The death clip.</summary>
        public AnimationClip CreepDeathClip => Required(creepDeathClip, nameof(creepDeathClip));

        /// <summary>
        /// Everything one unit type is drawn with — its model, its size, what
        /// it holds and, on a tower, the three clips it is posed with.
        /// </summary>
        /// <remarks>
        /// Handed over whole rather than a field at a time. The model and the
        /// scale always travel together, the hands travel with the model that
        /// has the bones, and the clips only mean anything against the weapon
        /// they were animated for — so a caller taking them separately is a
        /// caller that can put a bow in a mage's hands.
        /// </remarks>
        public UnitArt ArtFor(int unitId) => For(unitId);

        /// <summary>The model a unit type is drawn with.</summary>
        public GameObject ModelFor(int unitId) => For(unitId).Model;

        /// <summary>How big that unit type is drawn.</summary>
        public float ScaleFor(int unitId) => For(unitId).Scale;

        /// <summary>
        /// True when every reference is filled in. Lets a caller check the whole
        /// bundle without catching exceptions one at a time.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                if (units == null || units.Count == 0)
                {
                    return false;
                }

                foreach (UnitArt unit in units)
                {
                    if (unit == null || !unit.IsComplete)
                    {
                        return false;
                    }
                }

                return creepWalkClip != null && creepDeathClip != null;
            }
        }

        /// <summary>
        /// The entry for a unit id, or a throw naming the id and listing the
        /// ids that do have art.
        /// </summary>
        /// <remarks>
        /// A linear walk of nine entries, called once per view object built
        /// rather than once per frame. A dictionary here would be a second copy
        /// of the list to keep in step with what Unity serialized.
        /// </remarks>
        private UnitArt For(int unitId)
        {
            if (units != null)
            {
                foreach (UnitArt unit in units)
                {
                    if (unit != null && unit.UnitId == unitId && unit.IsComplete)
                    {
                        return unit;
                    }
                }
            }

            throw new InvalidOperationException(
                "MatchArt has no art for unit " + unitId + ". Art is per unit type and every row in "
                + "content/units.txt needs an entry — the match scene is generated, so run "
                + "tools/build-match-scene.ps1 and commit what it writes. It has: " + WiredIds());
        }

        private string WiredIds()
        {
            if (units == null || units.Count == 0)
            {
                return "nothing";
            }

            var ids = new List<string>(units.Count);

            foreach (UnitArt unit in units)
            {
                ids.Add(unit == null ? "null" : unit.UnitId.ToString());
            }

            return string.Join(", ", ids);
        }

        /// <summary>
        /// The reference, or a throw naming which one is missing.
        /// </summary>
        /// <remarks>
        /// Named rather than null-checked at the use site, because a null model
        /// reaches the drawing code as "nothing appeared" and a null clip
        /// reaches it as "the rig stands in its bind pose". Both look like a
        /// bug in the animation, and neither says which field in a generated
        /// scene was not wired.
        /// </remarks>
        private static TAsset Required<TAsset>(TAsset asset, string field)
            where TAsset : UnityEngine.Object
        {
            if (asset == null)
            {
                throw new InvalidOperationException(
                    "MatchArt." + field + " is not wired up. The match scene is generated — run "
                    + "tools/build-match-scene.ps1 and commit what it writes. Nothing here is chosen "
                    + "at runtime, and nothing here has a default.");
            }

            return asset;
        }
    }
}
