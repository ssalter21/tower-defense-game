using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sim
{
    /// <summary>
    /// One thing a run has standing on the map: an identity, the type it is
    /// now, and the cell it stands on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A placement is not a tower.</b> A <see cref="PlacedTower"/> is a row
    /// of an authored file and carries the line it was written on; a placement
    /// was made by a decision at a wave, and there is no file for it to point
    /// into. It also survives changing type, which is what an upgrade does to
    /// one: the same id, a different <see cref="Type"/>.
    /// </para>
    /// <para>
    /// The id is the ordinal of the <c>place</c> that made it, counted from
    /// one. No format carries it and nothing stores it -- it is the placement's
    /// position among the places a run has made, and <see cref="Board"/> is
    /// what works it out. See
    /// <c>docs/adr/0049-a-placement-identity-is-derived.md</c>.
    /// </para>
    /// </remarks>
    public readonly struct Placement
    {
        internal Placement(int id, UnitType type, int column, int row)
        {
            Id = id;
            Type = type;
            Column = column;
            Row = row;
            Hex = Hex.FromOddRowOffset(column, row);
        }

        /// <summary>Which placement of the run this is, counted from one.</summary>
        public int Id { get; }

        /// <summary>The type standing here now. An upgrade swaps it.</summary>
        public UnitType Type { get; }

        /// <summary>The column. Offset coordinates, as the map grid is written and an action names one.</summary>
        public int Column { get; }

        /// <summary>The row.</summary>
        public int Row { get; }

        /// <summary>The cell, axial. This is what distances are computed from.</summary>
        public Hex Hex { get; }

        public override string ToString() =>
            "placement "
            + Id.ToString(CultureInfo.InvariantCulture)
            + ", "
            + Type.Label
            + " at column "
            + Column.ToString(CultureInfo.InvariantCulture)
            + ", row "
            + Row.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// What a run has built: its placements, in the order they were placed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A board is not a layout, and deriving one is a computation.</b> Three
    /// things live here that have no business on a <see cref="TowerLayout"/>:
    /// the placement ordinals, placement order itself, and the absence of a
    /// source line. Deriving rather than widening leaves every reader of a
    /// layout reading exactly the type it reads today. See
    /// <c>docs/adr/0048-a-board-is-not-a-layout.md</c>.
    /// </para>
    /// <para>
    /// <b>The sort is the seam.</b> One place turns placement order into
    /// canonical order, and everything derived from a board comes out the far
    /// side of it: everything upstream is a sequence of decisions, everything
    /// downstream is a position.
    /// </para>
    /// <para>
    /// <b>An empty board is a position and not a fault.</b> A board with
    /// nothing on it derives a layout with no towers in it, and a match
    /// resolves against that. <see cref="TowerLayout.Parse"/>'s refusal of a
    /// file with no towers is a rule about a <i>file</i> and stays one.
    /// </para>
    /// <para>
    /// <b>A value, like the purse and the unlocks.</b> <see cref="Place"/> and
    /// <see cref="Upgrade"/> return a new board rather than moving this one, so
    /// a run's board is a fold over its build phases and a test can assert on
    /// any intermediate without replaying anything.
    /// </para>
    /// </remarks>
    public sealed class Board
    {
        private static readonly Board Nothing = new Board(new Placement[0]);

        /// <summary>
        /// The line a derived tower carries. Zero is what
        /// <see cref="ContentException"/> reads as "not on a line", which is
        /// what a placement made at wave 4 is.
        /// </summary>
        private const int NoLine = 0;

        /// <summary>What the reported block is called, on the line its columns are named on.</summary>
        private const string Label = "the board at the end";

        /// <summary>What the label is indented by, and with it the whole block.</summary>
        private const string Indent = "  ";

        /// <summary>The row a board with nothing on it reports instead of numbers.</summary>
        private const string NothingBuilt = "nothing was built";

        /// <summary>How wide the id column is, header word included.</summary>
        private const int IdWidth = 7;

        /// <summary>How wide the type column is.</summary>
        private const int TypeWidth = 7;

        /// <summary>How wide each of the two cell columns is.</summary>
        private const int CellWidth = 6;

        /// <summary>What a row is indented by: the label's own width, so the numbers clear it.</summary>
        private static readonly string RowIndent = new string(' ', Indent.Length + Label.Length);

        private readonly Placement[] _placements;

        private Board(Placement[] placements)
        {
            _placements = placements;
        }

        /// <summary>A run that has built nothing yet. Every run opens here.</summary>
        public static Board Empty => Nothing;

        /// <summary>
        /// The board an authored defense stands as: its towers placed in the
        /// order the file wrote them down.
        /// </summary>
        /// <remarks>
        /// The one fold from a composed defense to a board, so a caller holding
        /// a defense file has a board to open a run with and nowhere holds a
        /// second copy of the walk. The towers arrive in canonical order and
        /// <see cref="Layout"/> sorts into it, so the layout this derives is
        /// the layout that went in.
        /// </remarks>
        public static Board Of(TowerLayout defense)
        {
            if (defense is null)
            {
                throw new ArgumentNullException(nameof(defense));
            }

            Board board = Nothing;

            for (int index = 0; index < defense.Towers.Count; index++)
            {
                PlacedTower tower = defense.Towers[index];

                board = board.Place(tower.Type, tower.Column, tower.Row);
            }

            return board;
        }

        /// <summary>Every placement, in the order the run placed them.</summary>
        public IReadOnlyList<Placement> Placements => _placements;

        /// <summary>How many placements stand. One per <c>place</c> the run has made.</summary>
        public int Count => _placements.Length;

        /// <summary>
        /// Whether that cell has nothing on it, which is what
        /// <see cref="Place"/> requires and <see cref="Upgrade"/> refuses.
        /// </summary>
        /// <remarks>
        /// Asked rather than found out by being refused, so that whatever is
        /// choosing where to build walks the same one-placement-per-cell rule
        /// this type enforces instead of keeping a second copy of it.
        /// </remarks>
        public bool IsFree(int column, int row) => IndexOn(column, row) < 0;

        /// <summary>
        /// This board plus one more placement, which takes the next ordinal.
        /// </summary>
        /// <remarks>
        /// The ordinal is the count of placements already standing plus one,
        /// which is the count of <c>place</c> actions because an upgrade adds
        /// none and nothing removes one.
        /// </remarks>
        public Board Place(UnitType type, int column, int row)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            int standing = IndexOn(column, row);

            if (standing >= 0)
            {
                throw new SimulationException(
                    "A place puts a second thing on "
                    + CellOf(column, row)
                    + ", where "
                    + _placements[standing].ToString()
                    + " already stands. One cell holds one placement: two of them sharing a cell would be "
                    + "two placements with one set of coordinates, and the layout this board derives could "
                    + "not tell them apart.");
            }

            var grown = new Placement[_placements.Length + 1];

            for (int index = 0; index < _placements.Length; index++)
            {
                grown[index] = _placements[index];
            }

            grown[_placements.Length] = new Placement(_placements.Length + 1, type, column, row);

            return new Board(grown);
        }

        /// <summary>
        /// This board with the placement on that cell standing as another type.
        /// Its id and its position in placement order are both untouched.
        /// </summary>
        /// <remarks>
        /// Another type, and not the one already standing: an upgrade pays the
        /// full price of the row it names, so one that swaps a type for itself
        /// is a purchase that changes nothing.
        /// </remarks>
        public Board Upgrade(UnitType type, int column, int row)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            int standing = IndexOn(column, row);

            if (standing < 0)
            {
                throw new SimulationException(
                    "An upgrade names "
                    + CellOf(column, row)
                    + ", where nothing stands. "
                    + ToString()
                    + ". An upgrade swaps the type of a placement that is already standing, so a cell with "
                    + "nothing on it names none to swap.");
            }

            if (_placements[standing].Type.Id == type.Id)
            {
                throw new SimulationException(
                    "An upgrade puts "
                    + type.ToString()
                    + " on "
                    + CellOf(column, row)
                    + ", where "
                    + _placements[standing].ToString()
                    + " already stands as that type. An upgrade pays the full price of the row it names, "
                    + "so one that swaps a type for itself is a purchase that changes nothing -- refused "
                    + "rather than charged for, because a script that meant a different row has a typo in "
                    + "it and a script that meant this one has a line it does not need.");
            }

            var swapped = new Placement[_placements.Length];

            for (int index = 0; index < _placements.Length; index++)
            {
                swapped[index] = _placements[index];
            }

            swapped[standing] = new Placement(_placements[standing].Id, type, column, row);

            return new Board(swapped);
        }

        /// <summary>
        /// The defense this board is, in the canonical order a match, a record
        /// and the field pool all read: ascending by row and then by column.
        /// </summary>
        public TowerLayout Layout()
        {
            Placement[] ordered = InCanonicalOrder();
            var towers = new PlacedTower[ordered.Length];

            for (int index = 0; index < ordered.Length; index++)
            {
                Placement placement = ordered[index];

                towers[index] = new PlacedTower(placement.Type, placement.Column, placement.Row, NoLine);
            }

            return TowerLayout.FromBoard(towers);
        }

        /// <summary>
        /// The block a report prints at the bottom: a line naming the columns,
        /// then one row per placement in the same canonical order
        /// <see cref="Layout"/> derives, spelled the way a row of
        /// <c>content/defense.txt</c> is with the placement id beside it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The id is here and not on the layout, and that is the whole reason
        /// this is not a walk over one: a <see cref="TowerLayout"/> carries no
        /// ordinals, so the block that ties a standing tower back to the
        /// decision that built it can only be written from a board.
        /// </para>
        /// <para>
        /// A board with nothing on it reports the header and a row saying so,
        /// because a header over no rows reads as a report that was cut off.
        /// </para>
        /// </remarks>
        public string ToReportText()
        {
            var text = new StringBuilder();

            text.Append(Indent)
                .Append(Label)
                .Append("id".PadLeft(IdWidth))
                .Append("type".PadLeft(TypeWidth))
                .Append("col".PadLeft(CellWidth))
                .Append("row".PadLeft(CellWidth));

            if (_placements.Length == 0)
            {
                return text.Append('\n').Append(RowIndent).Append(NothingBuilt).ToString();
            }

            Placement[] ordered = InCanonicalOrder();

            for (int index = 0; index < ordered.Length; index++)
            {
                Placement placement = ordered[index];

                text.Append('\n')
                    .Append(RowIndent)
                    .Append(Number(placement.Id, IdWidth))
                    .Append(Number(placement.Type.Id, TypeWidth))
                    .Append(Number(placement.Column, CellWidth))
                    .Append(Number(placement.Row, CellWidth));
            }

            return text.ToString();
        }

        public override string ToString() =>
            _placements.Length == 0
                ? "nothing placed"
                : _placements.Length.ToString(CultureInfo.InvariantCulture)
                    + " placed: "
                    + string.Join(", ", Array.ConvertAll(_placements, placement => placement.ToString()));

        /// <summary>Whether a placement belongs before one that is already ordered.</summary>
        private static bool SortsAhead(Placement placement, Placement ordered) =>
            placement.Row < ordered.Row || (placement.Row == ordered.Row && placement.Column < ordered.Column);

        /// <summary>One number, right-aligned under the word naming its column.</summary>
        private static string Number(int value, int width) =>
            value.ToString(CultureInfo.InvariantCulture).PadLeft(width);

        private static string CellOf(int column, int row) =>
            "column "
            + column.ToString(CultureInfo.InvariantCulture)
            + ", row "
            + row.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// The placements ascending by row and then by column, which is the
        /// order a match, a record and the field pool all read a defense in.
        /// </summary>
        /// <remarks>
        /// The ordering is an insertion by hand because the framework's sorts
        /// are unstable and banned here. It needs no tie-break: one cell holds
        /// one placement, so no two of them share a <c>(row, column)</c>.
        /// </remarks>
        private Placement[] InCanonicalOrder()
        {
            var ordered = new Placement[_placements.Length];

            for (int index = 0; index < _placements.Length; index++)
            {
                Placement placement = _placements[index];
                int place = index;

                while (place > 0 && SortsAhead(placement, ordered[place - 1]))
                {
                    ordered[place] = ordered[place - 1];
                    place--;
                }

                ordered[place] = placement;
            }

            return ordered;
        }

        /// <summary>Where the placement on that cell sits in placement order, or -1 where the cell is free.</summary>
        private int IndexOn(int column, int row)
        {
            for (int index = 0; index < _placements.Length; index++)
            {
                if (_placements[index].Column == column && _placements[index].Row == row)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
