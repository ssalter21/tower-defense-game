using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One line of a wave: send this many of this type, this many ticks after
    /// the wave starts, down this corridor.
    /// </summary>
    /// <remarks>
    /// This is the shape the record format carries -- <c>u32 tick_offset</c>,
    /// <c>u16 type_id</c>, <c>u16 count</c>, <c>u8 corridor</c> -- so the
    /// authored file and the stored bytes describe the same thing and the
    /// authoring format never needs a migration to become a record.
    /// </remarks>
    public readonly struct UnitOrder
    {
        internal UnitOrder(int tickOffset, UnitType type, int count, int corridor)
        {
            TickOffset = tickOffset;
            Type = type;
            Count = count;
            Corridor = corridor;
        }

        /// <summary>Ticks after the wave starts.</summary>
        public int TickOffset { get; }

        /// <summary>Which unit type, by its stable id.</summary>
        public int TypeId => Type.Id;

        /// <summary>
        /// The type itself, resolved at load. The order already had to be
        /// checked against the type table to be accepted at all, so carrying the
        /// row it was checked against costs nothing and means nothing
        /// downstream has to re-resolve an id it was already told is good --
        /// which is what lets a match be constructed from the map, the defense,
        /// the wave and the seed, with no fifth argument holding the table.
        /// </summary>
        public UnitType Type { get; }

        /// <summary>How many. Repeats of one key are merged into this rather than being two rows.</summary>
        public int Count { get; }

        /// <summary>Which corridor. Zero is the only one the skeleton has.</summary>
        public int Corridor { get; }

        public override string ToString() =>
            "tick "
            + TickOffset.ToString(CultureInfo.InvariantCulture)
            + ": "
            + Count.ToString(CultureInfo.InvariantCulture)
            + " of type "
            + TypeId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A wave, as authored: an ordered list of unit orders, validated against
    /// the unit type table that has to already know every id it names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Orders ascend by <c>(tick, type)</c> and are unique on that pair, with
    /// repeats merged into a count. <b>That ordering is asserted, not
    /// sorted.</b> Sorting on load would stabilise iteration but leave two
    /// identical waves with different bytes, and content-addressing stops
    /// meaning anything the moment that is true.
    /// </para>
    /// <para>
    /// A type id this table has never heard of is a load error, not a skipped
    /// row. The usual schema-evolution advice -- tolerate what you do not
    /// understand -- is correct for messages and exactly wrong here: a replay
    /// that quietly drops an order it cannot read produces a confidently wrong
    /// result that still validates.
    /// </para>
    /// </remarks>
    public sealed class WaveScript
    {
        private const string Keyword = "order";

        /// <summary>Fields per row, keyword included.</summary>
        private const int FieldCount = 5;

        /// <summary>The words a row here may open with. There is one.</summary>
        private static readonly string[] RowWords = { Keyword };

        private readonly UnitOrder[] _orders;

        private WaveScript(UnitOrder[] orders, int totalUnits)
        {
            _orders = orders;
            TotalUnits = totalUnits;
        }

        /// <summary>The orders, in canonical <c>(tick, type)</c> order.</summary>
        public IReadOnlyList<UnitOrder> Orders => _orders;

        /// <summary>How many orders there are.</summary>
        public int Count => _orders.Length;

        /// <summary>Every unit this wave sends, summed.</summary>
        public int TotalUnits { get; }

        /// <summary>
        /// How many of one creep type this wave sends, over all of its orders.
        /// </summary>
        /// <remarks>
        /// <b>This is what a round is measured as carrying.</b> A wave a build
        /// phase composed holds a type in exactly one order, because a creep
        /// fills at most one slot -- but an authored wave may spell the same
        /// type at several ticks, and a count that ignored the others would
        /// under-report what a run already fields. Summing is the reading that
        /// is right for both.
        /// </remarks>
        public int CountOf(int typeId)
        {
            int found = 0;

            for (int index = 0; index < _orders.Length; index++)
            {
                if (_orders[index].TypeId == typeId)
                {
                    found += _orders[index].Count;
                }
            }

            return found;
        }

        /// <summary>
        /// The wave a run carries before it has played a round: nothing at all.
        /// </summary>
        /// <remarks>
        /// Round one is the one round that pays for every creep in it, and this
        /// is what says so. It is <see cref="FromSlots"/> over no orders rather
        /// than a fourth kind of wave, so it is the same empty wave a build
        /// phase composes when every slot is banked.
        /// </remarks>
        public static WaveScript Nothing { get; } = FromSlots(Array.Empty<UnitOrder>());

        /// <summary>Parses a wave from text, against the types it is allowed to name.</summary>
        public static WaveScript Parse(string text, UnitTypeTable types) => Parse("wave", text, types);

        /// <summary>Parses a wave from UTF-8 bytes, against the types it is allowed to name.</summary>
        public static WaveScript ParseUtf8(byte[] utf8, UnitTypeTable types) => ParseUtf8("wave", utf8, types);

        /// <summary>Parses a wave, naming the content in any error message.</summary>
        public static WaveScript ParseUtf8(string source, byte[] utf8, UnitTypeTable types) =>
            Parse(source, DataText.FromUtf8(source, utf8), types);

        /// <summary>Parses a wave, naming the content in any error message.</summary>
        public static WaveScript Parse(string source, string text, UnitTypeTable types)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var orders = new List<UnitOrder>();
            int previousTick = -1;
            int previousType = -1;
            long total = 0;

            foreach (DataText.Row row in DataText.Rows(source, text))
            {
                string[] fields = row.Fields;

                DataText.RequireRow(source, row, RowWords);

                if (fields.Length != FieldCount)
                {
                    throw DataText.WrongFieldCount(source, row.Line, Keyword, FieldCount, fields.Length);
                }

                int tick = DataText.IntegerInRange(source, row.Line, "the tick offset", fields[1], 0, int.MaxValue);
                int typeId = DataText.IntegerInRange(source, row.Line, "the type id", fields[2], 1, 65535);
                int count = DataText.IntegerInRange(source, row.Line, "the count", fields[3], 1, 65535);
                int corridor = DataText.IntegerInRange(source, row.Line, "the corridor", fields[4], 0, 255);

                UnitType type = DataText.RequireType(
                    source, row.Line, types, typeId, UnitRole.Moving, "a wave order");

                if (tick == previousTick && typeId == previousType)
                {
                    throw new ContentException(
                        source,
                        row.Line,
                        "repeats the order key (tick "
                        + tick.ToString(CultureInfo.InvariantCulture)
                        + ", type "
                        + typeId.ToString(CultureInfo.InvariantCulture)
                        + "). Repeats merge into one row's count, so that two identical waves cannot have "
                        + "two different sets of bytes.");
                }

                if (tick < previousTick || (tick == previousTick && typeId < previousType))
                {
                    throw new ContentException(
                        source,
                        row.Line,
                        "is out of canonical order: orders ascend by tick and then by type id. The order "
                        + "is asserted rather than sorted on load, because sorting would leave identical "
                        + "waves with different bytes.");
                }

                previousTick = tick;
                previousType = typeId;
                total += count;
                orders.Add(new UnitOrder(tick, type, count, corridor));
            }

            if (orders.Count == 0)
            {
                throw new ContentException(source, 0, "sends nothing at all.");
            }

            if (total > int.MaxValue)
            {
                throw new ContentException(source, 0, "sends more units than an integer can count.");
            }

            return new WaveScript(orders.ToArray(), (int)total);
        }

        /// <summary>
        /// The wave a build phase composed: one order per filled slot.
        /// </summary>
        /// <remarks>
        /// <b>This is the one wave that may send nothing.</b> A file or a record
        /// with no orders in it is one somebody did not finish, and both refuse.
        /// A build phase whose every slot was left empty is a player banking the
        /// round at the ruleset's interest instead of attacking, which is a
        /// position rather than an omission -- and a match resolves it as the
        /// nothing it is, because a wave with no units in it has released
        /// everything it will ever release on tick zero.
        /// </remarks>
        internal static WaveScript FromSlots(UnitOrder[] orders)
        {
            long total = 0;

            for (int index = 0; index < orders.Length; index++)
            {
                total += orders[index].Count;
            }

            return new WaveScript(orders, (int)total);
        }

        /// <summary>
        /// The same wave, arriving from a stored record instead of from text.
        /// </summary>
        /// <remarks>
        /// The canonical order and the uniqueness of <c>(tick, type)</c> are
        /// asserted by <see cref="WaveRecord"/> over the bytes, for the same
        /// reason the tower order is: one rule, one implementation, checked where
        /// the thing being checked actually is.
        /// </remarks>
        internal static WaveScript FromRecord(UnitOrder[] orders)
        {
            long total = 0;

            for (int index = 0; index < orders.Length; index++)
            {
                total += orders[index].Count;
            }

            if (orders.Length == 0)
            {
                throw new ContentException("wave record", 0, "sends nothing at all.");
            }

            if (total > int.MaxValue)
            {
                throw new ContentException("wave record", 0, "sends more units than an integer can count.");
            }

            return new WaveScript(orders, (int)total);
        }
    }
}
