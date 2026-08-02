namespace Sim.Poison
{
    /// <summary>
    /// Row 2: <c>System.Math</c>. Deliberately an integer overload, so the
    /// violation is the type being reachable at all rather than the obvious
    /// case of a transcendental returning a double. If the ban were written
    /// against the float-returning members only, this would slip through and
    /// the door would be open.
    /// </summary>
    public static class PoisonMath
    {
        public static int Magnitude(int value)
        {
            return System.Math.Abs(value);
        }
    }
}
