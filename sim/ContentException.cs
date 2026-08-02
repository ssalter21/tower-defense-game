using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Thrown when authored content cannot be loaded. Every parse failure in
    /// this assembly is one of these, and every one of them names the line it
    /// happened on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// There is no lenient mode, no "skip the bad row and carry on" and no
    /// tolerance for a field the parser does not recognise. That inverts the
    /// usual schema-evolution advice on purpose: silently ignoring something
    /// you do not understand degrades a message gracefully and degrades a
    /// deterministic replay into a confidently wrong result that still
    /// validates.
    /// </para>
    /// <para>
    /// The line number is one-based because the thing on the other end of this
    /// message is a person looking at an editor.
    /// </para>
    /// </remarks>
    public sealed class ContentException : Exception
    {
        /// <summary>
        /// What the content was called, for the message. Deliberately not
        /// <see cref="Exception.Source"/> and deliberately never a file path:
        /// the simulation is handed text and does not know where it came from.
        /// </summary>
        public string Content { get; }

        /// <summary>One-based line number, or zero when the fault is not on a line.</summary>
        public int Line { get; }

        public ContentException(string source, int line, string message)
            : base(Describe(source, line, message))
        {
            Content = source;
            Line = line;
        }

        private static string Describe(string source, int line, string message) =>
            line > 0
                ? source + "(" + line.ToString(CultureInfo.InvariantCulture) + "): " + message
                : source + ": " + message;
    }
}
