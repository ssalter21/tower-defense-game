using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What a map says about a cell a tower would stand on: whether the position
    /// is possible at all, and whether any part of the route comes near enough
    /// for the thing standing there to ever fire.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two answers rather than one, because the two callers deserve
    /// different refusals.</b> A cell off the edge of the grid and a cell inside
    /// the corridor describe positions that are <i>impossible</i> -- there is no
    /// hex there, or standing on it would be a wall, and this simulation traces
    /// its route rather than searching for one. A cell nothing walks past
    /// describes a position that is merely <i>bad</i>. So
    /// <see cref="Possible"/> and <see cref="ReachesRoute"/> are asked
    /// separately, and a caller reading a player's decision may refuse the first
    /// and accept the second. See
    /// <c>docs/adr/0048-a-board-is-not-a-layout.md</c>.
    /// </para>
    /// <para>
    /// <b>A cell, not a tower.</b> The question is asked about a type and a
    /// column and a row, and about nothing else -- no source file, no line, no
    /// identity. That is what lets a placement made at wave 4 ask it: there is
    /// no file for such a placement to point into, and nothing here invents one.
    /// The answer carries a <see cref="Fault"/> clause instead, which each
    /// caller prefixes with whatever it calls the thing it was asked about and
    /// wraps in whichever exception its own vocabulary uses.
    /// </para>
    /// <para>
    /// Coordinates are the offset column and row the map grid is written in and
    /// an action names, because that is the pair a person can count characters
    /// to; the axial conversion happens here, once, through
    /// <see cref="Hex.FromOddRowOffset"/>.
    /// </para>
    /// <para>
    /// <c>default</c> is a footing that stands nothing and reaches nothing, with
    /// nothing to say about why. Nothing produces one -- <see cref="Of"/> is the
    /// only way to get a footing -- and the default refusing everything is the
    /// safe direction for it to fail in.
    /// </para>
    /// </remarks>
    public readonly struct Footing
    {
        /// <summary>Thousandths of a hex per hex. Ranges are authored in milli-hexes.</summary>
        private const int MilliHexPerHex = 1000;

        private readonly string? _fault;

        private Footing(bool possible, bool reachesRoute, string? fault)
        {
            Possible = possible;
            ReachesRoute = reachesRoute;
            _fault = fault;
        }

        /// <summary>
        /// Whether anything could stand here at all: on the grid, and on ground
        /// rather than corridor. False is a position that could not have
        /// happened, and every caller refuses it.
        /// </summary>
        public bool Possible { get; }

        /// <summary>
        /// Whether the route passes within range of this cell. False is a
        /// position that is possible and useless, and whether that is a refusal
        /// is the caller's to decide.
        /// </summary>
        public bool ReachesRoute { get; }

        /// <summary>Both of the above: a cell worth building on.</summary>
        public bool Sound => Possible && ReachesRoute;

        /// <summary>
        /// Why this footing is not sound, as a clause that follows a comma after
        /// whatever the caller calls the thing standing here -- <c>"which is a
        /// corridor cell. ..."</c>. Empty where the footing is sound.
        /// </summary>
        public string Fault => _fault ?? string.Empty;

        /// <summary>What a map says about one cell, for one type of tower.</summary>
        public static Footing Of(HexMap map, UnitType type, int column, int row)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            // Negative coordinates are off the map for the same reason a
            // coordinate past the width is, and they are checked here rather
            // than left to HexMap.CellAt's ArgumentOutOfRangeException: a
            // command stream can name one, and a caller asking a question is
            // owed an answer rather than an argument fault.
            if (column < 0 || row < 0 || column >= map.Width || row >= map.Height)
            {
                return new Footing(
                    false,
                    false,
                    "which is off a "
                    + map.Width.ToString(CultureInfo.InvariantCulture)
                    + " by "
                    + map.Height.ToString(CultureInfo.InvariantCulture)
                    + " map.");
            }

            if (map.CellAt(column, row) != MapCell.Ground)
            {
                return new Footing(
                    false,
                    false,
                    "which is a corridor cell. A tower standing in the corridor would be a wall, and "
                    + "walls are how mazing gets in: this simulation derives its route by tracing and has "
                    + "nothing that could reroute around one.");
            }

            Hex hex = Hex.FromOddRowOffset(column, row);

            for (int step = 0; step < map.Route.Count; step++)
            {
                if (Reaches(hex, type.RangeMilliHex, map.Route[step]))
                {
                    return new Footing(true, true, null);
                }
            }

            return new Footing(
                true,
                false,
                "which cannot reach any part of the route: its range is "
                + type.RangeMilliHex.ToString(CultureInfo.InvariantCulture)
                + " thousandths of a hex and the nearest corridor cell is further than that. A "
                + "tower that can never fire is what a mistyped coordinate looks like, and it "
                + "would otherwise present as a balance problem.");
        }

        /// <summary>
        /// Whether one cell is within a range of another. This is the range
        /// test, and there is one of it.
        /// </summary>
        /// <remarks>
        /// Whole-hex integer arithmetic on cube coordinates, scaled to
        /// milli-hexes to compare against the authored range -- so no division
        /// and no rounding rule is involved, and the answer cannot depend on how
        /// anything is drawn. <see cref="TowerCoverage"/> walks the route with
        /// this to collect the runs of cells a tower covers; the walk above uses
        /// it to answer whether there are any.
        /// </remarks>
        internal static bool Reaches(Hex from, int rangeMilliHex, Hex cell) =>
            from.DistanceTo(cell) * MilliHexPerHex <= rangeMilliHex;

        public override string ToString() => Sound ? "stands and reaches the route" : Fault;
    }
}
