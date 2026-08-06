using System.Globalization;

namespace Sim
{
    /// <summary>
    /// The write side of the record format: a growing byte array and six
    /// little-endian primitives. Width checks throw
    /// <see cref="SimulationException"/> rather than <see cref="RecordException"/>,
    /// since a value that does not fit is a fault in this program rather than in
    /// stored bytes.
    /// See <c>docs/adr/0032-serialisation-is-hand-rolled-field-by-field.md</c>.
    /// </summary>
    internal sealed class ByteWriter
    {
        private byte[] _bytes;

        private int _length;

        internal ByteWriter(int capacity)
        {
            _bytes = new byte[capacity < 1 ? 1 : capacity];
        }

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

        /// <summary>Writes a string's characters as one byte each, rejecting anything outside printable ASCII.</summary>
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

        // Doubles capacity until the wanted bytes fit.
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
