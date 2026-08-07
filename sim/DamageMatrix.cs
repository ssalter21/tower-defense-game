using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Which of the three attack types a unit's shots carry. The value is the
    /// row index into <see cref="DamageMatrix"/>.
    /// </summary>
    public enum AttackType
    {
        Pierce = 0,

        Impact = 1,

        Magic = 2,

        /// <summary>
        /// Carried by a unit that delivers no damage. Not a row of the matrix,
        /// and looking a cell up with it is a refusal.
        /// </summary>
        None = 3,
    }

    /// <summary>
    /// Which of the three armour types a unit is protected by. The value is the
    /// column index into <see cref="DamageMatrix"/>.
    /// </summary>
    public enum ArmourType
    {
        Swift = 0,

        Armoured = 1,

        Arcane = 2,

        /// <summary>
        /// Carried by a unit with no health pool. Not a column of the matrix,
        /// and looking a cell up with it is a refusal.
        /// </summary>
        None = 3,
    }

    /// <summary>
    /// Three attack types against three armour types, as one flat array of nine
    /// percentages indexed <c>cells[attack * 3 + armour]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A flat array and not a lookup table.</b> The obvious keyed collection
    /// is a banned type, its enumeration order is an implementation detail, and
    /// nine integers in file order are what the content hash folds.
    /// </para>
    /// <para>
    /// <b>Every row and every column is a permutation of the same three
    /// values</b>, which is asserted where the matrix is parsed. A cell is a
    /// percentage applied to the hit: 100 leaves it alone, and the spread
    /// between the largest and smallest is how far type moves shots-to-kill.
    /// </para>
    /// </remarks>
    public sealed class DamageMatrix
    {
        /// <summary>Rows. <see cref="AttackType.None"/> is not one of them.</summary>
        public const int AttackTypes = 3;

        /// <summary>Columns. <see cref="ArmourType.None"/> is not one of them.</summary>
        public const int ArmourTypes = 3;

        /// <summary>Cells, which is rows times columns.</summary>
        public const int CellCount = AttackTypes * ArmourTypes;

        private readonly int[] _cells;

        internal DamageMatrix(int[] cells)
        {
            if (cells is null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Length != CellCount)
            {
                throw new ArgumentException(
                    "The matrix is "
                    + CellCount.ToString(CultureInfo.InvariantCulture)
                    + " cells and this one has "
                    + cells.Length.ToString(CultureInfo.InvariantCulture)
                    + ".",
                    nameof(cells));
            }

            _cells = cells;
        }

        /// <summary>The nine cells, in <c>attack * 3 + armour</c> order.</summary>
        public IReadOnlyList<int> Cells => _cells;

        /// <summary>The percentage this attack type deals against that armour type.</summary>
        /// <exception cref="SimulationException">Either type is outside the matrix.</exception>
        public int Cell(AttackType attack, ArmourType armour)
        {
            int row = (int)attack;
            int column = (int)armour;

            if (row < 0 || row >= AttackTypes)
            {
                throw new SimulationException(
                    "Attack type "
                    + Describe(row, attack.ToString())
                    + " is not a row of the damage matrix. A unit that fires carries one of the three "
                    + "attack types, and a shot that falls outside the matrix has no cell to be resolved "
                    + "through.");
            }

            if (column < 0 || column >= ArmourTypes)
            {
                throw new SimulationException(
                    "Armour type "
                    + Describe(column, armour.ToString())
                    + " is not a column of the damage matrix. A unit that can be damaged carries one of "
                    + "the three armour types, and a target that falls outside the matrix has no cell to "
                    + "be resolved through.");
            }

            return _cells[(row * ArmourTypes) + column];
        }

        /// <summary>
        /// Folds the nine cells into a hash in index order. Moving a cell is a
        /// content change; the order these are folded in is the layout.
        /// </summary>
        internal Hash64 Fold(Hash64 hash)
        {
            for (int index = 0; index < _cells.Length; index++)
            {
                hash = hash.Add(_cells[index]);
            }

            return hash;
        }

        private static string Describe(int value, string name) =>
            name + " (" + value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
