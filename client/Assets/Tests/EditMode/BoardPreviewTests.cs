using System.IO;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The preview surviving being redrawn.
    ///
    /// <b>Both tools draw into one preview, and the board editor tears it down
    /// after every stroke.</b> That is the arrangement, and it is fine right up
    /// until the teardown throws away what somebody moved — which it did:
    /// dressing the board and then painting a single hex put every tree back
    /// where the generator wanted it, silently, with no undo. These tests are
    /// the reason that cannot come back.
    /// </summary>
    public class BoardPreviewTests
    {
        [TearDown]
        public void TearDown() => BoardDressingTools.Clear();

        /// <summary>
        /// The bug, at its smallest: move a thing, draw the board again, and the
        /// thing is still where it was left.
        /// </summary>
        [Test]
        public void RedrawingTheBoardKeepsWhatWasMoved()
        {
            HexMap map = StreamingContent.ReadMap();

            HexFloor floor = Drawn(map);
            (int Column, int Row) cell = FirstDressed(floor, map);

            Vector3 left = Move(floor, cell, new Vector3(0.37f, 0f, -0.24f));

            HexFloor again = Drawn(map);

            Assert.That(
                Offset(again, cell),
                Is.EqualTo(left).Using(Near),
                "Drawing the board again put the piece back where the generator wanted it. "
                + "A redraw must carry unbaked work forward, because the board editor does "
                + "one after every stroke.");
        }

        /// <summary>
        /// The bug as it is actually met: the map changes under the dressing.
        /// </summary>
        /// <remarks>
        /// The stroke is what makes this worth its own test. A redraw that
        /// measured the standing board against the <i>new</i> map would call
        /// every cell the stroke touched an override, and quietly pin the
        /// generator's own scenery into the file as though a person had placed
        /// it — so the piece surviving is only half of what is asserted here.
        /// The other half is that nothing else moved into the file with it.
        /// </remarks>
        [Test]
        public void PaintingTheMapKeepsWhatWasMoved()
        {
            HexMap map = StreamingContent.ReadMap();

            HexFloor floor = Drawn(map);
            (int Column, int Row) cell = FirstDressed(floor, map);

            Vector3 left = Move(floor, cell, new Vector3(-0.29f, 0f, 0.41f));

            HexMap painted = Painted(map, cell);
            HexFloor again = Drawn(painted);

            Assert.That(
                Offset(again, cell),
                Is.EqualTo(left).Using(Near),
                "Painting a hex threw away the piece that had been moved on a different hex.");

            BoardDressing carried = BoardDressing.Parse(
                "carried",
                BoardDressingTools.TextFor(again, painted, BoardDressingTools.Settings()));

            Assert.That(
                carried.CellCount,
                Is.EqualTo(1),
                "One piece was moved and " + carried.CellCount + " cells came back authored. "
                + "The redraw is measuring the old board against the new map and calling the "
                + "generator's own work somebody's.");
        }

        /// <summary>
        /// The way back. Carrying work forward would be a trap without one.
        /// </summary>
        [Test]
        public void ClearingFirstGoesBackToTheFile()
        {
            HexMap map = StreamingContent.ReadMap();

            HexFloor floor = Drawn(map);
            (int Column, int Row) cell = FirstDressed(floor, map);

            Vector3 was = Offset(floor, cell);

            Move(floor, cell, new Vector3(0.5f, 0f, 0.5f));

            BoardDressingTools.Clear();

            Assert.That(
                Offset(Drawn(map), cell),
                Is.EqualTo(was).Using(Near),
                "Clearing and drawing again did not go back to what the file says, so there "
                + "is no way to abandon a change.");
        }

        // ---------------------------------------------------------------

        private static HexFloor Drawn(HexMap map) =>
            BoardDressingTools.DressWith(map).GetComponentInChildren<HexFloor>();

        private static Transform PieceAt(HexFloor floor, (int Column, int Row) cell) =>
            floor.SceneryAt(cell.Column, cell.Row).transform.GetChild(0);

        private static Vector3 Offset(HexFloor floor, (int Column, int Row) cell) =>
            PieceAt(floor, cell).localPosition;

        private static Vector3 Move(HexFloor floor, (int Column, int Row) cell, Vector3 by)
        {
            Transform piece = PieceAt(floor, cell);

            piece.localPosition += by;

            return piece.localPosition;
        }

        /// <summary>
        /// The map with one ground cell raised a tier — a stroke the editor
        /// allows that leaves the corridor alone, so what it disturbs is the
        /// generator rather than the board's legality.
        /// </summary>
        private static HexMap Painted(HexMap map, (int Column, int Row) avoid)
        {
            string authored = File.ReadAllText(MapPath()).Replace("\r\n", "\n");
            BoardDraft draft = BoardDraft.Of(map, authored);

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (map.CellAt(column, row) != MapCell.Ground
                        || (column == avoid.Column && row == avoid.Row))
                    {
                        continue;
                    }

                    draft.Raise(column, row, draft.LevelAt(column, row) + 1);

                    Assert.That(
                        draft.TryParse(out HexMap painted, out string refusal),
                        Is.True,
                        "Raising one ground cell was refused: " + refusal);

                    return painted;
                }
            }

            throw new AssertionException("The committed board has no ground on it to paint.");
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

        private static string MapPath() =>
            Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")),
                "content",
                "map.txt");

        /// <summary>
        /// Millimetre tolerance, because carrying work forward goes through the
        /// file's own integer format and lands on the nearest millimetre.
        /// </summary>
        private static readonly System.Collections.IComparer Near = new Millimetres();

        private sealed class Millimetres : System.Collections.IComparer
        {
            public int Compare(object left, object right) =>
                Vector3.Distance((Vector3)left, (Vector3)right) < 0.002f ? 0 : 1;
        }
    }
}
