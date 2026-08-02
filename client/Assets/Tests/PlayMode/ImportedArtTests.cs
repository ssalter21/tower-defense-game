using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tests.PlayMode
{
    /// <summary>
    /// What the art import actually produced, asserted rather than assumed.
    ///
    /// The choices themselves belong to the developer and are recorded on issue
    /// #44. What is checked here is everything downstream of the choosing: that
    /// both import paths landed — a skinned animated character and a static
    /// building mesh — that the clip bank arrived as generic transform curves,
    /// and above all that every atlas bound.
    ///
    /// The atlas is the one worth a test. A model whose texture failed to
    /// resolve does not throw, does not warn at runtime and does not fail to
    /// instantiate: it draws flat magenta, which looks like a licence problem, a
    /// pipeline problem and a shader problem, and is none of them. It is the
    /// single most common import failure there is, and it is invisible to every
    /// other test in this project because nothing else looks at a material.
    /// </summary>
    public class ImportedArtTests
    {
        /// <summary>The skinned character half of the pipeline: the projectile tower.</summary>
        public const string RangerPath = "Assets/Art/Characters/Ranger.fbx";

        /// <summary>The weapon, imported separately and hung off a bone at runtime.</summary>
        public const string BowPath = "Assets/Art/Weapons/bow_withString.fbx";

        /// <summary>The static mesh half of the pipeline: the hitscan tower.</summary>
        public const string TowerPath = "Assets/Art/Buildings/building_tower_A_blue.fbx";

        /// <summary>The creeps, already in the repo before this import.</summary>
        public const string WarriorPath = "Assets/Art/Characters/Skeleton_Warrior.fbx";

        /// <summary>The bank the three tower-state clips come out of.</summary>
        public const string RangedBankPath = "Assets/Art/Animations/Rig_Medium_CombatRanged.fbx";

        /// <summary>
        /// Model to atlas. One rig with two atlases is a deliberate exception,
        /// recorded on #44: the Ranger shares <c>Rig_Medium</c> with the
        /// skeletons — so no retargeting is ever needed — but carries its own
        /// texture, because a skeleton tower defending against skeleton creeps is
        /// unreadable. The bow is on the Ranger's atlas, not a third one.
        ///
        /// That sharing is why <c>bow_withString.fbx</c> is imported with
        /// <c>searchTexturesGlobally</c> on: the importer's default texture
        /// search walks the model's own folder and then upwards, so a weapon in
        /// <c>Art/Weapons</c> cannot see an atlas in <c>Art/Characters</c> and
        /// binds nothing at all. Watched: with the default it imported with a
        /// null texture on its one material, which is the flat-magenta failure
        /// this test exists for.
        /// </summary>
        private static readonly (string model, string atlas)[] AtlasBindings =
        {
            (RangerPath, RangerAtlasPath),
            (BowPath, RangerAtlasPath),
            (TowerPath, "Assets/Art/Buildings/hexagons_medieval.png"),
            (WarriorPath, "Assets/Art/Characters/skeleton_texture.png"),
        };

        /// <summary>The atlas shared by the Ranger and the bow it holds.</summary>
        private const string RangerAtlasPath = "Assets/Art/Characters/ranger_texture.png";

        /// <summary>The tower's three states, one clip each. See #44.</summary>
        private static readonly string[] TowerClipNames =
        {
            "Ranged_Bow_Idle",      // Idle
            "Ranged_Bow_Draw",      // Windup
            "Ranged_Bow_Release",   // Backswing
        };

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
        }

#if UNITY_EDITOR
        private GameObject Instantiate(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"nothing imported at {path}");

            GameObject instance = Object.Instantiate(prefab);
            _spawned.Add(instance);

            return instance;
        }

        /// <summary>
        /// The texture a material actually draws with. Checked through both
        /// names because the two live shaders disagree: the universal pipeline's
        /// Lit calls it <c>_BaseMap</c>, the built-in fallback <c>_MainTex</c>.
        /// A test that only knew one of them would report "no atlas" on a model
        /// that is textured perfectly well.
        /// </summary>
        private static Texture MainTextureOf(Material material)
        {
            if (material.HasProperty("_BaseMap"))
            {
                Texture baseMap = material.GetTexture("_BaseMap");
                if (baseMap != null) return baseMap;
            }

            return material.mainTexture;
        }

        [Test]
        public void EverySelectedAssetIsImported()
        {
            foreach (string path in new[] { RangerPath, BowPath, TowerPath, WarriorPath, RangedBankPath })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>(path),
                    $"{path} is not in the project — the import was not selective, it was absent");
            }
        }

        [Test]
        public void TheProjectileTowerIsASkinnedAnimatedCharacter()
        {
            GameObject ranger = Instantiate(RangerPath);

            SkinnedMeshRenderer[] skinned = ranger.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.IsNotEmpty(skinned, "the Ranger imported with no skinned mesh — this is the skinned import path");

            foreach (SkinnedMeshRenderer renderer in skinned)
            {
                Assert.Greater(renderer.bones.Length, 0, $"{renderer.name} is skinned to no bones");
                Assert.IsNotNull(renderer.rootBone, $"{renderer.name} has no root bone");
            }

            // The bone the bow goes on has to be one of them, or the weapon half
            // of this pipeline has nowhere to attach. Looked up by string here
            // rather than through the shipped helper, so this file asserts what
            // the import produced and nothing about how the view uses it.
            Assert.IsNotNull(
                ranger.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "handslot.l"),
                "the Ranger carries no 'handslot.l' bone");
        }

        [Test]
        public void TheHitscanTowerIsAStaticBuildingMesh()
        {
            GameObject tower = Instantiate(TowerPath);

            MeshFilter[] filters = tower.GetComponentsInChildren<MeshFilter>(true);
            Assert.IsNotEmpty(filters, "the tower imported with no mesh — this is the static import path");

            foreach (MeshFilter filter in filters)
            {
                Assert.IsNotNull(filter.sharedMesh, $"{filter.name} has a mesh filter and no mesh");
                Assert.Greater(filter.sharedMesh.vertexCount, 0, $"{filter.name}'s mesh is empty");
            }

            // Deliberately the other path, not a second copy of the first one:
            // a building that arrived skinned would mean the two halves of this
            // ticket are the same half twice.
            Assert.IsEmpty(tower.GetComponentsInChildren<SkinnedMeshRenderer>(true),
                "the building imported skinned — that is the character path, not the static one");
        }

        /// <summary>
        /// Every model draws with the atlas it was authored against, and with
        /// that exact file rather than something of the same name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The check is per model, not per material, and that is deliberate.
        /// Each of these FBXs declares exactly one texture, so resolving it is
        /// all-or-nothing for the whole file: either the importer found the
        /// atlas or nothing in the model is textured. What a per-material rule
        /// would add is a false failure — the skeleton's eyes carry a second
        /// material, <c>Glow</c>, that declares no map at all and draws a flat
        /// colour on purpose. Demanding a texture there is this test insisting
        /// the artist textured something he deliberately did not.
        /// </para>
        /// <para>
        /// Identity, not name. <c>bow_withString.fbx</c> is imported searching
        /// for its texture across the whole project, because it shares the
        /// Ranger's atlas from a different folder — so "a texture called
        /// ranger_texture" is exactly the assertion that a second file of that
        /// name somewhere else would satisfy while dressing the bow wrong.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryImportedAtlasBinds()
        {
            foreach ((string model, string atlas) in AtlasBindings)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(atlas),
                    $"the atlas {atlas} is not in the project");

                GameObject instance = Instantiate(model);
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

                Assert.IsNotEmpty(renderers, $"{model} instantiated with no renderer at all");

                var dressed = new List<string>();

                foreach (Renderer renderer in renderers)
                {
                    Assert.IsNotEmpty(renderer.sharedMaterials, $"{model}/{renderer.name} has no material");

                    foreach (Material material in renderer.sharedMaterials)
                    {
                        Assert.IsNotNull(material,
                            $"{model}/{renderer.name} has a null material slot — that slot draws magenta");

                        Assert.AreNotEqual("Hidden/InternalErrorShader", material.shader.name,
                            $"{model}/{renderer.name} material '{material.name}' is on the error shader — that draws magenta");

                        Texture bound = MainTextureOf(material);

                        if (bound == null) continue;

                        Assert.AreEqual(atlas, AssetDatabase.GetAssetPath(bound),
                            $"{model}/{renderer.name} material '{material.name}' bound '{bound.name}' " +
                            $"from {AssetDatabase.GetAssetPath(bound)}, not the atlas it was authored against");

                        dressed.Add($"{renderer.name}/{material.name}");
                    }
                }

                Assert.IsNotEmpty(dressed,
                    $"{model} bound no texture on any material. Expected {atlas}; " +
                    "a model whose atlas failed to resolve draws flat magenta and throws nothing.");

                Debug.Log($"[atlas] {model} -> {atlas} on {dressed.Count} material(s): {string.Join(", ", dressed)}");
            }
        }

        [Test]
        public void TheThreeTowerStateClipsAreInTheRangedBank()
        {
            // "__preview__" duplicates are editor thumbnail bookkeeping that
            // Unity hangs off any clip it has ever drawn an icon for.
            string[] names = AssetDatabase.LoadAllAssetsAtPath(RangedBankPath)
                .OfType<AnimationClip>()
                .Select(c => c.name)
                .Where(n => !n.StartsWith("__preview__"))
                .ToArray();

            foreach (string wanted in TowerClipNames)
            {
                Assert.Contains(wanted, names,
                    $"'{wanted}' is not in {RangedBankPath}. Found: {string.Join(", ", names)}");
            }
        }
#endif
    }
}
