using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One order as the record carries it: <c>u32 tick_offset + u16 type_id +
    /// u16 count + u8 corridor</c>, nine bytes.
    /// </summary>
    public readonly struct RecordOrder : IEquatable<RecordOrder>
    {
        public RecordOrder(int tickOffset, int typeId, int count, int corridor)
        {
            TickOffset = tickOffset;
            TypeId = typeId;
            Count = count;
            Corridor = corridor;
        }

        /// <summary>Ticks after the wave starts.</summary>
        public int TickOffset { get; }

        /// <summary>Which unit type, by its stable id.</summary>
        public int TypeId { get; }

        /// <summary>How many. Repeats of one key are merged into this rather than being two rows.</summary>
        public int Count { get; }

        /// <summary>Which corridor. Zero is the only one the skeleton has.</summary>
        public int Corridor { get; }

        public static bool operator ==(RecordOrder a, RecordOrder b) => a.Equals(b);

        public static bool operator !=(RecordOrder a, RecordOrder b) => !a.Equals(b);

        public bool Equals(RecordOrder other) =>
            TickOffset == other.TickOffset
            && TypeId == other.TypeId
            && Count == other.Count
            && Corridor == other.Corridor;

        public override bool Equals(object? obj) => obj is RecordOrder other && Equals(other);

        public override int GetHashCode() => ((TickOffset * 31 ^ TypeId) * 31 ^ Count) * 31 ^ Corridor;

        public override string ToString() =>
            "tick "
            + TickOffset.ToString(CultureInfo.InvariantCulture)
            + ": "
            + Count.ToString(CultureInfo.InvariantCulture)
            + " of type "
            + TypeId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A stored wave: the orders, in canonical order, unique on
    /// <c>(tick, type)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ascending by tick and then by type, strictly, and asserted rather than
    /// sorted.</b> Two orders of the same type on the same tick are one order and
    /// must have been merged into a count before they got here -- otherwise two
    /// waves that send exactly the same units have two different sets of bytes,
    /// two different ids, and the phrase "this wave goes with this defense" stops
    /// being checkable.
    /// </para>
    /// <para>
    /// A wave carries no seed and no result. What happened when it was thrown at
    /// a defense is the replay bundle's business, and the wave itself is the same
    /// object however many times it is used.
    /// </para>
    /// </remarks>
    public sealed class WaveRecord : IEquatable<WaveRecord>
    {
        private readonly RecordOrder[] _orders;

        private WaveRecord(RecordHeader header, RecordOrder[] orders)
        {
            Header = header;
            _orders = orders;
        }

        /// <summary>Magic, format version, simulation version, content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>The orders, ascending by <c>(tick, type)</c>. Asserted at load.</summary>
        public IReadOnlyList<RecordOrder> Orders => _orders;

        /// <summary>How many orders there are.</summary>
        public int Count => _orders.Length;

        /// <summary>Records a live wave, at the current format version.</summary>
        public static WaveRecord Of(WaveScript wave, UnitTypeTable types)
        {
            if (wave is null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var orders = new RecordOrder[wave.Count];

            for (int index = 0; index < wave.Count; index++)
            {
                UnitOrder order = wave.Orders[index];
                orders[index] = new RecordOrder(order.TickOffset, order.TypeId, order.Count, order.Corridor);
            }

            return new WaveRecord(RecordHeader.Current(RecordKind.Wave, types.ContentHash), orders);
        }

        /// <summary>Reads a wave from bytes. The read gate, and nothing else.</summary>
        public static WaveRecord FromBytes(byte[] bytes) => FromBytes("wave record", bytes);

        /// <summary>Reads a wave from bytes, naming them in any error message.</summary>
        public static WaveRecord FromBytes(string record, byte[] bytes)
        {
            var cursor = new ByteCursor(record, bytes);
            WaveRecord read = ReadFrom(cursor);
            cursor.ExpectEnd("wave");
            return read;
        }

        /// <summary>The bytes. Always the current format version -- there is one writer.</summary>
        public byte[] ToBytes()
        {
            var writer = new ByteWriter(RecordFormat.HeaderBytes + 2 + (_orders.Length * RecordFormat.OrderBytes));
            WriteTo(writer);
            return writer.ToArray();
        }

        /// <summary>
        /// The wave as the simulation wants it, resolved against a type table.
        /// An id the table does not define refuses; it is never skipped.
        /// </summary>
        public WaveScript ToScript(UnitTypeTable types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var orders = new UnitOrder[_orders.Length];

            for (int index = 0; index < _orders.Length; index++)
            {
                RecordOrder order = _orders[index];

                UnitType type = RecordFormat.RequireType(
                    RecordKind.Wave, types, order.TypeId, UnitRole.Moving, "a wave order");

                orders[index] = new UnitOrder(order.TickOffset, type, order.Count, order.Corridor);
            }

            return WaveScript.FromRecord(orders);
        }

        public bool Equals(WaveRecord? other)
        {
            if (other is null || Header != other.Header || _orders.Length != other._orders.Length)
            {
                return false;
            }

            for (int index = 0; index < _orders.Length; index++)
            {
                if (_orders[index] != other._orders[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as WaveRecord);

        public override int GetHashCode() => (Header.GetHashCode() * 31) ^ _orders.Length;

        public override string ToString() =>
            Header.ToString() + ", " + _orders.Length.ToString(CultureInfo.InvariantCulture) + " orders";

        internal void WriteTo(ByteWriter writer)
        {
            Header.Write(writer);
            writer.U16("order count", _orders.Length);

            for (int index = 0; index < _orders.Length; index++)
            {
                RecordOrder order = _orders[index];
                writer.U32("order tick offset", order.TickOffset);
                writer.U16("order type id", order.TypeId);
                writer.U16("order count", order.Count);
                writer.U8("order corridor", order.Corridor);
            }
        }

        internal static WaveRecord ReadFrom(ByteCursor cursor)
        {
            RecordHeader header = RecordHeader.Read(cursor, RecordKind.Wave);

            switch (header.FormatVersion)
            {
                case 0:
                    return ReadVersion0(cursor, header);

                default:
                    throw cursor.Fault(
                        "is wave format version "
                        + header.FormatVersion.ToString(CultureInfo.InvariantCulture)
                        + ", which the read gate accepted and this reader has no branch for. The two "
                        + "lists have drifted apart, which is a fault in this build rather than in the "
                        + "record.");
            }
        }

        /// <summary>
        /// Version 0: <c>u16 order_count + UnitOrder[]</c>. This branch never
        /// goes away; a later version gets a branch beside it.
        /// </summary>
        private static WaveRecord ReadVersion0(ByteCursor cursor, RecordHeader header)
        {
            int count = cursor.U16("the order count");

            if (count == 0)
            {
                throw cursor.Fault("sends nothing at all.");
            }

            var orders = new RecordOrder[count];
            long previousTick = -1;
            int previousType = -1;

            for (int index = 0; index < count; index++)
            {
                string what =
                    "order "
                    + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + count.ToString(CultureInfo.InvariantCulture);

                uint tick = cursor.U32("the tick offset of " + what);
                int typeId = cursor.U16("the type id of " + what);
                int unitCount = cursor.U16("the count of " + what);
                int corridor = cursor.U8("the corridor of " + what);

                if (tick > int.MaxValue)
                {
                    throw cursor.Fault(
                        what
                        + " starts at tick "
                        + tick.ToString(CultureInfo.InvariantCulture)
                        + ", which is past the last tick the simulation can count to.");
                }

                if (typeId == 0)
                {
                    throw cursor.Fault(what + " has type id 0, and zero means no unit.");
                }

                if (unitCount == 0)
                {
                    throw cursor.Fault(
                        what
                        + " sends no units. An order of zero is an order that should not have been "
                        + "written, and two waves differing only by one would have different bytes and "
                        + "identical meaning.");
                }

                if (index > 0 && tick == previousTick && typeId == previousType)
                {
                    throw cursor.Fault(
                        what
                        + " repeats the order key (tick "
                        + tick.ToString(CultureInfo.InvariantCulture)
                        + ", type "
                        + typeId.ToString(CultureInfo.InvariantCulture)
                        + "). Repeats merge into one order's count, so that two waves sending the same "
                        + "units cannot have two different sets of bytes.");
                }

                if (index > 0 && (tick < previousTick || (tick == previousTick && typeId < previousType)))
                {
                    throw cursor.Fault(
                        what
                        + " is out of canonical order: orders ascend by tick and then by type id. The "
                        + "order is asserted rather than sorted on load, because sorting would leave "
                        + "two identical waves with two different sets of bytes.");
                }

                previousTick = tick;
                previousType = typeId;
                orders[index] = new RecordOrder((int)tick, typeId, unitCount, corridor);
            }

            return new WaveRecord(header, orders);
        }
    }
}
