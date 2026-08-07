namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a command stream: a run to play, the
/// decisions to play into it, and the bytes they become.
/// </summary>
/// <remarks>
/// <para>
/// The tests open the files and hand the simulation text, exactly as
/// <see cref="TheMatch"/>, <see cref="TheRun"/> and <see cref="TheBuild"/> do.
/// Nothing here reads or writes a command file: a stream is bytes, and where
/// bytes come from is the command line's business.
/// </para>
/// <para>
/// <b>Every decision fills exactly one slot</b>, which is what lets the
/// negative suite name a byte: a command is then
/// <see cref="RecordFormat.CommandBytes"/> plus one
/// <see cref="RecordFormat.SlotBytes"/>, at a fixed stride down the stream.
/// </para>
/// </remarks>
public static class TheCommands
{
    /// <summary>How many rounds the streams in these tests decide.</summary>
    public const int Waves = 4;

    /// <summary>How many slots every command in these streams fills.</summary>
    public const int SlotsPerCommand = 1;

    /// <summary>How many creeps a filled slot sends. Two of anything on this roster opens affordable.</summary>
    private const int Sent = 2;

    /// <summary>A run on the committed content, with nothing played into it yet.</summary>
    public static Run Fresh(int waves = Waves, ulong seed = TheRun.Seed)
    {
        UnitTypeTable types = TheMatch.Types();

        return new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheSchedule.Committed(types),
            TheRun.Pool(types),
            seed,
            waves,
            fieldSize: 2);
    }

    /// <summary>
    /// One decision per wave: take the first thing on that round's menu, and
    /// send two of the creep it unlocks.
    /// </summary>
    /// <remarks>
    /// Read off <see cref="Run.OfferingAt"/> rather than off a run being
    /// played, because an offering is a function of the seed and the wave --
    /// which is the property that lets a whole stream be composed, and checked,
    /// before a round resolves.
    /// </remarks>
    public static IReadOnlyList<RecordCommand> Decisions(Run run, int waves = Waves)
    {
        var commands = new List<RecordCommand>();

        for (int wave = 1; wave <= waves; wave++)
        {
            Option first = run.OfferingAt(wave).Options[0];

            commands.Add(RecordCommand.Of(
                wave,
                BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, Sent))));
        }

        return commands;
    }

    /// <summary>The stream those decisions record as, stamped with that run's tables.</summary>
    public static CommandStream Stream(Run? run = null, int waves = Waves)
    {
        Run into = run ?? Fresh(waves);

        return CommandStream.Of(into, Decisions(into, waves));
    }

    /// <summary>Those bytes, which is what every negative case starts from.</summary>
    public static byte[] Bytes(int waves = Waves) => Stream(waves: waves).ToBytes();

    /// <summary>The defense that stands while each of a run's waves is sent.</summary>
    public static TowerLayout Defense() => TheMatch.Layout(TheMatch.Types());

    /// <summary>The committed ruleset with one number moved and nothing else touched.</summary>
    /// <remarks>
    /// The income base, because it is a single number on its own row that no
    /// load-time constraint couples to another: every record stamped with the
    /// old ruleset is retired by it and nothing else about the run moves.
    /// </remarks>
    public static Ruleset RetunedRules() =>
        Ruleset.Parse(TheRuleset.Replace(TheRuleset.CommittedText(), "income        100", "income        101"));

    /// <summary>
    /// The committed ruleset as a different file and the same rules: comments
    /// rewritten, columns respaced, line endings turned over.
    /// </summary>
    public static string ReformattedRulesText() => Reformatted(TheRuleset.CommittedText());

    /// <summary>That file, parsed.</summary>
    public static Ruleset ReformattedRules() => Ruleset.Parse(ReformattedRulesText());

    /// <summary>The committed schedule with one anchor's tier pool drawn from one wave earlier.</summary>
    public static AnchorSchedule RetunedSchedule() =>
        AnchorSchedule.Parse(
            TheSchedule.Replace(TheSchedule.CommittedText(), "anchor        6     2", "anchor        5     2"),
            TheMatch.Types());

    /// <summary>A run built on tables the caller names, on the seed and the pool everything else here uses.</summary>
    public static Run Against(Ruleset rules, AnchorSchedule? schedule = null, int waves = Waves)
    {
        UnitTypeTable types = TheMatch.Types();

        return new Run(
            TheMatch.Map(),
            rules,
            types,
            schedule ?? TheSchedule.Committed(types),
            TheRun.Pool(types),
            TheRun.Seed,
            waves,
            fieldSize: 2);
    }

    /// <summary>
    /// The same file, typed by somebody with different habits: no comments,
    /// leading indentation, tabs between the columns, trailing spaces and CRLF
    /// line endings.
    /// </summary>
    private static string Reformatted(string original) =>
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
