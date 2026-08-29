using UnityEngine;

namespace View
{
    /// <summary>
    /// Where a hex is, in metres. The only place in the project where a hex
    /// acquires an orientation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Orientation is a view question and it enters here.</b> The simulation
    /// converts the authored character grid to axial <c>(q, r)</c> through
    /// <c>Sim.Hex.FromOddRowOffset</c> and stops — it has no idea whether a hex
    /// is pointy-top, flat-top or drawn as a square, because none of that
    /// changes what happens in a match. Pointy-top is decided in this file and
    /// nowhere else, so nothing downstream is free to disagree.
    /// </para>
    /// <para>
    /// <b>Pointy-top, two metres across the flats.</b> A pointy-top hex has a
    /// vertex at the top; its width is measured between the two vertical flat
    /// sides, and that is <see cref="AcrossFlats"/>. The circumradius follows —
    /// <c>AcrossFlats / sqrt(3)</c> — and so does the row pitch, which is
    /// <c>1.5 * circumradius</c> and works out to <c>sqrt(3)</c>. Those are not
    /// three independent numbers: pick the width and the other two are
    /// arithmetic, which is why only the width is typed here.
    /// </para>
    /// <para>
    /// Rows are odd-r: odd-numbered rows sit half a cell to the right. That is
    /// the simulation's canonical convention, so the shift is not chosen here
    /// either — it falls out of the axial coordinate the simulation hands over.
    /// Row zero is at <c>z = 0</c> and rows increase towards <c>-Z</c>, so the
    /// grid reads top-to-bottom the way the map file does.
    /// </para>
    /// </remarks>
    public static class HexGeometry
    {
        /// <summary>
        /// The width of one hex, flat side to flat side, in metres. The one
        /// authored number in this file; everything else is derived from it.
        /// </summary>
        public const float AcrossFlats = 2.0f;

        /// <summary>The square root of three, to float precision.</summary>
        public const float Root3 = 1.7320508f;

        /// <summary>
        /// Centre to vertex, in metres. <c>AcrossFlats / sqrt(3)</c>, which is
        /// about 1.1547.
        /// </summary>
        public const float Circumradius = AcrossFlats / Root3;

        /// <summary>
        /// How far apart two consecutive rows are, in metres. <c>1.5</c> times
        /// the circumradius, which for a two-metre hex is <c>sqrt(3)</c> —
        /// 1.732. Rows interlock, so this is less than
        /// <see cref="PointToPoint"/>.
        /// </summary>
        public const float RowPitch = 1.5f * Circumradius;

        /// <summary>
        /// The height of one hex, vertex to vertex, in metres. Twice the
        /// circumradius, about 2.3094.
        /// </summary>
        public const float PointToPoint = 2f * Circumradius;

        /// <summary>
        /// How far apart two hexes in the same row are, in metres. Equal to
        /// <see cref="AcrossFlats"/>, because that is what "across the flats"
        /// means for a pointy-top hex.
        /// </summary>
        public const float ColumnPitch = AcrossFlats;

        /// <summary>
        /// How far one level stands above the one below it, in metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Half a metre, because the pack cuts half steps and there was
        /// nowhere to put them.</b> The tile models are authored with their
        /// walkable face at <c>y = 0</c> and a metre of body hanging to
        /// <c>y = -1</c>, and the ramps come in a matched pair: every
        /// <c>*_sloped_low</c> piece tops out at exactly <c>+0.5</c> and every
        /// <c>*_sloped_high</c> piece at exactly <c>+1.0</c>. At a one-metre
        /// level only the high one had a level to land on, so a board could
        /// only ever step a whole block and the ground read as a stack of
        /// plates. At half a metre both land: one level of climb is the low
        /// ramp, two is the high one, and a slope can be graded instead of
        /// stepped.
        /// </para>
        /// <para>
        /// <b>The body still closes the seam, and now it over-closes it.</b>
        /// A tile raised one level has a metre of earth under a half-metre
        /// drop, so the extra half is buried in the hillside rather than
        /// showing daylight. What a metre of body no longer covers is a drop of
        /// more than two levels, which is why <see cref="HexFloor"/> stacks
        /// <c>hex_grass_bottom</c> underneath to make the rest of the cliff.
        /// </para>
        /// <para>
        /// It is a view constant and never reaches the simulation, which knows
        /// a level as an integer and gives it a quarter hex of reach
        /// (<c>Reach.MilliHexPerLevel</c>) without any opinion about how tall
        /// that is in metres. See ADR-0023.
        /// </para>
        /// </remarks>
        public const float LevelStep = 0.5f;

        /// <summary>
        /// How tall one tile's body is, in metres: the earth hanging under its
        /// walkable face, which is what closes the seam between two levels.
        /// </summary>
        /// <remarks>
        /// A fact about the pack rather than a choice -- every base tile in it
        /// measures <c>y = -1</c> to <c>y = 0</c> -- and it is written down
        /// because <see cref="LevelStep"/> is no longer equal to it and the two
        /// used to be silently the same number.
        /// </remarks>
        public const float TileBody = 1.0f;

        /// <summary>
        /// Where the centre of a hex is in world space, from its axial
        /// coordinate. Pure: no state, no engine object, no map.
        /// </summary>
        /// <remarks>
        /// <c>q</c> steps one hex to the right; <c>r</c> steps one row down and
        /// half a hex to the right, which is where the odd-row shift comes from
        /// without anybody having to write it down.
        /// </remarks>
        public static Vector3 ToWorld(Sim.Hex hex) =>
            new Vector3(AcrossFlats * (hex.Q + (hex.R * 0.5f)), 0f, -RowPitch * hex.R);

        /// <summary>
        /// Where the centre of a hex is, from the column and row of the
        /// authored grid. Goes through the simulation's own odd-r conversion
        /// rather than repeating it, because a second implementation of that
        /// arithmetic is a second opinion about where a cell is.
        /// </summary>
        public static Vector3 ToWorld(int column, int row) =>
            ToWorld(Sim.Hex.FromOddRowOffset(column, row));

        /// <summary>
        /// Where the centre of a hex on a given tier is. The same place as
        /// <see cref="ToWorld(Sim.Hex)"/>, lifted by the tier.
        /// </summary>
        /// <remarks>
        /// An overload rather than a defaulted argument, so that every existing
        /// caller keeps meaning what it meant — ground level — and the ones that
        /// have an opinion about height say so.
        /// </remarks>
        public static Vector3 ToWorld(Sim.Hex hex, int level)
        {
            Vector3 flat = ToWorld(hex);

            return new Vector3(flat.x, level * LevelStep, flat.z);
        }

        /// <summary>
        /// Where the centre of a hex on a given tier is, from the column and row
        /// of the authored grid.
        /// </summary>
        public static Vector3 ToWorld(int column, int row, int level) =>
            ToWorld(Sim.Hex.FromOddRowOffset(column, row), level);

        /// <summary>
        /// The six corners of a hex centred on the origin, in world units,
        /// counter-clockwise seen from below and starting at the top vertex.
        /// </summary>
        /// <remarks>
        /// The winding is chosen so that a triangle fan <c>(centre, corner i,
        /// corner i+1)</c> faces <c>+Y</c> under Unity's convention, which is
        /// why the tile mesh needs no normal-flipping special case.
        /// </remarks>
        public static Vector3 Corner(int index)
        {
            float radians = index * (Mathf.PI / 3f);

            return new Vector3(Circumradius * Mathf.Sin(radians), 0f, Circumradius * Mathf.Cos(radians));
        }
    }
}
