using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace View.Editor
{
    /// <summary>
    /// The imported art, addressed by name: what <c>content/dressing.txt</c>'s
    /// <c>model</c> lines mean, and where the palette gets its list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A name is a path, because a path is already unique and a stem is
    /// not.</b> Four packs ship a <c>tent</c> and three ship a
    /// <c>wheelbarrow</c>; keyed on the file stem the catalogue would bind
    /// whichever import happened to be walked last, and a board would change
    /// scenery when somebody added a pack. The name is therefore the path under
    /// <see cref="Root"/> with the extension dropped -- <c>city-builder/wall_corner</c>
    /// -- which is typeable, greppable, sorts by pack, and cannot collide while
    /// two files cannot share a path.
    /// </para>
    /// <para>
    /// <b>The material is read off the model, not off a table.</b> Each pack
    /// ships its atlas inside its own <c>fbx(unity)</c> folder and each FBX
    /// names the texture it wants, so Unity's own importer has already resolved
    /// the question this class would otherwise be guessing at. A hand-written
    /// map of pack to atlas would be twenty-two rows that nothing checks, and it
    /// would be wrong for every pack shipping more than one texture -- Board
    /// Game Bits ships fifty-four.
    /// </para>
    /// <para>
    /// <b>Editor-only, and that is not an oversight.</b> Four thousand models
    /// are a thing to choose from, not a thing to ship: what reaches the scene
    /// is the handful the dressing file actually names, resolved by
    /// <see cref="Bind"/> at bake time. Nothing at runtime may ask this class a
    /// question, because at runtime there is no asset database to answer it.
    /// </para>
    /// </remarks>
    public static class SceneryCatalogue
    {
        /// <summary>Where the collection is imported.</summary>
        public const string Root = "Assets/Art/Kaykit";

        /// <summary>Where the per-atlas materials are written.</summary>
        public const string MaterialFolder = "Assets/Materials/Kaykit";

        /// <summary>
        /// Every model in the collection, by catalogue name, sorted.
        /// </summary>
        /// <remarks>
        /// Sorted because it is read by a person scrolling a list, and an
        /// unsorted four thousand is a list nobody can find anything in. The
        /// sort is ordinal so it does not move with a machine's locale.
        /// </remarks>
        public static string[] Names()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                return Array.Empty<string>();
            }

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { Root });
            var names = new List<string>(guids.Length);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(NameOf(path));
                }
            }

            names.Sort(StringComparer.Ordinal);

            return names.ToArray();
        }

        /// <summary>The catalogue name of an imported asset path.</summary>
        public static string NameOf(string assetPath)
        {
            string relative = assetPath.Substring(Root.Length + 1);

            return relative.Substring(0, relative.Length - ".fbx".Length).Replace('\\', '/');
        }

        /// <summary>The asset path a catalogue name refers to.</summary>
        public static string PathOf(string name) => Root + "/" + name + ".fbx";

        /// <summary>
        /// Resolves names to the meshes and materials that draw them. Names
        /// nothing is imported for are reported and left out, so a dressing file
        /// written against a fuller checkout still binds everything it can.
        /// </summary>
        public static SceneryModels.CataloguedModel[] Bind(IEnumerable<string> names)
        {
            var bound = new List<SceneryModels.CataloguedModel>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var materials = new Dictionary<string, Material>(StringComparer.Ordinal);

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name) || !seen.Add(name))
                {
                    continue;
                }

                string path = PathOf(name);
                Mesh mesh = MeshIn(path);

                if (mesh == null)
                {
                    Debug.LogWarning(
                        "content/dressing.txt names " + name + ", which is not imported at "
                        + path + ". It will draw nothing until it is.");

                    continue;
                }

                bound.Add(new SceneryModels.CataloguedModel(name, mesh, MaterialFor(path, materials)));
            }

            return bound.ToArray();
        }

        private static Mesh MeshIn(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Mesh mesh)
                {
                    return mesh;
                }
            }

            return null;
        }

        /// <summary>
        /// The material for a model: this project's own shader wearing whatever
        /// atlas the model's importer resolved, written once per atlas.
        /// </summary>
        /// <remarks>
        /// <b>Not the FBX's own embedded material.</b> That one is a sub-asset
        /// of the model, built by the importer against whatever the pack's
        /// exporter wrote, and it is read-only, unshared and on a shader this
        /// project does not otherwise use. Rebuilding it here means every piece
        /// of scenery on the board is on one shader with one set of properties,
        /// which is the rule <see cref="ViewMaterials"/> exists to hold, and it
        /// means two models out of the same pack share one material rather than
        /// breaking batching apart per mesh.
        /// </remarks>
        private static Material MaterialFor(string path, Dictionary<string, Material> made)
        {
            Texture atlas = AtlasFor(path);

            if (atlas == null)
            {
                return null;
            }

            string atlasPath = AssetDatabase.GetAssetPath(atlas);

            if (made.TryGetValue(atlasPath, out Material already))
            {
                return already;
            }

            string materialPath =
                MaterialFolder + "/" + Path.GetFileNameWithoutExtension(atlasPath) + ".mat";

            Material written = Write(materialPath, atlas);
            made[atlasPath] = written;

            return written;
        }

        /// <summary>
        /// The texture a model wants: what the importer bound to its own
        /// material, falling back to the one atlas sitting beside it.
        /// </summary>
        /// <remarks>
        /// The fallback is for a pack whose FBX names no material at all. It
        /// insists on there being exactly one texture in the folder, because a
        /// folder with two is a folder where picking one is a guess -- and a
        /// guessed atlas draws a model in somebody else's colours, which reads
        /// as a broken import rather than as a wrong choice.
        /// </remarks>
        private static Texture AtlasFor(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Material material && material.mainTexture != null)
                {
                    return material.mainTexture;
                }
            }

            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            string[] textures = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            Texture only = null;

            foreach (string guid in textures)
            {
                string found = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetDirectoryName(found).Replace('\\', '/') != folder)
                {
                    continue;
                }

                if (only != null)
                {
                    return null;
                }

                only = AssetDatabase.LoadAssetAtPath<Texture>(found);
            }

            return only;
        }

        private static Material Write(string path, Texture atlas)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));

            Material material =
                ViewMaterials.Textured(Path.GetFileNameWithoutExtension(path), atlas);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);

                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            // In place, so anything already referencing it keeps its reference.
            // The same reasoning as the scene builder's own material writing.
            existing.shader = material.shader;
            existing.CopyPropertiesFromMaterial(material);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(material);

            return existing;
        }

        private static void EnsureFolder(string folder)
        {
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
