using System.Globalization;
using Sim;

namespace Sim.Cli;

/// <summary>Which of the words a line at the prompt opened with.</summary>
/// <remarks>
/// <para>
/// A place and an upgrade are one word here and two in
/// <see cref="ActionKind"/>, because what tells them apart is carried whole by
/// the <see cref="TypedLine.Action"/> either produces. Two members would be a
/// second statement of the same fact, free to disagree with the action beside
/// it.
/// </para>
/// <para>
/// A line nobody could read is <see cref="Refused"/> and not
/// <see cref="Nothing"/>, so that a typo and a pressed return are two values
/// rather than one. A loop that reaches for the word without asking
/// <see cref="TypedLine.Understood"/> first then has a case it does not handle,
/// instead of a misspelling that silently does what a blank line does.
/// </para>
/// </remarks>
internal enum Typed
{
    /// <summary>Nothing was typed. A blank line is not a mistake, so it is a word.</summary>
    Nothing = 0,

    /// <summary>The round's one take, off one half of the menu.</summary>
    Take,

    /// <summary>A place or an upgrade.</summary>
    Act,

    /// <summary>A creep and a count, for the next wave slot.</summary>
    Send,

    /// <summary>Drop the last thing added.</summary>
    Undo,

    /// <summary>Reprint the playfield.</summary>
    Map,

    /// <summary>Reprint this round's offering.</summary>
    Menu,

    /// <summary>Reprint what things cost.</summary>
    Costs,

    /// <summary>Commit the phase.</summary>
    Done,

    /// <summary>End the run early.</summary>
    Quit,

    /// <summary>
    /// Not a word at all. <see cref="TypedLine.Refusal"/> is the sentence
    /// saying what was read instead.
    /// </summary>
    Refused,
}

/// <summary>
/// One line somebody typed, read as the word it is and the operands that word
/// carries -- or as a refusal saying what was read.
/// </summary>
/// <remarks>
/// <para>
/// <b>This decides what was typed and never whether it is legal.</b> No run, no
/// board, no purse and no offering: whether the cell is on the map, whether
/// anything stands on it, whether the round can afford it and whether the menu
/// carries that option are all questions for <see cref="BuildPhase.Resolve"/>,
/// which is where those rules already live. A cell no map has and a wave no
/// purse can pay for both read perfectly here. The roster is the one table
/// handed in, and only because a label needs one -- a <see cref="UnitTypeTable"/>
/// is what a run is built from rather than a thing a run holds.
/// </para>
/// <para>
/// <b>A refusal is a value and never an exception.</b> At a prompt a
/// misspelling is the ordinary case, so the loop above this reads a line, gets
/// a sentence back and prints it, with no catch around the parse and no shape
/// of mistake that can end a session.
/// </para>
/// <para>
/// <b>One vocabulary with the command script.</b> <c>place</c> and
/// <c>upgrade</c> come off <see cref="CommandScript.WordFor(ActionKind)"/> and
/// <c>ordinary</c> and <c>changer</c> off
/// <see cref="CommandScript.WordFor(OptionKind)"/>, so the word typed at the
/// prompt is the word the file carries. What each word's operands come to is
/// built by <see cref="BuildAction.Of"/> and <see cref="WaveSlot.Of"/>, so the
/// ranges a cell, a type id and a count fall in are the record's own and the
/// refusal for one outside them is the record's sentence.
/// </para>
/// <para>
/// <b>How many operands a word takes is stated here, though</b>, rather than
/// derived from the field count of the row it resembles. A script row's fields
/// and a typed line's operands differ by the wave already -- at a prompt the
/// round you are in is not something you should have to type -- and an arity
/// that moved on its own would start demanding a word nothing here reads.
/// A row that grows a field needs an operand read for it, which is an edit.
/// </para>
/// <para>
/// <b>Surrounding whitespace and letter case change nothing, and a number is
/// always an id.</b> A label may be typed where a type id is expected because
/// the roster carries labels already, so what resolves against the roster is
/// whatever did not read as a number -- which leaves a roster label spelled in
/// digits nameable only by its id, and keeps <c>place 3 4 4</c> from depending
/// on what something happens to be called. The take's id gets no such
/// convenience: a game changer's id is its own and not a type's, and the menu
/// that would resolve it is a run.
/// </para>
/// </remarks>
internal readonly struct TypedLine
{
    private const string TakeWord = "take";

    private const string SendWord = "send";

    private const string UndoWord = "undo";

    private const string MapWord = "map";

    private const string MenuWord = "menu";

    private const string CostsWord = "costs";

    private const string DoneWord = "done";

    private const string QuitWord = "quit";

    /// <summary>
    /// What each operand is called. The names are the refusals' as well as the
    /// counts', so a word's shape is written down once.
    /// </summary>
    private const string KindOperand = "the kind";

    private const string IdOperand = "the id";

    private const string TypeOperand = "the type";

    private const string ColumnOperand = "the column";

    private const string RowOperand = "the row";

    private const string CountOperand = "the count";

    private static readonly string[] NoOperands = new string[0];

    private static readonly string[] TakeOperands = { KindOperand, IdOperand };

    private static readonly string[] ActOperands = { TypeOperand, ColumnOperand, RowOperand };

    private static readonly string[] SendOperands = { TypeOperand, CountOperand };

    /// <summary>
    /// The halves of a menu a take names one of, and the things a word can do
    /// to the board. Both are read off the enum rather than listed, so a kind
    /// that gains a member and a word in <see cref="CommandScript"/> is a kind
    /// this prompt can spell without being told twice.
    /// </summary>
    private static readonly OptionKind[] Halves = Enum.GetValues<OptionKind>();

    private static readonly ActionKind[] Kinds = Enum.GetValues<ActionKind>();

    private TypedLine(
        Typed word,
        OptionKind take,
        int takeId,
        BuildAction action,
        WaveSlot slot,
        string? refusal)
    {
        Word = word;
        Take = take;
        TakeId = takeId;
        Action = action;
        Slot = slot;
        Refusal = refusal;
    }

    /// <summary>Which word was typed, or <see cref="Typed.Refused"/> where none was.</summary>
    public Typed Word { get; }

    /// <summary>Which half of the menu a <see cref="Typed.Take"/> named.</summary>
    public OptionKind Take { get; }

    /// <summary>
    /// Which option of that half, unbounded and unchecked against any menu.
    /// </summary>
    /// <remarks>
    /// A take is a kind and an id rather than a value of the record's, so
    /// unlike an action and a slot there is nothing here to build it through.
    /// The floor under an id is <see cref="BuildPhase.Of"/>'s and the menu is
    /// <see cref="Offering.Take"/>'s, and both are reached by the phase this
    /// take goes into.
    /// </remarks>
    public int TakeId { get; }

    /// <summary>What a <see cref="Typed.Act"/> does to the board, place or upgrade.</summary>
    public BuildAction Action { get; }

    /// <summary>What a <see cref="Typed.Send"/> fills a slot with.</summary>
    public WaveSlot Slot { get; }

    /// <summary>Why the line was not read, or nothing where it was.</summary>
    public string? Refusal { get; }

    /// <summary>
    /// Whether the line became a word. The same question as
    /// <see cref="Word"/> not being <see cref="Typed.Refused"/>, said as the
    /// one thing a caller asks first.
    /// </summary>
    public bool Understood => Refusal is null;

    /// <summary>Reads one line, against the roster a label names a row of.</summary>
    public static TypedLine Read(string line, UnitTypeTable types)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(types);

        string[] words = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return Of(Typed.Nothing);
        }

        // What a refusal quotes back: the words as they were typed, with the
        // spacing that carried no meaning taken out of them.
        string read = string.Join(' ', words);

        if (TryKind(words[0], Kinds, CommandScript.WordFor, out ActionKind kind))
        {
            return Acting(read, words, kind, types);
        }

        switch (words[0].ToLowerInvariant())
        {
            case TakeWord:
                return Taking(read, words);

            case SendWord:
                return Sending(read, words, types);

            case UndoWord:
                return Alone(read, words, Typed.Undo);

            case MapWord:
                return Alone(read, words, Typed.Map);

            case MenuWord:
                return Alone(read, words, Typed.Menu);

            case CostsWord:
                return Alone(read, words, Typed.Costs);

            case DoneWord:
                return Alone(read, words, Typed.Done);

            case QuitWord:
                return Alone(read, words, Typed.Quit);

            default:
                return Refused(
                    "'"
                    + read
                    + "' opens with '"
                    + words[0]
                    + "', which is not a word here. The words are "
                    + Listed(Vocabulary())
                    + ".");
        }
    }

    public override string ToString() => Refusal ?? Word.ToString();

    /// <summary>Every word a line may open with, in the order the prompt's own table lists them.</summary>
    private static string[] Vocabulary()
    {
        var words = new List<string> { TakeWord };

        for (int index = 0; index < Kinds.Length; index++)
        {
            words.Add(CommandScript.WordFor(Kinds[index]));
        }

        words.Add(SendWord);
        words.Add(UndoWord);
        words.Add(MapWord);
        words.Add(MenuWord);
        words.Add(CostsWord);
        words.Add(DoneWord);
        words.Add(QuitWord);

        return words.ToArray();
    }

    /// <summary>The round's take: one half of the menu, and an option of it.</summary>
    private static TypedLine Taking(string read, string[] words)
    {
        string? wrong = Miscounted(read, words, TakeOperands);

        if (wrong is not null)
        {
            return Refused(wrong);
        }

        if (!TryKind(words[1], Halves, CommandScript.WordFor, out OptionKind half))
        {
            return Refused(
                "'"
                + read
                + "' takes '"
                + words[1]
                + "', where a take names one half of the round's menu: "
                + CommandScript.WordFor(OptionKind.Ordinary)
                + " or "
                + CommandScript.WordFor(OptionKind.GameChanger)
                + ".");
        }

        string? refusal = NotANumber(read, IdOperand, words[2], out int id);

        return refusal is null ? Taken(half, id) : Refused(refusal);
    }

    /// <summary>A place or an upgrade: a type, and the cell it names.</summary>
    private static TypedLine Acting(string read, string[] words, ActionKind kind, UnitTypeTable types)
    {
        string? refusal = Miscounted(read, words, ActOperands);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        refusal = NoSuchType(read, words[1], types, out int typeId);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        refusal = NotANumber(read, ColumnOperand, words[2], out int column);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        refusal = NotANumber(read, RowOperand, words[3], out int row);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        try
        {
            return Acted(BuildAction.Of(kind, typeId, column, row));
        }
        catch (SimulationException stored)
        {
            return Refused(CannotRead(read, stored));
        }
    }

    /// <summary>A wave slot: a creep, and how many of it.</summary>
    private static TypedLine Sending(string read, string[] words, UnitTypeTable types)
    {
        string? refusal = Miscounted(read, words, SendOperands);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        refusal = NoSuchType(read, words[1], types, out int typeId);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        refusal = NotANumber(read, CountOperand, words[2], out int count);

        if (refusal is not null)
        {
            return Refused(refusal);
        }

        try
        {
            return Sent(WaveSlot.Of(typeId, count));
        }
        catch (SimulationException stored)
        {
            return Refused(CannotRead(read, stored));
        }
    }

    /// <summary>A word that carries nothing, refused where something followed it.</summary>
    private static TypedLine Alone(string read, string[] words, Typed word)
    {
        string? wrong = Miscounted(read, words, NoOperands);

        return wrong is null ? Of(word) : Refused(wrong);
    }

    /// <summary>
    /// The refusal for a line that does not carry the operands its word takes,
    /// naming them, or nothing where it does.
    /// </summary>
    private static string? Miscounted(string read, string[] words, string[] operands)
    {
        int typed = words.Length - 1;

        if (typed == operands.Length)
        {
            return null;
        }

        return "'"
            + read
            + "' carries "
            + Number(typed)
            + (typed == 1 ? " word after '" : " words after '")
            + words[0]
            + "', which takes "
            + (operands.Length == 0 ? "none" : Number(operands.Length) + ": " + Listed(operands))
            + ".";
    }

    /// <summary>
    /// The refusal for an operand that names no one type, or nothing where it
    /// names one -- its id where a number was typed, and otherwise the id of
    /// the row on the roster whose label it is.
    /// </summary>
    private static string? NoSuchType(string read, string field, UnitTypeTable types, out int id)
    {
        if (TryNumber(field, out id))
        {
            return null;
        }

        int found = 0;

        for (int index = 0; index < types.Count; index++)
        {
            UnitType type = types.Types[index];

            if (!string.Equals(type.Label, field, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            found++;

            if (found == 1)
            {
                id = type.Id;
            }
        }

        if (found == 1)
        {
            return null;
        }

        id = 0;

        return found == 0
            ? Naming(read, TypeOperand, field)
                + ", which is neither a number nor the label of anything on the roster."
            : Naming(read, TypeOperand, field)
                + ", which "
                + Number(found)
                + " rows of the roster answer to. A label picks one row by name, so a label two rows "
                + "carry can only be meant by naming the id.";
    }

    /// <summary>
    /// The refusal for an operand that is not a number, or nothing where it is
    /// one.
    /// </summary>
    private static string? NotANumber(string read, string what, string field, out int value) =>
        TryNumber(field, out value)
            ? null
            : Naming(read, what, field) + ", which is not a number written in digits.";

    /// <summary>
    /// A number, signed, in ASCII digits and nothing else. Negative is read
    /// rather than refused: a cell left of the grid is a coordinate
    /// <see cref="BuildAction"/> stores and a map turns down, and a count below
    /// one is <see cref="WaveSlot"/>'s refusal to make.
    /// </summary>
    private static bool TryNumber(string field, out int value) =>
        int.TryParse(field, NumberStyles.AllowLeadingSign, PlainText.Culture, out value);

    private static string Naming(string read, string what, string field) =>
        "'" + read + "' names " + what + " '" + field + "'";

    /// <summary>
    /// A value the record has to store that a typed line named outside its
    /// range, said in the record's own sentence with the line in front of it.
    /// </summary>
    private static string CannotRead(string read, SimulationException stored) =>
        "'" + read + "' cannot be read. " + stored.Message;

    /// <summary>
    /// Which member of a kind a word names, in the spelling a command script
    /// writes it as. One walk for both kinds, because a spelling is looked up
    /// the same way whichever enum it belongs to.
    /// </summary>
    private static bool TryKind<TKind>(
        string field,
        TKind[] kinds,
        Func<TKind, string> wordFor,
        out TKind kind)
        where TKind : struct
    {
        for (int index = 0; index < kinds.Length; index++)
        {
            if (string.Equals(wordFor(kinds[index]), field, StringComparison.OrdinalIgnoreCase))
            {
                kind = kinds[index];
                return true;
            }
        }

        kind = default;
        return false;
    }

    /// <summary>Words in a sentence: commas between them and an "and" before the last.</summary>
    private static string Listed(string[] words) =>
        words.Length == 1
            ? words[0]
            : string.Join(", ", words, 0, words.Length - 1) + " and " + words[words.Length - 1];

    private static string Number(int value) => value.ToString(PlainText.Culture);

    /// <summary>A word carrying nothing but itself.</summary>
    private static TypedLine Of(Typed word) => new TypedLine(word, default, 0, default, default, null);

    /// <summary>A take: which half of the menu, and which of it.</summary>
    private static TypedLine Taken(OptionKind half, int id) =>
        new TypedLine(Typed.Take, half, id, default, default, null);

    /// <summary>A place or an upgrade, which the action itself says which of.</summary>
    private static TypedLine Acted(BuildAction action) =>
        new TypedLine(Typed.Act, default, 0, action, default, null);

    /// <summary>A slot filled.</summary>
    private static TypedLine Sent(WaveSlot slot) =>
        new TypedLine(Typed.Send, default, 0, default, slot, null);

    /// <summary>A line that was read and became no word.</summary>
    private static TypedLine Refused(string sentence) =>
        new TypedLine(Typed.Refused, default, 0, default, default, sentence);
}
