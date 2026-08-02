using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One cell of the playfield: axial <c>q</c>, <c>r</c>, sixteen bits each,
    /// and no cube coordinate stored anywhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cube coordinate is always derivable -- <c>x = q</c>, <c>z = r</c>,
    /// <c>y = -q - r</c> -- so storing it would add nothing but an opportunity
    /// for two fields to disagree. <see cref="CubeX"/>, <see cref="CubeY"/> and
    /// <see cref="CubeZ"/> are therefore computed properties, and the record
    /// format carries the two shorts.
    /// </para>
    /// <para>
    /// <b>Odd-r offset to axial is the simulation's canonical conversion,
    /// regardless of how anything is drawn.</b> The map is authored as a
    /// character grid, so a cell arrives as a column and a row, and turning
    /// that pair into <c>(q, r)</c> is arithmetic -- which means it belongs to
    /// the simulation version and moves replays if it ever changes. Nothing
    /// downstream is free to choose even-r because a mesh looked better that
    /// way: orientation is a view question and it enters only at
    /// axial-to-world, which is not in this assembly.
    /// </para>
    /// <para>
    /// The six neighbour directions are fixed and indexed, because "the third
    /// neighbour" has to mean the same thing in every run.
    /// </para>
    /// </remarks>
    public readonly struct Hex : IEquatable<Hex>
    {
        /// <summary>The six axial neighbour offsets, in their fixed order.</summary>
        private static readonly int[] DirectionQ = { 1, 1, 0, -1, -1, 0 };

        private static readonly int[] DirectionR = { 0, -1, -1, 0, 1, 1 };

        /// <summary>How many neighbours a hex has. Six, forever.</summary>
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

        /// <summary>The axial column axis.</summary>
        public short Q { get; }

        /// <summary>The axial row axis. Identical to the offset row, by construction.</summary>
        public short R { get; }

        /// <summary>Cube <c>x</c>, derived. Never stored.</summary>
        public int CubeX => Q;

        /// <summary>Cube <c>y</c>, derived. Never stored.</summary>
        public int CubeY => -Q - R;

        /// <summary>Cube <c>z</c>, derived. Never stored.</summary>
        public int CubeZ => R;

        /// <summary>
        /// The canonical conversion: odd-r offset, where odd rows are the
        /// shifted ones. <c>row - (row &amp; 1)</c> is always even, so the
        /// halving is exact and no rounding rule is involved.
        /// </summary>
        public static Hex FromOddRowOffset(int column, int row) =>
            new Hex(column - ((row - (row & 1)) / 2), row);

        /// <summary>The inverse conversion, so a round trip can be asserted rather than assumed.</summary>
        public static void ToOddRowOffset(Hex hex, out int column, out int row)
        {
            row = hex.R;
            column = hex.Q + ((hex.R - (hex.R & 1)) / 2);
        }

        /// <summary>The neighbour in one of the six fixed directions.</summary>
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
        /// Hex distance, in steps. Computed from the derived cube coordinates,
        /// which is the whole reason they are worth deriving.
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

        /// <summary>Absolute value, written out because <c>System.Math</c> is banned.</summary>
        private static int Magnitude(int value) => value < 0 ? -value : value;
    }
}
