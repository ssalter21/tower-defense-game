using System.Collections.Generic;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.EditMode
{
    /// <summary>
    /// Which tile a beside prop stands on: the one its art asks for while
    /// nothing is on it, a free neighbour behind rather than in front when
    /// something is, never the corridor, and the tower's own hex when every
    /// neighbour is taken.
    /// </summary>
    /// <remarks>
    /// Pure arithmetic over a five-by-five map, for the reason
    /// <see cref="RoutePathTests"/> gives: the rule that decides where a prop
    /// is drawn is checkable in milliseconds and cannot be broken by an import.
    /// The corridor runs along row zero, so a tower resting at the identity --
    /// facing positive z, which is toward row zero -- faces it, and "behind" is
    /// toward the higher rows.
    /// </remarks>
    public class BesideStandingTests
    {
        private const string Map =
            "S###E\n"
            + ".....\n"
            + ".....\n"
            + ".....\n"
            + ".....\n"
            + "\n"
            + "aaaaa\n"
            + "aaaaa\n"
            + "aaaaa\n"
            + "aaaaa\n"
            + "aaaaa\n";

        /// <summary>The same board with the cell to the tower's right raised two tiers.</summary>
        private const string RaisedMap =
            "S###E\n"
            + ".....\n"
            + ".....\n"
            + ".....\n"
            + ".....\n"
            + "\n"
            + "aaaaa\n"
            + "aaaaa\n"
            + "aaaca\n"
            + "aaaaa\n"
            + "aaaaa\n";

        private const int Column = 2;

        private const int Row = 2;

        private static readonly Vector3 Asked = BesideProp.NextTile;

        [Test]
        public void ThePropStandsOnTheTileItsArtAsksForWhileNothingIsOnIt()
        {
            Vector3 offset = BesideStanding.On(HexMap.Parse(Map), Nothing, Column, Row, Quaternion.identity, Asked);

            Assert.That(offset, Is.EqualTo(Delta(Map, 3, 2)).Using(Near), "One tile to the right is the cell to the right.");
        }

        [Test]
        public void ATakenTileSendsThePropToTheFreeNeighbourBehindItRatherThanInFront()
        {
            Vector3 offset = BesideStanding.On(HexMap.Parse(Map), Only(3, 2), Column, Row, Quaternion.identity, Asked);

            Assert.That(
                offset,
                Is.EqualTo(Delta(Map, 2, 3)).Using(Near),
                "Sixty degrees behind the taken cell, away from the corridor; (2, 1) is the same angle "
                + "in front and loses the tie.");
        }

        [Test]
        public void WithTheCellBehindTakenTooThePropGoesBehindOnTheFarSideRatherThanTowardTheCorridor()
        {
            Vector3 offset = BesideStanding.On(
                HexMap.Parse(Map), Either((3, 2), (2, 3)), Column, Row, Quaternion.identity, Asked);

            Assert.That(
                offset,
                Is.EqualTo(Delta(Map, 1, 3)).Using(Near),
                "Behind and to the left beats in front and to the right: away from the corridor first, "
                + "toward the asked side second.");
        }

        [Test]
        public void TheCorridorIsNeverATile()
        {
            // A tower on row one: two of its neighbours are corridor, one of the
            // ground ones is taken, and the answer is the free ground cell
            // behind it -- not the route cell in front, which lies exactly as far
            // toward the asked direction.
            Vector3 offset = BesideStanding.On(HexMap.Parse(Map), Only(3, 1), 2, 1, Quaternion.identity, Asked);

            Assert.That(offset, Is.EqualTo(Delta(Map, 2, 1, 3, 2)).Using(Near));
        }

        [Test]
        public void WithNoFreeNeighbourThePropStandsInsideTheTowersOwnHex()
        {
            Vector3 offset = BesideStanding.On(HexMap.Parse(Map), Everything, Column, Row, Quaternion.identity, Asked);

            Assert.That(offset, Is.EqualTo(new Vector3(BesideStanding.InsideTheHex, 0f, 0f)).Using(Near));
            Assert.That(
                offset.magnitude,
                Is.LessThan(HexGeometry.ColumnPitch * 0.5f),
                "Inside the hex means short of its edge, on no tile but the tower's.");
        }

        [Test]
        public void ThePropStandsAtTheNeighboursOwnHeight()
        {
            Vector3 offset = BesideStanding.On(HexMap.Parse(RaisedMap), Nothing, Column, Row, Quaternion.identity, Asked);

            Assert.That(offset.y, Is.EqualTo(2 * HexGeometry.LevelStep).Within(1e-4f), "Two tiers up is two half-blocks.");
            Assert.That(offset, Is.EqualTo(Delta(RaisedMap, 3, 2)).Using(Near));
        }

        [Test]
        public void AnOffsetInsideTheTowersOwnHexIsLeftAsAsked()
        {
            var asked = new Vector3(0.6f, 0f, 0.2f);

            Vector3 offset = BesideStanding.On(HexMap.Parse(Map), Everything, Column, Row, Quaternion.identity, asked);

            Assert.That(offset, Is.EqualTo(asked), "It reaches no neighbour, so there is nothing to collide with.");
        }

        [Test]
        public void TheAnswerIsInTheFrameTheTowerRestsIn()
        {
            // Turned about: the tower's right is the world's left, so the asked
            // tile is the cell on the left, and the offset handed back composes
            // with the same facing to land on it.
            Quaternion resting = Quaternion.Euler(0f, 180f, 0f);

            Vector3 offset = BesideStanding.On(HexMap.Parse(Map), Nothing, Column, Row, resting, Asked);

            Assert.That(resting * offset, Is.EqualTo(Delta(Map, 1, 2)).Using(Near));
        }

        // ---------------------------------------------------------------
        // Scaffolding
        // ---------------------------------------------------------------

        private static bool Nothing(int column, int row) => false;

        private static bool Everything(int column, int row) => true;

        private static System.Func<int, int, bool> Only(int column, int row) =>
            (c, r) => c == column && r == row;

        private static System.Func<int, int, bool> Either((int Column, int Row) first, (int Column, int Row) second) =>
            (c, r) => (c == first.Column && r == first.Row) || (c == second.Column && r == second.Row);

        /// <summary>From the tower's cell to another, at each cell's own height.</summary>
        private static Vector3 Delta(string map, int column, int row) => Delta(map, Column, Row, column, row);

        private static Vector3 Delta(string map, int fromColumn, int fromRow, int column, int row)
        {
            HexMap parsed = HexMap.Parse(map);

            return HexGeometry.ToWorld(column, row, parsed.LevelAt(column, row))
                - HexGeometry.ToWorld(fromColumn, fromRow, parsed.LevelAt(fromColumn, fromRow));
        }

        private static readonly IEqualityComparer<Vector3> Near = new Nearly();

        private sealed class Nearly : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 left, Vector3 right) => (left - right).sqrMagnitude < 1e-6f;

            public int GetHashCode(Vector3 vector) => vector.GetHashCode();
        }
    }
}
