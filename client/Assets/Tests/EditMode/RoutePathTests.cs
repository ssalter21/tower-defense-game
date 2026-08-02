using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// The view's whole position arithmetic: turning a distance along the
    /// corridor into somewhere to stand.
    /// </summary>
    /// <remarks>
    /// These run without an engine object, a scene or an asset, because
    /// <see cref="RoutePath"/> and <see cref="SimUnits"/> are pure. That is
    /// worth having: the arithmetic that decides where everything in the match
    /// is drawn is checkable in milliseconds and cannot be broken by an import.
    /// </remarks>
    public class RoutePathTests
    {
        /// <summary>
        /// A three-cell straight corridor, so a hand-computed answer is
        /// possible.
        /// </summary>
        private const string StraightMap =
            "....\n"
            + ".S#E\n"
            + "....\n";

        private static RoutePath Straight() => RoutePath.For(HexMap.Parse(StraightMap));

        /// <summary>
        /// The one assumption <see cref="SimUnits.MetresPerHex"/> rests on: all
        /// six neighbours of a hex are the same distance away, so one route
        /// step is one constant and not a per-step length.
        /// </summary>
        /// <remarks>
        /// A property of hexes rather than a coincidence of these numbers — but
        /// asserted anyway, because it is exactly the quiet assumption that
        /// survives until somebody changes the grid, and then every creep in the
        /// match is subtly in the wrong place with nothing failing.
        /// </remarks>
        [Test]
        public void EverySixNeighbourIsOneHexAway()
        {
            Vector3 centre = HexGeometry.ToWorld(new Hex(3, 3));

            (int q, int r)[] neighbours =
            {
                (1, 0), (-1, 0), (0, 1), (0, -1), (1, -1), (-1, 1),
            };

            foreach ((int q, int r) in neighbours)
            {
                Vector3 neighbour = HexGeometry.ToWorld(new Hex(3 + q, 3 + r));

                Assert.That(
                    Vector3.Distance(centre, neighbour),
                    Is.EqualTo(SimUnits.MetresPerHex).Within(1e-3f),
                    $"neighbour ({q},{r}) is not one hex away, so one route step is not one constant");
            }
        }

        [Test]
        public void FixedPointConvertsToMetres()
        {
            Assert.That(SimUnits.ToFloat(Fix64.One), Is.EqualTo(1f).Within(1e-6f));
            Assert.That(SimUnits.ToFloat(Fix64.FromRatio(1, 2)), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(
                SimUnits.Metres(Fix64.FromInt(3)),
                Is.EqualTo(3f * SimUnits.MetresPerHex).Within(1e-4f));
        }

        [Test]
        public void TheEndsOfTheRouteAreTheEntranceAndTheExit()
        {
            RoutePath route = Straight();

            Assert.That(route.PointAt(0f, 0f), Is.EqualTo(route.Entrance));
            Assert.That(route.PointAt(route.StepCount, 0f), Is.EqualTo(route.Exit));
        }

        [Test]
        public void HalfwayAlongAStepIsHalfwayBetweenTwoCells()
        {
            RoutePath route = Straight();

            Vector3 expected = (route.Step(0) + route.Step(1)) * 0.5f;

            Assert.That(Vector3.Distance(route.PointAt(0.5f, 0f), expected), Is.LessThan(1e-4f));
        }

        /// <summary>
        /// The property everything else rests on: same distance in, same point
        /// out. Nothing accumulates, so nothing can drift.
        /// </summary>
        [Test]
        public void TheSameDistanceAlwaysGivesTheSamePoint()
        {
            RoutePath route = Straight();

            Vector3 first = route.PointAt(1.37f, 0.3f);

            // Walk somewhere else and come back. A path that remembered
            // anything would answer differently the second time.
            route.PointAt(0.1f, 0f);
            route.PointAt(2.0f, -0.3f);

            Assert.That(route.PointAt(1.37f, 0.3f), Is.EqualTo(first));
        }

        [Test]
        public void LateralOffsetMovesAcrossTheCorridorAndNotAlongIt()
        {
            RoutePath route = Straight();

            Vector3 centre = route.PointAt(1f, 0f);
            Vector3 offset = route.PointAt(1f, 0.3f);

            Vector3 across = offset - centre;

            Assert.That(
                across.magnitude,
                Is.EqualTo(0.3f * SimUnits.MetresPerHex).Within(1e-3f),
                "a lateral offset is measured in hexes, like every other distance");

            Assert.That(
                Vector3.Dot(across.normalized, route.TangentAt(1f)),
                Is.EqualTo(0f).Within(1e-3f),
                "a lateral offset that had a component along the corridor would move a creep forwards");
        }

        [Test]
        public void TwoCreepsOnOppositeOffsetsStandApart()
        {
            RoutePath route = Straight();

            // The simulation hands out 0, +0.3 and -0.3 in turn, which is what
            // makes an overtake something a person can watch rather than a
            // claim about ids.
            Vector3 left = route.PointAt(1.5f, 0.3f);
            Vector3 right = route.PointAt(1.5f, -0.3f);

            Assert.That(
                Vector3.Distance(left, right),
                Is.EqualTo(0.6f * SimUnits.MetresPerHex).Within(1e-3f));
        }

        /// <summary>
        /// Distances past either end are clamped, not refused. Interpolating
        /// between two snapshots can ask for a distance a hair past the exit on
        /// the last tick of a match, and throwing there would turn a
        /// sub-millimetre rounding detail into a crash on the final frame.
        /// </summary>
        [Test]
        public void DistancesPastTheEndsAreClamped()
        {
            RoutePath route = Straight();

            Assert.That(route.PointAt(-5f, 0f), Is.EqualTo(route.Entrance));
            Assert.That(route.PointAt(route.StepCount + 5f, 0f), Is.EqualTo(route.Exit));
        }

        [Test]
        public void FacingLooksTheWayTheCorridorRuns()
        {
            RoutePath route = Straight();

            Vector3 forward = route.FacingAt(0.5f) * Vector3.forward;

            Assert.That(Vector3.Dot(forward, route.TangentAt(0.5f)), Is.EqualTo(1f).Within(1e-3f));
        }

        /// <summary>
        /// The committed map, end to end — so the arithmetic is checked against
        /// the corridor the match is actually fought on and not only against a
        /// straight line.
        /// </summary>
        [Test]
        public void TheCommittedMapWalksWithoutAGap()
        {
            RoutePath route = RoutePath.For(StreamingContent.ReadMap());

            Assert.That(route.StepCount, Is.GreaterThan(0));

            Vector3 previous = route.PointAt(0f, 0f);

            for (float distance = 0.25f; distance <= route.StepCount; distance += 0.25f)
            {
                Vector3 point = route.PointAt(distance, 0f);
                float step = Vector3.Distance(previous, point);

                Assert.That(
                    step,
                    Is.EqualTo(0.25f * SimUnits.MetresPerHex).Within(1e-2f),
                    $"the corridor jumps at distance {distance} — a quarter step should always be a "
                    + "quarter of a hex, and a gap here would show up as a creep teleporting at a corner");

                previous = point;
            }
        }
    }
}
