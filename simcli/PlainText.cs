using System.Globalization;
using System.Text;

namespace Sim.Cli;

/// <summary>
/// How this program writes text, so that every file it produces is the same
/// bytes on every machine that runs it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Line feeds, no byte-order mark, one culture.</b> All three are load
/// bearing rather than tidy: the files written here are committed and compared
/// byte for byte by a build gate, so a Windows text writer's carriage returns,
/// a stray mark on the first line or a locale that renders digits differently
/// would each show up as a gate that is red on one machine and green on
/// another. The repository pins the checked-out form as well -- see the
/// <c>content/**</c> line in <c>.gitattributes</c> -- and this is the other
/// half of the same guarantee.
/// </para>
/// <para>
/// The simulation refuses to consult a culture at all, hand-rolling every
/// number it parses. This is the smaller version of that position, for the
/// program that prints them.
/// </para>
/// </remarks>
internal static class PlainText
{
    /// <summary>The one culture anything here formats a number with.</summary>
    public static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>UTF-8 without the byte-order mark nothing here wants.</summary>
    public static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// One block onto a screen: whatever it is, then the line feed that ends it.
    /// </summary>
    /// <remarks>
    /// Here rather than beside either prompt that calls it, because the line
    /// feed is the same decision the rest of this file is about: a terminal
    /// written through <see cref="TextWriter.WriteLine(string)"/> would end its
    /// lines with whatever the platform calls one, and a canned session's output
    /// would then be two different sets of bytes on two machines.
    /// </remarks>
    public static void Say(TextWriter writer, string block)
    {
        writer.Write(block);
        writer.Write('\n');
    }

    /// <summary>
    /// A generated file: a prose header as comment lines, a blank line, the
    /// body, and the trailing newline every line-oriented tool expects.
    /// </summary>
    public static string File(string[] header, string body)
    {
        var text = new StringBuilder();

        for (int index = 0; index < header.Length; index++)
        {
            text.Append(header[index].Length == 0 ? "#" : "# " + header[index]).Append('\n');
        }

        return text.Append('\n').Append(body).Append('\n').ToString();
    }
}
