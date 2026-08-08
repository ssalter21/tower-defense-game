namespace Sim
{
    /// <summary>
    /// Taking some of a pool without replacement.
    /// </summary>
    /// <remarks>
    /// A partial Fisher-Yates over an array of positions: one draw per item
    /// taken, no hashed collection anywhere near it, and the same walk wherever
    /// it is used -- an anchor's menu and a round's offering both come out of
    /// here, so the two cannot drift into two shuffles that agree on
    /// everything except which item a seed lands on.
    /// </remarks>
    internal static class Draws
    {
        /// <summary>
        /// This many positions of a pool that size, without replacement, in the
        /// order they were drawn. The caller has already established that the
        /// pool is at least as large as the count, because what to say when it
        /// is not belongs to whoever knows what the pool is.
        /// </summary>
        internal static int[] Positions(Pcg32 dice, int poolSize, int count)
        {
            var positions = new int[poolSize];

            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = index;
            }

            var drawn = new int[count];

            for (int index = 0; index < count; index++)
            {
                int remaining = positions.Length - index;
                int picked = index + (int)dice.NextBelow((uint)remaining);

                int swap = positions[index];
                positions[index] = positions[picked];
                positions[picked] = swap;

                drawn[index] = positions[index];
            }

            return drawn;
        }
    }
}
