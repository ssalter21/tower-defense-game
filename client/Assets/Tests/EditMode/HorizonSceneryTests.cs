using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// Where the treeline and the hills go. Asserted with no scene, because the
    /// chooser is pure and this is the half of it that can be.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Whether it looks right is judged by looking</b> — in
    /// <c>docs/prototypes/scenery/</c>, and it took a dozen renders. What is
    /// asserted here is the small set of things that would be wrong no matter
    /// what the numbers were tuned to: something standing on the board, a band
    /// that ignores its own bounds, or a scatter that comes out different on the
    /// second run.
    /// </para>
    /// <para>
    /// <b>The clearance is the one that matters.</b> Everything out here is
    /// drawn on a plain the simulation cannot see, and a piece that strayed over
    /// the board would be scenery standing on a buildable hex that no tower
    /// could ever clear — the board's own dressing is hidden when a tower takes
    /// the cell, and nothing hides this.
    /// </para>
    /// </remarks>
    public class HorizonSceneryTests
    {
        /// <summary>A board about the size of the committed one, centred off the origin.</summary>
        private const float CentreX = 18.5f;

        private const float CentreZ = -10.4f;

        private const float HalfWidth = 19.5f;

        private const float HalfDepth = 11.5f;

        private const float Radius = 265f;

        [Test]
        public void NothingStandsOverTheBoard()
        {
            foreach (DistantPiece piece in Planted())
            {
                Assert.That(
                    Clearance(piece),
                    Is.GreaterThan(0f),
                    piece.Group + " is standing on the board at " + piece.X + "," + piece.Z
                    + ", where a tower could not clear it.");
            }
        }

        /// <summary>
        /// The wood keeps to its band, and the hills to theirs. The bands are
        /// allowed to overlap — they do, deliberately, so that there is no ring
        /// of bare ground between the trees and the rising land behind them.
        /// </summary>
        [Test]
        public void EachBandKeepsToItsOwnGround()
        {
            Planting planting = Planting.Default;

            foreach (DistantPiece piece in Planted())
            {
                float clear = Clearance(piece);

                if (piece.Group == SceneryGroup.Grove)
                {
                    Assert.That(
                        clear,
                        Is.InRange(planting.TreeGap, planting.TreeGap + planting.TreeDepth),
                        "A tree is outside the treeline's band.");
                }
                else
                {
                    Assert.That(
                        clear,
                        Is.InRange(planting.HillGap, Radius * planting.HillReach),
                        "A hill is outside the range's band.");
                }
            }
        }

        /// <summary>
        /// The same board plants the same wood, every time. The whole reason the
        /// placement is a hash of a lattice index rather than a random sequence:
        /// a frame captured today and a frame captured next month are comparable
        /// only if nothing moved on its own.
        /// </summary>
        [Test]
        public void ThePlantingIsTheSameEveryTime()
        {
            IReadOnlyList<DistantPiece> first = Planted();
            IReadOnlyList<DistantPiece> again = Planted();

            Assert.That(again.Count, Is.EqualTo(first.Count));

            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(again[index].Group, Is.EqualTo(first[index].Group), "group at " + index);
                Assert.That(again[index].X, Is.EqualTo(first[index].X), "x at " + index);
                Assert.That(again[index].Z, Is.EqualTo(first[index].Z), "z at " + index);
                Assert.That(again[index].Scale, Is.EqualTo(first[index].Scale), "scale at " + index);
            }
        }

        /// <summary>
        /// There is a wood and there is a range, and the committed numbers
        /// produce enough of each to be one. A band that quietly emptied would
        /// still pass every other test here.
        /// </summary>
        [Test]
        public void ThereIsEnoughOfBothToBeAWoodAndARange()
        {
            var counts = new Dictionary<SceneryGroup, int>();

            foreach (DistantPiece piece in Planted())
            {
                counts.TryGetValue(piece.Group, out int seen);
                counts[piece.Group] = seen + 1;
            }

            counts.TryGetValue(SceneryGroup.Grove, out int trees);
            counts.TryGetValue(SceneryGroup.Peak, out int peaks);
            counts.TryGetValue(SceneryGroup.Hill, out int mounds);

            Assert.That(trees, Is.GreaterThan(40), "The treeline is too thin to read as a wood.");
            Assert.That(peaks + mounds, Is.GreaterThan(10), "The range is too thin to read as one.");

            // Both kinds of high ground, or the skyline is a row of one shape.
            Assert.That(peaks, Is.GreaterThan(0), "No mountains in the range.");
            Assert.That(mounds, Is.GreaterThan(0), "No mounds in the range.");
        }

        /// <summary>
        /// A step of zero plants nothing rather than dividing by it. The shape
        /// of a bad number reaching a loop that walks a lattice by it.
        /// </summary>
        [Test]
        public void AStepOfNothingPlantsNothing()
        {
            IReadOnlyList<DistantPiece> nothing = HorizonScenery.For(
                CentreX,
                CentreZ,
                HalfWidth,
                HalfDepth,
                Radius,
                new Planting(4f, 13f, 0f, 0.8f, 13f, 0f, 0.5f, 0.5f, 0.13f));

            Assert.That(nothing, Is.Empty);
        }

        private static IReadOnlyList<DistantPiece> Planted() =>
            HorizonScenery.For(CentreX, CentreZ, HalfWidth, HalfDepth, Radius, Planting.Default);

        /// <summary>How far a piece stands clear of the board's footprint.</summary>
        private static float Clearance(DistantPiece piece)
        {
            float outX = Mathf.Max(Mathf.Abs(piece.X - CentreX) - HalfWidth, 0f);
            float outZ = Mathf.Max(Mathf.Abs(piece.Z - CentreZ) - HalfDepth, 0f);

            return Mathf.Sqrt((outX * outX) + (outZ * outZ));
        }
    }
}
