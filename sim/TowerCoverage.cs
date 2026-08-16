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
    /// <b>Elevation costs this nothing, and that is why it is here.</b> A route
    /// cell's level is as fixed as its position, so the signed level term of
    /// <see cref="Reach.Shoots"/> is evaluated per route cell in the walk
    /// below, at load, exactly as flat range was. What height changes is how
    /// often the list has more than one entry in it: a ridge crossing the
    /// corridor carves a hole in a tower's reach where flat ground would not,
    /// and a hole in a run of route cells is the second interval this type
    /// already returns. Nothing about the tick loop moves, which is the whole
    /// of what a third dimension was priced at.
    /// </para>
    /// <para>
    /// Distance is measured in route steps: the entrance is 0, the exit is
    /// <see cref="RouteLength"/>, and one whole unit of distance is one hex of
    /// corridor. An interval covering route cells <c>a</c> through <c>b</c> is
    /// the closed interval <c>[a, b]</c>, so a creep between two cells is in
    /// range exactly when both of the cells it lies between are.
    /// </para>
    /// <para>
    /// <b>Three faults, and they do not all belong to the same caller.</b>
    /// <see cref="Footing"/> is the one map-aware answer about a cell, and this
    /// is one of the two callers that ask it.
    /// </para>
    /// <para>
    /// A tower off the edge of the grid and a tower standing inside the corridor
    /// are <i>impossible</i> positions, refused here and refused by anybody else
    /// who asks. A tower that cannot reach the route at all is a <i>bad</i>
    /// position rather than an impossible one, and that refusal belongs to the
    /// authored file alone: in a file it is the shape of a typo in a coordinate,
    /// which without the check would present as a tower that simply never fires
    /// and look exactly like a balance problem. A player who builds where
    /// nothing walks has made a bad decision rather than an illegal one, and
    /// every other refusal in this repository is for something that could not
    /// have happened.
    /// </para>
    /// <para>
    /// The two are told apart by <see cref="PlacedTower.Line"/>, which is the
    /// line of the file a tower was written on and is zero for a tower nobody
    /// wrote: a defense derived from a <see cref="Board"/> carries no line
    /// because a placement made at wave 4 was never in a file. So a layout that
    /// came from text keeps all three refusals with its own line numbers in
    /// them, and a layout a run built keeps the two that describe the
    /// impossible. A tower that reaches nothing covers nothing, which is what
    /// <see cref="Covers"/> then answers about it.
    /// </para>
    /// <para>
    /// Every refusal here is a <see cref="ContentException"/>, because
    /// everything with a line on it came out of authored content. A caller
    /// holding a board rather than a file asks <see cref="Footing"/> directly
    /// and refuses in its own vocabulary.
    /// </para>
    /// </remarks>
    public sealed class TowerCoverage
    {
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
        /// <remarks>
        /// A tower that reaches no part of the route has an <i>empty</i>
        /// bounding interval rather than a missing one: the start is above the
        /// end, so both comparisons fail for every distance and
        /// <see cref="Covers"/> answers false without a special case in the
        /// inner loop.
        /// </remarks>
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
                if (intervalCount[tower] == 0)
                {
                    _spanStart[tower] = long.MaxValue;
                    _spanEnd[tower] = long.MinValue;
                    continue;
                }

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

        /// <summary>
        /// Intersects a defense with a map's route. Every fault a layout with
        /// lines on it can have throws; a layout a run built keeps only the
        /// refusals for positions that could not have happened.
        /// </summary>
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
                Footing footing = Footing.Of(map, tower.Type, tower.Column, tower.Row);

                // Reaching nothing is a refusal only where there is a file to
                // blame it on. A tower with no line came off a board, and a
                // player is allowed to build somewhere useless.
                if (!footing.Possible || (!footing.ReachesRoute && tower.Line > 0))
                {
                    throw new ContentException(
                        "defense",
                        tower.Line,
                        "places " + tower.ToString() + ", " + footing.Fault);
                }

                firstInterval[index] = start.Count;
                intervalCount[index] = Intersect(map, tower, start, end);
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
        /// cells the tower is within range of. The range test is
        /// <see cref="Footing.Reaches"/> -- the same one that answered whether
        /// there are any runs at all, so the two cannot come apart, and the
        /// same one that folds in how far the tower is shooting up or down to
        /// reach each cell.
        /// </summary>
        private static int Intersect(HexMap map, PlacedTower tower, List<long> start, List<long> end)
        {
            int range = tower.Type.RangeMilliHex;
            int intervals = 0;
            int runStart = -1;

            for (int step = 0; step < map.Route.Count; step++)
            {
                bool inRange = Footing.Reaches(map, tower.Hex, range, map.Route[step]);

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
