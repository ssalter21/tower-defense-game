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

        /// <summary>
        /// How the three attack types are spelled in authored text. The index of
        /// each is its <see cref="AttackType"/>, and this is the one place the
        /// spelling lives.
        /// </summary>
        internal static readonly string[] AttackWords = { "pierce", "impact", "magic" };

        /// <summary>How the three armour types are spelled in authored text.</summary>
        internal static readonly string[] ArmourWords = { "swift", "armoured", "arcane" };

        /// <summary>
        /// The three spellings, for a refusal that has to list what it would
        /// have accepted.
        /// </summary>
        /// <remarks>
        /// A copy rather than the array, because the array is the one place the
        /// spelling lives and a caller handed it could write to it.
        /// </remarks>
        public static IReadOnlyList<string> AttackWordList => (string[])AttackWords.Clone();

        /// <summary>
        /// How an attack type is spelled, for something outside this assembly
        /// that has to name one.
        /// </summary>
        /// <remarks>
        /// <b>The spelling lives in one place and this is the way out of it.</b>
        /// A sweep played against a wall of one attack type writes that type's
        /// name into its report, and a second copy of these three words in the
        /// command line would be free to disagree with the one the content is
        /// parsed by -- which is a file naming a wall the roster does not have.
        /// </remarks>
        public static string WordFor(AttackType attack)
        {
            if (attack == AttackType.None || (int)attack >= AttackWords.Length)
            {
                throw new SimulationException(
                    "There is no word for attack type "
                    + attack.ToString()
                    + ". The three the matrix has rows for are "
                    + string.Join(", ", AttackWords)
                    + ", and None is what a unit that never attacks carries rather than a fourth kind.");
            }

            return AttackWords[(int)attack];
        }

        /// <summary>
        /// The attack type a word names, or <see cref="AttackType.None"/> where
        /// it names none of them.
        /// </summary>
        /// <remarks>
        /// None rather than an exception, because the caller is a command line
        /// reading an argument: it refuses in its own sentence, naming its own
        /// option, and a simulation exception surfacing through that would be
        /// this type answering a question about spelling that was never asked
        /// of the content.
        /// </remarks>
        public static AttackType AttackFor(string word)
        {
            for (int index = 0; index < AttackWords.Length; index++)
            {
                if (AttackWords[index] == word)
                {
                    return (AttackType)index;
                }
            }

            return AttackType.None;
        }

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
