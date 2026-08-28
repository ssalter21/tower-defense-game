using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Which cell of the authored grid a point on the screen is over.
    /// <see cref="HexGeometry"/> backwards, and the only implementation of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ray against a plane, and no collider anywhere.</b> The board is one
    /// flat hexagonal grid at <c>y = 0</c>, so where a screen ray meets it is
    /// arithmetic rather than a physics query. That is not a micro-optimisation:
    /// a collider per tile would be a second description of where a cell is,
    /// living in engine objects that a test cannot compare against
    /// <see cref="HexGeometry"/> — and the first thing it would disagree with is
    /// the tile mesh, which is generated from the same six corners.
    /// </para>
    /// <para>
    /// <b>Every camera angle, including the ones nobody expected.</b> The rig is
    /// free — unclamped pitch, unbounded pivot, see
    /// <see cref="OrbitCameraRig"/> — so a ray may cross the plane going down,
    /// going up from underneath, or never: it may run parallel to the ground, or
    /// away from it, and both are answered with <c>false</c> rather than with a
    /// point at infinity. Nothing here reads the camera's yaw or pitch, so there
    /// is no angle to have a special case for.
    /// </para>
    /// <para>
    /// <b>The answer is in the coordinates the content files speak.</b>
    /// <c>content/map.txt</c> is a character grid indexed by column and row, and
    /// a <c>place</c> names a cell the same way, so that is what comes back —
    /// converted through the simulation's own <see cref="Hex.ToOddRowOffset"/>
    /// rather than by a second copy of the odd-r arithmetic.
    /// </para>
    /// <para>
    /// <b>Nothing in this file can reach the simulation.</b> It reads a camera,
    /// a screen point and a map, and returns two integers. Where somebody is
    /// pointing decides which command the player composes and never what a tick
    /// does; the command is what reaches the run. See ADR-0039 and ADR-0051.
    /// </para>
    /// </remarks>
    public static class HexPicking
    {
        /// <summary>
        /// How steeply a ray has to cross the ground plane to be treated as
        /// crossing it at all, as a fraction of its length.
        /// </summary>
        /// <remarks>
        /// A ray exactly parallel to the plane meets it nowhere; a ray a
        /// millionth off parallel meets it a million lengths away, which is a
        /// hex somewhere past the far clip plane and is not what anybody meant
        /// by pointing at it. Both are refused by the same comparison.
        /// </remarks>
        private const float Grazing = 1e-6f;

        /// <summary>
        /// The largest axial coordinate that fits in a <see cref="Hex"/>. A
        /// ground point further out than this is off any board there could be,
        /// and rounding it to an integer would overflow before anybody got to
        /// ask whether it was on the map.
        /// </summary>
        private const float Reachable = short.MaxValue;

        /// <summary>
        /// Where <paramref name="ray"/> crosses the ground plane, if it does.
        /// </summary>
        /// <remarks>
        /// The plane is two-sided: a camera under the floor looking up gets the
        /// point above it, because the rig allows that view and refusing to pick
        /// from it would be a limit on the camera imposed from over here.
        /// </remarks>
        public static bool TryGroundPoint(Ray ray, out Vector3 point) =>
            TryGroundPoint(ray, 0f, out point);

        /// <summary>
        /// Where <paramref name="ray"/> crosses the horizontal plane at
        /// <paramref name="height"/> metres, if it does.
        /// </summary>
        /// <remarks>
        /// A board with tiers has one of these planes per tier, and which of
        /// them a click meant is not a question this can answer on its own —
        /// see <see cref="TryPick"/>, which asks the map.
        /// </remarks>
        public static bool TryGroundPoint(Ray ray, float height, out Vector3 point)
        {
            point = default;

            float slope = ray.direction.y;

            if (!(Mathf.Abs(slope) > Grazing))
            {
                return false;
            }

            float along = (height - ray.origin.y) / slope;

            // Behind the camera is not in front of it. Without this, aiming at
            // the sky picks the hex the camera is standing over.
            if (!(along > 0f))
            {
                return false;
            }

            point = ray.GetPoint(along);

            return true;
        }

        /// <summary>
        /// Which hex a point in the ground plane is inside.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The fractional axial coordinate is <see cref="HexGeometry.ToWorld"/>
        /// solved for <c>q</c> and <c>r</c>: <c>r</c> falls out of the row pitch
        /// on its own, and <c>q</c> is what is left of the width once the
        /// odd-row half-step <c>r</c> contributes is taken back off.
        /// </para>
        /// <para>
        /// Rounding it is the standard cube round, and it has to be that rather
        /// than three independent roundings: the three cube coordinates sum to
        /// zero and rounding each alone breaks that, which puts the answer in a
        /// neighbouring hex along the diagonals. So the two that moved least are
        /// kept and the third is recomputed from them.
        /// </para>
        /// </remarks>
        public static bool TryHexAt(Vector3 groundPoint, out Hex hex)
        {
            hex = default;

            float r = -groundPoint.z / HexGeometry.RowPitch;
            float q = (groundPoint.x / HexGeometry.AcrossFlats) - (r * 0.5f);

            // Written as a negated comparison so a NaN — which a degenerate
            // camera can produce — is refused rather than rounded.
            if (!(Mathf.Abs(q) <= Reachable) || !(Mathf.Abs(r) <= Reachable))
            {
                return false;
            }

            float y = -q - r;

            int roundedQ = Mathf.RoundToInt(q);
            int roundedY = Mathf.RoundToInt(y);
            int roundedR = Mathf.RoundToInt(r);

            float movedQ = Mathf.Abs(roundedQ - q);
            float movedY = Mathf.Abs(roundedY - y);
            float movedR = Mathf.Abs(roundedR - r);

            if (movedQ > movedY && movedQ > movedR)
            {
                roundedQ = -roundedY - roundedR;
            }
            else if (movedY > movedR)
            {
                roundedY = -roundedQ - roundedR;
            }
            else
            {
                roundedR = -roundedQ - roundedY;
            }

            hex = new Hex(roundedQ, roundedR);

            return true;
        }

        /// <summary>
        /// Which cell of <paramref name="map"/> a point in the ground plane is
        /// on. False where it is off the grid.
        /// </summary>
        public static bool TryCellAt(Vector3 groundPoint, HexMap map, out int column, out int row)
        {
            column = 0;
            row = 0;

            if (map is null || !TryHexAt(groundPoint, out Hex hex))
            {
                return false;
            }

            Hex.ToOddRowOffset(hex, out column, out row);

            return column >= 0 && column < map.Width && row >= 0 && row < map.Height;
        }

        /// <summary>
        /// Which cell of <paramref name="map"/> is under
        /// <paramref name="screenPoint"/>, seen through
        /// <paramref name="camera"/>. False where the pointer is off the board,
        /// off the grid, or not pointing at the ground at all.
        /// </summary>
        /// <param name="camera">The one camera. Any angle, any distance.</param>
        /// <param name="screenPoint">Pixels, origin at the bottom left, as the input system reports them.</param>
        /// <param name="map">The playfield, for its extent.</param>
        /// <param name="column">The column of the authored grid, where true.</param>
        /// <param name="row">The row of the authored grid, where true.</param>
        public static bool TryPick(Camera camera, Vector2 screenPoint, HexMap map, out int column, out int row)
        {
            column = 0;
            row = 0;

            if (camera == null)
            {
                return false;
            }

            if (map is null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPoint);

            // One horizontal plane per tier, tried in the order this ray crosses
            // them. A cell is only the answer if it actually stands on the tier
            // whose plane the ray crossed to reach it — otherwise the ray passed
            // over a raised cell and through the ground it was hiding, and the
            // hit belongs to whichever tier owns it.
            //
            // The order is the whole of the occlusion rule: whichever plane the
            // ray meets first is the one whose tile is in front. A ray heading
            // downwards meets the top tier first, so a click on a hillside lands
            // on the hill and not on the field beyond it. A ray heading upwards
            // — the rig allows a camera under the floor — meets the bottom tier
            // first, and there the low tile is the one in the way. Scanning top
            // down regardless would pick through the floor from underneath.
            bool descending = ray.direction.y < 0f;

            for (int step = 0; step < HexMap.LevelCount; step++)
            {
                int level = descending ? HexMap.LevelCount - 1 - step : step;

                if (!TryGroundPoint(ray, level * HexGeometry.LevelStep, out Vector3 ground))
                {
                    continue;
                }

                if (!TryCellAt(ground, map, out int at, out int on))
                {
                    continue;
                }

                if (map.LevelAt(at, on) != level)
                {
                    continue;
                }

                column = at;
                row = on;

                return true;
            }

            return false;
        }
    }
}
