namespace View
{
    /// <summary>
    /// The view side of the seam: which simulation this client is linked
    /// against, answered by the simulation itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type exists to be the smallest thing that crosses the boundary, so
    /// that the boundary is a fact about the build rather than a diagram. The
    /// simulation is a managed plug-in -- <c>Packages/com.ssalter.sim/Runtime/Sim.dll</c>,
    /// compiled by the .NET SDK outside Unity -- and its Auto Reference is off,
    /// so no assembly in this project can see <c>Sim</c> without saying so.
    /// <c>View.asmdef</c> says so, in two lines of JSON that show up in a diff:
    /// <c>"overrideReferences": true</c> and <c>"precompiledReferences": ["Sim.dll"]</c>.
    /// Delete those and this file stops compiling -- the reference is load-bearing,
    /// not decorative.
    /// </para>
    /// <para>
    /// Both numbers below are read from the assembly rather than restated here.
    /// A hand-copied constant on the view side would be a third place the sim's
    /// identity is written down, and the whole point of the record format's
    /// three identity fields is that each one has exactly one owner.
    /// </para>
    /// </remarks>
    public static class SimPlugin
    {
        /// <summary>
        /// The behaviour version of the linked simulation -- tick order,
        /// targeting, rounding, dice. Owned by <c>Sim.SimulationVersion</c>.
        /// </summary>
        public static uint Version => Sim.SimulationVersion.Current;

        /// <summary>
        /// The layout version of the ghost record this simulation reads and
        /// writes. Owned by <c>Sim.RecordFormat</c>.
        /// </summary>
        public static int GhostRecordVersion => Sim.RecordFormat.GhostVersion;

        /// <summary>
        /// One line naming the simulation the client is running, for a log or a
        /// corner of the screen. Nothing decides anything from this string.
        /// </summary>
        public static string Describe() =>
            "simulation v" + Version + ", ghost record v" + GhostRecordVersion;
    }
}
