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
        public Hash64 Add(long value)
        {
            ulong bits = unchecked((ulong)value);
            Hash64 hash = this;

            for (int shift = 0; shift < 64; shift += 8)
            {
                hash = hash.Absorb((byte)(bits >> shift));
            }

            return hash;
        }

        /// <summary>Folds one value in, widened to the same eight bytes.</summary>
        public Hash64 Add(int value) => Add((long)value);

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
