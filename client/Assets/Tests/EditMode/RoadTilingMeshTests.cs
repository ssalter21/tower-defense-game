using NUnit.Framework;
using Sim;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The edges <see cref="RoadTiling"/> says each piece's road meets, checked
    /// against the model it will actually be drawn with.
    ///
    /// <b>This exists because the first version of that table was wrong and
    /// nothing noticed.</b> It was typed from a probe of the pack's glTF export,
    /// which is right handed where Unity is left handed, so every entry arrived
    /// mirrored. The straight survived — a road across opposite edges is the one
    /// shape a mirroring cannot disturb — and everything else was laid down
    /// turned: curves bent the wrong way out of every corner, dead ends capped
    /// the corridor pointing back down it, and the ramps climbed away from the
    /// tier they were meant to reach. Every test passed, because every test
    /// compared the floor to the same table the floor was built from.
    ///
    /// So this one does not read the table twice. It measures the geometry.
    ///
    /// <b>How a road is told from a field.</b> The pack does not paint its roads
    /// on: a road is inset geometry, a shallow tray about five centimetres below
    /// the tile's walkable face. So the question "is there road at this rim" is
    /// answered by standing at the rim, looking down, and asking how far away
    /// the first surface is. That is also why an earlier attempt at this from
    /// texture coordinates read grass everywhere — it sampled the top face,
    /// which is six triangles at one flat UV and says nothing.
    /// </summary>
    public class RoadTilingMeshTests
    {
        /// <summary>
        /// How far out towards a rim to stand, as a fraction of the distance to
        /// it. Far enough out to be unambiguously that edge's business, far
        /// enough in to be clear of the bevel the pack puts on every rim.
        /// </summary>
        private const float Probe = 0.85f;

        /// <summary>
        /// How deep the road tray sits below the walkable face, in metres. The
        /// pack's number rather than ours, halved into a threshold below, so a
        /// pack that changed it fails loudly rather than silently reclassifying
        /// every tile as field.
        /// </summary>
        private const float RoadInset = 0.05f;

        private static readonly TilePiece[] FlatPieces =
        {
            TilePiece.Straight,
            TilePiece.Curve,
            TilePiece.Hairpin,
            TilePiece.DeadEnd,
        };

        [Test]
        public void EveryPieceMeetsTheEdgesTheTableClaims([ValueSource(nameof(FlatPieces))] TilePiece piece)
        {
            Mesh mesh = MatchSceneBuilder.TileMesh(piece);

            Assert.That(mesh, Is.Not.Null, piece + " has no mesh; the scene builder's bindings are stale.");

            Assert.That(
                Describe(RoadEdges(mesh)),
                Is.EqualTo(Describe(RoadTiling.EdgesOf(piece))),
                piece + " is authored with its road on different edges than RoadTiling says, so every "
                + piece + " on the board is laid down turned.");
        }

        [Test]
        public void GroundHasNoRoadOnIt()
        {
            Mesh mesh = MatchSceneBuilder.TileMesh(TilePiece.Ground);

            Assert.That(
                Describe(RoadEdges(mesh)),
                Is.EqualTo(Describe(0)),
                "The ground tile has a road on it, which would draw a path across cells the map calls field.");
        }

        /// <summary>
        /// The ramp climbs towards the edge the table calls its high side, by
        /// exactly one tier, from a low end that stands where an ordinary tile
        /// stands.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All three of those matter. Climb towards the wrong edge and every
        /// hill is entered from the top; climb the wrong distance and the slope
        /// stops short of the tier it is joining; start at the wrong height and
        /// the whole run is sunk or floating, because the floor stands a ramp on
        /// the lower of the two cells and trusts its face to meet its
        /// neighbour's.
        /// </para>
        /// <para>
        /// <b>Which way it leans is measured, how far it climbs is not.</b> The
        /// slope is not a plane � it eases off towards the top, so the two rims
        /// are at different points on the curve and the difference between them
        /// at any one probe is not the rise. The rise is read off the bounding
        /// box instead, against the straight's, so the metre of body every tile
        /// hangs below its face cancels rather than being assumed.
        /// </para>
        /// </remarks>
        [Test]
        public void TheRampClimbsOneTierTowardsItsHighEdge()
        {
            Mesh ramp = MatchSceneBuilder.TileMesh(TilePiece.StraightRamp);
            Mesh flat = MatchSceneBuilder.TileMesh(TilePiece.Straight);

            int high = RoadTiling.RampHighEdge;
            int low = (high + (Hex.DirectionCount / 2)) % Hex.DirectionCount;

            Assert.That(
                SurfaceHeight(ramp, high) - SurfaceHeight(ramp, low),
                Is.GreaterThan(HexGeometry.LevelStep * 0.5f),
                "The ramp does not lean towards edge " + high + ", so every hill is entered from its top.");

            Assert.That(
                ramp.bounds.min.y,
                Is.EqualTo(flat.bounds.min.y).Within(0.01f),
                "The ramp's low end does not stand where a flat tile stands, so the run onto it steps.");

            Assert.That(
                ramp.bounds.max.y - flat.bounds.max.y,
                Is.EqualTo(HexGeometry.LevelStep).Within(0.01f),
                "The ramp does not climb exactly one tier, so either the pack's rise is not LevelStep "
                + "or this is not the piece we think it is.");
        }

        /// <summary>
        /// Which rims have road under them, as the six-bit set
        /// <see cref="RoadTiling.CorridorEdges"/> speaks in.
        /// </summary>
        private static int RoadEdges(Mesh mesh)
        {
            int edges = 0;

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                if (SurfaceHeight(mesh, direction) < -(RoadInset * 0.5f))
                {
                    edges |= 1 << direction;
                }
            }

            return edges;
        }

        /// <summary>
        /// How high the tile's upper surface is just inside one rim. The topmost
        /// face over that point, so the tile's underside — which every vertical
        /// ray also meets — is never mistaken for the one walked on.
        /// </summary>
        private static float SurfaceHeight(Mesh mesh, int direction)
        {
            Vector3 at = Outward(direction) * (HexGeometry.AcrossFlats * 0.5f * Probe);
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            float top = float.MinValue;

            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector3 a = vertices[triangles[index]];
                Vector3 b = vertices[triangles[index + 1]];
                Vector3 c = vertices[triangles[index + 2]];

                if (!Covers(at, a, b, c, out float height))
                {
                    continue;
                }

                top = Mathf.Max(top, height);
            }

            Assert.That(
                top,
                Is.GreaterThan(float.MinValue),
                "No surface at all over the rim at direction " + direction + ".");

            return top;
        }

        /// <summary>
        /// Whether a triangle is over a point, and at what height. Vertical
        /// faces — the tile's sides — cover nothing, which is what the normal
        /// test rejects.
        /// </summary>
        private static bool Covers(Vector3 at, Vector3 a, Vector3 b, Vector3 c, out float height)
        {
            height = 0f;

            Vector3 normal = Vector3.Cross(b - a, c - a);

            if (Mathf.Abs(normal.y) < 1e-6f)
            {
                return false;
            }

            float denominator = ((b.z - c.z) * (a.x - c.x)) + ((c.x - b.x) * (a.z - c.z));

            if (Mathf.Abs(denominator) < 1e-9f)
            {
                return false;
            }

            float u = (((b.z - c.z) * (at.x - c.x)) + ((c.x - b.x) * (at.z - c.z))) / denominator;
            float v = (((c.z - a.z) * (at.x - c.x)) + ((a.x - c.x) * (at.z - c.z))) / denominator;
            float w = 1f - u - v;

            if (u < -1e-4f || v < -1e-4f || w < -1e-4f)
            {
                return false;
            }

            height = a.y - ((((at.x - a.x) * normal.x) + ((at.z - a.z) * normal.z)) / normal.y);

            return true;
        }

        /// <summary>
        /// Which way one of the simulation's six directions points, in metres.
        /// Taken from <see cref="HexGeometry"/> rather than written out, so the
        /// probe and the floor cannot disagree about where east is.
        /// </summary>
        private static Vector3 Outward(int direction)
        {
            var here = new Hex(0, 0);

            return (HexGeometry.ToWorld(here.Neighbour(direction)) - HexGeometry.ToWorld(here)).normalized;
        }

        /// <summary>
        /// A bit set as the direction names, so a failure reads "E,SW" against
        /// "W,SE" rather than "17" against "40".
        /// </summary>
        private static string Describe(int edges)
        {
            string[] names = { "E", "NE", "NW", "W", "SW", "SE" };
            var written = new System.Text.StringBuilder();

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                if ((edges & (1 << direction)) == 0)
                {
                    continue;
                }

                if (written.Length > 0)
                {
                    written.Append(',');
                }

                written.Append(names[direction]);
            }

            return written.Length == 0 ? "(none)" : written.ToString();
        }
    }
}
