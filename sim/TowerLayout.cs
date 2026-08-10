using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One tower of an authored defense: a unit type and the cell it stands on,
    /// for the whole match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cell is authored as a column and a row -- the same coordinates the
    /// map's character grid is read in, so a person placing a tower can count
    /// characters -- and converted to axial through
    /// <see cref="Hex.FromOddRowOffset"/> at load. The axial pair is what the
    /// record format carries and what every distance is computed from; the
    /// offset pair survives only so an error message can name the cell the way
    /// the author wrote it.
    /// </para>
    /// <para>
    /// There is no hp, no cooldown and no state here. A tower is invulnerable
    /// and static for the whole match, so everything about it that can change is
    /// owned by the match and everything that cannot is owned by the type.
    /// </para>
    /// </remarks>
    public readonly struct PlacedTower
    {
        internal PlacedTower(UnitType type, int column, int row, int line)
        {
            Type = type;
            Column = column;
            Row = row;
            Hex = Hex.FromOddRowOffset(column, row);
            Line = line;
        }

        /// <summary>The type this tower is, resolved at load against the type table.</summary>
        public UnitType Type { get; }

        /// <summary>The authored column. Offset coordinates, as the map grid is written.</summary>
        public int Column { get; }

        /// <summary>The authored row.</summary>
        public int Row { get; }

        /// <summary>The cell, axial. This is what the record carries and what distances use.</summary>
        public Hex Hex { get; }

        /// <summary>The line of the authored file this tower came from, for messages.</summary>
        internal int Line { get; }

        public override string ToString() =>
            Type.Label
            + " at column "
            + Column.ToString(CultureInfo.InvariantCulture)
            + ", row "
            + Row.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// An authored defense: the towers, in canonical order, validated against
    /// the unit type table that has to already know every id they name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The order is asserted, not sorted.</b> Towers ascend by row and then
    /// by column -- which, because a row is the axial <c>r</c> and the offset
    /// conversion is monotone in the column within a row, is the same ordering
    /// as ascending <c>(r, q)</c> that the record format asserts over the bytes.
    /// Sorting on load would stabilise iteration and still leave two identical
    /// defenses with two different sets of bytes, at which point
    /// content-addressing a defense stops meaning anything.
    /// </para>
    /// <para>
    /// That order is also the match's iteration order, and iteration order is a
    /// simulation input here: two towers whose ranges overlap can pick the same
    /// creep on the same tick, and which of them lands the killing shot is
    /// decided by which one the loop reaches first. A defense whose rows were
    /// typed in a different order is a different defense, and it says so.
    /// </para>
    /// <para>
    /// This file knows nothing about the map. Whether a tower is standing inside
    /// the corridor, off the edge of the grid, or somewhere it can never reach
    /// the route at all is a question about a tower <i>and</i> a map, and it is
    /// asked by <see cref="TowerCoverage"/>.
    /// </para>
    /// </remarks>
    public sealed class TowerLayout
    {
        private const string Keyword = "tower";

        /// <summary>Fields per row, keyword included.</summary>
        private const int FieldCount = 4;

        /// <summary>The words a row here may open with. There is one.</summary>
        private static readonly string[] RowWords = { Keyword };

        private readonly PlacedTower[] _towers;

        private TowerLayout(PlacedTower[] towers)
        {
            _towers = towers;
        }

        /// <summary>The towers, in canonical order. That order is the match's iteration order.</summary>
        public IReadOnlyList<PlacedTower> Towers => _towers;

        /// <summary>How many towers there are.</summary>
        public int Count => _towers.Length;

        /// <summary>Parses a defense from text, against the types it is allowed to name.</summary>
        public static TowerLayout Parse(string text, UnitTypeTable types) => Parse("defense", text, types);

        /// <summary>Parses a defense from UTF-8 bytes, which is what a caller that read a file holds.</summary>
        public static TowerLayout ParseUtf8(byte[] utf8, UnitTypeTable types) => ParseUtf8("defense", utf8, types);

        /// <summary>Parses a defense, naming the content in any error message.</summary>
        public static TowerLayout ParseUtf8(string source, byte[] utf8, UnitTypeTable types) =>
            Parse(source, DataText.FromUtf8(source, utf8), types);

        /// <summary>Parses a defense, naming the content in any error message.</summary>
        public static TowerLayout Parse(string source, string text, UnitTypeTable types)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var towers = new List<PlacedTower>();
            int previousColumn = -1;
            int previousRow = -1;

            foreach (DataText.Row placement in DataText.Rows(source, text))
            {
                string[] fields = placement.Fields;

                DataText.RequireRow(source, placement, RowWords);

                if (fields.Length != FieldCount)
                {
                    throw DataText.WrongFieldCount(source, placement.Line, Keyword, FieldCount, fields.Length);
                }

                int typeId = DataText.IntegerInRange(source, placement.Line, "the type id", fields[1], 1, 65535);
                int column = DataText.IntegerInRange(
                    source, placement.Line, "the column", fields[2], 0, short.MaxValue);
                int row = DataText.IntegerInRange(
                    source, placement.Line, "the row", fields[3], 0, short.MaxValue);

                UnitType type = DataText.RequireType(
                    source, placement.Line, types, typeId, UnitRole.Placed, "a defense");

                if (row == previousRow && column == previousColumn)
                {
                    throw new ContentException(
                        source,
                        placement.Line,
                        "puts a second tower on column "
                        + column.ToString(CultureInfo.InvariantCulture)
                        + ", row "
                        + row.ToString(CultureInfo.InvariantCulture)
                        + ". One cell holds one tower: two towers sharing a cell would be two towers with "
                        + "one set of coordinates, and a record could not tell them apart.");
                }

                if (row < previousRow || (row == previousRow && column < previousColumn))
                {
                    throw new ContentException(
                        source,
                        placement.Line,
                        "is out of canonical order: towers ascend by row and then by column. The order is "
                        + "asserted rather than sorted on load, because sorting would leave identical "
                        + "defenses with different bytes -- and because this order is the order the match "
                        + "iterates towers in, which decides who lands the killing shot when two of them "
                        + "fire at one creep on one tick.");
                }

                previousRow = row;
                previousColumn = column;
                towers.Add(new PlacedTower(type, column, row, placement.Line));
            }

            if (towers.Count == 0)
            {
                throw new ContentException(source, 0, "has no towers in it at all.");
            }

            return new TowerLayout(towers.ToArray());
        }

        /// <summary>
        /// The same defense, arriving from a stored record instead of from text.
        /// </summary>
        /// <remarks>
        /// There is no order assertion here, and that is not a gap:
        /// <see cref="GhostRecord"/> asserts ascending <c>(r, q)</c> over the
        /// bytes as it reads them, which is the same order this file asserts over
        /// rows and columns -- <c>r</c> is the row, and the offset conversion is
        /// monotone in the column within a row. Re-checking it here would be a
        /// second implementation of one rule, and the maps the two disagreed
        /// about would be the interesting ones.
        /// </remarks>
        internal static TowerLayout FromRecord(PlacedTower[] towers) => new TowerLayout(towers);

        /// <summary>
        /// The defense a <see cref="Board"/> derives, sorted there rather than
        /// authored in order.
        /// </summary>
        /// <remarks>
        /// There is no order assertion here either, and for a different reason
        /// than <see cref="FromRecord"/>'s: the board sorts, so the order is
        /// produced rather than read. A board with nothing on it derives a
        /// layout with no towers in it, which is a run that has built nothing
        /// and not a defense somebody did not finish -- the refusal of an empty
        /// one belongs to <see cref="Parse"/>, where the files are.
        /// </remarks>
        internal static TowerLayout FromBoard(PlacedTower[] towers) => new TowerLayout(towers);
    }
}
