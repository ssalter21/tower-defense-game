using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What each tower of a defense can reach, expressed once and for all as
    /// intervals of distance along the route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is where the two dimensions stop.</b> A tower stands on a hex and
    /// has a range in hexes, which is a circle; the route is a line. Intersecting
    /// them is done exactly once, here, at load, in whole-hex integer arithmetic
    /// -- and what comes out is a handful of intervals on that line. From then on
    /// "is this creep in range" is <c>start &lt;= d &lt;= end</c> and nothing in
    /// the tick loop has ever heard of a position in a plane.
    /// </para>
    /// <para>
    /// <b>A tower gets a list of intervals rather than one</b>, because the
    /// corridor doubles back on itself: a tower in the middle of the map can
    /// easily be within range of two separate stretches of route with an
    /// out-of-range stretch between them. Insisting on a single interval would
    /// not simplify anything -- it would just mean the middle of the map is
    /// unbuildable for a reason no player could see. The intervals are disjoint
    /// and ascending, which is asserted, so the range test stays a scan of a
    /// couple of comparisons.
    /// </para>
    /// <para>
    /// Distance is measured in route steps: the entrance is 0, the exit is
    /// <see cref="RouteLength"/>, and one whole unit of distance is one hex of
    /// corridor. An interval covering route cells <c>a</c> through <c>b</c> is
    /// the closed interval <c>[a, b]</c>, so a creep between two cells is in
    /// range exactly when both of the cells it lies between are.
    /// </para>
    /// <para>
    /// Three faults are load errors rather than surprises later: a tower off the
    /// edge of the grid, a tower standing inside the corridor, and a tower that
    /// cannot reach the route at all. The last one is the interesting one -- it
    /// is the shape of a typo in a coordinate, and without this check it would
    /// present as a tower that simply never fires, which looks exactly like a
    /// balance problem.
    /// </para>
    /// </remarks>
    public sealed class TowerCoverage
    {
        /// <summary>Thousandths of a hex per hex. Ranges are authored in milli-hexes.</summary>
        private const int MilliHexPerHex = 1000;

        private readonly int[] _firstInterval;

        private readonly int[] _intervalCount;

        /// <summary>
        /// Interval bounds as raw Q32.32, not as <see cref="Fix64"/>.
        /// </summary>
        /// <remarks>
        /// This is the simulation's second-hottest inner loop -- every tower
        /// asks about every creep on every tick it is hunting -- and the
        /// committed configuration is Debug, where each comparison through
        /// <see cref="Fix64"/> is an operator call and two property calls that
        /// nothing is going to inline. The public accessors still hand out
        /// <see cref="Fix64"/>; only the comparison is done on the underlying
        /// integers, and they are the same integers.
        /// </remarks>
        private readonly long[] _start;

        private readonly long[] _end;

        /// <summary>
        /// Per tower, the first and last point of route it reaches at all --
        /// the bounding interval around its list of intervals, so the common
        /// answer of "nowhere near" costs two comparisons.
        /// </summary>
        private readonly long[] _spanStart;

        private readonly long[] _spanEnd;

        private TowerCoverage(
            int[] firstInterval,
            int[] intervalCount,
            long[] start,
            long[] end,
            Fix64 routeLength)
        {
            _firstInterval = firstInterval;
            _intervalCount = intervalCount;
            _start = start;
            _end = end;
            RouteLength = routeLength;

            _spanStart = new long[firstInterval.Length];
            _spanEnd = new long[firstInterval.Length];

            for (int tower = 0; tower < firstInterval.Length; tower++)
            {
                _spanStart[tower] = start[firstInterval[tower]];
                _spanEnd[tower] = end[firstInterval[tower] + intervalCount[tower] - 1];
            }
        }

        /// <summary>How many towers this covers, in the layout's canonical order.</summary>
        public int TowerCount => _firstInterval.Length;

        /// <summary>
        /// The distance from the entrance to the exit, in hexes. A creep that
        /// reaches this has left the map.
        /// </summary>
        public Fix64 RouteLength { get; }

        /// <summary>Intersects a defense with a map's route. Every fault here throws.</summary>
        public static TowerCoverage For(HexMap map, TowerLayout layout)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (layout is null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            var firstInterval = new int[layout.Count];
            var intervalCount = new int[layout.Count];
            var start = new List<long>();
            var end = new List<long>();

            for (int index = 0; index < layout.Count; index++)
            {
                PlacedTower tower = layout.Towers[index];

                RefuseOffMap(map, tower);
                RefuseInsideCorridor(map, tower);

                firstInterval[index] = start.Count;
                intervalCount[index] = Intersect(map, tower, start, end);

                if (intervalCount[index] == 0)
                {
                    throw new ContentException(
                        "defense",
                        tower.Line,
                        "places "
                        + tower.ToString()
                        + ", which cannot reach any part of the route: its range is "
                        + tower.Type.RangeMilliHex.ToString(CultureInfo.InvariantCulture)
                        + " thousandths of a hex and the nearest corridor cell is further than that. A "
                        + "tower that can never fire is what a mistyped coordinate looks like, and it "
                        + "would otherwise present as a balance problem.");
                }
            }

            return new TowerCoverage(
                firstInterval,
                intervalCount,
                start.ToArray(),
                end.ToArray(),
                Fix64.FromInt(map.Route.Count - 1));
        }

        /// <summary>How many separate stretches of route a tower reaches.</summary>
        public int IntervalCount(int tower) => _intervalCount[Checked(tower)];

        /// <summary>Where one of a tower's stretches begins, as a distance along the route.</summary>
        public Fix64 IntervalStart(int tower, int index) => Fix64.FromRaw(_start[Slot(tower, index)]);

        /// <summary>Where that stretch ends.</summary>
        public Fix64 IntervalEnd(int tower, int index) => Fix64.FromRaw(_end[Slot(tower, index)]);

        /// <summary>
        /// Whether a tower can reach a point on the route. This is the whole
        /// range question, and it is a comparison of two numbers on a line.
        /// </summary>
        public bool Covers(int tower, Fix64 distance)
        {
            long raw = distance.Raw;

            // The bounding interval first. Most of what a tower is asked about
            // is nowhere near it, and this settles those in two comparisons
            // instead of walking the list.
            if (raw < _spanStart[tower] || raw > _spanEnd[tower])
            {
                return false;
            }

            int first = _firstInterval[tower];
            int last = first + _intervalCount[tower];

            for (int index = first; index < last; index++)
            {
                if (raw >= _start[index] && raw <= _end[index])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether two towers can be shooting at the same stretch of route --
        /// which is what makes overkill and iteration order real rather than
        /// hypothetical, and is therefore asserted about the committed defense
        /// rather than hoped for.
        /// </summary>
        public bool Overlaps(int tower, int other)
        {
            int firstA = _firstInterval[Checked(tower)];
            int lastA = firstA + _intervalCount[tower];
            int firstB = _firstInterval[Checked(other)];
            int lastB = firstB + _intervalCount[other];

            for (int a = firstA; a < lastA; a++)
            {
                for (int b = firstB; b < lastB; b++)
                {
                    if (_start[a] <= _end[b] && _start[b] <= _end[a])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Walks the route once, in order, collecting the runs of consecutive
        /// cells the tower is within range of. Distance is whole-hex integer
        /// arithmetic on cube coordinates, scaled to milli-hexes to compare
        /// against the authored range -- so no division and no rounding rule is
        /// involved, and the answer cannot depend on how anything is drawn.
        /// </summary>
        private static int Intersect(HexMap map, PlacedTower tower, List<long> start, List<long> end)
        {
            int range = tower.Type.RangeMilliHex;
            int intervals = 0;
            int runStart = -1;

            for (int step = 0; step < map.Route.Count; step++)
            {
                bool inRange = tower.Hex.DistanceTo(map.Route[step]) * MilliHexPerHex <= range;

                if (inRange && runStart < 0)
                {
                    runStart = step;
                }
                else if (!inRange && runStart >= 0)
                {
                    start.Add(Fix64.FromInt(runStart).Raw);
                    end.Add(Fix64.FromInt(step - 1).Raw);
                    intervals++;
                    runStart = -1;
                }
            }

            if (runStart >= 0)
            {
                start.Add(Fix64.FromInt(runStart).Raw);
                end.Add(Fix64.FromInt(map.Route.Count - 1).Raw);
                intervals++;
            }

            return intervals;
        }

        private static void RefuseOffMap(HexMap map, PlacedTower tower)
        {
            if (tower.Column >= map.Width || tower.Row >= map.Height)
            {
                throw new ContentException(
                    "defense",
                    tower.Line,
                    "places "
                    + tower.ToString()
                    + ", which is off a "
                    + map.Width.ToString(CultureInfo.InvariantCulture)
                    + " by "
                    + map.Height.ToString(CultureInfo.InvariantCulture)
                    + " map.");
            }
        }

        private static void RefuseInsideCorridor(HexMap map, PlacedTower tower)
        {
            if (map.CellAt(tower.Column, tower.Row) != MapCell.Ground)
            {
                throw new ContentException(
                    "defense",
                    tower.Line,
                    "places "
                    + tower.ToString()
                    + ", which is a corridor cell. A tower standing in the corridor would be a wall, and "
                    + "walls are how mazing gets in: this simulation derives its route by tracing and has "
                    + "nothing that could reroute around one.");
            }
        }

        private int Checked(int tower)
        {
            if (tower < 0 || tower >= _firstInterval.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tower),
                    "There are "
                    + _firstInterval.Length.ToString(CultureInfo.InvariantCulture)
                    + " towers; asked for number "
                    + tower.ToString(CultureInfo.InvariantCulture)
                    + ".");
            }

            return tower;
        }

        private int Slot(int tower, int index)
        {
            if (index < 0 || index >= _intervalCount[Checked(tower)])
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    "Tower "
                    + tower.ToString(CultureInfo.InvariantCulture)
                    + " reaches "
                    + _intervalCount[tower].ToString(CultureInfo.InvariantCulture)
                    + " stretches of route; asked for number "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + ".");
            }

            return _firstInterval[tower] + index;
        }
    }
}
