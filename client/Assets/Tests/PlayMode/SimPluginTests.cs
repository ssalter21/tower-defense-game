using NUnit.Framework;
using Sim;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The simulation is a managed plug-in built outside Unity, and until
    /// something inside a play session actually calls it, "the client ships the
    /// bytes the determinism run hashed" is a claim about a file on disk rather
    /// than about a running game.
    ///
    /// These tests are the first thing in the project that loads Sim.dll into
    /// the engine's own runtime and asks it to compute something. They are
    /// deliberately about the seam, not about the simulation: every rule in
    /// here is already covered by sim.tests under the .NET toolchain, and the
    /// only new information is that the same assembly answers the same way when
    /// Unity is the host.
    /// </summary>
    public class SimPluginTests
    {
        [Test]
        public void TheSimulationAssemblyRunsInsideTheEngine()
        {
            // Axial neighbour zero is (q + 1, r), fixed by the simulation and
            // not by anything about how a hex is drawn.
            var neighbour = new Hex(3, -2).Neighbour(0);

            Assert.That(neighbour.Q, Is.EqualTo(4));
            Assert.That(neighbour.R, Is.EqualTo(-2));

            // Fixed-point division, on the engine's Mono runtime rather than
            // the SDK's. Three halves is 1.5, exactly, in Q32.32.
            var threeHalves = Fix64.FromInt(3) / Fix64.FromInt(2);

            Assert.That(threeHalves.Raw, Is.EqualTo(3L << 31));
        }

        [Test]
        public void TheViewReadsTheSimulationsOwnVersionNumbers()
        {
            Assert.That(SimPlugin.Version, Is.EqualTo(SimulationVersion.Current));
            Assert.That(SimPlugin.GhostRecordVersion, Is.EqualTo(RecordFormat.GhostVersion));

            Assert.That(
                SimPlugin.Describe(),
                Is.EqualTo("simulation v" + SimulationVersion.Current +
                           ", ghost record v" + RecordFormat.GhostVersion));
        }
    }
}
