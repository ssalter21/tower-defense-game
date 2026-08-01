namespace Sim.Poison
{
    /// <summary>
    /// Row 1: floating point. One method, breaking the rule in all three
    /// places a float can hide, because the scan needs three different pieces
    /// of code to find them and each has to be watched working.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Signature</b> -- the parameter and the return type are
    /// <c>double</c>, which is metadata and never appears in the
    /// type-reference table, so a scan that only walks references sees
    /// nothing here at all.
    /// </description></item>
    /// <item><description>
    /// <b>Local slot</b> -- <c>scaled</c> occupies a local of type
    /// <c>float64</c> in the method body's standalone signature.
    /// </description></item>
    /// <item><description>
    /// <b>Instruction stream</b> -- the literal compiles to <c>ldc.r8</c> and
    /// the multiply to a floating-point <c>mul</c>.
    /// </description></item>
    /// </list>
    /// </remarks>
    public static class PoisonFloats
    {
        public static double Scale(double factor)
        {
            double scaled = factor * 1.5;
            return scaled;
        }
    }
}
