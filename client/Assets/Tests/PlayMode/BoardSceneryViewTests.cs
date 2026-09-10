using System.Collections.Generic;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The floor standing the board's scenery up, and taking it down again where
    /// a tower needs the hex.
    ///
    /// <b>What is being tested is the giving way, not the look.</b> Where a
    /// grove goes is decided by <see cref="BoardScenery"/> and asserted in edit
    /// mode, with no scene involved. What can only be checked here is that the
    /// floor actually made the objects, kept one host per cell, and can hide and
    /// restore them — because a tower and a tree occupying the same hex is the
    /// one way this feature can reach a player as a bug rather than as a taste.
    /// </summary>
    public class BoardSceneryViewTests : ViewTest
    {
        [Test]
        public void TheBoardIsDressedWhereTheChooserSaysAndNowhereElse()
        {
            HexFloor floor = Dressed(out HexMap map);

            var wanted = new HashSet<(int, int)>();

            foreach (SceneryPlacement placement in BoardScenery.For(map))
            {
                if (placement.Group != SceneryGroup.Cloud)
                {
                    wanted.Add((placement.Column, placement.Row));
                }
            }

            Assert.That(wanted, Is.Not.Empty, "The committed board produced no scenery to test with.");

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    Assert.That(
                        floor.HasSceneryAt(column, row),
                        Is.EqualTo(wanted.Contains((column, row))),
                        "The floor and the chooser disagree about the cell at " + column + "," + row + ".");
                }
            }
        }

        [Test]
        public void ATowerTakesTheHexAndTheSceneryStandsBackUpAfterwards()
        {
            HexFloor floor = Dressed(out HexMap map);

            (int Column, int Row) taken = FirstDressed(floor, map);

            floor.ClearSceneryUnder(new[] { taken });

            Assert.That(
                floor.SceneryAt(taken.Column, taken.Row).activeSelf,
                Is.False,
                "A tower is standing on the scenery at " + taken.Column + "," + taken.Row + ".");

            // The build phase places and unplaces freely, so a felled grove has
            // to grow back or the board's dressing would depend on the order
            // somebody tried placements in.
            floor.ClearSceneryUnder(null);

            Assert.That(
                floor.SceneryAt(taken.Column, taken.Row).activeSelf,
                Is.True,
                "The scenery did not come back when the tower left.");
        }

        [Test]
        public void ClearingOneHexLeavesEveryOtherAlone()
        {
            HexFloor floor = Dressed(out HexMap map);

            (int Column, int Row) taken = FirstDressed(floor, map);

            floor.ClearSceneryUnder(new[] { taken });

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    GameObject standing = floor.SceneryAt(column, row);

                    if (standing == null || (column == taken.Column && row == taken.Row))
                    {
                        continue;
                    }

                    Assert.That(
                        standing.activeSelf,
                        Is.True,
                        "Clearing one hex also cleared " + column + "," + row + ".");
                }
            }
        }

        /// <summary>
        /// A floor with no models wired draws no scenery, and says so without
        /// throwing when asked. The state a checkout without the art is in.
        /// </summary>
        [Test]
        public void AnUndressedBoardIsAskedAboutSafely()
        {
            HexMap map = StreamingContent.ReadMap();
            HexFloor floor = HexFloor.Build(Spawn("Bare").transform, map, Blockout());

            Assert.That(floor.HasSceneryAt(0, 0), Is.False);
            Assert.That(floor.SceneryAt(0, 0), Is.Null);

            floor.ShowScenery(0, 0, false);
            floor.ClearSceneryUnder(new[] { (0, 0) });
        }

        private static (int Column, int Row) FirstDressed(HexFloor floor, HexMap map)
        {
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (floor.HasSceneryAt(column, row))
                    {
                        return (column, row);
                    }
                }
            }

            throw new AssertionException("The committed board produced no scenery to test with.");
        }

        private HexFloor Dressed(out HexMap map)
        {
            map = StreamingContent.ReadMap();

            return HexFloor.Build(Spawn("Dressed").transform, map, Blockout(), StandIns());
        }

        private static TileSet Blockout() =>
            TileSet.Blockout(
                HexTileMesh.Create(),
                ViewMaterials.Create("Road", Color.grey),
                ViewMaterials.Create("Grass", Color.green));

        /// <summary>
        /// A scenery set made of the generated hexagon, six times over.
        /// </summary>
        /// <remarks>
        /// The imported models are not used here on purpose. What this fixture
        /// is asserting is the floor's bookkeeping — one host per cell, hidden
        /// and shown on request — and none of that depends on which mesh is
        /// hanging off it. Reaching for the real art would make a test about
        /// hiding objects fail whenever somebody changed a tree.
        /// </remarks>
        private static SceneryModels StandIns()
        {
            Mesh[] one = { HexTileMesh.Create() };

            return SceneryModels.Of(
                one, one, one, one, one, one, ViewMaterials.Create("Scenery", Color.white));
        }
    }
}
