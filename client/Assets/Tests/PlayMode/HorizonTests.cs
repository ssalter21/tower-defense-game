using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The world the board sits in: that there is one, that it is under the
    /// board rather than through it, and that its edge can never be seen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>None of this is about whether it looks nice.</b> That is judged by
    /// looking, in <c>docs/prototypes/scenery/</c>. What is asserted here is the
    /// handful of things that would make it look broken rather than plain — a
    /// plain cutting through the lowest tiles, an edge showing at the top of the
    /// frame, or a horizon left behind on the scene after the board it belonged
    /// to was torn down.
    /// </para>
    /// <para>
    /// <b>The last one is the one that bites.</b> The sky and the fog are
    /// scene-wide settings, not properties of an object, so a horizon that does
    /// not clean up after itself leaves the previous board's air over the next
    /// board — and in a test run, over every fixture that comes after it.
    /// </para>
    /// </remarks>
    public class HorizonTests : ViewTest
    {
        [Test]
        public void TheLandSitsUnderTheLowestTileRatherThanThroughIt()
        {
            MatchRoot root = Built(out HexMap map);
            Horizon horizon = root.Horizon;

            Assert.That(horizon, Is.Not.Null, "A built playfield has a horizon.");

            float lowest = float.MaxValue;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    lowest = Mathf.Min(lowest, map.LevelAt(column, row) * HexGeometry.LevelStep);
                }
            }

            Assert.That(
                horizon.LandHeight,
                Is.LessThan(lowest),
                "The plain is at or above the lowest tile's face, so it cuts through the board.");

            // And not so far under that the board is a plate hanging over a
            // gap. The rim drop is what decides this, and it is the number the
            // cliff columns are stacked to as well -- the two have to agree or
            // there is daylight under the edge of the world.
            DressingSettings dressing = DressingSettings.Default;

            Assert.That(
                horizon.LandHeight,
                Is.EqualTo(lowest - dressing.RimDrop).Within(0.001f),
                "The plain is not at the depth the board's own rim falls to.");
        }

        [Test]
        public void TheLandReachesPastTheBoardAndStaysInsideTheFarPlane()
        {
            MatchRoot root = Built(out _);

            Bounds board = root.Floor.WorldBounds;
            float across = new Vector2(board.size.x, board.size.z).magnitude;

            Assert.That(
                root.Horizon.Radius,
                Is.GreaterThan(across),
                "The plain does not reach past the board it is meant to surround.");

            // The camera dollies out to twice the framed distance, and anything
            // past the far plane is not drawn -- which would put a hole in the
            // world with the sky showing through it.
            float furthest = (root.CameraRig.FramedDistance * SceneFraming.CameraMaxDistanceFactor)
                + root.Horizon.Radius;

            Assert.That(
                furthest,
                Is.LessThan(SceneFraming.CameraFarClip),
                "The plain's far side is outside the camera's far plane at the outermost dolly stop.");
        }

        [Test]
        public void TheHazeClosesBeforeTheLandEndsSoTheEdgeIsNeverSeen()
        {
            MatchRoot root = Built(out _);

            Assert.That(RenderSettings.fog, Is.True, "There is no haze, so the plain ends in a line.");

            Assert.That(
                RenderSettings.fogEndDistance,
                Is.LessThanOrEqualTo(root.Horizon.Radius),
                "The haze closes further out than the plain reaches, so the plain's edge is visible.");

            Assert.That(
                RenderSettings.fogStartDistance,
                Is.LessThan(RenderSettings.fogEndDistance),
                "The haze begins further out than it ends.");
        }

        /// <summary>
        /// The camera clears to the sky where there is one. Worth asserting
        /// because the failure is silent: a camera left on its flat colour
        /// draws a board against a slab of grey and nothing anywhere reports a
        /// problem.
        /// </summary>
        [Test]
        public void TheCameraClearsToTheSkyWhenThereIsASkyToClearTo()
        {
            MatchRoot root = Built(out _);

            if (root.Horizon.Sky == null)
            {
                Assert.That(
                    root.CameraRig.Camera.clearFlags,
                    Is.EqualTo(CameraClearFlags.SolidColor),
                    "There is no sky, so the camera should be back on its flat colour.");

                Assert.Ignore(
                    "No " + SkyMaterial.ShaderName + " shader in this project, so there is no sky to "
                    + "clear to. The board still draws.");
            }

            Assert.That(
                root.CameraRig.Camera.clearFlags,
                Is.EqualTo(CameraClearFlags.Skybox),
                "There is a sky and the camera is not clearing to it.");

            Assert.That(
                RenderSettings.skybox,
                Is.SameAs(root.Horizon.Sky),
                "The scene's sky is not the one this horizon built.");
        }

        /// <summary>
        /// A horizon takes its air back down with it, so the next board is not
        /// drawn through the last one's fog.
        /// </summary>
        [Test]
        public void AHorizonTakesTheSkyAndTheHazeAwayWithIt()
        {
            var host = new GameObject("Torn down");

            try
            {
                Horizon.Build(
                    host.transform,
                    StreamingContent.ReadMap(),
                    new Bounds(Vector3.zero, new Vector3(38f, 2f, 22f)),
                    DressingSettings.Default);

                Assert.That(RenderSettings.fog, Is.True, "The horizon did not put any haze up.");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            Assert.That(RenderSettings.fog, Is.False, "The haze outlived the horizon that raised it.");
            Assert.That(RenderSettings.skybox, Is.Null, "The sky outlived the horizon that raised it.");
        }

        private MatchRoot Built(out HexMap map)
        {
            map = StreamingContent.ReadMap();

            MatchRoot root = Playfield();
            root.Build(map);

            return root;
        }
    }
}
