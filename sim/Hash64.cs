using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A 64-bit fold over a sequence of integers. This is how the content hash
    /// and the map hash are computed, and it is deliberately not a hash over
    /// bytes read from disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The distinction is the entire point.</b> Hashing file bytes would
    /// make reindenting a column, editing a comment or changing a line ending
    /// retire every stored record that pinned the old hash -- so the hash would
    /// stop meaning "the numbers changed" and start meaning "somebody touched
    /// the file", which is a signal nobody can act on and everybody learns to
    /// override. Folding the <i>parsed</i> integers in field order means a real
    /// tuning change moves the hash and nothing else does.
    /// </para>
    /// <para>
    /// The algorithm is FNV-1a, 64-bit, absorbing eight little-endian bytes per
    /// value. It is chosen for being specified rather than for being strong:
    /// <see cref="System.Security.Cryptography"/> is on the banned list because
    /// nothing in the simulation may reach for a platform-provided primitive,
    /// and FNV-1a is nine lines of integer arithmetic that produce identical
    /// results under Mono, IL2CPP and CoreCLR. This is a change detector, not a
    /// defence against a forger; the record format's own signature story is a
    /// separate question and not this one.
    /// </para>
    /// <para>
    /// Every fold starts from a <b>label</b> that names both the table and its
    /// field layout -- <c>"unit-types/1"</c>, <c>"hex-map/1"</c>. Two
    /// consequences follow, and both are wanted. Tables with coincidentally
    /// equal numbers cannot collide. And moving, adding or removing a column is
    /// a layout change that bumps the digit in the label, so every record
    /// pinned to the old layout is retired loudly rather than silently
    /// reinterpreted against a shifted field order.
    /// </para>
    /// </remarks>
    public readonly struct Hash64 : IEquatable<Hash64>
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private Hash64(ulong value)
        {
            Value = value;
        }

        /// <summary>The accumulated digest.</summary>
        public ulong Value { get; }

        /// <summary>
        /// Reads a digest back from the number it is. This is how a recorded
        /// hash re-enters the simulation to be compared against a live one; it
        /// is not a way to start a fold, which always begins from a label.
        /// </summary>
        public static Hash64 FromValue(ulong value) => new Hash64(value);

        /// <summary>
        /// Begins a fold, absorbing the label that names the table and its
        /// field layout. The label must be printable ASCII: a fold that
        /// depended on how a runtime encodes a character outside that range
        /// would be a fold that can differ between machines.
        /// </summary>
        public static Hash64 Start(string label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (label.Length == 0)
            {
                throw new ArgumentException("A hash label may not be empty.", nameof(label));
            }

            var hash = new Hash64(OffsetBasis);

            for (int index = 0; index < label.Length; index++)
            {
                char character = label[index];

                if (character < ' ' || character > '~')
                {
                    throw new ArgumentException(
                        "A hash label must be printable ASCII, so that the fold cannot depend on how a "
                        + "runtime encodes a character. Offending character at index "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " has code point "
                        + ((int)character).ToString(CultureInfo.InvariantCulture)
                        + ".",
                        nameof(label));
                }

                hash = hash.Absorb((byte)character);
            }

            return hash;
        }

        /// <summary>Folds one value in, as eight little-endian bytes.</summary>
        /// <remarks>
        /// The arithmetic is written out here rather than looping over
        /// <see cref="Absorb"/> because this is the simulation's hottest call
        /// site by an order of magnitude -- the rolling state hash folds a few
        /// dozen values every tick for the length of a match, and the committed
        /// configuration is Debug, where a method call is a method call. Eight
        /// calls and eight intermediate structs per value cost more than the
        /// whole rest of the tick loop put together. The result is identical to
        /// absorbing the eight bytes one at a time, which the tests pin.
        /// </remarks>
        public Hash64 Add(long value)
        {
            ulong bits = unchecked((ulong)value);
            ulong hash = Value;

            unchecked
            {
                hash = (hash ^ (byte)bits) * Prime;
                hash = (hash ^ (byte)(bits >> 8)) * Prime;
                hash = (hash ^ (byte)(bits >> 16)) * Prime;
                hash = (hash ^ (byte)(bits >> 24)) * Prime;
                hash = (hash ^ (byte)(bits >> 32)) * Prime;
                hash = (hash ^ (byte)(bits >> 40)) * Prime;
                hash = (hash ^ (byte)(bits >> 48)) * Prime;
                hash = (hash ^ (byte)(bits >> 56)) * Prime;
            }

            return new Hash64(hash);
        }

        /// <summary>Folds one value in, widened to the same eight bytes.</summary>
        public Hash64 Add(int value) => Add((long)value);

        /// <summary>
        /// Folds a pair of 32-bit values in as one eight-byte word, high half
        /// first.
        /// </summary>
        /// <remarks>
        /// Two fields per fold rather than one, for the same reason
        /// <see cref="Add(long)"/> is written out: the rolling state hash runs
        /// once a tick for the length of a match and this halves what it costs.
        /// It is not a weaker fold -- both values reach every byte of the digest
        /// -- but it is a different one, so a field that moves between the high
        /// and low halves is a layout change and bumps the fold's label.
        /// </remarks>
        public Hash64 Add(int high, int low) =>
            Add(unchecked(((long)high << 32) | (long)(uint)low));

        /// <summary>
        /// Folds a range of bytes in, one octet at a time. This is plain FNV-1a
        /// over those bytes and nothing else.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the one place hashing bytes is right, and the remarks on
        /// <see cref="Hash64"/> explain why it is wrong everywhere else.</b> A
        /// content hash folds parsed integers because the bytes it would
        /// otherwise fold are authored text, where reindenting a column is not a
        /// change to anything. A record id folds bytes because the bytes
        /// <i>are</i> the record: the format has one writer, fixed-width fields
        /// and a canonical array order, so two records with the same meaning have
        /// the same bytes by construction and there is no formatting left for a
        /// byte fold to be fooled by.
        /// </para>
        /// </remarks>
        public Hash64 Add(byte[] bytes, int start, int count)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (start < 0 || count < 0 || start > bytes.Length - count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(start),
                    "["
                    + start.ToString(CultureInfo.InvariantCulture)
                    + ", "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + ") is not inside an array of "
                    + bytes.Length.ToString(CultureInfo.InvariantCulture)
                    + " bytes.");
            }

            ulong hash = Value;

            unchecked
            {
                for (int index = start; index < start + count; index++)
                {
                    hash = (hash ^ bytes[index]) * Prime;
                }
            }

            return new Hash64(hash);
        }

        public static bool operator ==(Hash64 a, Hash64 b) => a.Value == b.Value;

        public static bool operator !=(Hash64 a, Hash64 b) => a.Value != b.Value;

        public bool Equals(Hash64 other) => Value == other.Value;

        public override bool Equals(object? obj) => obj is Hash64 other && Value == other.Value;

        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Sixteen uppercase hexadecimal digits, invariant.</summary>
        public override string ToString() => Value.ToString("X16", CultureInfo.InvariantCulture);

        private Hash64 Absorb(byte octet) => new Hash64(unchecked((Value ^ octet) * Prime));
    }
}
