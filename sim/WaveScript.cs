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

            string[] lines = DataText.SplitLines(text);
            var orders = new List<UnitOrder>();
            int previousTick = -1;
            int previousType = -1;
            long total = 0;

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                int number = index + 1;

                if (DataText.IsBlankOrComment(line))
                {
                    continue;
                }

                string[] fields = DataText.Fields(source, number, line);

                if (!string.Equals(fields[0], Keyword, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        source,
                        number,
                        "starts with '" + fields[0] + "', but the only row a wave has is '" + Keyword + "'.");
                }

                if (fields.Length != FieldCount)
                {
                    throw DataText.WrongFieldCount(source, number, Keyword, FieldCount, fields.Length);
                }

                int tick = DataText.IntegerInRange(source, number, "the tick offset", fields[1], 0, int.MaxValue);
                int typeId = DataText.IntegerInRange(source, number, "the type id", fields[2], 1, 65535);
                int count = DataText.IntegerInRange(source, number, "the count", fields[3], 1, 65535);
                int corridor = DataText.IntegerInRange(source, number, "the corridor", fields[4], 0, 255);

                if (!types.TryById(typeId, out UnitType? type))
                {
                    throw new ContentException(
                        source,
                        number,
                        "sends type id "
                        + typeId.ToString(CultureInfo.InvariantCulture)
                        + ", which the unit type table does not define. An unknown id refuses to load "
                        + "rather than being skipped.");
                }

                if (type!.Role != UnitRole.Moving)
                {
                    throw new ContentException(
                        source,
                        number,
                        "sends "
                        + type.ToString()
                        + ", which is a placed unit. A wave is composed of units that walk.");
                }

                if (tick == previousTick && typeId == previousType)
                {
                    throw new ContentException(
                        source,
                        number,
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
                        number,
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
