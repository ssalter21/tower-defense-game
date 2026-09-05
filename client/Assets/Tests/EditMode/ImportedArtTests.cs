using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using View;
using View.Editor;

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
        /// Everything a unit holds. Each is its own import, hung off a bone at
        /// runtime rather than baked into the body, which is why the body and
        /// the thing it carries can be assigned separately per unit.
        /// </summary>
        public static readonly string[] HeldPaths =
        {
            ChosenArt.BowModelPath,
            ChosenArt.StaffModelPath,
            ChosenArt.SwordModelPath,
            ChosenArt.SkeletonStaffModelPath,
            ChosenArt.SkeletonBladeModelPath,
            ChosenArt.SkeletonShieldAModelPath,
            ChosenArt.SkeletonShieldBModelPath,
        };

        /// <summary>
        /// The static mesh half of the pipeline.
        /// </summary>
        /// <remarks>
        /// No unit is drawn with it: every row in <c>content/units.txt</c> is a
        /// character. It stays in the project because the non-skinned import
        /// path is the half of this pipeline nothing else exercises. Named here
        /// rather than in <see cref="ChosenArt"/>, which is the list of what a
        /// match is actually drawn with.
        /// </remarks>
        public const string TowerPath = "Assets/Art/Buildings/building_tower_A_blue.fbx";

        /// <summary>The bank the three tower-state clips come out of.</summary>
        public const string RangedBankPath = ChosenArt.RangedBankPath;

        /// <summary>The tier-1 Archer.</summary>
        private const int ArcherUnitId = 3;

        /// <summary>The tier-2 Ranger, which stands on the Archer's model.</summary>
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
            (ChosenArt.StaffModelPath, "Assets/Art/Characters/mage_texture.png"),
            (ChosenArt.SwordModelPath, "Assets/Art/Characters/knight_texture.png"),
            (ChosenArt.SkeletonStaffModelPath, SkeletonAtlasPath),
            (ChosenArt.SkeletonBladeModelPath, SkeletonAtlasPath),
            (ChosenArt.SkeletonShieldAModelPath, SkeletonAtlasPath),
            (ChosenArt.SkeletonShieldBModelPath, SkeletonAtlasPath),
            (TowerPath, "Assets/Art/Buildings/hexagons_medieval.png"),
            (ChosenArt.WarriorModelPath, "Assets/Art/Characters/skeleton_texture.png"),
            (ChosenArt.MinionModelPath, SkeletonAtlasPath),
            (ChosenArt.RogueModelPath, SkeletonAtlasPath),
            (ChosenArt.SkeletonMageModelPath, SkeletonAtlasPath),
            (ChosenArt.KnightModelPath, "Assets/Art/Characters/knight_texture.png"),
            (ChosenArt.MageModelPath, "Assets/Art/Characters/mage_texture.png"),

            // The stand-in a row with no art yet draws as. It comes from a
            // different pack and wears that pack's own atlas, which is the
            // failure this table exists for: a model drawn against the wrong
            // atlas draws confetti, and one drawn against none draws magenta.
            (UnboundUnits.StandInModelPath, "Assets/Art/Kaykit/prototype/prototypebits_texture.png"),
        };

        /// <summary>
        /// Every clip a tower is posed with. Three states each, and the set a
        /// tower gets depends on what it holds — the bow three for the Archer
        /// and the Ranger, rest-and-cast for the Mage, rest-and-chop for the
        /// Soldier. See #44 and the 14 August weapon pass.
        /// </summary>
        private static readonly string[] TowerClipNames =
        {
            ChosenArt.BowIdleClipName,
            ChosenArt.BowDrawClipName,
            ChosenArt.BowReleaseClipName,
            ChosenArt.RestClipName,
            ChosenArt.SpellcastClipName,
            ChosenArt.ChopClipName,
        };

        /// <summary>The clip banks: the FBXs imported for their curves, not their meshes.</summary>
        private static readonly string[] ClipBankPaths =
        {
            ChosenArt.MovementBankPath,
            ChosenArt.GeneralBankPath,
            ChosenArt.RangedBankPath,
            ChosenArt.MeleeBankPath,
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
            foreach (string path in RiggedPaths.Concat(HeldPaths).Concat(new[] { TowerPath }))
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
        /// <b>Two multipliers and no exceptions.</b> "Towers 1, every creep a
        /// half" is the whole rule: size says which side a row is on and never
        /// which rung of a line it is. The role is read off the shipped table
        /// rather than written out per unit, which makes this a third
        /// transcription of the roster after the scene builder's and the
        /// fixture's, and deliberately so: an assertion that read either table
        /// would be checking it against itself.
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

                float expected = type.Role == UnitRole.Moving
                    ? MatchArt.CreepScale
                    : MatchArt.TowerScale;

                Assert.That(art.ScaleFor(type.Id), Is.EqualTo(expected),
                    $"unit {type.Id} ({type.Label}) is drawn at the wrong size for its role");
            }
        }

        /// <summary>
        /// Every atlas a row names is imported, and imported as a texture.
        /// </summary>
        /// <remarks>
        /// A row naming an atlas that is not there is the flat-magenta failure
        /// this class exists for, one row further along: the material is built
        /// on a null map and the body draws in the base colour alone.
        /// </remarks>
        [Test]
        public void EveryAtlasARowNamesIsImported()
        {
            foreach (string path in ChosenArt.TexturePaths)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(path), Is.Not.Null,
                    $"{path} is not in the project — a row names an atlas nothing imported");
            }
        }

        /// <summary>
        /// The two rows that share a model are told apart by something other
        /// than size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nothing else in this project holds the two rungs apart.</b> The
        /// Archer and the Ranger are one model at one scale, so a build that
        /// gave the Ranger no colour, no prop and no second body would ship two
        /// rungs a player cannot tell apart — and every other test here would
        /// stay green over it.
        /// </para>
        /// <para>
        /// <b>Both halves, not either.</b> <c>docs/roster.md</c> signs this
        /// rung as a colour <i>and</i> a prop, and an assertion satisfied by
        /// whichever of the two happened to survive would let the other go
        /// back to null with every runner green. Which atlas and which prop
        /// stay unnamed here: those are the developer's to move, and naming
        /// them would make this test the place the art is decided.
        /// </para>
        /// </remarks>
        [Test]
        public void TheTwoRowsOnOneModelAreToldApartWithoutSize()
        {
            MatchArt art = ChosenArt.Load();

            UnitArt archer = art.ArtFor(ArcherUnitId);
            UnitArt ranger = art.ArtFor(RangerUnitId);

            Assert.That(ranger.Model, Is.SameAs(archer.Model),
                "these are the two rows that share a model; if they no longer do, this test is "
                + "asserting nothing and the roster has moved under it");

            Assert.That(ranger.Scale, Is.EqualTo(archer.Scale),
                "size is not a tier signal, so the two rungs of the Archer line draw at one scale");

            Assert.That(ranger.Texture, Is.Not.Null,
                "the Ranger shares the Archer's model and its size, so its own atlas is the colour "
                + "half of what tells the two rungs apart — see docs/roster.md");

            Assert.That(ranger.Texture, Is.Not.SameAs(archer.Texture),
                "the Ranger draws in the atlas the Archer draws in, so the colour separates nothing");

            Assert.That(
                ranger.RightHand != archer.RightHand || ranger.LeftHand != archer.LeftHand,
                Is.True,
                "the Ranger holds exactly what the Archer holds, so the prop separates nothing");
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

        /// <summary>
        /// Everything a unit holds is on the bone it was assigned to, and is
        /// big enough to see.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>What this does not prove, and why.</b> The first version of this
        /// asserted that a held item's bounding box reaches outside the body's,
        /// on the strength of <c>WeaponSocket</c>'s note that a bow on the wrong
        /// bone "sits 100% inside the Ranger's own". Measured here it does not
        /// hold even for the bow on the right bone: a character's imported
        /// bounds are its bind pose, arms out, and that box swallows anything in
        /// either hand. Containment is not a signal at this scale, so the
        /// assertion was dropped rather than loosened until it passed.
        /// </para>
        /// <para>
        /// <b>Size is asserted, and it caught the bug this test was written
        /// for.</b> The bow imports with a root scale of 100 and every other
        /// weapon with 1, and <c>WeaponSocket.Attach</c> used to force a scale
        /// of one — so the bow drew two centimetres across, in a hand, from the
        /// day it was added. Nothing threw and no test failed; it simply looked
        /// like an archer holding nothing, and nobody had opened the editor.
        /// The margin is wide because the two cases are three orders of
        /// magnitude apart: a correctly sized weapon measures around half the
        /// body it is held by, and the broken bow measured under one hundredth.
        /// </para>
        /// <para>
        /// It also proves the other silent failure: an item parented to the
        /// rig's root instead of to a hand, which draws as a weapon lying
        /// through the middle of the body. Which hand looks <i>right</i> is
        /// still an eye check, and this project makes those by opening the
        /// editor.
        /// </para>
        /// </remarks>
        [Test]
        public void EverythingHeldIsOnItsBoneAndBigEnoughToSee()
        {
            MatchArt art = ChosenArt.Load();

            var measured = 0;

            foreach (UnitArt unit in art.Units)
            {
                measured += MeasureHeld(unit, unit.RightHand, WeaponSocket.MeleeHand);
                measured += MeasureHeld(unit, unit.LeftHand, WeaponSocket.OffHand);
            }

            Assert.That(measured, Is.GreaterThan(0),
                "no unit holds anything, so this measured nothing at all");
        }

        /// <summary>
        /// Every tower's shots leave a point on its own art, and that point is
        /// on the model or on what the model is holding.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Walked from the shipped unit table rather than from the art</b>,
        /// for the reason the scale test gives: a walk of the art finds every
        /// anchor there is and never the row that has none. A placed row is a
        /// row that shoots, so a placed row with no anchor fires from a fixed
        /// height above its own root — which is the thing anchors replaced, and
        /// which no other assertion here would notice.
        /// </para>
        /// <para>
        /// Built through the real <see cref="TowerView"/>, so what is asserted
        /// is the resolution the game performs and not a second copy of it.
        /// <c>BuildStatic</c> rather than <c>BuildAnimated</c> because the
        /// anchor is found before the animator is bound and a Playables graph
        /// in edit mode would be a second thing that could fail here.
        /// </para>
        /// <para>
        /// The measurements are logged because "leaving the staff tip" is an eye
        /// check in the end, and the numbers are what tell a reader of a green
        /// run whether the tip came out at the orb or at the butt.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryTowerFiresFromAPointOnItsOwnArt()
        {
            MatchArt art = ChosenArt.Load();

            var measured = 0;

            foreach (UnitType type in StreamingContent.ReadUnitTypes().Types)
            {
                if (type.Role != UnitRole.Placed)
                {
                    // A row that walks has no shot to draw, so nothing ever
                    // resolves its anchor and a misspelt one on it would sit in
                    // two generated files failing nowhere. Held both ways, since
                    // an anchor that cannot be reached is the one kind this
                    // cannot make fail by name.
                    Assert.That(art.ArtFor(type.Id).EffectAnchor.IsSet, Is.False,
                        $"unit {type.Id} ({type.Label}) walks, and an effect anchor on a walking row is "
                        + "read by nothing — no creep fires, so it would never resolve and never fail");

                    continue;
                }

                UnitArt unit = art.ArtFor(type.Id);

                Assert.That(unit.EffectAnchor.IsSet, Is.True,
                    $"unit {type.Id} ({type.Label}) stands on the board and shoots, and its art names "
                    + "nowhere for the shot to leave from — so it fires from a height above its own root, "
                    + "whatever it is holding");

                TowerView tower = BuiltTower(type, unit);
                Transform anchor = tower.AnchorTransform;

                Assert.That(anchor, Is.Not.Null,
                    $"unit {type.Id} ({type.Label}) has an anchor that resolved to nothing");

                Assert.That(anchor.IsChildOf(tower.Model.transform), Is.True,
                    $"unit {type.Id} ({type.Label}) anchors on {anchor.name}, which is not part of its "
                    + "own model — an effect anchor is a point on the art, not on the scene");

                Vector3 fromRoot = tower.Muzzle - tower.transform.position;
                float alongTheProp = Vector3.Distance(tower.Muzzle, anchor.position);

                Debug.Log(
                    $"[anchor] unit {type.Id} ({type.Label}) fires from "
                    + $"{unit.EffectAnchor.TransformName}, {alongTheProp:F2} m along it, "
                    + $"{fromRoot.y:F2} m above its base and {fromRoot.magnitude:F2} m from it");

                Assert.That(fromRoot.y, Is.GreaterThan(0f),
                    $"unit {type.Id} ({type.Label}) fires from below its own feet");

                if (unit.EffectAnchor.Tip != Vector3.zero)
                {
                    Assert.That(alongTheProp, Is.GreaterThan(0.05f),
                        $"unit {type.Id} ({type.Label}) asks for the far end of "
                        + $"{unit.EffectAnchor.TransformName} and got a point on top of its origin, so "
                        + "either the prop has no geometry or the tip is being thrown away");
                }

                measured++;
            }

            Assert.That(measured, Is.GreaterThan(0),
                "no row in the shipped table stands still, so this measured nothing at all");
        }

        /// <summary>
        /// An anchor naming something the art does not carry stops the view
        /// being built, and says which name.
        /// </summary>
        /// <remarks>
        /// The alternative is what every silent fallback here would produce: the
        /// flash and the tracer come out of the model's own origin, which is on
        /// the floor between the tower's feet, and reads as a bad effect rather
        /// than as a misspelt string. Same reasoning as
        /// <see cref="WeaponSocket"/>'s refusal, and the same failure it is
        /// guarding against — a name that agrees with nothing.
        /// </remarks>
        [Test]
        public void AnAnchorNamingSomethingTheArtDoesNotCarryFailsByName()
        {
            UnitArt real = ChosenArt.Load().ArtFor(RangerUnitId);

            UnitArt misspelt = UnitArt.Armed(
                real.UnitId, real.Model, real.Scale, null, null, null, null, null,
                default, default, EffectAnchor.At("handslot.left"));

            var host = new GameObject("misspelt-anchor");
            _spawned.Add(host);

            var tower = host.AddComponent<TowerView>();

            var refused = Assert.Throws<System.InvalidOperationException>(
                () => tower.BuildStatic(
                    real.UnitId, TypeOf(RangerUnitId), misspelt, Quaternion.identity));

            Assert.That(refused.Message, Does.Contain("handslot.left"),
                "the refusal has to name the anchor that was not found, or it sends the reader looking "
                + "at the art instead of at the string");
        }

        /// <summary>One tower built the way the game builds it, unposed.</summary>
        private TowerView BuiltTower(UnitType type, UnitArt art)
        {
            var host = new GameObject("tower-" + type.Id);
            _spawned.Add(host);

            var view = host.AddComponent<TowerView>();
            view.BuildStatic(type.Id, type, art, Quaternion.identity);

            return view;
        }

        /// <summary>The shipped row for an id.</summary>
        private static UnitType TypeOf(int unitId) =>
            StreamingContent.ReadUnitTypes().Types.First(t => t.Id == unitId);

        /// <summary>
        /// Attaches one held item and measures it. Returns 1 when something was
        /// measured, 0 when the hand was empty.
        /// </summary>
        private int MeasureHeld(UnitArt unit, GameObject held, string bone)
        {
            if (held == null)
            {
                return 0;
            }

            GameObject body = Instantiate(AssetDatabase.GetAssetPath(unit.Model));

            Transform socket = WeaponSocket.FindBone(body, bone);

            Assert.That(socket, Is.Not.Null,
                $"unit {unit.UnitId}'s model has no {bone} to hang {held.name} off");

            GameObject instance = WeaponSocket.Attach(body, held, bone);

            Assert.That(instance.transform.IsChildOf(socket), Is.True,
                $"unit {unit.UnitId}'s {held.name} is not under {bone} — it is hanging off the root, "
                + "which draws as a weapon lying through the middle of the body");

            Bounds bodyBounds = WorldBounds(body, except: instance);
            Bounds heldBounds = WorldBounds(instance, except: null);

            float ratio = heldBounds.size.magnitude / bodyBounds.size.magnitude;

            Debug.Log(
                $"[held] unit {unit.UnitId} carries {held.name} on {bone}: "
                + $"{heldBounds.size.magnitude:F2} m across against a {bodyBounds.size.magnitude:F2} m body "
                + $"({ratio:P0})");

            Assert.That(ratio, Is.GreaterThan(0.1f),
                $"unit {unit.UnitId}'s {held.name} measures {ratio:P0} of the body holding it. That is a "
                + "weapon whose own scale was thrown away on the way to the bone, and it draws as an "
                + "empty hand.");

            return 1;
        }

        /// <summary>
        /// The world bounds of every renderer under an object, optionally
        /// ignoring one subtree — used to measure a body without the thing it
        /// is holding.
        /// </summary>
        private static Bounds WorldBounds(GameObject root, GameObject except)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds? bounds = null;

            foreach (Renderer renderer in renderers)
            {
                if (except != null && renderer.transform.IsChildOf(except.transform))
                {
                    continue;
                }

                bounds = bounds == null ? renderer.bounds : Encapsulated(bounds.Value, renderer.bounds);
            }

            Assert.That(bounds, Is.Not.Null, root.name + " has no renderer to measure");

            return bounds.Value;
        }

        private static Bounds Encapsulated(Bounds first, Bounds second)
        {
            first.Encapsulate(second);

            return first;
        }

        /// <summary>
        /// Every model a unit is drawn with came in through the skinned path,
        /// with the bone a weapon hangs off.
        /// </summary>
        /// <remarks>
        /// All of them, rather than the one the spike started with. The bone is
        /// the rig coupling this project has: <c>handslot.l</c> is a KayKit
        /// name, and a model imported without it has nowhere for a weapon to go
        /// — a fact that only surfaces the day that model is given one. Looked
        /// up by string here rather than through the shipped helper, so this
        /// file asserts what the import produced and nothing about how the view
        /// uses it.
        /// </remarks>
        [Test]
        public void EveryUnitModelIsSkinnedAndCarriesTheWeaponBone()
        {
            foreach (string path in ChosenArt.ModelPaths)
            {
                GameObject character = Instantiate(path);

                SkinnedMeshRenderer[] skinned = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Assert.IsNotEmpty(skinned, $"{path} imported with no skinned mesh — this is the skinned import path");

                foreach (SkinnedMeshRenderer renderer in skinned)
                {
                    Assert.Greater(renderer.bones.Length, 0, $"{path}/{renderer.name} is skinned to no bones");
                    Assert.IsNotNull(renderer.rootBone, $"{path}/{renderer.name} has no root bone");
                }

                // Both hands, not just the one the bow found. A model missing
                // handslot.r imports and draws perfectly and only fails the day
                // somebody gives that unit a sword.
                foreach (string bone in new[] { "handslot.l", "handslot.r" })
                {
                    Assert.IsNotNull(
                        character.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == bone),
                        $"{path} carries no '{bone}' bone");
                }
            }
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

        /// <summary>
        /// Every clip a tower is posed with is in one of the banks.
        /// </summary>
        /// <remarks>
        /// They were all in the ranged bank while every tower drew a bow. Now a
        /// mage casts and a soldier chops, so the set spans three banks and the
        /// assertion is that a name resolves at all rather than that it resolves
        /// in one particular file.
        /// </remarks>
        [Test]
        public void EveryTowerStateClipIsInSomeBank()
        {
            // "__preview__" duplicates are editor thumbnail bookkeeping that
            // Unity hangs off any clip it has ever drawn an icon for.
            string[] names = ClipBankPaths
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<AnimationClip>()
                .Select(c => c.name)
                .Where(n => !n.StartsWith("__preview__"))
                .ToArray();

            foreach (string wanted in TowerClipNames)
            {
                Assert.Contains(wanted, names,
                    $"'{wanted}' is in none of the {ClipBankPaths.Length} banks. "
                    + $"Found: {string.Join(", ", names)}");
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
