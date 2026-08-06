using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Thrown when authored content cannot be loaded. Every parse failure in
    /// this assembly is one of these and names the line it happened on. No
    /// lenient mode and no tolerance for an unrecognised field: any fault throws.
    /// </summary>
    public sealed class ContentException : Exception
    {
        /// <summary>What the content was called. Never a file path.</summary>
        public string Content { get; }

        /// <summary>One-based line number, or zero when the fault is not on a line.</summary>
        public int Line { get; }

        public ContentException(string source, int line, string message)
            : base(Describe(source, line, message))
        {
            Content = source;
            Line = line;
        }

        // source(line): message, dropping the parentheses when the line is zero.
        private static string Describe(string source, int line, string message) =>
            line > 0
                ? source + "(" + line.ToString(CultureInfo.InvariantCulture) + "): " + message
                : source + ": " + message;
    }
}
