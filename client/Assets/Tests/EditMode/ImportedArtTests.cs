using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using View;

namespace Tests.EditMode
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
    ///
    /// <b>Edit mode, because every question here is a question for the
    /// importer.</b> These sat in the play-mode suite behind
    /// <c>#if UNITY_EDITOR</c>, which is to say they were compiled out of every
    /// build that was not an editor, leaving a class that yielded no tests at
    /// all. An assertion about <see cref="AssetImporter"/> settings cannot be
    /// made anywhere but an editor, so it belongs in the suite that is honestly
    /// editor-only rather than in the one that was pretending not to be.
    /// </summary>
    public class ImportedArtTests
    {
        /// <summary>The skinned character every other adventurer import is read against.</summary>
        public const string RangerPath = ChosenArt.RangerModelPath;

        /// <summary>The weapon, imported separately and hung off a bone at runtime.</summary>
        public const string BowPath = ChosenArt.BowModelPath;

        /// <summary>
        /// The static mesh half of the pipeline.
        /// </summary>
        /// <remarks>
        /// No unit is drawn with it — every row in <c>content/units.txt</c> is a
        /// character now — but the building is still in the repository and the
        /// non-skinned import path is still the half of this pipeline nothing
        /// else exercises. Named here rather than in <see cref="ChosenArt"/>,
        /// which is the list of what a match is actually drawn with.
        /// </remarks>
        public const string TowerPath = "Assets/Art/Buildings/building_tower_A_blue.fbx";

        /// <summary>The bank the three tower-state clips come out of.</summary>
        public const string RangedBankPath = ChosenArt.RangedBankPath;

        /// <summary>The unit id the Ranger's one-and-a-half is written against in the roster.</summary>
        private const int RangerUnitId = 14;

        /// <summary>The atlas shared by the Ranger and the bow it holds.</summary>
        private const string RangerAtlasPath = "Assets/Art/Characters/ranger_texture.png";

        /// <summary>The atlas the Skeletons 1.1 characters were authored against.</summary>
        private const string SkeletonAtlasPath = "Assets/Art/Characters/skeleton_texture_A.png";

        /// <summary>
        /// Model to atlas. The adventurers each carry their own and the
        /// skeletons share one, which is deliberate and recorded on #44: the
        /// Ranger shares <c>Rig_Medium</c> with the skeletons — so no
        /// retargeting is ever needed — but carries its own texture, because a
        /// skeleton tower defending against skeleton creeps is unreadable. The
        /// bow is on the Ranger's atlas, not a third one.
        ///
        /// That sharing is why <c>bow_withString.fbx</c> is imported with
        /// <c>searchTexturesGlobally</c> on: the importer's default texture
        /// search walks the model's own folder and then upwards, so a weapon in
        /// <c>Art/Weapons</c> cannot see an atlas in <c>Art/Characters</c> and
        /// binds nothing at all. Watched: with the default it imported with a
        /// null texture on its one material, which is the flat-magenta failure
        /// this test exists for.
        ///
        /// <b>Two skeleton atlases, and that is two pack versions rather than a
        /// duplicate.</b> <c>Skeleton_Warrior.fbx</c> came in from Skeletons 1.0
        /// and names <c>skeleton_texture</c>; the three imported since are 1.1
        /// and name <c>skeleton_texture_A</c>. A model bound to the wrong one of
        /// the two does not throw — it draws, in the wrong skin — so both are
        /// written down and both are asserted by identity.
        /// </summary>
        private static readonly (string model, string atlas)[] AtlasBindings =
        {
            (RangerPath, RangerAtlasPath),
            (BowPath, RangerAtlasPath),
            (TowerPath, "Assets/Art/Buildings/hexagons_medieval.png"),
            (ChosenArt.WarriorModelPath, "Assets/Art/Characters/skeleton_texture.png"),
            (ChosenArt.MinionModelPath, SkeletonAtlasPath),
            (ChosenArt.RogueModelPath, SkeletonAtlasPath),
            (ChosenArt.SkeletonMageModelPath, SkeletonAtlasPath),
            (ChosenArt.KnightModelPath, "Assets/Art/Characters/knight_texture.png"),
            (ChosenArt.MageModelPath, "Assets/Art/Characters/mage_texture.png"),
        };

        /// <summary>The tower's three states, one clip each. See #44.</summary>
        private static readonly string[] TowerClipNames =
        {
            ChosenArt.TowerIdleClipName,       // Idle
            ChosenArt.TowerWindupClipName,     // Windup
            ChosenArt.TowerBackswingClipName,  // Backswing
        };

        /// <summary>The clip banks: the FBXs imported for their curves, not their meshes.</summary>
        private static readonly string[] ClipBankPaths =
        {
            ChosenArt.MovementBankPath,
            ChosenArt.GeneralBankPath,
            ChosenArt.RangedBankPath,
        };

        /// <summary>
        /// Every FBX in this project that carries a rig or clips: every model a
        /// unit is drawn with, plus the three banks. Walked rather than listed,
        /// so a model added to the roster is covered by being assigned rather
        /// than by somebody remembering to add it here.
        /// </summary>
        private static IEnumerable<string> RiggedPaths => ChosenArt.ModelPaths.Concat(ClipBankPaths);

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
            foreach (string path in RiggedPaths.Concat(new[] { BowPath, TowerPath }))
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>(path),
                    $"{path} is not in the project — the import was not selective, it was absent");
            }
        }

        /// <summary>
        /// Every unit the simulation can put on the board has a model and a
        /// size, and the sizes are the ones <c>docs/roster.md</c> signed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Walked from the shipped unit table rather than from the art.</b>
        /// The failure this catches is a row with no entry, and a test that
        /// iterated the art would find every entry it had and never notice the
        /// one it did not — the Necromancer arriving on a menu and drawing
        /// nothing at all.
        /// </para>
        /// <para>
        /// The scales are checked against the role rather than written out per
        /// unit, because "towers 1, every creep a half" is the rule and the
        /// Ranger is its one stated exception.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryUnitTypeIsDrawnAtItsRosterScale()
        {
            MatchArt art = ChosenArt.Load();

            foreach (UnitType type in StreamingContent.ReadUnitTypes().Types)
            {
                Assert.That(art.ModelFor(type.Id), Is.Not.Null,
                    $"unit {type.Id} ({type.Label}) has no model");

                float expected =
                    type.Role == UnitRole.Moving ? MatchArt.CreepScale
                    : type.Id == RangerUnitId ? MatchArt.RangerScale
                    : MatchArt.TowerScale;

                Assert.That(art.ScaleFor(type.Id), Is.EqualTo(expected),
                    $"unit {type.Id} ({type.Label}) is drawn at the wrong size for its role");
            }
        }

        /// <summary>
        /// A creep stands lower than a tower, measured off the geometry rather
        /// than off the multipliers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Comparing the two scale numbers would prove nothing.</b> A half
        /// applied to a taller model is not smaller than a one applied to a
        /// shorter one, and the models come from two different packs. So each
        /// is instantiated and its renderers' world bounds measured, which is
        /// what a player's eye is doing.
        /// </para>
        /// <para>
        /// The margin is a fifth rather than a hair, because the claim being
        /// held is "unmistakably smaller" and a creep that measured one percent
        /// shorter would satisfy a strict inequality while reading as the same
        /// size.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryCreepStandsUnmistakablyLowerThanEveryTower()
        {
            MatchArt art = ChosenArt.Load();
            IReadOnlyList<UnitType> types = StreamingContent.ReadUnitTypes().Types;

            float shortestTower = float.MaxValue;
            float tallestCreep = 0f;
            string shortest = null;
            string tallest = null;

            foreach (UnitType type in types)
            {
                float height = DrawnHeightOf(art, type.Id);

                if (type.Role == UnitRole.Moving && height > tallestCreep)
                {
                    (tallestCreep, tallest) = (height, type.Label);
                }

                if (type.Role == UnitRole.Placed && height < shortestTower)
                {
                    (shortestTower, shortest) = (height, type.Label);
                }

                Debug.Log($"[scale] {type.Label} draws {height:F2} m tall");
            }

            Assert.That(tallestCreep, Is.LessThan(shortestTower * 0.8f),
                $"the tallest creep ({tallest}, {tallestCreep:F2} m) is not unmistakably shorter than "
                + $"the shortest tower ({shortest}, {shortestTower:F2} m)");
        }

        /// <summary>
        /// How tall one unit is drawn: the world bounds of every renderer on its
        /// instantiated model, times the scale the view will apply.
        /// </summary>
        private float DrawnHeightOf(MatchArt art, int unitId)
        {
            GameObject instance = Instantiate(AssetDatabase.GetAssetPath(art.ModelFor(unitId)));
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

            Assert.IsNotEmpty(renderers, $"unit {unitId}'s model has no renderer to measure");

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds.size.y * art.ScaleFor(unitId);
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

        /// <summary>
        /// No clip owns any translation of its own.
        /// </summary>
        /// <remarks>
        /// Locomotion phase is driven from distance travelled in the simulation,
        /// so a clip carrying root motion would be authoritative progress living
        /// in the view — the exact thing the architecture forbids.
        /// </remarks>
        [Test]
        public void RealClipsCarryNoRootMotion()
        {
            foreach (var path in ClipBankPaths)
            {
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    Assert.IsFalse(clip.hasRootCurves, $"{clip.name} carries root curves");
                    Assert.IsFalse(clip.hasMotionCurves, $"{clip.name} carries motion curves");
                    Assert.IsFalse(clip.hasGenericRootTransform, $"{clip.name} carries a generic root transform");
                }
            }
        }

        /// <summary>
        /// Every rig arrived Generic, with no avatar.
        /// </summary>
        /// <remarks>
        /// The proven path is generic transform curves: the clip animates named
        /// transforms in this hierarchy directly. Humanoid would put a
        /// retargeting solver between the clip and the bones — one more thing
        /// between sim time and the pose, on a rig that never needed retargeting
        /// in the first place.
        /// </remarks>
        [Test]
        public void TheRigIsImportedGenericWithNoAvatar()
        {
            foreach (var path in RiggedPaths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.IsNotNull(importer, $"no model importer for {path}");
                Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType,
                    $"{path} is not imported as Generic");
                Assert.AreEqual(ModelImporterAvatarSetup.NoAvatar, importer.avatarSetup,
                    $"{path} was given an avatar");
            }
        }
    }
}
