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

        private const string EngineerModelPath = "Assets/Art/Kaykit/adventurers/Engineer.fbx";

        private const string TurretPath = "Assets/Art/Kaykit/adventurers/turret_base.fbx";

        private const string CratePath = "Assets/Art/Kaykit/adventurers/ammo_crate.fbx";

        private const string EngineerAtlasPath = "Assets/Art/Kaykit/adventurers/engineer_texture.png";

        private const string BarbarianAtlasPath = "Assets/Art/Kaykit/adventurers/barbarian_texture.png";

        /// <summary>
        /// The Adventurers pack's own knight sheet, which is not the copy in
        /// <c>Art/Characters</c> the live Knight model binds. The
        /// <c>shield_square</c> is authored on this one and sits beside it, so
        /// the importer's recursive-up search finds this file and not that one.
        /// </summary>
        private const string AdventurersKnightAtlasPath =
            "Assets/Art/Kaykit/adventurers/knight_texture.png";

        /// <summary>
        /// The Adventurers pack's own mage sheet, which for the same reason is
        /// not the copy in <c>Art/Characters</c> the live Mage model binds. The
        /// <c>spellbook_open</c> is authored on this one and sits beside it.
        /// </summary>
        private const string AdventurersMageAtlasPath =
            "Assets/Art/Kaykit/adventurers/mage_texture.png";

        private const string PaladinModelPath =
            "Assets/Art/Kaykit/mystery-monthly-series-4/paladin/Paladin_with_Helmet.fbx";

        private const string StatuePath =
            "Assets/Art/Kaykit/mystery-monthly-series-4/paladin/paladin_statue.fbx";

        private const string PaladinAtlasPath =
            "Assets/Art/Kaykit/mystery-monthly-series-4/paladin/paladin_texture_A.png";

        private const string PaladinAltAtlasPath =
            "Assets/Art/Kaykit/mystery-monthly-series-4/paladin/paladin_texture_B.png";

        private const string ClericModelPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/cleric/Cleric.fbx";

        private const string FontPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/cleric/Cleric_Font.fbx";

        private const string ClericAtlasPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/cleric/cleric_texture.png";

        private const string ClericAltAtlasPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/cleric/cleric_texture_B.png";

        /// <summary>The Lorekeeper's one sheet — that character ships no alternate.</summary>
        private const string LorekeeperAtlasPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/lorekeeper/lorekeeper_texture.png";

        /// <summary>
        /// The second model proposed for the Druid's tier 3 and set aside on
        /// issue #250: it read as a different creature rather than as the same
        /// person promoted, so that line is colour and a prop at every rung.
        /// Imported, and drawn by nothing.
        /// </summary>
        private const string PlantWarriorModelPath =
            "Assets/Art/Kaykit/mystery-monthly-series-6/plant-warrior/PlantWarrior.fbx";

        private const string DruidModelPath = "Assets/Art/Kaykit/adventurers/Druid.fbx";

        private const string DruidAtlasPath = "Assets/Art/Kaykit/adventurers/druid_texture.png";

        private const string DruidAltBAtlasPath =
            "Assets/Art/Kaykit/adventurers/druid_texture_alt_B.png";

        /// <summary>The bare weirwood the developer picked on 5 September 2026.</summary>
        private const string WeirwoodPath =
            "Assets/Art/Kaykit/forest-nature/Color8/Tree_Bare_1_C_Color8.fbx";

        /// <summary>
        /// The atlas the Forest Nature pack ships in every one of its eight
        /// colourway folders. The eight files are byte-identical: a colourway
        /// is where a model's UVs land on the sheet and not a different sheet.
        /// So which of the eight binds is the thing worth asserting -- any of
        /// them would draw, and only the one in the model's own folder is what
        /// the importer's recursive-up search is supposed to find.
        /// </summary>
        private const string ForestAtlasPath =
            "Assets/Art/Kaykit/forest-nature/Color8/forest_texture.png";

        /// <summary>
        /// How big the weirwood is drawn beside the Druid.
        /// </summary>
        /// <remarks>
        /// Measured rather than chosen by eye. At its own imported size the
        /// tree spreads 3.74 m across, which is nearly two of this board's
        /// 2.0 m tiles and reaches back through the Druid himself; this brings
        /// the spread to 2.06 -- the tile it is standing on -- and leaves it
        /// 2.89 m tall against a Druid who measures about two. The other three
        /// beside props are authored in the same packs as the characters they
        /// stand with and need no correction at all, which is the whole reason
        /// the size is per prop and not a constant.
        /// </remarks>
        private const float WeirwoodScale = 0.55f;

        /// <summary>
        /// The four looks <c>docs/roster.md</c> signs that put something on the
        /// ground beside a tower: which character, which atlas it wears, and
        /// what stands beside it at what size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Three of these are now bound to rows and one is not.</b> The
        /// Blessing, the Consecration and the Overgrowth have art, so the two
        /// binding tables carry them and this list holds those three against
        /// the record a second time; the Engineer's turret is still a look
        /// waiting for a row, with nowhere else in code to be written down. The
        /// record is <c>docs/roster.md</c>, and
        /// <c>docs/roster-expansion-beside-candidates.txt</c> is the same four
        /// again as something that can be photographed.
        /// </para>
        /// <para>
        /// <b>The Artificer's ammo crate is not here.</b> A tower has one
        /// beside slot and that rung's look puts the crate beside the turret,
        /// which is two -- so the crate is a question about the rung rather
        /// than a binding, and it goes on the candidate sheet instead.
        /// </para>
        /// </remarks>
        private static readonly (
            string look,
            string model,
            string atlas,
            string beside,
            float scale,
            string propAtlas)[] SignedBesideLooks =
        {
            ("the Engineer's turret", EngineerModelPath, null, TurretPath, 1f, EngineerAtlasPath),
            ("the Paladin's Blessing", PaladinModelPath, PaladinAltAtlasPath, StatuePath, 1f,
                PaladinAtlasPath),
            ("the Cleric's Consecration", ClericModelPath, ClericAltAtlasPath, FontPath, 1f,
                ClericAtlasPath),
            ("the Druid's Overgrowth", DruidModelPath, DruidAltBAtlasPath, WeirwoodPath, WeirwoodScale,
                ForestAtlasPath),
        };

        /// <summary>
        /// Where the Engineer's shot leaves: the top of the turret standing
        /// beside him, and not his own hands.
        /// </summary>
        /// <remarks>
        /// The name is the prop's, because <c>DrawnModel</c> names an instance
        /// after the asset and an FBX root node is named after its file. How far
        /// up the turret is not written down -- <see cref="EffectAnchor"/> reads
        /// it off the mesh, so a re-exported turret moves its own muzzle.
        /// </remarks>
        private static readonly EffectAnchor TurretMuzzle =
            EffectAnchor.AtTipOf("turret_base", Vector3.up);

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

            // The four props that stand beside a tower, and the characters they
            // stand beside. This is where the confetti risk is sharpest: a
            // row's own atlas covers its body only, so each of these has to
            // arrive already wearing its own pack's. The tree's pack ships
            // eight folders of identical bytes, so which one it binds is what
            // says whether the colourway resolved or whether another Color
            // folder answered first.
            (EngineerModelPath, EngineerAtlasPath),
            (TurretPath, EngineerAtlasPath),
            (CratePath, EngineerAtlasPath),
            (PaladinModelPath, PaladinAtlasPath),
            (StatuePath, PaladinAtlasPath),

            // The rest of the melee lines' art: each a body or a prop whose
            // atlas is in its own pack's folder -- the Adventurers barbarian
            // sheet for the Barbarian and both axes, the Paladin pack's for his
            // hammer, shield and book. The shield_square is on the Adventurers
            // pack's own knight sheet, which is not the copy in Art/Characters
            // that the live Knight model binds.
            (ChosenArt.BarbarianModelPath, BarbarianAtlasPath),
            (ChosenArt.BarbarianLargeModelPath, BarbarianAtlasPath),
            (ChosenArt.AxeModelPath, BarbarianAtlasPath),
            (ChosenArt.LargeAxeModelPath, BarbarianAtlasPath),
            (ChosenArt.ShieldSquareModelPath, AdventurersKnightAtlasPath),
            (ChosenArt.PaladinModelPath, PaladinAtlasPath),
            (ChosenArt.HammerModelPath, PaladinAtlasPath),
            (ChosenArt.PaladinShieldModelPath, PaladinAtlasPath),
            (ChosenArt.BookModelPath, PaladinAtlasPath),

            (ClericModelPath, ClericAtlasPath),
            (FontPath, ClericAtlasPath),
            (DruidModelPath, DruidAtlasPath),
            (WeirwoodPath, ForestAtlasPath),

            // The caster lines' remaining art. The spellbook is authored beside
            // the Adventurers pack's own mage sheet, which is not the copy in
            // Art/Characters that the live Mage model binds; the two Cleric
            // props are on the Cleric's; and the Lorekeeper is a whole
            // character with its own.
            (ChosenArt.SpellbookModelPath, AdventurersMageAtlasPath),
            (ChosenArt.ClericTomeModelPath, ClericAtlasPath),
            (ChosenArt.ClericMaceModelPath, ClericAtlasPath),
            (ChosenArt.LorekeeperModelPath, LorekeeperAtlasPath),
            (ChosenArt.LorekeeperTomeModelPath, LorekeeperAtlasPath),
            (ChosenArt.DruidStaffModelPath, DruidAtlasPath),
        };

        /// <summary>
        /// Every clip a tower is posed with, as a bare name. Three states each,
        /// and the set a tower gets depends on what it holds — the bow three
        /// for the Archer and the Ranger, rest-and-cast for the Mage,
        /// rest-and-chop for the Soldier, the two-handed chop for the
        /// Barbarian, the raised guard for the Shield Wall, the slam for the
        /// Slam and the cast for the Cleric and Druid lines. See #44, the
        /// 14 August weapon pass and <c>docs/roster.md</c>.
        /// </summary>
        /// <remarks>
        /// The bank a name comes out of is asserted separately, in
        /// <see cref="EveryClipComesOutOfTheBankForItsRowsRig"/>. This one asks
        /// only that the name exists somewhere, which is why the Large rig's
        /// clip is written here without its bank.
        /// </remarks>
        private static readonly string[] TowerClipNames =
        {
            ChosenArt.BowIdleClipName,
            ChosenArt.BowDrawClipName,
            ChosenArt.BowReleaseClipName,
            ChosenArt.RestClipName,
            ChosenArt.SpellcastClipName,
            ChosenArt.ChopClipName,
            ChosenArt.TwoHandedChopClipName,
            ChosenArt.BlockingClipName,
            ChosenArt.ShootClipName,
            SlamClipBareName,
        };

        /// <summary>The Slam's swing, without the bank its binding names.</summary>
        private const string SlamClipBareName = "Melee_2H_Slam";

        /// <summary>The clip banks: the FBXs imported for their curves, not their meshes.</summary>
        private static readonly string[] ClipBankPaths =
        {
            ChosenArt.MovementBankPath,
            ChosenArt.GeneralBankPath,
            ChosenArt.RangedBankPath,
            ChosenArt.MeleeBankPath,
            ChosenArt.LargeGeneralBankPath,
            ChosenArt.LargeMeleeBankPath,
        };

        /// <summary>The banks of the second rig, which one row is drawn on.</summary>
        private static readonly string[] LargeBankPaths =
        {
            ChosenArt.LargeGeneralBankPath,
            ChosenArt.LargeMeleeBankPath,
        };

        /// <summary>The Slam, which is the only row on the Large rig.</summary>
        private const int SlamUnitId = 19;

        /// <summary>The Druid, the Elder and the Overgrowth, in roster order.</summary>
        private static readonly int[] DruidLineUnitIds = { 28, 29, 30 };

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
        /// No two rows drawn with the same model are drawn identically.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The rule from <c>docs/roster.md</c>, swept over every pair rather
        /// than named one pair at a time.</b> A rung is told apart from the one
        /// below it by what the body wears, holds or stands beside — never by
        /// how big it is — so two rows on one model that agree about all four of
        /// those are two rungs a player cannot tell apart. Nine rows now share a
        /// model with another, and the day somebody adds a tenth this covers it
        /// by being written this way rather than by anybody remembering.
        /// </para>
        /// <para>
        /// Which atlas or which prop separates a given pair stays unnamed:
        /// those are the developer's to move, and naming them would make this
        /// test the place the art is decided. It asserts only that something
        /// does.
        /// </para>
        /// <para>
        /// <b>The rows on <see cref="UnboundUnits"/>'s list are skipped, and
        /// they are the reason this needs saying.</b> Every one of them draws
        /// the same stand-in, holding nothing, in no atlas — they are
        /// deliberately indistinguishable, which is how an undressed row reads
        /// as undressed. Issue #271 empties that list and this covers those
        /// rows the moment it does.
        /// </para>
        /// </remarks>
        [Test]
        public void NoTwoRowsOnOneModelAreDrawnAlike()
        {
            IReadOnlyList<UnitArt> rows = ChosenArt.Load().Units;
            var compared = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                for (int j = i + 1; j < rows.Count; j++)
                {
                    UnitArt below = rows[i];
                    UnitArt above = rows[j];

                    if (below.Model != above.Model
                        || UnboundUnits.Lists(below.UnitId)
                        || UnboundUnits.Lists(above.UnitId))
                    {
                        continue;
                    }

                    compared++;

                    bool told = below.Texture != above.Texture
                        || below.RightHand != above.RightHand
                        || below.LeftHand != above.LeftHand
                        || below.Beside.Model != above.Beside.Model;

                    Assert.That(told, Is.True,
                        $"units {below.UnitId} and {above.UnitId} draw the same model in the same atlas, "
                        + "holding the same things, with the same thing beside them — so nothing on the "
                        + "board tells the two rungs apart. See docs/roster.md: a rung is told apart by "
                        + "what the body wears, holds or stands beside");
                }
            }

            Assert.That(compared, Is.GreaterThan(0),
                "no two rows share a model, so this compared nothing at all");
        }

        /// <summary>
        /// Every rung of the Druid line is drawn on the Druid, and no row
        /// anywhere is drawn on the PlantWarrior.
        /// </summary>
        /// <remarks>
        /// <b>A rejection only holds where something reads it.</b> The
        /// PlantWarrior was proposed as this line's second model and set aside
        /// on issue #250 — of the six second models it was the only one that
        /// read as a different creature rather than as the same person promoted
        /// — so the Druid keeps his own body and is told apart by colour and by
        /// the weirwood beside him, the way the Knight, the Cleric and the
        /// Engineer are. The model is imported and the proposal that named it
        /// is still in <c>docs/</c>, so what keeps it unbound is this rather
        /// than everybody remembering. Held over the whole table and over the
        /// beside socket, since a body may stand beside a tower as easily as
        /// under one.
        /// </remarks>
        [Test]
        public void TheDruidLineIsDrawnOnTheDruidAndNothingOnThePlantWarrior()
        {
            MatchArt art = ChosenArt.Load();
            GameObject druid = Loaded(DruidModelPath);
            GameObject plantWarrior = Loaded(PlantWarriorModelPath);

            foreach (int unitId in DruidLineUnitIds)
            {
                Assert.That(art.ModelFor(unitId), Is.SameAs(druid),
                    $"unit {unitId} is a rung of the Druid line and docs/roster.md draws every one of "
                    + "them on the Druid himself — that line has no second model");
            }

            foreach (UnitArt unit in art.Units)
            {
                Assert.That(unit.Model, Is.Not.SameAs(plantWarrior),
                    $"unit {unit.UnitId} is drawn on the PlantWarrior, which issue #250 set aside");

                Assert.That(unit.Beside.Model, Is.Not.SameAs(plantWarrior),
                    $"unit {unit.UnitId} stands beside the PlantWarrior, which issue #250 set aside");
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
        /// <b>A row on <see cref="UnboundUnits"/>'s list is the one exception,
        /// and it is held the other way round.</b> An anchor is a point on a
        /// bone or inside a held prop, so it is chosen by whoever chooses the
        /// prop — and a row drawing the stand-in is a row nobody has chosen one
        /// for. Such a row is required to name NO anchor, which is the fixed
        /// height above its own root, and reads as undressed exactly as its
        /// empty hands and its bind pose do. Issue #271 empties that list, and
        /// this assertion covers every placed row again the moment it does.
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

                if (UnboundUnits.Lists(type.Id))
                {
                    Assert.That(unit.EffectAnchor.IsSet, Is.False,
                        $"unit {type.Id} ({type.Label}) is listed as having no art yet and names an "
                        + "anchor anyway. An anchor is a point on a prop nobody has chosen for this row, "
                        + "so there is nothing for it to be a point on");

                    continue;
                }

                Assert.That(unit.EffectAnchor.IsSet, Is.True,
                    $"unit {type.Id} ({type.Label}) stands on the board and shoots, and its art names "
                    + "nowhere for the shot to leave from — so it fires from a height above its own root, "
                    + "whatever it is holding");

                TowerView tower = BuiltTower(type, unit);
                Transform anchor = tower.AnchorTransform;

                Assert.That(anchor, Is.Not.Null,
                    $"unit {type.Id} ({type.Label}) has an anchor that resolved to nothing");

                // On the body, or on the thing standing beside it. Both are
                // this row's own art and a row may fire from either — the
                // Engineer's shell leaves his turret while the Paladin beside
                // his statue still fires from his book. What is excluded is
                // everything else, which is the scene.
                var onTheArt = anchor.IsChildOf(tower.Model.transform)
                    || (tower.Beside != null && anchor.IsChildOf(tower.Beside.transform));

                Assert.That(onTheArt, Is.True,
                    $"unit {type.Id} ({type.Label}) anchors on {anchor.name}, which is neither part of "
                    + "its model nor part of what stands beside it — an effect anchor is a point on the "
                    + "art, not on the scene");

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

        /// <summary>
        /// The Engineer's turret stands on the tile beside him, keeps standing
        /// there while he turns, and is where his shots leave from.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the whole of the Engineer line's identity.</b> He is a
        /// wrench and a turret at every rung; the turret is what fires, so an
        /// anchor that fell back to his own body would put the shell coming out
        /// of the man rather than out of the machine. The anchor resolves
        /// across the built tower, which is what makes a node inside a beside
        /// prop the same kind of name as a bone.
        /// </para>
        /// <para>
        /// <b>The turn is asserted because that is the failure this cannot see
        /// in a photograph.</b> A tower rotates to track a creep, so a prop left
        /// at a fixed local offset orbits it — swinging through the neighbouring
        /// tiles once per target — and a still frame of any single tick looks
        /// perfectly correct.
        /// </para>
        /// </remarks>
        [Test]
        public void TheEngineersTurretStandsBesideHimAndHisShotsLeaveIt()
        {
            TowerView tower = BuiltTower(TypeOf(ArcherUnitId), EngineerWithHisTurret());

            Assert.That(tower.Beside, Is.Not.Null, "nothing was drawn beside the Engineer");
            Assert.That(tower.Beside.name, Is.EqualTo("turret_base"));

            Assert.That(tower.Beside.transform.IsChildOf(tower.transform), Is.True,
                "the turret hangs off the tower root, which is what makes it a socket rather than "
                + "scenery somebody left on the board");

            Assert.That(tower.Beside.transform.IsChildOf(tower.Model.transform), Is.False,
                "the turret is under the body, so it inherits the row's scale and whatever atlas the "
                + "row wears — which for a prop off another pack is confetti at the wrong size");

            AssertStandsAt(
                tower.Beside.transform.position,
                tower.transform.position + BesideProp.NextTile,
                "the turret does not stand on the tile beside him");

            Transform anchor = tower.AnchorTransform;

            Assert.That(anchor, Is.Not.Null, "the Engineer's anchor resolved to nothing");

            Assert.That(anchor.IsChildOf(tower.Beside.transform), Is.True,
                $"the Engineer fires from {anchor.name}, which is not part of the turret — the shot "
                + "leaves the machine and not the man holding the wrench");

            Bounds turret = WorldBounds(tower.Beside, null);

            Assert.That(tower.Muzzle.y, Is.GreaterThan(turret.center.y),
                "the shell leaves the underside of the turret");

            Debug.Log(
                $"[beside] the Engineer's turret is {turret.size.y:F2} m tall and fires from "
                + $"{tower.Muzzle.y:F2} m up, {Vector3.Distance(tower.Muzzle, tower.transform.position):F2} m "
                + "from his own root");

            // Turned to track a creep behind him. The tower rotates; the thing
            // on the ground beside it does not.
            Vector3 stood = tower.Beside.transform.position;

            tower.Pose(TowerState.Idle, 0, tower.transform.position + (Vector3.forward * 8f));

            AssertStandsAt(
                tower.Beside.transform.position, stood,
                "the turret moved when the Engineer turned, so it orbits him rather than standing on a tile");
        }

        /// <summary>
        /// Each of the four signed beside looks resolves: the prop is there, it
        /// is drawn at the size written down for it, and it wears its own
        /// pack's atlas rather than the row's.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The atlas is the assertion that matters here.</b> A row's colour
        /// is put on the bare body before anything else is attached, and these
        /// props are each their own import off their own pack — so a font drawn
        /// against a cleric's character sheet is confetti rather than a
        /// slightly-wrong font. That the prop is a sibling of the body rather
        /// than a child of it is what makes that true, and it is asserted by
        /// reading the material the prop actually ends up drawing with.
        /// </para>
        /// <para>
        /// <b>The row it is built against is a stand-in.</b> None of these four
        /// looks has a row in <c>content/units.txt</c>; a live placed row lends
        /// its <see cref="UnitType"/> because <see cref="TowerView"/> reads two
        /// tick budgets off one and neither is reached from a tower that is
        /// never posed.
        /// </para>
        /// </remarks>
        [Test]
        public void EverySignedBesidePropStandsForItsRow()
        {
            foreach ((string look, string model, string atlas, string beside, float scale, string propAtlas)
                in SignedBesideLooks)
            {
                UnitArt drawnAs = UnitArt.Armed(
                    0,
                    Loaded(model),
                    MatchArt.TowerScale,
                    null,
                    null,
                    null,
                    null,
                    null,
                    default,
                    default,
                    default,
                    atlas == null ? null : LoadedAtlas(atlas),
                    BesideProp.OnTheNextTile(Loaded(beside), scale));

                TowerView tower = BuiltTower(TypeOf(ArcherUnitId), drawnAs);

                Assert.That(tower.Beside, Is.Not.Null, look + " drew nothing beside its tower");

                Renderer[] renderers = tower.Beside.GetComponentsInChildren<Renderer>(true);

                Assert.That(renderers, Is.Not.Empty, look + " stands beside its tower with no mesh at all");

                AssertStandsAt(
                    tower.Beside.transform.position,
                    tower.transform.position + BesideProp.NextTile,
                    look + " does not stand on the tile beside its tower");

                foreach (Renderer renderer in renderers)
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        Assert.That(material, Is.Not.Null, look + " has a null material slot");

                        Texture bound = MainTextureOf(material);

                        Assert.That(bound, Is.Not.Null,
                            look + " bound no atlas at all, which draws flat magenta");

                        // Named, not merely "not the row's own". The row's atlas
                        // is null on one of these looks, so an inequality
                        // against it would assert nothing at all there — and
                        // the failure being guarded is a prop wearing SOME
                        // other sheet, not specifically the character's.
                        Assert.That(AssetDatabase.GetAssetPath(bound), Is.EqualTo(propAtlas),
                            look + " is drawn against an atlas that is not its own pack's, which is "
                            + "confetti rather than a slightly-wrong prop — the row's colour goes on the "
                            + "bare body and must not reach what stands beside it");
                    }
                }

                Bounds drawn = WorldBounds(tower.Beside, null);

                Debug.Log(
                    $"[beside] {look}: {tower.Beside.name} at x{scale}, {drawn.size.y:F2} m tall, "
                    + $"{drawn.size.x:F2} x {drawn.size.z:F2} on the ground");

                Assert.That(drawn.size.x, Is.LessThan(HexGeometry.AcrossFlats * 1.5f),
                    look + " spreads wider than half again the tile it is standing on, so it is over "
                    + "its neighbours and probably over its own tower");
            }
        }

        /// <summary>
        /// A row that names a prop to stand beside it and never says how big
        /// stops the view being built, and says which prop.
        /// </summary>
        /// <remarks>
        /// Zero is what an unwritten serialized field holds, so this is the
        /// shape of both failures that can reach here: a model dropped into the
        /// inspector slot, and a binding table that named a prop and left the
        /// size out. Neither throws anywhere else — a prop drawn at no size at
        /// all is a prop nobody can see missing, which is the same refusal
        /// <see cref="WeaponSocket"/> makes about a bone that is not there.
        /// </remarks>
        [Test]
        public void APropStandingBesideATowerAtNoSizeAtAllFailsByName()
        {
            UnitArt sizeless = UnitArt.Armed(
                0, Loaded(EngineerModelPath), MatchArt.TowerScale, null, null, null, null, null,
                default, default, default, null,
                BesideProp.Standing(Loaded(TurretPath), 0f, BesideProp.NextTile));

            var host = new GameObject("sizeless-beside-prop");
            _spawned.Add(host);

            var tower = host.AddComponent<TowerView>();

            var refused = Assert.Throws<System.InvalidOperationException>(
                () => tower.BuildStatic(0, TypeOf(ArcherUnitId), sizeless, Quaternion.identity));

            Assert.That(refused.Message, Does.Contain("turret_base"),
                "the refusal has to name the prop that has no size, or it sends the reader looking at the "
                + "art instead of at the table");
        }

        /// <summary>The Engineer's tier-1 look: the wrench in hand, the turret beside him.</summary>
        private static UnitArt EngineerWithHisTurret() =>
            UnitArt.Armed(
                0,
                Loaded(EngineerModelPath),
                MatchArt.TowerScale,
                Loaded("Assets/Art/Kaykit/adventurers/engineer_Wrench.fbx"),
                null,
                null,
                null,
                null,
                default,
                default,
                TurretMuzzle,
                null,
                BesideProp.OnTheNextTile(Loaded(TurretPath), 1f));

        /// <summary>Where something stands, to the millimetre.</summary>
        private static void AssertStandsAt(Vector3 actual, Vector3 expected, string message) =>
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(1e-3f),
                message + " — it is at " + actual + " and not at " + expected);

        private static GameObject Loaded(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path)
            ?? throw new AssertionException("nothing imported at " + path);

        private static Texture2D LoadedAtlas(string path) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(path)
            ?? throw new AssertionException("nothing imported at " + path);

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
        /// Every clip a tower is posed with comes out of the bank for the rig
        /// its model is on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the failure the second rig introduced.</b> The collection
        /// ships <c>Rig_Medium</c> and <c>Rig_Large</c>, and
        /// <c>Idle_A</c>, <c>Walking_A</c> and <c>Death_A</c> are in both — so
        /// a bare clip name asked of the banks in order answers with the medium
        /// one every time. A medium clip on a Large body does not throw and
        /// does not warn: it drives bones that are not there and leaves the ones
        /// that are where they started, which reads as the model being bad.
        /// Both binding tables spell a Large clip with its bank for that
        /// reason, and this is what holds them to it.
        /// </para>
        /// <para>
        /// <b>Held both ways.</b> A Large row's clips must come from the Large
        /// banks and every other row's must not, because the second half is
        /// what catches a medium row that gained a bank prefix by being copied
        /// from the row above it.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryClipComesOutOfTheBankForItsRowsRig()
        {
            var large = new HashSet<AnimationClip>(
                LargeBankPaths.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>());

            MatchArt art = ChosenArt.Load();
            UnitArt slam = art.ArtFor(SlamUnitId);

            Assert.That(slam.Model, Is.SameAs(Loaded(ChosenArt.BarbarianLargeModelPath)),
                $"unit {SlamUnitId} is the row this test knows to be on the Large rig, and it is drawn "
                + "with something else now — the roster has moved under this assertion");

            Assert.That(slam.IsPosed, Is.True,
                $"unit {SlamUnitId} carries no clips, so the rig this test is about is not exercised");

            var measured = 0;

            foreach (UnitArt unit in art.Units)
            {
                bool onTheLargeRig = unit.UnitId == SlamUnitId;

                foreach (AnimationClip clip in new[] { unit.IdleClip, unit.WindupClip, unit.BackswingClip })
                {
                    if (clip == null)
                    {
                        continue;
                    }

                    Assert.That(large.Contains(clip), Is.EqualTo(onTheLargeRig),
                        $"unit {unit.UnitId} is posed with '{clip.name}' from the "
                        + (onTheLargeRig ? "Medium" : "Large") + " banks, and its model is on the "
                        + (onTheLargeRig ? "Large" : "Medium") + " rig");

                    measured++;
                }
            }

            Assert.That(measured, Is.GreaterThan(0), "no row is posed at all, so this measured nothing");
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
