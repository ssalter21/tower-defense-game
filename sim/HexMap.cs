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
    /// height and the cell kinds in row-major order -- and not over the file.
    /// So nudging one hex under a stored record fails loudly whether the map
    /// was typed by hand, generated, or downloaded from somebody else, and
    /// rewrapping the comment above it does nothing at all.
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
        /// Names this grid's layout inside the hash. The digit is the layout
        /// version: change what a cell byte means and it bumps, retiring every
        /// record pinned to the old meaning.
        /// </summary>
        private const string HashLabel = "hex-map/1";

        private const char GroundCharacter = '.';

        private const char RouteCharacter = '#';

        private const char SpawnCharacter = 'S';

        private const char ExitCharacter = 'E';

        private readonly MapCell[] _cells;

        private readonly Hex[] _route;

        private HexMap(int width, int height, MapCell[] cells, Hex[] route, Hash64 mapHash)
        {
            Width = width;
            Height = height;
            _cells = cells;
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
        /// Builds a map from the parsed grid itself -- width, height and one
        /// byte per cell, row-major -- which is exactly what a replay bundle
        /// inlines and exactly what <see cref="MapHash"/> covers.
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
        public static HexMap FromCells(string source, int width, int height, byte[] cells)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (cells is null)
            {
                throw new ArgumentNullException(nameof(cells));
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

            var parsed = new MapCell[cells.Length];

            for (int index = 0; index < cells.Length; index++)
            {
                parsed[index] = ReadCellByte(source, width, index, cells[index]);
            }

            return FromGrid(new Grid(source, width, height, parsed));
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

        private static HexMap FromGrid(Grid grid)
        {
            Hash64 hash = Hash64.Start(HashLabel).Add(grid.Width).Add(grid.Height);

            for (int index = 0; index < grid.Cells.Length; index++)
            {
                hash = hash.Add((int)grid.Cells[index]);
            }

            return new HexMap(grid.Width, grid.Height, grid.Cells, grid.TraceCorridor(), hash);
        }

        /// <summary>The cell at an offset column and row.</summary>
        public MapCell CellAt(int column, int row)
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

            return _cells[(row * Width) + column];
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
        /// One parse in progress. It exists so that every message can name the
        /// line of the file the fault is on rather than the row of the grid,
        /// which are different numbers as soon as the map carries a comment.
        /// </summary>
        private sealed class Grid
        {
            private readonly string _source;

            private readonly int _firstLine;

            internal Grid(string source, string[] lines)
            {
                _source = source;

                List<string> rows = Rows(source, lines, out _firstLine);

                Height = rows.Count;
                Width = rows[0].Length;
                Cells = new MapCell[Width * Height];

                for (int row = 0; row < Height; row++)
                {
                    string line = rows[row];

                    if (line.Length != Width)
                    {
                        throw new ContentException(
                            source,
                            _firstLine + row,
                            "is "
                            + line.Length.ToString(CultureInfo.InvariantCulture)
                            + " characters wide where the first row is "
                            + Width.ToString(CultureInfo.InvariantCulture)
                            + ". A ragged grid has no unambiguous reading, so it is refused rather than "
                            + "padded.");
                    }

                    for (int column = 0; column < Width; column++)
                    {
                        Cells[(row * Width) + column] = ReadCell(source, _firstLine + row, column, line[column]);
                    }
                }
            }

            /// <summary>
            /// A grid that is already cells. The line numbers in any message are
            /// grid rows counted from one, because bytes inside a record have no
            /// lines for them to be offset from.
            /// </summary>
            internal Grid(string source, int width, int height, MapCell[] cells)
            {
                _source = source;
                _firstLine = 1;
                Width = width;
                Height = height;
                Cells = cells;
            }

            internal int Width { get; }

            internal int Height { get; }

            internal MapCell[] Cells { get; }

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

            private static List<string> Rows(string source, string[] lines, out int firstLine)
            {
                var rows = new List<string>();
                int index = 0;

                while (index < lines.Length && IsBlankOrMapComment(lines[index]))
                {
                    index++;
                }

                firstLine = index + 1;

                while (index < lines.Length && !IsBlankOrMapComment(lines[index]))
                {
                    rows.Add(lines[index]);
                    index++;
                }

                for (int after = index; after < lines.Length; after++)
                {
                    if (!IsBlankOrMapComment(lines[after]))
                    {
                        throw new ContentException(
                            source,
                            after + 1,
                            "comes after the grid, with a blank line between them. A map holds one grid, "
                            + "so a second block of rows is either a stray edit or a second map that "
                            + "nothing will ever read.");
                    }
                }

                if (rows.Count == 0)
                {
                    throw new ContentException(source, 0, "has no grid in it at all.");
                }

                return rows;
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
        }
    }
}
