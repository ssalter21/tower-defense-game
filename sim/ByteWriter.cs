using System.Globalization;

namespace Sim
{
    /// <summary>
    /// The whole write side of the record format: a growing byte array and six
    /// little-endian primitives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hand-rolled, and that is the decision rather than an accident of having
    /// no library available. A reflection serializer's output is a function of
    /// the type definitions it was pointed at, so renaming a field or reordering
    /// an enum silently changes what stored records mean years later. Here the
    /// bytes are written out one field at a time in an order somebody chose, and
    /// changing that order is a visible edit to this assembly that has to be
    /// paid for with a format version.
    /// </para>
    /// <para>
    /// <c>System.IO</c> is banned in this assembly and the IL scan enforces it,
    /// so there is no <c>BinaryWriter</c> to reach for even if one were wanted.
    /// That ban is why the writer is fifty lines instead of five, and it is also
    /// why the byte order is stated here rather than inherited from whatever the
    /// machine happens to be.
    /// </para>
    /// <para>
    /// Every width check throws a <see cref="SimulationException"/> rather than
    /// a <see cref="RecordException"/>, because a caller handing a
    /// seventy-thousandth tower to a <c>u16</c> count is a fault in this program
    /// and not in somebody's stored bytes.
    /// </para>
    /// </remarks>
    internal sealed class ByteWriter
    {
        private byte[] _bytes;

        private int _length;

        internal ByteWriter(int capacity)
        {
            _bytes = new byte[capacity < 1 ? 1 : capacity];
        }

        /// <summary>How many bytes have been written so far.</summary>
        internal int Length => _length;

        internal void U8(string field, int value)
        {
            Check(field, value, 0, 255);
            Room(1);
            _bytes[_length++] = (byte)value;
        }

        internal void U16(string field, int value)
        {
            Check(field, value, 0, 65535);
            Room(2);
            _bytes[_length++] = (byte)value;
            _bytes[_length++] = (byte)(value >> 8);
        }

        internal void I16(string field, int value)
        {
            Check(field, value, short.MinValue, short.MaxValue);
            Room(2);
            _bytes[_length++] = (byte)value;
            _bytes[_length++] = (byte)(value >> 8);
        }

        internal void U32(string field, long value)
        {
            Check(field, value, 0, 4294967295L);
            Room(4);

            for (int shift = 0; shift < 32; shift += 8)
            {
                _bytes[_length++] = (byte)(value >> shift);
            }
        }

        internal void U64(ulong value)
        {
            Room(8);

            for (int shift = 0; shift < 64; shift += 8)
            {
                _bytes[_length++] = (byte)(value >> shift);
            }
        }

        /// <summary>Writes bytes as they are: magic, map cells, an inner record.</summary>
        internal void Raw(byte[] source)
        {
            Room(source.Length);

            for (int index = 0; index < source.Length; index++)
            {
                _bytes[_length++] = source[index];
            }
        }

        /// <summary>Writes a string's characters as one byte each. ASCII only, asserted.</summary>
        internal void Ascii(string field, string text)
        {
            Room(text.Length);

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                if (character < ' ' || character > '~')
                {
                    throw new SimulationException(
                        "The "
                        + field
                        + " is not printable ASCII, so what it becomes on disk would depend on how a "
                        + "runtime encodes a character.");
                }

                _bytes[_length++] = (byte)character;
            }
        }

        /// <summary>The bytes written, exactly, with no spare capacity on the end.</summary>
        internal byte[] ToArray()
        {
            var trimmed = new byte[_length];

            for (int index = 0; index < _length; index++)
            {
                trimmed[index] = _bytes[index];
            }

            return trimmed;
        }

        private static void Check(string field, long value, long low, long high)
        {
            if (value >= low && value <= high)
            {
                return;
            }

            throw new SimulationException(
                "The "
                + field
                + " is "
                + value.ToString(CultureInfo.InvariantCulture)
                + ", which does not fit the field the record format gives it ("
                + low.ToString(CultureInfo.InvariantCulture)
                + " to "
                + high.ToString(CultureInfo.InvariantCulture)
                + "). Widening a field is a format version bump, not a cast.");
        }

        private void Room(int wanted)
        {
            if (_length + wanted <= _bytes.Length)
            {
                return;
            }

            int size = _bytes.Length;

            while (size < _length + wanted)
            {
                size *= 2;
            }

            var grown = new byte[size];

            for (int index = 0; index < _length; index++)
            {
                grown[index] = _bytes[index];
            }

            _bytes = grown;
        }
    }
}
