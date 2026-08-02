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

        /// <summary>
        /// The models and clips the match is drawn with, as
        /// <c>MatchArt</c> field name to asset.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every one of these was chosen by the developer, on issue #44, and
        /// none of them is chosen here.</b> This table is a transcription of
        /// answers already given — the walk and the death picked from a live
        /// scrubber rather than from filenames, the three bow clips picked to
        /// stand one per simulation state. A builder that reached for "the
        /// obvious clip" would be making an art decision unattended, which is a
        /// standing prohibition on this project and not a style preference.
        /// </para>
        /// <para>
        /// Written down here rather than looked up by convention because a
        /// convention would silently pick a different clip the day a pack adds
        /// one. A missing entry throws by name.
        /// </para>
        /// </remarks>
        private static readonly (string field, string asset, string clip)[] ArtBindings =
        {
            ("creepModel", "Assets/Art/Characters/Skeleton_Warrior.fbx", null),
            ("creepWalkClip", null, "Walking_A"),
            ("creepDeathClip", null, "Death_A"),
            ("projectileTowerModel", "Assets/Art/Characters/Ranger.fbx", null),
            ("bowModel", "Assets/Art/Weapons/bow_withString.fbx", null),
            ("towerIdleClip", null, "Ranged_Bow_Idle"),
            ("towerWindupClip", null, "Ranged_Bow_Draw"),
            ("towerBackswingClip", null, "Ranged_Bow_Release"),
            ("hitscanTowerModel", "Assets/Art/Buildings/building_tower_A_blue.fbx", null),
        };

        /// <summary>
        /// The three clip banks, searched in order for a clip by name.
        /// </summary>
        /// <remarks>
        /// All three share one rig, which is why a clip from any of them drives
        /// any of the characters — verified by measurement rather than trusted,
        /// and the reason this project has one artist rather than two.
        /// </remarks>
        private static readonly string[] ClipBankPaths =
        {
            "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx",
            "Assets/Art/Animations/Rig_Medium_General.fbx",
            "Assets/Art/Animations/Rig_Medium_CombatRanged.fbx",
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
            foreach ((string field, string asset, string clip) in ArtBindings)
            {
                SerializedProperty property = serialized.FindProperty("art." + field);

                if (property == null)
                {
                    throw new IOException(
                        "MatchArt has no serialized field called '" + field + "'. The binding table in "
                        + "MatchSceneBuilder and the fields on MatchArt have drifted apart.");
                }

                property.objectReferenceValue = clip == null ? LoadModel(asset) : LoadClip(clip);
            }
        }

        /// <summary>The imported model at a path, or a throw naming it.</summary>
        private static Object LoadModel(string path)
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
        /// The clip of that name, from whichever of the three banks holds it.
        /// </summary>
        /// <remarks>
        /// <c>__preview__</c> duplicates are editor thumbnail bookkeeping Unity
        /// hangs off any clip it has ever drawn an icon for. Wiring one of those
        /// into a scene would work in the editor and resolve to nothing in a
        /// build, which is the worst of both.
        /// </remarks>
        private static Object LoadClip(string name)
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
                "No clip called '" + name + "' in any of the three banks. Found: "
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
