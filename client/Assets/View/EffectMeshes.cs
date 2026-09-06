using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The three shapes a capstone's signature is drawn with, generated in
    /// code: a ring, a set of cracks running out from a centre, and a burst of
    /// shards. Each one is a single mesh made of solid bars.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bars, because a particle system is not available here and a card is
    /// not allowed.</b> This project has no <c>ParticleSystem</c> anywhere and
    /// two play-mode tests keep it that way —
    /// <c>NothingInTheMatchTurnsToFaceTheCamera</c> and
    /// <c>EverythingDrawnIsRealGeometryLitByARealLight</c> — because the camera
    /// orbits freely and Unity's default particles, line renderers and sprites
    /// all billboard. So a signature is real triangles lit by the same
    /// directional light everything else on the board is lit by, and it reads
    /// the same from every heading. The same constraint already picked
    /// <see cref="MatchDecorations"/>'s stretched box for a tracer and its
    /// sphere for a spark; these are what it picks once a shape stops being
    /// expressible as one primitive.
    /// </para>
    /// <para>
    /// <b>Every mesh is built at an outer radius of
    /// <see cref="OuterRadius"/>.</b> That is a half, so a caller scales by a
    /// diameter and gets exactly the radius it asked for, which is the same
    /// arithmetic the bubble ring's cylinder already does. Nothing here knows
    /// what a hex is or how big a bubble was.
    /// </para>
    /// <para>
    /// <b>Each bar is six four-cornered faces with its own vertices, so the
    /// shading is flat.</b> Sharing the eight corners between faces and letting
    /// <c>RecalculateNormals</c> average them rounds every edge off, which on a
    /// bar a few centimetres thick reads as a smear rather than as a solid.
    /// </para>
    /// </remarks>
    public static class EffectMeshes
    {
        /// <summary>
        /// How far the outside of every mesh here sits from its own origin, in
        /// mesh units. A half, so a caller scaling by a diameter gets the
        /// radius it meant.
        /// </summary>
        public const float OuterRadius = 0.5f;

        /// <summary>
        /// A flat ring lying in the XZ plane: <paramref name="sides"/> bars laid
        /// end to end round a circle of <see cref="OuterRadius"/>.
        /// </summary>
        /// <param name="sides">How many bars the circle is made of.</param>
        /// <param name="band">How wide the band is, radially, in mesh units.</param>
        /// <param name="thickness">How tall the band stands off the plane.</param>
        public static Mesh Ring(int sides, float band, float thickness)
        {
            var builder = new Bars();

            for (var side = 0; side < sides; side++)
            {
                builder.Add(
                    OnCircle(side, sides, OuterRadius),
                    OnCircle(side + 1, sides, OuterRadius),
                    band * 0.5f,
                    thickness * 0.5f);
            }

            return builder.ToMesh("EffectRing");
        }

        /// <summary>
        /// Cracks running out from the middle: <paramref name="spokes"/> bars
        /// lying in the XZ plane, each from <paramref name="inner"/> out to
        /// <see cref="OuterRadius"/>.
        /// </summary>
        public static Mesh Cracks(int spokes, float inner, float width, float thickness)
        {
            var builder = new Bars();

            for (var spoke = 0; spoke < spokes; spoke++)
            {
                Vector3 along = OnCircle(spoke, spokes, 1f);

                builder.Add(along * inner, along * OuterRadius, width * 0.5f, thickness * 0.5f);
            }

            return builder.ToMesh("EffectCracks");
        }

        /// <summary>
        /// A burst: <paramref name="shards"/> bars leaving the middle in
        /// directions spread over the upper half of a sphere, each reaching
        /// <see cref="OuterRadius"/>.
        /// </summary>
        /// <remarks>
        /// The directions are a sunflower spiral rather than rings of equal
        /// latitude — <paramref name="shards"/> points spread evenly over the
        /// dome by construction, at whatever count a caller asks for, where
        /// rings would need a count that divides neatly and would leave visible
        /// seams where the camera looks down one.
        /// </remarks>
        public static Mesh Burst(int shards, float width)
        {
            var builder = new Bars();

            for (var shard = 0; shard < shards; shard++)
            {
                builder.Add(Vector3.zero, OverTheDome(shard, shards) * OuterRadius, width * 0.5f, width * 0.5f);
            }

            return builder.ToMesh("EffectBurst");
        }

        /// <summary>
        /// The point <paramref name="step"/> steps of <paramref name="steps"/>
        /// round a circle of <paramref name="radius"/> in the XZ plane.
        /// </summary>
        private static Vector3 OnCircle(int step, int steps, float radius)
        {
            float angle = 2f * Mathf.PI * step / steps;

            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        /// <summary>
        /// One of <paramref name="count"/> directions spread over the dome,
        /// never straight up.
        /// </summary>
        /// <remarks>
        /// The elevation comes from <c>asin</c> of an evenly-spaced height,
        /// which is what makes equal counts cover equal area; the half-step
        /// keeps the last one off the pole, where a bar has no unambiguous
        /// sideways direction to be thick in.
        /// </remarks>
        private static Vector3 OverTheDome(int index, int count)
        {
            const float GoldenAngle = 2.399963f;

            float height = (index + 0.5f) / count;
            float ring = Mathf.Sqrt(1f - (height * height));
            float around = index * GoldenAngle;

            return new Vector3(Mathf.Cos(around) * ring, height, Mathf.Sin(around) * ring);
        }

        /// <summary>
        /// A mesh under construction: solid bars appended one at a time, each
        /// one six faces of four corners.
        /// </summary>
        private sealed class Bars
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();

            private readonly List<Vector3> _normals = new List<Vector3>();

            private readonly List<int> _triangles = new List<int>();

            /// <summary>
            /// Adds a box running from <paramref name="from"/> to
            /// <paramref name="to"/>, <paramref name="halfWidth"/> to either
            /// side of that line and <paramref name="halfHeight"/> above and
            /// below it.
            /// </summary>
            /// <remarks>
            /// The cross-section's frame is derived from the bar's own
            /// direction rather than being world-aligned, so a shard leaving
            /// the middle of a burst at forty degrees is as thick as one lying
            /// flat. A bar of no length is skipped rather than producing a
            /// degenerate frame.
            /// </remarks>
            public void Add(Vector3 from, Vector3 to, float halfWidth, float halfHeight)
            {
                Vector3 along = to - from;

                if (along.sqrMagnitude < 1e-8f)
                {
                    return;
                }

                along.Normalize();

                Vector3 side = Vector3.Cross(along, Vector3.up);

                // Straight up has no sideways direction of its own, so the
                // frame falls back to a world axis. Nothing here asks for one,
                // and a silent zero-length cross product would collapse the bar
                // to a line.
                side = side.sqrMagnitude < 1e-6f
                    ? Vector3.right
                    : side.normalized;

                Vector3 up = Vector3.Cross(side, along);

                Vector3 outward = side * halfWidth;
                Vector3 upward = up * halfHeight;

                Vector3 a = from - outward - upward;
                Vector3 b = from + outward - upward;
                Vector3 c = from + outward + upward;
                Vector3 d = from - outward + upward;

                Vector3 e = to - outward - upward;
                Vector3 f = to + outward - upward;
                Vector3 g = to + outward + upward;
                Vector3 h = to - outward + upward;

                Face(a, b, c, d, -along);
                Face(h, g, f, e, along);
                Face(a, e, f, b, -up);
                Face(d, c, g, h, up);
                Face(b, f, g, c, side);
                Face(a, d, h, e, -side);
            }

            /// <summary>The mesh, named so a pooled object can be told apart.</summary>
            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };

                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();

                return mesh;
            }

            /// <summary>
            /// One four-cornered face as two triangles, with
            /// <paramref name="normal"/> on all four corners.
            /// </summary>
            /// <remarks>
            /// <b>The corners have to be given in the order that makes
            /// <c>Cross(b - a, c - a)</c> point the way
            /// <paramref name="normal"/> does</b>, because that is the
            /// direction Unity draws a triangle's front from — the same
            /// arithmetic <c>Mesh.RecalculateNormals</c> uses. Get it backwards
            /// and the face is still there, still lit and still in the bounds,
            /// and is culled from the one side anybody looks at it from.
            /// </remarks>
            private void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                int first = _vertices.Count;

                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _vertices.Add(d);

                for (var corner = 0; corner < 4; corner++)
                {
                    _normals.Add(normal);
                }

                _triangles.Add(first);
                _triangles.Add(first + 1);
                _triangles.Add(first + 2);

                _triangles.Add(first);
                _triangles.Add(first + 2);
                _triangles.Add(first + 3);
            }
        }
    }
}
