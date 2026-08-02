using System.IO;
using NUnit.Framework;
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
