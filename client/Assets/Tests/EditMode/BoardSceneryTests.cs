using System.Collections.Generic;
using NUnit.Framework;
using Sim;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// Where the board's scenery is allowed to stand.
    ///
    /// Every rule here exists because scenery and towers compete for the same
    /// hexes: the longest range in the roster is 4.6 hexes, which on a board
    /// this size leaves no ground cell out of reach of something, so there is no
    /// such thing as a cell the player will never want. What keeps the two apart
    /// is that small props are pushed to the rim and hex-filling pieces are
    /// placed only where the floor will clear them.
    ///
    /// Pure, so this is edit mode and there is no scene: <see cref="BoardScenery"/>
    /// takes a map and returns a list.
    /// </summary>
    public class BoardSceneryTests
    {
        /// <summary>
        /// The groups that occupy a whole hex, and so must never appear beside
        /// the path.
        /// </summary>
        private static readonly SceneryGroup[] Filling =
        {
            SceneryGroup.Grove,
            SceneryGroup.Peak,
        };

        [Test]
        public void NothingStandsOnTheCorridor()
        {
            HexMap map = Map();

            foreach (SceneryPlacement placement in BoardScenery.For(map))
            {
                if (placement.Group == SceneryGroup.Cloud)
                {
                    continue;
                }

                Assert.That(
                    map.CellAt(placement.Column, placement.Row),
                    Is.EqualTo(MapCell.Ground),
                    "A " + placement.Group + " is standing on the corridor at "
                    + placement.Column + "," + placement.Row
                    + ", where it would sit in the road the creeps walk down.");
            }
        }

        /// <summary>
        /// A grove or a mountain never stands on a cell touching the path.
        /// </summary>
        /// <remarks>
        /// Those are the cells towers actually go on, and terrain filling one
        /// reads as ground you cannot build on — which is a lie, because you
        /// can. Rim props are exempt: they stand at the edge of their own tile
        /// and leave the middle of it free.
        /// </remarks>
        [Test]
        public void TheCorridorKeepsClearShoulders()
        {
            HexMap map = Map();

            foreach (SceneryPlacement placement in BoardScenery.For(map))
            {
                if (System.Array.IndexOf(Filling, placement.Group) < 0)
                {
                    continue;
                }

                Assert.That(
                    RoadTiling.CorridorEdges(map, placement.Column, placement.Row),
                    Is.Zero,
                    "A " + placement.Group + " fills the cell at "
                    + placement.Column + "," + placement.Row
                    + ", which touches the corridor. That is prime tower ground dressed as terrain.");
            }
        }

        /// <summary>
        /// Nothing that stands on the ground stands at a hex's centre, except
        /// the pieces the floor is able to clear.
        /// </summary>
        /// <remarks>
        /// A tower is drawn at the centre of its cell. A prop there would be
        /// inside it, which no amount of hiding at build time fixes for the
        /// build phase's own preview — so the rule is geometric rather than
        /// procedural.
        /// </remarks>
        [Test]
        public void SmallPropsLeaveTheCentreOfTheirHexFree()
        {
            HexMap map = Map();

            foreach (SceneryPlacement placement in BoardScenery.For(map))
            {
                if (placement.Group != SceneryGroup.RimProp && placement.Group != SceneryGroup.Camp)
                {
                    continue;
                }

                float distance = UnityEngine.Mathf.Sqrt(
                    (placement.OffsetX * placement.OffsetX) + (placement.OffsetZ * placement.OffsetZ));

                Assert.That(
                    distance,
                    Is.GreaterThan(HexGeometry.Circumradius * 0.5f),
                    "A " + placement.Group + " at " + placement.Column + "," + placement.Row
                    + " is too near the middle of its hex, where a tower stands.");

                Assert.That(
                    distance,
                    Is.LessThan(HexGeometry.AcrossFlats * 0.5f),
                    "A " + placement.Group + " at " + placement.Column + "," + placement.Row
                    + " is past its own rim and hanging over the next cell.");
            }
        }

        /// <summary>
        /// The same map dresses identically every time, which is what makes a
        /// redraw after a seek put every rock back where it was.
        /// </summary>
        [Test]
        public void TheSameBoardIsDressedTheSameWayTwice()
        {
            HexMap map = Map();

            List<SceneryPlacement> first = BoardScenery.For(map);
            List<SceneryPlacement> second = BoardScenery.For(map);

            Assert.That(second.Count, Is.EqualTo(first.Count), "A second dressing produced a different count.");

            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(second[index].Group, Is.EqualTo(first[index].Group), "Group " + index);
                Assert.That(second[index].Variant, Is.EqualTo(first[index].Variant), "Variant " + index);
                Assert.That(second[index].Column, Is.EqualTo(first[index].Column), "Column " + index);
                Assert.That(second[index].Row, Is.EqualTo(first[index].Row), "Row " + index);
                Assert.That(second[index].OffsetX, Is.EqualTo(first[index].OffsetX), "OffsetX " + index);
                Assert.That(second[index].OffsetZ, Is.EqualTo(first[index].OffsetZ), "OffsetZ " + index);
            }
        }

        /// <summary>
        /// The committed board gets dressed at all, and not so heavily that the
        /// field disappears under it.
        /// </summary>
        /// <remarks>
        /// A range rather than a number, because the exact count is a matter of
        /// taste that will be tuned — but zero means the rules stopped matching
        /// anything, and one piece per ground cell means the board has become
        /// scenery with a path through it.
        /// </remarks>
        [Test]
        public void TheCommittedBoardIsDressedButNotBuried()
        {
            HexMap map = Map();
            int ground = 0;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (map.CellAt(column, row) == MapCell.Ground)
                    {
                        ground++;
                    }
                }
            }

            int placed = BoardScenery.For(map).Count;

            Assert.That(placed, Is.GreaterThan(ground / 8), "The board is barely dressed at all.");
            Assert.That(placed, Is.LessThan(ground), "There is more scenery than there is field to put it on.");
        }

        private static HexMap Map() => View.StreamingContent.ReadMap();
    }
}
