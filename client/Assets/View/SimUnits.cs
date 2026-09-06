using Sim;

namespace View
{
    /// <summary>
    /// The one place a simulation number turns into a view number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every float in the client starts here.</b> The simulation is
    /// Q32.32 fixed point and has no floating-point anything in it — that is
    /// enforced by an IL scan over <c>Sim.dll</c>, not by habit. The view is
    /// Unity, and Unity is floats all the way down. Somewhere the two have to
    /// meet, and it is better that the meeting is a named file with a test than
    /// forty <c>(float)</c> casts spread through the drawing code.
    /// </para>
    /// <para>
    /// <b>The conversion is one-way and that is the point.</b> Nothing here
    /// goes back: there is no <c>ToFix64</c>, because a float that re-entered
    /// the simulation is the entire class of bug this architecture exists to
    /// prevent, and the cheapest way to prevent it is to not write the
    /// function. A view number is a dead end.
    /// </para>
    /// <para>
    /// <b>Precision is not a concern in this direction.</b> A <c>float</c> has
    /// 24 bits of mantissa and the playfield is about 30 metres across, so the
    /// worst rounding here is a couple of micrometres — four orders of
    /// magnitude below a pixel. The reason the simulation is fixed point was
    /// never that floats are imprecise; it is that they are imprecise
    /// <i>differently</i> on different machines, and nothing downstream of this
    /// file is ever compared against another machine.
    /// </para>
    /// </remarks>
    public static class SimUnits
    {
        /// <summary>
        /// How many world metres one unit of simulation distance is.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One unit of simulation distance is one hex of corridor — the
        /// simulation measures distance in route steps, entrance at zero. So
        /// the conversion is the distance between two neighbouring hex centres,
        /// and for a pointy-top hex that is exactly the width across the flats:
        /// a same-row neighbour is <c>AcrossFlats</c> away by definition, and a
        /// neighbour one row up or down is
        /// <c>sqrt((AcrossFlats/2)^2 + RowPitch^2)</c>, which works out to
        /// <c>AcrossFlats</c> as well.
        /// </para>
        /// <para>
        /// That all six neighbours are equidistant is what makes this a single
        /// constant rather than a per-step length. It is a property of hexes
        /// and not a coincidence of these numbers, but it is asserted by a test
        /// anyway, because it is exactly the kind of quiet assumption that
        /// survives right up until somebody changes the grid.
        /// </para>
        /// </remarks>
        public const float MetresPerHex = HexGeometry.AcrossFlats;

        /// <summary>
        /// Two to the thirty-second, as a double: the scale of one whole unit
        /// in the simulation's fixed-point representation.
        /// </summary>
        private const double OneRaw = 4294967296.0;

        /// <summary>
        /// What a milli-hex is a thousandth of. The unit a bubble's radius and
        /// a walking speed are authored in, and it is a plain integer rather
        /// than fixed point.
        /// </summary>
        private const float MilliHexPerHex = 1000f;

        /// <summary>
        /// A fixed-point number as a float. Divided as a <c>double</c> and
        /// narrowed once at the end, so the division is exact and only the
        /// final narrowing rounds.
        /// </summary>
        public static float ToFloat(Fix64 value) => (float)(value.Raw / OneRaw);

        /// <summary>
        /// A simulation distance — route steps, or hexes sideways — in world
        /// metres.
        /// </summary>
        public static float Metres(Fix64 hexes) => (float)(hexes.Raw / OneRaw * MetresPerHex);

        /// <summary>
        /// A radius authored in thousandths of a hex — a bubble's — in world
        /// metres.
        /// </summary>
        /// <remarks>
        /// Flat, and that is the whole of the conversion: the simulation reads
        /// the same number as a sphere by adding half a hex per level of
        /// height, and nothing that draws a bubble asks how tall the ground
        /// under it is.
        /// </remarks>
        public static float MetresFromMilliHex(int milliHex) => milliHex / MilliHexPerHex * MetresPerHex;
    }
}
