using System;

namespace Sim
{
    /// <summary>
    /// The simulation's dice: one seeded PCG-XSH-RR 64/32 stream, and the only
    /// source of randomness the simulation has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is exactly one stream and it takes exactly one input, the seed
    /// carried by the record. There is deliberately no stream selector: a
    /// second knob would be a second thing a replay has to reproduce, and the
    /// first time two subsystems disagreed about which stream they were on the
    /// symptom would be a desync with no bad line to point at.
    /// </para>
    /// <para>
    /// Nothing here reaches ambient nondeterminism -- no <c>System.Random</c>,
    /// no clock, no thread id, no hardware entropy. That is not a promise made
    /// in a comment: the IL scan over the compiled assembly rejects every one
    /// of those, and the poison project proves the scan can see them.
    /// </para>
    /// <para>
    /// <see cref="State"/> is exposed because the stream's position is part of
    /// the simulation's rolling state hash. Accumulated fixed-point
    /// remainders, stream position and target-selection tiebreaks are exactly
    /// the fields likeliest to desync and exactly the ones a view never sees,
    /// so they have to be hashed rather than compared through the snapshot.
    /// </para>
    /// <para>
    /// The generator is O'Neill's PCG-XSH-RR variant: a 64-bit LCG whose
    /// output is a xorshift-folded, randomly-rotated 32-bit word. It is chosen
    /// over a plain LCG because the low bits of an LCG are notoriously
    /// short-period, and over a hash-based generator because it needs no
    /// intrinsics and no table -- so the same nine lines of integer arithmetic
    /// run identically under Mono, IL2CPP and CoreCLR.
    /// </para>
    /// </remarks>
    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;

        /// <summary>
        /// The single stream's increment. Fixed, odd (PCG requires it), and
        /// never configurable -- see the remarks on <see cref="Pcg32"/>.
        /// </summary>
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

        /// <summary>
        /// The stream's position. Part of the rolling state hash; two runs
        /// that agree on every visible field but disagree here have already
        /// desynced and have not noticed yet.
        /// </summary>
        public ulong State => _state;

        /// <summary>The next 32 bits of the stream.</summary>
        public uint NextUInt()
        {
            ulong previous = _state;
            Advance();

            uint xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
            int rotation = (int)(previous >> 59);
            return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
        }

        /// <summary>
        /// A uniform value in <c>[0, bound)</c>, with no modulo bias.
        /// </summary>
        /// <remarks>
        /// The rejection threshold is <c>2^32 mod bound</c>, computed in
        /// unsigned arithmetic as <c>(0 - bound) % bound</c>. Draws below it
        /// are discarded, which removes the bias exactly rather than
        /// approximately. The loop terminates with probability 1 and, because
        /// the discarded band is always smaller than <c>bound</c>, in under
        /// two draws on average.
        /// </remarks>
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
        /// A uniform fixed-point value in <c>[0, 1)</c>, with 32 bits of
        /// resolution -- which is every value Q32.32 can represent below one,
        /// so the mapping loses nothing.
        /// </summary>
        public Fix64 NextUnitInterval() => Fix64.FromRaw(NextUInt());

        private void Advance()
        {
            _state = unchecked((_state * Multiplier) + Increment);
        }
    }
}
