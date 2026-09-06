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
        internal MatchResult(int leaked, int total, int finalTick, int bounty, Hash64 rollingStateHash)
        {
            Leaked = leaked;
            Total = total;
            FinalTick = finalTick;
            Bounty = bounty;
            RollingStateHash = rollingStateHash;
        }

        /// <summary>How many creeps reached the exit.</summary>
        public int Leaked { get; }

        /// <summary>How many the wave sent in total.</summary>
        public int Total { get; }

        /// <summary>The tick the last of them stopped existing on.</summary>
        public int FinalTick { get; }

        /// <summary>
        /// What the kills paid the defense, in gold. The one number here that a
        /// build phase reads rather than reports: it is income earned inside the
        /// match, and it lands in the same purse the towers were bought out of.
        /// See <c>docs/adr/0060-a-kill-pays-the-defender.md</c>.
        /// </summary>
        public int Bounty { get; }

        /// <summary>Internal simulation state, folded once per tick from the first tick to this one.</summary>
        public Hash64 RollingStateHash { get; }

        public override string ToString() =>
            Leaked.ToString(CultureInfo.InvariantCulture)
            + " of "
            + Total.ToString(CultureInfo.InvariantCulture)
            + " leaked by tick "
            + FinalTick.ToString(CultureInfo.InvariantCulture)
            + ", "
            + Bounty.ToString(CultureInfo.InvariantCulture)
            + " gold paid, state "
            + RollingStateHash.ToString();
    }
}
