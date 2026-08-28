using System;
using System.Text;
using Sim;

namespace View.Editor
{
    /// <summary>
    /// A board being drawn: the same two grids <c>content/map.txt</c> holds,
    /// mutable, plus the comment header that file opens with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The simulation's parser is the only authority on whether a board is
    /// legal, and this class does not contain a second one.</b>
    /// <see cref="TryParse"/> writes the draft out and hands it to
    /// <see cref="HexMap.ParseUtf8"/> — so the editor accepts exactly what the
    /// game accepts, refuses with the game's own sentence, and cannot drift from
    /// it. Re-implementing "one spawn, one exit, no branch, tier steps on
    /// straight runs" over here would have been a second rulebook, and the
    /// interesting boards are exactly the ones the two would disagree about.
    /// </para>
    /// <para>
    /// <b>The header is carried, not regenerated.</b> That file opens with forty
    /// lines explaining the offset rule, why comments start with <c>//</c>, and
    /// what breaks first in a hand drawing. It is the most useful thing in the
    /// file and a tool that wrote its own preamble over it would destroy it on
    /// the first bake.
    /// </para>
    /// <para>
    /// <b>Levels are letters and the file holds no digits at all</b> — that is
    /// the map's own rule, and it is what keeps this file outside the
    /// decimal-point rule the numeric content files carry.
    /// </para>
    /// </remarks>
    public sealed class BoardDraft
    {
        /// <summary>Ground, no path.</summary>
        public const char GroundMark = '.';

        /// <summary>Corridor.</summary>
        public const char CorridorMark = '#';

        /// <summary>The entrance.</summary>
        public const char SpawnMark = 'S';

        /// <summary>The exit.</summary>
        public const char ExitMark = 'E';

        /// <summary>The letter the ground tier is written with. Tiers count up from it.</summary>
        public const char GroundLevelMark = 'a';

        /// <summary>What a comment line in the map file opens with.</summary>
        public const string CommentMark = "//";

        private readonly MapCell[] _cells;

        private readonly int[] _levels;

        private BoardDraft(int width, int height, string preamble, MapCell[] cells, int[] levels)
        {
            Width = width;
            Height = height;
            Preamble = preamble;
            _cells = cells;
            _levels = levels;
        }

        /// <summary>How many columns.</summary>
        public int Width { get; }

        /// <summary>How many rows.</summary>
        public int Height { get; }

        /// <summary>
        /// The comment block the file opens with, newline-terminated, carried
        /// through every edit and written back out unchanged.
        /// </summary>
        public string Preamble { get; }

        /// <summary>A blank board of a given size, at the ground tier.</summary>
        public static BoardDraft Empty(int width, int height, string preamble) =>
            new BoardDraft(width, height, preamble, new MapCell[width * height], new int[width * height]);

        /// <summary>
        /// A draft of a map the simulation has already parsed, keeping the
        /// header from the text it was parsed out of.
        /// </summary>
        public static BoardDraft Of(HexMap map, string text)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            var cells = new MapCell[map.Width * map.Height];
            var levels = new int[map.Width * map.Height];

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    int index = (row * map.Width) + column;

                    cells[index] = map.CellAt(column, row);
                    levels[index] = map.LevelAt(column, row);
                }
            }

            return new BoardDraft(map.Width, map.Height, PreambleOf(text), cells, levels);
        }

        /// <summary>
        /// The comment block a map file opens with: every line up to the first
        /// one that is neither a comment nor blank, with the blank lines at the
        /// end of it dropped.
        /// </summary>
        /// <remarks>
        /// Trailing blanks are dropped and <see cref="ToText"/> puts exactly one
        /// back, so the separator between the header and the grid is written in
        /// one place. Carrying it in the preamble instead round-trips the
        /// committed file correctly and then adds a line every time somebody
        /// bakes a board that had none.
        /// </remarks>
        public static string PreambleOf(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            int taken = 0;

            foreach (string line in lines)
            {
                if (line.Trim().Length != 0 && !line.TrimStart().StartsWith(CommentMark, StringComparison.Ordinal))
                {
                    break;
                }

                taken++;
            }

            while (taken > 0 && lines[taken - 1].Trim().Length == 0)
            {
                taken--;
            }

            var header = new StringBuilder();

            for (int index = 0; index < taken; index++)
            {
                header.Append(lines[index]).Append('\n');
            }

            return header.ToString();
        }

        /// <summary>What kind of cell this is.</summary>
        public MapCell CellAt(int column, int row) => _cells[(row * Width) + column];

        /// <summary>Which tier it stands at.</summary>
        public int LevelAt(int column, int row) => _levels[(row * Width) + column];

        /// <summary>True if the cell is on the board.</summary>
        public bool Holds(int column, int row) =>
            column >= 0 && column < Width && row >= 0 && row < Height;

        /// <summary>
        /// Paints one cell.
        /// </summary>
        /// <remarks>
        /// <b>A second spawn takes the first one's place rather than being
        /// refused.</b> The map allows exactly one of each end, and a brush that
        /// simply would not paint reads as a broken tool — where an entrance
        /// that moves when you place another reads as the tool understanding
        /// what you meant. The old one becomes plain corridor, since it was
        /// corridor when it was an end.
        /// </remarks>
        public void Paint(int column, int row, MapCell cell)
        {
            if (cell == MapCell.Spawn || cell == MapCell.Exit)
            {
                for (int index = 0; index < _cells.Length; index++)
                {
                    if (_cells[index] == cell)
                    {
                        _cells[index] = MapCell.Route;
                    }
                }
            }

            _cells[(row * Width) + column] = cell;
        }

        /// <summary>Sets the tier of one cell, clamped to what the map allows.</summary>
        public void Raise(int column, int row, int level) =>
            _levels[(row * Width) + column] = level < 0
                ? 0
                : (level >= HexMap.LevelCount ? HexMap.LevelCount - 1 : level);

        /// <summary>
        /// The same board at a different size, keeping every cell that still
        /// fits and filling anything new with ground at the bottom tier.
        /// </summary>
        public BoardDraft Resized(int width, int height)
        {
            BoardDraft bigger = Empty(width, height, Preamble);

            for (int row = 0; row < height && row < Height; row++)
            {
                for (int column = 0; column < width && column < Width; column++)
                {
                    bigger._cells[(row * width) + column] = CellAt(column, row);
                    bigger._levels[(row * width) + column] = LevelAt(column, row);
                }
            }

            return bigger;
        }

        /// <summary>
        /// The draft as <c>map.txt</c>: the header, the terrain grid, a blank
        /// line, the level grid.
        /// </summary>
        /// <remarks>
        /// Odd rows are written indented by one space, which the loader strips.
        /// The indent is decoration and it earns its place: odd rows are the ones
        /// sitting half a cell right, so what is typed looks like the board it
        /// produces.
        /// </remarks>
        public string ToText()
        {
            var written = new StringBuilder();

            written.Append(Preamble);
            written.Append('\n');

            Grid(written, (column, row) => Mark(CellAt(column, row)));

            written.Append('\n');

            Grid(written, (column, row) => (char)(GroundLevelMark + LevelAt(column, row)));

            return written.ToString();
        }

        /// <summary>
        /// The draft as the simulation reads it, or the refusal it would give.
        /// </summary>
        /// <remarks>
        /// <b>This is the whole of the editor's validation.</b> A draft is legal
        /// exactly when the game will load it, which is not a rule anybody has to
        /// keep in step because it is the same call the game makes.
        /// </remarks>
        public bool TryParse(out HexMap map, out string refusal)
        {
            map = null;
            refusal = null;

            try
            {
                map = HexMap.ParseUtf8("the board you are drawing", Encoding.UTF8.GetBytes(ToText()));

                return true;
            }
            catch (ContentException failed)
            {
                refusal = failed.Message;

                return false;
            }
        }

        /// <summary>The character one cell is written with.</summary>
        public static char Mark(MapCell cell)
        {
            switch (cell)
            {
                case MapCell.Ground: return GroundMark;
                case MapCell.Route: return CorridorMark;
                case MapCell.Spawn: return SpawnMark;
                case MapCell.Exit: return ExitMark;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cell), cell, "No mark for this cell.");
            }
        }

        /// <summary>How many corridor neighbours a cell has, off-grid ones absent.</summary>
        /// <remarks>
        /// A hint for the scene overlay and never the authority — see
        /// <see cref="TryParse"/>. It is here rather than in the overlay because
        /// it is a fact about the grid, and the overlay should not be doing hex
        /// arithmetic.
        /// </remarks>
        public int CorridorNeighbours(int column, int row)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            int touching = 0;

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                Hex.ToOddRowOffset(hex.Neighbour(direction), out int otherColumn, out int otherRow);

                if (Holds(otherColumn, otherRow) && CellAt(otherColumn, otherRow) != MapCell.Ground)
                {
                    touching++;
                }
            }

            return touching;
        }

        private void Grid(StringBuilder written, Func<int, int, char> mark)
        {
            for (int row = 0; row < Height; row++)
            {
                if ((row & 1) == 1)
                {
                    written.Append(' ');
                }

                for (int column = 0; column < Width; column++)
                {
                    written.Append(mark(column, row));
                }

                written.Append('\n');
            }
        }
    }
}
