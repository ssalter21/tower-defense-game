using Sim;

namespace Sim.Cli;

/// <summary>Which word stopped a round being composed.</summary>
internal enum Stopped
{
    /// <summary><c>done</c>: the phase is finished, and playing it is the caller's.</summary>
    Done = 0,

    /// <summary><c>quit</c>: the run ends here, and this phase is not to be played.</summary>
    Quit,

    /// <summary>
    /// The reader ran out of lines before either word was typed, which is what
    /// a transcript that stops mid-round does.
    /// </summary>
    OutOfLines,
}

/// <summary>A round's decision as composing left it, and the word that left it there.</summary>
internal readonly struct Composed
{
    public Composed(BuildPhase? phase, Stopped stopped)
    {
        Phase = phase;
        Stopped = stopped;
    }

    /// <summary>
    /// The phase, or nothing where the round never took anything. It resolves
    /// against the run it was composed against -- every word that reached it was
    /// priced before it was accepted.
    /// </summary>
    public BuildPhase? Phase { get; }

    /// <summary>Which word stopped it.</summary>
    public Stopped Stopped { get; }
}

/// <summary>
/// One round's build phase, composed a word at a time at a prompt and priced
/// after every word, with nothing moved.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here is committed and nothing here advances.</b> The run is read
/// -- its offering, its purse, its board, its unlocks -- and never written, so
/// what comes back is a decision somebody composed and not a round somebody
/// played. Handing the phase to <see cref="Run.Advance"/> belongs to whoever
/// called this, which is what keeps a lifecycle out of the loop.
/// </para>
/// <para>
/// <b>A word is priced before it is accepted, and the pricing is thrown
/// away.</b> <see cref="BuildPhase.Resolve"/> checks a decision against the
/// offering, the unlocks, the slot width, the map, the board and the purse and
/// moves nothing, so the loop can resolve a half-composed phase, keep the answer
/// only long enough to learn that it had one, and discard the
/// <see cref="Build"/>. The invariant that falls out is worth naming: <b>the
/// composed phase always resolves</b>, because a candidate that did not is never
/// the one kept -- so <c>done</c> cannot arrive at a phase the run will refuse,
/// which is the surprise <c>docs/playing-a-run-from-a-shell.md</c> §3 is careful
/// about.
/// </para>
/// <para>
/// <b>A refusal is caught and printed, never thrown out of the loop.</b> The
/// sentences <see cref="SimulationException"/> carries already name the round,
/// the verb and the cell, because they were written for somebody authoring a
/// command file -- and that is exactly what somebody at a prompt needs to be
/// told. The word that raised one is simply not added, so what was already legal
/// stands.
/// </para>
/// <para>
/// <b><c>undo</c> drops the last accepted phase rather than the last typed
/// word.</b> Every accepted word leaves a whole phase behind and those are what
/// is kept, so undoing is stepping back one and there is no separate ledger of
/// words to disagree with what is actually composed. It follows that a refused
/// word leaves nothing to step back to, and that a second <c>take</c> is undone
/// to the first rather than to nothing.
/// </para>
/// <para>
/// <b>The reader and the writer are handed in.</b> <see cref="Console.In"/> and
/// <see cref="Console.Out"/> in the ordinary case and a canned transcript in a
/// test, which is the whole of how an interactive verb is exercised from a cold
/// shell.
/// </para>
/// <para>
/// <b>Nothing echoes the line that was read.</b> In a terminal the echo is the
/// terminal's, and printing a second one would put every typed line on the
/// screen twice. A transcript therefore produces output whose prompt runs into
/// whatever follows it, which is the same characters a played session emits and
/// not a second arrangement of them.
/// </para>
/// </remarks>
internal static class BuildPrompt
{
    /// <summary>What stands in front of a line waiting to be typed.</summary>
    private const string Prompt = "> ";

    /// <summary>
    /// What a place, an upgrade or a send is told before the round has taken
    /// anything.
    /// </summary>
    private const string TakeFirst =
        "This round has taken nothing yet. A phase is composed around the one thing it takes off the "
        + "menu, so until a take is named there is nothing here to add to and nothing to price against.";

    /// <summary>What <c>done</c> is told in the same state.</summary>
    private const string TakeBeforeDone =
        "This round has taken nothing yet, and a round's take is which of the menu rather than whether: "
        + "unlocking is free, so declining is a decision nothing rewards. There is no phase to be done "
        + "with until one is named.";

    /// <summary>What <c>undo</c> is told where no word has been accepted.</summary>
    private const string NothingToUndo =
        "There is nothing to undo. This round has taken nothing, built nothing and filled no slot, so "
        + "there is no accepted word for the last one to be dropped.";

    /// <summary>
    /// Composes one round's phase, printing the frame when the round opens and
    /// again after every word that changes it.
    /// </summary>
    /// <param name="run">The run the round belongs to. Read, never written.</param>
    /// <param name="ladder">Which unit follows which, which is how the map cases its letters.</param>
    /// <param name="reader">Where the words come from.</param>
    /// <param name="writer">Where the frames and the refusals go.</param>
    /// <param name="opening">
    /// A decision this round is already holding, or nothing for a round being
    /// composed from the start. It is what a caller whose <c>done</c> was
    /// refused hands back, so that the round is carried on rather than typed
    /// again -- and it goes in above the round's opening nothing, so
    /// <c>undo</c> steps out of it the way it steps out of any other state.
    /// </param>
    public static Composed Compose(
        Run run,
        UpgradeLadder ladder,
        TextReader reader,
        TextWriter writer,
        BuildPhase? opening = null)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(ladder);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(writer);

        // Every state the decision has been in, the round's opening empty
        // phase first. The last of them is what is composed and what `undo`
        // drops. It opens empty rather than absent because there is no longer a
        // take to wait for: a round with nothing typed into it is a legal
        // decision that builds nothing and sends nothing.
        var accepted = new List<BuildPhase> { BuildPhase.Of() };

        if (opening is not null)
        {
            accepted.Add(opening);
        }

        PlainText.Say(writer, RoundFrame.ToText(run, ladder, opening));

        while (true)
        {
            BuildPhase phase = accepted[accepted.Count - 1];

            writer.Write(Prompt);

            string? line = reader.ReadLine();

            if (line is null)
            {
                return new Composed(phase, Stopped.OutOfLines);
            }

            TypedLine typed = TypedLine.Read(line, run.Types);

            switch (typed.Word)
            {
                case Typed.Nothing:
                    break;

                case Typed.Refused:
                    PlainText.Say(writer, typed.Refusal!);
                    break;

                case Typed.Act:
                    // The same phase under a name the closure below can see:
                    // what a captured local holds is not carried into a lambda.
                    BuildPhase acting = phase;

                    Propose(run, ladder, writer, accepted, () => acting.With(typed.Action));
                    break;

                case Typed.Send:
                    BuildPhase sending = phase;

                    Propose(run, ladder, writer, accepted, () => Filling(sending, typed.Slot));
                    break;

                case Typed.Undo:
                    if (accepted.Count == 1)
                    {
                        PlainText.Say(writer, NothingToUndo);
                        break;
                    }

                    accepted.RemoveAt(accepted.Count - 1);
                    PlainText.Say(writer, RoundFrame.ToText(run, ladder, accepted[accepted.Count - 1]));
                    break;

                case Typed.Map:
                    PlainText.Say(writer, RoundFrame.ToText(run, ladder, phase, Panel.Map));
                    break;

                case Typed.Menu:
                    PlainText.Say(writer, RoundFrame.ToText(run, ladder, phase, Panel.Menu));
                    break;

                case Typed.Costs:
                    PlainText.Say(writer, RoundFrame.ToText(run, ladder, phase, Panel.Costs));
                    break;

                case Typed.Done:
                    return new Composed(phase, Stopped.Done);

                case Typed.Quit:
                    return new Composed(phase, Stopped.Quit);

                default:
                    throw new InvalidOperationException(
                        "The prompt read the word "
                        + typed.Word
                        + ", which this loop has no case for. Every member of Typed is a word somebody can "
                        + "get to the prompt, so one added to that list without a case here is a word that "
                        + "would otherwise be read and silently ignored.");
            }
        }
    }

    /// <summary>
    /// Prices a candidate decision and keeps it where it resolved, printing the
    /// frame it leaves; or prints why it was refused and keeps the one already
    /// composed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The candidate is built inside the catch as well as resolved inside it,
    /// because a take id below one is <see cref="BuildPhase.Of"/>'s refusal
    /// rather than <see cref="BuildPhase.Resolve"/>'s -- and at a prompt the two
    /// are the same event: a word that did not land, said in the sentence the
    /// simulation wrote for it.
    /// </para>
    /// <para>
    /// The <see cref="Build"/> is dropped on the floor. What it was asked for is
    /// whether it exists; the numbers on the frame come from the frame resolving
    /// the phase it draws, so nothing here hands a pricing to a drawing that
    /// could have been made against a different one.
    /// </para>
    /// </remarks>
    private static void Propose(
        Run run,
        UpgradeLadder ladder,
        TextWriter writer,
        List<BuildPhase> accepted,
        Func<BuildPhase> candidate)
    {
        BuildPhase composed;

        try
        {
            composed = candidate();
            composed.Resolve(
                run.Round + 1, run.Ladder, run.Purse, run.Costs, run.Types, run.Map, run.Board);
        }
        catch (SimulationException refused)
        {
            PlainText.Say(writer, refused.Message);
            return;
        }

        accepted.Add(composed);
        PlainText.Say(writer, RoundFrame.ToText(run, ladder, composed));
    }

    /// <summary>The decision with one more slot filled, after the ones it already fills.</summary>
    /// <remarks>
    /// Appended in the order the sends were typed and never sorted into the
    /// ascending order <see cref="BuildPhase.Resolve"/> asks of them. Sorting
    /// would rewrite a decision on its author's behalf, and the ascent is what
    /// makes two slots on one creep a slot spent twice rather than a wave with
    /// two spellings -- so a send out of order is refused, in the record's own
    /// sentence.
    /// </remarks>
    private static BuildPhase Filling(BuildPhase phase, WaveSlot slot)
    {
        WaveSlot[] slots = Copied(phase.Slots, 1);
        slots[slots.Length - 1] = slot;

        return Rebuilt(slots, phase.Actions);
    }

    /// <summary>
    /// A phase out of its parts: the slots, then the actions in the order they
    /// were written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A slot is given to <see cref="BuildPhase.Of"/> and an action only to
    /// <see cref="BuildPhase.With"/>, so changing the slots means building the
    /// phase again rather than editing it. That is the shape of an immutable
    /// decision, and a round is a handful of words.
    /// </para>
    /// <para>
    /// It sits here rather than beside <see cref="BuildPhase.With"/> as the
    /// <c>Taking</c> and <c>Filling</c> the composing wants, because composing is
    /// this prompt's problem and the specification's bottom line is that the
    /// simulation gains no surface for it. Nothing that reads or writes a command
    /// file rebuilds a phase: a stored one arrives whole.
    /// </para>
    /// </remarks>
    private static BuildPhase Rebuilt(
        WaveSlot[] slots,
        IReadOnlyList<BuildAction>? actions)
    {
        BuildPhase phase = BuildPhase.Of(slots);

        for (int index = 0; index < (actions?.Count ?? 0); index++)
        {
            phase = phase.With(actions![index]);
        }

        return phase;
    }

    /// <summary>
    /// These slots as an array, with room for this many more after them.
    /// </summary>
    private static WaveSlot[] Copied(IReadOnlyList<WaveSlot>? slots, int spare)
    {
        int held = slots?.Count ?? 0;
        var copied = new WaveSlot[held + spare];

        for (int index = 0; index < held; index++)
        {
            copied[index] = slots![index];
        }

        return copied;
    }
}
