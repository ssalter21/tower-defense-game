using System.Collections.Generic;

namespace Sim.Poison
{
    /// <summary>
    /// Row 3: hashed collections. Enumerating a <c>Dictionary</c> yields an
    /// order that is an implementation detail and has changed between
    /// runtimes, so a tick loop that iterates one is a tick loop whose order
    /// the record does not pin.
    /// </summary>
    public static class PoisonHashedCollections
    {
        public static int Count()
        {
            var lookup = new Dictionary<int, int>();
            lookup[1] = 2;
            return lookup.Count;
        }
    }
}
