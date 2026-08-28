using System.IO;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The board editor's model of a map.
    ///
    /// <b>The round trip is the one that matters.</b> This tool writes over
    /// <c>content/map.txt</c>, the file the simulation, its tests, the command
    /// line and the replay's own hash all read. If loading the committed board
    /// and writing it back is not byte-for-byte identical, then opening the
    /// editor and pressing bake — changing nothing — moves the board, and every
    /// artifact computed from it goes stale for no reason anybody could see.
    /// </summary>
    public class BoardDraftTests
    {
        [Test]
        public void TheCommittedBoardWritesBackByteForByte()
        {
            string authored = File.ReadAllText(MapPath()).Replace("\r\n", "\n");

            BoardDraft draft = BoardDraft.Of(StreamingContent.ReadMap(), authored);

            Assert.That(
                draft.ToText(),
                Is.EqualTo(authored),
                "Loading the committed map and writing it back changed it. A bake that changed nothing "
                + "would still invalidate the defense, the record and the landmark table.");
        }

        [Test]
        public void TheHeaderIsCarriedRatherThanRegenerated()
        {
            string authored = File.ReadAllText(MapPath()).Replace("\r\n", "\n");
            string preamble = BoardDraft.PreambleOf(authored);

            Assert.That(preamble, Does.StartWith("//"), "The preamble did not come back as comments.");

            Assert.That(
                preamble,
                Does.Contain("ODD-NUMBERED ROWS"),
                "The map's own explanation of the offset rule was lost. That paragraph is the most "
                + "useful thing in the file and a bake must not write over it.");

            Assert.That(
                preamble.TrimEnd('\n'),
                Does.Not.Contain("\n\n\n"),
                "The preamble carries blank lines at its end, which would grow by one on every bake.");
        }

        [Test]
        public void ADraftTheSimulationRefusesIsRefusedHere()
        {
            BoardDraft draft = Draft();

            // Two exits is one of the things the loader will not have. Which
            // particular refusal comes back is the simulation's business; that
            // one comes back at all is this test's.
            draft.Paint(0, 0, MapCell.Exit);
            draft.Paint(0, 2, MapCell.Route);

            Assert.That(
                draft.TryParse(out HexMap _, out string refusal),
                Is.False,
                "A board with a corridor cell floating in the corner was accepted.");

            Assert.That(refusal, Is.Not.Null.And.Not.Empty, "The refusal came back empty.");
        }

        [Test]
        public void PaintingAnEndMovesItRatherThanMakingASecond()
        {
            BoardDraft draft = Draft();

            (int Column, int Row) was = Find(draft, MapCell.Spawn);

            draft.Paint(was.Column, was.Row + 2, MapCell.Spawn);

            Assert.That(
                draft.CellAt(was.Column, was.Row),
                Is.EqualTo(MapCell.Route),
                "The old entrance stayed an entrance, so the board now has two.");

            Assert.That(Count(draft, MapCell.Spawn), Is.EqualTo(1));
        }

        [Test]
        public void ATierCannotBeRaisedPastWhatTheMapAllows()
        {
            BoardDraft draft = Draft();

            draft.Raise(0, 0, 99);

            Assert.That(draft.LevelAt(0, 0), Is.EqualTo(HexMap.LevelCount - 1));

            draft.Raise(0, 0, -4);

            Assert.That(draft.LevelAt(0, 0), Is.Zero);
        }

        [Test]
        public void ResizingKeepsWhatStillFits()
        {
            BoardDraft draft = Draft();

            (int Column, int Row) spawn = Find(draft, MapCell.Spawn);

            BoardDraft bigger = draft.Resized(draft.Width + 4, draft.Height + 2);

            Assert.That(bigger.Width, Is.EqualTo(draft.Width + 4));
            Assert.That(bigger.CellAt(spawn.Column, spawn.Row), Is.EqualTo(MapCell.Spawn));
            Assert.That(
                bigger.CellAt(draft.Width + 3, draft.Height + 1),
                Is.EqualTo(MapCell.Ground),
                "New ground did not come in as ground.");
        }

        /// <summary>
        /// Every level letter the map allows survives a write and a read, so a
        /// board using the top tier does not come back flattened.
        /// </summary>
        [Test]
        public void EveryTierSurvivesTheWrite()
        {
            BoardDraft draft = Draft();

            for (int level = 0; level < HexMap.LevelCount; level++)
            {
                draft.Raise(level, 0, level);
            }

            BoardDraft again = BoardDraft.Of(
                HexMap.ParseUtf8("again", System.Text.Encoding.UTF8.GetBytes(draft.ToText())),
                draft.ToText());

            for (int level = 0; level < HexMap.LevelCount; level++)
            {
                Assert.That(again.LevelAt(level, 0), Is.EqualTo(level), "Tier " + level + " did not survive.");
            }
        }

        private static BoardDraft Draft() =>
            BoardDraft.Of(StreamingContent.ReadMap(), File.ReadAllText(MapPath()).Replace("\r\n", "\n"));

        private static (int Column, int Row) Find(BoardDraft draft, MapCell what)
        {
            for (int row = 0; row < draft.Height; row++)
            {
                for (int column = 0; column < draft.Width; column++)
                {
                    if (draft.CellAt(column, row) == what)
                    {
                        return (column, row);
                    }
                }
            }

            throw new AssertionException("The committed board has no " + what + " on it.");
        }

        private static int Count(BoardDraft draft, MapCell what)
        {
            int found = 0;

            for (int row = 0; row < draft.Height; row++)
            {
                for (int column = 0; column < draft.Width; column++)
                {
                    if (draft.CellAt(column, row) == what)
                    {
                        found++;
                    }
                }
            }

            return found;
        }

        private static string MapPath() =>
            Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")),
                "content",
                "map.txt");
    }
}
