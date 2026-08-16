namespace Sim
{
    /// <summary>
    /// How far a thing standing on one hex reaches another once the ground
    /// stops being flat: two rules and a floor, in whole-hex integer
    /// arithmetic.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Height is a relationship and never a property.</b> A shot's range is
    /// the signed difference <c>baseRange + (fromLevel - targetLevel) * 500</c>
    /// -- shooting down a tier buys half a hex and shooting up one costs half a
    /// hex -- so a tower on a cliff is better at shooting the valley and worse
    /// at shooting the ridge above it. A flat bonus for standing high would
    /// have made it better at both. See
    /// <c>docs/adr/0054-height-is-a-relationship-and-a-radius-is-a-sphere.md</c>.
    /// </para>
    /// <para>
    /// <b>Anything with a radius is a sphere</b>, where the level term is a
    /// magnitude rather than a difference: <c>hexDistance * 1000 +
    /// |levelDifference| * 500 &lt;= radius</c>. Height only ever costs a
    /// radius, which is what stops a bubble centred on a cliff blanketing the
    /// board. Both rules are one expression with two level terms, because the
    /// difference between them is exactly the sign and nothing else.
    /// </para>
    /// <para>
    /// <b>The floor guarantees adjacency on both.</b> Anything with any reach
    /// at all reaches the hexes touching it whatever the terrain does. Without
    /// it a soldier standing one tier below the creep beside him has an
    /// effective range of half a hex and cannot hit the thing he is touching,
    /// which is not the design; and a radius small enough to be a self-centred
    /// bubble would stop being one on a slope.
    /// </para>
    /// <para>
    /// <b>Milli-hexes, so no unit of measurement is invented.</b>
    /// <c>content/units.txt</c> authors range in thousandths of a hex already,
    /// so a tier is an integer add of 500 and there is no division and no
    /// rounding rule anywhere in here. The per-level value and the tier count
    /// are recorded together in <c>docs/build-order.md</c> seam 9, because
    /// neither means anything without the other.
    /// </para>
    /// <para>
    /// <b>A hex and the level it stands at travel as two arguments and not as
    /// a type.</b> The thing that pairs them is a map -- a hex has no height of
    /// its own, and <see cref="HexMap.LevelAt(Hex)"/> is what knows it -- so a
    /// pair type here would be a second place for a hex to be married to the
    /// wrong level. <see cref="Footing.Reaches"/> takes the map and does the
    /// pairing once, which is why nothing outside it calls these directly with
    /// levels it worked out itself.
    /// </para>
    /// <para>
    /// <b>Nothing here runs in the tick loop.</b> A route cell's level is fixed
    /// and the route is fixed, so <see cref="TowerCoverage"/> evaluates this
    /// per route cell at load exactly as it evaluated flat range before, and
    /// hands the tick loop intervals of distance. What elevation changes
    /// downstream is that a tower's coverage fragments where a ridge crosses
    /// it, and a list of disjoint intervals is what that type already returns.
    /// </para>
    /// </remarks>
    public static class Reach
    {
        /// <summary>Thousandths of a hex per hex. Ranges are authored in milli-hexes.</summary>
        private const int MilliHexPerHex = 1000;

        /// <summary>
        /// What one tier of height is worth, in milli-hexes: half a hex.
        /// </summary>
        /// <remarks>
        /// Half a hex rather than a whole one, so that the shape of a fold
        /// matters more than its height map. At a whole hex per tier an archer
        /// swings between 1.2 and 5.2 hexes across the three tiers, which makes
        /// the height of a placement dominate every other thing about it.
        /// </remarks>
        private const int MilliHexPerLevel = 500;

        /// <summary>
        /// Whether a thing standing on one hex, with a range, can shoot a
        /// target standing on another. This is the attack-range rule, and there
        /// is one of it.
        /// </summary>
        public static bool Shoots(Hex from, int fromLevel, int rangeMilliHex, Hex target, int targetLevel) =>
            Within(from.DistanceTo(target), rangeMilliHex, targetLevel - fromLevel);

        /// <summary>
        /// Whether a bubble of some radius, centred on a hex, encloses a cell.
        /// This is the rule for every radius there is -- a sweep, a blast, an
        /// aura -- measured from whichever point the bubble is centred on.
        /// </summary>
        public static bool Encloses(Hex centre, int centreLevel, int radiusMilliHex, Hex cell, int cellLevel)
        {
            int climb = cellLevel - centreLevel;

            return Within(centre.DistanceTo(cell), radiusMilliHex, climb < 0 ? -climb : climb);
        }

        /// <summary>
        /// The one comparison both rules are. The caller supplies the level
        /// term -- signed for a shot, a magnitude for a radius -- and
        /// everything else about the two is identical, including the floor.
        /// </summary>
        /// <remarks>
        /// <b>No reach is not a short reach.</b> Every walking row authors zero
        /// in the range column, and the signed term alone would hand one two
        /// tiers above its neighbour an effective range of a whole hex -- so a
        /// creep would reach the hexes touching it for no reason but the height
        /// of the ground under it. Nothing asks this about a creep today, which
        /// is exactly why it is settled here rather than by every future caller
        /// remembering to. It withdraws the hex a zero-range thing stands on as
        /// well, which flat arithmetic used to grant it: nothing has ever asked
        /// -- a tower may not stand in the corridor, so the route walk never
        /// asks at no distance at all -- and "reaches nothing" is a whole answer
        /// where "reaches only itself" is a special case waiting to be found.
        /// </remarks>
        private static bool Within(int hexes, int radiusMilliHex, int levels) =>
            radiusMilliHex > 0
            && (hexes <= 1
                || (hexes * MilliHexPerHex) + (levels * MilliHexPerLevel) <= radiusMilliHex);
    }
}
