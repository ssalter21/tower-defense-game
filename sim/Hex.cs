using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One cell of the playfield: axial <c>q</c>, <c>r</c>, sixteen bits each.
    /// Cube coordinates are computed properties rather than stored fields. The
    /// six neighbour directions are fixed and indexed, so a given index names
    /// the same direction in every run.
    /// See <c>docs/adr/0020-hex-orientation-is-a-view-concern.md</c>.
    /// </summary>
    public readonly struct Hex : IEquatable<Hex>
    {
        /// <summary>The six axial neighbour offsets, in their fixed order.</summary>
        private static readonly int[] DirectionQ = { 1, 1, 0, -1, -1, 0 };

        private static readonly int[] DirectionR = { 0, -1, -1, 0, 1, 1 };

        /// <summary>How many neighbours a hex has. Six, forever.</summary>
        public const int DirectionCount = 6;

        /// <summary>Throws when either coordinate does not fit in a signed 16-bit value.</summary>
        public Hex(int q, int r)
        {
            if (q < short.MinValue || q > short.MaxValue || r < short.MinValue || r > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(q),
                    "A hex is two signed 16-bit axial coordinates; ("
                    + q.ToString(CultureInfo.InvariantCulture)
                    + ", "
                    + r.ToString(CultureInfo.InvariantCulture)
                    + ") does not fit.");
            }

            Q = (short)q;
            R = (short)r;
        }

        /// <summary>The axial column axis.</summary>
        public short Q { get; }

        /// <summary>The axial row axis. Identical to the offset row, by construction.</summary>
        public short R { get; }

        /// <summary>Cube <c>x</c>, derived as <c>q</c>.</summary>
        public int CubeX => Q;

        /// <summary>Cube <c>y</c>, derived as <c>-q - r</c>.</summary>
        public int CubeY => -Q - R;

        /// <summary>Cube <c>z</c>, derived as <c>r</c>.</summary>
        public int CubeZ => R;

        /// <summary>
        /// The canonical conversion: odd-r offset, where odd rows are the
        /// shifted ones. <c>row - (row &amp; 1)</c> is always even, so the
        /// halving is exact and no rounding rule is involved.
        /// </summary>
        public static Hex FromOddRowOffset(int column, int row) =>
            new Hex(column - ((row - (row & 1)) / 2), row);

        /// <summary>The inverse conversion, back to a column and a row.</summary>
        public static void ToOddRowOffset(Hex hex, out int column, out int row)
        {
            row = hex.R;
            column = hex.Q + ((hex.R - (hex.R & 1)) / 2);
        }

        /// <summary>
        /// The neighbour in one of the six fixed directions. Throws when the
        /// direction is outside 0 to 5.
        /// </summary>
        public Hex Neighbour(int direction)
        {
            if (direction < 0 || direction >= DirectionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    "A hex has six neighbours, indexed 0 to 5; asked for "
                    + direction.ToString(CultureInfo.InvariantCulture)
                    + ".");
            }

            return new Hex(Q + DirectionQ[direction], R + DirectionR[direction]);
        }

        /// <summary>
        /// Hex distance in steps: half the sum of the absolute differences of
        /// the three cube coordinates.
        /// </summary>
        public int DistanceTo(Hex other)
        {
            int dx = Magnitude(CubeX - other.CubeX);
            int dy = Magnitude(CubeY - other.CubeY);
            int dz = Magnitude(CubeZ - other.CubeZ);

            return (dx + dy + dz) / 2;
        }

        public static bool operator ==(Hex a, Hex b) => a.Q == b.Q && a.R == b.R;

        public static bool operator !=(Hex a, Hex b) => a.Q != b.Q || a.R != b.R;

        public bool Equals(Hex other) => Q == other.Q && R == other.R;

        public override bool Equals(object? obj) => obj is Hex other && Equals(other);

        /// <summary>Packs <c>q</c> into the high bits and exclusive-ors <c>r</c> in.</summary>
        public override int GetHashCode() => (Q << 16) ^ (ushort)R;

        /// <summary>Renders as <c>"(q, r)"</c>.</summary>
        public override string ToString() =>
            "("
            + Q.ToString(CultureInfo.InvariantCulture)
            + ", "
            + R.ToString(CultureInfo.InvariantCulture)
            + ")";

        /// <summary>Absolute value, written out because <c>System.Math</c> is banned.</summary>
        private static int Magnitude(int value) => value < 0 ? -value : value;
    }
}
