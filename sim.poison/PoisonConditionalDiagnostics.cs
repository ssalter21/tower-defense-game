namespace Sim.Poison
{
    /// <summary>
    /// Row 7: conditional-compilation diagnostics. <c>Debug.Assert</c> is
    /// marked <c>[Conditional("DEBUG")]</c>, so the call below exists in this
    /// image and would not exist in a Release one. That is the whole reason
    /// Debug is the committed configuration: the shipping build must be the
    /// one carrying the loud-failure architecture, and the only way this row
    /// is catchable in IL at all is that the call is actually emitted.
    /// </summary>
    /// <remarks>
    /// The rule this poisons is that the simulation's invariants are
    /// unconditional throws. An invariant written as an assertion is an
    /// invariant that vanishes from the configuration that ships, which
    /// converts an unenforced rule into a believed one.
    /// </remarks>
    public static class PoisonConditionalDiagnostics
    {
        public static void Check(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition, "poison");
        }
    }
}
