using System.Globalization;
using System.Text;
using Sim;

namespace View
{
    /// <summary>
    /// The one place a row of <c>content/units.txt</c> becomes words on the
    /// screen: what a unit is called, and what a price reads as.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No type ids anywhere on screen, and none of the record's
    /// vocabulary.</b> A unit is its name. The id is how a record pins a row
    /// forever and is meaningless to a person; the words the stored formats use
    /// belong to the formats. Keeping the rendering in one file is what stops a
    /// second screen inventing its own — see ADR-0051, and issues #196 and #197,
    /// which say the same thing about the palette and the wave bar.
    /// </para>
    /// <para>
    /// <b>The name is derived from the label and never authored here.</b>
    /// <c>docs/roster.md</c>'s index is the naming authority, and the labels in
    /// <c>content/units.txt</c> are those names in kebab case — so replacing the
    /// hyphens and capitalising reproduces all nine of them exactly, from
    /// <c>minion</c> to <c>skeleton-warrior</c>. That is a derivation, and an
    /// edit-mode test holds it against the roster's table. A per-unit display
    /// name typed over here would be a second roster, free to drift from the one
    /// the design document keeps.
    /// </para>
    /// <para>
    /// <b>The price format is the command line's, moved rather than
    /// reinvented</b> — <c>simcli/Ladder.cs</c> prints a tier as
    /// <c>40 gold</c>, and that is what the palette says too.
    /// </para>
    /// </remarks>
    public static class RosterNames
    {
        /// <summary>The character <c>content/units.txt</c> joins words with.</summary>
        private const char LabelSeparator = '-';

        /// <summary>What this unit is called on screen.</summary>
        public static string Of(UnitType type) => Of(type is null ? string.Empty : type.Label);

        /// <summary>
        /// The same, from a label on its own. <c>skeleton-scout</c> becomes
        /// <c>Skeleton Scout</c>.
        /// </summary>
        public static string Of(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return string.Empty;
            }

            var name = new StringBuilder(label.Length);
            bool starting = true;

            foreach (char letter in label)
            {
                if (letter == LabelSeparator)
                {
                    name.Append(' ');
                    starting = true;

                    continue;
                }

                name.Append(starting ? char.ToUpperInvariant(letter) : letter);
                starting = false;
            }

            return name.ToString();
        }

        /// <summary>An amount of gold, in words: <c>40 gold</c>.</summary>
        public static string Gold(int gold) =>
            gold.ToString(CultureInfo.InvariantCulture) + " gold";

        /// <summary>
        /// How many of one creep a wave box is sending: <c>x3</c>.
        /// </summary>
        /// <remarks>
        /// Here rather than in <see cref="WaveBar"/> for the reason the price
        /// format is here: it is player-facing wording, and a second surface
        /// that showed a count would otherwise invent its own. The letter is
        /// ASCII on purpose -- the multiplication sign is the typographically
        /// right character and it is one the runtime theme's font is not
        /// guaranteed to carry, and a missing glyph draws as an empty box,
        /// which on a bar made of boxes reads as a bug.
        /// </remarks>
        public static string Count(int count) =>
            "x" + count.ToString(CultureInfo.InvariantCulture);
    }
}
