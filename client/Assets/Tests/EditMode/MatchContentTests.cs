using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The two generated things a match needs before it can be drawn: the
    /// content beside the player, and the art wired into the scene.
    /// </summary>
    /// <remarks>
    /// Both are the same failure if they go wrong, and it is a bad one — the
    /// project runs, the floor appears, and the match silently does not start.
    /// Neither throws, because neither is missing anything the engine cares
    /// about. So both are checked here rather than left to be noticed.
    /// </remarks>
    public class MatchContentTests
    {
        /// <summary>
        /// The repository root, from the Unity project directory.
        /// </summary>
        private static string RepositoryRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>
        /// Every file a match reads has an up-to-date copy beside the player.
        /// </summary>
        /// <remarks>
        /// The map alone was enough to draw the floor. A match also needs the
        /// type table, the defense and the wave, and a file that is read but
        /// does not ship presents as an empty playfield in a build that worked
        /// perfectly in the editor.
        /// </remarks>
        [Test]
        public void EveryContentFileAMatchNeedsIsShipped()
        {
            foreach (string fileName in StreamingContent.MatchFileNames)
            {
                string authored = Path.Combine(RepositoryRoot(), "content", fileName);
                string shipped = StreamingContent.PathOf(fileName);

                Assert.That(File.Exists(authored), Is.True, $"nothing authored at {authored}");

                Assert.That(File.Exists(shipped), Is.True,
                    $"no streaming copy at {shipped}. Run tools/sync-streaming-content.ps1.");

                Assert.That(
                    File.ReadAllBytes(shipped),
                    Is.EqualTo(File.ReadAllBytes(authored)),
                    $"the streaming copy of {fileName} has drifted from the authored one. "
                    + "Run tools/sync-streaming-content.ps1 and commit what it writes.");
            }
        }

        /// <summary>
        /// The whole match parses out of the shipped copies — not just the map.
        /// </summary>
        [Test]
        public void TheShippedContentParsesIntoAMatch()
        {
            Assert.That(StreamingContent.HasEveryMatchFile(), Is.True);

            Sim.UnitTypeTable types = StreamingContent.ReadUnitTypes();
            Sim.TowerLayout defense = StreamingContent.ReadDefense(types);
            Sim.WaveScript wave = StreamingContent.ReadWave(types);

            Assert.That(defense.Count, Is.EqualTo(6));
            Assert.That(wave.TotalUnits, Is.GreaterThan(0));

            // Constructing it is the assertion: every load-time invariant in the
            // simulation is an unconditional throw.
            var match = new Sim.Match(
                StreamingContent.ReadMap(), StreamingContent.ReadRuleset(), defense, wave, 1);

            Assert.That(match.Tick, Is.EqualTo(0));
        }

        /// <summary>
        /// The shipped record is one this build will actually replay.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Reading a record and being allowed to replay it are different gates,
        /// and only the second is about this build: the simulation version, the
        /// content hash and the map hash all have to be the ones in front of it.
        /// A record that ships and is then refused presents as a player that
        /// draws its floor and never starts a match — the same silent shape the
        /// rest of this fixture exists for.
        /// </para>
        /// <para>
        /// It matters more than it used to, because the record is now the whole
        /// of what the player plays: the map it draws, the towers, the wave and
        /// the seed. Every tick number in <c>docs/sit-down.md</c> is a tick of
        /// this run.
        /// </para>
        /// </remarks>
        [Test]
        public void TheShippedRecordPassesTheReplayGate()
        {
            Sim.ReplayBundle record = StreamingContent.ReadRecordedMatch();

            // Getting through the call is the assertion: the gate is three
            // unconditional throws and what is wanted is that none of them
            // fires. Asserting the returned match is on tick zero would be
            // asserting about a freshly constructed object, which is true
            // whatever the gate did — a check that cannot fail.
            Assert.DoesNotThrow(
                () => record.Replay(StreamingContent.ReadUnitTypes(), StreamingContent.ReadRuleset()));

            // The floor is drawn from the record's inlined grid, so this is the
            // check that the playfield a human looks at is still the one
            // somebody authored rather than one only the record remembers.
            Assert.That(record.Map.MapHash, Is.EqualTo(StreamingContent.ReadMap().MapHash),
                "the record was recorded on a different playfield than the one authored in content/map.txt");
        }

        /// <summary>
        /// The committed scene carries every art reference, so a player built
        /// from it draws a match rather than a floor.
        /// </summary>
        [Test]
        public void TheGeneratedSceneHasEveryArtReferenceWiredUp()
        {
            UnityEngine.SceneManagement.Scene scene =
                EditorSceneManager.OpenScene(MatchSceneBuilder.ScenePath, OpenSceneMode.Additive);

            try
            {
                MatchRoot root = null;

                foreach (GameObject rootObject in scene.GetRootGameObjects())
                {
                    MatchRoot found = rootObject.GetComponent<MatchRoot>();

                    if (found != null) root = found;
                }

                Assert.That(root, Is.Not.Null, $"no MatchRoot in {MatchSceneBuilder.ScenePath}");

                Assert.That(root.Art.IsComplete, Is.True,
                    "the scene's art is not fully wired. The scene is generated: run "
                    + "tools/build-match-scene.ps1 and commit what it writes.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }
    }
}
