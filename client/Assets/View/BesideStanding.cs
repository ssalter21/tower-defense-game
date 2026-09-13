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
    /// <b>The asked cell first, then behind.</b> The neighbour the art's offset
    /// lands on is taken whenever it is free ground. When it is not, every
    /// other neighbour that is free ground is ranked by how far away from the
    /// corridor it lies, with a smaller weight for lying toward the asked
    /// side -- so the cell sixty degrees behind the taken one wins, the one
    /// behind on the far side comes next, and the cells toward the corridor
    /// come last. Behind is the side a prop can move to without ever standing
    /// where creeps walk, which is what the candidate the sitting signed said
    /// of itself. The corridor is never a candidate at all, whether or not the
    /// route runs through the cell asked for.
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
        /// How much lying toward the asked side counts beside lying away from
        /// the corridor, once the asked cell itself is taken: enough to break
        /// the tie between the two cells equally far behind, and no more.
        /// </summary>
        private const float AskedSideWeight = 0.25f;

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
            int askedDirection = -1;
            float nearestToAsked = float.NegativeInfinity;
            float best = float.NegativeInfinity;
            Vector3 chosen = Vector3.zero;

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                Hex.ToOddRowOffset(hex.Neighbour(direction), out int neighbourColumn, out int neighbourRow);
                Vector3 across = Flat(HexGeometry.ToWorld(neighbourColumn, neighbourRow) - Flat(here)).normalized;
                float towardAsked = Vector3.Dot(across, toward);

                if (towardAsked > nearestToAsked)
                {
                    nearestToAsked = towardAsked;
                    askedDirection = direction;
                }

                if (!IsFreeGround(map, occupied, neighbourColumn, neighbourRow))
                {
                    continue;
                }

                float score = Vector3.Dot(across, behind) + (AskedSideWeight * towardAsked);

                if (score > best)
                {
                    best = score;
                    chosen = Standing(map, here, neighbourColumn, neighbourRow);
                }
            }

            Hex.ToOddRowOffset(hex.Neighbour(askedDirection), out int askedColumn, out int askedRow);

            if (IsFreeGround(map, occupied, askedColumn, askedRow))
            {
                return Quaternion.Inverse(resting) * Standing(map, here, askedColumn, askedRow);
            }

            if (best == float.NegativeInfinity)
            {
                return askedFlat.normalized * InsideTheHex;
            }

            return Quaternion.Inverse(resting) * chosen;
        }

        /// <summary>From the tower's root to the centre of a neighbouring cell, at that cell's own height.</summary>
        private static Vector3 Standing(HexMap map, Vector3 here, int column, int row) =>
            HexGeometry.ToWorld(column, row, map.LevelAt(column, row)) - here;

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
