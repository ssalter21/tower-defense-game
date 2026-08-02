using System;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The corridor as a polyline in world space: what turns a distance along
    /// the path into somewhere to stand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the whole of the view's position arithmetic.</b> The
    /// simulation never computes a point in a plane — a creep is a distance
    /// along the route plus a lateral offset, and a projectile is a countdown
    /// and a reference. Free 2D is kept out of the simulation permanently, and
    /// the cost of that is one class over here that knows where the corridor
    /// goes. This is that class, and it is the only one.
    /// </para>
    /// <para>
    /// <b>Built from the simulation's own route.</b> <see cref="HexMap.Route"/>
    /// is the corridor traced from the entrance to the exit, asserted on load
    /// to be one hex wide and never branching. This class walks it and nothing
    /// else — there is no second opinion here about where the corridor goes,
    /// which is the same reason <see cref="HexFloor"/> takes a parsed map
    /// rather than reading the grid itself.
    /// </para>
    /// <para>
    /// <b>Everything here is a pure function of its arguments.</b> Nothing
    /// accumulates and nothing is remembered between calls, so asking for the
    /// same distance twice gives the same point, and asking for a decreasing
    /// sequence of distances walks backwards. That is what makes scrubbing work
    /// at all, and it is a property this class has by having no fields that
    /// change rather than by being careful.
    /// </para>
    /// </remarks>
    public sealed class RoutePath
    {
        private readonly Vector3[] _points;

        private RoutePath(Vector3[] points)
        {
            _points = points;
        }

        /// <summary>How many route steps there are. The exit is at this distance.</summary>
        public int StepCount => _points.Length - 1;

        /// <summary>Where the entrance is.</summary>
        public Vector3 Entrance => _points[0];

        /// <summary>Where the exit is.</summary>
        public Vector3 Exit => _points[_points.Length - 1];

        /// <summary>
        /// The world centre of one route cell, by its index along the corridor.
        /// </summary>
        public Vector3 Step(int index) => _points[index];

        /// <summary>
        /// The corridor of <paramref name="map"/>, in world space.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is null.</exception>
        public static RoutePath For(HexMap map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            var points = new Vector3[map.Route.Count];

            for (int index = 0; index < points.Length; index++)
            {
                points[index] = HexGeometry.ToWorld(map.Route[index]);
            }

            return new RoutePath(points);
        }

        /// <summary>
        /// Where something standing <paramref name="distance"/> route steps
        /// along the corridor, and <paramref name="lateral"/> hexes to the side
        /// of its centre line, is.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Distances outside the corridor are clamped rather than refused. A
        /// creep is spawned at zero and removed at the exit, so the simulation
        /// never asks for one — but the view interpolates between two snapshots,
        /// and rounding at the very last tick can ask for a distance a hair past
        /// the end. Throwing there would turn a sub-millimetre arithmetic
        /// detail into a crash on the final frame of every match.
        /// </para>
        /// <para>
        /// The lateral offset is applied perpendicular to the segment the point
        /// lies on, in the ground plane. It is what keeps two creeps at the same
        /// distance from occupying the same spot, which is what makes an
        /// overtake something a person can watch rather than a claim about ids.
        /// </para>
        /// </remarks>
        public Vector3 PointAt(float distance, float lateral)
        {
            ResolveSegment(distance, out int from, out float fraction);

            Vector3 centre = Vector3.Lerp(_points[from], _points[from + 1], fraction);

            if (lateral == 0f)
            {
                return centre;
            }

            Vector3 across = Vector3.Cross(Vector3.up, SegmentDirection(from));

            return centre + (across * (lateral * SimUnits.MetresPerHex));
        }

        /// <summary>
        /// Which way the corridor runs at <paramref name="distance"/>, as a
        /// unit vector in the ground plane.
        /// </summary>
        public Vector3 TangentAt(float distance)
        {
            ResolveSegment(distance, out int from, out _);

            return SegmentDirection(from);
        }

        /// <summary>
        /// Which way something walking the corridor at
        /// <paramref name="distance"/> is facing.
        /// </summary>
        /// <remarks>
        /// Facing is derived from the route rather than from where the thing
        /// was last frame. Deriving it from movement would mean remembering a
        /// previous position — view-side state that a scrub would immediately
        /// disagree with, and which would leave a creep facing backwards for one
        /// frame every time the corridor turned.
        /// </remarks>
        public Quaternion FacingAt(float distance) =>
            Quaternion.LookRotation(TangentAt(distance), Vector3.up);

        /// <summary>
        /// Splits a distance into the segment it lies on and how far along that
        /// segment it is.
        /// </summary>
        private void ResolveSegment(float distance, out int from, out float fraction)
        {
            if (distance <= 0f)
            {
                from = 0;
                fraction = 0f;

                return;
            }

            if (distance >= StepCount)
            {
                // The last segment, at its far end -- rather than the segment
                // that starts at the exit, which does not exist.
                from = StepCount - 1;
                fraction = 1f;

                return;
            }

            from = Mathf.FloorToInt(distance);
            fraction = distance - from;
        }

        /// <summary>
        /// The unit direction of one segment. Segments are hex-centre to
        /// hex-centre and so are never zero length, which is asserted by the
        /// map's own corridor check long before this runs.
        /// </summary>
        private Vector3 SegmentDirection(int from) =>
            (_points[from + 1] - _points[from]).normalized;
    }
}
