namespace Sim.Poison
{
    /// <summary>
    /// Row 6: threading. Interleaving is a source of nondeterminism no seed
    /// can pin, and a tick loop that wants a thread has stopped being a tick
    /// loop. The whole <c>System.Threading</c> namespace is out of bounds
    /// rather than a listed set of types, because the hazard is the area and
    /// not the particular entry point.
    /// </summary>
    public static class PoisonThreading
    {
        public static void Yield()
        {
            System.Threading.Thread.Sleep(0);
        }
    }
}
