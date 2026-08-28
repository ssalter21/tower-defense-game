using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// The hand-placed scenery file: what it means, and that a bake of it reads
    /// back as itself.
    ///
    /// <b>The round trip is the load-bearing test.</b> Everything else here
    /// checks a rule somebody could look up; that one checks the thing the
    /// feature is actually for. The editor draws a board, a person moves things
    /// on it, and the bake writes text — and if that text does not parse back to
    /// the same board, the failure the person sees is "my tree moved slightly
    /// when I saved", which is the worst bug an authoring tool can have because
    /// it is invisible until it has happened a hundred times.
    /// </summary>
    public class BoardDressingTests
    {
        [Test]
        public void ACellNamedInTheFileIsTakenOverByIt()
        {
            HexMap map = StreamingContent.ReadMap();
            (int Column, int Row) dressed = FirstGenerated(map);

            BoardDressing authored = BoardDressing.Parse(
                "test", "clear " + dressed.Column + " " + dressed.Row);

            foreach (SceneryPlacement placement in BoardScenery.For(map, null, authored))
            {
                Assert.That(
                    placement.Group == SceneryGroup.Cloud
                        || placement.Column != dressed.Column
                        || placement.Row != dressed.Row,
                    Is.True,
                    "The generator put something back on a cell the file cleared.");
            }
        }

        /// <summary>
        /// An override means the same thing however heavy the dressing is.
        /// </summary>
        /// <remarks>
        /// This is the whole division of labour between the settings and the
        /// file, in one assertion: turn every chance to its extreme and the
        /// cells somebody spoke for come out identical. Without it the two would
        /// be one feature that half works.
        /// </remarks>
        [Test]
        public void AnOverrideSurvivesEverySetting()
        {
            HexMap map = StreamingContent.ReadMap();
            (int Column, int Row) cell = FirstGenerated(map);

            BoardDressing authored = BoardDressing.Parse(
                "test",
                "place " + cell.Column + " " + cell.Row + " grove 1 -300 100 40 120");

            var bare = new DressingSettings
            {
                GroveChance = 0f,
                PeakChance = 0f,
                BorderGroveChance = 0f,
                PropChance = 0f,
                CampChance = 0f,
                CloudCount = 0,
            };

            var heavy = new DressingSettings
            {
                GroveChance = 1f,
                PeakChance = 1f,
                BorderGroveChance = 1f,
                PropChance = 1f,
                SecondPropChance = 1f,
                CampChance = 1f,
                CloudCount = 12,
            };

            Assert.That(
                Describe(On(BoardScenery.For(map, bare, authored), cell)),
                Is.EqualTo(Describe(On(BoardScenery.For(map, heavy, authored), cell))),
                "The cell somebody placed by hand changed when a slider moved.");
        }

        [Test]
        public void OneCloudLineReplacesTheWholeSky()
        {
            HexMap map = StreamingContent.ReadMap();

            BoardDressing authored = BoardDressing.Parse("test", "cloud 0 1000 7000 -2000 90 150");

            var clouds = new List<SceneryPlacement>();

            foreach (SceneryPlacement placement in BoardScenery.For(map, null, authored))
            {
                if (placement.Group == SceneryGroup.Cloud)
                {
                    clouds.Add(placement);
                }
            }

            Assert.That(clouds.Count, Is.EqualTo(1), "The generated sky survived a cloud line.");
            Assert.That(clouds[0].OffsetY, Is.EqualTo(7f).Within(1e-4f));
            Assert.That(clouds[0].Scale, Is.EqualTo(1.5f).Within(1e-4f));
        }

        [Test]
        public void WhatTheBakeWritesParsesBackAsItself()
        {
            var pieces = new List<SceneryPlacement>
            {
                new SceneryPlacement(SceneryGroup.Grove, 2, 9, 5, -0.3f, 0f, 0.1f, 40f, 1.2f),
                new SceneryPlacement(SceneryGroup.RimProp, 7, 9, 5, 0.62f, 0f, -0.44f, 315f, 1.7f),
                new SceneryPlacement(SceneryGroup.Camp, 1, 2, 11, -0.5f, 0f, 0.5f, 200f, 1.55f),
            };

            var cleared = new List<(int Column, int Row)> { (6, 3) };

            var sky = new List<SceneryPlacement>
            {
                new SceneryPlacement(SceneryGroup.Cloud, 0, 0, 0, 1.25f, 7.5f, -2f, 90f, 1.4f),
            };

            string text = BoardDressing.Write(pieces, cleared, sky);
            BoardDressing read = BoardDressing.Parse("baked", text);

            Assert.That(read.Speaks(6, 3), Is.True, "The cleared cell did not survive the round trip.");
            Assert.That(read.At(6, 3), Is.Empty, "The cleared cell came back with something on it.");

            Assert.That(Describe(read.At(9, 5)), Is.EqualTo(Describe(pieces.GetRange(0, 2))));
            Assert.That(Describe(read.At(2, 11)), Is.EqualTo(Describe(pieces.GetRange(2, 1))));
            Assert.That(Describe(new List<SceneryPlacement>(read.Sky)), Is.EqualTo(Describe(sky)));
        }

        /// <summary>
        /// Baking the same board twice writes the same bytes, so a diff shows
        /// what somebody moved rather than what order the loop ran in.
        /// </summary>
        [Test]
        public void TheBakeIsStable()
        {
            var pieces = new List<SceneryPlacement>
            {
                new SceneryPlacement(SceneryGroup.Grove, 2, 9, 5, -0.3f, 0f, 0.1f, 40f, 1.2f),
                new SceneryPlacement(SceneryGroup.Peak, 0, 1, 1, 0f, 0f, 0f, 0f, 1f),
            };

            Assert.That(
                BoardDressing.Write(pieces, null, null),
                Is.EqualTo(BoardDressing.Write(pieces, null, null)));
        }

        [Test]
        public void TheShippedFileParses()
        {
            string path = Path.Combine(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")),
                "content",
                "dressing.txt");

            Assert.That(File.Exists(path), Is.True, "Nothing authored at " + path + ".");

            Assert.DoesNotThrow(
                () => BoardDressing.Parse("dressing.txt", File.ReadAllText(path)),
                "The committed dressing file does not parse.");
        }

        [Test]
        public void ABadLineIsRefusedByNumber()
        {
            FormatException(() => BoardDressing.Parse("test", "\n\nplace 1 2 grove"), "line 3");
            FormatException(() => BoardDressing.Parse("test", "wobble 1 2"), "wobble");
            FormatException(() => BoardDressing.Parse("test", "place 1 2 shrubbery 0 0 0 0 100"), "shrubbery");
            FormatException(() => BoardDressing.Parse("test", "place 1 2 grove 0 0.5 0 0 100"), "0.5");
            FormatException(() => BoardDressing.Parse("test", "place 1 2 cloud 0 0 0 0 100"), "cloud");
        }

        [Test]
        public void CommentsAndBlankLinesAreIgnored()
        {
            BoardDressing read = BoardDressing.Parse(
                "test",
                "# a comment\n\n   \nclear 4 4   # trailing\n");

            Assert.That(read.CellCount, Is.EqualTo(1));
            Assert.That(read.Speaks(4, 4), Is.True);
        }

        private static void FormatException(TestDelegate what, string mentioning)
        {
            var thrown = Assert.Throws<System.FormatException>(what);

            Assert.That(
                thrown.Message,
                Does.Contain(mentioning),
                "The refusal does not name what was wrong with it.");
        }

        private static List<SceneryPlacement> On(List<SceneryPlacement> all, (int Column, int Row) cell)
        {
            var on = new List<SceneryPlacement>();

            foreach (SceneryPlacement placement in all)
            {
                if (placement.Group != SceneryGroup.Cloud
                    && placement.Column == cell.Column
                    && placement.Row == cell.Row)
                {
                    on.Add(placement);
                }
            }

            return on;
        }

        /// <summary>
        /// A list of pieces as text, so a failure reads as the board rather than
        /// as two object references that are not equal.
        /// </summary>
        private static string Describe(IReadOnlyList<SceneryPlacement> pieces)
        {
            var written = new System.Text.StringBuilder();

            foreach (SceneryPlacement piece in pieces)
            {
                written
                    .Append(piece.Group).Append(' ')
                    .Append(piece.Variant).Append(' ')
                    .Append(piece.Column).Append(',').Append(piece.Row).Append(' ')
                    .Append(Mathf.RoundToInt(piece.OffsetX * 1000f)).Append(' ')
                    .Append(Mathf.RoundToInt(piece.OffsetY * 1000f)).Append(' ')
                    .Append(Mathf.RoundToInt(piece.OffsetZ * 1000f)).Append(' ')
                    .Append(Mathf.RoundToInt(piece.Turn)).Append(' ')
                    .Append(Mathf.RoundToInt(piece.Scale * 100f))
                    .Append('\n');
            }

            return written.ToString();
        }

        /// <summary>The first cell the generator puts something on, at the shipped weight.</summary>
        private static (int Column, int Row) FirstGenerated(HexMap map)
        {
            foreach (SceneryPlacement placement in BoardScenery.For(map))
            {
                if (placement.Group != SceneryGroup.Cloud)
                {
                    return (placement.Column, placement.Row);
                }
            }

            throw new AssertionException("The committed board generates no scenery to override.");
        }
    }
}
