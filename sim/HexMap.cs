using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>What one cell of the authored grid is.</summary>
    public enum MapCell
    {
        /// <summary>Not corridor. Drawn as grass; the simulation never looks at it again.</summary>
        Ground = 0,

        /// <summary>Corridor.</summary>
        Route = 1,

        /// <summary>The corridor's entrance. Exactly one per map.</summary>
        Spawn = 2,

        /// <summary>The corridor's exit. Exactly one per map.</summary>
        Exit = 3,
    }

    /// <summary>
    /// The playfield: a character grid parsed into cells, the corridor traced
    /// out of it, and a hash over the parsed grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The map is authored as one row of characters per line because that is
    /// diffable and an agent can write it. <c>.</c> is ground, <c>#</c> is
    /// corridor, <c>S</c> is the entrance and <c>E</c> the exit. Every other
    /// character is a load error -- including every digit, which is how this
    /// file stays free of numbers without needing the decimal-point rule the
    /// numeric data files carry.
    /// </para>
    /// <para>
    /// <b>A map is two blocks and not one: the terrain grid, a blank line, and
    /// a level grid of the same shape.</b> A level is <c>a</c>, <c>b</c> or
    /// <c>c</c> -- the three tiers, counted from the ground up -- and it is a
    /// letter for the same reason the terrain is: this file holds no numbers,
    /// so a digit is refused wherever it appears and the decimal-point question
    /// never has to be asked here. The second plane is why a level is a second
    /// block rather than a wider alphabet in the first: the terrain a hex is
    /// and the height it stands at are two facts, and folding them into one
    /// character would need twelve characters to spell four kinds at three
    /// tiers, none of which anybody could read.
    /// </para>
    /// <para>
    /// <b>A row is trimmed at both ends, and the whitespace that goes is
    /// decoration.</b> Odd rows are the shifted ones in odd-r offset, so the
    /// committed file indents them and what is typed then looks like the board
    /// it produces. Nothing about that indent is data -- the hash is over the
    /// parsed grid, so indenting a row moves nothing at all -- and requiring it
    /// would be a parser refusing a grid it had already read correctly. The
    /// trailing end goes for the opposite reason: a space after the last cell
    /// is invisible, and a refusal naming a character nobody can see is a fault
    /// nobody can find.
    /// </para>
    /// <para>
    /// <b>The corridor is asserted well-formed at load: exactly one hex wide,
    /// never branching.</b> Every corridor cell has one or two corridor
    /// neighbours, exactly two of them have one, those two are the entrance and
    /// the exit, and the walk from entrance to exit visits every corridor cell.
    /// A branch, a two-wide stretch, a stray loop and a second disconnected
    /// corridor each fail one of those. Together they are what keeps a
    /// pathfinder out of the simulation by accident: route derivation is this
    /// trace, done once, and there is nothing left for a search to do.
    /// </para>
    /// <para>
    /// <b><see cref="MapHash"/> is over the parsed grid alone</b> -- width,
    /// height, the cell kinds in row-major order and then the levels in the
    /// same order -- and not over the file. So nudging one hex under a stored
    /// record fails loudly whether the map was typed by hand, generated, or
    /// downloaded from somebody else, and rewrapping the comment above it does
    /// nothing at all. Raising one hex a tier is such a nudge: two maps with
    /// the same corridor at different heights are two maps.
    /// </para>
    /// <para>
    /// Adjacency is computed in axial coordinates, through
    /// <see cref="Hex.FromOddRowOffset"/>. The corridor assertion therefore
    /// exercises the canonical odd-r conversion on every cell of every map that
    /// loads, which is a harder test of it than a test would be.
    /// </para>
    /// </remarks>
    public sealed class HexMap
    {
        /// <summary>
        /// The layout this build folds a map hash under. Change what the fold
        /// covers and it bumps, retiring every record pinned to the old
        /// meaning; layout 2 is the one that added the levels.
        /// </summary>
        /// <remarks>
        /// It is public because a reader that knows which layout a stored stamp
        /// was taken under is the only reader that can compare like with like.
        /// See <see cref="MapHashUnder"/>.
        /// </remarks>
        public const int HashLayout = 2;

        /// <summary>How many tiers there are. The letters are <c>a</c> to this many.</summary>
        public const int LevelCount = 3;

        /// <summary>
        /// Names this grid's layout inside the hash. The digit is
        /// <see cref="HashLayout"/>.
        /// </summary>
        private const string HashLabel = "hex-map/2";

        /// <summary>
        /// The label layout 1 folded under: width, height and the cell kinds,
        /// with no levels because there were none. Kept for as long as any
        /// record stamped under it exists, which is the same terms every
        /// retired reader branch is kept on.
        /// </summary>
        private const string LevellessHashLabel = "hex-map/1";

        private const char GroundCharacter = '.';

        private const char RouteCharacter = '#';

        private const char SpawnCharacter = 'S';

        private const char ExitCharacter = 'E';

        /// <summary>The letter the lowest tier is written with; the rest follow it.</summary>
        private const char FirstLevelCharacter = 'a';

        private readonly MapCell[] _cells;

        private readonly byte[] _levels;

        private readonly Hex[] _route;

        private HexMap(int width, int height, MapCell[] cells, byte[] levels, Hex[] route, Hash64 mapHash)
        {
            Width = width;
            Height = height;
            _cells = cells;
            _levels = levels;
            _route = route;
            MapHash = mapHash;
        }

        /// <summary>Columns. Every row has exactly this many characters.</summary>
        public int Width { get; }

        /// <summary>Rows.</summary>
        public int Height { get; }

        /// <summary>
        /// The corridor, entrance first and exit last, one step per cell. This
        /// is the route: derived once, by tracing, and never searched for.
        /// </summary>
        public IReadOnlyList<Hex> Route => _route;

        /// <summary>Where the wave enters.</summary>
        public Hex Spawn => _route[0];

        /// <summary>Where a leak leaves.</summary>
        public Hex Exit => _route[_route.Length - 1];

        /// <summary>The hash over the parsed grid. See the remarks on <see cref="HexMap"/>.</summary>
        public Hash64 MapHash { get; }

        /// <summary>
        /// The same grid folded under a named hash layout, for comparing a
        /// stamp against the layout it was taken under.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A digest under one layout and a digest under another are not two
        /// answers to one question.</b> They are answers to different
        /// questions: layout 1 asked what terrain this grid is, and layout 2
        /// asks what terrain it is and how high each hex of it stands. A reader
        /// holding a stamp from before the levels existed can still check that
        /// stamp exactly -- against the terrain, which is all that stamp ever
        /// covered -- and a reader that compared it against the current fold
        /// would be reporting a layout bump as a corrupted record.
        /// </para>
        /// <para>
        /// <b>This weakens nothing.</b> The comparison stays exact under
        /// whichever layout is named, so a hex nudged in an old record is
        /// refused by the old fold just as loudly as it always was. What is
        /// retired by the bump is a stamp that arrives without its record --
        /// a stored defense pinned to a map hash, matched against a map loaded
        /// today -- because those two are now folded under different layouts
        /// with nothing to say which. A replay bundle is not in that position:
        /// its stamp and its grid travel in the same bytes, under a format
        /// version that says which layout they were written at.
        /// </para>
        /// </remarks>
        public Hash64 MapHashUnder(int layout)
        {
            switch (layout)
            {
                case 1:
                    return Fold(LevellessHashLabel, Width, Height, _cells, levels: null);

                case HashLayout:
                    return MapHash;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layout),
                        layout.ToString(CultureInfo.InvariantCulture)
                        + " is not a map hash layout this build has ever folded under. The layouts are 1, "
                        + "which covered the terrain alone, and "
                        + HashLayout.ToString(CultureInfo.InvariantCulture)
                        + ", which covers the terrain and the levels.");
            }
        }

        /// <summary>Parses a map from text. Not from a path -- see <see cref="DataText"/>.</summary>
        public static HexMap Parse(string text) => Parse("map", text);

        /// <summary>Parses a map from UTF-8 bytes, which is what a caller that read a file holds.</summary>
        public static HexMap ParseUtf8(byte[] utf8) => ParseUtf8("map", utf8);

        /// <summary>Parses a map, naming the content in any error message.</summary>
        public static HexMap ParseUtf8(string source, byte[] utf8) =>
            Parse(source, DataText.FromUtf8(source, utf8));

        /// <summary>Parses a map, naming the content in any error message.</summary>
        public static HexMap Parse(string source, string text)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return FromGrid(new Grid(source, DataText.SplitLines(text)));
        }

        /// <summary>
        /// Builds a map from the parsed grid itself -- width, height, one byte
        /// per cell row-major and then one byte per level in the same order --
        /// which is exactly what a replay bundle inlines and exactly what
        /// <see cref="MapHash"/> covers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the same corridor assertion, not a second one.</b> A map
        /// arriving as bytes inside a stored replay is checked one hex wide and
        /// never branching by the identical code that checks a map arriving as
        /// text, because a second implementation is a second opinion and the
        /// interesting maps are the ones the two would disagree about.
        /// </para>
        /// <para>
        /// Faults are <see cref="ContentException"/> rather than a record error
        /// for the same reason: what is wrong with the grid is wrong with the
        /// grid however it arrived. The <paramref name="source"/> is what names
        /// where it came from.
        /// </para>
        /// </remarks>
        public static HexMap FromCells(string source, int width, int height, byte[] cells, byte[] levels)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (cells is null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (levels is null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            if (width < 1 || height < 1)
            {
                throw new ContentException(
                    source,
                    0,
                    "is "
                    + width.ToString(CultureInfo.InvariantCulture)
                    + " by "
                    + height.ToString(CultureInfo.InvariantCulture)
                    + ", which has no grid in it at all.");
            }

            if ((long)width * height != cells.Length)
            {
                throw new ContentException(
                    source,
                    0,
                    "says it is "
                    + width.ToString(CultureInfo.InvariantCulture)
                    + " by "
                    + height.ToString(CultureInfo.InvariantCulture)
                    + " and carries "
                    + cells.Length.ToString(CultureInfo.InvariantCulture)
                    + " cells. A grid whose shape and contents disagree has no unambiguous reading.");
            }

            if (levels.Length != cells.Length)
            {
                throw new ContentException(
                    source,
                    0,
                    "carries "
                    + cells.Length.ToString(CultureInfo.InvariantCulture)
                    + " cells and "
                    + levels.Length.ToString(CultureInfo.InvariantCulture)
                    + " levels. Every hex stands at a height, so the two planes are the same length or "
                    + "there is a hex whose height nothing states.");
            }

            var parsed = new MapCell[cells.Length];
            var heights = new byte[levels.Length];

            for (int index = 0; index < cells.Length; index++)
            {
                parsed[index] = ReadCellByte(source, width, index, cells[index]);
                heights[index] = ReadLevelByte(source, width, index, levels[index]);
            }

            return FromGrid(new Grid(source, width, height, parsed, heights));
        }

        private static MapCell ReadCellByte(string source, int width, int index, byte value)
        {
            if (value > (byte)MapCell.Exit)
            {
                throw new ContentException(
                    source,
                    (index / width) + 1,
                    "has "
                    + value.ToString(CultureInfo.InvariantCulture)
                    + " at column "
                    + ((index % width) + 1).ToString(CultureInfo.InvariantCulture)
                    + ". A cell byte is 0 for ground, 1 for corridor, 2 for the entrance or 3 for the "
                    + "exit, and a byte outside that range is refused rather than read as ground.");
            }

            return (MapCell)value;
        }

        /// <summary>
        /// One byte of the level plane. The terrain plane's validation above is
        /// untouched by the levels arriving, which is the whole reason they
        /// arrive as a second plane rather than as a widened cell encoding.
        /// </summary>
        private static byte ReadLevelByte(string source, int width, int index, byte value)
        {
            if (value >= LevelCount)
            {
                throw new ContentException(
                    source,
                    (index / width) + 1,
                    "has level "
                    + value.ToString(CultureInfo.InvariantCulture)
                    + " at column "
                    + ((index % width) + 1).ToString(CultureInfo.InvariantCulture)
                    + ". There are "
                    + LevelCount.ToString(CultureInfo.InvariantCulture)
                    + " tiers, counted from the ground up, so a level byte is 0, 1 or 2 -- and one "
                    + "outside that range is refused rather than flattened onto the tier below it.");
            }

            return value;
        }

        private static HexMap FromGrid(Grid grid)
        {
            return new HexMap(
                grid.Width,
                grid.Height,
                grid.Cells,
                grid.Levels,
                grid.TraceCorridor(),
                Fold(HashLabel, grid.Width, grid.Height, grid.Cells, grid.Levels));
        }

        /// <summary>
        /// The grid folded under one layout's label: the shape, then the
        /// terrain plane, then the level plane where that layout has one.
        /// </summary>
        private static Hash64 Fold(string label, int width, int height, MapCell[] cells, byte[]? levels)
        {
            Hash64 hash = Hash64.Start(label).Add(width).Add(height);

            for (int index = 0; index < cells.Length; index++)
            {
                hash = hash.Add((int)cells[index]);
            }

            if (levels is null)
            {
                return hash;
            }

            for (int index = 0; index < levels.Length; index++)
            {
                hash = hash.Add(levels[index]);
            }

            return hash;
        }

        /// <summary>The cell at an offset column and row.</summary>
        public MapCell CellAt(int column, int row)
        {
            RequireOnTheGrid(column, row);

            return _cells[(row * Width) + column];
        }

        /// <summary>
        /// The tier the hex at an offset column and row stands at: zero for the
        /// ground, up to <see cref="LevelCount"/> minus one.
        /// </summary>
        /// <remarks>
        /// The level belongs to the hex and not to whatever stands on it, which
        /// is why a tower carries no level of its own: it stands on a hex, and
        /// the hex is asked.
        /// </remarks>
        public int LevelAt(int column, int row)
        {
            RequireOnTheGrid(column, row);

            return _levels[(row * Width) + column];
        }

        /// <summary>
        /// The grid as one byte per cell, row-major -- exactly the bytes the
        /// replay record carries, and exactly what <see cref="MapHash"/> covers
        /// after the width and the height.
        /// </summary>
        public byte[] ToCellBytes()
        {
            var bytes = new byte[_cells.Length];

            for (int index = 0; index < _cells.Length; index++)
            {
                bytes[index] = (byte)_cells[index];
            }

            return bytes;
        }

        /// <summary>
        /// The level plane as one byte per cell, row-major -- the second run of
        /// bytes a replay record carries, and what <see cref="MapHash"/> covers
        /// after the terrain.
        /// </summary>
        public byte[] ToLevelBytes() => (byte[])_levels.Clone();

        private void RequireOnTheGrid(int column, int row)
        {
            if (column < 0 || column >= Width || row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(column),
                    "("
                    + column.ToString(CultureInfo.InvariantCulture)
                    + ", "
                    + row.ToString(CultureInfo.InvariantCulture)
                    + ") is off a "
                    + Width.ToString(CultureInfo.InvariantCulture)
                    + " by "
                    + Height.ToString(CultureInfo.InvariantCulture)
                    + " grid.");
            }
        }

        /// <summary>
        /// One parse in progress. It exists so that every message can name the
        /// line of the file the fault is on rather than the row of the grid,
        /// which are different numbers as soon as the map carries a comment.
        /// </summary>
        private sealed class Grid
        {
            /// <summary>Why a terrain row of the wrong width is refused rather than padded.</summary>
            private const string RaggedGrid =
                "A ragged grid has no unambiguous reading, so it is refused rather than padded.";

            /// <summary>Why the level block has to be the shape of the terrain block.</summary>
            private const string OneBoardTwice =
                "The two blocks are one board seen twice, so a level grid of another shape leaves a hex "
                + "whose height nothing states.";

            private readonly string _source;

            private readonly int _firstLine;

            internal Grid(string source, string[] lines)
            {
                _source = source;

                List<Block> blocks = Blocks(source, lines);
                Block terrain = blocks[0];
                Block levels = blocks[1];

                _firstLine = terrain.FirstLine;

                Height = terrain.Rows.Count;
                Width = terrain.Rows[0].Length;
                Cells = new MapCell[Width * Height];
                Levels = new byte[Width * Height];

                for (int row = 0; row < Height; row++)
                {
                    string line = Row(source, terrain, row, Width, "the first row", RaggedGrid);

                    for (int column = 0; column < Width; column++)
                    {
                        Cells[(row * Width) + column] =
                            ReadCell(source, terrain.FirstLine + row, column, line[column]);
                    }
                }

                if (levels.Rows.Count != Height)
                {
                    throw new ContentException(
                        source,
                        levels.FirstLine,
                        "opens a level grid "
                        + levels.Rows.Count.ToString(CultureInfo.InvariantCulture)
                        + " rows deep where the terrain grid above it is "
                        + Height.ToString(CultureInfo.InvariantCulture)
                        + ". "
                        + OneBoardTwice);
                }

                for (int row = 0; row < Height; row++)
                {
                    string line = Row(source, levels, row, Width, "the terrain grid", OneBoardTwice);

                    for (int column = 0; column < Width; column++)
                    {
                        Levels[(row * Width) + column] =
                            ReadLevel(source, levels.FirstLine + row, column, line[column]);
                    }
                }
            }

            /// <summary>
            /// A grid that is already cells. The line numbers in any message are
            /// grid rows counted from one, because bytes inside a record have no
            /// lines for them to be offset from.
            /// </summary>
            internal Grid(string source, int width, int height, MapCell[] cells, byte[] levels)
            {
                _source = source;
                _firstLine = 1;
                Width = width;
                Height = height;
                Cells = cells;
                Levels = levels;
            }

            internal int Width { get; }

            internal int Height { get; }

            internal MapCell[] Cells { get; }

            /// <summary>The tier every hex stands at, row-major, beside <see cref="Cells"/>.</summary>
            internal byte[] Levels { get; }

            /// <summary>
            /// The corridor assertion, and the route it produces. Everything
            /// the phrase "exactly one hex wide, never branching" means is
            /// checked here; nothing about it is left to a comment.
            /// </summary>
            internal Hex[] TraceCorridor()
            {
                int corridorCells = 0;
                int spawnIndex = -1;
                int exitIndex = -1;

                for (int index = 0; index < Cells.Length; index++)
                {
                    switch (Cells[index])
                    {
                        case MapCell.Route:
                            corridorCells++;
                            break;

                        case MapCell.Spawn:
                            corridorCells++;
                            spawnIndex = Single(spawnIndex, index, "entrance", SpawnCharacter);
                            break;

                        case MapCell.Exit:
                            corridorCells++;
                            exitIndex = Single(exitIndex, index, "exit", ExitCharacter);
                            break;
                    }
                }

                if (spawnIndex < 0 || exitIndex < 0)
                {
                    throw Fault(
                        0,
                        "has no "
                        + (spawnIndex < 0 ? "entrance '" + SpawnCharacter + "'" : "exit '" + ExitCharacter + "'")
                        + ". A corridor with an unmarked end has no direction, and a wave has to enter "
                        + "somewhere the map states rather than somewhere the reader infers.");
                }

                if (corridorCells < 2)
                {
                    throw Fault(0, "has a corridor shorter than two cells.");
                }

                var degree = new int[Cells.Length];

                for (int index = 0; index < Cells.Length; index++)
                {
                    if (!IsCorridor(Cells[index]))
                    {
                        continue;
                    }

                    degree[index] = NeighbourCount(index);

                    if (degree[index] > 2)
                    {
                        throw At(
                            index,
                            "branches: that corridor cell has "
                            + degree[index].ToString(CultureInfo.InvariantCulture)
                            + " corridor neighbours where a corridor exactly one hex wide allows two. A "
                            + "junction, a two-wide stretch and a blob all read as this, and any of them "
                            + "would need the pathfinder this simulation is never going to have.");
                    }

                    if (degree[index] == 0)
                    {
                        throw At(index, "is an isolated corridor cell, joined to nothing.");
                    }
                }

                for (int index = 0; index < Cells.Length; index++)
                {
                    if (!IsCorridor(Cells[index]) || degree[index] != 1)
                    {
                        continue;
                    }

                    if (index != spawnIndex && index != exitIndex)
                    {
                        throw At(
                            index,
                            "is a dead end that is neither the entrance nor the exit. A corridor has "
                            + "exactly two ends and both of them are marked.");
                    }
                }

                if (degree[spawnIndex] != 1)
                {
                    throw At(spawnIndex, "is marked as the entrance but has more than one corridor neighbour.");
                }

                if (degree[exitIndex] != 1)
                {
                    throw At(exitIndex, "is marked as the exit but has more than one corridor neighbour.");
                }

                return Walk(corridorCells, spawnIndex, exitIndex);
            }

            /// <summary>
            /// The blocks of rows the file is made of, blank lines and comments
            /// between them, each row stripped of the whitespace it was drawn
            /// with. There are exactly two or the map is refused.
            /// </summary>
            /// <remarks>
            /// <b>A second block used to be the fault and is now the
            /// requirement.</b> The map holds two planes -- what each hex is,
            /// and how high it stands -- and neither of them is optional: a
            /// hex whose height nothing states is a hex the reader would have
            /// to invent a height for, which is the one thing a reader may
            /// never do.
            /// </remarks>
            /// <summary>
            /// One row of a block, as wide as the grid says or a fault naming
            /// the reader's own line.
            /// </summary>
            /// <remarks>
            /// Both blocks come through here because both are checked the same
            /// way and only the sentence differs. Two copies of a width check
            /// are two chances for one of them to start padding.
            /// </remarks>
            private static string Row(
                string source,
                Block block,
                int row,
                int width,
                string against,
                string because)
            {
                string line = block.Rows[row];

                if (line.Length != width)
                {
                    throw new ContentException(
                        source,
                        block.FirstLine + row,
                        "is "
                        + line.Length.ToString(CultureInfo.InvariantCulture)
                        + " characters wide where "
                        + against
                        + " is "
                        + width.ToString(CultureInfo.InvariantCulture)
                        + ". "
                        + because);
                }

                return line;
            }

            private static List<Block> Blocks(string source, string[] lines)
            {
                var blocks = new List<Block>();
                int index = 0;

                while (index < lines.Length)
                {
                    while (index < lines.Length && IsBlankOrMapComment(lines[index]))
                    {
                        index++;
                    }

                    if (index == lines.Length)
                    {
                        break;
                    }

                    var block = new Block(index + 1);

                    while (index < lines.Length && !IsBlankOrMapComment(lines[index]))
                    {
                        block.Rows.Add(lines[index].Trim());
                        index++;
                    }

                    blocks.Add(block);
                }

                if (blocks.Count == 0)
                {
                    throw new ContentException(source, 0, "has no grid in it at all.");
                }

                if (blocks.Count == 1)
                {
                    throw new ContentException(
                        source,
                        blocks[0].FirstLine + blocks[0].Rows.Count,
                        "ends after one block of rows, and a map is two: the terrain grid, a blank line, "
                        + "then a level grid of the same shape carrying '"
                        + FirstLevelCharacter
                        + "', '"
                        + (char)(FirstLevelCharacter + 1)
                        + "' or '"
                        + (char)(FirstLevelCharacter + LevelCount - 1)
                        + "' on every hex. The level block is missing, and there is no height a reader "
                        + "could supply on its behalf.");
                }

                if (blocks.Count > 2)
                {
                    throw new ContentException(
                        source,
                        blocks[2].FirstLine,
                        "opens a third block of rows. A map holds two -- the terrain and the levels -- so "
                        + "anything after them is either a stray edit, a second map that nothing will "
                        + "ever read, or a comment line between two rows of one grid, which splits it "
                        + "into two the same way a blank line would.");
                }

                return blocks;
            }

            /// <summary>
            /// A map comment starts with <c>//</c> rather than <c>#</c>, which
            /// is what the numeric data files use. <c>#</c> is a corridor cell
            /// here, so sharing the marker would make a corridor beginning at
            /// column one vanish into a comment -- silently, and only on some
            /// maps.
            /// </summary>
            private static bool IsBlankOrMapComment(string line)
            {
                for (int index = 0; index < line.Length; index++)
                {
                    char character = line[index];

                    if (character == ' ' || character == '\t')
                    {
                        continue;
                    }

                    return character == '/' && index + 1 < line.Length && line[index + 1] == '/';
                }

                return true;
            }

            private static MapCell ReadCell(string source, int line, int column, char character)
            {
                switch (character)
                {
                    case GroundCharacter:
                        return MapCell.Ground;

                    case RouteCharacter:
                        return MapCell.Route;

                    case SpawnCharacter:
                        return MapCell.Spawn;

                    case ExitCharacter:
                        return MapCell.Exit;

                    default:
                        throw new ContentException(
                            source,
                            line,
                            "has '"
                            + character
                            + "' at column "
                            + (column + 1).ToString(CultureInfo.InvariantCulture)
                            + ". A map cell is '"
                            + GroundCharacter
                            + "' for ground, '"
                            + RouteCharacter
                            + "' for corridor, '"
                            + SpawnCharacter
                            + "' for the entrance or '"
                            + ExitCharacter
                            + "' for the exit, and nothing else -- a digit least of all, because this "
                            + "file holds no numbers.");
                }
            }

            /// <summary>
            /// One character of the level grid. Letters and never digits, for
            /// the reason <see cref="ReadCell"/> gives: this file holds no
            /// numbers, so it never has to answer the decimal-point question
            /// the numeric data files do.
            /// </summary>
            private static byte ReadLevel(string source, int line, int column, char character)
            {
                int level = character - FirstLevelCharacter;

                if (level < 0 || level >= LevelCount)
                {
                    throw new ContentException(
                        source,
                        line,
                        "has '"
                        + character
                        + "' at column "
                        + (column + 1).ToString(CultureInfo.InvariantCulture)
                        + " of the level grid. A level is '"
                        + FirstLevelCharacter
                        + "' for the ground, '"
                        + (char)(FirstLevelCharacter + 1)
                        + "' for the tier above it or '"
                        + (char)(FirstLevelCharacter + LevelCount - 1)
                        + "' for the one above that, and nothing else -- a digit least of all, because "
                        + "this file holds no numbers.");
                }

                return (byte)level;
            }

            private static bool IsCorridor(MapCell cell) =>
                cell == MapCell.Route || cell == MapCell.Spawn || cell == MapCell.Exit;

            private Hex[] Walk(int corridorCells, int spawnIndex, int exitIndex)
            {
                var route = new Hex[corridorCells];

                int current = spawnIndex;
                int previous = -1;

                for (int step = 0; step < corridorCells; step++)
                {
                    route[step] = HexAt(current);

                    int next = NextStep(current, previous);

                    if (next >= 0)
                    {
                        previous = current;
                        current = next;
                        continue;
                    }

                    if (current != exitIndex)
                    {
                        throw At(current, "runs out of corridor before the walk from the entrance reaches the exit.");
                    }

                    if (step + 1 != corridorCells)
                    {
                        throw Fault(
                            0,
                            "has "
                            + corridorCells.ToString(CultureInfo.InvariantCulture)
                            + " corridor cells, but the walk from the entrance to the exit visits only "
                            + (step + 1).ToString(CultureInfo.InvariantCulture)
                            + ". The rest are a second corridor, or a ring that touches nothing -- either "
                            + "way there is more than one thing here calling itself the route.");
                    }

                    return route;
                }

                throw Fault(
                    0,
                    "has a corridor that closes on itself: the walk from the entrance kept finding a next "
                    + "cell after it had already visited every one of them.");
            }

            private int NextStep(int index, int previous)
            {
                Hex hex = HexAt(index);

                for (int direction = 0; direction < Hex.DirectionCount; direction++)
                {
                    int neighbour = IndexOf(hex.Neighbour(direction));

                    if (neighbour < 0 || neighbour == previous || !IsCorridor(Cells[neighbour]))
                    {
                        continue;
                    }

                    return neighbour;
                }

                return -1;
            }

            private int NeighbourCount(int index)
            {
                Hex hex = HexAt(index);
                int count = 0;

                for (int direction = 0; direction < Hex.DirectionCount; direction++)
                {
                    int neighbour = IndexOf(hex.Neighbour(direction));

                    if (neighbour >= 0 && IsCorridor(Cells[neighbour]))
                    {
                        count++;
                    }
                }

                return count;
            }

            private Hex HexAt(int index) => Hex.FromOddRowOffset(index % Width, index / Width);

            private int IndexOf(Hex hex)
            {
                Hex.ToOddRowOffset(hex, out int column, out int row);

                if (column < 0 || column >= Width || row < 0 || row >= Height)
                {
                    return -1;
                }

                return (row * Width) + column;
            }

            private int Single(int already, int index, string what, char character)
            {
                if (already >= 0)
                {
                    throw At(index, "is a second '" + character + "'. A map has exactly one " + what + ".");
                }

                return index;
            }

            private ContentException At(int index, string message) =>
                Fault(
                    _firstLine + (index / Width),
                    "column " + ((index % Width) + 1).ToString(CultureInfo.InvariantCulture) + " " + message);

            private ContentException Fault(int line, string message) =>
                new ContentException(_source, line, message);

            /// <summary>
            /// One run of rows with blank lines either side of it, and the line
            /// of the file its first row is on -- which is what lets a fault in
            /// the second block name the reader's own line rather than a row
            /// number counted from somewhere else.
            /// </summary>
            private sealed class Block
            {
                internal Block(int firstLine)
                {
                    FirstLine = firstLine;
                    Rows = new List<string>();
                }

                internal int FirstLine { get; }

                internal List<string> Rows { get; }
            }
        }
    }
}
