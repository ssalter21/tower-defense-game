using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Thrown when the simulation itself has gone wrong, as opposed to when
    /// authored content will not load. The throw is unconditional in every build
    /// configuration.
    /// See <c>docs/adr/0025-invariants-are-unconditional-throws.md</c>.
    /// </summary>
    public class SimulationException : Exception
    {
        public SimulationException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown when a rolling per-tick state hash does not match the trace it was
    /// checked against, naming the first tick the two disagreed on.
    /// </summary>
    public sealed class DesyncException : SimulationException
    {
        public DesyncException(int tick, Hash64 expected, Hash64 actual)
            : base(Describe(tick, expected, actual))
        {
            Tick = tick;
            Expected = expected;
            Actual = actual;
        }

        public int Tick { get; }

        /// <summary>What the trace said the state was.</summary>
        public Hash64 Expected { get; }

        /// <summary>What this run's state actually was.</summary>
        public Hash64 Actual { get; }

        private static string Describe(int tick, Hash64 expected, Hash64 actual) =>
            "The simulation diverged from the golden trace at tick "
            + tick.ToString(CultureInfo.InvariantCulture)
            + ": the trace says "
            + expected.ToString()
            + " and this run says "
            + actual.ToString()
            + ". The hash covers internal state the snapshot never carries -- accumulated fixed-point "
            + "remainders, the position of the dice, and how target-selection ties were broken -- so a "
            + "difference here can be invisible on screen and still make every later tick a different "
            + "match.";
    }
}
