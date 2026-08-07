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
/// and reads the outcome off the other side. Every rule -- what a take may
/// name, how wide a round's slots are, what a wave costs, when the run is over
/// -- is behind <see cref="CommandStream.Replay"/> and none of it is here.
/// </para>
/// <para>
/// <b>The seed comes off the stream and never off an argument.</b> Every
/// offering and every field in a run is derived from its seed, so a run built
/// on a seed beside the record would be a different run wearing the record's
/// decisions -- and the gate refuses that by name rather than playing it.
/// </para>
/// </remarks>
internal sealed class PlayedRun
{
    private PlayedRun(CommandStream stream, Run run, RunOutcome outcome)
    {
        Stream = stream;
        Run = run;
        Outcome = outcome;
    }

    /// <summary>The record that was played, as it came off the disk.</summary>
    public CommandStream Stream { get; }

    /// <summary>The run the stream was played into, after every round of it resolved.</summary>
    public Run Run { get; }

    /// <summary>The vector: the per-round pairs, and every fold over them.</summary>
    public RunOutcome Outcome { get; }

    /// <summary>
    /// Reads a command file, builds the run it says it is about, and plays it
    /// to the end.
    /// </summary>
    /// <param name="record">What the bytes are called in any error message. Never a path.</param>
    public static PlayedRun Of(string record, byte[] bytes, RunContent content, int waves, int fieldSize)
    {
        CommandStream stream = CommandStream.FromBytes(record, bytes);

        // The read gate is behind FromBytes; the replay gate is behind Replay,
        // and it is where the simulation version and the three content hashes
        // are held against the run in front of them.
        Run run = content.Fresh(stream.Seed, waves, fieldSize);

        return new PlayedRun(stream, run, stream.Replay(run, content.Defense));
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
    /// not re-implement it; the run handed in comes back played, and the outcome
    /// is read off it.
    /// </remarks>
    public static (byte[] Bytes, PlayedRun Proof) Recorded(
        string source,
        string scriptText,
        RunContent content,
        ulong seed,
        int waves,
        int fieldSize)
    {
        IReadOnlyList<RecordCommand> commands = CommandScript.Parse(source, scriptText);
        Run run = content.Fresh(seed, waves, fieldSize);
        byte[] bytes = CommandStream.Recorded(run, content.Defense, commands);

        return (bytes, new PlayedRun(CommandStream.FromBytes(source, bytes), run, run.Outcome));
    }

    /// <summary>The shape the run was played at: N, K, and whether death ends it.</summary>
    public string ShapeLine() =>
        "shape      "
        + Run.Waves.ToString(PlainText.Culture)
        + " waves, a field of "
        + Run.FieldSize.ToString(PlainText.Culture)
        + (Run.DeathEndsTheRun ? ", death ends the run" : ", death does not end the run");

    /// <summary>What a person reads: the folds, and how the run stopped.</summary>
    public string Summary() =>
        "outcome    " + Outcome.ToString() + ", ended " + Outcome.Ending.ToString();

    /// <summary>
    /// One line per round: the decision that was stored, and what it came to.
    /// </summary>
    /// <remarks>
    /// Walked over the vector rather than over the stream, because the vector
    /// is what happened. A stream is played to its end or refused, so the two
    /// are the same length -- and a report that walked the longer of them would
    /// invent a round on the day that stopped being true.
    /// </remarks>
    public string Rounds()
    {
        var text = new StringBuilder();

        for (int index = 0; index < Outcome.Rounds.Count; index++)
        {
            if (index > 0)
            {
                text.Append('\n');
            }

            text.Append(Stream.Commands[index].ToString())
                .Append("   ->   ")
                .Append(Outcome.Rounds[index].ToString());
        }

        return text.ToString();
    }

    /// <summary>
    /// The outcome as the committed file: the prose that says what it is, what
    /// run produced it, and then a line per round.
    /// </summary>
    /// <remarks>
    /// The run is named by what is intrinsic to it -- the stream's own header,
    /// its three stamped hashes, its seed and the shape it was played at -- and
    /// never by the paths it was invoked with. A file whose bytes depended on
    /// how somebody spelled an argument could not be compared against a
    /// committed copy by anything.
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
                "round, so a lifecycle regression -- an offering that moved, an interest rate that",
                "compounded differently, a slot width that widened at the wrong anchor -- is a diff rather",
                "than an argument. It is deliberately not produced by whatever is checking it.",
                string.Empty,
                "EVERY ROUND IS A DECISION AND A PAIR. The decision came out of the command file and",
                "nothing else: a run consumes build phases from a record, and there is no other route into",
                "the tick loop. The pair is what that round's wave got past the field and what the field's",
                "waves got past this run's defense, both priced in sauce and both the average over the",
                "field rather than the sum.",
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
                "  decision                                                    round",
            },
            Rounds());
}
