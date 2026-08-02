using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Thrown when the simulation itself has gone wrong, as opposed to when
    /// authored content will not load.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every invariant in this assembly is an unconditional throw</b>, and
    /// this is one of the two types they throw. Not an assertion, not a
    /// conditional-compilation macro, not a logged warning: the whole point of
    /// the arrangement is that there is no configuration in which the loud
    /// failure everything else rests on is switched off. An assertion compiles
    /// out of the build that ships, which is precisely the build a desync will
    /// be found in months later with nothing left to point at.
    /// </para>
    /// <para>
    /// The banned-API scan enforces the other half of this: <c>Debug.Assert</c>,
    /// <c>Trace</c> and <c>[Conditional]</c> are all refused inside this
    /// assembly, so the easy way to write a check that quietly disappears is not
    /// available.
    /// </para>
    /// </remarks>
    public class SimulationException : Exception
    {
        public SimulationException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown when a rolling per-tick state hash does not match the trace it was
    /// checked against, naming the tick it happened on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The tick is the point.</b> An end-of-match hash tells you that
    /// something diverged; this tells you when, which is the difference between
    /// a bug you can bisect and a bug you can only stare at. The comparison is
    /// per tick precisely so the first tick that disagrees is the one that gets
    /// named, before the divergence has propagated into everything else.
    /// </para>
    /// </remarks>
    public sealed class DesyncException : SimulationException
    {
        public DesyncException(int tick, Hash64 expected, Hash64 actual)
            : base(Describe(tick, expected, actual))
        {
            Tick = tick;
            Expected = expected;
            Actual = actual;
        }

        /// <summary>The tick the two runs first disagreed on.</summary>
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
