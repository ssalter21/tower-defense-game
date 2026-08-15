namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a command stream: a run to play, the
/// decisions to play into it, the bytes they become, and the committed script's
/// own decisions.
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

    /// <summary>An archer on a cell the committed defense leaves empty.</summary>
    /// <remarks>
    /// The pair above names a cell the defense file already stands a mage on,
    /// which is nothing to a case that only reads bytes and everything to a
    /// case that applies one.
    /// </remarks>
    public static readonly BuildAction PlacedOnFreeCell = BuildAction.Of(ActionKind.Place, 3, 0, 0);

    /// <summary>The mage that archer becomes, on the cell it stands on.</summary>
    public static readonly BuildAction UpgradedOnFreeCell = BuildAction.Of(ActionKind.Upgrade, 4, 0, 0);

    /// <summary>How many creeps a filled slot sends. Two of anything on this roster opens affordable.</summary>
    private const int Sent = 2;

    /// <summary>
    /// The committed run's own decisions, read off <c>content/commands.txt</c>.
    /// </summary>
    /// <remarks>
    /// Read out of the committed script rather than written out here, so that
    /// the committed run is an <i>input</i> to whatever plays it and there is no
    /// second copy of those ten rounds to keep current. What a caller does with
    /// them is play them into a run a round at a time, which is what the client
    /// does and what <see cref="ProvedSession"/> is handed.
    /// </remarks>
    public static IReadOnlyList<RecordCommand> Committed() =>
        CommandScript.Parse(File.ReadAllText(RepoLayout.CommandScriptFile));

    /// <summary>A run on the committed content, with nothing played into it yet.</summary>
    public static Run Fresh(int waves = Waves, ulong seed = TheRun.Seed)
    {
        UnitTypeTable types = TheMatch.Types();

        return new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheLadder.Committed(types),
            TheRun.Pool(types),
            seed,
            waves,
            fieldSize: 2);
    }

    /// <summary>
    /// One decision per wave: send two of the roster's first creep.
    /// </summary>
    /// <remarks>
    /// Composed off the roster rather than off a run being played, because
    /// nothing rations what a wave may send any more -- every creep is sendable
    /// from wave one, which is the property that lets a whole stream be
    /// composed, and checked, before a round resolves.
    /// </remarks>
    public static IReadOnlyList<RecordCommand> Decisions(Run run, int waves = Waves)
    {
        var commands = new List<RecordCommand>();
        UnitType first = TheBuild.FirstCreep(run.Types);

        for (int wave = 1; wave <= waves; wave++)
        {
            commands.Add(RecordCommand.Of(wave, WaveSlot.Of(first.Id, Sent)));
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
    public static Run Against(Ruleset rules, UpgradeLadder? ladder = null, int waves = Waves)
    {
        UnitTypeTable types = TheMatch.Types();

        return new Run(
            TheMatch.Map(),
            rules,
            types,
            ladder ?? TheLadder.Committed(types),
            TheRun.Pool(types),
            TheRun.Seed,
            waves,
            fieldSize: 2);
    }
}
