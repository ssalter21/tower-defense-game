using System.IO;
using System.Linq;
using NUnit.Framework;
using Tests.Fixtures;
using UnityEditor;
using UnityEngine;
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

            Assert.That(
                generated.Units.Select(u => u.UnitId),
                Is.EqualTo(chosen.Units.Select(u => u.UnitId)),
                GeneratedTestAssets.ManifestPath + " covers different units than ChosenArt does. "
                + "Run tools/build-test-assets.ps1 and commit what it writes.");

            foreach (UnitArt unit in chosen.Units)
            {
                Same(generated.ModelFor(unit.UnitId), unit.Model, "model for unit " + unit.UnitId);

                Assert.That(generated.ScaleFor(unit.UnitId), Is.EqualTo(unit.Scale),
                    GeneratedTestAssets.ManifestPath + " draws unit " + unit.UnitId
                    + " at a different size than ChosenArt does. Run tools/build-test-assets.ps1 "
                    + "and commit what it writes.");
            }

            Same(generated.CreepWalkClip, chosen.CreepWalkClip, nameof(chosen.CreepWalkClip));
            Same(generated.CreepDeathClip, chosen.CreepDeathClip, nameof(chosen.CreepDeathClip));
            Same(generated.BowModel, chosen.BowModel, nameof(chosen.BowModel));
            Same(generated.TowerIdleClip, chosen.TowerIdleClip, nameof(chosen.TowerIdleClip));
            Same(generated.TowerWindupClip, chosen.TowerWindupClip, nameof(chosen.TowerWindupClip));
            Same(generated.TowerBackswingClip, chosen.TowerBackswingClip, nameof(chosen.TowerBackswingClip));
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
