using NUnit.Framework;
using Sim;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// Drawing the board and reading it back: the loop a person actually uses.
    ///
    /// <b>Nothing moved must bake to nothing.</b> The preview's transforms are
    /// floats that have been through a rotation, a scale and a parent, and a
    /// bake that wrote a line for every cell whose tree had come back a micron
    /// out would look exactly like working — until the first diff showed two
    /// hundred exceptions nobody made. So the empty case is the one worth an
    /// assertion, and the one-thing-moved case proves the diff is not simply
    /// blind.
    /// </summary>
    public class BoardBakeTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }
        }

        [Test]
        public void ABoardNobodyTouchedBakesToNothing()
        {
            HexFloor floor = Drawn(out HexMap map);

            string text = BoardDressingTools.TextFor(floor, map, DressingSettings.Default);
            BoardDressing read = BoardDressing.Parse("baked", text);

            Assert.That(
                read.CellCount,
                Is.Zero,
                "A board nobody moved anything on baked " + read.CellCount
                + " exceptions. The diff is seeing a difference that is not there.");

            Assert.That(read.HasSky, Is.False, "The sky was baked without anybody moving a cloud.");
        }

        [Test]
        public void MovingOneThingBakesThatOneThing()
        {
            HexFloor floor = Drawn(out HexMap map);

            (int Column, int Row) cell = FirstDressed(floor, map);
            Transform piece = floor.SceneryAt(cell.Column, cell.Row).transform.GetChild(0);

            piece.localPosition += new Vector3(0.31f, 0f, -0.22f);

            BoardDressing read = BoardDressing.Parse(
                "baked", BoardDressingTools.TextFor(floor, map, DressingSettings.Default));

            Assert.That(read.CellCount, Is.EqualTo(1), "Moving one piece baked more than one cell.");
            Assert.That(read.Speaks(cell.Column, cell.Row), Is.True, "The wrong cell was baked.");

            Assert.That(
                read.At(cell.Column, cell.Row)[0].OffsetX,
                Is.EqualTo(piece.localPosition.x).Within(0.001f),
                "The baked offset is not where the piece was left.");
        }

        [Test]
        public void DeletingEverythingOnAHexBakesAClear()
        {
            HexFloor floor = Drawn(out HexMap map);

            (int Column, int Row) cell = FirstDressed(floor, map);
            GameObject host = floor.SceneryAt(cell.Column, cell.Row);

            for (int index = host.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(host.transform.GetChild(index).gameObject);
            }

            BoardDressing read = BoardDressing.Parse(
                "baked", BoardDressingTools.TextFor(floor, map, DressingSettings.Default));

            Assert.That(read.Speaks(cell.Column, cell.Row), Is.True, "The emptied cell was not baked.");
            Assert.That(read.At(cell.Column, cell.Row), Is.Empty, "The emptied cell baked with something on it.");
        }

        /// <summary>
        /// What was baked, drawn again, bakes to the same thing — so a second
        /// press of the button is not a second diff.
        /// </summary>
        [Test]
        public void BakingWhatWasBakedChangesNothing()
        {
            HexFloor floor = Drawn(out HexMap map);

            (int Column, int Row) cell = FirstDressed(floor, map);
            floor.SceneryAt(cell.Column, cell.Row).transform.GetChild(0).localPosition +=
                new Vector3(0.15f, 0f, 0.15f);

            string once = BoardDressingTools.TextFor(floor, map, DressingSettings.Default);

            Object.DestroyImmediate(_host);
            _host = new GameObject("Bake Again");

            HexFloor again = HexFloor.Build(
                _host.transform,
                map,
                MatchSceneBuilder.Tiles(),
                MatchSceneBuilder.Scenery(),
                DressingSettings.Default,
                BoardDressing.Parse("baked", once));

            Assert.That(
                BoardDressingTools.TextFor(again, map, DressingSettings.Default),
                Is.EqualTo(once),
                "Drawing a baked board and baking it again produced different text.");
        }

        private HexFloor Drawn(out HexMap map)
        {
            map = StreamingContent.ReadMap();
            _host = new GameObject("Bake Test");

            return HexFloor.Build(
                _host.transform,
                map,
                MatchSceneBuilder.Tiles(),
                MatchSceneBuilder.Scenery(),
                DressingSettings.Default,
                BoardDressing.Empty);
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

            throw new AssertionException("The committed board drew no scenery to move.");
        }
    }
}
