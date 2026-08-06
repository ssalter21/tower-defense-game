namespace Sim
{
    /// <summary>
    /// The version of the simulation's behaviour: tick order, targeting, the
    /// rounding rule, the dice algorithm. One of the three identity fields a
    /// record carries, and the only one set by hand -- the record format version
    /// covers byte layout and the content hash covers the numbers.
    /// See <c>docs/adr/0009-three-identity-fields.md</c>.
    /// </summary>
    public static class SimulationVersion
    {
        /// <summary>
        /// The behaviour this build implements. Changing it retires every stored
        /// record made under an earlier value.
        /// </summary>
        public const uint Current = 1;
    }
}
