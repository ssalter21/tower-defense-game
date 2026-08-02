using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Q32.32 fixed point: a signed 64-bit integer holding 32 integer bits and
    /// 32 fractional bits, so one whole unit is 2^32 raw.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type exists because C# does not promise that a <c>float</c> or
    /// <c>double</c> expression is evaluated at the precision of its declared
    /// type. The specification explicitly permits an implementation to compute
    /// floating-point operations "with a greater range and/or precision than
    /// the type", which makes the result of a float expression a property of
    /// the machine and the JIT rather than of the program. A replay that has
    /// to reproduce a match years later cannot be built on that.
    /// </para>
    /// <para>
    /// Every operation here is integer arithmetic on <see cref="Raw"/> and is
    /// therefore bit-for-bit identical on every runtime that implements
    /// two's-complement 64-bit integers -- which every runtime does, because
    /// unlike floating point the integer semantics are mandated.
    /// </para>
    /// <para>
    /// <b>Rounding is truncation toward zero</b>, for multiplication and
    /// division alike. That choice is arithmetic, so it belongs to the
    /// simulation version: changing it changes replays even though no number
    /// in any record moved.
    /// </para>
    /// <para>
    /// <b>Overflow throws.</b> The invariants of this simulation are
    /// unconditional throws rather than assertions, because an assertion
    /// compiles out of the configuration that ships and a silently wrapped
    /// result desyncs a replay months later with nothing to point at.
    /// </para>
    /// </remarks>
    public readonly struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
    {
        /// <summary>Number of fractional bits. The "32" after the point in Q32.32.</summary>
        public const int FractionalBits = 32;

        private const long OneRaw = 1L << FractionalBits;
        private const ulong FractionMask = 0xFFFF_FFFFUL;

        /// <summary>The underlying scaled integer. Value = <c>Raw / 2^32</c>.</summary>
        public long Raw { get; }

        private Fix64(long raw)
        {
            Raw = raw;
        }

        /// <summary>Zero.</summary>
        public static Fix64 Zero => new Fix64(0);

        /// <summary>One.</summary>
        public static Fix64 One => new Fix64(OneRaw);

        /// <summary>The smallest representable positive value, <c>2^-32</c>.</summary>
        public static Fix64 Epsilon => new Fix64(1);

        /// <summary>The most negative representable value.</summary>
        public static Fix64 MinValue => new Fix64(long.MinValue);

        /// <summary>The largest representable value.</summary>
        public static Fix64 MaxValue => new Fix64(long.MaxValue);

        /// <summary>Reinterprets a scaled integer as a fixed-point value.</summary>
        public static Fix64 FromRaw(long raw) => new Fix64(raw);

        /// <summary>
        /// Exact conversion from a whole number. Every <see cref="int"/> is
        /// representable -- <c>int.MinValue</c> scales to exactly
        /// <c>long.MinValue</c> -- so this cannot overflow.
        /// </summary>
        public static Fix64 FromInt(int value) => new Fix64((long)value << FractionalBits);

        /// <summary>
        /// Exact conversion from a ratio, rounded toward zero. This is how a
        /// constant such as "three tenths" enters the simulation: as
        /// <c>Fix64.FromRatio(3, 10)</c> and never as a decimal literal, which
        /// would have to be a <c>double</c> and would be caught by the IL scan.
        /// </summary>
        public static Fix64 FromRatio(int numerator, int denominator) =>
            FromInt(numerator) / FromInt(denominator);

        /// <summary>The whole part, truncated toward zero.</summary>
        public int ToIntTowardZero()
        {
            ulong whole = Magnitude(Raw) >> FractionalBits;

            if (Raw < 0)
            {
                if (whole > (ulong)int.MaxValue + 1UL)
                {
                    throw new OverflowException("Fix64 does not fit in an int: " + ToString());
                }

                return unchecked((int)(0L - (long)whole));
            }

            if (whole > int.MaxValue)
            {
                throw new OverflowException("Fix64 does not fit in an int: " + ToString());
            }

            return (int)whole;
        }

        /// <summary>The largest whole number not greater than this value.</summary>
        public int ToIntFloor()
        {
            long whole = Raw >> FractionalBits; // arithmetic shift already floors
            if (whole < int.MinValue || whole > int.MaxValue)
            {
                throw new OverflowException("Fix64 does not fit in an int: " + ToString());
            }

            return (int)whole;
        }

        /// <summary>The fractional part, always in <c>[0, 1)</c>, even when negative.</summary>
        public Fix64 Fraction() => new Fix64(unchecked((long)((ulong)Raw & FractionMask)));

        /// <summary>Magnitude.</summary>
        /// <exception cref="OverflowException"><see cref="MinValue"/> has no positive counterpart.</exception>
        public Fix64 Abs()
        {
            if (Raw == long.MinValue)
            {
                throw new OverflowException("Fix64.Abs overflowed: MinValue has no positive counterpart.");
            }

            return new Fix64(Raw < 0 ? -Raw : Raw);
        }

        /// <summary>The smaller of two values.</summary>
        public static Fix64 Min(Fix64 a, Fix64 b) => a.Raw <= b.Raw ? a : b;

        /// <summary>The larger of two values.</summary>
        public static Fix64 Max(Fix64 a, Fix64 b) => a.Raw >= b.Raw ? a : b;

        /// <summary>Sign: -1, 0 or 1.</summary>
        public int Sign() => Raw < 0 ? -1 : (Raw > 0 ? 1 : 0);

        public static Fix64 operator +(Fix64 a, Fix64 b)
        {
            long sum = unchecked(a.Raw + b.Raw);
            // Overflow iff both operands share a sign that the result does not.
            if (((a.Raw ^ sum) & (b.Raw ^ sum)) < 0)
            {
                throw new OverflowException("Fix64 addition overflowed: " + a + " + " + b);
            }

            return new Fix64(sum);
        }

        public static Fix64 operator -(Fix64 a, Fix64 b)
        {
            long difference = unchecked(a.Raw - b.Raw);
            // Overflow iff the operands differ in sign and the result took b's.
            if (((a.Raw ^ b.Raw) & (a.Raw ^ difference)) < 0)
            {
                throw new OverflowException("Fix64 subtraction overflowed: " + a + " - " + b);
            }

            return new Fix64(difference);
        }

        public static Fix64 operator -(Fix64 value)
        {
            if (value.Raw == long.MinValue)
            {
                throw new OverflowException("Fix64 negation overflowed: MinValue has no positive counterpart.");
            }

            return new Fix64(-value.Raw);
        }

        /// <summary>
        /// Multiplication. The exact 128-bit product of the two raw values is
        /// formed from 32-bit limbs, then shifted right 32 to undo one factor
        /// of the scale. Truncating that shift is what makes rounding
        /// toward-zero rather than toward-negative-infinity: the shift is
        /// applied to the magnitude and the sign is reattached afterwards.
        /// </summary>
        public static Fix64 operator *(Fix64 a, Fix64 b)
        {
            bool negative = (a.Raw < 0) ^ (b.Raw < 0);
            ulong left = Magnitude(a.Raw);
            ulong right = Magnitude(b.Raw);

            Multiply64(left, right, out ulong high, out ulong low);

            // Shift the 128-bit product right by the fractional bit count.
            ulong magnitude = (low >> FractionalBits) | (high << (64 - FractionalBits));
            ulong overflowBits = high >> FractionalBits;

            if (overflowBits != 0 || magnitude > (negative ? (ulong)long.MaxValue + 1UL : (ulong)long.MaxValue))
            {
                throw new OverflowException("Fix64 multiplication overflowed: " + a + " * " + b);
            }

            return new Fix64(negative ? unchecked(-(long)magnitude) : (long)magnitude);
        }

        /// <summary>
        /// Division. The dividend's magnitude is scaled up by 2^32 into a
        /// 128-bit value and divided by the divisor's magnitude with a
        /// restoring shift-subtract loop, so the result is exact to the last
        /// representable bit and then truncated toward zero.
        /// </summary>
        public static Fix64 operator /(Fix64 a, Fix64 b)
        {
            if (b.Raw == 0)
            {
                throw new DivideByZeroException("Fix64 division by zero: " + a + " / 0");
            }

            bool negative = (a.Raw < 0) ^ (b.Raw < 0);
            ulong dividend = Magnitude(a.Raw);
            ulong divisor = Magnitude(b.Raw);

            // dividend << 32, as a 128-bit value.
            ulong high = dividend >> (64 - FractionalBits);
            ulong low = dividend << FractionalBits;

            if (high >= divisor)
            {
                throw new OverflowException("Fix64 division overflowed: " + a + " / " + b);
            }

            ulong magnitude = Divide128By64(high, low, divisor);

            if (magnitude > (negative ? (ulong)long.MaxValue + 1UL : (ulong)long.MaxValue))
            {
                throw new OverflowException("Fix64 division overflowed: " + a + " / " + b);
            }

            return new Fix64(negative ? unchecked(-(long)magnitude) : (long)magnitude);
        }

        public static bool operator ==(Fix64 a, Fix64 b) => a.Raw == b.Raw;

        public static bool operator !=(Fix64 a, Fix64 b) => a.Raw != b.Raw;

        public static bool operator <(Fix64 a, Fix64 b) => a.Raw < b.Raw;

        public static bool operator >(Fix64 a, Fix64 b) => a.Raw > b.Raw;

        public static bool operator <=(Fix64 a, Fix64 b) => a.Raw <= b.Raw;

        public static bool operator >=(Fix64 a, Fix64 b) => a.Raw >= b.Raw;

        public bool Equals(Fix64 other) => Raw == other.Raw;

        public override bool Equals(object? obj) => obj is Fix64 other && Raw == other.Raw;

        public override int GetHashCode() => Raw.GetHashCode();

        public int CompareTo(Fix64 other) => Raw.CompareTo(other.Raw);

        /// <summary>
        /// Decimal rendering to nine fractional digits, truncated. Built out
        /// of integer arithmetic and the invariant culture, because the
        /// obvious implementation -- convert to double and format -- would put
        /// a float in this assembly's instruction stream and fail the scan.
        /// </summary>
        public override string ToString()
        {
            const ulong DecimalScale = 1_000_000_000UL; // nine digits; ten would overflow the multiply

            bool negative = Raw < 0;
            ulong magnitude = Magnitude(Raw);
            ulong whole = magnitude >> FractionalBits;
            ulong fraction = magnitude & FractionMask;
            ulong digits = (fraction * DecimalScale) >> FractionalBits;

            return (negative ? "-" : string.Empty)
                + whole.ToString(CultureInfo.InvariantCulture)
                + "."
                + digits.ToString("D9", CultureInfo.InvariantCulture);
        }

        /// <summary>Two's-complement magnitude, correct for <see cref="long.MinValue"/>.</summary>
        private static ulong Magnitude(long value) =>
            value < 0 ? unchecked(0UL - (ulong)value) : (ulong)value;

        /// <summary>Exact 64x64 -> 128 unsigned multiply, from 32-bit limbs.</summary>
        private static void Multiply64(ulong left, ulong right, out ulong high, out ulong low)
        {
            ulong leftLow = left & FractionMask;
            ulong leftHigh = left >> 32;
            ulong rightLow = right & FractionMask;
            ulong rightHigh = right >> 32;

            ulong lowLow = leftLow * rightLow;
            ulong crossA = leftHigh * rightLow;
            ulong crossB = leftLow * rightHigh;
            ulong highHigh = leftHigh * rightHigh;

            ulong carry = crossA + (lowLow >> 32);
            ulong carryLow = carry & FractionMask;
            ulong carryHigh = carry >> 32;

            ulong merged = crossB + carryLow;

            low = (lowLow & FractionMask) | (merged << 32);
            high = highHigh + carryHigh + (merged >> 32);
        }

        /// <summary>
        /// Unsigned 128-by-64 division whose quotient is known to fit in 64
        /// bits. Restoring shift-subtract, one bit per iteration; the extra
        /// bit shifted out of the remainder is tracked explicitly because
        /// <c>remainder * 2</c> can exceed 64 bits even when the quotient does not.
        /// </summary>
        private static ulong Divide128By64(ulong high, ulong low, ulong divisor)
        {
            ulong quotient = 0;
            ulong remainder = high;

            for (int bit = 63; bit >= 0; bit--)
            {
                ulong shiftedOut = remainder >> 63;
                remainder = (remainder << 1) | ((low >> bit) & 1UL);

                if (shiftedOut != 0 || remainder >= divisor)
                {
                    remainder -= divisor;
                    quotient |= 1UL << bit;
                }
            }

            return quotient;
        }
    }
}
