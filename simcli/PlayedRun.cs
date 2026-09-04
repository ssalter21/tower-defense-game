using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// One run, played from a command file with nobody watching, and the outcome
/// file that falls out of it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A run is played from a record, exactly as a match is.</b> The stream
/// carries the seed, the build phases and the hashes of the three tables the
/// decisions were made under; this program hands it the run it says it is about
/// and reads the outcome off the other side. Every rule -- what a placement may
/// become, what a wave costs, what the purse can reach, when the run is over --
/// is behind <see cref="CommandStream.Replay"/> and none of it is here.
/// </para>
/// <para>
/// <b>The seed comes off the stream and never off an argument.</b> Every field
/// a run is scored against is derived from its seed, so a run built on a seed
/// beside the record would be a different run wearing the record's decisions --
/// and the gate refuses that by name rather than playing it.
/// </para>
/// </remarks>
internal sealed class PlayedRun
{
    private readonly IReadOnlyList<RoundReport> _rounds;

    private readonly StoredRounds? _pool;

    private PlayedRun(
        CommandStream stream,
        Run run,
        IReadOnlyList<RoundReport> rounds,
        StoredRounds? pool)
    {
        Stream = stream;
        Run = run;
        _rounds = rounds;
        _pool = pool;
    }

    /// <summary>The record that was played, as it came off the disk.</summary>
    public CommandStream Stream { get; }

    /// <summary>The run the stream was played into, after every round of it resolved.</summary>
    public Run Run { get; }

    /// <summary>
    /// Reads a command file, builds the run it says it is about, and plays it
    /// to the end.
    /// </summary>
    /// <param name="record">What the bytes are called in any error message. Never a path.</param>
    /// <param name="pool">
    /// The folder the run's opponents were drawn from, so a round can name the
    /// ids it met. Null where no folder was named.
    /// </param>
    public static PlayedRun Of(
        string record,
        byte[] bytes,
        RunContent content,
        RunShape shape,
        StoredRounds? pool)
    {
        CommandStream stream = CommandStream.FromBytes(record, bytes);

        // The read gate is behind FromBytes; the replay gate is behind Replay,
        // and it is where the simulation version and the three content hashes
        // are held against the run in front of them.
        Run run = content.Fresh(stream.Seed, shape);

        return new PlayedRun(stream, run, stream.Replay(run), pool);
    }

    /// <summary>
    /// Records the authored decisions as a command file, having read the bytes
    /// back and played them to the end first.
    /// </summary>
    /// <remarks>
    /// Nothing is returned that will not replay -- the rule the record verb
    /// already follows for a replay bundle, and the reason a stored run is never
    /// something somebody finds out about when they try to play it.
    /// <see cref="CommandStream.Recorded"/> is where that happens, so this does
    /// not re-implement it; the run handed in comes back played, and the rounds
    /// the proving resolved come back with the bytes rather than being played
    /// for a second time out here to report them.
    /// </remarks>
    public static (byte[] Bytes, PlayedRun Proof) Recorded(
        string source,
        string scriptText,
        RunContent content,
        ulong seed,
        RunShape shape)
    {
        IReadOnlyList<RecordCommand> commands = CommandScript.Parse(source, scriptText);
        Run run = content.Fresh(seed, shape);

        (byte[] bytes, IReadOnlyList<RoundReport> rounds) =
            CommandStream.Recorded(run, commands);

        return (bytes, new PlayedRun(CommandStream.FromBytes(source, bytes), run, rounds, pool: null));
    }

    /// <summary>The shape the run was played at: N, K, and whether death ends it.</summary>
    public string ShapeLine() =>
        "shape      "
        + Run.Waves.ToString(PlainText.Culture)
        + " waves, a field of "
        + Run.FieldSize.ToString(PlainText.Culture)
        + (Run.DeathEndsTheRun ? ", death ends the run" : ", death does not end the run");

    /// <summary>What a person reads: the folds, and how the run stopped.</summary>
    public string Summary() => RunSummary.Outcome(Run);

    /// <summary>
    /// One line per round: the decision that was stored, what it came to, what
    /// its wave cost and what the wave paid back.
    /// </summary>
    /// <remarks>
    /// Walked over what the rounds reported rather than over the stream, because
    /// the rounds are what happened. A stream is played to its end or refused,
    /// so the two are the same length -- and a report that walked the longer of
    /// them would invent a round on the day that stopped being true.
    /// </remarks>
    private string Rounds()
    {
        var text = new StringBuilder();

        for (int index = 0; index < _rounds.Count; index++)
        {
            if (index > 0)
            {
                text.Append('\n');
            }

            text.Append(Stream.Commands[index].ToString())
                .Append("   ->   ")
                .Append(_rounds[index].ToString())
                .Append(FieldOf(index));
        }

        return text.ToString();
    }

    /// <summary>
    /// Who the round met, where any of them was somebody: how many of the K
    /// slots drew a stored round, which ones by id, and how many the canned
    /// field stood in for.
    /// </summary>
    /// <remarks>
    /// <b>Nothing at all where the folder holds no round.</b> A run against an
    /// empty pool is resolved against the canned field exactly as it was before
    /// there were folders, and a line saying so on every round of every report
    /// would be a claim about a population that does not exist -- and would move
    /// <c>content/run-outcome.txt</c>, which is a run played against no folder.
    /// </remarks>
    private string FieldOf(int round)
    {
        if (_pool is null || _pool.Count == 0)
        {
            return string.Empty;
        }

        FieldDraw field = _rounds[round].Field;
        var names = new List<string>();

        for (int slot = 0; slot < field.Drawn.Count; slot++)
        {
            Hash64? id = _pool.Drawn(round + 1, field.Drawn[slot]);

            if (id is not null)
            {
                names.Add(id.Value.ToString());
            }
        }

        return ", field "
            + PlainText.Number(names.Count)
            + " stored and "
            + PlainText.Number(field.Canned)
            + " canned"
            + (names.Count == 0 ? string.Empty : ": " + string.Join(", ", names));
    }

    /// <summary>
    /// What a report says a run did: the round lines, a blank line, and the
    /// board the last of them left.
    /// </summary>
    /// <remarks>
    /// One method rather than two beside each other, because where the block
    /// sits is a layout decision and a terminal and a committed file that made
    /// it separately would drift. The board is read off the run itself, which
    /// is what every build phase acted on and handed back, rather than walked
    /// out of the decisions a second time.
    /// </remarks>
    public string RoundsAndBoard() => Rounds() + "\n\n" + Run.Board.ToReportText();

    /// <summary>
    /// The outcome as the committed file: the prose that says what it is, what
    /// run produced it, a line per round, and the board it ended on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The run is named by what is intrinsic to it -- the stream's own header,
    /// its three stamped hashes and its seed -- and never by the paths it was
    /// invoked with. A file whose bytes depended on how somebody spelled an
    /// argument could not be compared against a committed copy by anything.
    /// </para>
    /// <para>
    /// <b>The shape is the exception, and it is here for the opposite
    /// reason.</b> N and K are arguments that no record stamps, so the same
    /// decisions played against a wider field are a legal run and a different
    /// set of numbers. Printing the shape is what puts that where a diff can
    /// see it.
    /// </para>
    /// </remarks>
    public string OutcomeFile() =>
        PlainText.File(
            new[]
            {
                "The outcome of one whole run, as a real run of the committed command file produced it.",
                "Regenerate it with tools/run-headless-match.ps1 -Regenerate; never edit it by hand.",
                string.Empty,
                "THIS IS A GENERATED FILE AND IT IS COMMITTED ON PURPOSE. It is the golden trace's rule",
                "applied one level up: the trace pins a match tick by tick, and this pins a run round by",
                "round, so a lifecycle regression -- a price that moved, an interest rate that",
                "compounded differently, a field that was drawn from somewhere else -- is a diff rather",
                "than an argument. It is deliberately not produced by whatever is checking it.",
                string.Empty,
                "EVERY ROUND IS A DECISION, A PAIR AND AN ECONOMY. The decision came out of the command",
                "file and nothing else: a run consumes build phases from a record, and there is no other",
                "route into the tick loop. The pair is what that round's wave got past the field and what",
                "the field's waves got past this run's defense, both priced in gold and both the average",
                "over the field rather than the sum. Then how many towers stood while that pair was",
                "resolved, which is the board after this round's own building: the purse walks the take,",
                "then the actions, then the slots, so a tower bought here is standing when this round's",
                "waves arrive. Then what the whole phase cost -- what it took, what it built and what it",
                "sends, out of the one wallet -- and what the wave paid the purse back: the bank it opened",
                "on, the interest that bank earned, the flat base, the share of what its offense dealt",
                "that the ruleset pays for damage, and the gold it closed on.",
                string.Empty,
                "THE LAST BLOCK IS THE BOARD AT THE END. A run's ending position is not a round and no",
                "round line adds up to it, so it is printed once, under everything, with a column header of",
                "its own. It is a position, so it is the layout and not the board: the type, the column and",
                "the row of every tower standing when the run stopped, in the order a defense file writes",
                "them -- ascending by row and then by column. The column beside them is the placement id,",
                "the ordinal of the place that put the tower there, which survives an upgrade swapping its",
                "type and is what ties a row down there to a decision up here. A run that built nothing says",
                "so on a row of its own, and a run that died prints the board it died on: there is no run",
                "the block is missing from.",
                string.Empty,
                "The run this came from:",
                string.Empty,
                "  " + Stream.ToString(),
                "  " + ShapeLine(),
                "  " + Summary(),
                string.Empty,
                "Any of those moving moves every line below it. That is the point: the outcome is retired",
                "loudly rather than quietly comparing a run against a different one.",
                string.Empty,
                "  decision                                                    round, cost and payment",
            },
            RoundsAndBoard());
}
