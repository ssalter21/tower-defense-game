using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Sim;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The board is clickable: a tower is chosen from the palette and stood on a
    /// hex, a standing tower offers its ladder where it stands, and nothing
    /// either of them does reaches the run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every legality assertion here is really an assertion about who
    /// decided.</b> The tests do not check that a corridor cell is refused —
    /// <c>sim</c> has tests for that. They check that the screen's answer is the
    /// same answer, because it came from the same call: prevention that drifted
    /// from refusal is the failure ADR-0051 is built to make impossible, and it
    /// would be invisible from either side alone.
    /// </para>
    /// <para>
    /// <b>Driven through the screen and not around it.</b> A placement is a
    /// world point projected to a screen point and clicked, so the picking, the
    /// palette's selection and the composed phase are all in the path. The one
    /// thing simulated by hand is the device: <see cref="BuildInput.Click"/>
    /// takes the coordinate a mouse would have reported, which is the same split
    /// the camera rig's tests use.
    /// </para>
    /// </remarks>
    public class BuildingTests : ViewTest
    {
        /// <summary>Rows in <c>content/units.txt</c>, named so the tests read.</summary>
        private const int SoldierId = 11;

        private const int ArcherId = 3;

        private const int MageId = 4;

        private const int RangerId = 14;

        /// <summary>
        /// A cell of ground with nothing on it, well away from the corridor and
        /// high enough on the default screen to be nowhere near the chrome.
        /// </summary>
        private const int FreeColumn = 7;

        private const int FreeRow = 0;

        /// <summary>A second one, for the tests that build twice.</summary>
        private const int SecondColumn = 5;

        private const int SecondRow = 0;

        /// <summary>A cell the corridor runs through. Nothing may stand here.</summary>
        private const int CorridorColumn = 4;

        private const int CorridorRow = 1;

        [Test]
        public void ThePaletteListsWhatMayBeBuilt()
        {
            MatchRoot root = Building(Opening());

            IReadOnlyList<UnitType> palette = root.Composing.Palette;

            Assert.That(
                Ids(palette),
                Is.EqualTo(new[] { SoldierId, ArcherId, MageId }),
                "The three tier-one towers, cheapest first. The Ranger is some edge's target, so it is "
                + "reached by upgrading and never placed — offering it would be offering a refusal.");

            Assert.That(root.Palette.Entries.Count, Is.EqualTo(3));
        }

        /// <summary>
        /// The entry says a name and a price and nothing else. No type ids on
        /// screen, and none of the record's vocabulary — see ADR-0051 and #196.
        /// </summary>
        [Test]
        public void AnEntryIsANameAndAPrice()
        {
            MatchRoot root = Building(Opening());

            Assert.That(Wording(root.Palette.Entries[0]), Does.Contain("Soldier"));
            Assert.That(Wording(root.Palette.Entries[0]), Does.Contain("30 gold"));
            Assert.That(Wording(root.Palette.Entries[1]), Does.Contain("Archer"));
            Assert.That(Wording(root.Palette.Entries[2]), Does.Contain("Mage"));

            foreach (Button entry in root.Palette.Entries)
            {
                Assert.That(Wording(entry).ToLowerInvariant(), Does.Not.Contain("ordinary"));
                Assert.That(Wording(entry).ToLowerInvariant(), Does.Not.Contain("game changer"));
                Assert.That(Wording(entry).ToLowerInvariant(), Does.Not.Contain("type"));
            }
        }

        [Test]
        public void AClickPlacesTheSelectedTowerOnTheHexUnderIt()
        {
            MatchRoot root = Building(Opening());

            Select(root, ArcherId);

            Assert.That(root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow)), Is.True);

            UnitType standing = root.Composing.StandingOn(FreeColumn, FreeRow);

            Assert.That(standing, Is.Not.Null, "The tower is on the composed board.");
            Assert.That(standing.Id, Is.EqualTo(ArcherId));
            Assert.That(root.Composing.Phase.Actions.Count, Is.EqualTo(1));
            Assert.That(root.Composing.Phase.Actions[0].Kind, Is.EqualTo(ActionKind.Place));
            Assert.That(root.Composing.Phase.Actions[0].Column, Is.EqualTo(FreeColumn));
            Assert.That(root.Composing.Phase.Actions[0].Row, Is.EqualTo(FreeRow));
            Assert.That(root.Composing.Gold, Is.EqualTo(60), "A hundred, less what an Archer costs.");
        }

        /// <summary>
        /// The tower turns up on screen, at the cell it was clicked on, drawn
        /// with the model that unit type is drawn with everywhere else.
        /// </summary>
        [Test]
        public void ThePlacedTowerIsDrawnWhereItStands()
        {
            MatchRoot root = Building(Opening());

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Building.Towers.Count, Is.EqualTo(1));

            foreach (KeyValuePair<int, TowerView> drawn in root.Building.Towers)
            {
                Assert.That(drawn.Value.Type.Id, Is.EqualTo(ArcherId));
                Assert.That(drawn.Value.Model, Is.Not.Null);
                Assert.That(
                    drawn.Value.transform.position,
                    Is.EqualTo(HexGeometry.ToWorld(FreeColumn, FreeRow)).Using(Near));
            }
        }

        /// <summary>
        /// The whole point of ray-plane picking rather than a fixed projection:
        /// the same click works after the camera has been turned, tilted past
        /// the top and flown off the middle of the board.
        /// </summary>
        [Test]
        public void ItPlacesFromARotatedCamera()
        {
            MatchRoot root = Building(Opening());
            OrbitCameraRig rig = root.CameraRig;

            rig.PointAt(137f, 62f, rig.FramedDistance * 0.8f);
            rig.Fly(new Vector3(0.1f, 0f, -0.05f));

            Select(root, SoldierId);

            Assert.That(TryBuildableCellOnScreen(root, out int column, out int row), Is.True);
            Assert.That(root.Pointer.Click(ScreenPointOf(root, column, row)), Is.True);
            Assert.That(root.Composing.StandingOn(column, row).Id, Is.EqualTo(SoldierId));
        }

        /// <summary>
        /// Prevention, not refusal: what lights is what
        /// <c>BuildPhase.Resolve</c> would accept, and a cell it would refuse
        /// simply does not light.
        /// </summary>
        [Test]
        public void AHexThatCannotTakeTheSelectedTowerDoesNotLight()
        {
            MatchRoot root = Building(Opening());

            Select(root, ArcherId);

            root.Pointer.Point(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Building.IsLit, Is.True);
            Assert.That((root.Building.LitColumn, root.Building.LitRow), Is.EqualTo((FreeColumn, FreeRow)));

            root.Pointer.Point(ScreenPointOf(root, CorridorColumn, CorridorRow));

            Assert.That(root.Building.IsLit, Is.False, "The corridor is not somewhere a tower may stand.");

            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));
            root.Pointer.Point(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Building.IsLit, Is.False, "Something is already standing there.");
        }

        [Test]
        public void NothingLightsWhileNothingIsSelected()
        {
            MatchRoot root = Building(Opening());

            root.Pointer.Point(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Building.IsLit, Is.False);
        }

        /// <summary>
        /// An unaffordable tower stays on the bar, reads its price in red, and
        /// lights nothing. The price is the explanation, so the price is what
        /// changes.
        /// </summary>
        [Test]
        public void AnUnaffordableTowerReadsRedAndLightsNothing()
        {
            MatchRoot root = Building(Opening(gold: 35));

            Assert.That(
                Ids(root.Composing.Palette),
                Is.EqualTo(new[] { SoldierId, ArcherId, MageId }),
                "The bar does not shrink when the purse does.");

            Assert.That(root.Composing.CanAfford(root.Composing.Palette[0]), Is.True, "A Soldier is 30.");
            Assert.That(root.Composing.CanAfford(root.Composing.Palette[1]), Is.False, "An Archer is 40.");

            Assert.That(PriceColour(root.Palette.Entries[1]), Is.Not.EqualTo(PriceColour(root.Palette.Entries[0])));

            Select(root, ArcherId);
            root.Pointer.Point(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Building.IsLit, Is.False);
            Assert.That(root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow)), Is.False);
            Assert.That(root.Composing.Phase.Actions.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// The price goes red as the round is composed, not only when it opens
        /// poor: spending is what makes the next thing unaffordable.
        /// </summary>
        [Test]
        public void PricesGoRedAsThePurseIsSpent()
        {
            MatchRoot root = Building(Opening());

            Color before = PriceColour(root.Palette.Entries[2]);

            Assert.That(root.Composing.CanAfford(root.Composing.Palette[2]), Is.True, "A Mage is 92 of 100.");

            Select(root, SoldierId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Composing.Gold, Is.EqualTo(70));
            Assert.That(root.Composing.CanAfford(root.Composing.Palette[2]), Is.False);
            Assert.That(PriceColour(root.Palette.Entries[2]), Is.Not.EqualTo(before));
        }

        [Test]
        public void ANumberKeySelectsAnEntryAndPressingItAgainClearsIt()
        {
            MatchRoot root = Building(Opening());

            root.Pointer.Shortcut(1);

            Assert.That(root.Palette.Selected.Id, Is.EqualTo(ArcherId), "The second entry.");

            root.Pointer.Shortcut(1);

            Assert.That(root.Palette.Selected, Is.Null);

            root.Pointer.Shortcut(TowerPalette.ShortcutCount - 1);

            Assert.That(root.Palette.Selected, Is.Null, "There is no ninth tower.");
        }

        /// <summary>
        /// An upgrade names its target by hex, so it is offered at the hex: the
        /// tower standing there is what decides which rungs exist.
        /// </summary>
        [Test]
        public void ClickingAPlacedTowerOffersItsLadderThere()
        {
            MatchRoot root = Building(Opening());

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow)), Is.False);
            Assert.That(root.Palette.IsOffering, Is.True);
            Assert.That((root.Palette.OfferColumn, root.Palette.OfferRow), Is.EqualTo((FreeColumn, FreeRow)));
            Assert.That(root.Palette.Rungs.Count, Is.EqualTo(1));
            Assert.That(root.Palette.Rungs[0].text, Does.Contain("Ranger"));
            Assert.That(root.Palette.Rungs[0].text, Does.Contain("40 gold"));
        }

        [Test]
        public void TakingARungClimbsTheLadderInPlace()
        {
            MatchRoot root = Building(Opening());

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            int ordinal = root.Composing.Board.Placements[0].Id;

            root.Palette.Take(Types().ById(RangerId));

            Assert.That(root.Composing.StandingOn(FreeColumn, FreeRow).Id, Is.EqualTo(RangerId));
            Assert.That(root.Composing.Board.Count, Is.EqualTo(1), "An upgrade replaces, it does not add.");
            Assert.That(
                root.Composing.Board.Placements[0].Id,
                Is.EqualTo(ordinal),
                "A placement keeps its ordinal across an upgrade.");

            Assert.That(root.Composing.Gold, Is.EqualTo(20), "A hundred, less an Archer and a Ranger.");
            Assert.That(root.Palette.IsOffering, Is.False, "The ladder goes away once a rung is taken.");
            Assert.That(root.Building.Towers[ordinal].Type.Id, Is.EqualTo(RangerId), "And it is redrawn.");
        }

        /// <summary>
        /// A tower with nothing above it offers nothing, and neither does one
        /// whose rung the purse cannot cover. Prevention at a hex.
        /// </summary>
        [Test]
        public void ATowerWithNoAffordableUpgradeOffersNone()
        {
            MatchRoot root = Building(Opening(gold: 105));

            Select(root, SoldierId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Palette.IsOffering, Is.False, "The ladder carries no edge out of a Soldier.");

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, SecondColumn, SecondRow));

            Assert.That(root.Composing.Gold, Is.EqualTo(35), "A hundred and five, less 30 and 40.");
            Assert.That(root.Composing.UpgradesOn(SecondColumn, SecondRow), Is.Empty, "A Ranger is 40.");

            root.Pointer.Click(ScreenPointOf(root, SecondColumn, SecondRow));

            Assert.That(root.Palette.IsOffering, Is.False, "An Archer with no reachable rung offers none.");
        }

        /// <summary>
        /// <b>No input reaches the simulation.</b> Everything above composes a
        /// phase in a local; the run is untouched until somebody commits, which
        /// is a thing nothing on this screen can do yet.
        /// </summary>
        [Test]
        public void NothingClickedReachesTheRun()
        {
            MatchRoot root = Playfield();
            UnitTypeTable types = Types();

            Ruleset rules = StreamingContent.ReadRuleset();
            UpgradeLadder ladder = StreamingContent.ReadUpgrades(types);

            var run = new Run(
                root.Map,
                rules,
                types,
                ladder,
                FieldPool.Canned(
                    root.Map,
                    rules,
                    types,
                    ladder,
                    StreamingContent.ReadDefense(types),
                    StreamingContent.ReadWave(types)),
                TheMatchOnScreen.Seed);

            root.BeginBuilding(ComposedRound.For(run), TheMatchOnScreen.Art());

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Composing.Board.Count, Is.EqualTo(1));
            Assert.That(run.Board.Count, Is.EqualTo(0), "The run's board has not moved.");
            Assert.That(run.Purse.Gold, Is.EqualTo(100), "Nor has its purse.");
            Assert.That(run.Round, Is.EqualTo(0), "Nor has its round.");
        }

        /// <summary>
        /// A click on the chrome stops at the chrome. Which way up a screen
        /// point is read is the one thing here a reasonable person could get
        /// backwards, and getting it backwards is invisible: the palette would
        /// still select and the tower would <i>also</i> land on whatever hex was
        /// behind the bar.
        /// </summary>
        /// <remarks>
        /// A frame has to pass first. A runtime panel lays out when it is
        /// updated, and asking an unlaid-out panel what is under a point gets
        /// nothing under anything — which would pass an assertion that the top
        /// of the screen is clear while proving nothing about the bottom. So
        /// both ends are asserted, and the pair is what pins the orientation.
        /// </remarks>
        [UnityTest]
        public IEnumerator ChromeSwallowsTheClicksThatLandOnIt()
        {
            MatchRoot root = Building(Opening());

            yield return null;
            yield return null;

            // The palette is anchored to the bottom edge of a panel laid out at
            // 1080 high and scaled to the window's height, so a point half its
            // height up is a point on it.
            float scale = Screen.height / 1080f;
            var onTheBar = new Vector2(
                Screen.width * 0.5f, TowerPalette.BarHeight * 0.5f * scale);
            var overTheBoard = new Vector2(Screen.width * 0.5f, Screen.height * 0.8f);

            Assert.That(root.Palette.Covers(onTheBar), Is.True, "A point on the palette bar.");
            Assert.That(root.Palette.Covers(overTheBoard), Is.False, "A point well above it.");

            Select(root, ArcherId);

            Assert.That(root.Pointer.Click(onTheBar), Is.False);
            Assert.That(
                root.Composing.Phase.Actions.Count,
                Is.EqualTo(0),
                "A click on the bar must not also land on the board behind it.");

            root.Pointer.Point(onTheBar);

            Assert.That(root.Building.IsLit, Is.False);
        }

        /// <summary>
        /// The build chrome reaches the bottom edge of the screen. Nothing else
        /// in build mode is drawn down there, so a gap under the palette is a
        /// strip of board the player can see, cannot reach past the panels above
        /// it, and has no reason to expect to be dead.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The point is a literal, on purpose.</b> The defect this pins was a
        /// bar anchored to <c>PlaybackControls</c>'s height — watch mode's
        /// chrome, which <see cref="RunLoop.Commit"/> only puts up after it has
        /// taken this chrome down — and every assertion in this file computed
        /// its click point from that same expression, so all of them moved with
        /// it. An assertion written in the layout's own arithmetic cannot see a
        /// layout mistake.
        /// </para>
        /// <para>
        /// Two reference units up rather than zero: the bottom row of pixels is
        /// where a rounding disagreement between the panel's scale and the
        /// screen's height would land, and this test is about a bar being
        /// eighty-eight units off the floor rather than about a pixel.
        /// </para>
        /// </remarks>
        [UnityTest]
        public IEnumerator TheBuildChromeReachesTheBottomEdge()
        {
            MatchRoot root = Building(Opening());

            yield return null;
            yield return null;

            float scale = Screen.height / 1080f;
            var onTheEdge = new Vector2(Screen.width * 0.5f, 2f * scale);

            Assert.That(
                root.Palette.Covers(onTheEdge),
                Is.True,
                "The bottom edge of the screen in build mode is the palette.");

            Select(root, ArcherId);

            Assert.That(root.Pointer.Click(onTheEdge), Is.False);
            Assert.That(
                root.Composing.Phase.Actions.Count,
                Is.EqualTo(0),
                "And a click there is the palette's, not the board's.");
        }

        /// <summary>
        /// The offer at a hex hangs on the same premise the chrome guard does:
        /// that a panel's <c>y</c> runs down from the top while a screen point's
        /// runs up from the bottom. Pinned here, because the two use different
        /// engine calls to cross that boundary and only one of them is exercised
        /// by the guard.
        /// </summary>
        [UnityTest]
        public IEnumerator AHexProjectsIntoThePanelTheSameWayUp()
        {
            MatchRoot root = Building(Opening());

            yield return null;

            IPanel panel = root.Palette.Document.rootVisualElement.panel;
            Camera camera = root.CameraRig.Camera;
            Vector3 world = HexGeometry.ToWorld(FreeColumn, FreeRow);

            Vector3 screen = camera.WorldToScreenPoint(world);
            Vector2 expected = RuntimePanelUtils.ScreenToPanel(panel, RuntimePanel.Downwards(screen));
            Vector2 projected = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, camera);

            Assert.That(projected.x, Is.EqualTo(expected.x).Within(1f));
            Assert.That(projected.y, Is.EqualTo(expected.y).Within(1f));
        }

        // ---------------------------------------------------------------
        // Scaffolding
        // ---------------------------------------------------------------

        private static readonly IEqualityComparer<Vector3> Near = new Nearly();

        private MatchRoot Building(ComposedRound round)
        {
            MatchRoot root = Playfield();

            root.BeginBuilding(round, TheMatchOnScreen.Art());

            return root;
        }

        /// <summary>
        /// A round opening on an empty board with as much gold as the caller
        /// says, priced and laddered out of the shipped content.
        /// </summary>
        private static ComposedRound Opening(int gold = 100, WaveScript carried = null)
        {
            UnitTypeTable types = Types();
            Ruleset rules = StreamingContent.ReadRuleset();

            return new ComposedRound(
                wave: 1,
                carried ?? WaveScript.Nothing,
                StreamingContent.ReadUpgrades(types),
                Purse.Holding(gold),
                CostTable.From(rules, types),
                types,
                StreamingContent.ReadMap(),
                Board.Empty);
        }

        private static UnitTypeTable Types() => StreamingContent.ReadUnitTypes();

        private static void Select(MatchRoot root, int typeId)
        {
            for (int index = 0; index < root.Composing.Palette.Count; index++)
            {
                if (root.Composing.Palette[index].Id == typeId)
                {
                    root.Pointer.Shortcut(index);

                    return;
                }
            }

            Assert.Fail("No palette entry for type id " + typeId);
        }

        private static Vector2 ScreenPointOf(MatchRoot root, int column, int row) =>
            root.CameraRig.Camera.WorldToScreenPoint(
                HexGeometry.ToWorld(column, row, root.Map.LevelAt(column, row)));

        /// <summary>
        /// A cell that could take the selected tower and is high enough on the
        /// screen to be clear of the chrome along the bottom.
        /// </summary>
        private static bool TryBuildableCellOnScreen(MatchRoot root, out int column, out int row)
        {
            Camera camera = root.CameraRig.Camera;

            for (row = 0; row < root.Map.Height; row++)
            {
                for (column = 0; column < root.Map.Width; column++)
                {
                    Vector3 screen = camera.WorldToScreenPoint(
                        HexGeometry.ToWorld(column, row, root.Map.LevelAt(column, row)));

                    if (screen.z > 0f
                        && screen.y > Screen.height * 0.4f
                        && root.Composing.Allows(
                            BuildAction.Of(ActionKind.Place, root.Palette.Selected.Id, column, row)))
                    {
                        return true;
                    }
                }
            }

            column = 0;
            row = 0;

            return false;
        }

        private static int[] Ids(IReadOnlyList<UnitType> types)
        {
            var ids = new int[types.Count];

            for (int index = 0; index < ids.Length; index++)
            {
                ids[index] = types[index].Id;
            }

            return ids;
        }

        private static string Wording(VisualElement entry)
        {
            var words = new System.Text.StringBuilder();

            foreach (Label label in entry.Query<Label>().ToList())
            {
                words.Append(label.text).Append(' ');
            }

            return words.ToString();
        }

        /// <summary>
        /// The colour the price is written in. Read off the inline style rather
        /// than the resolved one, because a panel in a headless run has never
        /// been laid out and resolves nothing.
        /// </summary>
        private static Color PriceColour(VisualElement entry) =>
            entry.Q<Label>("Price").style.color.value;

        /// <summary>Vector equality to a tenth of a millimetre.</summary>
        private sealed class Nearly : IEqualityComparer<Vector3>
        {
            public bool Equals(Vector3 left, Vector3 right) => (left - right).sqrMagnitude < 1e-8f;

            public int GetHashCode(Vector3 vector) => vector.GetHashCode();
        }
    }
}
