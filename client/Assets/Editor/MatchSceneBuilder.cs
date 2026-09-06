using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Generates the one scene this project has, and the two plain materials it
    /// references.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The scene is generated, not authored.</b> It holds one empty object
    /// carrying <see cref="MatchRoot"/> and nothing else, so there is nothing in
    /// it worth hand-editing and nothing in it a merge could lose. Everything
    /// that decides what the playfield looks like lives in
    /// <see cref="SceneFraming"/>, in C#, where a diff is readable.
    /// </para>
    /// <para>
    /// It runs from a shell — <c>tools/build-match-scene.ps1</c>, which is
    /// <c>-batchmode -executeMethod</c> and needs no editor session, no bridge
    /// and nobody at a keyboard. The menu item is a convenience for a human who
    /// happens to have the editor open, not the entry point.
    /// </para>
    /// </remarks>
    public static class MatchSceneBuilder
    {
        /// <summary>The scene, and the only one in the build.</summary>
        public const string ScenePath = "Assets/Scenes/Match.unity";

        /// <summary>The corridor's material.</summary>
        public const string RoadMaterialPath = "Assets/Materials/Road.mat";

        /// <summary>Everything else's material.</summary>
        public const string GrassMaterialPath = "Assets/Materials/Grass.mat";

        private const string BowPath = "Assets/Art/Weapons/bow_withString.fbx";

        private const string StaffPath = "Assets/Art/Weapons/staff.fbx";

        private const string SwordPath = "Assets/Art/Weapons/sword_1handed.fbx";

        private const string SkeletonStaffPath = "Assets/Art/Weapons/Skeleton_Staff.fbx";

        private const string SkeletonBladePath = "Assets/Art/Weapons/Skeleton_Blade.fbx";

        private const string SkeletonShieldAPath = "Assets/Art/Weapons/Skeleton_Shield_Large_A.fbx";

        private const string SkeletonShieldBPath = "Assets/Art/Weapons/Skeleton_Shield_Large_B.fbx";

        /// <summary>The Ranger's quiver, in the fist for want of a socket on the spine.</summary>
        private const string QuiverPath = "Assets/Art/Kaykit/adventurers/quiver.fbx";

        /// <summary>The folder the Paladin's model, props and atlases all import into.</summary>
        private const string PaladinFolder = "Assets/Art/Kaykit/mystery-monthly-series-4/paladin/";

        /// <summary>The folder the Cleric's model, props and both atlases import into.</summary>
        private const string ClericFolder = "Assets/Art/Kaykit/mystery-monthly-series-6/cleric/";

        /// <summary>The folder the Lorekeeper's model, tome and one atlas import into.</summary>
        private const string LorekeeperFolder =
            "Assets/Art/Kaykit/mystery-monthly-series-6/lorekeeper/";

        /// <summary>The open book the Mage holds.</summary>
        private const string SpellbookPath = "Assets/Art/Kaykit/adventurers/spellbook_open.fbx";

        /// <summary>The Cleric's tier-1 tome.</summary>
        private const string ClericTomePath = ClericFolder + "Cleric_Tome.fbx";

        /// <summary>The mace the Bishop carries in place of the tome.</summary>
        private const string ClericMacePath = ClericFolder + "Cleric_Mace.fbx";

        /// <summary>The basin that stands on the tile beside the Consecration.</summary>
        private const string ClericFontPath = ClericFolder + "Cleric_Font.fbx";

        /// <summary>The open tome the Unravel holds, off the Lorekeeper's own sheet.</summary>
        private const string LorekeeperTomePath = LorekeeperFolder + "Lorekeeper_Tome.fbx";

        /// <summary>The Druid's staff, which every rung of his line carries.</summary>
        private const string DruidStaffPath = "Assets/Art/Kaykit/adventurers/druid_staff.fbx";

        /// <summary>The Overwatch's body, and the only tower off the Adventurers pack's archers.</summary>
        private const string MarksmanPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/marksman/Marksman.fbx";

        /// <summary>What the Overwatch shoots with, in place of the rifle its own pack ships.</summary>
        private const string CrossbowPath = "Assets/Art/Kaykit/adventurers/crossbow_2handed.fbx";

        /// <summary>The Rogue's body, at the bottom rung of his line.</summary>
        private const string RoguePath = "Assets/Art/Kaykit/adventurers/Rogue.fbx";

        /// <summary>The hood, which is this line's second model and arrives at tier 2.</summary>
        private const string HoodedRoguePath = "Assets/Art/Kaykit/adventurers/Rogue_Hooded.fbx";

        /// <summary>What the Rogue line throws, one at the lower rungs and two at the top.</summary>
        private const string DaggerPath = "Assets/Art/Kaykit/adventurers/dagger.fbx";

        /// <summary>The Engineer's body, which all three rungs of his line are drawn on.</summary>
        private const string EngineerPath = "Assets/Art/Kaykit/adventurers/Engineer.fbx";

        /// <summary>The wrench in the Engineer's hand, which is not what fires.</summary>
        private const string WrenchPath = "Assets/Art/Kaykit/adventurers/engineer_Wrench.fbx";

        /// <summary>The machine that stands on the tile beside the Engineer and does the firing.</summary>
        private const string TurretPath = "Assets/Art/Kaykit/adventurers/turret_base.fbx";

        /// <summary>The bare weirwood that stands on the tile beside the Overgrowth.</summary>
        private const string WeirwoodPath =
            "Assets/Art/Kaykit/forest-nature/Color8/Tree_Bare_1_C_Color8.fbx";

        /// <summary>
        /// How big the weirwood is drawn, which is the one beside prop that is
        /// not authored beside the character it stands with.
        /// </summary>
        /// <remarks>
        /// At its own imported size the tree spreads 3.74 m, which is nearly two
        /// of this board's 2.0 m tiles and reaches back through the Druid. This
        /// brings the spread to 2.06 — the tile it stands on — and leaves it
        /// 2.89 m tall against a Druid of about two. Signed in
        /// <c>docs/roster.md</c>, measured on issue #274.
        /// </remarks>
        private const float WeirwoodScale = 0.55f;

        /// <summary>The Sergeant's off-hand shield, which the Shield Wall raises.</summary>
        private const string ShieldSquarePath = "Assets/Art/Kaykit/adventurers/shield_square.fbx";

        private const string AxePath = "Assets/Art/Kaykit/adventurers/axe_2handed.fbx";

        /// <summary>The Berserker's bigger axe, which the Slam carries onto the Large rig.</summary>
        private const string LargeAxePath = "Assets/Art/Kaykit/adventurers/axe_2handed_Large.fbx";

        private const string HammerPath = PaladinFolder + "paladin_hammer.fbx";

        private const string PaladinShieldPath = PaladinFolder + "paladin_shield.fbx";

        /// <summary>The open book the Blessing holds instead of its hammer.</summary>
        private const string BookPath = PaladinFolder + "paladin_book.fbx";

        /// <summary>The gold statue that stands on the tile beside the Blessing.</summary>
        private const string StatuePath = PaladinFolder + "paladin_statue.fbx";

        /// <summary>The Adventurers pack's second ranger colourway.</summary>
        private const string RangerAltAtlasPath =
            "Assets/Art/Kaykit/adventurers/ranger_texture_alt_A.png";

        private const string KnightAltAAtlasPath =
            "Assets/Art/Kaykit/adventurers/knight_texture_alt_A.png";

        private const string KnightAltBAtlasPath =
            "Assets/Art/Kaykit/adventurers/knight_texture_alt_B.png";

        private const string BarbarianAltAtlasPath =
            "Assets/Art/Kaykit/adventurers/barbarian_texture_alt_A.png";

        private const string PaladinAltAtlasPath = PaladinFolder + "paladin_texture_B.png";

        /// <summary>The Adventurers pack's second mage colourway, which the Sorcerer wears.</summary>
        private const string MageAltAtlasPath = "Assets/Art/Kaykit/adventurers/mage_texture_alt_A.png";

        private const string ClericAltAtlasPath = ClericFolder + "cleric_texture_B.png";

        private const string DruidAltAAtlasPath =
            "Assets/Art/Kaykit/adventurers/druid_texture_alt_A.png";

        private const string DruidAltBAtlasPath =
            "Assets/Art/Kaykit/adventurers/druid_texture_alt_B.png";

        /// <summary>The Adventurers pack's second rogue colourway, which the Fan of Knives wears.</summary>
        private const string RogueAltAtlasPath =
            "Assets/Art/Kaykit/adventurers/rogue_texture_alt_A.png";

        private const string EngineerAltAAtlasPath =
            "Assets/Art/Kaykit/adventurers/engineer_texture_alt_A.png";

        private const string EngineerAltBAtlasPath =
            "Assets/Art/Kaykit/adventurers/engineer_texture_alt_B.png";

        private const string WalkClipName = "Walking_A";

        private const string DeathClipName = "Death_A";

        /// <summary>The clip a tower rests in between shots, whatever it holds.</summary>
        private const string RestClipName = "Idle_A";

        private const string BowIdleClipName = "Ranged_Bow_Idle";

        private const string BowDrawClipName = "Ranged_Bow_Draw";

        private const string BowReleaseClipName = "Ranged_Bow_Release";

        private const string SpellcastClipName = "Ranged_Magic_Spellcasting";

        /// <summary>The cast the Cleric and Druid lines swing with.</summary>
        private const string ShootClipName = "Ranged_Magic_Shoot";

        private const string ChopClipName = "Melee_1H_Attack_Chop";

        private const string TwoHandedChopClipName = "Melee_2H_Attack_Chop";

        /// <summary>The raised guard the Shield Wall stands in between swings.</summary>
        private const string BlockingClipName = "Melee_Blocking";

        /// <summary>
        /// The sighted stance the Overwatch holds, and the only clip
        /// <c>docs/roster.md</c> names anywhere on that row.
        /// </summary>
        private const string AimingClipName = "Ranged_2H_Aiming";

        /// <summary>The Rogue's overarm, which is how that line delivers a dagger.</summary>
        private const string ThrowClipName = "Throw";

        /// <summary>The two-knife swing the Fan of Knives throws with.</summary>
        private const string DualwieldSliceClipName = "Melee_Dualwield_Attack_Slice";

        /// <summary>
        /// The Slam's swing, which exists on the Large rig alone and so names
        /// its bank.
        /// </summary>
        private const string SlamClipName = "Rig_Large_CombatMelee/Melee_2H_Slam";

        /// <summary>
        /// <see cref="RestClipName"/> on the Large rig: the same name, in the
        /// other rig's bank, driving the other rig's bones.
        /// </summary>
        private const string LargeRestClipName = "Rig_Large_General/Idle_A";

        /// <summary>
        /// The bow's half turn. Every weapon in this pack is authored for the
        /// right hand; the bow is the only one that goes in the left, and at
        /// the bone's own rotation it comes out with its belly curving into the
        /// archer and its string facing the target. Backwards, and it took
        /// somebody looking at it to notice.
        /// </summary>
        private static readonly Vector3 BowFlip = new Vector3(0f, 180f, 0f);

        /// <summary>
        /// The quarter turn a staff is hung at, which stands it on end.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nothing about the staff is inverted — it is horizontal.</b>
        /// Measured on 14 August 2026 from its vertices expressed in the hand
        /// bone's own frame: the shaft runs along the bone's local +Y and the
        /// orb is at the +Y end, which is the same axis and the same direction
        /// as the sword's blade. That is why the <c>[grip]</c> bounds could not
        /// tell the two apart, and why a half turn — the first guess — buried
        /// the staff in the body instead of righting it. In the Mage's
        /// <c>Idle_A</c> that bone axis points forward, world
        /// <c>(0.263, 0, 0.965)</c>, so the shaft lies flat and the orb comes to
        /// rest out by the feet.
        /// </para>
        /// <para>
        /// <b>The Soldier's sword is in exactly the same position, and is left
        /// there.</b> Measured the same way, in all three of its states: its
        /// blade also runs along the bone's local +Y, and it also comes out
        /// 90.0° off vertical with the tip level with the hand at world Y
        /// 0.536, pointing forward. So the geometry does not separate the two
        /// cases at all — what separates them is the read. A sword held out
        /// level at hip height is a stance; an orb resting by the feet is a
        /// staff somebody dropped. Which of those is wrong is the developer's
        /// call and not this table's, and issue #204 asked for the staffs and
        /// recorded the sword as reading correctly. Anyone changing that should
        /// change it on a ticket, not by noticing this paragraph.
        /// </para>
        /// <para>
        /// The bone's local +X is world <c>(0, 1, 0)</c> in that same pose —
        /// exactly up, out of the fist. So the correction is the quarter turn
        /// about Z that carries the shaft from local +Y onto local +X, and it is
        /// read off the measured bone frame rather than fitted to a screenshot.
        /// </para>
        /// <para>
        /// <b>It is only exactly upright in the pose it was measured in.</b> A
        /// weapon parented to a hand turns with the arm, so no fixed tilt can be
        /// right everywhere. This one was taken in <c>Idle_A</c> at frame 0,
        /// which is where the Mage stands. The Skeleton Mage is a creep and the
        /// roster capture poses it a quarter of the way through <c>Walking_A</c>,
        /// where the same bone axis is about 43° off vertical: head-up, and
        /// leaning. If a pose ever needs the staff dead upright regardless of
        /// the arm, that is a different mechanism — an aim constraint — and not
        /// a bigger number here.
        /// </para>
        /// </remarks>
        private static readonly Vector3 StaffQuarterTurn = new Vector3(0f, 0f, -90f);

        /// <summary>
        /// What a held prop's own transform is called once it is on the bone.
        /// </summary>
        /// <remarks>
        /// <see cref="WeaponSocket"/> names the instance after the asset, and an
        /// FBX's root node is named after its file, so these are the file names
        /// above without their folder or extension. An anchor naming one that
        /// is not there throws when the view is built.
        /// </remarks>
        private const string BowNode = "bow_withString";

        private const string StaffNode = "staff";

        private const string SwordNode = "sword_1handed";

        private const string AxeNode = "axe_2handed";

        private const string LargeAxeNode = "axe_2handed_Large";

        private const string HammerNode = "paladin_hammer";

        private const string BookNode = "paladin_book";

        private const string SpellbookNode = "spellbook_open";

        private const string ClericTomeNode = "Cleric_Tome";

        private const string ClericMaceNode = "Cleric_Mace";

        private const string LorekeeperTomeNode = "Lorekeeper_Tome";

        private const string DruidStaffNode = "druid_staff";

        private const string CrossbowNode = "crossbow_2handed";

        private const string DaggerNode = "dagger";

        private const string TurretNode = "turret_base";

        /// <summary>
        /// Which way along a shafted weapon its far end lies, in the prop's own
        /// local space.
        /// </summary>
        /// <remarks>
        /// The same axis <see cref="StaffQuarterTurn"/> was measured from: both
        /// the staff's shaft and the sword's blade run along local +Y with the
        /// orb and the tip at the +Y end, which is why the <c>[grip]</c> bounds
        /// could not tell the two apart. The distance is not written down —
        /// <see cref="EffectAnchor"/> takes it off the prop's own mesh, so a
        /// re-exported staff moves its own tip.
        /// </remarks>
        private static readonly Vector3 AlongTheShaft = Vector3.up;

        /// <summary>
        /// The bow's own origin, which is the grip the bone puts in the fist and
        /// the point the string is drawn back from. No tip: an arrow leaves an
        /// archer's hand and not the end of a limb.
        /// </summary>
        private static readonly EffectAnchor Bow = EffectAnchor.At(BowNode);

        /// <summary>The orb on the end of the Sorcerer's staff.</summary>
        private static readonly EffectAnchor StaffTip =
            EffectAnchor.AtTipOf(StaffNode, AlongTheShaft);

        /// <summary>The point of the Soldier's sword.</summary>
        private static readonly EffectAnchor SwordTip =
            EffectAnchor.AtTipOf(SwordNode, AlongTheShaft);

        /// <summary>The head of the Barbarian's two-handed axe.</summary>
        private static readonly EffectAnchor AxeHead =
            EffectAnchor.AtTipOf(AxeNode, AlongTheShaft);

        /// <summary>The head of the bigger axe the Berserker and the Slam swing.</summary>
        private static readonly EffectAnchor LargeAxeHead =
            EffectAnchor.AtTipOf(LargeAxeNode, AlongTheShaft);

        /// <summary>The head of the Paladin's hammer.</summary>
        private static readonly EffectAnchor HammerHead =
            EffectAnchor.AtTipOf(HammerNode, AlongTheShaft);

        /// <summary>
        /// The open book itself, and no tip. A book is held, not swung: its
        /// pages are where the Blessing's light comes from, and the far end of
        /// one is a corner of the cover.
        /// </summary>
        private static readonly EffectAnchor Book = EffectAnchor.At(BookNode);

        /// <summary>
        /// The open spellbook in the Mage's hand, and no tip, for the reason
        /// <see cref="Book"/> has none: a book is held rather than swung.
        /// </summary>
        private static readonly EffectAnchor Spellbook = EffectAnchor.At(SpellbookNode);

        /// <summary>The Cleric's tome, held the same way.</summary>
        private static readonly EffectAnchor ClericTome = EffectAnchor.At(ClericTomeNode);

        /// <summary>The Unravel's tome, held the same way.</summary>
        private static readonly EffectAnchor LorekeeperTome = EffectAnchor.At(LorekeeperTomeNode);

        /// <summary>The head of the Bishop's mace.</summary>
        private static readonly EffectAnchor MaceHead =
            EffectAnchor.AtTipOf(ClericMaceNode, AlongTheShaft);

        /// <summary>The head of the Druid's staff.</summary>
        private static readonly EffectAnchor DruidStaffTip =
            EffectAnchor.AtTipOf(DruidStaffNode, AlongTheShaft);

        /// <summary>
        /// The crossbow's own origin, which is the stock the bone puts in the
        /// fist. No tip, for the reason <see cref="Bow"/> has none: a bolt
        /// leaves the weapon a shooter is holding and not the end of a limb.
        /// </summary>
        private static readonly EffectAnchor Crossbow = EffectAnchor.At(CrossbowNode);

        /// <summary>
        /// The dagger's own origin, which is the grip and so is the hand.
        /// </summary>
        /// <remarks>
        /// No tip: this line throws its knives, so what leaves is the whole
        /// dagger from where it is held. The Fan of Knives carries two, both
        /// named after the same asset, and this resolves to whichever the
        /// lookup reaches first — one hand of the two, which is a point on the
        /// art either way.
        /// </remarks>
        private static readonly EffectAnchor Dagger = EffectAnchor.At(DaggerNode);

        /// <summary>
        /// The top of the turret standing beside the Engineer, which is where
        /// his shell leaves from rather than the man.
        /// </summary>
        /// <remarks>
        /// Up rather than <see cref="AlongTheShaft"/> by coincidence of axis
        /// and not of meaning: this is a barrel on a base rather than a shaft,
        /// and the far end of it is its top. How far up is not written down —
        /// <see cref="EffectAnchor"/> reads it off the mesh.
        /// </remarks>
        private static readonly EffectAnchor TurretMuzzle =
            EffectAnchor.AtTipOf(TurretNode, Vector3.up);

        /// <summary>
        /// What each unit type is drawn as, and how big — one entry per row of
        /// <c>content/units.txt</c> that has art. A row that has none yet is on
        /// <see cref="UnboundUnits"/>'s list instead and draws the stand-in.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every one of these was chosen by the developer and none of them
        /// is chosen here.</b> The assignments are signed in
        /// <c>docs/roster.md</c> — the Minion and the Skeleton share the minion
        /// skin, the Warrior takes the warrior, the Scout the rogue, the
        /// Skeleton Mage the mage, and the four towers take the Knight, the
        /// Ranger twice and the Mage. A builder that reached for "the obvious
        /// model" would be making an art decision unattended, which is a
        /// standing prohibition on this project and not a style preference.
        /// </para>
        /// <para>
        /// <b>Size says which side a row is on and nothing else.</b> Towers
        /// draw at 1 and every creep at a half; no rung of a line is drawn
        /// bigger than the rung below it. The numbers are
        /// <see cref="MatchArt"/>'s, so the two tables that carry these rows
        /// cannot disagree about what a half is.
        /// </para>
        /// <para>
        /// <b>A tier is told apart by colour, by a prop or by a second
        /// model, and a prop may be held or may stand beside.</b> The Archer
        /// and the Ranger are the pair that proves the rule: one model, one
        /// scale, and what separates them on sight is the Ranger's own atlas
        /// and the quiver in its hand. The atlas covers the body only — a prop
        /// is its own import off its own pack's atlas, and this quiver is
        /// authored on the rogue's. Six rows here fill the beside column in —
        /// the Blessing's statue, the Consecration's font, the Overgrowth's
        /// weirwood and the turret that stands beside all three rungs of the
        /// Engineer.
        /// </para>
        /// <para>
        /// <b>The Artificer stands beside one thing and his look names two.</b>
        /// <c>docs/roster.md</c> puts an <c>ammo_crate</c> beside the turret at
        /// that rung, and a tower has one beside slot — which that page writes
        /// on the rung's own <c>Needs</c> line as a thing the engine would have
        /// to gain. What the one slot holds is the turret, because the turret
        /// is what the Engineer's shell leaves from at every rung and the
        /// crate would take the anchor's own prop off the board; so the crate
        /// is not drawn and the Artificer is told from the Engineer by colour
        /// alone. Nothing is invented to close that gap here.
        /// </para>
        /// <para>
        /// <b>A rung inherits what the rung below it holds, and its
        /// <c>Looks</c> line in <c>docs/roster.md</c> names only what
        /// changes.</b> So the Sergeant and the Shield Wall carry the
        /// Soldier's sword, the Slam carries the Berserker's axe and the
        /// Blessing keeps the Templar's shield in the hand its book does not
        /// fill. Reading those lines as a complete inventory instead would
        /// strip the weapon off six of the nine rows below.
        /// </para>
        /// <para>
        /// <b>A prop carries up a line and a colour does not.</b> A body has to
        /// be holding something, so a rung that names no weapon keeps the one
        /// below it; a colour is one of the three things that tell a rung
        /// apart, and that page gives tier 3 the second model instead wherever
        /// one exists. So the Slam is drawn in the atlas
        /// <c>Barbarian_Large</c> imports wearing rather than in the
        /// Berserker's, and the Unravel in the Lorekeeper's own — that body has
        /// no alternate atlas anywhere in the collection, and the Sorcerer's
        /// belongs to a different character's UVs.
        /// </para>
        /// <para>
        /// <b>A named prop replaces what was in that hand rather than joining
        /// it.</b> The Blessing's book takes the hand the Templar's hammer was
        /// in and the Bishop's mace takes the Cleric's tome, because a
        /// <c>Looks</c> line names the thing that changed and a hand holds one
        /// thing. The Templar's shield stays because it is in the other hand.
        /// <c>docs/roster.md</c> does not say where the tome goes when the mace
        /// arrives, and moving it to the off hand would be inventing a second
        /// assignment rather than reading one; so the tome is put down, and
        /// whether that is the read is on the sheet as a question.
        /// </para>
        /// <para>
        /// <b>The Paladin and Engineer lines are bound with no clips, and that
        /// is the record speaking rather than an omission.</b>
        /// <c>docs/roster.md</c> names a clip on every rung of the Knight,
        /// Barbarian, Cleric, Druid and Rogue lines and none on any rung of
        /// those two, whose windup and backswing carry the <c>_</c> that page
        /// puts on a number nobody has signed. A clip chosen here to fill the
        /// gap would be this table deciding how a tower swings, so those six
        /// rows stand in their bind pose until the ask is answered.
        /// </para>
        /// <para>
        /// <b>The Overwatch is posed by one clip in all three states, because
        /// one is all that page signs.</b> Its <c>Looks</c> line names a stance
        /// — <c>Ranged_2H_Aiming</c>, the sighted pose — the way the Shield
        /// Wall's names a raised guard, and names no swing at all. A row is
        /// animated only when it carries all three clips, so an idle on its own
        /// would leave the signed stance unreachable and the Marksman standing
        /// in his bind pose; the alternative, carrying the Ranger's bow draw and
        /// release up onto a body holding a crossbow, would be posing this row
        /// with another weapon's action. So the one signed pose is held through
        /// all three states, and what is not signed is a second clip rather than
        /// a way of reaching this one.
        /// </para>
        /// <para>
        /// <b>The clip a <c>Looks</c> line names is the swing, and the rest
        /// clip either side of it is not a per-row choice.</b> That page names
        /// <c>Melee_2H_Attack_Chop</c> for the Barbarian,
        /// <c>Ranged_Magic_Shoot</c> for the Cleric and the Druid,
        /// <c>Throw</c> for the Rogue and
        /// <c>Melee_Dualwield_Attack_Slice</c> for the Fan of Knives in the
        /// same grammar, and those are attacks — so each goes in the windup,
        /// with <see cref="RestClipName"/> either side of it as every posed row
        /// that is not holding a bow has. Where that page means the resting
        /// stance it says so, as the Shield Wall's raised guard and the
        /// Overwatch's sighted aim do.
        /// </para>
        /// <para>
        /// <b>What each unit holds, and the clips it holds it with, are the
        /// same choice and are made in the same row.</b> They were two
        /// project-wide fields until 14 August 2026, keyed on <c>Delivery</c>,
        /// which put the bow on the mage — the one projectile row — and left
        /// the archer and the ranger, both hitscan, holding nothing. The pairs
        /// were signed off by the developer: staff and Spellcasting for the
        /// Mage, bow and the three Ranged_Bow clips for the Archer and Ranger,
        /// sword and 1H_Attack_Chop for the Soldier. Creeps carry scenery and
        /// no clips of their own — nothing in the simulation swings a walker's
        /// weapon.
        /// </para>
        /// <para>
        /// <b>The anchor is where the shot leaves, and it names the weapon
        /// rather than the body.</b> It is what keeps the Mage's spell off the
        /// point in front of its chest that a height above the root would put it
        /// at, and what makes the Archer's arrow leave a bow rather than the
        /// same point on a taller model. A row that walks anchors nowhere:
        /// nothing in the simulation gives a creep a shot to draw.
        /// </para>
        /// </remarks>
        private static readonly (
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
            (string model, float scale, Vector3 offset) beside)[] UnitBindings =
        {
            (1, "Assets/Art/Characters/Skeleton_Minion.fbx", MatchArt.CreepScale, null,
                null, null, null, null, null, default, default, default, default),
            (2, "Assets/Art/Characters/Skeleton_Rogue.fbx", MatchArt.CreepScale, null,
                null, null, null, null, null, default, default, default, default),
            (3, "Assets/Art/Characters/Ranger.fbx", MatchArt.TowerScale, null,
                null, BowPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName, default, BowFlip,
                Bow, default),
            (4, "Assets/Art/Characters/Mage.fbx", MatchArt.TowerScale, null,
                SpellbookPath, null, RestClipName, SpellcastClipName, RestClipName, default, default,
                Spellbook, default),
            (7, "Assets/Art/Characters/Skeleton_Mage.fbx", MatchArt.CreepScale, null,
                SkeletonStaffPath, null, null, null, null, StaffQuarterTurn, default, default, default),
            (11, "Assets/Art/Characters/Knight.fbx", MatchArt.TowerScale, null,
                SwordPath, null, RestClipName, ChopClipName, RestClipName, default, default,
                SwordTip, default),
            (12, "Assets/Art/Characters/Skeleton_Minion.fbx", MatchArt.CreepScale, null,
                SkeletonBladePath, SkeletonShieldAPath, null, null, null, default, default, default,
                default),
            (13, "Assets/Art/Characters/Skeleton_Warrior.fbx", MatchArt.CreepScale, null,
                SkeletonBladePath, SkeletonShieldBPath, null, null, null, default, default, default,
                default),
            (14, "Assets/Art/Characters/Ranger.fbx", MatchArt.TowerScale, RangerAltAtlasPath,
                QuiverPath, BowPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName, default, BowFlip,
                Bow, default),
            (15, "Assets/Art/Characters/Knight.fbx", MatchArt.TowerScale, KnightAltAAtlasPath,
                SwordPath, ShieldSquarePath, RestClipName, ChopClipName, RestClipName, default, default,
                SwordTip, default),
            (16, "Assets/Art/Characters/Knight.fbx", MatchArt.TowerScale, KnightAltBAtlasPath,
                SwordPath, ShieldSquarePath, BlockingClipName, ChopClipName, BlockingClipName,
                default, default, SwordTip, default),
            (17, "Assets/Art/Kaykit/adventurers/Barbarian.fbx", MatchArt.TowerScale, null,
                AxePath, null, RestClipName, TwoHandedChopClipName, RestClipName, default, default,
                AxeHead, default),
            (18, "Assets/Art/Kaykit/adventurers/Barbarian.fbx", MatchArt.TowerScale, BarbarianAltAtlasPath,
                LargeAxePath, null, RestClipName, TwoHandedChopClipName, RestClipName, default, default,
                LargeAxeHead, default),
            (19, "Assets/Art/Kaykit/adventurers/Barbarian_Large.fbx", MatchArt.TowerScale, null,
                LargeAxePath, null, LargeRestClipName, SlamClipName, LargeRestClipName, default, default,
                LargeAxeHead, default),
            (20, PaladinFolder + "Paladin.fbx", MatchArt.TowerScale, null,
                HammerPath, null, null, null, null, default, default,
                HammerHead, default),
            (21, PaladinFolder + "Paladin_with_Helmet.fbx", MatchArt.TowerScale, null,
                HammerPath, PaladinShieldPath, null, null, null, default, default,
                HammerHead, default),
            (22, PaladinFolder + "Paladin_with_Helmet.fbx", MatchArt.TowerScale, PaladinAltAtlasPath,
                BookPath, PaladinShieldPath, null, null, null, default, default,
                Book, (StatuePath, 1f, BesideProp.NextTile)),
            (23, ClericFolder + "Cleric.fbx", MatchArt.TowerScale, null,
                ClericTomePath, null, RestClipName, ShootClipName, RestClipName, default, default,
                ClericTome, default),
            (24, ClericFolder + "Cleric.fbx", MatchArt.TowerScale, ClericAltAtlasPath,
                ClericMacePath, null, RestClipName, ShootClipName, RestClipName, default, default,
                MaceHead, default),
            (25, ClericFolder + "Cleric.fbx", MatchArt.TowerScale, ClericAltAtlasPath,
                ClericMacePath, null, RestClipName, ShootClipName, RestClipName, default, default,
                MaceHead, (ClericFontPath, 1f, BesideProp.NextTile)),
            (26, "Assets/Art/Characters/Mage.fbx", MatchArt.TowerScale, MageAltAtlasPath,
                StaffPath, null, RestClipName, SpellcastClipName, RestClipName, StaffQuarterTurn, default,
                StaffTip, default),
            (27, LorekeeperFolder + "Lorekeeper.fbx", MatchArt.TowerScale, null,
                LorekeeperTomePath, null, RestClipName, SpellcastClipName, RestClipName, default, default,
                LorekeeperTome, default),
            (28, "Assets/Art/Kaykit/adventurers/Druid.fbx", MatchArt.TowerScale, null,
                DruidStaffPath, null, RestClipName, ShootClipName, RestClipName, StaffQuarterTurn, default,
                DruidStaffTip, default),
            (29, "Assets/Art/Kaykit/adventurers/Druid.fbx", MatchArt.TowerScale, DruidAltAAtlasPath,
                DruidStaffPath, null, RestClipName, ShootClipName, RestClipName, StaffQuarterTurn, default,
                DruidStaffTip, default),
            (30, "Assets/Art/Kaykit/adventurers/Druid.fbx", MatchArt.TowerScale, DruidAltBAtlasPath,
                DruidStaffPath, null, RestClipName, ShootClipName, RestClipName, StaffQuarterTurn, default,
                DruidStaffTip, (WeirwoodPath, WeirwoodScale, BesideProp.NextTile)),
            (31, MarksmanPath, MatchArt.TowerScale, null,
                CrossbowPath, null, AimingClipName, AimingClipName, AimingClipName, default, default,
                Crossbow, default),
            (32, RoguePath, MatchArt.TowerScale, null,
                DaggerPath, null, RestClipName, ThrowClipName, RestClipName, default, default,
                Dagger, default),
            (33, HoodedRoguePath, MatchArt.TowerScale, null,
                DaggerPath, null, RestClipName, ThrowClipName, RestClipName, default, default,
                Dagger, default),
            (34, HoodedRoguePath, MatchArt.TowerScale, RogueAltAtlasPath,
                DaggerPath, DaggerPath, RestClipName, DualwieldSliceClipName, RestClipName,
                default, default, Dagger, default),
            (35, EngineerPath, MatchArt.TowerScale, null,
                WrenchPath, null, null, null, null, default, default,
                TurretMuzzle, (TurretPath, 1f, BesideProp.NextTile)),
            (36, EngineerPath, MatchArt.TowerScale, EngineerAltAAtlasPath,
                WrenchPath, null, null, null, null, default, default,
                TurretMuzzle, (TurretPath, 1f, BesideProp.NextTile)),
            (37, EngineerPath, MatchArt.TowerScale, EngineerAltBAtlasPath,
                WrenchPath, null, null, null, null, default, default,
                TurretMuzzle, (TurretPath, 1f, BesideProp.NextTile)),
        };

        /// <summary>
        /// Everything on <c>MatchArt</c> that is not per unit type, as field
        /// name to asset.
        /// </summary>
        /// <remarks>
        /// Only the creep clips are shared now, and they are shared because
        /// every creep does the same two things: it walks and it dies. Both
        /// were chosen by the developer on issue #44, picked from a live
        /// scrubber rather than from filenames. Written down rather than looked
        /// up by convention, because a convention would silently pick a
        /// different clip the day a pack adds one. A missing entry throws by
        /// name.
        /// </remarks>
        private static readonly (string field, string asset, string clip)[] SharedBindings =
        {
            ("creepWalkClip", null, WalkClipName),
            ("creepDeathClip", null, DeathClipName),
        };

        /// <summary>Where the tile atlas material is written.</summary>
        public const string TileMaterialPath = "Assets/Materials/Tiles.mat";

        /// <summary>
        /// Where the dressing settings live. Made once and never rewritten.
        /// </summary>
        /// <remarks>
        /// <b>Created if absent, and left completely alone otherwise.</b> Every
        /// other asset this file touches is regenerated on every run, because
        /// every other asset is derived from something. This one is the thing a
        /// human slides, so regenerating it would throw away the afternoon they
        /// spent finding a density the board reads well at -- and it would do it
        /// silently, on a tool somebody ran for an unrelated reason.
        /// </remarks>
        public const string DressingAssetPath = "Assets/Settings/BoardDressing.asset";

        /// <summary>The texture every tile wears. The pack's own atlas.</summary>
        private const string TileAtlasPath = "Assets/Art/Buildings/hexagons_medieval.png";

        /// <summary>
        /// The serialized field on <c>TileSet</c> for each tile, and the model
        /// it is filled from.
        /// </summary>
        /// <remarks>
        /// <b>Six models and nine pieces left behind.</b> KayKit's road set has
        /// thirteen pieces, A to M, and nine of them are junctions of three
        /// edges or more. The corridor assertion in <c>HexMap</c> gives every
        /// corridor cell one or two corridor neighbours, so a junction can never
        /// be selected and importing one would be shipping art nothing draws and
        /// nothing checks. What the letters mean was read off the meshes rather
        /// than off the pack's user guide; see issue #224.
        /// </remarks>
        private static readonly (TilePiece piece, string field, string asset)[] TileBindings =
        {
            (TilePiece.Ground, "ground", "Assets/Art/Tiles/hex_grass.fbx"),
            (TilePiece.Straight, "straight", "Assets/Art/Tiles/hex_road_A.fbx"),
            (TilePiece.Curve, "curve", "Assets/Art/Tiles/hex_road_B.fbx"),
            (TilePiece.Hairpin, "hairpin", "Assets/Art/Tiles/hex_road_C.fbx"),
            (TilePiece.DeadEnd, "deadEnd", "Assets/Art/Tiles/hex_road_M.fbx"),
            (TilePiece.StraightRamp, "straightRamp", "Assets/Art/Tiles/hex_road_A_sloped_high.fbx"),
        };

        /// <summary>
        /// The models behind each scenery group, and the serialized field on
        /// <c>SceneryModels</c> that holds them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Adding a model is an edit to this list and nothing else.</b>
        /// <c>BoardScenery</c> asks for a group and a variant number and never
        /// counts the art, so a rock appended below is in the rotation on the
        /// next scene build with no other change anywhere.
        /// </para>
        /// <para>
        /// <b>Mountains are not in the grove list even though both fill a hex.</b>
        /// A mountain is 1.8 metres tall against a 2-metre tile, so it reads as
        /// terrain rather than as dressing, and the scatter puts it only on the
        /// border where it frames the board instead of standing in front of it.
        /// </para>
        /// <para>
        /// <b>The pack's hills are not here, and that is not an oversight.</b>
        /// Its <c>hill_single</c> models are shells authored to cap a hex that
        /// is already raised; standing one on flat ground draws its inside,
        /// which reads as a crater. Nor are the smallest props — a bucket, a
        /// sack, a stump — which at the distance a whole board is framed from
        /// are a pixel and a half of noise. Both were imported, looked at on a
        /// contact sheet, and dropped again.
        /// </para>
        /// </remarks>
        private static readonly (string field, string[] assets)[] SceneryBindings =
        {
            ("rimProps", new[]
            {
                "rock_single_B", "rock_single_C", "rock_single_D", "rock_single_E",
                "tree_single_A", "tree_single_B",
                "barrel", "crate_A_big", "haybale",
            }),
            ("camp", new[] { "tent", "weaponrack", "target", "wheelbarrow" }),
            ("groves", new[]
            {
                "trees_A_small", "trees_A_medium", "trees_A_large",
                "trees_B_small", "trees_B_medium", "trees_B_large",
            }),
            ("peaks", new[] { "mountain_A", "mountain_B", "mountain_C" }),
            ("clouds", new[] { "cloud_big", "cloud_small" }),
        };

        /// <summary>Where the scenery models are imported.</summary>
        private const string SceneryFolder = "Assets/Art/Scenery/";

        /// <summary>
        /// Fills in the board's scenery models. They wear the tile atlas, being
        /// out of the same pack and mapped to the same texture.
        /// </summary>
        /// <remarks>
        /// Throws on a missing model, unlike the floor's tolerance of an empty
        /// set at runtime: a scene generated against a checkout with half the
        /// scenery imported would silently drop a whole group, and a board with
        /// no mountains looks like a design choice rather than a failed copy.
        /// </remarks>
        private static void WireScenery(SerializedObject serialized)
        {
            SerializedProperty scenery = serialized.FindProperty("scenery");

            foreach ((string field, string[] assets) in SceneryBindings)
            {
                SerializedProperty property = scenery.FindPropertyRelative(field);

                if (property == null)
                {
                    throw new IOException("SceneryModels has no serialized field named " + field + ".");
                }

                property.arraySize = assets.Length;

                for (int index = 0; index < assets.Length; index++)
                {
                    property.GetArrayElementAtIndex(index).objectReferenceValue =
                        LoadMesh(SceneryFolder + assets[index] + ".fbx");
                }
            }

            scenery.FindPropertyRelative("surface").objectReferenceValue = TileMaterial();

            WireCatalogue(scenery.FindPropertyRelative("catalogue"));
        }

        /// <summary>
        /// Fills in the models <c>content/dressing.txt</c> names one by one.
        /// </summary>
        /// <remarks>
        /// <b>Only what the file names, which is why this reads the file.</b>
        /// The collection under <c>Assets/Art/Kaykit</c> is four thousand
        /// models; a scene carrying all of them would load four thousand meshes
        /// to draw the six somebody placed, and every one of them would be a
        /// reference the scene's YAML had to spell out. What a board needs is
        /// the models it stands on, so that is what goes in -- and the bake
        /// rebuilds this straight after writing the file, so the two cannot
        /// drift.
        /// </remarks>
        private static void WireCatalogue(SerializedProperty catalogue)
        {
            if (catalogue == null)
            {
                throw new IOException("SceneryModels has no serialized field named catalogue.");
            }

            SceneryModels.CataloguedModel[] bound =
                SceneryCatalogue.Bind(StreamingContent.ReadDressing().Names());

            catalogue.arraySize = bound.Length;

            for (int index = 0; index < bound.Length; index++)
            {
                SerializedProperty entry = catalogue.GetArrayElementAtIndex(index);

                entry.FindPropertyRelative("name").stringValue = bound[index].Name;
                entry.FindPropertyRelative("mesh").objectReferenceValue = bound[index].Mesh;
                entry.FindPropertyRelative("material").objectReferenceValue = bound[index].Material;
            }
        }

        /// <summary>
        /// The board's scenery, loaded from the project rather than from a
        /// scene. The frame capture's, for the reason <see cref="Tiles"/> is.
        /// </summary>
        public static SceneryModels Scenery() =>
            SceneryModels.Of(
                    Group("rimProps"),
                    Group("camp"),
                    Group("groves"),
                    Group("peaks"),
                    Group("clouds"),
                    TileMaterial())
                .With(SceneryCatalogue.Bind(StreamingContent.ReadDressing().Names()));

        private static Mesh[] Group(string field)
        {
            foreach ((string bound, string[] assets) in SceneryBindings)
            {
                if (bound != field)
                {
                    continue;
                }

                var meshes = new Mesh[assets.Length];

                for (int index = 0; index < assets.Length; index++)
                {
                    meshes[index] = LoadMesh(SceneryFolder + assets[index] + ".fbx");
                }

                return meshes;
            }

            throw new IOException("No scenery group is bound for " + field + ".");
        }

        /// <summary>
        /// Fills in the floor's six tile models and the atlas they wear.
        /// </summary>
        /// <remarks>
        /// Throws on anything missing, for the same reason <see cref="WireArt"/>
        /// does: a null mesh draws nothing at all, and a floor with a hole in it
        /// where the path bends reads as a broken map rather than as a scene
        /// generated against a project missing an import.
        /// </remarks>
        private static void WireTiles(SerializedObject serialized)
        {
            SerializedProperty tiles = serialized.FindProperty("tiles");

            foreach ((TilePiece _, string field, string asset) in TileBindings)
            {
                SerializedProperty property = tiles.FindPropertyRelative(field);

                if (property == null)
                {
                    throw new IOException("TileSet has no serialized field named " + field + ".");
                }

                property.objectReferenceValue = LoadMesh(asset);
            }

            tiles.FindPropertyRelative("surface").objectReferenceValue = TileMaterial();
        }

        /// <summary>
        /// The floor's tiles, loaded from the project rather than from a scene.
        /// </summary>
        /// <remarks>
        /// <b>The same six assets the scene is wired with, from the same list.</b>
        /// An editor tool that assembles its own root — the frame capture — needs
        /// the tiles too, and building a second list for it is how a capture ends
        /// up being a picture of a floor the game does not have.
        /// </remarks>
        public static TileSet Tiles() =>
            TileSet.Of(
                TileMesh(TilePiece.Ground),
                TileMesh(TilePiece.Straight),
                TileMesh(TilePiece.Curve),
                TileMesh(TilePiece.Hairpin),
                TileMesh(TilePiece.DeadEnd),
                TileMesh(TilePiece.StraightRamp),
                TileMaterial());

        /// <summary>
        /// The model one piece is drawn with, on its own.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Tiles"/> because asking for the whole set
        /// writes the atlas material as a side effect, and a test that only
        /// wants to measure a mesh should not dirty an asset to do it.
        /// </remarks>
        public static Mesh TileMesh(TilePiece piece)
        {
            foreach ((TilePiece bound, string _, string asset) in TileBindings)
            {
                if (bound == piece)
                {
                    return LoadMesh(asset);
                }
            }

            throw new IOException("No tile model is bound for " + piece + ".");
        }

        /// <summary>Points the root at the dressing settings, making them if they are not there.</summary>
        private static void WireDressing(SerializedObject serialized)
        {
            SerializedProperty property = serialized.FindProperty("dressing");

            if (property == null)
            {
                throw new IOException("MatchRoot has no serialized field named dressing.");
            }

            property.objectReferenceValue = DressingAsset();
        }

        /// <summary>The dressing settings, made at their defaults on first run.</summary>
        private static BoardDressingAsset DressingAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BoardDressingAsset>(DressingAssetPath);

            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(Path.GetDirectoryName(DressingAssetPath));

            BoardDressingAsset made = ScriptableObject.CreateInstance<BoardDressingAsset>();
            AssetDatabase.CreateAsset(made, DressingAssetPath);

            return AssetDatabase.LoadAssetAtPath<BoardDressingAsset>(DressingAssetPath);
        }

        /// <summary>The committed tile material, written if it is not there yet.</summary>
        private static Material TileMaterial()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture>(TileAtlasPath);

            if (atlas == null)
            {
                throw new IOException("The tile atlas is not imported at " + TileAtlasPath + ".");
            }

            return WriteTextured(TileMaterialPath, "Tiles", atlas);
        }

        /// <summary>
        /// The one mesh inside an imported model, by asset path.
        /// </summary>
        /// <remarks>
        /// The mesh rather than the prefab, because the floor draws tiles with
        /// its own renderer and one shared material; taking the prefab would
        /// bring the importer's own material along and bind the atlas in six
        /// places instead of one.
        /// </remarks>
        private static Mesh LoadMesh(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Mesh mesh)
                {
                    return mesh;
                }
            }

            throw new IOException("No mesh inside " + path + ". Is it imported?");
        }

        /// <summary>Writes the tile material, rewriting in place if it exists.</summary>
        private static Material WriteTextured(string path, string name, Texture atlas)
        {
            EnsureFolder(Path.GetDirectoryName(path));

            Material material = ViewMaterials.Textured(name, atlas);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);

                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            // In place, so the scene's reference survives. Same reasoning as
            // WriteMaterial.
            existing.shader = material.shader;
            existing.CopyPropertiesFromMaterial(material);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(material);

            return existing;
        }

        [MenuItem("Tools/Rebuild the match scene")]
        public static void Rebuild()
        {
            Material road = WriteMaterial(RoadMaterialPath, "Road", SceneFraming.RoadColor);
            Material grass = WriteMaterial(GrassMaterialPath, "Grass", SceneFraming.GrassColor);

            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject(SceneFraming.RootObjectName);
            var matchRoot = root.AddComponent<MatchRoot>();

            var serialized = new SerializedObject(matchRoot);
            serialized.FindProperty("roadMaterial").objectReferenceValue = road;
            serialized.FindProperty("grassMaterial").objectReferenceValue = grass;
            WireArt(serialized);
            WireTiles(serialized);
            WireScenery(serialized);
            WireDressing(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder(Path.GetDirectoryName(ScenePath));

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save the match scene to " + ScenePath + ".");
            }

            // The one scene, at index zero, so a double-clickable build opens on
            // the playfield rather than on nothing.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, enabled: true) };

            AssetDatabase.SaveAssets();

            Debug.Log("MatchSceneBuilder: wrote " + ScenePath + " with one root object.");
        }

        /// <summary>
        /// Fills in every art reference on the root's <c>MatchArt</c>.
        /// </summary>
        /// <remarks>
        /// Throws on anything it cannot find rather than leaving a null. A null
        /// model reaches the drawing code as "nothing appeared" and a null clip
        /// as "the rig stands in its bind pose" — both of which read as a bug in
        /// the animation, and neither of which says that a generated scene was
        /// generated against a project missing an import.
        /// </remarks>
        private static void WireArt(SerializedObject serialized)
        {
            SerializedProperty units = Field(serialized, "units");
            units.arraySize = UnitBindings.Length + UnboundUnits.Rows.Length;

            for (var i = 0; i < UnitBindings.Length; i++)
            {
                var binding = UnitBindings[i];

                WireUnit(
                    units.GetArrayElementAtIndex(i),
                    binding.unitId,
                    binding.model,
                    binding.scale,
                    binding.texture,
                    binding.rightHand,
                    binding.leftHand,
                    binding.idle,
                    binding.windup,
                    binding.backswing,
                    binding.rightTilt,
                    binding.leftTilt,
                    binding.anchor,
                    binding.beside);
            }

            // A row with no art chosen for it yet: the stand-in at the size its
            // role is drawn at, empty hands and no clips. The list is empty at
            // rest, so this loop usually writes nothing.
            for (var i = 0; i < UnboundUnits.Rows.Length; i++)
            {
                var row = UnboundUnits.Rows[i];

                WireUnit(
                    units.GetArrayElementAtIndex(UnitBindings.Length + i),
                    row.UnitId,
                    UnboundUnits.StandInModelPath,
                    row.Scale);
            }

            foreach ((string field, string asset, string clip) in SharedBindings)
            {
                Field(serialized, field).objectReferenceValue =
                    clip == null ? LoadModel(asset) : LoadClip(clip);
            }
        }

        /// <summary>
        /// Writes one entry that stands there and holds nothing — the same
        /// shape as <see cref="UnitArt.Of(int, GameObject, float)"/>.
        /// </summary>
        private static void WireUnit(SerializedProperty entry, int unitId, string model, float scale) =>
            WireUnit(
                entry, unitId, model, scale, null, null, null, null, null, null, default, default, default,
                default);

        /// <summary>
        /// Writes one entry of the serialized unit list.
        /// </summary>
        /// <remarks>
        /// Every field, every time. Growing a serialized array copies the last
        /// element into the new slots, so an entry that set only the fields it
        /// cared about would inherit the previous row's weapons and clips.
        /// </remarks>
        private static void WireUnit(
            SerializedProperty entry,
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
            (string model, float scale, Vector3 offset) beside)
        {
            entry.FindPropertyRelative("unitId").intValue = unitId;
            entry.FindPropertyRelative("model").objectReferenceValue = LoadModel(model);
            entry.FindPropertyRelative("scale").floatValue = scale;
            entry.FindPropertyRelative("texture").objectReferenceValue = MaybeTexture(texture);
            entry.FindPropertyRelative("rightHand").objectReferenceValue = MaybeModel(rightHand);
            entry.FindPropertyRelative("leftHand").objectReferenceValue = MaybeModel(leftHand);
            entry.FindPropertyRelative("idleClip").objectReferenceValue = MaybeClip(idle);
            entry.FindPropertyRelative("windupClip").objectReferenceValue = MaybeClip(windup);
            entry.FindPropertyRelative("backswingClip").objectReferenceValue = MaybeClip(backswing);
            entry.FindPropertyRelative("rightHandTilt").vector3Value = rightTilt;
            entry.FindPropertyRelative("leftHandTilt").vector3Value = leftTilt;

            SerializedProperty anchored = entry.FindPropertyRelative("effectAnchor");

            // The empty string and not null: a serialized string field holds "",
            // and writing null leaves the previous row's anchor name in the slot
            // for exactly the reason this method writes every field.
            anchored.FindPropertyRelative("transformName").stringValue = anchor.TransformName ?? string.Empty;
            anchored.FindPropertyRelative("tip").vector3Value = anchor.Tip;

            SerializedProperty standing = entry.FindPropertyRelative("beside");

            standing.FindPropertyRelative("model").objectReferenceValue = MaybeModel(beside.model);
            standing.FindPropertyRelative("scale").floatValue = beside.scale;
            standing.FindPropertyRelative("offset").vector3Value = beside.offset;
        }

        /// <summary>
        /// One serialized field on the root's <c>MatchArt</c>, or a throw
        /// saying the tables here and the fields over there have drifted apart.
        /// </summary>
        private static SerializedProperty Field(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty("art." + field);

            if (property == null)
            {
                throw new IOException(
                    "MatchArt has no serialized field called '" + field + "'. The binding tables in "
                    + "MatchSceneBuilder and the fields on MatchArt have drifted apart.");
            }

            return property;
        }

        /// <summary>
        /// The same art the scene is wired with, as a bundle in memory.
        /// </summary>
        /// <remarks>
        /// For an editor tool that draws a match without reading the generated
        /// scene — the frame capture, which has to work on a checkout whose
        /// scene has not been rebuilt yet, and which would otherwise be a third
        /// transcription of these paths. The <i>tests</i> deliberately keep
        /// their own list, in <c>Tests.Fixtures.ChosenArt</c>: a fixture that
        /// took its art from this class could not catch this class choosing the
        /// wrong model, because it would be asserting that the choice matched
        /// itself.
        /// </remarks>
        public static MatchArt Art()
        {
            var units = new List<UnitArt>(UnitBindings.Length + UnboundUnits.Rows.Length);

            foreach (var binding in UnitBindings)
            {
                units.Add(UnitArt.Armed(
                    binding.unitId,
                    LoadModel(binding.model),
                    binding.scale,
                    MaybeModel(binding.rightHand),
                    MaybeModel(binding.leftHand),
                    MaybeClip(binding.idle),
                    MaybeClip(binding.windup),
                    MaybeClip(binding.backswing),
                    binding.rightTilt,
                    binding.leftTilt,
                    binding.anchor,
                    MaybeTexture(binding.texture),
                    BesideProp.Standing(
                        MaybeModel(binding.beside.model), binding.beside.scale, binding.beside.offset)));
            }

            units.AddRange(UnboundUnits.StandIns());

            return MatchArt.Of(units, LoadClip(WalkClipName), LoadClip(DeathClipName));
        }

        /// <summary>
        /// The model at a path, or null when the path is null — a unit that
        /// holds nothing in that hand.
        /// </summary>
        /// <remarks>
        /// A null path means "empty hand" and a path that finds nothing means
        /// "the import is missing", which is why this cannot simply return null
        /// on failure. <see cref="LoadModel"/> throws for the second case.
        /// </remarks>
        private static GameObject MaybeModel(string path) => path == null ? null : LoadModel(path);

        /// <summary>The named clip, or null when the name is null — a creep.</summary>
        private static AnimationClip MaybeClip(string clip) => clip == null ? null : LoadClip(clip);

        /// <summary>
        /// The atlas at a path, or null when the path is null — a row drawn in
        /// the one its model imported wearing.
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
                throw new IOException(
                    "Nothing imported at " + path + ". A row naming an atlas it has not got draws in "
                    + "the one its model came with, which is the rung below it wearing the same face.");
            }

            return texture;
        }

        /// <summary>The imported model at a path, or a throw naming it.</summary>
        private static GameObject LoadModel(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
            {
                throw new IOException(
                    "Nothing imported at " + path + ". The match cannot be drawn without it, and the "
                    + "import is selective by design — see issue #44.");
            }

            return model;
        }

        /// <summary>
        /// The clip a binding names, or a throw saying where it looked.
        /// <see cref="ClipBanks"/> is what decides which banks that is, so a
        /// set file and this table cannot disagree about what a name means.
        /// </summary>
        private static AnimationClip LoadClip(string name)
        {
            AnimationClip clip = ClipBanks.Find(name, out string whereItLooked);

            return clip ?? throw new IOException(
                "No clip called '" + ClipBanks.NameIn(name) + "' in " + whereItLooked);
        }

        /// <summary>
        /// Writes one plain material, taking its colour from
        /// <see cref="SceneFraming"/> rather than from an argument at the call
        /// site, so the committed asset and the committed constant cannot
        /// drift. An edit-mode test asserts they have not.
        /// </summary>
        private static Material WriteMaterial(string path, string name, Color color)
        {
            EnsureFolder(Path.GetDirectoryName(path));

            Material material = ViewMaterials.Create(name, color);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);

                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            // Rewriting in place rather than deleting and recreating: the asset
            // GUID is what the scene points at, and a new GUID would silently
            // null the reference in a scene nobody rebuilt.
            existing.shader = material.shader;
            existing.CopyPropertiesFromMaterial(material);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(material);

            return existing;
        }

        /// <summary>Creates a project folder and everything above it.</summary>
        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');

            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');

            if (!string.IsNullOrEmpty(parent) && parent != "Assets")
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
