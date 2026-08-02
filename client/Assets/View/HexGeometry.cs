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
