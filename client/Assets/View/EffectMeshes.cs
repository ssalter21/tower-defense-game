using System;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The shapes a row's shots and blasts are drawn with, generated in code: a
    /// ring in pieces, a burst of shards and a thrown knife, each a single mesh
    /// made of solid bars — and one shape that is not a shot at all, the disc
    /// cut to the board that <see cref="ClippedDisc"/> builds for a capture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bars, because a card is not allowed and nothing here needed more.</b>
    /// The camera orbits freely, so nothing in the match may turn to face it —
    /// <c>NothingInTheMatchTurnsToFaceTheCamera</c> and
    /// <c>EverythingDrawnIsRealGeometryLitByARealLight</c> hold that line, and
    /// Unity's default particles, line renderers and sprites all billboard. So
    /// a signature is real triangles lit by the same directional light
    /// everything else on the board is lit by, and it reads the same from every
    /// heading. <b>A particle system is allowed here as of 7 Sep 2026</b>, in
    /// <c>Mesh</c> render mode, which faces nothing; this file predates that
    /// and none of the three shapes left in it wanted one. The same constraint already picked
    /// <see cref="MatchDecorations"/>'s stretched box for a tracer and its
    /// sphere for a spark; these are what it picks once a shape stops being
    /// expressible as one primitive.
    /// </para>
    /// <para>
    /// <b>Every mesh that stands for a radius is built at an outer radius of
    /// <see cref="OuterRadius"/>.</b> That is a half, so a caller scales by a
    /// diameter and gets exactly the radius it asked for, which is the same
    /// arithmetic the bubble ring's cylinder already does. Nothing here knows
    /// what a hex is or how big a bubble was. <see cref="Knife"/> is the one
    /// shape that is not a radius — it is an object of a size somebody picked
    /// rather than a reach the simulation reported — so it is built one unit
    /// long instead, and its caller scales by the length it wants.
    /// </para>
    /// <para>
    /// <b>How far a shape stands off the plane it lies on is a distance in
    /// metres and is not scaled with the radius.</b> A caller scaling a flat
    /// shape leaves its vertical axis at one, so a band's thickness comes out
    /// of this file already in the units the board is in. <see cref="Burst"/>
    /// is the exception on the other side: it is as tall as it is wide because
    /// it stands for a radius in all three directions, so its caller scales it
    /// uniformly.
    /// </para>
    /// <para>
    /// <b>Five shapes stood here and are gone.</b> A ring, cracks, roots, a
    /// cage of arcs and a crown of upright shards were what nine auras were
    /// drawn as; every aura is one flat translucent circle now, which is a
    /// Unity cylinder and needs nothing generated. See
    /// <c>docs/decision-log.md</c>.
    /// </para>
    /// <para>
    /// <b><see cref="ClippedDisc"/> is the one thing here the game never
    /// draws.</b> The other three are shapes the shipped match puts on screen;
    /// that one exists so a capture can photograph an aura that stops at the
    /// edge of the board beside the shipped one that does not, which is a
    /// decision standing on nobody's signature. See issue #280, and
    /// <see cref="EffectLook"/> for why a candidate is drawn through the real
    /// match rather than mocked up.
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
        /// How many corners <see cref="ClippedDisc"/> draws a whole circle
        /// with. Twenty, which is what Unity's own cylinder primitive has, so a
        /// clipped circle differs from the shipped one in where it stops and in
        /// nothing else.
        /// </summary>
        /// <remarks>
        /// <b>The number is asserted rather than trusted.</b> Unity documents
        /// no tessellation for its primitives and is free to change it, and a
        /// generated disc even slightly smoother than the cylinder beside it
        /// would make a candidate about <i>where a circle ends</i> into a
        /// candidate about how round it is as well — two questions in one
        /// picture, which is the species this project keeps deleting.
        /// <c>EffectMeshTests</c> stands a primitive cylinder up and counts.
        /// </remarks>
        public const int DiscSides = 20;

        /// <summary>
        /// The same ring with every other bar left out, so it reads as a band
        /// that has come apart rather than as one that is whole.
        /// </summary>
        /// <param name="sides">How many bars a whole circle would take; half of them are drawn.</param>
        /// <param name="band">How wide the band is, radially, in mesh units.</param>
        /// <param name="thickness">How tall the band stands off the plane.</param>
        /// <remarks>
        /// The gaps are as wide as the pieces, which is the plainest split
        /// there is: any other share would be a proportion somebody chose, and
        /// what this shape has to say is that the ring is broken and not by how
        /// much.
        /// </remarks>
        public static Mesh BrokenRing(int sides, float band, float thickness)
        {
            var builder = new Bars();

            for (var side = 0; side < sides; side += 2)
            {
                builder.Add(
                    OnCircle(side, sides, OuterRadius),
                    OnCircle(side + 1, sides, OuterRadius),
                    band * 0.5f,
                    thickness * 0.5f);
            }

            return builder.ToMesh("EffectBrokenRing");
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
        /// A knife lying along +Z, one unit from the butt of its grip to its
        /// point and centred on its own origin: a blade, a crossguard across
        /// it, and a grip behind that.
        /// </summary>
        /// <param name="bladeWidth">How wide the blade is, in mesh units.</param>
        /// <param name="guardSpan">How far the crossguard reaches, end to end.</param>
        /// <param name="thickness">How deep all three bars are.</param>
        /// <remarks>
        /// <para>
        /// Three bars, because that is the fewest that reads as a knife rather
        /// than as a stick: a bar on its own is what the ordinary tracer
        /// already is, and what makes this shape a knife is the crossguard
        /// interrupting it and the grip being narrower than the blade.
        /// </para>
        /// <para>
        /// <b>Where the three meet along the knife is fixed here and how thick
        /// they are is the caller's.</b> The split points are what a knife
        /// <i>is</i> — a blade taking most of the length with a short grip
        /// behind a guard — and reproportioning them would be drawing a
        /// different weapon; the widths are how big it is drawn, which is the
        /// kind of number this project keeps in <see cref="MatchTuning"/>.
        /// </para>
        /// <para>
        /// Along +Z so that a caller aims it with
        /// <c>Quaternion.LookRotation</c>, which is the same arithmetic the
        /// stretched box of a tracer is already pointed with.
        /// </para>
        /// </remarks>
        public static Mesh Knife(float bladeWidth, float guardSpan, float thickness)
        {
            // Measured from the butt at -0.5 to the point at +0.5.
            const float GuardAt = -0.2f;
            const float GripEnd = -0.14f;

            var builder = new Bars();

            builder.Add(
                new Vector3(0f, 0f, GripEnd),
                new Vector3(0f, 0f, 0.5f),
                bladeWidth * 0.5f,
                thickness * 0.5f);

            builder.Add(
                new Vector3(-guardSpan * 0.5f, 0f, GuardAt),
                new Vector3(guardSpan * 0.5f, 0f, GuardAt),
                thickness * 0.5f,
                thickness * 0.5f);

            builder.Add(
                new Vector3(0f, 0f, -0.5f),
                new Vector3(0f, 0f, GuardAt),
                thickness * 0.5f,
                thickness * 0.5f);

            return builder.ToMesh("EffectKnife");
        }

        /// <summary>
        /// A solid disc of <see cref="OuterRadius"/> lying in the XZ plane, cut
        /// to whatever part of it falls inside <paramref name="keep"/>.
        /// </summary>
        /// <param name="sides">How many corners the whole circle is drawn with.</param>
        /// <param name="thickness">How tall it stands off the plane, in mesh units.</param>
        /// <param name="keep">
        /// The region to keep, in the same mesh units the disc is built in — so
        /// a caller with a board and a centre hands in the board expressed
        /// relative to that centre and divided by the diameter it is about to
        /// scale by. Its <c>y</c> is the board's z, which is what
        /// <see cref="Rect"/> is in a plan view.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>This is a candidate, and the shipped circle is not built here.</b>
        /// Every aura the game draws is a Unity cylinder scaled flat, which
        /// needs nothing generated and reaches exactly as far as the bubble did
        /// — over the rim of the board and out into the background when the
        /// emitter stands near one. Whether that is right is nobody's signature
        /// yet, so this exists to photograph the alternative beside it and the
        /// shipped path is untouched. See issue #280.
        /// </para>
        /// <para>
        /// <b>The cut is a polygon clip and not a shader.</b> This project has
        /// no shader of its own, and every effect in it is real triangles lit
        /// by the one directional light — so a circle that stops at a line is a
        /// circle with the far side of that line taken off it. Four half-planes
        /// applied in turn, which is exact for the rectangle a board is and
        /// leaves a convex outline, so the top and bottom are fans and the wall
        /// is one quad per edge.
        /// </para>
        /// <para>
        /// <b>A disc entirely outside the region comes back as an empty
        /// mesh</b>, rather than as a throw or as a whole one. An emitter can
        /// stand far enough off the board that no part of its aura is on it,
        /// and drawing nothing is what stopping at the rim means there.
        /// </para>
        /// </remarks>
        public static Mesh ClippedDisc(int sides, float thickness, Rect keep)
        {
            var circle = new List<Vector3>(sides);

            for (var side = 0; side < sides; side++)
            {
                circle.Add(OnCircle(side, sides, OuterRadius));
            }

            List<Vector3> kept = Clipped(circle, keep);
            var builder = new Slab();

            if (kept.Count >= 3)
            {
                builder.Add(kept, thickness);
            }

            return builder.ToMesh("EffectClippedDisc");
        }

        /// <summary>
        /// <paramref name="polygon"/> with everything outside
        /// <paramref name="keep"/> taken off it.
        /// </summary>
        /// <remarks>
        /// Sutherland–Hodgman: clip against one edge of the rectangle at a
        /// time and carry the outline through all four. Each pass keeps the
        /// corners on the inside and adds a crossing point wherever the outline
        /// goes over the line, so a convex outline stays convex and the count
        /// changes only by the corners the line cut off.
        /// </remarks>
        private static List<Vector3> Clipped(List<Vector3> polygon, Rect keep)
        {
            List<Vector3> kept = polygon;

            kept = Inside(kept, point => point.x - keep.xMin, keep.xMin, onX: true);
            kept = Inside(kept, point => keep.xMax - point.x, keep.xMax, onX: true);
            kept = Inside(kept, point => point.z - keep.yMin, keep.yMin, onX: false);
            kept = Inside(kept, point => keep.yMax - point.z, keep.yMax, onX: false);

            return kept;
        }

        /// <summary>
        /// One pass of the clip: the part of <paramref name="polygon"/> lying
        /// on the keeping side of a single axis-aligned line.
        /// </summary>
        /// <param name="depth">
        /// How far a point is on the keeping side of the line. At or above zero
        /// is kept.
        /// </param>
        /// <param name="at">Where the line stands, in the axis it is stated in.</param>
        /// <param name="onX">
        /// True when the line is stated in x, so a crossing point is placed in
        /// x and keeps its z.
        /// </param>
        private static List<Vector3> Inside(
            List<Vector3> polygon, Func<Vector3, float> depth, float at, bool onX)
        {
            var kept = new List<Vector3>(polygon.Count + 4);

            for (var corner = 0; corner < polygon.Count; corner++)
            {
                Vector3 current = polygon[corner];
                Vector3 previous = polygon[((corner + polygon.Count) - 1) % polygon.Count];

                float here = depth(current);
                float there = depth(previous);

                if (here >= 0f)
                {
                    if (there < 0f)
                    {
                        kept.Add(Crossing(previous, current, at, onX));
                    }

                    kept.Add(current);
                }
                else if (there >= 0f)
                {
                    kept.Add(Crossing(previous, current, at, onX));
                }
            }

            return kept;
        }

        /// <summary>
        /// Where the segment from <paramref name="from"/> to
        /// <paramref name="to"/> crosses the line at <paramref name="at"/>.
        /// </summary>
        /// <remarks>
        /// The crossed coordinate is then written on exactly rather than
        /// nearly, so two discs cut by the same rim share that edge instead of
        /// missing each other by a rounding error.
        /// </remarks>
        private static Vector3 Crossing(Vector3 from, Vector3 to, float at, bool onX)
        {
            float start = onX ? from.x : from.z;
            float span = (onX ? to.x : to.z) - start;

            // A segment that does not move in the axis the line is stated in
            // cannot cross it. Nothing reaches here with one, since a crossing
            // is only asked for when the two ends fall on opposite sides -- but
            // dividing by it would put a NaN in the mesh, and a NaN draws as
            // nothing and reports as nothing.
            float fraction = Mathf.Approximately(span, 0f) ? 0f : (at - start) / span;

            Vector3 crossed = Vector3.Lerp(from, to, fraction);

            if (onX)
            {
                crossed.x = at;
            }
            else
            {
                crossed.z = at;
            }

            return crossed;
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
        /// A mesh under construction from one convex outline: a flat solid with
        /// a top, a bottom and a wall round the edge.
        /// </summary>
        /// <remarks>
        /// <b><see cref="Bars"/>'s sibling, and separate for the same reason
        /// <see cref="Bars"/> is separate from a cylinder.</b> That one appends
        /// boxes and every shape it builds is made of them; this one takes an
        /// outline that is not a box and cannot be expressed as any number of
        /// them. Both give every face its own vertices, so the shading stays
        /// flat.
        /// </remarks>
        private sealed class Slab
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();

            private readonly List<Vector3> _normals = new List<Vector3>();

            private readonly List<int> _triangles = new List<int>();

            /// <summary>
            /// Adds the solid standing on <paramref name="outline"/>,
            /// <paramref name="thickness"/> tall and centred on the plane the
            /// outline lies in.
            /// </summary>
            public void Add(List<Vector3> outline, float thickness)
            {
                float half = thickness * 0.5f;

                Fan(outline, half, Vector3.up, wound: true);
                Fan(outline, -half, Vector3.down, wound: false);

                for (var corner = 0; corner < outline.Count; corner++)
                {
                    Vector3 from = outline[corner];
                    Vector3 to = outline[(corner + 1) % outline.Count];
                    Vector3 along = to - from;

                    if (along.sqrMagnitude < 1e-10f)
                    {
                        continue;
                    }

                    // Which way a wall faces comes off the edge it stands on
                    // rather than off the middle, so a cut edge faces the way it
                    // was cut instead of away from the centre of a circle it is
                    // no longer the whole of.
                    Vector3 outward = Vector3.Cross(Vector3.up, along.normalized).normalized;

                    Face(
                        from - (Vector3.up * half),
                        from + (Vector3.up * half),
                        to + (Vector3.up * half),
                        to - (Vector3.up * half),
                        outward);
                }
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
            /// One flat face over the whole outline, lifted
            /// <paramref name="height"/> off the plane.
            /// </summary>
            /// <remarks>
            /// A fan, which is correct because the clip leaves the outline
            /// convex. Hand it a re-entrant one and it fills across the notch.
            /// </remarks>
            private void Fan(List<Vector3> outline, float height, Vector3 normal, bool wound)
            {
                int first = _vertices.Count;

                foreach (Vector3 corner in outline)
                {
                    _vertices.Add(corner + (Vector3.up * height));
                    _normals.Add(normal);
                }

                for (var corner = 1; corner < outline.Count - 1; corner++)
                {
                    // The two faces are the same outline seen from opposite
                    // sides, so one of them is wound the other way round or it
                    // is culled from the only side anybody looks at it from.
                    //
                    // WHICH WAY ROUND IS NOT OBVIOUS AND WAS WRONG FIRST. The
                    // outline runs anticlockwise in the XZ plane, and in Unity's
                    // left-handed frame that makes Cross(b - a, c - a) point
                    // DOWN, not up -- so the face taking the outline in its own
                    // order is the bottom one. Rendered inside out, a
                    // translucent disc does not look wrong: it disappears, and
                    // the frame reads as an aura that did not fire.
                    // AWindingIsNotTakenOnTrust holds this now.
                    if (wound)
                    {
                        _triangles.Add(first);
                        _triangles.Add(first + corner + 1);
                        _triangles.Add(first + corner);
                    }
                    else
                    {
                        _triangles.Add(first);
                        _triangles.Add(first + corner);
                        _triangles.Add(first + corner + 1);
                    }
                }
            }

            /// <summary>
            /// One four-cornered face as two triangles, wound so its front is
            /// the way <paramref name="normal"/> points.
            /// </summary>
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
