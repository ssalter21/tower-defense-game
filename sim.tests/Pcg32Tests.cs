using Sim;

namespace Sim.Tests;

/// <summary>
/// The dice: the same seed reproduces the same sequence, and the sequence is
/// the one PCG-XSH-RR 64/32 specifies rather than merely a repeatable one.
/// </summary>
/// <remarks>
/// The correctness chain here has two links, because a golden sequence copied
/// out of a run only ever proves that nothing has changed since the run. So:
/// <list type="number">
/// <item><description>
/// <see cref="ReferencePcg32"/> is an independent transcription of the
/// published algorithm, written from the specification and not from
/// <c>Sim.Pcg32</c>. It is checked against O'Neill's own demo output for
/// seed 42 and stream 54, which is the vector the reference implementation
/// ships and which no bug in this repository can influence.
/// </description></item>
/// <item><description>
/// The simulation's stream is then checked against that reference. A
/// transcription slip in either one shows up as a disagreement, and neither
/// can drag the other along with it.
/// </description></item>
/// </list>
/// </remarks>
public class Pcg32Tests
{
    /// <summary>
    /// The fixed increment <see cref="Pcg32"/> uses. It is a constant of the
    /// simulation, not a parameter, and it is written out here so the
    /// reference implementation can be pointed at the same stream. If this
    /// ever stops matching, every record ever written replays differently and
    /// the simulation version has to move.
    /// </summary>
    private const ulong SimIncrement = 1442695040888963407UL;

    [Fact]
    public void The_reference_implementation_reproduces_the_published_vector()
    {
        // pcg32-demo, seeded pcg32_srandom_r(&rng, 42u, 54u), first six draws.
        var reference = ReferencePcg32.FromSeedAndStream(42UL, 54UL);

        Assert.Equal(0xa15c02b7U, reference.Next());
        Assert.Equal(0x7b47f409U, reference.Next());
        Assert.Equal(0xba1d3330U, reference.Next());
        Assert.Equal(0x83d2f293U, reference.Next());
        Assert.Equal(0xbfa4784bU, reference.Next());
        Assert.Equal(0xcbed606eU, reference.Next());
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(42UL)]
    [InlineData(ulong.MaxValue)]
    public void The_simulation_stream_is_the_algorithm_the_reference_implements(ulong seed)
    {
        var simulation = new Pcg32(seed);
        var reference = ReferencePcg32.FromSeedAndIncrement(seed, SimIncrement);

        for (int draw = 0; draw < 64; draw++)
        {
            Assert.Equal(reference.Next(), simulation.NextUInt());
        }
    }

    [Fact]
    public void The_same_seed_reproduces_the_same_sequence()
    {
        var first = new Pcg32(20260801UL);
        var second = new Pcg32(20260801UL);

        for (int draw = 0; draw < 1000; draw++)
        {
            Assert.Equal(first.NextUInt(), second.NextUInt());
        }

        Assert.Equal(first.State, second.State);
    }

    [Fact]
    public void A_different_seed_gives_a_different_sequence()
    {
        var first = new Pcg32(1UL);
        var second = new Pcg32(2UL);

        bool differed = false;
        for (int draw = 0; draw < 16 && !differed; draw++)
        {
            differed = first.NextUInt() != second.NextUInt();
        }

        Assert.True(differed, "Two different seeds produced the same first sixteen draws.");
    }

    [Fact]
    public void The_stream_position_is_visible_because_the_state_hash_needs_it()
    {
        var stream = new Pcg32(7UL);
        ulong before = stream.State;

        stream.NextUInt();

        Assert.NotEqual(before, stream.State);
        Assert.Equal(7UL, stream.Seed);
    }

    [Fact]
    public void A_bounded_draw_stays_in_range_and_covers_it()
    {
        var stream = new Pcg32(99UL);
        var seen = new bool[6];

        for (int draw = 0; draw < 10000; draw++)
        {
            uint value = stream.NextBelow(6);
            Assert.InRange(value, 0U, 5U);
            seen[value] = true;
        }

        Assert.DoesNotContain(false, seen);
    }

    [Fact]
    public void A_bound_of_one_is_always_zero_and_a_bound_of_zero_throws()
    {
        var stream = new Pcg32(3UL);

        for (int draw = 0; draw < 100; draw++)
        {
            Assert.Equal(0U, stream.NextBelow(1));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextBelow(0));
    }

    [Fact]
    public void A_ranged_draw_spans_negatives_and_rejects_an_empty_range()
    {
        var stream = new Pcg32(11UL);

        for (int draw = 0; draw < 1000; draw++)
        {
            Assert.InRange(stream.NextInRange(-3, 4), -3, 3);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextInRange(0, 0));
    }

    [Fact]
    public void The_unit_interval_draw_is_fixed_point_and_stays_below_one()
    {
        var stream = new Pcg32(5UL);

        for (int draw = 0; draw < 1000; draw++)
        {
            Fix64 value = stream.NextUnitInterval();
            Assert.True(value >= Fix64.Zero, "unit-interval draw went negative: " + value);
            Assert.True(value < Fix64.One, "unit-interval draw reached one: " + value);
        }
    }

    /// <summary>
    /// PCG-XSH-RR 64/32, transcribed from the published algorithm. This is a
    /// second opinion, not a copy: it is here so that the simulation's stream
    /// has something to be wrong against.
    /// </summary>
    private sealed class ReferencePcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;

        private ulong _increment;

        public static ReferencePcg32 FromSeedAndStream(ulong seed, ulong stream) =>
            FromSeedAndIncrement(seed, (stream << 1) | 1UL);

        public static ReferencePcg32 FromSeedAndIncrement(ulong seed, ulong increment)
        {
            var generator = new ReferencePcg32 { _state = 0UL, _increment = increment };
            generator.Next();
            generator._state = unchecked(generator._state + seed);
            generator.Next();
            return generator;
        }

        public uint Next()
        {
            ulong previous = _state;
            _state = unchecked((previous * Multiplier) + _increment);

            uint xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
            int rotation = (int)(previous >> 59);
            return (xorshifted >> rotation) | (xorshifted << ((-rotation) & 31));
        }
    }
}
