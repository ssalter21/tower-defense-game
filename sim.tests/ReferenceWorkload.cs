namespace Sim.Tests;

/// <summary>
/// A fixed amount of arithmetic whose only job is to say how fast the machine
/// running the tests is, right now, under whatever load it is under.
/// </summary>
/// <remarks>
/// <para>
/// This exists so <see cref="BudgetTests"/> can express the re-simulation
/// budget as "some multiple of what this machine costs" rather than as a
/// number of milliseconds. A hard-coded millisecond budget is a statement
/// about one laptop, and continuous integration does not run on that laptop.
/// </para>
/// <para>
/// <b>It deliberately does not call into the simulation.</b> The obvious
/// version of this -- calibrate against <c>Fix64</c>, since that is what the
/// tick loop is made of -- fails in the one case the budget exists for: a
/// change that made <c>Fix64.Mul</c> slower would slow the reference and the
/// match by the same factor, the ratio between them would not move, and the
/// test would stay green through exactly the regression it is there to catch.
/// The reference has to be independent of the code under test, so it is
/// written here out of nothing but <c>long</c> arithmetic and one array.
/// </para>
/// <para>
/// The shape is chosen to track the tick loop rather than to be pretty: 64-bit
/// multiply, shift and mask, which is what Q32.32 fixed point compiles into,
/// over a working set of <see cref="Lanes"/> longs -- 32 KB, small enough to
/// live in cache the way the creep and tower lists do. A pure register-bound
/// loop would measure a machine's ALU and miss the memory system the match
/// actually spends time in.
/// </para>
/// <para>
/// Every operation is integer, so <see cref="Checksum"/> is the same number on
/// every runtime that implements two's-complement 64-bit integers, for the
/// same reason <c>Fix64</c> is. That is what lets the work be verified rather
/// than assumed: a loop the optimiser deleted would still return quickly, and
/// a budget calibrated against a deleted loop is zero.
/// </para>
/// </remarks>
public static class ReferenceWorkload
{
    /// <summary>
    /// How many iterations one call runs. Sized on 6 Aug 2026 so the reference
    /// costs about what the match costs on the machine it was calibrated on --
    /// 2.77 ms against the match's 2.75 ms, both Debug, both medians of twelve
    /// (Windows 11, x64, .NET 10). Keeping the two within a factor of two of
    /// each other is what makes their ratio a stable statistic rather than a
    /// small number divided by a large one.
    /// </summary>
    public const int Iterations = 440_000;

    /// <summary>
    /// What <see cref="Churn"/> returns for <see cref="Iterations"/>. Not a
    /// magic number to be regenerated when it fails: if this moves, the workload
    /// changed, and every calibration taken against the old one is void.
    /// </summary>
    public const long Checksum = -4390160875024291372L;

    /// <summary>
    /// The working set, in longs. 4096 * 8 bytes = 32 KB, which is L1-resident
    /// on every runner class in the matrix.
    /// </summary>
    private const int Lanes = 4096;

    private const long Golden = unchecked((long)0x9E3779B97F4A7C15UL);

    /// <summary>
    /// Churns through the workload and returns its accumulator, which the caller
    /// is expected to check. The return value is the whole reason the optimiser
    /// cannot remove the loop.
    /// </summary>
    /// <remarks>
    /// Not called <c>Run</c>, because the independence check above it matches
    /// every exported simulation type name as a word against this file's source
    /// -- and a run is one of the things the simulation exports.
    /// </remarks>
    public static long Churn(int iterations)
    {
        long[] lanes = new long[Lanes];

        for (int lane = 0; lane < Lanes; lane++)
        {
            lanes[lane] = lane * Golden;
        }

        long accumulator = 1L;

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            // The index depends on the accumulator, so the walk cannot be
            // hoisted, prefetched away or reordered into something cheaper.
            int slot = (int)((uint)(accumulator ^ iteration) % Lanes);
            long value = lanes[slot] + accumulator;

            long high = (value >> 32) * (value & 0xFFFF_FFFFL);
            long low = (value & 0xFFFF_FFFFL) * ((value >> 16) & 0xFFFF_FFFFL);

            accumulator = (accumulator ^ (high + (low >> 32))) + Golden;
            lanes[slot] = accumulator;
        }

        return accumulator;
    }
}
