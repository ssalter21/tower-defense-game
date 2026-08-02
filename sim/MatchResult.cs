using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What a finished match was: how many got through, out of how many, in how
    /// long, and the hash of everything that happened on the way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four numbers, and the fourth is the one that matters. <see cref="Leaked"/>
    /// out of <see cref="Total"/> is the outcome a person reads; the rolling
    /// state hash is the outcome a machine compares, and two runs that agree on
    /// the first three and disagree on the fourth have desynced in a field
    /// nobody was looking at.
    /// </para>
    /// <para>
    /// There is nothing here about who won. The skeleton has no scoring rule,
    /// and inventing one would be inventing a game design decision inside a
    /// result type.
    /// </para>
    /// </remarks>
    public readonly struct MatchResult
    {
        internal MatchResult(int leaked, int total, int finalTick, Hash64 rollingStateHash)
        {
            Leaked = leaked;
            Total = total;
            FinalTick = finalTick;
            RollingStateHash = rollingStateHash;
        }

        /// <summary>How many creeps reached the exit.</summary>
        public int Leaked { get; }

        /// <summary>How many the wave sent in total.</summary>
        public int Total { get; }

        /// <summary>The tick the last of them stopped existing on.</summary>
        public int FinalTick { get; }

        /// <summary>
        /// The rolling hash of internal simulation state, folded once per tick
        /// from the first tick to this one. Always computed; there is no
        /// configuration in which it is not.
        /// </summary>
        public Hash64 RollingStateHash { get; }

        public override string ToString() =>
            Leaked.ToString(CultureInfo.InvariantCulture)
            + " of "
            + Total.ToString(CultureInfo.InvariantCulture)
            + " leaked by tick "
            + FinalTick.ToString(CultureInfo.InvariantCulture)
            + ", state "
            + RollingStateHash.ToString();
    }
}
