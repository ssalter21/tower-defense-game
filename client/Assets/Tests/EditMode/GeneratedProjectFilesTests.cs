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

                // The anchor, which is two fields and neither is an asset
                // reference -- so nothing above would have noticed a manifest
                // still firing every tower from a height above its root.
                Assert.That(made.EffectAnchor.TransformName, Is.EqualTo(unit.EffectAnchor.TransformName),
                    GeneratedTestAssets.ManifestPath + " anchors unit " + unit.UnitId
                    + "'s effects somewhere else than ChosenArt does. Run tools/build-test-assets.ps1 "
                    + "and commit what it writes.");

                Assert.That(made.EffectAnchor.Tip, Is.EqualTo(unit.EffectAnchor.Tip),
                    GeneratedTestAssets.ManifestPath + " takes unit " + unit.UnitId
                    + "'s anchor from a different end of the same thing than ChosenArt does. "
                    + "Run tools/build-test-assets.ps1 and commit what it writes.");

                SameOrBothEmpty(made.RightHand, unit.RightHand, "right hand for unit " + unit.UnitId);
                SameOrBothEmpty(made.LeftHand, unit.LeftHand, "left hand for unit " + unit.UnitId);

                // The atlas, which is neither a model nor a clip and would
                // otherwise drift the way the tilts once did -- leaving the
                // player path drawing a tier in the colour of the rung below it
                // while every editor path drew it right.
                SameOrBothEmpty(made.Texture, unit.Texture, "atlas for unit " + unit.UnitId);

                // What stands beside the row, which is a model, a size and a
                // place. Only the first of the three is an asset reference, so
                // a manifest that kept the turret and drew it at a tenth of its
                // size, or on the tower's own tile, would pass everything else
                // here.
                SameOrBothEmpty(made.Beside.Model, unit.Beside.Model, "beside prop for unit " + unit.UnitId);

                Assert.That(made.Beside.Scale, Is.EqualTo(unit.Beside.Scale),
                    GeneratedTestAssets.ManifestPath + " draws what stands beside unit " + unit.UnitId
                    + " at a different size than ChosenArt does. Run tools/build-test-assets.ps1 "
                    + "and commit what it writes.");

                Assert.That(made.Beside.Offset, Is.EqualTo(unit.Beside.Offset),
                    GeneratedTestAssets.ManifestPath + " stands what is beside unit " + unit.UnitId
                    + " somewhere else than ChosenArt does. Run tools/build-test-assets.ps1 "
                    + "and commit what it writes.");
                SameOrBothEmpty(made.IdleClip, unit.IdleClip, "idle clip for unit " + unit.UnitId);
                SameOrBothEmpty(made.WindupClip, unit.WindupClip, "windup clip for unit " + unit.UnitId);
                SameOrBothEmpty(
                    made.BackswingClip, unit.BackswingClip, "backswing clip for unit " + unit.UnitId);

                // The per-row walk and death, which only the four Large-rig
                // creeps carry. A manifest that dropped them would fall back to
                // the shared medium pair and draw those four sliding down the
                // corridor in their bind pose -- in the player only, with every
                // editor path drawing them right, which is exactly how the
                // staffs' tilt drifted.
                SameOrBothEmpty(made.WalkClip, unit.WalkClip, "walk clip for unit " + unit.UnitId);
                SameOrBothEmpty(made.DeathClip, unit.DeathClip, "death clip for unit " + unit.UnitId);
            }

            Same(generated.CreepWalkClip, chosen.CreepWalkClip, nameof(chosen.CreepWalkClip));
            Same(generated.CreepDeathClip, chosen.CreepDeathClip, nameof(chosen.CreepDeathClip));
        }

        /// <summary>
        /// The committed panel settings carry the text engine's ICU data.
        /// </summary>
        /// <remarks>
        /// The editor attaches that data on its way to disk and to nothing
        /// created at runtime, so it is the whole reason the asset is committed
        /// and the whole reason <see cref="RuntimePanel.LoadTextData"/> loads
        /// it. An asset written without it exists, loads, and leaves a player
        /// build measuring every string as nothing — identical from the outside
        /// to a working one, which is why this is asserted rather than assumed.
        /// </remarks>
        [Test]
        public void TheCommittedPanelSettingsCarryICUData()
        {
            var committed = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsAsset.AssetPath);

            Assert.That(committed, Is.Not.Null,
                "No panel settings at " + PanelSettingsAsset.AssetPath
                + ". Run tools/build-panel-settings.ps1.");

            Assert.That(
                new SerializedObject(committed).FindProperty(PanelSettingsAsset.ICUDataField)
                    ?.objectReferenceValue,
                Is.Not.Null,
                PanelSettingsAsset.AssetPath + " carries no ICU data, which is the one thing it is "
                + "for. Run tools/build-panel-settings.ps1 and commit what it writes.");
        }

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
