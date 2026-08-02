namespace Sim.Poison
{
    /// <summary>
    /// Row 5: ambient time and randomness. <c>System.Random</c> is the sharp
    /// end of the row -- a simulation with its own seeded stream that also
    /// reaches for the ambient one produces a replay that is right until it
    /// is not, with nothing in the record to point at.
    /// </summary>
    /// <remarks>
    /// The clock is on this same row and is banned by the same table entry
    /// kind: <c>System.DateTime</c>, <c>System.DateTimeOffset</c>,
    /// <c>System.TimeZoneInfo</c>, <c>System.Diagnostics.Stopwatch</c> and
    /// <c>System.Environment::get_TickCount</c> are all listed. Only one
    /// violation is poisoned per row, so those entries are data the clause
    /// reads rather than clauses of their own.
    /// </remarks>
    public static class PoisonAmbient
    {
        public static int Draw()
        {
            var dice = new System.Random();
            return dice.Next();
        }
    }
}
