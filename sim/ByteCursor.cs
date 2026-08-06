using System.Globalization;

namespace Sim
{
    /// <summary>
    /// The read side of the record format: a position in a byte array and the
    /// same six little-endian primitives the writer has. Every read is
    /// bounds-checked and every failure names the field; bytes left over at the
    /// end are refused rather than ignored.
    /// See <c>docs/adr/0013-record-reading-is-an-all-or-nothing-gate.md</c>.
    /// </summary>
    internal sealed class ByteCursor
    {
        private readonly string _record;

        private readonly byte[] _bytes;

        private int _position;

        internal ByteCursor(string record, byte[] bytes)
        {
            _record = record;
            _bytes = bytes;
        }

        internal string Record => _record;

        internal int Position => _position;

        internal int Remaining => _bytes.Length - _position;

        internal int U8(string field)
        {
            Need(field, 1);
            return _bytes[_position++];
        }

        internal int U16(string field)
        {
            Need(field, 2);
            int value = _bytes[_position] | (_bytes[_position + 1] << 8);
            _position += 2;
            return value;
        }

        internal int I16(string field) => (short)U16(field);

        internal uint U32(string field)
        {
            Need(field, 4);
            uint value = 0;

            for (int shift = 0; shift < 32; shift += 8)
            {
                value |= (uint)_bytes[_position++] << shift;
            }

            return value;
        }

        internal ulong U64(string field)
        {
            Need(field, 8);
            ulong value = 0;

            for (int shift = 0; shift < 64; shift += 8)
            {
                value |= (ulong)_bytes[_position++] << shift;
            }

            return value;
        }

        /// <summary>A run of bytes, copied out: magic, or a map's cells.</summary>
        internal byte[] Raw(string field, int count)
        {
            Need(field, count);

            var taken = new byte[count];

            for (int index = 0; index < count; index++)
            {
                taken[index] = _bytes[_position + index];
            }

            _position += count;
            return taken;
        }

        /// <summary>A run of bytes as one character each, for the magic tag.</summary>
        internal string Ascii(string field, int count)
        {
            Need(field, count);

            var characters = new char[count];

            for (int index = 0; index < count; index++)
            {
                characters[index] = (char)_bytes[_position + index];
            }

            _position += count;
            return new string(characters);
        }

        /// <summary>
        /// A copy of a range already read, keeping an inner record's own bytes so
        /// its id can be taken over them rather than over a re-serialised parse.
        /// </summary>
        internal byte[] Slice(int start, int count)
        {
            var taken = new byte[count];

            for (int index = 0; index < count; index++)
            {
                taken[index] = _bytes[start + index];
            }

            return taken;
        }

        internal void ExpectEnd(string what)
        {
            if (Remaining == 0)
            {
                return;
            }

            throw new RecordException(
                _record,
                "has "
                + Remaining.ToString(CultureInfo.InvariantCulture)
                + " bytes left over after the "
                + what
                + " was read. Trailing bytes mean the reader and the writer disagree about the layout, "
                + "which is what the format version exists to prevent -- so they are refused rather than "
                + "ignored.");
        }

        internal RecordException Fault(string message) => new RecordException(_record, message);

        private void Need(string field, int count)
        {
            if (count >= 0 && count <= Remaining)
            {
                return;
            }

            throw new RecordException(
                _record,
                "ran out of bytes reading "
                + field
                + ": "
                + count.ToString(CultureInfo.InvariantCulture)
                + " needed at offset "
                + _position.ToString(CultureInfo.InvariantCulture)
                + " and "
                + Remaining.ToString(CultureInfo.InvariantCulture)
                + " left. A truncated record is refused outright rather than read as far as it goes.");
        }
    }
}
