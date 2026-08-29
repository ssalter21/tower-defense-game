using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace View
{
    /// <summary>
    /// The scenery a human placed by hand, read from and written back to
    /// <c>content/dressing.txt</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A cell named in this file is taken over by it, whole.</b> Not merged,
    /// not added to: whatever the generator would have put on that hex is
    /// discarded and this file's lines are what stands there. Merging was the
    /// other option and it is unlivable — you could never delete anything,
    /// because the generator would put it straight back, and the file would
    /// have to grow a second verb meaning "no, really".
    /// </para>
    /// <para>
    /// <b>So an override survives every knob.</b> Turn up the grove chance and
    /// the whole board re-dresses except the cells somebody has spoken for.
    /// That is the division of labour between this and
    /// <see cref="DressingSettings"/>: the settings say how heavy the board is,
    /// this says where the exceptions are, and neither can express the other.
    /// </para>
    /// <para>
    /// <b>Integers only, and the units are in the column names.</b> Millimetres
    /// for offsets, degrees for turns, percent for scale — the same reasoning as
    /// <c>units.txt</c>: a decimal point in a content file is a locale bug
    /// waiting for a machine whose separator is a comma, and a diff of round
    /// numbers is a diff somebody can read.
    /// </para>
    /// <para>
    /// <b>The sky is all-or-nothing.</b> Clouds belong to no cell, so there is
    /// nothing to key an override on. One <c>cloud</c> line replaces the
    /// generated sky entirely, which is a rule that can be explained in a
    /// sentence and baked without a diff.
    /// </para>
    /// </remarks>
    public sealed class BoardDressing
    {
        /// <summary>What a scale of 100 means: the model at its authored size.</summary>
        public const int ScalePercentAtAuthoredSize = 100;

        /// <summary>Millimetres in a metre. The offsets' unit.</summary>
        public const float MillimetresPerMetre = 1000f;

        private readonly Dictionary<(int Column, int Row), List<SceneryPlacement>> _cells;

        private readonly List<SceneryPlacement> _sky;

        private BoardDressing(
            Dictionary<(int, int), List<SceneryPlacement>> cells,
            List<SceneryPlacement> sky)
        {
            _cells = cells;
            _sky = sky;
        }

        /// <summary>No overrides at all. What a board with no such file draws.</summary>
        public static BoardDressing Empty { get; } =
            new BoardDressing(new Dictionary<(int, int), List<SceneryPlacement>>(), null);

        /// <summary>How many cells this file speaks for.</summary>
        public int CellCount => _cells.Count;

        /// <summary>True if this file replaces the generated sky.</summary>
        public bool HasSky => _sky != null;

        /// <summary>True if this file speaks for a cell, whether or not it puts anything on it.</summary>
        public bool Speaks(int column, int row) => _cells.ContainsKey((column, row));

        /// <summary>
        /// What stands on a cell this file speaks for. Empty for a cleared one.
        /// </summary>
        public IReadOnlyList<SceneryPlacement> At(int column, int row) =>
            _cells.TryGetValue((column, row), out List<SceneryPlacement> on)
                ? on
                : (IReadOnlyList<SceneryPlacement>)Array.Empty<SceneryPlacement>();

        /// <summary>The authored sky, where there is one.</summary>
        public IReadOnlyList<SceneryPlacement> Sky =>
            _sky ?? (IReadOnlyList<SceneryPlacement>)Array.Empty<SceneryPlacement>();

        /// <summary>
        /// Parses the file. Refuses by naming the line, because a mistyped
        /// coordinate that was quietly skipped would present as "my tree did not
        /// save".
        /// </summary>
        /// <exception cref="FormatException">Any line this cannot read.</exception>
        public static BoardDressing Parse(string fileName, string text)
        {
            var cells = new Dictionary<(int, int), List<SceneryPlacement>>();
            List<SceneryPlacement> sky = null;

            if (text == null)
            {
                return Empty;
            }

            string[] lines = text.Replace("\r\n", "\n").Split('\n');

            for (int index = 0; index < lines.Length; index++)
            {
                string line = Strip(lines[index]);

                if (line.Length == 0)
                {
                    continue;
                }

                string[] word = line.Split(
                    new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                switch (word[0])
                {
                    case "place":
                        Place(fileName, index + 1, word, cells);
                        break;

                    case "clear":
                        Clear(fileName, index + 1, word, cells);
                        break;

                    case "cloud":
                        sky = sky ?? new List<SceneryPlacement>();
                        sky.Add(Cloud(fileName, index + 1, word));
                        break;

                    default:
                        throw Bad(fileName, index + 1, "starts with " + word[0]
                            + ", which is not one of place, clear or cloud");
                }
            }

            return new BoardDressing(cells, sky);
        }

        /// <summary>
        /// Writes a file that parses back to the same thing. What the editor's
        /// bake produces.
        /// </summary>
        /// <remarks>
        /// Cells come out in row-major order and pieces in the order they were
        /// given, so baking the same board twice writes the same bytes and a
        /// diff shows what somebody actually moved. Sorting on read would have
        /// been the alternative and it hides that.
        /// </remarks>
        public static string Write(
            IEnumerable<SceneryPlacement> cellPieces,
            IEnumerable<(int Column, int Row)> cleared,
            IEnumerable<SceneryPlacement> sky)
        {
            var written = new StringBuilder();
            written.Append(Preamble);

            var byCell = new SortedDictionary<(int Row, int Column), List<SceneryPlacement>>();

            foreach ((int column, int row) in cleared ?? Array.Empty<(int, int)>())
            {
                byCell[(row, column)] = new List<SceneryPlacement>();
            }

            foreach (SceneryPlacement piece in cellPieces ?? Array.Empty<SceneryPlacement>())
            {
                var key = (piece.Row, piece.Column);

                if (!byCell.TryGetValue(key, out List<SceneryPlacement> on))
                {
                    on = new List<SceneryPlacement>();
                    byCell[key] = on;
                }

                on.Add(piece);
            }

            foreach (KeyValuePair<(int Row, int Column), List<SceneryPlacement>> cell in byCell)
            {
                if (cell.Value.Count == 0)
                {
                    written.Append("clear  ")
                        .Append(Pad(cell.Key.Column, 4))
                        .Append(Pad(cell.Key.Row, 4))
                        .Append('\n');

                    continue;
                }

                foreach (SceneryPlacement piece in cell.Value)
                {
                    written.Append("place  ")
                        .Append(Pad(piece.Column, 4))
                        .Append(Pad(piece.Row, 4))
                        .Append(piece.Group.ToString().ToLowerInvariant().PadRight(10))
                        .Append(Pad(piece.Variant, 4))
                        .Append(Pad(Millimetres(piece.OffsetX), 8))
                        .Append(Pad(Millimetres(piece.OffsetZ), 8))
                        .Append(Pad(Degrees(piece.Turn), 6))
                        .Append(Pad(Percent(piece.Scale), 6))
                        .Append('\n');
                }
            }

            foreach (SceneryPlacement cloud in sky ?? Array.Empty<SceneryPlacement>())
            {
                written.Append("cloud  ")
                    .Append(Pad(cloud.Variant, 4))
                    .Append(Pad(Millimetres(cloud.OffsetX), 8))
                    .Append(Pad(Millimetres(cloud.OffsetY), 8))
                    .Append(Pad(Millimetres(cloud.OffsetZ), 8))
                    .Append(Pad(Degrees(cloud.Turn), 6))
                    .Append(Pad(Percent(cloud.Scale), 6))
                    .Append('\n');
            }

            return written.ToString();
        }

        private static void Place(
            string fileName,
            int line,
            string[] word,
            Dictionary<(int, int), List<SceneryPlacement>> cells)
        {
            if (word.Length != 9)
            {
                throw Bad(fileName, line, "has " + (word.Length - 1)
                    + " values after 'place'; it wants 8: col row what variant east north turn scale");
            }

            int column = Number(fileName, line, word[1], "col");
            int row = Number(fileName, line, word[2], "row");
            SceneryGroup group = Group(fileName, line, word[3]);

            if (group == SceneryGroup.Cloud)
            {
                throw Bad(fileName, line, "places a cloud on a cell. Clouds are not on the board; "
                    + "use a cloud line, which replaces the whole sky");
            }

            var placement = new SceneryPlacement(
                group,
                Number(fileName, line, word[4], "variant"),
                column,
                row,
                Number(fileName, line, word[5], "east") / MillimetresPerMetre,
                0f,
                Number(fileName, line, word[6], "north") / MillimetresPerMetre,
                Number(fileName, line, word[7], "turn"),
                Number(fileName, line, word[8], "scale") / (float)ScalePercentAtAuthoredSize);

            if (!cells.TryGetValue((column, row), out List<SceneryPlacement> on))
            {
                on = new List<SceneryPlacement>();
                cells[(column, row)] = on;
            }

            on.Add(placement);
        }

        private static void Clear(
            string fileName,
            int line,
            string[] word,
            Dictionary<(int, int), List<SceneryPlacement>> cells)
        {
            if (word.Length != 3)
            {
                throw Bad(fileName, line, "has " + (word.Length - 1)
                    + " values after 'clear'; it wants 2: col row");
            }

            int column = Number(fileName, line, word[1], "col");
            int row = Number(fileName, line, word[2], "row");

            if (cells.TryGetValue((column, row), out List<SceneryPlacement> on) && on.Count > 0)
            {
                throw Bad(fileName, line, "clears " + column + "," + row
                    + ", which an earlier line puts something on. A cell is either cleared or dressed, "
                    + "and a file saying both does not say which was meant");
            }

            cells[(column, row)] = new List<SceneryPlacement>();
        }

        private static SceneryPlacement Cloud(string fileName, int line, string[] word)
        {
            if (word.Length != 7)
            {
                throw Bad(fileName, line, "has " + (word.Length - 1)
                    + " values after 'cloud'; it wants 6: variant east up north turn scale");
            }

            return new SceneryPlacement(
                SceneryGroup.Cloud,
                Number(fileName, line, word[1], "variant"),
                0,
                0,
                Number(fileName, line, word[2], "east") / MillimetresPerMetre,
                Number(fileName, line, word[3], "up") / MillimetresPerMetre,
                Number(fileName, line, word[4], "north") / MillimetresPerMetre,
                Number(fileName, line, word[5], "turn"),
                Number(fileName, line, word[6], "scale") / (float)ScalePercentAtAuthoredSize);
        }

        private static SceneryGroup Group(string fileName, int line, string word)
        {
            foreach (SceneryGroup group in (SceneryGroup[])Enum.GetValues(typeof(SceneryGroup)))
            {
                if (string.Equals(group.ToString(), word, StringComparison.OrdinalIgnoreCase))
                {
                    return group;
                }
            }

            throw Bad(fileName, line, "names '" + word + "', which is not a scenery group. They are: "
                + string.Join(", ", LowerNames()));
        }

        private static string[] LowerNames()
        {
            string[] names = Enum.GetNames(typeof(SceneryGroup));

            for (int index = 0; index < names.Length; index++)
            {
                names[index] = names[index].ToLowerInvariant();
            }

            return names;
        }

        private static int Number(string fileName, int line, string word, string column)
        {
            if (!int.TryParse(word, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                throw Bad(fileName, line, "has '" + word + "' in the " + column
                    + " column, which is not a whole number. This file is integers only: millimetres, "
                    + "degrees and percent, so that no machine's decimal separator can change what it says");
            }

            return value;
        }

        private static FormatException Bad(string fileName, int line, string what) =>
            new FormatException(fileName + " line " + line.ToString(CultureInfo.InvariantCulture) + " " + what + ".");

        /// <summary>Everything before a <c>#</c>, trimmed.</summary>
        private static string Strip(string line)
        {
            int hash = line.IndexOf('#');

            return (hash < 0 ? line : line.Substring(0, hash)).Trim();
        }

        private static int Millimetres(float metres) =>
            (int)Math.Round(metres * MillimetresPerMetre, MidpointRounding.AwayFromZero);

        private static int Degrees(float turn) =>
            ((int)Math.Round(turn, MidpointRounding.AwayFromZero) % 360 + 360) % 360;

        private static int Percent(float scale) =>
            (int)Math.Round(scale * ScalePercentAtAuthoredSize, MidpointRounding.AwayFromZero);

        private static string Pad(int value, int width) =>
            value.ToString(CultureInfo.InvariantCulture).PadRight(width);

        private const string Preamble =
            "# The scenery somebody placed by hand. Everything not named here is\n"
            + "# generated -- see client/Assets/View/BoardScenery.cs.\n"
            + "#\n"
            + "# A CELL NAMED IN THIS FILE IS TAKEN OVER BY IT, WHOLE. Whatever the\n"
            + "# generator would have put on that hex is discarded and these lines are what\n"
            + "# stands there. So an override survives every setting: turn the grove chance\n"
            + "# up and the board re-dresses except the cells this file speaks for.\n"
            + "#\n"
            + "# WRITTEN BY THE EDITOR, AND EDITABLE BY HAND. Tools > Board > Dress draws\n"
            + "# the board in the scene view, you move things, Tools > Board > Bake writes\n"
            + "# this. Nothing stops you typing a line instead.\n"
            + "#\n"
            + "# Integers only. Offsets are millimetres from the middle of the cell, east\n"
            + "# and north; turn is degrees; scale is percent of the model's authored size.\n"
            + "# A decimal point here would be a bug on the first machine whose separator\n"
            + "# is a comma.\n"
            + "#\n"
            + "# THE CELL IS A COLUMN AND A ROW, counted the way map.txt is written, so a\n"
            + "# piece can be placed by counting characters in that file.\n"
            + "#\n"
            + "# clear <col> <row>                    the hex stands empty\n"
            + "# place <col> <row> <what> <variant> <east> <north> <turn> <scale>\n"
            + "# cloud <variant> <east> <up> <north> <turn> <scale>\n"
            + "#\n"
            + "# ONE cloud LINE REPLACES THE WHOLE SKY. Clouds belong to no cell, so there\n"
            + "# is nothing to key an override on and no diff to take.\n"
            + "#\n"
            + "# what : rimprop, camp, grove, peak or hill. Which model of that family is\n"
            + "#        drawn\n"
            + "#        is the variant, counted from the lists in MatchSceneBuilder and\n"
            + "#        wrapped, so a variant past the end of a family is not an error.\n"
            + "#\n"
            + "#       col row  what      variant east    north   turn  scale\n"
            + "\n";
    }
}
