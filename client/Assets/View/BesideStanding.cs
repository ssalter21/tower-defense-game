using System;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Which tile a beside prop stands on when the one its art asks for has a
    /// tower on it: a free neighbour, chosen for the tower's right and away
    /// from the corridor, and the tower's own hex when there is none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The art asks and the board answers.</b> <see cref="BesideProp.Offset"/>
    /// is where a row's prop would like to stand -- one tile to the right, for
    /// all four signed ones -- and nothing in the art knows what is standing
    /// there. With towers in a row #270 photographed the Mortar's turret, the
    /// Consecration's font and the Overgrowth's weirwood drawn inside the
    /// neighbouring tower's hex. The board is the one thing that knows what is
    /// standing where, so the two views that draw a board
    /// (<see cref="BuildBoard"/>, <see cref="MatchView"/>) ask this with what
    /// they know and hand the answer to <see cref="TowerView"/>, which stands
    /// the prop and does not decide. Signed by #284 on 11 September 2026 over
    /// the alternatives -- the prop tucked inside its own hex, or no prop at all.
    /// </para>
    /// <para>
    /// <b>Ranked, not searched.</b> Every neighbour that is ground with nothing
    /// on it is scored by how far toward the asked direction it lies, with a
    /// smaller weight for lying away from the corridor. The cell the art asked
    /// for wins whenever it is free, because nothing else is as far toward it;
    /// when it is taken, the cell sixty degrees behind it beats the one sixty
    /// degrees in front, and both beat anything on the far side. The corridor
    /// is never a candidate, whether or not the route currently runs through
    /// the cell asked for, because a prop on the route is a prop creeps walk
    /// through.
    /// </para>
    /// <para>
    /// <b>The prop stands on the tile, at the tile's height.</b> The offset
    /// returned is the neighbour's centre rather than the art's own vector, so a
    /// prop on a raised neighbour stands on it rather than floating at its
    /// tower's level beside it. An offset the art keeps inside the tower's own
    /// hex -- shorter than half a tile -- is left exactly as asked, since it
    /// never reaches a neighbour to collide with.
    /// </para>
    /// <para>
    /// <b>What it does not know is other props.</b> Two capstones in a row can
    /// choose one free cell between them. Towers are the only thing counted as
    /// standing, because they are the only thing the board records; the day a
    /// board holds two adjacent capstones with one free cell is the day this
    /// gets a second reading.
    /// </para>
    /// </remarks>
    public static class BesideStanding
    {
        /// <summary>
        /// How far sideways a prop stands when no neighbour is free: inside its
        /// own hex, clear of the body, and on no tile but its own.
        /// </summary>
        public const float InsideTheHex = 0.8f;

        /// <summary>
        /// How much lying away from the corridor counts beside lying toward the
        /// asked direction. Small enough that the asked cell always wins when it
        /// is free; large enough to break the tie between the two cells either
        /// side of it.
        /// </summary>
        private const float BehindWeight = 0.25f;

        /// <summary>
        /// Where the prop of the tower on (<paramref name="column"/>,
        /// <paramref name="row"/>) stands, as an offset in the frame the tower
        /// rests in -- what <see cref="TowerView"/> composes with the resting
        /// facing every frame.
        /// </summary>
        /// <param name="map">The board, for which cells exist and which are corridor.</param>
        /// <param name="occupied">Whether a tower stands on a cell.</param>
        /// <param name="column">The tower's cell.</param>
        /// <param name="row">The tower's cell.</param>
        /// <param name="resting">The facing the tower rests in, which the offset is measured in.</param>
        /// <param name="asked">The art's own offset: where the prop would like to stand.</param>
        public static Vector3 On(
            HexMap map, Func<int, int, bool> occupied, int column, int row, Quaternion resting, Vector3 asked)
        {
            if (map is null) throw new ArgumentNullException(nameof(map));
            if (occupied is null) throw new ArgumentNullException(nameof(occupied));

            Vector3 askedFlat = new Vector3(asked.x, 0f, asked.z);

            if (askedFlat.magnitude < HexGeometry.ColumnPitch * 0.5f)
            {
                return asked;
            }

            Vector3 here = HexGeometry.ToWorld(column, row, map.LevelAt(column, row));
            Vector3 toward = Flat(resting * askedFlat).normalized;
            Vector3 behind = -Flat(resting * Vector3.forward).normalized;

            Hex hex = Hex.FromOddRowOffset(column, row);
            float best = float.NegativeInfinity;
            Vector3 chosen = Vector3.zero;

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                Hex.ToOddRowOffset(hex.Neighbour(direction), out int neighbourColumn, out int neighbourRow);

                if (!IsFreeGround(map, occupied, neighbourColumn, neighbourRow))
                {
                    continue;
                }

                Vector3 delta = HexGeometry.ToWorld(neighbourColumn, neighbourRow, map.LevelAt(neighbourColumn, neighbourRow)) - here;
                Vector3 across = Flat(delta).normalized;
                float score = Vector3.Dot(across, toward) + (BehindWeight * Vector3.Dot(across, behind));

                if (score > best)
                {
                    best = score;
                    chosen = delta;
                }
            }

            if (best == float.NegativeInfinity)
            {
                return askedFlat.normalized * InsideTheHex;
            }

            return Quaternion.Inverse(resting) * chosen;
        }

        private static bool IsFreeGround(HexMap map, Func<int, int, bool> occupied, int column, int row)
        {
            if (column < 0 || column >= map.Width || row < 0 || row >= map.Height)
            {
                return false;
            }

            return map.CellAt(column, row) == MapCell.Ground && !occupied(column, row);
        }

        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);
    }
}
