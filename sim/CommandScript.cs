using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A run's build phases as authored text: a row per round naming the wave,
    /// what was taken off that round's offering and how the wave's slots were
    /// filled, and a row per defensive action beneath it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the authoring form of a command stream and nothing more.</b>
    /// It reads rows and hands them to <see cref="RecordCommand"/>, which is
    /// where the rules about a decision already live -- so a slot at or below
    /// the one above it, a wave counted from zero and a take id nothing can
    /// answer are refused by the same code the stored bytes are refused by, and
    /// each rule has one implementation rather than two. A refusal from there
    /// is rewrapped with the line it happened on, because a person editing a
    /// file needs to be told where.
    /// </para>
    /// <para>
    /// <b>A row's slots are a trailing run of pairs.</b> A row is
    /// <c>build wave take-kind take-id</c> followed by a <c>type-id count</c>
    /// pair for every slot, and <c>0 0</c> is the empty slot -- the same
    /// spelling the record carries, so the authored file and the stored bytes
    /// describe the same thing and the authoring format never needs a migration
    /// to become a record.
    /// </para>
    /// <para>
    /// <b>An action row has fixed arity instead:</b> <c>place wave type-id
    /// column row</c>, and <c>upgrade</c> the same, five fields either way. The
    /// cell is the column and row <c>content/map.txt</c> is written in, so an
    /// action can be composed by counting characters in the map.
    /// </para>
    /// <para>
    /// <b>Only three refusals are this parser's:</b> the keyword, the field
    /// count and the integer ranges. Every interesting refusal about an action
    /// -- what the type id names, whether the cell is on the map, whether
    /// anything stands there already, whether the round can afford it -- needs
    /// the board, the map, the roster or the purse, and nothing here holds any
    /// of them. That is what keeps <see cref="Parse(string)"/> text-only.
    /// </para>
    /// <para>
    /// <b>Three rules about the file's own shape are this parser's, though</b>,
    /// because nothing but a file has them: rows ascend by wave, a wave's action
    /// rows follow its <c>build</c> row, and an action row for a wave with no
    /// build row is refused.
    /// </para>
    /// <para>
    /// <b>Nothing here knows what is on an offering.</b> The take is a kind and
    /// an id, checked against the round's menu when the stream is played, which
    /// is the only place the menu exists -- an offering is drawn from the run's
    /// seed and the wave, and a file cannot carry a seed it is not stamped with.
    /// </para>
    /// </remarks>
    public static class CommandScript
    {
        /// <summary>
        /// The word a row that decides a whole build phase opens with, as
        /// against the two that act on the board.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Public for the reason <see cref="WordFor(ActionKind)"/> is:
        /// whatever writes a script writes the word this parser reads, and two
        /// spellings of one grammar means the one nothing parses goes stale.
        /// </para>
        /// <para>
        /// <b>Which is not surface a player-facing caller added.</b> What is
        /// exposed here is the grammar's own vocabulary, already public at
        /// <see cref="WordFor(ActionKind)"/> before anything composed a round
        /// interactively. The distinction the rule turns on is behaviour:
        /// composing a phase is the caller's problem and stays there, while
        /// reading a word off an enum this file already owns is the alternative
        /// to spelling the same grammar twice.
        /// </para>
        /// </remarks>
        public const string DecisionWord = "build";

        private const string PlaceWord = "place";

        private const string UpgradeWord = "upgrade";

        /// <summary>Fields before the slots: the keyword and the wave.</summary>
        private const int FixedFields = 2;

        /// <summary>Fields per slot: a type id and a count.</summary>
        private const int FieldsPerSlot = 2;

        /// <summary>
        /// Fields on an action row: the keyword, the wave, the type id, the
        /// column and the row. Fixed, unlike a build row's -- an action names
        /// one cell and one type, and there is nothing on it to repeat.
        /// </summary>
        private const int ActionFields = 5;

        /// <summary>
        /// The largest any number on a row may be. The wave, a type id and a
        /// count are all <c>u16</c> in the record, so a row that
        /// could be authored and not stored would be a file its own writer
        /// refuses.
        /// </summary>
        private const int Largest = 65535;

        /// <summary>
        /// What each defensive action is called on a row. The position in this
        /// list is the <see cref="ActionKind"/>, so a kind added to the enum
        /// without a word here is a kind no file can name.
        /// </summary>
        private static readonly string[] ActionWords = { PlaceWord, UpgradeWord };

        /// <summary>The words a row here may open with: the build phase, and the two actions.</summary>
        private static readonly string[] RowWords = { DecisionWord, PlaceWord, UpgradeWord };

        /// <summary>
        /// The word a row spells this defensive action with.
        /// </summary>
        /// <remarks>
        /// Here rather than wherever an action is printed, so that whatever
        /// asks somebody for an action offers them the word a row already
        /// carries, and a prompt and a file cannot come to hold two
        /// vocabularies.
        /// </remarks>
        public static string WordFor(ActionKind kind)
        {
            int index = (int)kind;

            if (index < 0 || index >= ActionWords.Length)
            {
                throw new SimulationException(
                    "Action kind "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " has no word a command script can spell it with. A kind is declared, applied to a "
                    + "board and authorable, and all three or none.");
            }

            return ActionWords[index];
        }

        /// <summary>Parses a run's build phases from text.</summary>
        public static IReadOnlyList<RecordCommand> Parse(string text) => Parse("command script", text);

        /// <summary>Parses a run's build phases, naming the content in any error message.</summary>
        public static IReadOnlyList<RecordCommand> Parse(string source, string text)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var commands = new List<RecordCommand>();

            foreach (DataText.Row row in DataText.Rows(source, text))
            {
                string[] fields = row.Fields;

                DataText.RequireRow(source, row, RowWords);

                bool decides = string.Equals(row.Keyword, DecisionWord, StringComparison.Ordinal);

                RequireFields(source, row, decides);

                int wave = DataText.IntegerInRange(source, row.Line, "the wave", fields[1], 1, Largest);

                if (decides)
                {
                    RequireAscends(source, row.Line, wave, commands);
                    commands.Add(Decision(source, row.Line, wave, fields));
                    continue;
                }

                RequireAnOpenPhase(source, row.Line, wave, commands);
                commands[commands.Count - 1] =
                    commands[commands.Count - 1].With(Action(source, row.Line, fields));
            }

            if (commands.Count == 0)
            {
                throw new ContentException(
                    source,
                    0,
                    "decides nothing at all. A run consumes build phases, and a script with none in it is a "
                    + "run nobody played.");
            }

            return commands;
        }

        /// <summary>
        /// A row required to carry the fields its keyword has: a build row's
        /// four plus two per slot, or an action row's five.
        /// </summary>
        private static void RequireFields(string source, DataText.Row row, bool decides)
        {
            int count = row.Fields.Length;

            if (decides)
            {
                if (count >= FixedFields && (count - FixedFields) % FieldsPerSlot == 0)
                {
                    return;
                }

                throw new ContentException(
                    source,
                    row.Line,
                    "has "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + " fields. A '"
                    + DecisionWord
                    + "' row carries the wave, the take kind and the take id, and then a type id and a "
                    + "count for each of the round's slots -- "
                    + FixedFields.ToString(CultureInfo.InvariantCulture)
                    + " fields plus two per slot. A row outside that is a slot with half of it missing, "
                    + "and guessing which half is how a wave nobody composed gets sent.");
            }

            if (count == ActionFields)
            {
                return;
            }

            throw new ContentException(
                source,
                row.Line,
                "has "
                + count.ToString(CultureInfo.InvariantCulture)
                + " fields. A '"
                + row.Keyword
                + "' row carries the wave, the type id, the column and the row -- "
                + ActionFields.ToString(CultureInfo.InvariantCulture)
                + " fields, always. An action names one cell and one type, so there is nothing on it that "
                + "repeats and no count of fields but this one that could be read.");
        }

        /// <summary>One build row's decision, with a refusal from the record given the line it came from.</summary>
        private static RecordCommand Decision(string source, int line, int wave, string[] fields)
        {


            try
            {
                return RecordCommand.Of(wave, Slots(source, line, fields));
            }
            catch (SimulationException refused)
            {
                // The rule is the record's and the line number is this file's.
                // Rewrapped rather than reimplemented, so that moving the rule
                // moves both the bytes and the text.
                throw new ContentException(source, line, refused.Message);
            }
        }

        /// <summary>
        /// One action row's action. The kind comes off the keyword, on the same
        /// terms the take kind comes off its own word.
        /// </summary>
        private static BuildAction Action(string source, int line, string[] fields)
        {
            int kind = DataText.Keyword(source, line, "the action", fields[0], ActionWords);
            int typeId = DataText.IntegerInRange(source, line, "the type id", fields[2], 1, Largest);
            int column = DataText.IntegerInRange(
                source,
                line,
                "the column",
                fields[3],
                BuildAction.LeastCoordinate,
                BuildAction.GreatestCoordinate);
            int row = DataText.IntegerInRange(
                source,
                line,
                "the row",
                fields[4],
                BuildAction.LeastCoordinate,
                BuildAction.GreatestCoordinate);

            return BuildAction.Of((ActionKind)kind, typeId, column, row);
        }

        /// <summary>
        /// A build row required to name a wave above every wave above it.
        /// </summary>
        /// <remarks>
        /// The same rule <see cref="CommandStream"/> asserts over the bytes,
        /// checked here as well because only a file has a line to name -- a
        /// person editing one needs to be told which row is out of order, and a
        /// stream refusing later would name neither.
        /// </remarks>
        private static void RequireAscends(
            string source,
            int line,
            int wave,
            IReadOnlyList<RecordCommand> commands)
        {
            if (commands.Count == 0 || wave > commands[commands.Count - 1].Wave)
            {
                return;
            }

            throw new ContentException(
                source,
                line,
                "decides wave "
                + wave.ToString(CultureInfo.InvariantCulture)
                + ", at or below the "
                + commands[commands.Count - 1].Wave.ToString(CultureInfo.InvariantCulture)
                + " a row above it already decided. Rows ascend by wave across the whole file: a run plays "
                + "its rounds in the order they are written, and two build rows for one round is two runs "
                + "written down as one.");
        }

        /// <summary>
        /// An action row required to sit under the build row of its own wave.
        /// </summary>
        /// <remarks>
        /// Three ways to fail and three sentences, because they are three
        /// different mistakes: a row that has fallen below the phase it belongs
        /// to, a row that has been written above it, and a row belonging to a
        /// phase nobody composed.
        /// </remarks>
        private static void RequireAnOpenPhase(
            string source,
            int line,
            int wave,
            IReadOnlyList<RecordCommand> commands)
        {
            if (commands.Count == 0)
            {
                throw new ContentException(
                    source,
                    line,
                    "acts on wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
                    + ", and no build row stands above it. An action row for a wave with no build row is "
                    + "refused: an action is paid for out of its round's purse and applied in its round's "
                    + "order, so one belonging to no build phase belongs to no round either.");
            }

            int open = commands[commands.Count - 1].Wave;

            if (wave == open)
            {
                return;
            }

            if (wave < open)
            {
                throw new ContentException(
                    source,
                    line,
                    "acts on wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
                    + ", below the "
                    + open.ToString(CultureInfo.InvariantCulture)
                    + " a row above it already decided. Rows ascend by wave across the whole file, so an "
                    + "action for a round the file has already left is one nothing would ever apply.");
            }

            throw new ContentException(
                source,
                line,
                "acts on wave "
                + wave.ToString(CultureInfo.InvariantCulture)
                + " where the build row above it decided wave "
                + open.ToString(CultureInfo.InvariantCulture)
                + ". A wave's action rows follow its own build row, because a round's take is decided "
                + "before what it spends the rest of its gold on, and an action above its build row would "
                + "be paid for by the round before it.");
        }

        /// <summary>
        /// One row's slots, read off the pairs after the fixed fields.
        /// <c>0 0</c> is the empty slot; every other arrangement of a zero and a
        /// number is <see cref="WaveSlot.Of"/> refusing, because leaving a slot
        /// empty already has exactly one spelling.
        /// </summary>
        private static WaveSlot[] Slots(string source, int line, string[] fields)
        {
            var slots = new WaveSlot[(fields.Length - FixedFields) / FieldsPerSlot];

            for (int index = 0; index < slots.Length; index++)
            {
                int at = FixedFields + (index * FieldsPerSlot);
                string which = "slot " + (index + 1).ToString(CultureInfo.InvariantCulture);

                int typeId = DataText.IntegerInRange(
                    source, line, "the type id of " + which, fields[at], 0, Largest);
                int count = DataText.IntegerInRange(
                    source, line, "the count of " + which, fields[at + 1], 0, Largest);

                slots[index] = typeId == 0 && count == 0 ? WaveSlot.Empty : WaveSlot.Of(typeId, count);
            }

            return slots;
        }
    }
}
