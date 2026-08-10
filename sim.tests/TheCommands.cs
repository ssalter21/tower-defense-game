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

    /// <summary>The place every stream carrying an action stores first.</summary>
    public static readonly BuildAction Placed = BuildAction.Of(ActionKind.Place, 3, 9, 0);

    /// <summary>The upgrade stored after it, on the cell that place named.</summary>
    public static readonly BuildAction Upgraded = BuildAction.Of(ActionKind.Upgrade, 4, 9, 0);

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
            TheBuild.Standing(types),
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

    /// <summary>
    /// The same decisions with two actions hung on the first of them: a place
    /// and an upgrade, in that order. What every case that needs a stored
    /// action starts from.
    /// </summary>
    /// <remarks>
    /// Nothing checks what these name. An action's type id, its cell and what
    /// stands there are questions for whatever applies one, and a stream is
    /// read, walked and replayed without any of them being asked.
    /// </remarks>
    public static IReadOnlyList<RecordCommand> Acting(Run run)
    {
        var commands = new List<RecordCommand>(Decisions(run));

        commands[0] = commands[0].With(Placed).With(Upgraded);

        return commands;
    }

    /// <summary>The bytes of a stream whose first build phase carries both of them.</summary>
    public static byte[] ActingBytes()
    {
        Run run = Fresh();

        return CommandStream.Of(run, Acting(run)).ToBytes();
    }

    /// <summary>The stream those decisions record as, stamped with that run's tables.</summary>
    public static CommandStream Stream(Run? run = null, int waves = Waves)
    {
        Run into = run ?? Fresh(waves);

        return CommandStream.Of(into, Decisions(into, waves));
    }

    /// <summary>Those bytes, which is what every negative case starts from.</summary>
    public static byte[] Bytes(int waves = Waves) => Stream(waves: waves).ToBytes();

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
            TheBuild.Standing(types),
            TheRun.Seed,
            waves,
            fieldSize: 2);
    }
}
