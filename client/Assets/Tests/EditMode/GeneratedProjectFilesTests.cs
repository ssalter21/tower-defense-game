using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The generated files this ticket introduced, checked against the things
    /// they were generated from.
    /// </summary>
    /// <remarks>
    /// Generated files are committed in this repository, beside the change that
    /// caused them, so that a fresh clone is the same project as the one it was
    /// cloned from. The cost of that rule is drift: a copy is only as good as
    /// the last time somebody remembered to regenerate it. These tests are what
    /// replaces remembering.
    /// </remarks>
    public class GeneratedProjectFilesTests
    {
        /// <summary>
        /// The authored content is in <c>content/</c> at the repository root
        /// and the player reads <c>Assets/StreamingAssets/content/</c>. The
        /// second is a generated copy of the first, and if they have drifted
        /// then the game and the simulation's own tests are running different
        /// maps -- which is the shape of a bug nobody would think to look for.
        /// </summary>
        [Test]
        public void TheStreamingCopyOfTheMapMatchesTheAuthoredOne()
        {
            string authored = Path.Combine(RepositoryRoot(), "content", StreamingContent.MapFileName);
            string shipped = StreamingContent.PathOf(StreamingContent.MapFileName);

            Assert.That(File.Exists(authored), Is.True, "No authored map at " + authored);
            Assert.That(
                File.Exists(shipped),
                Is.True,
                "No streaming copy at " + shipped + ". Run tools/sync-streaming-content.ps1.");

            Assert.That(
                File.ReadAllBytes(shipped),
                Is.EqualTo(File.ReadAllBytes(authored)),
                "The streaming copy has drifted from " + authored
                + ". Run tools/sync-streaming-content.ps1 and commit what it writes.");
        }

        /// <summary>
        /// The map the view reads is parsed by the simulation's parser, so the
        /// corridor assertion runs on the bytes that shipped rather than only
        /// on the bytes in the repository.
        /// </summary>
        [Test]
        public void TheShippedMapLoadsThroughTheSimulationsParser()
        {
            Sim.HexMap map = StreamingContent.ReadMap();

            Assert.That(map.Width, Is.GreaterThan(0));
            Assert.That(map.Height, Is.GreaterThan(0));
            Assert.That(map.Route.Count, Is.GreaterThan(1), "The corridor was traced, not merely read.");
        }

        /// <summary>
        /// The two materials are assets so a build carries their shader, but
        /// their colours are constants so a diff can show them. Two homes for
        /// one number is drift waiting to happen, so it is asserted.
        /// </summary>
        [Test]
        public void TheCommittedMaterialsCarryTheCommittedColours()
        {
            AssertColour(MatchSceneBuilder.RoadMaterialPath, SceneFraming.RoadColor);
            AssertColour(MatchSceneBuilder.GrassMaterialPath, SceneFraming.GrassColor);
        }

        private static void AssertColour(string path, Color expected)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            Assert.That(material, Is.Not.Null, "No material at " + path + ". Run tools/build-match-scene.ps1.");

            Color actual = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.GetColor("_Color");

            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.002f), path + " red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.002f), path + " green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.002f), path + " blue");
        }

        /// <summary>
        /// The generated art manifest still names the assets it was generated
        /// from.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>Resources/MatchArt.asset</c> is what the play-mode suite reads
        /// when there is no editor to ask — a player, where
        /// <c>AssetDatabase</c> does not exist. It is generated from
        /// <see cref="ChosenArt"/> by <c>tools/build-test-assets.ps1</c> and
        /// committed, so like every other generated file here it can drift from
        /// its source between somebody changing one and remembering to run the
        /// other. This is what replaces remembering.
        /// </para>
        /// <para>
        /// Compared by reference identity rather than by name: two clips called
        /// <c>Walking_A</c> out of two different banks are the same string and
        /// different animations, and it is exactly that substitution a stale
        /// manifest would make.
        /// </para>
        /// </remarks>
        [Test]
        public void TheGeneratedArtManifestMatchesTheArtItWasGeneratedFrom()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MatchArtAsset>(GeneratedTestAssets.ManifestPath);

            Assert.That(asset, Is.Not.Null,
                "No manifest at " + GeneratedTestAssets.ManifestPath + ". Run tools/build-test-assets.ps1.");

            MatchArt generated = asset.Art;
            MatchArt chosen = ChosenArt.Load();

            void Same(Object inManifest, Object inChosenArt, string field) =>
                Assert.That(inManifest, Is.SameAs(inChosenArt),
                    GeneratedTestAssets.ManifestPath + " names a different " + field
                    + " than ChosenArt does. Run tools/build-test-assets.ps1 and commit what it writes.");

            // Is.SameAs does not hold for two nulls, and two nulls are exactly
            // what an empty hand or a creep's absent clips look like on both
            // sides. Absent-on-one-side-only is still a failure.
            void SameOrBothEmpty(Object inManifest, Object inChosenArt, string field)
            {
                if (inManifest == null && inChosenArt == null)
                {
                    return;
                }

                Same(inManifest, inChosenArt, field);
            }

            void SameTilt(Quaternion inManifest, Quaternion inChosenArt, string field) =>
                Assert.That(Quaternion.Angle(inManifest, inChosenArt), Is.LessThan(0.01f),
                    GeneratedTestAssets.ManifestPath + " turns the " + field
                    + " differently than ChosenArt does. Run tools/build-test-assets.ps1 "
                    + "and commit what it writes.");

            Assert.That(
                generated.Units.Select(u => u.UnitId),
                Is.EqualTo(chosen.Units.Select(u => u.UnitId)),
                GeneratedTestAssets.ManifestPath + " covers different units than ChosenArt does. "
                + "Run tools/build-test-assets.ps1 and commit what it writes.");

            foreach (UnitArt unit in chosen.Units)
            {
                UnitArt made = generated.ArtFor(unit.UnitId);

                Same(made.Model, unit.Model, "model for unit " + unit.UnitId);

                Assert.That(made.Scale, Is.EqualTo(unit.Scale),
                    GeneratedTestAssets.ManifestPath + " draws unit " + unit.UnitId
                    + " at a different size than ChosenArt does. Run tools/build-test-assets.ps1 "
                    + "and commit what it writes.");

                // What a unit holds and the clips it holds it with are per unit
                // and generated the same way the model is, so they drift the
                // same way and are compared the same way. Optional, though: an
                // empty hand and a creep's absent clips are both a legitimate
                // null, and two nulls agree.
                // Tilt, and this is the field that proves the comparison has to
                // be exhaustive rather than representative. The staffs' quarter
                // turn went into ChosenArt and into the builder on 14 August
                // 2026 and not into the manifest, and this test stayed green
                // over a manifest that disagreed with its own source -- so the
                // player path, which is the manifest, went on drawing both
                // staffs flat while every editor path drew them upright.
                //
                // Compared as an angle and not as Euler triples: two different
                // triples can name the same rotation, and a failure on that
                // difference would name a drift nobody could act on.
                SameTilt(made.RightHandTilt, unit.RightHandTilt, "right-hand item for unit " + unit.UnitId);
                SameTilt(made.LeftHandTilt, unit.LeftHandTilt, "left-hand item for unit " + unit.UnitId);

                SameOrBothEmpty(made.RightHand, unit.RightHand, "right hand for unit " + unit.UnitId);
                SameOrBothEmpty(made.LeftHand, unit.LeftHand, "left hand for unit " + unit.UnitId);
                SameOrBothEmpty(made.IdleClip, unit.IdleClip, "idle clip for unit " + unit.UnitId);
                SameOrBothEmpty(made.WindupClip, unit.WindupClip, "windup clip for unit " + unit.UnitId);
                SameOrBothEmpty(
                    made.BackswingClip, unit.BackswingClip, "backswing clip for unit " + unit.UnitId);
            }

            Same(generated.CreepWalkClip, chosen.CreepWalkClip, nameof(chosen.CreepWalkClip));
            Same(generated.CreepDeathClip, chosen.CreepDeathClip, nameof(chosen.CreepDeathClip));
        }

        /// <summary>
        /// The committed panel settings hold the text engine's ICU data, and
        /// every other value on them is what a fresh <see cref="PanelSettings"/>
        /// has.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="RuntimePanel.Settings"/> clones the asset, and
        /// <see cref="Object.Instantiate"/> copies every serialized field —
        /// including the ones that method does not go on to assign. So a value
        /// edited into this asset's YAML reaches every bar in the game without
        /// appearing in any code, which is the drift the whole file is against.
        /// The clear colour, the render mode and the DPI pair are the ones that
        /// would show.
        /// </para>
        /// <para>
        /// The theme is set on the comparison as well as on the asset, because
        /// the generator writes it: it is the one value the asset is expected to
        /// differ from a bare instance by, and asserting it here is what keeps
        /// the list of expected differences down to it and the ICU reference.
        /// </para>
        /// </remarks>
        [Test]
        public void TheCommittedPanelSettingsCarryICUDataAndNothingElse()
        {
            var committed = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset.AssetPath);

            Assert.That(committed, Is.Not.Null,
                "No panel settings at " + PanelSettingsAsset.AssetPath
                + ". Run tools/build-panel-settings.ps1.");

            var fresh = ScriptableObject.CreateInstance<PanelSettings>();
            fresh.themeStyleSheet = RuntimePanel.Theme();

            try
            {
                var written = new SerializedObject(committed);
                var bare = new SerializedObject(fresh);

                Assert.That(
                    written.FindProperty(ICUDataField)?.objectReferenceValue,
                    Is.Not.Null,
                    PanelSettingsAsset.AssetPath + " carries no ICU data, which is the one thing it is "
                    + "for. Run tools/build-panel-settings.ps1 and commit what it writes.");

                var drifted = new List<string>();
                SerializedProperty walk = written.GetIterator();

                // Entered once and never again: DataEquals compares a struct
                // like m_DynamicAtlasSettings whole, and descending would walk
                // a string's char array and report m_Name twenty-one times.
                bool enter = true;

                while (walk.Next(enter))
                {
                    enter = false;

                    if (Bookkeeping(walk.propertyPath))
                    {
                        continue;
                    }

                    SerializedProperty same = bare.FindProperty(walk.propertyPath);

                    if (same == null || !SerializedProperty.DataEquals(walk, same))
                    {
                        drifted.Add(walk.propertyPath);
                    }
                }

                Assert.That(drifted, Is.Empty,
                    PanelSettingsAsset.AssetPath + " differs from a fresh PanelSettings in more than its "
                    + "ICU data: " + string.Join(", ", drifted) + ". Those values are cloned onto every "
                    + "panel in the game. Either they belong in RuntimePanel.Settings, or run "
                    + "tools/build-panel-settings.ps1 and commit what it writes.");
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }
        }

        /// <summary>
        /// Whether a serialized property is Unity's own record-keeping rather
        /// than a value anybody chose — the asset's name, the script it points
        /// at, and the ICU reference this asset exists to carry.
        /// </summary>
        private static bool Bookkeeping(string path) =>
            path == "m_Name"
            || path == "m_Script"
            || path == "m_EditorClassIdentifier"
            || path == "m_ObjectHideFlags"
            || path == "m_EditorHideFlags"
            || path == "m_CorrespondingSourceObject"
            || path == "m_PrefabInstance"
            || path == "m_PrefabAsset"
            || path == "m_GameObject"
            || path == "m_Enabled"
            || path == ICUDataField;

        /// <summary>The field the editor puts the text engine's ICU data in.</summary>
        private const string ICUDataField = "m_ICUDataAsset";

        /// <summary>
        /// Walks up from the project looking for the simulation's project file,
        /// the same way the simulation's own tests find the root. Throwing with
        /// the path it looked at, because a test that silently skips is a test
        /// that is green for no reason.
        /// </summary>
        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Application.dataPath);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "sim", "Sim.csproj")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not find the repository root by walking up from " + Application.dataPath
                + " looking for sim/Sim.csproj.");
        }
    }
}
