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

    /// <summary>Parses <c>--name value</c> pairs, refusing anything else.</summary>
    public static Arguments Parse(string verb, string[] args, int from, string[] allowed)
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

            if (index + 1 == args.Length)
            {
                throw new UsageException($"'--{name}' was given with nothing after it.");
            }

            arguments._names.Add(name);
            arguments._values.Add(args[index + 1]);
            index++;
        }

        return arguments;
    }

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
}

/// <summary>A command line that cannot be acted on, said in a sentence.</summary>
internal sealed class UsageException : Exception
{
    public UsageException(string message)
        : base(message)
    {
    }
}
