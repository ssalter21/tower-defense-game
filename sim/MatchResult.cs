using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What a finished match was: how many got through, out of how many, in how
    /// long, and the hash of everything that happened on the way. Carries no
    /// win or loss verdict.
    /// </summary>
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

        /// <summary>Renders as <c>"L of T leaked by tick N, state H"</c>.</summary>
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
