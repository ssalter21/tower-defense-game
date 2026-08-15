namespace Sim
{
    /// <summary>
    /// The version of the simulation's behaviour: tick order, targeting, the
    /// rounding rule, the dice algorithm. The only one of a record's three
    /// identity fields set by hand, and changing it retires every record made
    /// under an earlier value.
    /// See <c>docs/adr/0009-three-identity-fields.md</c>.
    /// </summary>
    public static class SimulationVersion
    {
        public const uint Current = 6;
    }
}
