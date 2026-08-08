using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A run's build phases as authored text: one row per round, naming the
    /// wave, what was taken off that round's offering, and how the wave's slots
    /// were filled.
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
    /// <b>Nothing here knows what is on an offering.</b> The take is a kind and
    /// an id, checked against the round's menu when the stream is played, which
    /// is the only place the menu exists -- an offering is drawn from the run's
    /// seed and the wave, and a file cannot carry a seed it is not stamped with.
    /// </para>
    /// </remarks>
    public static class CommandScript
    {
        private const string Keyword = "build";

        /// <summary>Fields before the slots: the keyword, the wave, the take kind and the take id.</summary>
        private const int FixedFields = 4;

        /// <summary>Fields per slot: a type id and a count.</summary>
        private const int FieldsPerSlot = 2;

        /// <summary>
        /// The largest any number on a row may be. The wave, the take id, a
        /// type id and a count are all <c>u16</c> in the record, so a row that
        /// could be authored and not stored would be a file its own writer
        /// refuses.
        /// </summary>
        private const int Largest = 65535;

        /// <summary>
        /// What each half of a round's menu is called on a row. The position in
        /// this list is the <see cref="OptionKind"/>, so a kind added to the
        /// enum without a word here is a kind no file can name.
        /// </summary>
        private static readonly string[] TakeKinds = { "ordinary", "changer" };

        /// <summary>
        /// The word a row spells this half of a round's menu with.
        /// </summary>
        /// <remarks>
        /// Here rather than wherever a menu is printed, so that whatever shows
        /// somebody an offering shows them the word they then have to type. Two
        /// lists would be two vocabularies, and the one that goes stale is the
        /// one nothing parses.
        /// </remarks>
        public static string WordFor(OptionKind kind)
        {
            int index = (int)kind;

            if (index < 0 || index >= TakeKinds.Length)
            {
                throw new SimulationException(
                    "Option kind "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " has no word a command script can spell it with. A kind is declared, drawn onto an "
                    + "offering and authorable, and all three or none.");
            }

            return TakeKinds[index];
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

            string[] lines = DataText.SplitLines(text);
            var commands = new List<RecordCommand>();

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                int number = index + 1;

                if (DataText.IsBlankOrComment(line))
                {
                    continue;
                }

                string[] fields = DataText.Fields(source, number, line);

                if (!string.Equals(fields[0], Keyword, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        source,
                        number,
                        "starts with '"
                        + fields[0]
                        + "', but the only row a command script has is '"
                        + Keyword
                        + "'.");
                }

                if (fields.Length < FixedFields || (fields.Length - FixedFields) % FieldsPerSlot != 0)
                {
                    throw new ContentException(
                        source,
                        number,
                        "has "
                        + fields.Length.ToString(CultureInfo.InvariantCulture)
                        + " fields. A '"
                        + Keyword
                        + "' row carries the wave, the take kind and the take id, and then a type id and a "
                        + "count for each of the round's slots -- "
                        + FixedFields.ToString(CultureInfo.InvariantCulture)
                        + " fields plus two per slot. A row outside that is a slot with half of it missing, "
                        + "and guessing which half is how a wave nobody composed gets sent.");
                }

                int wave = DataText.IntegerInRange(source, number, "the wave", fields[1], 1, Largest);
                int take = DataText.Keyword(source, number, "the take kind", fields[2], TakeKinds);
                int takeId = DataText.IntegerInRange(source, number, "the take id", fields[3], 1, Largest);

                try
                {
                    commands.Add(RecordCommand.Of(wave, (OptionKind)take, takeId, Slots(source, number, fields)));
                }
                catch (SimulationException refused)
                {
                    // The rule is the record's and the line number is this
                    // file's. Rewrapped rather than reimplemented, so that
                    // moving the rule moves both the bytes and the text.
                    throw new ContentException(source, number, refused.Message);
                }
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
