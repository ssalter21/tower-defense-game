using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.Fixtures
{
    /// <summary>
    /// The editor adapter for the art seam: the assets the fixtures draw a
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
        public const string MinionModelPath = "Assets/Art/Characters/Skeleton_Minion.fbx";
        public const string RogueModelPath = "Assets/Art/Characters/Skeleton_Rogue.fbx";
        public const string SkeletonMageModelPath = "Assets/Art/Characters/Skeleton_Mage.fbx";
        public const string WarriorModelPath = "Assets/Art/Characters/Skeleton_Warrior.fbx";
        public const string KnightModelPath = "Assets/Art/Characters/Knight.fbx";
        public const string RangerModelPath = "Assets/Art/Characters/Ranger.fbx";
        public const string MageModelPath = "Assets/Art/Characters/Mage.fbx";

        public const string BowModelPath = "Assets/Art/Weapons/bow_withString.fbx";
        public const string StaffModelPath = "Assets/Art/Weapons/staff.fbx";
        public const string SwordModelPath = "Assets/Art/Weapons/sword_1handed.fbx";
        public const string SkeletonStaffModelPath = "Assets/Art/Weapons/Skeleton_Staff.fbx";
        public const string SkeletonBladeModelPath = "Assets/Art/Weapons/Skeleton_Blade.fbx";
        public const string SkeletonShieldAModelPath = "Assets/Art/Weapons/Skeleton_Shield_Large_A.fbx";
        public const string SkeletonShieldBModelPath = "Assets/Art/Weapons/Skeleton_Shield_Large_B.fbx";

        /// <summary>The Ranger's quiver, in the fist for want of a socket on the spine.</summary>
        public const string QuiverModelPath = "Assets/Art/Kaykit/adventurers/quiver.fbx";

        /// <summary>The Adventurers pack's second ranger colourway.</summary>
        public const string RangerAltAtlasPath = "Assets/Art/Kaykit/adventurers/ranger_texture_alt_A.png";

        public const string MovementBankPath = "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx";
        public const string GeneralBankPath = "Assets/Art/Animations/Rig_Medium_General.fbx";
        public const string RangedBankPath = "Assets/Art/Animations/Rig_Medium_CombatRanged.fbx";
        public const string MeleeBankPath = "Assets/Art/Animations/Rig_Medium_CombatMelee.fbx";

        public const string WalkClipName = "Walking_A";
        public const string DeathClipName = "Death_A";
        public const string RestClipName = "Idle_A";
        public const string BowIdleClipName = "Ranged_Bow_Idle";
        public const string BowDrawClipName = "Ranged_Bow_Draw";
        public const string BowReleaseClipName = "Ranged_Bow_Release";
        public const string SpellcastClipName = "Ranged_Magic_Spellcasting";
        public const string ChopClipName = "Melee_1H_Attack_Chop";

        /// <summary>The bow's half turn -- it is the only left-hand weapon.</summary>
        public static readonly Vector3 BowFlip = new Vector3(0f, 180f, 0f);

        /// <summary>
        /// The quarter turn a staff is hung at, which stands it on end. Nothing
        /// about the staff is inverted -- it is horizontal. Measured on 14
        /// August 2026 from the mesh's vertices expressed in the hand bone's own
        /// frame: the shaft runs along the bone's local +Y with the orb at the
        /// +Y end, and in the Mage's Idle_A that axis points forward, world
        /// (0.263, 0, 0.965), so the shaft lies flat and the orb comes to rest
        /// out by the feet. The bone's local +X is world (0, 1, 0) in that same
        /// pose -- exactly up, out of the fist -- so the quarter turn about Z
        /// that carries the shaft from local +Y onto local +X is what stands it
        /// up.
        /// <para>
        /// Only exactly upright in the pose it was measured in, because a weapon
        /// parented to a hand turns with the arm. The Necromancer's capture pose
        /// is a quarter of the way through Walking_A and leans about 43 degrees,
        /// head-up.
        /// </para>
        /// <para>
        /// Re-derive this rather than copying it -- from the vertices and the
        /// bone's world basis, and NOT from the [grip] bounds
        /// tools/capture-armed-roster.ps1 logs. Those are a world AABB
        /// re-expressed in the bone's frame, a box drawn round a box, and they
        /// are why the first attempt at this number was a half turn: they cannot
        /// tell a staff's orb from a sword's tip.
        /// </para>
        /// </summary>
        public static readonly Vector3 StaffQuarterTurn = new Vector3(0f, 0f, -90f);

        /// <summary>
        /// What a held prop's transform is called once <see cref="WeaponSocket"/>
        /// has put it on the bone: the asset's name, which is the FBX's file
        /// name. An effect anchor names one of these, or a bone.
        /// </summary>
        public const string BowNode = "bow_withString";

        public const string StaffNode = "staff";

        public const string SwordNode = "sword_1handed";

        /// <summary>
        /// The bow's own origin -- the grip the bone puts in the fist, which is
        /// where the string is drawn back from.
        /// </summary>
        public static readonly EffectAnchor Bow = EffectAnchor.At(BowNode);

        /// <summary>
        /// The orb on the end of the Mage's staff. The direction is the axis
        /// <see cref="StaffQuarterTurn"/> was measured from -- shaft along the
        /// prop's own local +Y, orb at the +Y end -- and the distance is not
        /// written down anywhere, because <see cref="EffectAnchor"/> reads it
        /// off the mesh.
        /// </summary>
        public static readonly EffectAnchor StaffTip = EffectAnchor.AtTipOf(StaffNode, Vector3.up);

        /// <summary>The point of the Soldier's sword, whose blade runs along the same axis.</summary>
        public static readonly EffectAnchor SwordTip = EffectAnchor.AtTipOf(SwordNode, Vector3.up);

        /// <summary>
        /// What each row in <c>content/units.txt</c> that has art is drawn as,
        /// and how big, as signed in <c>docs/roster.md</c>. A row that has none
        /// yet is on <see cref="UnboundUnits"/>'s list instead.
        /// </summary>
        /// <remarks>
        /// The Minion and the Skeleton share the minion skin, and the Archer
        /// and the Ranger share the ranger — told apart by the Ranger's own
        /// atlas and the quiver in its hand, because size says which side a row
        /// is on and nothing else. The scale numbers come from
        /// <see cref="MatchArt"/> rather than being typed again here, so this
        /// table and the builder's can disagree about which model a unit takes
        /// but never about what a half is.
        /// </remarks>
        public static readonly (
            int unitId,
            string model,
            float scale,
            string texture,
            string rightHand,
            string leftHand,
            string idle,
            string windup,
            string backswing,
            Vector3 rightTilt,
            Vector3 leftTilt,
            EffectAnchor anchor)[] UnitPaths =
        {
            (1, MinionModelPath, MatchArt.CreepScale, null,
                null, null, null, null, null, default, default, default),
            (2, RogueModelPath, MatchArt.CreepScale, null,
                null, null, null, null, null, default, default, default),
            (3, RangerModelPath, MatchArt.TowerScale, null,
                null, BowModelPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName, default, BowFlip,
                Bow),
            (4, MageModelPath, MatchArt.TowerScale, null,
                StaffModelPath, null, RestClipName, SpellcastClipName, RestClipName, StaffQuarterTurn, default,
                StaffTip),
            (7, SkeletonMageModelPath, MatchArt.CreepScale, null,
                SkeletonStaffModelPath, null, null, null, null, StaffQuarterTurn, default, default),
            (11, KnightModelPath, MatchArt.TowerScale, null,
                SwordModelPath, null, RestClipName, ChopClipName, RestClipName, default, default,
                SwordTip),
            (12, MinionModelPath, MatchArt.CreepScale, null,
                SkeletonBladeModelPath, SkeletonShieldAModelPath, null, null, null, default, default, default),
            (13, WarriorModelPath, MatchArt.CreepScale, null,
                SkeletonBladeModelPath, SkeletonShieldBModelPath, null, null, null, default, default, default),
            (14, RangerModelPath, MatchArt.TowerScale, RangerAltAtlasPath,
                QuiverModelPath, BowModelPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName,
                default, BowFlip, Bow),
        };

        /// <summary>Installs this adapter, in every editor domain, before play mode.</summary>
        [InitializeOnLoadMethod]
        private static void Install() => MatchArtSource.Use(new Adapter());

        /// <summary>
        /// Every asset above, loaded now, and a stand-in for each row that has
        /// no art yet.
        /// </summary>
        /// <remarks>
        /// <see cref="UnboundUnits"/> is the one thing here that is taken from
        /// the builder's side of the seam rather than written out again. The
        /// duplication above exists so that two tables can disagree about which
        /// model a unit takes; a row with no art has no such choice in it, and
        /// two lists of which rows those are could only ever disagree by one of
        /// them being stale.
        /// </remarks>
        public static MatchArt Load() =>
            MatchArt.Of(
                UnitPaths.Select(u => UnitArt.Armed(
                    u.unitId,
                    Model(u.model),
                    u.scale,
                    MaybeModel(u.rightHand),
                    MaybeModel(u.leftHand),
                    MaybeClip(u.idle),
                    MaybeClip(u.windup),
                    MaybeClip(u.backswing),
                    u.rightTilt,
                    u.leftTilt,
                    u.anchor,
                    MaybeTexture(u.texture)))
                    .Concat(UnboundUnits.StandIns()),
                Clip(MovementBankPath, WalkClipName),
                Clip(GeneralBankPath, DeathClipName));

        private static GameObject MaybeModel(string path) => path == null ? null : Model(path);

        /// <summary>Every atlas a row in the table above names, each named once.</summary>
        public static IEnumerable<string> TexturePaths =>
            UnitPaths.Select(u => u.texture).Where(t => t != null).Distinct();

        /// <summary>
        /// The atlas at a path, or null for a row drawn in the one its model
        /// imported wearing.
        /// </summary>
        private static Texture2D MaybeTexture(string path)
        {
            if (path == null)
            {
                return null;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
            {
                throw new InvalidOperationException("Nothing imported at " + path);
            }

            return texture;
        }

        /// <summary>
        /// A clip by name from whichever bank holds it. The fixture searches
        /// rather than being told the bank, because which bank a clip lives in
        /// is the pack's business and not a choice anybody signed off.
        /// </summary>
        private static AnimationClip MaybeClip(string name)
        {
            if (name == null)
            {
                return null;
            }

            foreach (string bank in new[] { MovementBankPath, GeneralBankPath, RangedBankPath, MeleeBankPath })
            {
                AnimationClip found = Clips(bank).FirstOrDefault(c => c.name == name);

                if (found != null)
                {
                    return found;
                }
            }

            throw new InvalidOperationException("No clip called '" + name + "' in any of the four banks.");
        }

        /// <summary>Every model path in the table above, each named once.</summary>
        public static IEnumerable<string> ModelPaths => UnitPaths.Select(u => u.model).Distinct();

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
            AnimationClip[] clips = Clips(bank);

            AnimationClip clip = clips.FirstOrDefault(c => c.name == name);

            if (clip == null)
            {
                throw new InvalidOperationException(
                    "No clip named '" + name + "' in " + bank + ". Found: "
                    + string.Join(", ", clips.Select(c => c.name)));
            }

            return clip;
        }

        /// <summary>Every real clip in a bank, the preview duplicates dropped.</summary>
        private static AnimationClip[] Clips(string bank) =>
            AssetDatabase.LoadAllAssetsAtPath(bank)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();

        private sealed class Adapter : IMatchArtSource
        {
            public MatchArt Load() => ChosenArt.Load();
        }
    }
}
