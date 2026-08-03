using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using View;

namespace Tests.Fixtures
{
    /// <summary>
    /// The editor adapter for the art seam: the nine assets the fixtures draw a
    /// match with, by path, out of the <see cref="AssetDatabase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The paths are written out here a second time on purpose.</b>
    /// <c>MatchSceneBuilder</c> has its own list and this one is deliberately
    /// not asked of it: a fixture that took its art from the builder could not
    /// catch the builder choosing the wrong clip, because it would be drawing
    /// whatever the builder chose and asserting that it matched itself. Two
    /// lists that disagree are a failure; one list is a tautology.
    /// </para>
    /// <para>
    /// <b>It registers itself, and that is why the seam works at all.</b> The
    /// play-mode assembly must compile with no editor assembly in sight, so it
    /// cannot name this class — it names <see cref="MatchArtSource"/>, which is
    /// ordinary runtime code. This registers into that on domain load, which in
    /// the editor happens before play mode ever starts. In a player nothing
    /// registers, and the player adapter answers instead.
    /// </para>
    /// <para>
    /// Editor-only by assembly, not by <c>#if</c>. That distinction is the whole
    /// ticket: an <c>#if</c> around a test class deletes the tests and leaves
    /// the run green, while an editor-only assembly simply is not there and the
    /// thing that needs it has somewhere else to go.
    /// </para>
    /// </remarks>
    public static class ChosenArt
    {
        public const string CreepModelPath = "Assets/Art/Characters/Skeleton_Warrior.fbx";
        public const string ProjectileTowerModelPath = "Assets/Art/Characters/Ranger.fbx";
        public const string BowModelPath = "Assets/Art/Weapons/bow_withString.fbx";
        public const string HitscanTowerModelPath = "Assets/Art/Buildings/building_tower_A_blue.fbx";

        public const string MovementBankPath = "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx";
        public const string GeneralBankPath = "Assets/Art/Animations/Rig_Medium_General.fbx";
        public const string RangedBankPath = "Assets/Art/Animations/Rig_Medium_CombatRanged.fbx";

        public const string WalkClipName = "Walking_A";
        public const string DeathClipName = "Death_A";
        public const string TowerIdleClipName = "Ranged_Bow_Idle";
        public const string TowerWindupClipName = "Ranged_Bow_Draw";
        public const string TowerBackswingClipName = "Ranged_Bow_Release";

        /// <summary>Installs this adapter, in every editor domain, before play mode.</summary>
        [InitializeOnLoadMethod]
        private static void Install() => MatchArtSource.Use(new Adapter());

        /// <summary>The nine assets, loaded now.</summary>
        public static MatchArt Load() =>
            MatchArt.Of(
                Model(CreepModelPath),
                Clip(MovementBankPath, WalkClipName),
                Clip(GeneralBankPath, DeathClipName),
                Model(ProjectileTowerModelPath),
                Model(BowModelPath),
                Clip(RangedBankPath, TowerIdleClipName),
                Clip(RangedBankPath, TowerWindupClipName),
                Clip(RangedBankPath, TowerBackswingClipName),
                Model(HitscanTowerModelPath));

        private static GameObject Model(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (asset == null)
            {
                throw new InvalidOperationException("Nothing imported at " + path);
            }

            return asset;
        }

        /// <summary>
        /// A named clip out of an FBX bank. "__preview__" duplicates are editor
        /// thumbnail bookkeeping Unity hangs off any clip it has drawn an icon
        /// for; they never match a real name and only make the failure message
        /// longer.
        /// </summary>
        private static AnimationClip Clip(string bank, string name)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(bank)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();

            AnimationClip clip = clips.FirstOrDefault(c => c.name == name);

            if (clip == null)
            {
                throw new InvalidOperationException(
                    "No clip named '" + name + "' in " + bank + ". Found: "
                    + string.Join(", ", clips.Select(c => c.name)));
            }

            return clip;
        }

        private sealed class Adapter : IMatchArtSource
        {
            public MatchArt Load() => ChosenArt.Load();
        }
    }
}
