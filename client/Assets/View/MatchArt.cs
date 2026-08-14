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
    /// match. The three numbers it takes are on <see cref="MatchArt"/>.
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

        /// <summary>One unit's art, for a caller that has the model in hand.</summary>
        public static UnitArt Of(int unitId, GameObject model, float scale) =>
            new UnitArt { unitId = unitId, model = model, scale = scale };

        /// <summary>The row in <c>content/units.txt</c> this stands for.</summary>
        public int UnitId => unitId;

        /// <summary>The model.</summary>
        public GameObject Model => model;

        /// <summary>How much bigger or smaller than the imported model this draws.</summary>
        public float Scale => scale;

        /// <summary>
        /// True when both halves are filled in. A zero scale is as incomplete as
        /// a null model: it draws the unit at no size at all, which on screen is
        /// a unit that never appeared.
        /// </summary>
        public bool IsComplete => model != null && scale > 0f;
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
    /// <b>Models are per unit type; clips are per role.</b> The lookup is by
    /// the id in <c>content/units.txt</c>, so the Necromancer and the Skeleton
    /// Warrior are two different bodies on the board rather than two rows that
    /// happen to draw the same. What is still shared is the animation — one
    /// walk and one death for every creep, one clip per state for the tower
    /// that draws a bow — because all nine models are on <c>Rig_Medium</c> and
    /// a clip from any bank drives any of them.
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

        /// <summary>
        /// The Ranger's size. It shares the Archer's model and differs from it
        /// in one stat, so size is the only thing separating the two rungs.
        /// </summary>
        public const float RangerScale = 1.5f;

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

        [Header("The projectile tower — the skinned import path")]
        [SerializeField]
        [Tooltip("The bow, hung off handslot.l at runtime.")]
        private GameObject bowModel;

        [SerializeField]
        [Tooltip("Ranged_Bow_Idle — played while the tower is Idle.")]
        private AnimationClip towerIdleClip;

        [SerializeField]
        [Tooltip("Ranged_Bow_Draw — played across the Windup, which ends when the shot is released.")]
        private AnimationClip towerWindupClip;

        [SerializeField]
        [Tooltip("Ranged_Bow_Release — played across the Backswing, after the shot has landed.")]
        private AnimationClip towerBackswingClip;

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
            AnimationClip death,
            GameObject bow,
            AnimationClip idle,
            AnimationClip windup,
            AnimationClip backswing) =>
            new MatchArt
            {
                units = new List<UnitArt>(units ?? throw new ArgumentNullException(nameof(units))),
                creepWalkClip = walk,
                creepDeathClip = death,
                bowModel = bow,
                towerIdleClip = idle,
                towerWindupClip = windup,
                towerBackswingClip = backswing,
            };

        /// <summary>Every unit type that has art, in the order it was wired.</summary>
        public IReadOnlyList<UnitArt> Units => units;

        /// <summary>The walk cycle.</summary>
        public AnimationClip CreepWalkClip => Required(creepWalkClip, nameof(creepWalkClip));

        /// <summary>The death clip.</summary>
        public AnimationClip CreepDeathClip => Required(creepDeathClip, nameof(creepDeathClip));

        /// <summary>The weapon the projectile tower holds.</summary>
        public GameObject BowModel => Required(bowModel, nameof(bowModel));

        /// <summary>The clip for <see cref="Sim.TowerState.Idle"/>.</summary>
        public AnimationClip TowerIdleClip => Required(towerIdleClip, nameof(towerIdleClip));

        /// <summary>The clip for <see cref="Sim.TowerState.Windup"/>.</summary>
        public AnimationClip TowerWindupClip => Required(towerWindupClip, nameof(towerWindupClip));

        /// <summary>The clip for <see cref="Sim.TowerState.Backswing"/>.</summary>
        public AnimationClip TowerBackswingClip =>
            Required(towerBackswingClip, nameof(towerBackswingClip));

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

                return creepWalkClip != null
                    && creepDeathClip != null
                    && bowModel != null
                    && towerIdleClip != null
                    && towerWindupClip != null
                    && towerBackswingClip != null;
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
