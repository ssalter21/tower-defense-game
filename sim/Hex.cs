using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One cell of the playfield: axial <c>q</c>, <c>r</c>, sixteen bits each,
    /// with cube coordinates derived rather than stored. The six neighbour
    /// directions are fixed and indexed, so an index names the same direction in
    /// every run. See <c>docs/adr/0020-hex-orientation-is-a-view-concern.md</c>.
    /// </summary>
    public readonly struct Hex : IEquatable<Hex>
    {
        private static readonly int[] DirectionQ = { 1, 1, 0, -1, -1, 0 };

        private static readonly int[] DirectionR = { 0, -1, -1, 0, 1, 1 };

        public const int DirectionCount = 6;

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

        public short Q { get; }

        /// <summary>The axial row axis. Identical to the offset row, by construction.</summary>
        public short R { get; }

        public int CubeX => Q;

        public int CubeY => -Q - R;

        public int CubeZ => R;

        /// <summary>
        /// Odd-r offset to axial, where odd rows are the shifted ones.
        /// <c>row - (row &amp; 1)</c> is always even, so the halving is exact and
        /// no rounding rule is involved.
        /// </summary>
        public static Hex FromOddRowOffset(int column, int row) =>
            new Hex(column - ((row - (row & 1)) / 2), row);

        public static void ToOddRowOffset(Hex hex, out int column, out int row)
        {
            row = hex.R;
            column = hex.Q + ((hex.R - (hex.R & 1)) / 2);
        }

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
        /// Distance in steps: half the sum of the absolute differences of the
        /// three cube coordinates.
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

        public override int GetHashCode() => (Q << 16) ^ (ushort)R;

        public override string ToString() =>
            "("
            + Q.ToString(CultureInfo.InvariantCulture)
            + ", "
            + R.ToString(CultureInfo.InvariantCulture)
            + ")";

        private static int Magnitude(int value) => value < 0 ? -value : value;
    }
}
