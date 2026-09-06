using System.Collections.Generic;
using Sim;

namespace View
{
    /// <summary>
    /// The families of scenery a board is dressed with. A group is a slot the
    /// art fills, not a model: how many models are behind one, and which, is the
    /// tile set's business and can change without this file moving.
    /// </summary>
    public enum SceneryGroup
    {
        /// <summary>Small things that stand near a rim: rocks, stumps, barrels, a haybale.</summary>
        RimProp,

        /// <summary>The register of a defended camp: a tent, a weapon rack, a target.</summary>
        Camp,

        /// <summary>A stand of trees, filling most of a hex.</summary>
        Grove,

        /// <summary>A mountain. Tall enough to be a silhouette rather than a detail.</summary>
        Peak,

        /// <summary>
        /// A low mound, sat on the lip where the ground falls to a lower
        /// neighbour so that the step between two levels has something growing
        /// over it instead of a clean hexagonal edge.
        /// </summary>
        Hill,

        /// <summary>Above the board, touching nothing.</summary>
        Cloud,
    }

    /// <summary>
    /// One thing to draw, and where. Offsets are in metres from the cell's
    /// centre, turn is in degrees about Y.
    /// </summary>
    public readonly struct SceneryPlacement
    {
        public SceneryPlacement(
            SceneryGroup group,
            int variant,
            int column,
            int row,
            float offsetX,
            float offsetY,
            float offsetZ,
            float turn,
            float scale)
            : this(group, variant, null, column, row, offsetX, offsetY, offsetZ, turn, scale)
        {
        }

        private SceneryPlacement(
            SceneryGroup group,
            int variant,
            string model,
            int column,
            int row,
            float offsetX,
            float offsetY,
            float offsetZ,
            float turn,
            float scale)
        {
            Group = group;
            Variant = variant;
            Model = model;
            Column = column;
            Row = row;
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
            Turn = turn;
            Scale = scale;
        }

        /// <summary>
        /// A piece that names one model out of the imported art, rather than
        /// asking a family for its n-th.
        /// </summary>
        /// <remarks>
        /// <b>The two addressings answer different questions and neither
        /// replaces the other.</b> The generator scatters a board it has never
        /// seen, so it must be able to ask for "a grove" without knowing what
        /// groves exist — that is <see cref="Group"/> and <see cref="Variant"/>,
        /// and it stays exactly as it was. A person placing a thing has already
        /// looked at it and means <em>that one</em>; giving them an index into a
        /// list of four thousand would be asking them to count. So a named piece
        /// carries its name, and the family it does not belong to is
        /// <see cref="SceneryGroup.RimProp"/> only because the field cannot be
        /// absent.
        /// </remarks>
        public static SceneryPlacement Named(
            string model,
            int column,
            int row,
            float offsetX,
            float offsetY,
            float offsetZ,
            float turn,
            float scale) =>
            new SceneryPlacement(
                SceneryGroup.RimProp, 0, model, column, row, offsetX, offsetY, offsetZ, turn, scale);

        /// <summary>Which family. Meaningless where <see cref="Model"/> is set.</summary>
        public SceneryGroup Group { get; }

        /// <summary>Which model within that family, by index. Wrapped by the set.</summary>
        public int Variant { get; }

        /// <summary>
        /// The catalogue name of the one model meant, or null where this piece
        /// came from a family. <see cref="IsNamed"/> is the question to ask.
        /// </summary>
        public string Model { get; }

        /// <summary>True where this piece names its model rather than a family.</summary>
        public bool IsNamed => !string.IsNullOrEmpty(Model);

        /// <summary>The cell it belongs to, so a tower standing here can clear it.</summary>
        public int Column { get; }

        /// <summary>The cell it belongs to.</summary>
        public int Row { get; }

        /// <summary>Metres east of the cell centre.</summary>
        public float OffsetX { get; }

        /// <summary>Metres above the cell's face. Zero for anything standing on the ground.</summary>
        public float OffsetY { get; }

        /// <summary>Metres north of the cell centre.</summary>
        public float OffsetZ { get; }

        /// <summary>Degrees about Y.</summary>
        public float Turn { get; }

        /// <summary>Uniform scale.</summary>
        public float Scale { get; }
    }

    /// <summary>
    /// Where the board's scenery goes. Pure: a map goes in and a list of
    /// placements comes out, with no engine type, no asset and no scene in
    /// sight.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing here may stand where a tower could.</b> Every ground cell is
    /// buildable — the longest range in the roster is 4.6 hexes, which on a
    /// board this size puts the whole field in reach of something — so scenery
    /// and towers want the same hexes. Two rules keep them apart. Small props
    /// are pushed out to the rim and never occupy a centre, so a tower dropped
    /// beside one simply stands beside it. Anything that fills a hex — a grove,
    /// a mound, a mountain — is placed knowing the floor will hide it the moment
    /// a tower is built there, which is the felling a player would expect.
    /// </para>
    /// <para>
    /// <b>The corridor keeps clear shoulders.</b> A cell touching the corridor
    /// never gets a hex-filling piece, only rim props and the occasional camp.
    /// That is a legibility rule rather than a taste one: the cells beside the
    /// path are where towers actually go, and a forest standing on the best
    /// ground reads as terrain you cannot build on.
    /// </para>
    /// <para>
    /// <b>Deterministic, and derived from the cell rather than from a
    /// sequence.</b> Every roll is a hash of the column, the row and a salt, so
    /// the same board dresses identically every time it is drawn, in any order,
    /// on any machine — and a redraw after a seek puts every rock back where it
    /// was. A shared <c>System.Random</c> would make the scenery depend on
    /// which cell was visited first, which is the sort of thing that looks fine
    /// until the floor is rebuilt mid-match.
    /// </para>
    /// </remarks>
    public static class BoardScenery
    {
        /// <summary>
        /// Everything to draw for a map, dressed as it ships.
        /// </summary>
        public static List<SceneryPlacement> For(HexMap map) => For(map, null, null);

        /// <summary>
        /// Everything to draw for a map, at a chosen weight and with a chosen
        /// set of hand-placed exceptions.
        /// </summary>
        /// <param name="map">The playfield. Null draws nothing.</param>
        /// <param name="settings">How heavy the dressing is. Null is the shipped weight.</param>
        /// <param name="dressing">
        /// The cells somebody has spoken for. Null is none of them.
        /// </param>
        /// <remarks>
        /// <b>The override is applied cell by cell, before the generator runs on
        /// that cell rather than after.</b> A cell this file speaks for never
        /// reaches <see cref="Dress"/> at all, so no setting can put anything
        /// back on it and no roll is spent on it. That is what makes an override
        /// mean the same thing at every weight.
        /// </remarks>
        public static List<SceneryPlacement> For(HexMap map, DressingSettings settings, BoardDressing dressing)
        {
            var placements = new List<SceneryPlacement>();

            if (map is null)
            {
                return placements;
            }

            DressingSettings weight = settings ?? DressingSettings.Default;
            BoardDressing authored = dressing ?? BoardDressing.Empty;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (authored.Speaks(column, row))
                    {
                        // Whole, and without asking what the cell is. A hand
                        // placed piece on the corridor is somebody's decision;
                        // this is not the place to overrule it.
                        placements.AddRange(authored.At(column, row));

                        continue;
                    }

                    if (map.CellAt(column, row) != MapCell.Ground)
                    {
                        continue;
                    }

                    Dress(map, weight, column, row, placements);
                }
            }

            if (authored.HasSky)
            {
                placements.AddRange(authored.Sky);
            }
            else
            {
                Clouds(map, weight, placements);
            }

            return placements;
        }

        /// <summary>One cell's worth.</summary>
        private static void Dress(
            HexMap map, DressingSettings weight, int column, int row, List<SceneryPlacement> placements)
        {
            // Nothing grows under water. The cell is ordinary ground as far as
            // the simulation is concerned, so this is the only place that knows.
            if (map.LevelAt(column, row) <= weight.WaterLevel)
            {
                return;
            }

            bool besideTheRoad = TouchesCorridor(map, column, row);

            if (!besideTheRoad && TryFiller(map, weight, column, row, out SceneryPlacement filler))
            {
                placements.Add(filler);

                return;
            }

            // The camp register, and only along the path, so an encampment reads
            // as being posted where the fighting is rather than scattered over
            // the whole field.
            if (besideTheRoad && Unit(column, row, 11) < weight.CampChance)
            {
                placements.Add(Standing(weight, SceneryGroup.Camp, column, row, 12));

                return;
            }

            // The lip of a drop, before the small props, because a mound is
            // the thing the cell is for where it has one and a barrel beside it
            // is noise.
            if (TryRidge(map, weight, column, row, out SceneryPlacement mound))
            {
                placements.Add(mound);

                return;
            }

            int props = Unit(column, row, 20) < weight.PropChance
                ? (Unit(column, row, 21) < weight.SecondPropChance ? 2 : 1)
                : 0;

            for (int index = 0; index < props; index++)
            {
                placements.Add(Standing(weight, SceneryGroup.RimProp, column, row, 30 + (index * 7)));
            }
        }

        /// <summary>
        /// A hex-filling piece, if this cell draws one. Mountains stand on the
        /// border, where they frame the board rather than block the view of it.
        /// </summary>
        private static bool TryFiller(
            HexMap map, DressingSettings weight, int column, int row, out SceneryPlacement placement)
        {
            placement = default;

            bool border = column == 0 || row == 0 || column == map.Width - 1 || row == map.Height - 1;
            float roll = Unit(column, row, 3);

            SceneryGroup group;

            if (border)
            {
                if (roll < weight.PeakChance)
                {
                    group = SceneryGroup.Peak;
                }
                else if (roll < weight.PeakChance + weight.BorderGroveChance)
                {
                    group = SceneryGroup.Grove;
                }
                else
                {
                    return false;
                }
            }
            else if (roll < weight.GroveChance)
            {
                group = SceneryGroup.Grove;
            }
            else
            {
                return false;
            }

            // A filler is centred: it is the hex's whole content, and offsetting
            // it would hang it over the neighbour's rim.
            placement = new SceneryPlacement(
                group,
                (int)(Hash(column, row, 4) % 64u),
                column,
                row,
                0f,
                0f,
                0f,
                Unit(column, row, 5) * 360f,
                0.94f + (Unit(column, row, 6) * 0.16f));

            return true;
        }

        /// <summary>
        /// A mound on the lip of a drop, if this cell stands over one and the
        /// roll goes that way.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Toward the lowest neighbour, and off centre.</b> The mound is
        /// pushed out along the edge the ground falls over, so it straddles the
        /// step rather than sitting on the plateau behind it. The centre of the
        /// hex is left alone for the same reason every other prop leaves it
        /// alone: a tower goes there.
        /// </para>
        /// <para>
        /// <b>A cell on the board's rim counts as standing over a drop.</b> The
        /// world ends there, and an edge with nothing growing over it is the
        /// most obviously sawn-off part of any board.
        /// </para>
        /// </remarks>
        private static bool TryRidge(
            HexMap map, DressingSettings weight, int column, int row, out SceneryPlacement placement)
        {
            placement = default;

            if (Unit(column, row, 40) >= weight.RidgeChance)
            {
                return false;
            }

            if (!Falls(map, column, row, out int direction))
            {
                return false;
            }

            // Where the middle of that edge is, in metres from the cell centre.
            // Direction zero is east and they run anticlockwise, which is the
            // order Hex.Neighbour counts in.
            double bearing = direction * (System.Math.PI / 3d);
            float reach = HexGeometry.AcrossFlats * 0.5f * (0.62f + (Unit(column, row, 41) * 0.22f));

            placement = new SceneryPlacement(
                SceneryGroup.Hill,
                (int)(Hash(column, row, 42) % 64u),
                column,
                row,
                (float)System.Math.Cos(bearing) * reach,
                0f,
                (float)System.Math.Sin(bearing) * reach,
                Unit(column, row, 43) * 360f,
                1.1f + (Unit(column, row, 44) * 0.7f));

            return true;
        }

        /// <summary>
        /// The direction the ground falls furthest from a cell, or false where
        /// it does not fall at all. Off the grid counts as the deepest fall
        /// there is.
        /// </summary>
        private static bool Falls(HexMap map, int column, int row, out int direction)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            int here = map.LevelAt(column, row);
            int deepest = 0;

            direction = -1;

            for (int step = 0; step < Hex.DirectionCount; step++)
            {
                Hex neighbour = hex.Neighbour(step);
                Hex.ToOddRowOffset(neighbour, out int otherColumn, out int otherRow);

                bool offGrid = otherColumn < 0 || otherColumn >= map.Width
                    || otherRow < 0 || otherRow >= map.Height;

                int drop = offGrid
                    ? here + 1
                    : here - map.LevelAt(otherColumn, otherRow);

                if (drop > deepest)
                {
                    deepest = drop;
                    direction = step;
                }
            }

            return direction >= 0;
        }

        /// <summary>One small thing, out near a rim, turned any way.</summary>
        private static SceneryPlacement Standing(
            DressingSettings weight, SceneryGroup group, int column, int row, int salt)
        {
            // A rim rather than a rim's midpoint: the angle is free within the
            // band, so a row of props does not line up across the board.
            float angle = Unit(column, row, salt) * 2f * (float)System.Math.PI;
            float radius = HexGeometry.Circumradius
                * (weight.RimNear + (Unit(column, row, salt + 1) * (weight.RimFar - weight.RimNear)));

            return new SceneryPlacement(
                group,
                (int)(Hash(column, row, salt + 2) % 64u),
                column,
                row,
                (float)System.Math.Cos(angle) * radius,
                0f,
                (float)System.Math.Sin(angle) * radius,
                Unit(column, row, salt + 3) * 360f,
                weight.PropScale * (0.85f + (Unit(column, row, salt + 4) * 0.35f)));
        }

        /// <summary>
        /// The clouds, spread over the board's extent. They belong to no cell,
        /// so they carry the cell of the board's corner and are never cleared —
        /// nothing can be built in the sky.
        /// </summary>
        private static void Clouds(HexMap map, DressingSettings weight, List<SceneryPlacement> placements)
        {
            for (int index = 0; index < weight.CloudCount; index++)
            {
                float across = Unit(index, 0, 91);
                float down = Unit(index, 0, 92);

                int column = (int)(across * (map.Width - 1));
                int row = (int)(down * (map.Height - 1));

                placements.Add(new SceneryPlacement(
                    SceneryGroup.Cloud,
                    (int)(Hash(index, 0, 93) % 64u),
                    column,
                    row,
                    (Unit(index, 0, 94) - 0.5f) * HexGeometry.AcrossFlats,
                    weight.CloudHeight + (Unit(index, 0, 95) * weight.CloudSpread),
                    (Unit(index, 0, 96) - 0.5f) * HexGeometry.AcrossFlats,
                    Unit(index, 0, 97) * 360f,
                    0.8f + (Unit(index, 0, 98) * 0.6f)));
            }
        }

        /// <summary>True if any of the six neighbours is corridor.</summary>
        private static bool TouchesCorridor(HexMap map, int column, int row) =>
            RoadTiling.CorridorEdges(map, column, row) != 0;

        /// <summary>
        /// A roll in <c>[0, 1)</c> for one cell and one purpose.
        /// </summary>
        private static float Unit(int column, int row, int salt) =>
            (Hash(column, row, salt) & 0xFFFFFFu) / (float)0x1000000;

        /// <summary>
        /// A hash of a cell and a salt. FNV mixed with a final avalanche,
        /// because the inputs are small consecutive integers and an unmixed FNV
        /// leaves neighbouring cells' low bits correlated — which shows up as
        /// scenery in stripes.
        /// </summary>
        private static uint Hash(int column, int row, int salt)
        {
            unchecked
            {
                uint hash = 2166136261u;

                hash = (hash ^ (uint)column) * 16777619u;
                hash = (hash ^ (uint)row) * 16777619u;
                hash = (hash ^ (uint)salt) * 16777619u;

                hash ^= hash >> 13;
                hash *= 0x5bd1e995u;
                hash ^= hash >> 15;

                return hash;
            }
        }
    }
}
