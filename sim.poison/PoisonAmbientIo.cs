namespace Sim.Poison
{
    /// <summary>
    /// Row 8: ambient IO. The simulation is handed bytes and text and never a
    /// path, so a method that takes one and opens it is the violation this row
    /// exists to catch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what the content parsers would have looked like if the seam had
    /// been drawn one level lower -- a convenience overload, added because a
    /// caller had a filename and it seemed rude to make them read the file
    /// themselves. It would have compiled, worked on the machine that wrote it,
    /// and quietly given the simulation a second input that no record pins.
    /// </para>
    /// <para>
    /// <c>System.IO</c> is banned by namespace prefix, so the second line is a
    /// violation as well and would be one on its own: <c>StringReader</c> reads
    /// no file at all and is still out of bounds, because the rule is about
    /// where the seam is rather than about which types are dangerous.
    /// </para>
    /// </remarks>
    public static class PoisonAmbientIo
    {
        public static string Load(string path)
        {
            string text = System.IO.File.ReadAllText(path);
            using var reader = new System.IO.StringReader(text);
            return reader.ReadLine();
        }
    }
}
