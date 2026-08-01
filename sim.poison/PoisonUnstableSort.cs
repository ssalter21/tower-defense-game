namespace Sim.Poison
{
    /// <summary>
    /// Row 4: unstable sorts. <c>Array.Sort</c> is documented as not
    /// preserving the order of equal elements, which turns every tie in the
    /// simulation -- and target selection is nothing but ties -- into a coin
    /// flip the replay cannot reproduce.
    /// </summary>
    /// <remarks>
    /// This one is also the scan's only exercise of a generic call: the ban is
    /// written against <c>System.Array::Sort</c>, and the token at the call
    /// site is a method specification that has to be followed one hop to the
    /// member reference underneath. A scan that stopped at the token would see
    /// nothing here.
    /// </remarks>
    public static class PoisonUnstableSort
    {
        public static int[] Ordered(int[] values)
        {
            System.Array.Sort(values);
            return values;
        }
    }
}
