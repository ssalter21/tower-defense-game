namespace Sim.Tests;

/// <summary>
/// A committed data file as somebody with different habits would have typed it.
/// </summary>
/// <remarks>
/// Every content hash in this repository folds parsed integers in field order
/// rather than file bytes, so none of what this does moves one. That is what
/// the pair of assertions around it is for: the text has to change and the hash
/// has to not, and a hash over the file would fail the second half of every one
/// of them.
/// </remarks>
public static class Reauthoring
{
    /// <summary>
    /// The same file with no comments, leading indentation, tabs between the
    /// columns, trailing spaces and CRLF line endings.
    /// </summary>
    public static string Reauthored(string original) =>
        "# a completely different comment\r\n\r\n"
        + string.Join(
            "\r\n",
            original
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                .Where(line => line.Trim().Length > 0)
                .Select(line => "  " + string.Join(
                    "\t",
                    line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) + "   "));
}
