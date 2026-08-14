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
        /// What each unit type is drawn as, and how big — one entry per row in
        /// <c>content/units.txt</c>.
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
        /// <b>The scale is the tier signal and it is the only one.</b> Towers
        /// draw at 1, every creep at a half, and the Ranger — which shares the
        /// Archer's model and differs from it in one stat — at one and a half.
        /// The numbers are <see cref="MatchArt"/>'s, so the two tables that
        /// carry these rows cannot disagree about what a half is.
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
        /// </remarks>
        private static readonly (
            int unitId,
            string model,
            float scale,
            string rightHand,
            string leftHand,
            string idle,
            string windup,
            string backswing,
            Vector3 rightTilt,
            Vector3 leftTilt)[] UnitBindings =
        {
            (1, "Assets/Art/Characters/Skeleton_Minion.fbx", MatchArt.CreepScale,
                null, null, null, null, null, default, default),
            (2, "Assets/Art/Characters/Skeleton_Rogue.fbx", MatchArt.CreepScale,
                null, null, null, null, null, default, default),
            (3, "Assets/Art/Characters/Ranger.fbx", MatchArt.TowerScale,
                null, BowPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName, default, BowFlip),
            (4, "Assets/Art/Characters/Mage.fbx", MatchArt.TowerScale,
                StaffPath, null, RestClipName, SpellcastClipName, RestClipName, default, default),
            (7, "Assets/Art/Characters/Skeleton_Mage.fbx", MatchArt.CreepScale,
                SkeletonStaffPath, null, null, null, null, default, default),
            (11, "Assets/Art/Characters/Knight.fbx", MatchArt.TowerScale,
                SwordPath, null, RestClipName, ChopClipName, RestClipName, default, default),
            (12, "Assets/Art/Characters/Skeleton_Minion.fbx", MatchArt.CreepScale,
                SkeletonBladePath, SkeletonShieldAPath, null, null, null, default, default),
            (13, "Assets/Art/Characters/Skeleton_Warrior.fbx", MatchArt.CreepScale,
                SkeletonBladePath, SkeletonShieldBPath, null, null, null, default, default),
            (14, "Assets/Art/Characters/Ranger.fbx", MatchArt.RangerScale,
                null, BowPath, BowIdleClipName, BowDrawClipName, BowReleaseClipName, default, BowFlip),
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
            units.arraySize = UnitBindings.Length;

            for (var i = 0; i < UnitBindings.Length; i++)
            {
                var binding = UnitBindings[i];
                SerializedProperty entry = units.GetArrayElementAtIndex(i);

                entry.FindPropertyRelative("unitId").intValue = binding.unitId;
                entry.FindPropertyRelative("model").objectReferenceValue = LoadModel(binding.model);
                entry.FindPropertyRelative("scale").floatValue = binding.scale;
                entry.FindPropertyRelative("rightHand").objectReferenceValue = MaybeModel(binding.rightHand);
                entry.FindPropertyRelative("leftHand").objectReferenceValue = MaybeModel(binding.leftHand);
                entry.FindPropertyRelative("idleClip").objectReferenceValue = MaybeClip(binding.idle);
                entry.FindPropertyRelative("windupClip").objectReferenceValue = MaybeClip(binding.windup);
                entry.FindPropertyRelative("backswingClip").objectReferenceValue =
                    MaybeClip(binding.backswing);
                entry.FindPropertyRelative("rightHandTilt").vector3Value = binding.rightTilt;
                entry.FindPropertyRelative("leftHandTilt").vector3Value = binding.leftTilt;
            }

            foreach ((string field, string asset, string clip) in SharedBindings)
            {
                Field(serialized, field).objectReferenceValue =
                    clip == null ? LoadModel(asset) : LoadClip(clip);
            }
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
            var units = new List<UnitArt>(UnitBindings.Length);

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
                    binding.leftTilt));
            }

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
