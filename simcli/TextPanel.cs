namespace Sim.Cli;

/// <summary>
/// Blocks of text laid out beside one another, which is how every panel of a
/// drawing here is placed.
/// </summary>
/// <remarks>
/// <para>
/// <b>A panel is written over what is already drawn rather than joined to
/// it.</b> A line shorter than the column is padded out to reach it and a line
/// the panel runs past the bottom of is grown from nothing, so two blocks of
/// different heights sit beside each other without either being told the
/// other's shape.
/// </para>
/// <para>
/// <b>A line no panel reaches is left exactly as it was.</b> Nothing here pads
/// a line it does not write on, so no line of a drawing ends in a space -- which
/// is what lets one be compared against a committed block character for
/// character.
/// </para>
/// </remarks>
internal static class TextPanel
{
    /// <summary>How wide the widest of these lines is.</summary>
    public static int Widest(IReadOnlyList<string> lines)
    {
        int widest = 0;

        for (int index = 0; index < lines.Count; index++)
        {
            widest = Math.Max(widest, lines[index].Length);
        }

        return widest;
    }

    /// <summary>
    /// Writes a panel down the right of what is already drawn: its first line
    /// beside line <paramref name="top"/>, and every line of it beginning at
    /// <paramref name="column"/>.
    /// </summary>
    public static void Beside(
        List<string> lines,
        IReadOnlyList<string> panel,
        int top,
        int column)
    {
        for (int index = 0; index < panel.Count; index++)
        {
            while (lines.Count <= top + index)
            {
                lines.Add(string.Empty);
            }

            lines[top + index] = lines[top + index].PadRight(column) + panel[index];
        }
    }
}
