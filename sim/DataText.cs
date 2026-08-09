using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sim
{
    /// <summary>
    /// The one place in the simulation that turns authored text into integers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It takes text, never a path, and it cannot open anything.</b> Reading
    /// a file is the caller's job -- the Unity view's, the command line's, a
    /// test's -- and the simulation is handed the result. That is not a
    /// convention: <c>System.IO</c> is a banned namespace and the IL scan over
    /// the compiled assembly rejects any reference to it, so a second seam
    /// underneath this one cannot be added quietly.
    /// </para>
    /// <para>
    /// <b>Nothing here consults a culture.</b> <c>int.Parse</c> is not used and
    /// neither is any framework number parser: a data line is checked to be
    /// printable ASCII, and an integer is accumulated one <c>'0'</c>-to-
    /// <c>'9'</c> digit at a time. A Turkish locale cannot change what a digit
    /// is, a comma-decimal locale has nothing to reinterpret, and an
    /// Arabic-Indic digit is rejected as a character rather than accepted as a
    /// number. The hostile-locale test proves that on this machine now instead
    /// of finding out the first time a record crosses to another one.
    /// </para>
    /// <para>
    /// <b>A decimal point on a data line is a load error before tokenising
    /// even starts</b>, and so is a comma. Those are the two characters a
    /// designer types when they want a fraction, and the simulation has no
    /// representation for a fraction that arrived as text. Refusing them here
    /// closes the floating-point side door by construction rather than by
    /// review -- a fraction has to be authored as the two integers it is made
    /// of, the way <c>Fix64.FromRatio</c> takes them.
    /// </para>
    /// </remarks>
    internal static class DataText
    {
        /// <summary>
        /// Decodes UTF-8, rejecting a malformed sequence rather than
        /// substituting a replacement character -- content that does not decode
        /// is content nobody has read, and quietly turning it into U+FFFD would
        /// let it parse anyway.
        /// </summary>
        internal static string FromUtf8(string source, byte[] utf8)
        {
            if (utf8 is null)
            {
                throw new ArgumentNullException(nameof(utf8));
            }

            try
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(utf8);
            }
            catch (ArgumentException malformed)
            {
                throw new ContentException(source, 0, "is not valid UTF-8: " + malformed.Message);
            }
        }

        /// <summary>
        /// Splits into lines on <c>\n</c>, tolerating a <c>\r\n</c> pair and a
        /// leading byte-order mark, because those are what a Windows editor and
        /// a Windows text writer respectively produce and neither is a content
        /// change. Line endings therefore cannot reach a hash.
        /// </summary>
        internal static string[] SplitLines(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length > 0 && text[0] == (char)0xFEFF)
            {
                text = text.Substring(1);
            }

            string[] lines = text.Split('\n');

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];

                if (line.Length > 0 && line[line.Length - 1] == '\r')
                {
                    lines[index] = line.Substring(0, line.Length - 1);
                }
            }

            return lines;
        }

        /// <summary>
        /// The data rows of a file: every line that is neither blank nor a
        /// comment, split into fields, each carrying the one-based line number
        /// it came from.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The blank-and-comment test happens before the split, and holding
        /// that order is what this method is for.</b> <see cref="Fields"/>
        /// refuses a full stop and a comma anywhere on the line, and a comment
        /// is prose -- every committed content file has one that ends in a full
        /// stop. A walk that split first would refuse the file on its own
        /// documentation.
        /// </para>
        /// <para>
        /// The number is the line in the file rather than the row's position
        /// among the rows, because it is what a person reads in an editor and
        /// what <see cref="ContentException"/> prints.
        /// </para>
        /// </remarks>
        internal static IEnumerable<Row> Rows(string source, string text)
        {
            string[] lines = SplitLines(text);

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];

                if (IsBlankOrComment(line))
                {
                    continue;
                }

                int number = index + 1;

                yield return new Row(number, Fields(source, number, line));
            }
        }

        /// <summary>
        /// A row required to open with one of the words this file has a reader
        /// branch for.
        /// </summary>
        /// <remarks>
        /// A file whose rows are read through a <c>switch</c> refuses from its
        /// default arm with <see cref="NoSuchRow"/> instead, which is the same
        /// sentence thrown from where the branching already is. That is the
        /// pairing <see cref="RequireFieldCount"/> and
        /// <see cref="WrongFieldCount"/> are.
        /// </remarks>
        internal static void RequireRow(string source, Row row, string[] words)
        {
            for (int index = 0; index < words.Length; index++)
            {
                if (string.Equals(row.Keyword, words[index], StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw NoSuchRow(source, row.Line, row.Keyword, words);
        }

        /// <summary>
        /// A row that opens with a word the file has no reader branch for. The
        /// words it does have are the parameter.
        /// </summary>
        internal static ContentException NoSuchRow(string source, int line, string keyword, string[] words)
        {
            return new ContentException(
                source,
                line,
                "starts with '"
                + keyword
                + "', which is not one of the rows this file has: "
                + string.Join(", ", words)
                + ". An unrecognised row is refused rather than skipped, because a row nobody read is "
                + "content the reader's defaults quietly supplied.");
        }

        /// <summary>
        /// True for a line the row walk skips entirely: blank, or a comment. A
        /// comment is not scanned for anything, which is exactly why editing
        /// one must not move a content hash.
        /// </summary>
        private static bool IsBlankOrComment(string line)
        {
            for (int index = 0; index < line.Length; index++)
            {
                char character = line[index];

                if (character == ' ' || character == '\t')
                {
                    continue;
                }

                return character == '#';
            }

            return true;
        }

        /// <summary>
        /// Splits a data line into whitespace-separated fields, after refusing
        /// every character that has no business being on one. See the remarks
        /// on <see cref="DataText"/> for why the refusal happens here rather
        /// than inside the number parser.
        /// </summary>
        private static string[] Fields(string source, int line, string text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                if (character == '.' || character == ',')
                {
                    throw new ContentException(
                        source,
                        line,
                        "carries a '"
                        + character
                        + "' at column "
                        + (index + 1).ToString(CultureInfo.InvariantCulture)
                        + ". This file holds integers only: a decimal point -- or a decimal comma, which is "
                        + "the same mistake in another locale -- is the front door a float would walk in "
                        + "through, so it is refused before the line is even read. Author the ratio as the "
                        + "two integers it is made of.");
                }

                if (character == '\t')
                {
                    continue;
                }

                if (character < ' ' || character > '~')
                {
                    throw new ContentException(
                        source,
                        line,
                        "carries a character outside printable ASCII at column "
                        + (index + 1).ToString(CultureInfo.InvariantCulture)
                        + " (code point "
                        + ((int)character).ToString(CultureInfo.InvariantCulture)
                        + "). A digit that is not '0' to '9' is not a digit here.");
                }
            }

            var fields = new List<string>();
            int start = -1;

            for (int index = 0; index <= text.Length; index++)
            {
                bool separator = index == text.Length || text[index] == ' ' || text[index] == '\t';

                if (separator)
                {
                    if (start >= 0)
                    {
                        fields.Add(text.Substring(start, index - start));
                        start = -1;
                    }
                }
                else if (start < 0)
                {
                    start = index;
                }
            }

            return fields.ToArray();
        }

        /// <summary>
        /// An integer, accumulated from ASCII digits. No culture, no
        /// <c>NumberStyles</c>, no framework parser, no thousands separator and
        /// no sign but a leading <c>'-'</c>.
        /// </summary>
        internal static int Integer(string source, int line, string name, string field)
        {
            int index = 0;
            bool negative = false;

            if (field.Length > 0 && field[0] == '-')
            {
                negative = true;
                index = 1;
            }

            if (index == field.Length)
            {
                throw Malformed(source, line, name, field);
            }

            long magnitude = 0;

            for (; index < field.Length; index++)
            {
                char digit = field[index];

                if (digit < '0' || digit > '9')
                {
                    throw Malformed(source, line, name, field);
                }

                magnitude = (magnitude * 10) + (digit - '0');

                if (magnitude > 2147483648L)
                {
                    throw new ContentException(
                        source,
                        line,
                        name + " is '" + field + "', which does not fit in a 32-bit integer.");
                }
            }

            if (negative)
            {
                // The loop above already refused anything past 2^31, which is
                // exactly int.MinValue's magnitude, so this cannot overflow.
                return unchecked((int)-magnitude);
            }

            if (magnitude > 2147483647L)
            {
                throw new ContentException(
                    source,
                    line,
                    name + " is '" + field + "', which does not fit in a 32-bit integer.");
            }

            return (int)magnitude;
        }

        /// <summary>An integer required to fall inside a stated range.</summary>
        internal static int IntegerInRange(
            string source,
            int line,
            string name,
            string field,
            int minimum,
            int maximum)
        {
            int value = Integer(source, line, name, field);

            if (value < minimum || value > maximum)
            {
                throw new ContentException(
                    source,
                    line,
                    name
                    + " is "
                    + value.ToString(CultureInfo.InvariantCulture)
                    + ", outside the allowed range "
                    + minimum.ToString(CultureInfo.InvariantCulture)
                    + " to "
                    + maximum.ToString(CultureInfo.InvariantCulture)
                    + ".");
            }

            return value;
        }

        /// <summary>
        /// A 64-bit value written as exactly sixteen hexadecimal digits, which
        /// is what <see cref="Hash64.ToString"/> produces.
        /// </summary>
        /// <remarks>
        /// Uppercase only, and exactly sixteen digits, both on purpose. A hash
        /// trace is generated by one program and read by another, so there is
        /// nothing to be gained by accepting two spellings of the same value and
        /// something to lose: a file that can be written two ways is a file that
        /// diffs against itself. This reads digits by hand for the same reason
        /// <see cref="Integer"/> does -- no framework parser, no culture, and no
        /// <c>NumberStyles</c> flag to get wrong.
        /// </remarks>
        internal static ulong Hex64(string source, int line, string name, string field)
        {
            const int Digits = 16;

            if (field.Length != Digits)
            {
                throw new ContentException(
                    source,
                    line,
                    name
                    + " is '"
                    + field
                    + "', which is "
                    + field.Length.ToString(CultureInfo.InvariantCulture)
                    + " characters where a 64-bit hash is written as exactly "
                    + Digits.ToString(CultureInfo.InvariantCulture)
                    + " uppercase hexadecimal digits.");
            }

            ulong value = 0;

            for (int index = 0; index < field.Length; index++)
            {
                char character = field[index];
                int digit;

                if (character >= '0' && character <= '9')
                {
                    digit = character - '0';
                }
                else if (character >= 'A' && character <= 'F')
                {
                    digit = 10 + (character - 'A');
                }
                else
                {
                    throw new ContentException(
                        source,
                        line,
                        name
                        + " is '"
                        + field
                        + "', which is not sixteen uppercase hexadecimal digits.");
                }

                value = unchecked((value << 4) | (uint)digit);
            }

            return value;
        }

        /// <summary>
        /// A keyword, matched ordinally against the words a column allows.
        /// Ordinal on purpose: a case-insensitive comparison would consult a
        /// culture, and in Turkish the letters in <c>hitscan</c> do not
        /// round-trip through one.
        /// </summary>
        internal static int Keyword(string source, int line, string name, string field, string[] words)
        {
            for (int index = 0; index < words.Length; index++)
            {
                if (string.Equals(field, words[index], StringComparison.Ordinal))
                {
                    return index;
                }
            }

            throw new ContentException(
                source,
                line,
                name + " is '" + field + "', which is not one of: " + string.Join(", ", words) + ".");
        }

        /// <summary>A label: printable ASCII, no spaces, and never an identity.</summary>
        internal static string Label(string source, int line, string name, string field)
        {
            if (field.Length == 0 || field.Length > 32)
            {
                throw new ContentException(
                    source,
                    line,
                    name + " is '" + field + "', which must be between 1 and 32 characters.");
            }

            for (int index = 0; index < field.Length; index++)
            {
                char character = field[index];
                bool allowed = (character >= 'a' && character <= 'z')
                    || (character >= 'A' && character <= 'Z')
                    || (character >= '0' && character <= '9')
                    || character == '-'
                    || character == '_';

                if (!allowed)
                {
                    throw new ContentException(
                        source,
                        line,
                        name + " is '" + field + "', which may hold only letters, digits, '-' and '_'.");
                }
            }

            return field;
        }

        /// <summary>
        /// The row a type id on this line names, required to play that half of
        /// the loop where <paramref name="role"/> says one.
        /// </summary>
        /// <remarks>
        /// The rule is <see cref="UnitTypeTable.Require"/>'s and the line number
        /// is this file's, so the refusal is rewrapped rather than
        /// reimplemented: moving the rule moves every content file that names a
        /// type id, and the bytes beside them.
        /// </remarks>
        internal static UnitType RequireType(
            string source,
            int line,
            UnitTypeTable types,
            int id,
            UnitRole? role,
            string what)
        {
            try
            {
                return types.Require(id, role, what);
            }
            catch (SimulationException refused)
            {
                throw new ContentException(source, line, refused.Message);
            }
        }

        /// <summary>A row required to carry exactly the fields its keyword has.</summary>
        internal static void RequireFieldCount(
            string source,
            int line,
            string keyword,
            int expected,
            string[] fields)
        {
            if (fields.Length != expected)
            {
                throw WrongFieldCount(source, line, keyword, expected, fields.Length);
            }
        }

        internal static ContentException WrongFieldCount(
            string source,
            int line,
            string keyword,
            int expected,
            int actual)
        {
            return new ContentException(
                source,
                line,
                "a '"
                + keyword
                + "' row has "
                + actual.ToString(CultureInfo.InvariantCulture)
                + " fields where the layout has "
                + expected.ToString(CultureInfo.InvariantCulture)
                + ". Field order is what the content hash folds, so a row with the wrong number of them "
                + "cannot be read at all.");
        }

        private static ContentException Malformed(string source, int line, string name, string field) =>
            new ContentException(
                source,
                line,
                name + " is '" + field + "', which is not an integer written in ASCII digits.");

        /// <summary>
        /// One data row: the fields it was split into, and the line of the file
        /// they were written on.
        /// </summary>
        /// <remarks>
        /// <see cref="Fields"/> is never empty -- a line with no fields on it is
        /// blank, and the walk skipped it -- so a reader may take the keyword
        /// off the front without checking.
        /// </remarks>
        internal readonly struct Row
        {
            internal Row(int line, string[] fields)
            {
                Line = line;
                Fields = fields;
            }

            /// <summary>The one-based line of the file this row was written on.</summary>
            internal int Line { get; }

            /// <summary>The whitespace-separated fields, keyword included.</summary>
            internal string[] Fields { get; }

            /// <summary>The first field, which is the word the row says it is.</summary>
            internal string Keyword => Fields[0];
        }
    }
}
