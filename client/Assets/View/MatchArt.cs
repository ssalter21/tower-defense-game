using System;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The models and clips the match is drawn with — every one of them chosen
    /// by the developer, and none of them chosen here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This type picks nothing.</b> It is a set of serialized references
    /// filled in by <c>MatchSceneBuilder</c> from paths recorded on issue #44,
    /// where each choice was put to the developer and answered before anything
    /// was imported. Art and animation are never decided unattended on this
    /// project, and a field with a sensible-looking default would be exactly
    /// that decision made quietly. So there are no defaults: an unfilled field
    /// throws by name.
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
        [Header("Creeps")]
        [SerializeField]
        [Tooltip("The skeleton warrior. Both creep types are drawn with it; they differ in speed and HP.")]
        private GameObject creepModel;

        [SerializeField]
        [Tooltip("Walking_A. Sampled at a phase derived from distance travelled, never from elapsed time.")]
        private AnimationClip creepWalkClip;

        [SerializeField]
        [Tooltip("Death_A. Played across exactly the tick duration the simulation gave the Dying state.")]
        private AnimationClip creepDeathClip;

        [Header("The projectile tower — the skinned import path")]
        [SerializeField]
        [Tooltip("The Ranger. Rotates to face its target and draws a bow in step with firing.")]
        private GameObject projectileTowerModel;

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

        [Header("The hitscan tower — the static import path")]
        [SerializeField]
        [Tooltip("building_tower_A_blue. No rig, no clips: its shot is an event and a tracer.")]
        private GameObject hitscanTowerModel;

        /// <summary>
        /// A bundle built in memory, for a caller that already has the assets
        /// in hand.
        /// </summary>
        /// <remarks>
        /// The scene builder does not use this — it fills in the serialized
        /// copy on the root object, because that is what has to survive into a
        /// build. This is for a caller holding nine loaded assets and wanting a
        /// match drawn with them, which is a test and is also anything that
        /// ever draws a second match with different art.
        /// </remarks>
        public static MatchArt Of(
            GameObject creep,
            AnimationClip walk,
            AnimationClip death,
            GameObject projectileTower,
            GameObject bow,
            AnimationClip idle,
            AnimationClip windup,
            AnimationClip backswing,
            GameObject hitscanTower) =>
            new MatchArt
            {
                creepModel = creep,
                creepWalkClip = walk,
                creepDeathClip = death,
                projectileTowerModel = projectileTower,
                bowModel = bow,
                towerIdleClip = idle,
                towerWindupClip = windup,
                towerBackswingClip = backswing,
                hitscanTowerModel = hitscanTower,
            };

        /// <summary>The model every creep is drawn with.</summary>
        public GameObject CreepModel => Required(creepModel, nameof(creepModel));

        /// <summary>The walk cycle.</summary>
        public AnimationClip CreepWalkClip => Required(creepWalkClip, nameof(creepWalkClip));

        /// <summary>The death clip.</summary>
        public AnimationClip CreepDeathClip => Required(creepDeathClip, nameof(creepDeathClip));

        /// <summary>The skinned character that fires projectiles.</summary>
        public GameObject ProjectileTowerModel =>
            Required(projectileTowerModel, nameof(projectileTowerModel));

        /// <summary>The weapon it holds.</summary>
        public GameObject BowModel => Required(bowModel, nameof(bowModel));

        /// <summary>The clip for <see cref="Sim.TowerState.Idle"/>.</summary>
        public AnimationClip TowerIdleClip => Required(towerIdleClip, nameof(towerIdleClip));

        /// <summary>The clip for <see cref="Sim.TowerState.Windup"/>.</summary>
        public AnimationClip TowerWindupClip => Required(towerWindupClip, nameof(towerWindupClip));

        /// <summary>The clip for <see cref="Sim.TowerState.Backswing"/>.</summary>
        public AnimationClip TowerBackswingClip =>
            Required(towerBackswingClip, nameof(towerBackswingClip));

        /// <summary>The static building that fires hitscan shots.</summary>
        public GameObject HitscanTowerModel => Required(hitscanTowerModel, nameof(hitscanTowerModel));

        /// <summary>
        /// True when every reference is filled in. Lets a caller check the whole
        /// bundle without catching six exceptions one at a time.
        /// </summary>
        public bool IsComplete =>
            creepModel != null
            && creepWalkClip != null
            && creepDeathClip != null
            && projectileTowerModel != null
            && bowModel != null
            && towerIdleClip != null
            && towerWindupClip != null
            && towerBackswingClip != null
            && hitscanTowerModel != null;

        /// <summary>
        /// The reference, or a throw naming which one is missing.
        /// </summary>
        /// <remarks>
        /// Named rather than null-checked at the use site, because a null model
        /// reaches the drawing code as "nothing appeared" and a null clip
        /// reaches it as "the rig stands in its bind pose". Both look like a
        /// bug in the animation, and neither says which of nine fields in a
        /// generated scene was not wired.
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
