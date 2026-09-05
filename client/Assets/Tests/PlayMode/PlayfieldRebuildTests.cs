using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// That building a playfield twice leaves one playfield.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Play mode is the whole point of this fixture.</b>
    /// <see cref="MatchRoot.Awake"/> builds as soon as the component exists,
    /// and in play mode that is the moment <c>AddComponent</c> returns — so
    /// every editor tool that adds the root and then calls
    /// <see cref="MatchRoot.Build(HexMap, TileSet, SceneryModels, DressingSettings, BoardDressing)"/>
    /// with the real tiles is building twice. In an edit-mode editor
    /// <c>Awake</c> does not run at all, so the same code builds once and the
    /// fault is invisible. An edit-mode test could not see this.
    /// </para>
    /// <para>
    /// <b>What it looked like when it was wrong.</b> Two floors at one height
    /// z-fight, which draws the board as a chequerboard of triangles in two
    /// materials, and two suns light it twice. It reached the committed chrome
    /// sheets and read as shadow acne. See #240.
    /// </para>
    /// <para>
    /// Counting only what is <em>active</em> is deliberate: a play-mode
    /// <c>Destroy</c> is deferred to the end of the frame, so the retired
    /// objects are still in the hierarchy while the test runs. Being off the
    /// screen now is the property that matters, and it is the one asserted.
    /// </para>
    /// </remarks>
    public sealed class PlayfieldRebuildTests : ViewTest
    {
        [Test]
        public void BuildingAgainReplacesThePlayfieldRatherThanAddingASecond()
        {
            MatchRoot root = Playfield();

            root.Build(Map());

            Assert.That(Standing<HexFloor>(root), Is.EqualTo(1), "floors");
            Assert.That(Standing<Light>(root), Is.EqualTo(1), "suns");
            Assert.That(Standing<OrbitCameraRig>(root), Is.EqualTo(1), "camera rigs");
        }

        [Test]
        public void ThePlayfieldLeftStandingIsTheOneBuiltLast()
        {
            MatchRoot root = Playfield();
            HexFloor first = root.Floor;

            Assume.That(first, Is.Not.Null, "Awake builds one before anything asks it to.");

            root.Build(Map());

            Assert.That(root.Floor, Is.Not.SameAs(first));
            Assert.That(root.Floor.gameObject.activeInHierarchy, Is.True);
            Assert.That(first == null || !first.gameObject.activeInHierarchy, Is.True, "the first floor is down");
        }

        /// <summary>
        /// The tile mesh survives a rebuild, because <see cref="BuildBoard"/>
        /// is handed it and outlives the call that made it.
        /// </summary>
        [Test]
        public void TheTileMeshIsTheSameOneAfterARebuild()
        {
            MatchRoot root = Playfield();
            Mesh first = root.TileMesh;

            root.Build(Map());

            Assert.That(root.TileMesh, Is.SameAs(first));
        }

        /// <summary>How many of a thing are drawn under the root right now.</summary>
        private static int Standing<T>(MatchRoot root)
            where T : Component =>
            root.GetComponentsInChildren<T>(includeInactive: false).Length;

        /// <summary>The shipped board, which is what the root builds by default.</summary>
        private static HexMap Map() => StreamingContent.ReadMap();
    }
}
