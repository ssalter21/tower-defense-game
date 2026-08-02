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
