using Sim;

namespace View
{
    /// <summary>
    /// Which tile model a cell is drawn with, and how far it is turned.
    /// </summary>
    /// <remarks>
    /// The names are shapes rather than the pack's letters. KayKit calls the
    /// straight piece <c>hex_road_A</c> and the dead end <c>hex_road_M</c>, and
    /// those letters mean nothing outside that pack — binding them to a shape
    /// happens once, in the tile set, so swapping packs is a rebinding rather
    /// than a rewrite.
    /// </remarks>
    public enum TilePiece
    {
        /// <summary>Not corridor. Plain ground, no path on it.</summary>
        Ground,

        /// <summary>Corridor entering and leaving by opposite edges.</summary>
        Straight,

        /// <summary>Corridor turning by 120 degrees — one edge skipped between.</summary>
        Curve,

        /// <summary>Corridor turning by 60 degrees, into an adjacent edge.</summary>
        Hairpin,

        /// <summary>The spawn and the exit: corridor with exactly one neighbour.</summary>
        DeadEnd,

        /// <summary>A straight run whose far edge stands one tier higher.</summary>
        StraightRamp,
    }

    /// <summary>A tile model and the turn that orients it. Rotation is in sixths.</summary>
    public readonly struct TileChoice
    {
        public TileChoice(TilePiece piece, int rotation)
        {
            Piece = piece;
            Rotation = rotation;
        }

        /// <summary>Which model.</summary>
        public TilePiece Piece { get; }

        /// <summary>
        /// How many sixths of a turn to rotate it by, 0 to 5, counted the way
        /// <see cref="Sim.Hex.Neighbour"/> counts directions.
        /// </summary>
        public int Rotation { get; }
    }

    /// <summary>
    /// Picks the tile for every cell of a map. Pure: no engine type, no asset,
    /// no scene — a map goes in and a choice comes out, so the rule can be
    /// tested without an editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A cell's tile is decided by which of its six neighbours are corridor,
    /// and by nothing else.</b> Not by where it sits on the route, not by which
    /// way a creep walks over it, not by whether it is the third bend or the
    /// fourth — a tile that depended on any of those would make the floor
    /// disagree with the map under a redraw. The corridor assertion in
    /// <see cref="HexMap"/> guarantees one or two corridor neighbours per
    /// corridor cell, so three-way and larger junctions are unreachable by
    /// construction and the pack's nine junction pieces are never selected.
    /// </para>
    /// <para>
    /// <b>Directions are the simulation's, and the pack's pieces are described
    /// in them.</b> <see cref="Sim.Hex.Neighbour"/> indexes 0=E, 1=NE, 2=NW,
    /// 3=W, 4=SW, 5=SE, and each piece below is authored with its road meeting
    /// a known subset of those. Rotating a piece by <c>k</c> sixths adds
    /// <c>k</c> to every direction in that subset, which is the whole of the
    /// arithmetic.
    /// </para>
    /// <para>
    /// <b>A ramp is a straight whose far end is higher, and the map guarantees
    /// it can be.</b> <c>content/map.txt</c> requires every tier change to sit
    /// between two same-row corridor cells with corridor either side, precisely
    /// because the pack ships a slope for its straight piece and for nothing
    /// else. The ramp tile is placed on the <em>lower</em> of the two cells and
    /// climbs to meet the higher, so the two cells' surfaces join.
    /// </para>
    /// </remarks>
    public static class RoadTiling
    {
        /// <summary>Edges the straight piece's road meets: opposite, E and W.</summary>
        private static readonly int[] StraightEdges = { 0, 3 };

        /// <summary>Edges the 120-degree curve meets: E and SW, one edge skipped.</summary>
        private static readonly int[] CurveEdges = { 0, 4 };

        /// <summary>Edges the 60-degree hairpin meets: E and SE, which are adjacent.</summary>
        private static readonly int[] HairpinEdges = { 0, 5 };

        /// <summary>The one edge a dead end meets: E.</summary>
        private static readonly int[] DeadEndEdges = { 0 };

        /// <summary>
        /// The edge a ramp climbs towards. The slope's road runs E to W and
        /// rises westwards, so this is the piece's high side before any
        /// rotation, and its low side is the opposite edge.
        /// </summary>
        public const int RampHighEdge = 3;

        /// <summary>
        /// The edges a piece's road meets before it is turned, as the six-bit
        /// set <see cref="CorridorEdges"/> speaks in.
        /// </summary>
        /// <remarks>
        /// <b>Public because these numbers are a claim about the models, and a
        /// claim about a model should be checked against the model.</b> They
        /// were once typed from a probe of the pack's glTF, which is right
        /// handed where Unity is left handed, and every piece but the
        /// straight � the one shape symmetric enough to survive a mirroring �
        /// came out turned. Nothing failed, because nothing was comparing them
        /// to anything. <c>RoadTilingMeshTests</c> now does.
        /// </remarks>
        public static int EdgesOf(TilePiece piece)
        {
            int[] edges = Table(piece);
            int set = 0;

            for (int index = 0; index < edges.Length; index++)
            {
                set |= 1 << edges[index];
            }

            return set;
        }

        private static int[] Table(TilePiece piece) =>
            piece switch
            {
                TilePiece.Ground => System.Array.Empty<int>(),
                TilePiece.Straight => StraightEdges,
                TilePiece.Curve => CurveEdges,
                TilePiece.Hairpin => HairpinEdges,
                TilePiece.DeadEnd => DeadEndEdges,
                TilePiece.StraightRamp => StraightEdges,
                _ => throw new System.InvalidOperationException("No edge table for " + piece + "."),
            };

        /// <summary>
        /// The tile for one cell of the grid.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// If a corridor cell's neighbours match no piece. That cannot happen
        /// for a map the loader accepted, so it is a throw rather than a
        /// fallback — a silently wrong tile is a floor that lies about where
        /// the path goes.
        /// </exception>
        public static TileChoice For(HexMap map, int column, int row)
        {
            if (map.CellAt(column, row) == MapCell.Ground)
            {
                return new TileChoice(TilePiece.Ground, 0);
            }

            int edges = CorridorEdges(map, column, row);

            if (TryRamp(map, column, row, edges, out TileChoice ramp))
            {
                return ramp;
            }

            if (TryMatch(StraightEdges, edges, out int turn))
            {
                return new TileChoice(TilePiece.Straight, turn);
            }

            if (TryMatch(CurveEdges, edges, out turn))
            {
                return new TileChoice(TilePiece.Curve, turn);
            }

            if (TryMatch(HairpinEdges, edges, out turn))
            {
                return new TileChoice(TilePiece.Hairpin, turn);
            }

            if (TryMatch(DeadEndEdges, edges, out turn))
            {
                return new TileChoice(TilePiece.DeadEnd, turn);
            }

            throw new System.InvalidOperationException(
                "The corridor cell at "
                + column.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + ","
                + row.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " has corridor on edges "
                + Describe(edges)
                + ", which no tile in the set has. A map the loader accepted cannot"
                + " reach this, so either the loader's corridor assertion or this"
                + " table is wrong.");
        }

        /// <summary>
        /// Which of a cell's six neighbours are corridor, as a six-bit set
        /// indexed by <see cref="Sim.Hex.Neighbour"/>'s directions.
        /// </summary>
        /// <remarks>
        /// A bit set rather than a list, so that comparing a cell against a
        /// piece is one integer comparison and rotating a piece is a rotate of
        /// six bits. Off-grid neighbours are simply absent, which is what makes
        /// a corridor cell on the border read as a dead end without a special
        /// case for the border.
        /// </remarks>
        public static int CorridorEdges(HexMap map, int column, int row)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            int edges = 0;

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                Hex neighbour = hex.Neighbour(direction);
                Hex.ToOddRowOffset(neighbour, out int otherColumn, out int otherRow);

                if (otherColumn < 0 || otherColumn >= map.Width || otherRow < 0 || otherRow >= map.Height)
                {
                    continue;
                }

                if (map.CellAt(otherColumn, otherRow) != MapCell.Ground)
                {
                    edges |= 1 << direction;
                }
            }

            return edges;
        }

        /// <summary>
        /// A ramp if this cell is a straight run standing one tier below a
        /// corridor neighbour. The rotation puts the piece's climbing edge on
        /// that neighbour.
        /// </summary>
        private static bool TryRamp(HexMap map, int column, int row, int edges, out TileChoice choice)
        {
            choice = default;

            if (!TryMatch(StraightEdges, edges, out _))
            {
                return false;
            }

            int here = map.LevelAt(column, row);
            Hex hex = Hex.FromOddRowOffset(column, row);

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                if ((edges & (1 << direction)) == 0)
                {
                    continue;
                }

                Hex neighbour = hex.Neighbour(direction);
                Hex.ToOddRowOffset(neighbour, out int otherColumn, out int otherRow);

                if (map.LevelAt(otherColumn, otherRow) != here + 1)
                {
                    continue;
                }

                // The piece climbs towards its own high edge, so the turn that
                // puts that edge on this neighbour is the turn we want.
                choice = new TileChoice(TilePiece.StraightRamp, Turn(RampHighEdge, direction));

                return true;
            }

            return false;
        }

        /// <summary>
        /// True if the piece's edges, turned by some sixth of a rotation, are
        /// exactly the cell's. The turn is the one that does it.
        /// </summary>
        private static bool TryMatch(int[] piece, int edges, out int turn)
        {
            for (turn = 0; turn < Hex.DirectionCount; turn++)
            {
                int rotated = 0;

                for (int index = 0; index < piece.Length; index++)
                {
                    rotated |= 1 << ((piece[index] + turn) % Hex.DirectionCount);
                }

                if (rotated == edges)
                {
                    return true;
                }
            }

            turn = 0;

            return false;
        }

        /// <summary>Sixths of a turn taking <paramref name="from"/> onto <paramref name="to"/>.</summary>
        private static int Turn(int from, int to) =>
            ((to - from) + Hex.DirectionCount) % Hex.DirectionCount;

        /// <summary>The directions in a bit set, named, for a message a human reads.</summary>
        private static string Describe(int edges)
        {
            string[] names = { "E", "NE", "NW", "W", "SW", "SE" };
            var written = new System.Text.StringBuilder();

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                if ((edges & (1 << direction)) == 0)
                {
                    continue;
                }

                if (written.Length > 0)
                {
                    written.Append(',');
                }

                written.Append(names[direction]);
            }

            return written.Length == 0 ? "(none)" : written.ToString();
        }
    }
}
