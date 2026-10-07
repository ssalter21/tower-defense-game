using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The scene the game ships, opened the way the game opens it, with the real
    /// tile art rather than the blockout every other fixture draws with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> The board skin reads the KayKit tiles' vertices
    /// and UVs on the CPU. The editor lets any mesh be read; a player lets only a
    /// mesh imported with Read/Write. Every other fixture builds the board from
    /// <see cref="TileSet"/>'s generated blockout, which is always readable, so
    /// the suite stayed green in a player while the shipped build drew no road
    /// and a red ground, logging "Not allowed to access vertices" once per piece.
    /// </para>
    /// <para>
    /// The test framework fails a test on any error logged during it, which is
    /// the assertion that would have caught that; the explicit checks below say
    /// what the error broke.
    /// </para>
    /// </remarks>
    public sealed class ShippedSceneTests
    {
        private const string Scene = "Match";

        [UnityTest]
        public IEnumerator TheShippedSceneBuildsItsBoardWithoutAnError()
        {
            yield return SceneManager.LoadSceneAsync(Scene, LoadSceneMode.Additive);

            try
            {
                MatchRoot root = SceneManager.GetSceneByName(Scene)
                    .GetRootGameObjects()
                    .Select(host => host.GetComponentInChildren<MatchRoot>())
                    .FirstOrDefault(found => found != null);

                Assert.That(root, Is.Not.Null, "The shipped scene has no MatchRoot.");
                Assert.That(root.Floor, Is.Not.Null, "The shipped scene built no floor.");
                Assert.That(root.Floor.Tiles, Is.Not.Empty, "The shipped scene laid no road.");

                foreach (MeshRenderer tile in root.Floor.Tiles)
                {
                    Mesh mesh = tile.GetComponent<MeshFilter>().sharedMesh;

                    Assert.That(mesh, Is.Not.Null, tile.name + " has no mesh.");
                    Assert.That(mesh.isReadable, Is.True, mesh.name + " could not be reshaped in a player.");
                }

                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                SceneManager.UnloadSceneAsync(Scene);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheShippedSceneOpensOnTheMenuWithNoRunStarted()
        {
            yield return SceneManager.LoadSceneAsync(Scene, LoadSceneMode.Additive);

            try
            {
                MatchRoot root = SceneManager.GetSceneByName(Scene)
                    .GetRootGameObjects()
                    .Select(host => host.GetComponentInChildren<MatchRoot>())
                    .First(found => found != null);

                Assert.That(root.Loop, Is.Null);
                Assert.That(root.Menu, Is.Not.Null);
                Assert.That(root.Menu.IsShown, Is.True);
            }
            finally
            {
                SceneManager.UnloadSceneAsync(Scene);
            }

            yield return null;
        }
    }
}
