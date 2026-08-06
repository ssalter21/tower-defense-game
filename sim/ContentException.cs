using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Thrown when authored content cannot be loaded. Every parse failure in
    /// this assembly is one of these, and every one of them names the line it
    /// happened on. There is no lenient mode and no tolerance for an
    /// unrecognised field: any fault throws.
    /// </summary>
    public sealed class ContentException : Exception
    {
        /// <summary>
        /// What the content was called, for the message. Not
        /// <see cref="Exception.Source"/>, and never a file path -- this
        /// assembly is handed text and is not told where it came from.
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

        /// <summary>
        /// Formats the message as <c>source(line): message</c>, dropping the
        /// parenthesised line when it is zero.
        /// </summary>
        private static string Describe(string source, int line, string message) =>
            line > 0
                ? source + "(" + line.ToString(CultureInfo.InvariantCulture) + "): " + message
                : source + ": " + message;
    }
}
