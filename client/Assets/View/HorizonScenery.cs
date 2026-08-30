using System.Collections.Generic;

namespace View
{
    /// <summary>
    /// One thing standing on the plain, and where. Metres in world space, not
    /// offsets from a cell — because nothing out here belongs to a cell.
    /// </summary>
    /// <remarks>
    /// A separate type from <see cref="SceneryPlacement"/> on purpose. That one
    /// carries the column and row of the hex it sits on so that a tower placed
    /// there can clear it, and every piece out here sits on no hex at all. A
    /// placement with a made-up cell would be a lie the first time somebody
    /// looped over them looking for what a tower displaces.
    /// </remarks>
    public readonly struct DistantPiece
    {
        public DistantPiece(SceneryGroup group, int variant, float x, float z, float turn, float scale)
        {
            Group = group;
            Variant = variant;
            X = x;
            Z = z;
            Turn = turn;
            Scale = scale;
        }

        /// <summary>Which family.</summary>
        public SceneryGroup Group { get; }

        /// <summary>Which model within it, by index. Wrapped by the set.</summary>
        public int Variant { get; }

        /// <summary>Metres east, in world space.</summary>
        public float X { get; }

        /// <summary>Metres north, in world space.</summary>
        public float Z { get; }

        /// <summary>Degrees about Y.</summary>
        public float Turn { get; }

        /// <summary>Uniform scale.</summary>
        public float Scale { get; }
    }

    /// <summary>
    /// How thickly the plain around the board is planted. Plain numbers, so the
    /// chooser stays a function of its arguments and a test can plant a band it
    /// picked rather than the one that ships.
    /// </summary>
    public readonly struct Planting
    {
        public Planting(
            float treeGap,
            float treeDepth,
            float treeStep,
            float treeChance,
            float hillGap,
            float hillStep,
            float hillChance,
            float peakShare,
            float hillReach)
        {
            TreeGap = treeGap;
            TreeDepth = treeDepth;
            TreeStep = treeStep;
            TreeChance = treeChance;
            HillGap = hillGap;
            HillStep = hillStep;
            HillChance = hillChance;
            PeakShare = peakShare;
            HillReach = hillReach;
        }

        /// <summary>The committed planting, from <see cref="SceneFraming"/>.</summary>
        public static Planting Default =>
            new Planting(
                SceneFraming.TreelineGap,
                SceneFraming.TreelineDepth,
                SceneFraming.TreelineStep,
                SceneFraming.TreelineChance,
                SceneFraming.DistantHillGap,
                SceneFraming.DistantHillStep,
                SceneFraming.DistantHillChance,
                SceneFraming.DistantPeakShare,
                SceneFraming.DistantHillReach);

        /// <summary>How far clear of the board's edge the wood begins, in metres.</summary>
        public float TreeGap { get; }

        /// <summary>How deep the band of wood is, in metres.</summary>
        public float TreeDepth { get; }

        /// <summary>How far apart the wood's candidate positions are, in metres.</summary>
        public float TreeStep { get; }

        /// <summary>The chance one of them is taken, in <c>[0, 1]</c>.</summary>
        public float TreeChance { get; }

        /// <summary>How far clear of the board the hills begin, in metres.</summary>
        public float HillGap { get; }

        /// <summary>How far apart the hills' candidate positions are, in metres.</summary>
        public float HillStep { get; }

        /// <summary>The chance one of them is taken.</summary>
        public float HillChance { get; }

        /// <summary>What share of the hills are mountains rather than mounds.</summary>
        public float PeakShare { get; }

        /// <summary>How far out the hills go, as a share of the plain's radius.</summary>
        public float HillReach { get; }
    }

    /// <summary>
    /// What stands on the plain around the board: a treeline just off its edge,
    /// and hills in the distance behind that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Pure, for the reason <see cref="BoardScenery"/> is.</b> A footprint
    /// and a radius go in and a list comes out, with no engine type, no asset
    /// and no scene in sight — so where a treeline goes can be asserted without
    /// an editor, and drawing it is somebody else's job.
    /// </para>
    /// <para>
    /// <b>The treeline is what stops the board being a plate.</b> The rim is a
    /// clean hexagonal cut all the way round and no amount of ground behind it
    /// changes that; something with an irregular silhouette standing just off
    /// the edge does. So the wood begins a fixed distance clear of the board
    /// rather than at some fraction of it — the number that matters is how far
    /// the trees are from the tiles, not how big the board happens to be.
    /// </para>
    /// <para>
    /// <b>It begins clear of the board and not against it.</b> A tree hard
    /// against the rim hides the outer ring of cells from a low camera, and
    /// those are cells somebody builds on. The gap is set so that nothing is
    /// occluded at the shipped pitch; drop the camera far enough and the wood
    /// does cross in front of the near edge, which is what a treeline does.
    /// </para>
    /// <para>
    /// <b>Candidates are a lattice, not a ring.</b> The board is a rectangle,
    /// and a ring of trees around a rectangle is close on the long sides and far
    /// off the short ones. Walking a grid and measuring each point's distance to
    /// the board's own footprint gives an even band whatever shape the board is.
    /// </para>
    /// <para>
    /// <b>Nothing out here is reachable.</b> These stand on the plain, which
    /// carries no collider, is never a tile and is never picked. The simulation
    /// has no idea any of it exists — see <see cref="Horizon"/>.
    /// </para>
    /// </remarks>
    public static class HorizonScenery
    {
        /// <summary>
        /// Everything standing on the plain around a board of this footprint.
        /// </summary>
        /// <param name="centreX">The middle of the board, east, in metres.</param>
        /// <param name="centreZ">The middle of the board, north, in metres.</param>
        /// <param name="halfWidth">Half the board's extent east–west.</param>
        /// <param name="halfDepth">Half the board's extent north–south.</param>
        /// <param name="radius">How far the plain reaches.</param>
        /// <param name="planting">How thickly to plant it.</param>
        public static IReadOnlyList<DistantPiece> For(
            float centreX,
            float centreZ,
            float halfWidth,
            float halfDepth,
            float radius,
            Planting planting)
        {
            var standing = new List<DistantPiece>();

            Wood(standing, centreX, centreZ, halfWidth, halfDepth, planting);
            Hills(standing, centreX, centreZ, halfWidth, halfDepth, radius, planting);

            return standing;
        }

        /// <summary>The band of trees just off the board's edge.</summary>
        private static void Wood(
            List<DistantPiece> standing,
            float centreX,
            float centreZ,
            float halfWidth,
            float halfDepth,
            Planting planting)
        {
            float step = planting.TreeStep;

            if (step <= 0f)
            {
                return;
            }

            float outer = planting.TreeGap + planting.TreeDepth;
            int across = (int)((halfWidth + outer) / step) + 1;
            int down = (int)((halfDepth + outer) / step) + 1;

            for (int j = -down; j <= down; j++)
            {
                for (int i = -across; i <= across; i++)
                {
                    // Jittered off the lattice, or the wood is a grid of trees
                    // and reads as an orchard.
                    float x = centreX + (i * step) + ((Unit(i, j, 1) - 0.5f) * step * 0.9f);
                    float z = centreZ + (j * step) + ((Unit(i, j, 2) - 0.5f) * step * 0.9f);

                    float clear = Outside(x - centreX, z - centreZ, halfWidth, halfDepth);

                    if (clear < planting.TreeGap || clear > outer)
                    {
                        continue;
                    }

                    // Thinning outward, so the band has an edge that frays
                    // rather than one that stops.
                    float through = (clear - planting.TreeGap) / planting.TreeDepth;

                    if (Unit(i, j, 3) >= planting.TreeChance * (1f - (through * 0.65f)))
                    {
                        continue;
                    }

                    standing.Add(
                        new DistantPiece(
                            SceneryGroup.Grove,
                            (int)(Hash(i, j, 4) % 64u),
                            x,
                            z,
                            Unit(i, j, 5) * 360f,
                            SceneFraming.TreelineScale * (0.8f + (Unit(i, j, 6) * 0.5f))));
                }
            }
        }

        /// <summary>The hills behind the wood, out to where the haze closes.</summary>
        private static void Hills(
            List<DistantPiece> standing,
            float centreX,
            float centreZ,
            float halfWidth,
            float halfDepth,
            float radius,
            Planting planting)
        {
            float step = planting.HillStep;
            float reach = radius * planting.HillReach;

            if (step <= 0f || reach <= planting.HillGap)
            {
                return;
            }

            int across = (int)((halfWidth + reach) / step) + 1;
            int down = (int)((halfDepth + reach) / step) + 1;

            for (int j = -down; j <= down; j++)
            {
                for (int i = -across; i <= across; i++)
                {
                    float x = centreX + (i * step) + ((Unit(i, j, 11) - 0.5f) * step * 0.85f);
                    float z = centreZ + (j * step) + ((Unit(i, j, 12) - 0.5f) * step * 0.85f);

                    float clear = Outside(x - centreX, z - centreZ, halfWidth, halfDepth);

                    if (clear < planting.HillGap || clear > reach)
                    {
                        continue;
                    }

                    if (Unit(i, j, 13) >= planting.HillChance)
                    {
                        continue;
                    }

                    // Bigger the further off, so a hill on the skyline is still
                    // a shape rather than a speck. The pack's models are cut for
                    // a hex and the nearest of these is twenty of them away.
                    float through = (clear - planting.HillGap) / (reach - planting.HillGap);
                    float scale =
                        SceneFraming.DistantHillNearScale
                        + ((SceneFraming.DistantHillFarScale - SceneFraming.DistantHillNearScale) * through);

                    standing.Add(
                        new DistantPiece(
                            Unit(i, j, 14) < planting.PeakShare ? SceneryGroup.Peak : SceneryGroup.Hill,
                            (int)(Hash(i, j, 15) % 64u),
                            x,
                            z,
                            Unit(i, j, 16) * 360f,
                            scale * (0.75f + (Unit(i, j, 17) * 0.6f))));
                }
            }
        }

        /// <summary>
        /// How far a point is from the board's footprint, in metres. Zero
        /// anywhere over the board itself.
        /// </summary>
        /// <remarks>
        /// The distance to a rectangle rather than to its middle, which is what
        /// makes the treeline the same depth off a short side as off a long one.
        /// </remarks>
        private static float Outside(float dx, float dz, float halfWidth, float halfDepth)
        {
            float outX = System.Math.Abs(dx) - halfWidth;
            float outZ = System.Math.Abs(dz) - halfDepth;

            if (outX < 0f)
            {
                outX = 0f;
            }

            if (outZ < 0f)
            {
                outZ = 0f;
            }

            return (float)System.Math.Sqrt((outX * outX) + (outZ * outZ));
        }

        /// <summary>A roll in <c>[0, 1)</c> for one lattice point and one purpose.</summary>
        private static float Unit(int i, int j, int salt) =>
            (Hash(i, j, salt) & 0xFFFFFFu) / (float)0x1000000;

        /// <summary>
        /// The same FNV-and-avalanche as <see cref="BoardScenery"/>, and for the
        /// same reason: the inputs are small consecutive integers, and an
        /// unmixed FNV leaves neighbours' low bits correlated — which shows up
        /// as trees in stripes.
        /// </summary>
        private static uint Hash(int i, int j, int salt)
        {
            unchecked
            {
                uint hash = 2166136261u;

                hash = (hash ^ (uint)i) * 16777619u;
                hash = (hash ^ (uint)j) * 16777619u;
                hash = (hash ^ (uint)salt) * 16777619u;

                hash ^= hash >> 13;
                hash *= 0x5bd1e995u;
                hash ^= hash >> 15;

                return hash;
            }
        }
    }
}
