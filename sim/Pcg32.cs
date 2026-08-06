using System;

namespace Sim
{
    /// <summary>
    /// The simulation's dice: one seeded PCG-XSH-RR 64/32 stream, and the only
    /// source of randomness the simulation has. One stream, one input -- the
    /// seed carried by the record -- and no stream selector.
    /// See <c>docs/adr/0031-one-rng-stream-no-ambient-nondeterminism.md</c>.
    /// </summary>
    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;

        /// <summary>The single stream's increment. Fixed, odd as PCG requires, and not configurable.</summary>
        private const ulong Increment = 1442695040888963407UL;

        private ulong _state;

        /// <summary>Creates the stream for a seed. The same seed always yields the same sequence.</summary>
        public Pcg32(ulong seed)
        {
            Seed = seed;
            _state = 0UL;
            Advance();
            _state = unchecked(_state + seed);
            Advance();
        }

        /// <summary>The seed this stream was constructed from.</summary>
        public ulong Seed { get; }

        /// <summary>The stream's position. Folded into the rolling state hash.</summary>
        public ulong State => _state;

        /// <summary>
        /// The next 32 bits of the stream: the previous state xorshift-folded to
        /// 32 bits, then rotated by its own top five bits.
        /// </summary>
        public uint NextUInt()
        {
            ulong previous = _state;
            Advance();

            uint xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
            int rotation = (int)(previous >> 59);
            return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
        }

        /// <summary>
        /// A uniform value in <c>[0, bound)</c>, with no modulo bias. Draws
        /// below the rejection threshold <c>2^32 mod bound</c> -- computed in
        /// unsigned arithmetic as <c>(0 - bound) % bound</c> -- are discarded and
        /// redrawn. The discarded band is always smaller than <c>bound</c>, so
        /// the loop takes under two draws on average.
        /// </summary>
        public uint NextBelow(uint bound)
        {
            if (bound == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "Pcg32.NextBelow needs a bound above zero.");
            }

            uint threshold = unchecked(0U - bound) % bound;

            while (true)
            {
                uint draw = NextUInt();
                if (draw >= threshold)
                {
                    return draw % bound;
                }
            }
        }

        /// <summary>A uniform value in <c>[minInclusive, maxExclusive)</c>.</summary>
        public int NextInRange(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    "Pcg32.NextInRange needs maxExclusive above minInclusive.");
            }

            uint span = unchecked((uint)((long)maxExclusive - minInclusive));
            return unchecked((int)(minInclusive + (long)NextBelow(span)));
        }

        /// <summary>
        /// A uniform fixed-point value in <c>[0, 1)</c> with 32 bits of
        /// resolution, which is every value Q32.32 can represent below one.
        /// </summary>
        public Fix64 NextUnitInterval() => Fix64.FromRaw(NextUInt());

        /// <summary>Steps the 64-bit LCG once.</summary>
        private void Advance()
        {
            _state = unchecked((_state * Multiplier) + Increment);
        }
    }
}
