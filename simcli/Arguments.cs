using System.Globalization;

namespace Sim.Cli;

/// <summary>
/// The command line, read strictly: named options, every one of them known,
/// none of them twice, and nothing assumed.
/// </summary>
/// <remarks>
/// A misspelled option is refused rather than ignored. This program's whole job
/// is to produce a file somebody else compares byte for byte, so silently
/// running with a default in place of the argument that was meant is the one
/// failure mode worth spending code on: it would produce a plausible file about
/// a different match.
/// </remarks>
internal sealed class Arguments
{
    private readonly string _verb;

    private readonly List<string> _names = new();

    private readonly List<string> _values = new();

    private Arguments(string verb)
    {
        _verb = verb;
    }

    /// <summary>
    /// Parses <c>--name value</c> pairs, refusing anything else.
    /// </summary>
    /// <param name="valueless">
    /// Which of <paramref name="allowed"/> are switches: present or absent, and
    /// never followed by a value. Read with <see cref="Given"/>. They are named
    /// here rather than discovered at the point of use, because a parser that
    /// guessed would swallow the next option as a switch's value the first time
    /// a switch was written last.
    /// </param>
    public static Arguments Parse(
        string verb,
        string[] args,
        int from,
        string[] allowed,
        string[]? valueless = null)
    {
        var arguments = new Arguments(verb);

        for (int index = from; index < args.Length; index++)
        {
            string name = args[index];

            if (!name.StartsWith("--", StringComparison.Ordinal))
            {
                throw new UsageException(
                    $"'{name}' is not an option. Every argument to '{verb}' is a --name followed by a "
                    + "value.");
            }

            name = name.Substring(2);

            if (Array.IndexOf(allowed, name) < 0)
            {
                throw new UsageException(
                    $"'--{name}' is not an option of '{verb}'. It takes: --{string.Join(", --", allowed)}.");
            }

            if (arguments._names.Contains(name))
            {
                throw new UsageException($"'--{name}' was given twice, and only one of them can be meant.");
            }

            arguments._names.Add(name);

            if (valueless is not null && Array.IndexOf(valueless, name) >= 0)
            {
                arguments._values.Add(string.Empty);
                continue;
            }

            if (index + 1 == args.Length)
            {
                throw new UsageException($"'--{name}' was given with nothing after it.");
            }

            arguments._values.Add(args[index + 1]);
            index++;
        }

        return arguments;
    }

    /// <summary>The verb these arguments were read for, which is what a refusal names.</summary>
    public string Verb => _verb;

    /// <summary>Whether a switch was written on the command line.</summary>
    public bool Given(string name) => _names.Contains(name);

    /// <summary>An option that has to be there.</summary>
    public string Required(string name)
    {
        int index = _names.IndexOf(name);

        if (index < 0)
        {
            throw new UsageException($"'{_verb}' needs --{name}, and it was not given.");
        }

        return _values[index];
    }

    /// <summary>An option that may be absent, in which case null.</summary>
    public string? Optional(string name)
    {
        int index = _names.IndexOf(name);
        return index < 0 ? null : _values[index];
    }

    /// <summary>An unsigned 64-bit number, in ASCII digits and nothing else.</summary>
    public ulong RequiredUnsigned(string name)
    {
        string value = Required(name);

        if (!ulong.TryParse(value, NumberStyles.None, PlainText.Culture, out ulong parsed))
        {
            throw new UsageException($"--{name} is '{value}', which is not a number written in digits.");
        }

        return parsed;
    }

    /// <summary>
    /// A number in a range, or the fallback when the option was not given.
    /// </summary>
    /// <remarks>
    /// The range is checked here rather than at the record's edge so the
    /// complaint names the option somebody typed. A <c>u16</c> field refusing
    /// "70000" says nothing about which argument produced it.
    /// </remarks>
    public int Optional(string name, int fallback, int minimum, int maximum)
    {
        string? value = Optional(name);

        if (value is null)
        {
            return fallback;
        }

        if (!int.TryParse(value, NumberStyles.None, PlainText.Culture, out int parsed))
        {
            throw new UsageException($"--{name} is '{value}', which is not a number written in digits.");
        }

        if (parsed < minimum || parsed > maximum)
        {
            throw new UsageException(
                $"--{name} is {parsed.ToString(PlainText.Culture)}, and it has to be between "
                + $"{minimum.ToString(PlainText.Culture)} and {maximum.ToString(PlainText.Culture)}.");
        }

        return parsed;
    }
}

/// <summary>A command line that cannot be acted on, said in a sentence.</summary>
internal sealed class UsageException : Exception
{
    public UsageException(string message)
        : base(message)
    {
    }
}
