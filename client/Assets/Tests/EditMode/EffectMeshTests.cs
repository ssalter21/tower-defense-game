using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// The generated disc that stops at the edge of the board.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Only the clipped disc is asserted here, and it is asserted because it
    /// is arithmetic rather than art.</b> Where a circle is cut is a fact about
    /// a rectangle and a radius that is either right or wrong; whether a cut
    /// circle is the right picture is <see cref="EffectLook"/>'s candidate and
    /// nobody's signature yet. The other three shapes in
    /// <see cref="EffectMeshes"/> are bars, and what would be worth asserting
    /// about a knife is that it reads as one, which no test can say.
    /// </para>
    /// <para>
    /// <b>Nothing here draws anything.</b> A mesh is vertices and triangles, so
    /// the questions are answered by measuring them — which needs no camera, no
    /// scene and no play mode, and is why this is an EditMode test at all.
    /// </para>
    /// </remarks>
    public class EffectMeshTests
    {
        /// <summary>
        /// A rectangle far larger than the disc, so nothing is cut off.
        /// </summary>
        private static Rect Everything => new Rect(-10f, -10f, 20f, 20f);

        [Test]
        public void TheGeneratedDiscIsAsRoundAsTheCylinderTheGameDraws()
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            try
            {
                Mesh primitive = cylinder.GetComponent<MeshFilter>().sharedMesh;

                Assert.That(
                    DirectionsAround(primitive),
                    Is.EqualTo(EffectMeshes.DiscSides),
                    "EffectMeshes.DiscSides is meant to be the tessellation of Unity's own cylinder, "
                    + "so that a candidate about where a circle stops is not also a candidate about "
                    + "how round it is. Unity's has moved.");
            }
            finally
            {
                Object.DestroyImmediate(cylinder);
            }
        }

        [Test]
        public void ADiscNothingCutsIsTheWholeCircle()
        {
            Mesh disc = EffectMeshes.ClippedDisc(EffectMeshes.DiscSides, 0.1f, Everything);

            Assert.That(DirectionsAround(disc), Is.EqualTo(EffectMeshes.DiscSides));

            Assert.That(
                Widest(disc),
                Is.EqualTo(EffectMeshes.OuterRadius).Within(0.0001f),
                "A disc reaches OuterRadius, so a caller scaling by a diameter gets the radius it meant.");
        }

        [Test]
        public void ADiscIsCutOffWhereTheRegionEnds()
        {
            // The keeping region ends a quarter of the way out, so the whole
            // right-hand half of the circle beyond that is gone.
            Mesh disc = EffectMeshes.ClippedDisc(
                EffectMeshes.DiscSides, 0.1f, new Rect(-10f, -10f, 10.25f, 20f));

            List<Vector3> corners = Corners(disc);

            Assert.That(corners, Is.Not.Empty, "Most of the circle is inside the region.");

            Assert.That(
                corners.Max(corner => corner.x),
                Is.EqualTo(0.25f).Within(0.0001f),
                "Nothing is drawn past the line the region ends at.");

            Assert.That(
                corners.Min(corner => corner.x),
                Is.EqualTo(-EffectMeshes.OuterRadius).Within(0.0001f),
                "And the side the line does not touch is left whole.");
        }

        [Test]
        public void ADiscWhollyOutsideTheRegionIsNotDrawnAtAll()
        {
            Mesh disc = EffectMeshes.ClippedDisc(
                EffectMeshes.DiscSides, 0.1f, new Rect(40f, 40f, 1f, 1f));

            Assert.That(
                disc.triangles,
                Is.Empty,
                "An emitter standing off the board has no part of its aura on it, and drawing "
                + "nothing is what stopping at the rim means there.");
        }

        [Test]
        public void ACutDiscIsStillSolidFromEverySide()
        {
            Mesh disc = EffectMeshes.ClippedDisc(
                EffectMeshes.DiscSides, 0.2f, new Rect(-10f, -10f, 10.1f, 20f));

            Assert.That(
                Corners(disc).Max(corner => corner.y),
                Is.EqualTo(0.1f).Within(0.0001f),
                "The thickness is in mesh units and is not scaled with the radius, so a caller "
                + "leaves the vertical axis at one.");

            // A top, a bottom and a wall all the way round: every edge of the
            // cut outline carries a quad, or the solid is open where it was cut
            // and the camera orbits into the hole.
            Assert.That(
                disc.normals.Any(normal => normal.y > 0.9f), Is.True, "There is a top.");

            Assert.That(
                disc.normals.Any(normal => normal.y < -0.9f), Is.True, "There is a bottom.");

            Assert.That(
                disc.normals.Any(normal => Mathf.Abs(normal.y) < 0.1f),
                Is.True,
                "And a wall standing between them.");
        }

        [Test]
        public void AWindingIsNotTakenOnTrust()
        {
            // Both faces and the wall, on a disc that has been cut, so every
            // kind of triangle the builder makes is in the one mesh.
            Mesh disc = EffectMeshes.ClippedDisc(
                EffectMeshes.DiscSides, 0.2f, new Rect(-10f, -10f, 10.15f, 20f));

            int[] triangles = disc.triangles;
            Vector3[] vertices = disc.vertices;
            Vector3[] normals = disc.normals;

            var backwards = 0;

            for (var corner = 0; corner < triangles.Length; corner += 3)
            {
                Vector3 a = vertices[triangles[corner]];
                Vector3 b = vertices[triangles[corner + 1]];
                Vector3 c = vertices[triangles[corner + 2]];

                Vector3 facing = Vector3.Cross(b - a, c - a);

                if (Vector3.Dot(facing, normals[triangles[corner]]) <= 0f)
                {
                    backwards++;
                }
            }

            Assert.That(
                backwards,
                Is.Zero,
                "A triangle is drawn from the side Cross(b - a, c - a) points at, so a face whose "
                + "winding disagrees with its own normal is culled from the only side anybody looks "
                + "at it from. This is not a cosmetic failure: rendered inside out, a translucent "
                + "disc does not look wrong, it is simply not there -- which reads as an aura that "
                + "never fired. Every face of this mesh came out backwards the first time, and the "
                + "render was what caught it.");
        }

        /// <summary>
        /// How many directions the outer rim of <paramref name="mesh"/> has
        /// corners in, which is how many sides the circle it approximates is
        /// drawn with.
        /// </summary>
        /// <remarks>
        /// Measured off the vertices rather than counted, because Unity's
        /// cylinder and this file's disc are built by different code and share
        /// nothing but the shape they are meant to be. A vertex is binned by
        /// its angle about y to a hundredth of a radian, which separates twenty
        /// evenly-spread directions comfortably and forgives the rounding of a
        /// mesh whose radius is not exactly a half.
        /// </remarks>
        private static int DirectionsAround(Mesh mesh)
        {
            var directions = new HashSet<int>();

            foreach (Vector3 vertex in mesh.vertices)
            {
                var flat = new Vector2(vertex.x, vertex.z);

                // Unity's cylinder carries the centre of each cap, which points
                // nowhere; so does anything a clip left on the axis.
                if (flat.magnitude < 0.4f)
                {
                    continue;
                }

                directions.Add(Mathf.RoundToInt(Mathf.Atan2(flat.y, flat.x) * 100f));
            }

            return directions.Count;
        }

        /// <summary>How far the furthest vertex stands from the axis.</summary>
        private static float Widest(Mesh mesh) =>
            mesh.vertices.Max(vertex => new Vector2(vertex.x, vertex.z).magnitude);

        /// <summary>The vertices, as a list that can be measured more than once.</summary>
        private static List<Vector3> Corners(Mesh mesh) => mesh.vertices.ToList();
    }
}
