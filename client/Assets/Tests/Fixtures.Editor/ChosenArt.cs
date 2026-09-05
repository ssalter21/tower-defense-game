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

        /// <summary>The folder the Paladin's model, props and atlases all import into.</summary>
        public const string PaladinFolder = "Assets/Art/Kaykit/mystery-monthly-series-4/paladin/";

        /// <summary>The folder the Cleric's model, props and both atlases import into.</summary>
        public const string ClericFolder = "Assets/Art/Kaykit/mystery-monthly-series-6/cleric/";

        /// <summary>The folder the Lorekeeper's model, tome and one atlas import into.</summary>
        public const string LorekeeperFolder =
            "Assets/Art/Kaykit/mystery-monthly-series-6/lorekeeper/";

        public const string ClericModelPath = ClericFolder + "Cleric.fbx";

        /// <summary>The Unravel's body, and the one character here with no alternate atlas.</summary>
        public const string LorekeeperModelPath = LorekeeperFolder + "Lorekeeper.fbx";

        public const string DruidModelPath = "Assets/Art/Kaykit/adventurers/Druid.fbx";

        public const string BarbarianModelPath = "Assets/Art/Kaykit/adventurers/Barbarian.fbx";

        /// <summary>The Slam's body, and the one thing here on the Large rig.</summary>
        public const string BarbarianLargeModelPath =
            "Assets/Art/Kaykit/adventurers/Barbarian_Large.fbx";

        public const string PaladinModelPath = PaladinFolder + "Paladin.fbx";

        public const string HelmetedPaladinModelPath = PaladinFolder + "Paladin_with_Helmet.fbx";

        public const string BowModelPath = "Assets/Art/Weapons/bow_withString.fbx";
        public const string StaffModelPath = "Assets/Art/Weapons/staff.fbx";
        public const string SwordModelPath = "Assets/Art/Weapons/sword_1handed.fbx";
        public const string SkeletonStaffModelPath = "Assets/Art/Weapons/Skeleton_Staff.fbx";
        public const string SkeletonBladeModelPath = "Assets/Art/Weapons/Skeleton_Blade.fbx";
        public const string SkeletonShieldAModelPath = "Assets/Art/Weapons/Skeleton_Shield_Large_A.fbx";
        public const string SkeletonShieldBModelPath = "Assets/Art/Weapons/Skeleton_Shield_Large_B.fbx";

        /// <summary>The Ranger's quiver, in the fist for want of a socket on the spine.</summary>
        public const string QuiverModelPath = "Assets/Art/Kaykit/adventurers/quiver.fbx";

        /// <summary>The Sergeant's off-hand shield, which the Shield Wall raises.</summary>
        public const string ShieldSquareModelPath = "Assets/Art/Kaykit/adventurers/shield_square.fbx";

        public const string AxeModelPath = "Assets/Art/Kaykit/adventurers/axe_2handed.fbx";

        /// <summary>The Berserker's bigger axe, which the Slam carries onto the Large rig.</summary>
        public const string LargeAxeModelPath = "Assets/Art/Kaykit/adventurers/axe_2handed_Large.fbx";

        public const string HammerModelPath = PaladinFolder + "paladin_hammer.fbx";

        public const string PaladinShieldModelPath = PaladinFolder + "paladin_shield.fbx";

        /// <summary>The open book the Blessing holds instead of its hammer.</summary>
        public const string BookModelPath = PaladinFolder + "paladin_book.fbx";

        /// <summary>The gold statue that stands on the tile beside the Blessing.</summary>
        public const string StatueModelPath = PaladinFolder + "paladin_statue.fbx";

        /// <summary>The open book the Mage holds.</summary>
        public const string SpellbookModelPath = "Assets/Art/Kaykit/adventurers/spellbook_open.fbx";

        /// <summary>The Cleric's tier-1 tome.</summary>
        public const string ClericTomeModelPath = ClericFolder + "Cleric_Tome.fbx";

        /// <summary>The mace the Bishop carries in place of the tome.</summary>
        public const string ClericMaceModelPath = ClericFolder + "Cleric_Mace.fbx";

        /// <summary>The basin that stands on the tile beside the Consecration.</summary>
        public const string ClericFontModelPath = ClericFolder + "Cleric_Font.fbx";

        /// <summary>The open tome the Unravel holds, off the Lorekeeper's own sheet.</summary>
        public const string LorekeeperTomeModelPath = LorekeeperFolder + "Lorekeeper_Tome.fbx";

        /// <summary>The Druid's staff, which every rung of his line carries.</summary>
        public const string DruidStaffModelPath = "Assets/Art/Kaykit/adventurers/druid_staff.fbx";

        /// <summary>The bare weirwood that stands on the tile beside the Overgrowth.</summary>
        public const string WeirwoodModelPath =
            "Assets/Art/Kaykit/forest-nature/Color8/Tree_Bare_1_C_Color8.fbx";

        /// <summary>
        /// How big the weirwood is drawn -- the one beside prop not authored
        /// beside the character it stands with. Signed in
        /// <c>docs/roster.md</c>, from the measurements issue #274 took.
        /// </summary>
        public const float WeirwoodScale = 0.55f;

        /// <summary>The Adventurers pack's second ranger colourway.</summary>
        public const string RangerAltAtlasPath = "Assets/Art/Kaykit/adventurers/ranger_texture_alt_A.png";

        public const string KnightAltAAtlasPath =
            "Assets/Art/Kaykit/adventurers/knight_texture_alt_A.png";

        public const string KnightAltBAtlasPath =
            "Assets/Art/Kaykit/adventurers/knight_texture_alt_B.png";

        public const string BarbarianAltAtlasPath =
            "Assets/Art/Kaykit/adventurers/barbarian_texture_alt_A.png";

        public const string PaladinAltAtlasPath = PaladinFolder + "paladin_texture_B.png";

        /// <summary>The Adventurers pack's second mage colourway, which the Sorcerer wears.</summary>
        public const string MageAltAtlasPath = "Assets/Art/Kaykit/adventurers/mage_texture_alt_A.png";

        public const string ClericAltAtlasPath = ClericFolder + "cleric_texture_B.png";

        public const string DruidAltAAtlasPath =
            "Assets/Art/Kaykit/adventurers/druid_texture_alt_A.png";

        public const string DruidAltBAtlasPath =
            "Assets/Art/Kaykit/adventurers/druid_texture_alt_B.png";

        /// <summary>Where every bank of both rigs is imported.</summary>
        public const string ClipBankFolder = "Assets/Art/Animations/";

        public const string MovementBankPath = ClipBankFolder + "Rig_Medium_MovementBasic.fbx";
        public const string GeneralBankPath = ClipBankFolder + "Rig_Medium_General.fbx";
        public const string RangedBankPath = ClipBankFolder + "Rig_Medium_CombatRanged.fbx";
        public const string MeleeBankPath = ClipBankFolder + "Rig_Medium_CombatMelee.fbx";

        /// <summary>The Large rig's banks, reached only by a clip name that says so.</summary>
        public const string LargeGeneralBankPath = ClipBankFolder + "Rig_Large_General.fbx";

        public const string LargeMeleeBankPath = ClipBankFolder + "Rig_Large_CombatMelee.fbx";

        /// <summary>Every bank of both rigs, which a qualified name is matched against.</summary>
        private static readonly string[] AllBankPaths =
        {
            MovementBankPath, GeneralBankPath, RangedBankPath, MeleeBankPath,
            LargeGeneralBankPath, LargeMeleeBankPath,
        };

        public const string WalkClipName = "Walking_A";
        public const string DeathClipName = "Death_A";
        public const string RestClipName = "Idle_A";
        public const string BowIdleClipName = "Ranged_Bow_Idle";
        public const string BowDrawClipName = "Ranged_Bow_Draw";
        public const string BowReleaseClipName = "Ranged_Bow_Release";
        public const string SpellcastClipName = "Ranged_Magic_Spellcasting";

        /// <summary>The cast the Cleric and Druid lines swing with.</summary>
        public const string ShootClipName = "Ranged_Magic_Shoot";
        public const string ChopClipName = "Melee_1H_Attack_Chop";
        public const string TwoHandedChopClipName = "Melee_2H_Attack_Chop";

        /// <summary>The raised guard the Shield Wall stands in between swings.</summary>
        public const string BlockingClipName = "Melee_Blocking";

        /// <summary>The Slam's swing, which is on the Large rig alone and names its bank.</summary>
        public const string SlamClipName = "Rig_Large_CombatMelee/Melee_2H_Slam";

        /// <summary><see cref="RestClipName"/> out of the other rig's bank.</summary>
        public const string LargeRestClipName = "Rig_Large_General/Idle_A";

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

        public const string AxeNode = "axe_2handed";

        public const string LargeAxeNode = "axe_2handed_Large";

        public const string HammerNode = "paladin_hammer";

        public const string BookNode = "paladin_book";

        public const string SpellbookNode = "spellbook_open";

        public const string ClericTomeNode = "Cleric_Tome";

        public const string ClericMaceNode = "Cleric_Mace";

        public const string LorekeeperTomeNode = "Lorekeeper_Tome";

        public const string DruidStaffNode = "druid_staff";

        /// <summary>
        /// The bow's own origin -- the grip the bone puts in the fist, which is
        /// where the string is drawn back from.
        /// </summary>
        public static readonly EffectAnchor Bow = EffectAnchor.At(BowNode);

        /// <summary>
        /// The orb on the end of the Sorcerer's staff. The direction is the axis
        /// <see cref="StaffQuarterTurn"/> was measured from -- shaft along the
        /// prop's own local +Y, orb at the +Y end -- and the distance is not
        /// written down anywhere, because <see cref="EffectAnchor"/> reads it
        /// off the mesh.
        /// </summary>
        public static readonly EffectAnchor StaffTip = EffectAnchor.AtTipOf(StaffNode, Vector3.up);

        /// <summary>The point of the Soldier's sword, whose blade runs along the same axis.</summary>
        public static readonly EffectAnchor SwordTip = EffectAnchor.AtTipOf(SwordNode, Vector3.up);

        /// <summary>The head of the Barbarian's two-handed axe.</summary>
        public static readonly EffectAnchor AxeHead = EffectAnchor.AtTipOf(AxeNode, Vector3.up);

        /// <summary>The head of the bigger axe the Berserker and the Slam swing.</summary>
        public static readonly EffectAnchor LargeAxeHead =
            EffectAnchor.AtTipOf(LargeAxeNode, Vector3.up);

        /// <summary>The head of the Paladin's hammer.</summary>
        public static readonly EffectAnchor HammerHead = EffectAnchor.AtTipOf(HammerNode, Vector3.up);

        /// <summary>
        /// The open book itself, and no tip -- a book is held rather than
        /// swung, and the far end of one is a corner of the cover.
        /// </summary>
        public static readonly EffectAnchor Book = EffectAnchor.At(BookNode);

        /// <summary>The open spellbook in the Mage's hand, held rather than swung.</summary>
        public static readonly EffectAnchor Spellbook = EffectAnchor.At(SpellbookNode);

        /// <summary>The Cleric's tome, held the same way.</summary>
        public static readonly EffectAnchor ClericTome = EffectAnchor.At(ClericTomeNode);

        /// <summary>The Unravel's tome, held the same way.</summary>
        public static readonly EffectAnchor LorekeeperTome = EffectAnchor.At(LorekeeperTomeNode);

        /// <summary>The head of the Bishop's mace, whose shaft runs along the same axis.</summary>
        public static readonly EffectAnchor MaceHead = EffectAnchor.AtTipOf(ClericMaceNode, Vector3.up);

        /// <summary>The head of the Druid's staff.</summary>
        public static readonly EffectAnchor DruidStaffTip =
            EffectAnchor.AtTipOf(DruidStaffNode, Vector3.up);

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
        /// <para>
        /// Three rows stand beside something: the Blessing's statue, the
        /// Consecration's font and the Overgrowth's weirwood. The fourth beside
        /// look <c>docs/roster.md</c> signs, the Engineer's turret, is on a line
        /// nobody has dressed yet.
        /// </para>
        /// <para>
        /// A rung inherits what the rung below it holds — the Sergeant and the
        /// Shield Wall carry the Soldier's sword, the Slam the Berserker's axe,
        /// the Blessing the Templar's shield and the Elder and the Overgrowth
        /// the Druid's staff — because a <c>Looks</c> line in
        /// <c>docs/roster.md</c> names what changes at that rung and not
        /// everything the body carries. What it does name replaces what was in
        /// that hand: the Bishop's mace takes the hand the Cleric's tome was
        /// in, as the Blessing's book takes the Templar's hammer's — that page
        /// does not say where the tome goes, and moving it to the off hand
        /// would be inventing an assignment rather than reading one. A colour
        /// does not carry at all: it is one of the three things that tell a
        /// rung apart, and that page gives tier 3 the second model instead
        /// wherever one exists, so the Slam wears the atlas its own model
        /// imports with and so does the Unravel — the Lorekeeper has no
        /// alternate atlas anywhere in the collection, and the Sorcerer's
        /// belongs to another character's UVs.
        /// </para>
        /// <para>
        /// The three Paladin rows carry no clips. That page names a clip on
        /// every rung of the Knight, Barbarian, Cleric and Druid lines and none
        /// on any rung of the Paladin's, whose windup and backswing carry its
        /// <c>_</c> for an unsigned number, so those rows stand in their bind
        /// pose rather than being posed by a clip this table picked. Where it
        /// does name one it is the swing — <c>Melee_2H_Attack_Chop</c> for the
        /// Barbarian, <c>Ranged_Magic_Shoot</c> for the Cleric and the Druid —
        /// so that clip is the windup with <see cref="RestClipName"/> either
        /// side of it. Where the page means the resting stance it says so, as
        /// the Shield Wall's raised guard does.
        /// </para>
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
            EffectAnchor anchor,
            (string model, float scale, Vector3 offset) beside)[] UnitPaths =
        {
            (1, MinionModelPath, MatchArt.CreepScale, null,
                null, null, null, null, null, default, default, default, default),
            (2, RogueModelPath, MatchArt.CreepScale, null,
                null, null, null, null, null, default, default, default, default),
            (3, RangerModelPath, MatchArt.TowerScale, null,
                null, BowModelPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName, default, BowFlip,
                Bow, default),
            (4, MageModelPath, MatchArt.TowerScale, null,
                SpellbookModelPath, null, RestClipName, SpellcastClipName, RestClipName, default, default,
                Spellbook, default),
            (7, SkeletonMageModelPath, MatchArt.CreepScale, null,
                SkeletonStaffModelPath, null, null, null, null, StaffQuarterTurn, default, default, default),
            (11, KnightModelPath, MatchArt.TowerScale, null,
                SwordModelPath, null, RestClipName, ChopClipName, RestClipName, default, default,
                SwordTip, default),
            (12, MinionModelPath, MatchArt.CreepScale, null,
                SkeletonBladeModelPath, SkeletonShieldAModelPath, null, null, null, default, default, default,
                default),
            (13, WarriorModelPath, MatchArt.CreepScale, null,
                SkeletonBladeModelPath, SkeletonShieldBModelPath, null, null, null, default, default, default,
                default),
            (14, RangerModelPath, MatchArt.TowerScale, RangerAltAtlasPath,
                QuiverModelPath, BowModelPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName,
                default, BowFlip, Bow, default),
            (15, KnightModelPath, MatchArt.TowerScale, KnightAltAAtlasPath,
                SwordModelPath, ShieldSquareModelPath, RestClipName, ChopClipName, RestClipName,
                default, default, SwordTip, default),
            (16, KnightModelPath, MatchArt.TowerScale, KnightAltBAtlasPath,
                SwordModelPath, ShieldSquareModelPath, BlockingClipName, ChopClipName, BlockingClipName,
                default, default, SwordTip, default),
            (17, BarbarianModelPath, MatchArt.TowerScale, null,
                AxeModelPath, null, RestClipName, TwoHandedChopClipName, RestClipName,
                default, default, AxeHead, default),
            (18, BarbarianModelPath, MatchArt.TowerScale, BarbarianAltAtlasPath,
                LargeAxeModelPath, null, RestClipName, TwoHandedChopClipName, RestClipName,
                default, default, LargeAxeHead, default),
            (19, BarbarianLargeModelPath, MatchArt.TowerScale, null,
                LargeAxeModelPath, null, LargeRestClipName, SlamClipName, LargeRestClipName,
                default, default, LargeAxeHead, default),
            (20, PaladinModelPath, MatchArt.TowerScale, null,
                HammerModelPath, null, null, null, null,
                default, default, HammerHead, default),
            (21, HelmetedPaladinModelPath, MatchArt.TowerScale, null,
                HammerModelPath, PaladinShieldModelPath, null, null, null,
                default, default, HammerHead, default),
            (22, HelmetedPaladinModelPath, MatchArt.TowerScale, PaladinAltAtlasPath,
                BookModelPath, PaladinShieldModelPath, null, null, null,
                default, default, Book, (StatueModelPath, 1f, BesideProp.NextTile)),
            (23, ClericModelPath, MatchArt.TowerScale, null,
                ClericTomeModelPath, null, RestClipName, ShootClipName, RestClipName,
                default, default, ClericTome, default),
            (24, ClericModelPath, MatchArt.TowerScale, ClericAltAtlasPath,
                ClericMaceModelPath, null, RestClipName, ShootClipName, RestClipName,
                default, default, MaceHead, default),
            (25, ClericModelPath, MatchArt.TowerScale, ClericAltAtlasPath,
                ClericMaceModelPath, null, RestClipName, ShootClipName, RestClipName,
                default, default, MaceHead, (ClericFontModelPath, 1f, BesideProp.NextTile)),
            (26, MageModelPath, MatchArt.TowerScale, MageAltAtlasPath,
                StaffModelPath, null, RestClipName, SpellcastClipName, RestClipName,
                StaffQuarterTurn, default, StaffTip, default),
            (27, LorekeeperModelPath, MatchArt.TowerScale, null,
                LorekeeperTomeModelPath, null, RestClipName, SpellcastClipName, RestClipName,
                default, default, LorekeeperTome, default),
            (28, DruidModelPath, MatchArt.TowerScale, null,
                DruidStaffModelPath, null, RestClipName, ShootClipName, RestClipName,
                StaffQuarterTurn, default, DruidStaffTip, default),
            (29, DruidModelPath, MatchArt.TowerScale, DruidAltAAtlasPath,
                DruidStaffModelPath, null, RestClipName, ShootClipName, RestClipName,
                StaffQuarterTurn, default, DruidStaffTip, default),
            (30, DruidModelPath, MatchArt.TowerScale, DruidAltBAtlasPath,
                DruidStaffModelPath, null, RestClipName, ShootClipName, RestClipName,
                StaffQuarterTurn, default, DruidStaffTip,
                (WeirwoodModelPath, WeirwoodScale, BesideProp.NextTile)),
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
                    MaybeTexture(u.texture),
                    BesideProp.Standing(MaybeModel(u.beside.model), u.beside.scale, u.beside.offset)))
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
        /// A clip by name from whichever bank holds it. A bare name searches
        /// the four <c>Rig_Medium</c> banks, because which of them a clip lives
        /// in is the pack's business and not a choice anybody signed off;
        /// <c>Rig_Large_General/Idle_A</c> names its bank and searches only
        /// that, because <c>Idle_A</c>, <c>Walking_A</c> and <c>Death_A</c>
        /// exist in both rigs and a medium clip on a Large body draws wrongly
        /// rather than failing.
        /// </summary>
        private static AnimationClip MaybeClip(string name)
        {
            if (name == null)
            {
                return null;
            }

            int slash = name.IndexOf('/');
            string wanted = slash < 0 ? name : name.Substring(slash + 1);
            string qualified = slash < 0 ? null : ClipBankFolder + name.Substring(0, slash) + ".fbx";

            // Filtered out of the declared banks rather than composed into a
            // path, so a bank name with a typo in it comes back as "no bank of
            // that name" instead of as an empty search of a path nothing is at.
            string[] banks = qualified == null
                ? new[] { MovementBankPath, GeneralBankPath, RangedBankPath, MeleeBankPath }
                : AllBankPaths.Where(b => b == qualified).ToArray();

            if (banks.Length == 0)
            {
                throw new InvalidOperationException(
                    "No bank called '" + name.Substring(0, slash) + "' among " +
                    string.Join(", ", AllBankPaths) + ".");
            }

            foreach (string bank in banks)
            {
                AnimationClip found = Clips(bank).FirstOrDefault(c => c.name == wanted);

                if (found != null)
                {
                    return found;
                }
            }

            throw new InvalidOperationException(
                "No clip called '" + wanted + "' in " + string.Join(", ", banks) + ".");
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
