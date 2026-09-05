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

        /// <summary>The Adventurers pack's second ranger colourway.</summary>
        private const string RangerAltAtlasPath =
            "Assets/Art/Kaykit/adventurers/ranger_texture_alt_A.png";

        private const string WalkClipName = "Walking_A";

        private const string DeathClipName = "Death_A";

        /// <summary>The clip a tower rests in between shots, whatever it holds.</summary>
        private const string RestClipName = "Idle_A";

        private const string BowIdleClipName = "Ranged_Bow_Idle";

        private const string BowDrawClipName = "Ranged_Bow_Draw";

        private const string BowReleaseClipName = "Ranged_Bow_Release";

        private const string SpellcastClipName = "Ranged_Magic_Spellcasting";

        private const string ChopClipName = "Melee_1H_Attack_Chop";

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
        /// which is where the Mage stands. The Necromancer is a creep and the
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

        /// <summary>The orb on the end of the Mage's staff.</summary>
        private static readonly EffectAnchor StaffTip =
            EffectAnchor.AtTipOf(StaffNode, AlongTheShaft);

        /// <summary>The point of the Soldier's sword.</summary>
        private static readonly EffectAnchor SwordTip =
            EffectAnchor.AtTipOf(SwordNode, AlongTheShaft);

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
        /// Necromancer the mage, and the four towers take the Knight, the
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
        /// model, and a prop may be held or may stand beside.</b> The beside
        /// column is empty on every row here: the four looks that need it —
        /// the Engineer's turret, the Paladin's statue, the Cleric's font and
        /// the Druid's weirwood — are signed in <c>docs/roster.md</c> and none
        /// of them has a row in <c>content/units.txt</c> to be authored
        /// against yet. The Archer and the Ranger are the pair that proves the
        /// rule: one model, one scale, and what separates them on sight is the
        /// Ranger's own atlas and the quiver in its hand. The atlas covers the
        /// body only — a prop is its own import off its own pack's atlas, and
        /// this quiver is authored on the rogue's.
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
                StaffPath, null, RestClipName, SpellcastClipName, RestClipName, StaffQuarterTurn, default,
                StaffTip, default),
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

        /// <summary>
        /// The four clip banks, searched in order for a clip by name.
        /// </summary>
        /// <remarks>
        /// All four share one rig, which is why a clip from any of them drives
        /// any of the characters — verified by measurement rather than trusted,
        /// and the reason this project has one artist rather than two. The
        /// melee bank is the newest and arrived with the Soldier's sword: a
        /// tower holding a sword and playing <c>Ranged_Bow_Draw</c> is the same
        /// class of mistake as a mage holding a bow.
        /// </remarks>
        private static readonly string[] ClipBankPaths =
        {
            "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx",
            "Assets/Art/Animations/Rig_Medium_General.fbx",
            "Assets/Art/Animations/Rig_Medium_CombatRanged.fbx",
            "Assets/Art/Animations/Rig_Medium_CombatMelee.fbx",
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
        /// The clip of that name, from whichever of the four banks holds it.
        /// </summary>
        /// <remarks>
        /// <c>__preview__</c> duplicates are editor thumbnail bookkeeping Unity
        /// hangs off any clip it has ever drawn an icon for. Wiring one of those
        /// into a scene would work in the editor and resolve to nothing in a
        /// build, which is the worst of both.
        /// </remarks>
        private static AnimationClip LoadClip(string name)
        {
            var found = new List<string>();

            foreach (string bank in ClipBankPaths)
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(bank))
                {
                    if (!(asset is AnimationClip candidate) || candidate.name.StartsWith("__preview__"))
                    {
                        continue;
                    }

                    if (candidate.name == name)
                    {
                        return candidate;
                    }

                    found.Add(candidate.name);
                }
            }

            throw new IOException(
                "No clip called '" + name + "' in any of the four banks. Found: "
                + string.Join(", ", found));
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
