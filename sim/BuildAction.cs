using System;
using System.Globalization;

namespace Sim
{
    /// <summary>Which of the two things a defensive action does to the board.</summary>
    /// <remarks>
    /// The two are separate rather than one instruction the board disambiguates
    /// by whether the cell is occupied, because each names the other's mistake:
    /// a <c>place</c> on a taken cell and an <c>upgrade</c> on an empty one are
    /// both refusals, and a single verb would silently do whichever the board
    /// happened to be in a position for.
    /// </remarks>
    public enum ActionKind
    {
        /// <summary>Stand a new thing on an empty cell. It takes the next placement ordinal.</summary>
        Place = 0,

        /// <summary>Change the type of the placement already standing on a cell, keeping its identity.</summary>
        Upgrade = 1,
    }

    /// <summary>
    /// One instruction of a build phase that changes the board: which of the two
    /// things it does, the type it names, and the cell it names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An action is not a placement.</b> An action is the instruction and a
    /// <see cref="Placement"/> is what it creates or changes -- an action has no
    /// identity, survives nothing and is the same value however many times it is
    /// written. See <c>docs/adr/0048-a-board-is-not-a-layout.md</c>.
    /// </para>
    /// <para>
    /// <b>Actions have no canonical order and must not get one.</b> Their order
    /// is meaning: a phase may upgrade what it has just placed, the placement
    /// ordinals fall out of the sequence, and the same two actions written the
    /// other way round are a different run rather than a different spelling of
    /// one. The wave slots beside them used to be exactly the opposite and
    /// ascended strictly by type id; since #191 a slot's position is its release
    /// order, so both halves of a build phase are read the same way and for the
    /// same reason.
    /// </para>
    /// <para>
    /// <b>A type id and not a type.</b> Nothing here holds a
    /// <see cref="UnitTypeTable"/>, so what an id names, whether that row is a
    /// tower, whether the run has unlocked it and whether the purse can afford
    /// it are all questions for whatever applies the action.
    /// </para>
    /// <para>
    /// <b>The cell is a column and a row</b>, spelled the way
    /// <c>content/map.txt</c> is written and <c>content/defense.txt</c> names
    /// one, so that a person can count characters in the map to compose an
    /// action. The conversion to axial happens where the cell meets the map --
    /// <see cref="Footing"/> and <see cref="Placement"/> -- through the one
    /// canonical <see cref="Hex.FromOddRowOffset"/>, and not here, because a
    /// coordinate that is off the map is a question an action may ask and only a
    /// map can answer.
    /// </para>
    /// <para>
    /// <c>default</c> is a place of type id 0, which <see cref="Of"/> refuses.
    /// Nothing produces one -- <see cref="Of"/> is the only way to get an action
    /// -- and a value nobody built naming a type nothing defines is the safe
    /// direction for it to be wrong in.
    /// </para>
    /// </remarks>
    public readonly struct BuildAction : IEquatable<BuildAction>
    {
        /// <summary>
        /// The range a column or a row may fall in. Signed 16-bit, because a
        /// record stores a cell as an axial pair of those, and negative because
        /// a coordinate off the edge of the grid is a thing a person can write
        /// and the map is what refuses it.
        /// </summary>
        public const int LeastCoordinate = -32768;

        /// <summary>The other end of that range.</summary>
        public const int GreatestCoordinate = 32767;

        /// <summary>
        /// The largest type id an action may name. Unsigned 16-bit, which is
        /// what every format that stores a type id stores one as.
        /// </summary>
        public const int GreatestTypeId = 65535;

        private BuildAction(ActionKind kind, int typeId, int column, int row)
        {
            Kind = kind;
            TypeId = typeId;
            Column = column;
            Row = row;
        }

        /// <summary>Whether this stands something new or changes what stands.</summary>
        public ActionKind Kind { get; }

        /// <summary>Which row of the unit table it names.</summary>
        public int TypeId { get; }

        /// <summary>The column of the cell. Offset coordinates, as the map grid is written.</summary>
        public int Column { get; }

        /// <summary>The row of the cell.</summary>
        public int Row { get; }

        /// <summary>One action, of a kind, on a type, at a cell.</summary>
        public static BuildAction Of(ActionKind kind, int typeId, int column, int row)
        {
            if (kind != ActionKind.Place && kind != ActionKind.Upgrade)
            {
                throw new SimulationException(
                    "A build action is of kind "
                    + ((int)kind).ToString(CultureInfo.InvariantCulture)
                    + ", and the kinds there are are "
                    + ((int)ActionKind.Place).ToString(CultureInfo.InvariantCulture)
                    + " and "
                    + ((int)ActionKind.Upgrade).ToString(CultureInfo.InvariantCulture)
                    + ". A kind nothing declares is an instruction no board has a branch for.");
            }

            if (typeId < 1 || typeId > GreatestTypeId)
            {
                throw new SimulationException(
                    "A build action names type id "
                    + typeId.ToString(CultureInfo.InvariantCulture)
                    + ". An action names one row of the unit table, and every row is identified from one.");
            }

            if (column < LeastCoordinate
                || column > GreatestCoordinate
                || row < LeastCoordinate
                || row > GreatestCoordinate)
            {
                throw new SimulationException(
                    "A build action names column "
                    + column.ToString(CultureInfo.InvariantCulture)
                    + ", row "
                    + row.ToString(CultureInfo.InvariantCulture)
                    + ". A cell is two signed 16-bit coordinates, because that is what a record stores one "
                    + "as, so a cell outside that could be decided and never written down.");
            }

            return new BuildAction(kind, typeId, column, row);
        }

        public static bool operator ==(BuildAction a, BuildAction b) => a.Equals(b);

        public static bool operator !=(BuildAction a, BuildAction b) => !a.Equals(b);

        public bool Equals(BuildAction other) =>
            Kind == other.Kind
            && TypeId == other.TypeId
            && Column == other.Column
            && Row == other.Row;

        public override bool Equals(object? obj) => obj is BuildAction other && Equals(other);

        public override int GetHashCode() => (((int)Kind * 31 ^ TypeId) * 31 ^ Column) * 31 ^ Row;

        public override string ToString() =>
            (Kind == ActionKind.Place ? "place " : "upgrade ")
            + "type "
            + TypeId.ToString(CultureInfo.InvariantCulture)
            + " at column "
            + Column.ToString(CultureInfo.InvariantCulture)
            + ", row "
            + Row.ToString(CultureInfo.InvariantCulture);
    }
}
