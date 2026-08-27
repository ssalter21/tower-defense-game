using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// Screen to hex: exact at every cell, from every angle the free camera can
    /// be put at, and honest about the rays that meet the board nowhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The round trip is the test, and it is the only one that could catch
    /// this.</b> Picking is <see cref="HexGeometry"/> inverted, so checking it
    /// against re-derived arithmetic would only check that two copies of the
    /// same algebra agree. Projecting the centre of every cell through the real
    /// camera and requiring the pick to name that cell again goes through the
    /// projection, the rig's transform and the odd-r conversion — every step a
    /// bug could live in.
    /// </para>
    /// <para>
    /// <b>Every angle means every angle.</b> The rig's pitch is unclamped and
    /// its pivot unbounded, so the sweep below includes looking almost straight
    /// down, looking up at the board from underneath, and three quarter turns of
    /// yaw. A camera that only picked correctly from the default view would have
    /// passed a single-angle test and failed the first time somebody dragged.
    /// </para>
    /// </remarks>
    public class HexPickingTests : ViewTest
    {
        /// <summary>
        /// Angles the sweep is run at: the default, three quarter turns, nearly
        /// overhead, and two from below the floor.
        /// </summary>
        /// <remarks>
        /// None of them is level. A camera in the ground plane looks along it,
        /// and what that picks has its own test below.
        /// </remarks>
        private static readonly Vector2[] Angles =
        {
            new Vector2(SceneFraming.CameraDefaultYawDegrees, SceneFraming.CameraDefaultPitchDegrees),
            new Vector2(90f, 20f),
            new Vector2(180f, 55f),
            new Vector2(270f, 75f),
            new Vector2(45f, 89f),
            new Vector2(137f, 210f),
            new Vector2(300f, 330f),
        };

        /// <summary>
        /// Project a cell's centre to the screen, pick it back, and get the same
        /// cell — unless something taller is standing in the way.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The exception is the board having tiers, and it is not a
        /// weakening.</b> This used to demand the cell back every time, which
        /// held while the board was flat. A raised tile genuinely hides the
        /// lower ground behind it, so a round trip through a cell's centre can
        /// legitimately come back as its taller neighbour; refusing that would
        /// be asserting that elevation does not occlude, which is the one thing
        /// drawing it is for.
        /// </para>
        /// <para>
        /// <b>What is still asserted exactly is the arithmetic.</b> A pick that
        /// lands on a cell at the SAME tier is a hex-rounding bug and fails
        /// here, which is what this test was always really about. Only a
        /// different tier is allowed to differ, and only because
        /// <see cref="HexPicking.TryPick"/> reaches a cell at another tier by
        /// crossing that tier's plane first — which is what being in front
        /// means.
        /// </para>
        /// </remarks>
        [Test]
        public void EveryCellPicksItselfBackUnlessSomethingTallerIsInTheWay()
        {
            MatchRoot root = Playfield();
            Camera camera = root.CameraRig.Camera;
            HexMap map = root.Map;

            foreach (Vector2 angle in Angles)
            {
                root.CameraRig.PointAt(angle.x, angle.y, root.CameraRig.FramedDistance);

                for (int row = 0; row < map.Height; row++)
                {
                    for (int column = 0; column < map.Width; column++)
                    {
                        Vector3 screen = camera.WorldToScreenPoint(
                            HexGeometry.ToWorld(column, row, map.LevelAt(column, row)));

                        // A cell behind the camera is not on the screen at all,
                        // and asking what is under a point that does not exist
                        // is not a question this class answers.
                        if (screen.z <= 0f)
                        {
                            continue;
                        }

                        Assert.That(
                            HexPicking.TryPick(camera, screen, map, out int picked, out int pickedRow),
                            Is.True,
                            $"cell {column},{row} at yaw {angle.x} pitch {angle.y} picked nothing");

                        if (picked == column && pickedRow == row)
                        {
                            continue;
                        }

                        Assert.That(
                            map.LevelAt(picked, pickedRow),
                            Is.Not.EqualTo(map.LevelAt(column, row)),
                            $"cell {column},{row} at yaw {angle.x} pitch {angle.y} picked "
                            + $"{picked},{pickedRow} on its own tier — nothing at the same height can "
                            + "be in front of it, so this is the hex arithmetic and not occlusion");
                    }
                }
            }
        }

        /// <summary>
        /// A point anywhere inside a hex picks that hex, not only its exact
        /// centre — which is what the cube rounding is for.
        /// </summary>
        [Test]
        public void APointInsideAHexPicksThatHex()
        {
            MatchRoot root = Playfield();
            HexMap map = root.Map;

            // Comfortably inside: the incircle is half the width across the
            // flats, and this is four tenths of the circumradius.
            const float Inside = 0.4f;

            for (int corner = 0; corner < Sim.Hex.DirectionCount; corner++)
            {
                Vector3 offset = HexGeometry.Corner(corner) * Inside;

                for (int row = 0; row < map.Height; row++)
                {
                    for (int column = 0; column < map.Width; column++)
                    {
                        Vector3 point = HexGeometry.ToWorld(column, row) + offset;

                        Assert.That(
                            HexPicking.TryCellAt(point, map, out int picked, out int pickedRow),
                            Is.True);

                        Assert.That((picked, pickedRow), Is.EqualTo((column, row)), $"corner {corner}");
                    }
                }
            }
        }

        [Test]
        public void ARayParallelToTheGroundMeetsItNowhere()
        {
            Assert.That(
                HexPicking.TryGroundPoint(new Ray(new Vector3(0f, 10f, 0f), Vector3.forward), out _),
                Is.False);
        }

        [Test]
        public void ARayPointingAwayFromTheGroundMeetsItNowhere()
        {
            Assert.That(
                HexPicking.TryGroundPoint(new Ray(new Vector3(0f, 10f, 0f), Vector3.up), out _),
                Is.False,
                "The plane is behind the camera, and behind is not in front.");
        }

        /// <summary>
        /// A camera lying in the ground plane looks along it and picks nothing.
        /// The degenerate case the free rig can actually be put in, rather than
        /// a hypothetical one.
        /// </summary>
        [Test]
        public void ALevelCameraPicksNothing()
        {
            MatchRoot root = Playfield();

            root.CameraRig.PointAt(SceneFraming.CameraDefaultYawDegrees, 0f, root.CameraRig.FramedDistance);

            Assert.That(root.CameraRig.Camera.transform.position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                HexPicking.TryPick(
                    root.CameraRig.Camera,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                    root.Map,
                    out _,
                    out _),
                Is.False);
        }

        [Test]
        public void GroundOffTheGridIsNotACell()
        {
            MatchRoot root = Playfield();

            Assert.That(
                HexPicking.TryCellAt(new Vector3(-40f, 0f, 12f), root.Map, out _, out _),
                Is.False,
                "Off the near corner of the grid.");

            Assert.That(
                HexPicking.TryCellAt(new Vector3(400f, 0f, -400f), root.Map, out _, out _),
                Is.False,
                "Far past the far corner.");
        }

        /// <summary>
        /// A point so far out that its axial coordinate would not fit in a hex
        /// is refused rather than rounded into an overflow.
        /// </summary>
        [Test]
        public void GroundBeyondTheCoordinateSpaceIsNotAHex()
        {
            Assert.That(HexPicking.TryHexAt(new Vector3(1e9f, 0f, 0f), out _), Is.False);
            Assert.That(HexPicking.TryHexAt(new Vector3(0f, 0f, -1e9f), out _), Is.False);
        }
    }
}
