using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Sim;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// That the chrome has a size: every bar this game puts up, in both of its
    /// modes, resolves to a width and a height, and so does the text on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The assertion that matters is the one about text.</b> A bar is
    /// absolutely positioned with a stated height, so it measures out at that
    /// height whatever happens to the strings on it; a label has no size of its
    /// own and is whatever measuring its text returns. Where the text engine
    /// cannot measure, every label comes back zero by zero, the row they sit in
    /// collapses, and what is drawn stops agreeing with what is hit-tested — the
    /// palette is a sliver, and a click that looks like it landed on the board
    /// selects a tower nobody can see.
    /// </para>
    /// <para>
    /// <b>Written to be run outside the editor.</b> The editor process has the
    /// text engine's ICU data loaded, and a player only carries it when the
    /// build contains a <see cref="PanelSettings"/> asset, so this fixture
    /// passes in the editor whether the build carries one or not. The run that
    /// can fail is <c>tools/run-player-tests.ps1</c>, which is the one place
    /// <c>UNITY_EDITOR</c> is undefined.
    /// </para>
    /// <para>
    /// <b>The second half is about the roster's size.</b> Forty-four rows broke
    /// three pieces of chrome that twelve had fitted (#282): the wave bar, the
    /// offer's longest rung, and the header once a token count joined it.
    /// #285 signed an answer for each and asked for an assertion beside it, so
    /// that the next row added turns a test red rather than clipping quietly.
    /// Each is asserted against the roster as shipped -- every creep in the
    /// bar, every name on a rung -- and not against the number that happened
    /// to fit today.
    /// </para>
    /// </remarks>
    public class ChromeLayoutTests : ViewTest
    {
        /// <summary>Enough to carry the whole roster into one wave, and to climb a ladder.</summary>
        private const int ADeepPurse = 100000;

        private const int ArcherId = 3;

        private const int RangerId = 14;

        /// <summary>A cell of ground well away from the corridor and the chrome.</summary>
        private const int FreeColumn = 7;

        private const int FreeRow = 0;

        private const int PlayerWidth = 1600;

        private const int PlayerHeight = 900;

        /// <summary>
        /// Build mode puts three bars up. Each is asserted, because they are
        /// three panels built independently and a fix that reached only the one
        /// somebody looked at would pass on that one.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryBarOfTheBuildChromeMeasuresItsText()
        {
            MatchRoot root = Playfield();

            root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            // A runtime panel lays out when it is updated, so an assertion made
            // before a frame has passed reads zero off everything and fails for
            // the wrong reason.
            yield return null;
            yield return null;

            Measured(root.Loop.Header.Document, "Header");
            Measured(root.Palette.Document, "Palette");
            Measured(root.Wave.Document, "Wave");
        }

        /// <summary>
        /// Committing takes the build chrome down and puts watch mode's up: two
        /// more panels, built the same way and out of reach of the fixture
        /// above, which never leaves the round it opened in.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryBarOfTheWatchChromeMeasuresItsText()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            loop.Press();

            Assert.That(loop.Mode, Is.EqualTo(RunMode.Watching), "Committing puts the round on screen.");

            yield return null;
            yield return null;

            Measured(root.Loop.Header.Document, "Header");
            Measured(root.Controls.Document, "Bar");
            Measured(root.Loop.Switch.Document, "Results");
        }

        /// <summary>
        /// Every creep the roster has in one wave, and the bar scrolls: the
        /// boxes are wider than the strip, a scroller is up, and the trailing
        /// empty box -- the one a creep is added through -- has been scrolled
        /// into view rather than left past the edge.
        /// </summary>
        /// <remarks>
        /// The frame #270 photographed had the eleventh box cut in half and the
        /// empty one past three thousand pixels. This is composed rather than
        /// played: a round carrying one of every creep is the state the bar has
        /// to hold, and how a run got there is not what is being asserted.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheWaveBarScrollsToItsEmptyBoxWithEveryCreepInIt()
        {
            MatchRoot root = Playfield();

            root.BeginBuilding(Opening(gold: ADeepPurse, carried: EveryCreep()), TheMatchOnScreen.Art());

            yield return null;
            yield return null;
            yield return null;

            ScrollView scroller = root.Wave.Scroller;
            IReadOnlyList<VisualElement> boxes = root.Wave.Boxes;
            float strip = scroller.contentViewport.worldBound.width;
            float boxesWide = scroller.contentContainer.worldBound.width;

            Assert.That(boxes.Count, Is.EqualTo(CreepCount() + 1), "One box per creep and the empty one.");
            Assert.That(
                boxesWide,
                Is.GreaterThan(strip),
                "The premise: the roster's creeps are wider than the strip. If they are not, the bar no "
                + "longer overflows and this fixture is asserting nothing.");
            Assert.That(
                scroller.horizontalScroller.resolvedStyle.display,
                Is.Not.EqualTo(DisplayStyle.None),
                "A scroller shows once the boxes overflow.");

            Rect viewport = scroller.contentViewport.worldBound;
            Rect empty = boxes[boxes.Count - 1].worldBound;

            Assert.That(empty.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 1f), "The empty box is on screen.");
            Assert.That(empty.xMax, Is.LessThanOrEqualTo(viewport.xMax + 1f), "The empty box is on screen.");

            // The words the sitting used were "a scroll wheel": from the start
            // of the strip, one notch down moves it along. Sent to the strip's
            // content, which is where a ScrollView listens.
            scroller.scrollOffset = Vector2.zero;

            yield return null;

            var notch = new Event { type = EventType.ScrollWheel, delta = new Vector2(0f, 1f), mousePosition = viewport.center };

            using (WheelEvent wheel = WheelEvent.GetPooled(notch))
            {
                wheel.target = scroller.contentContainer;
                scroller.contentContainer.SendEvent(wheel);
            }

            yield return null;

            Assert.That(scroller.scrollOffset.x, Is.GreaterThan(0f), "A wheel notch scrolls the strip.");
        }

        /// <summary>
        /// Every tower name on the roster, over the longest price the surface
        /// carries, fits the width of a rung. Measured, not laid out: a label
        /// in a column stretches to its parent's width whatever its text, so
        /// the text is what is measured. Towers only, because a creep is never
        /// a rung.
        /// </summary>
        /// <remarks>
        /// Asserted on a real rung -- the Ranger's capstone, offered to a round
        /// holding a token -- so the font and the sizes are the offer's own and
        /// not a copy of them. The name it carries is then swapped for every
        /// other on the roster, because the row added next is the one this is
        /// for.
        /// </remarks>
        [UnityTest]
        public IEnumerator EveryTowerNameOnTheRosterFitsARungOverItsPrice()
        {
            MatchRoot root = Playfield();

            root.BeginBuilding(Opening(gold: ADeepPurse, tokens: 1), TheMatchOnScreen.Art());
            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));
            root.Palette.Take(Types().ById(RangerId));
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Palette.IsOffering, Is.True, "A Ranger with a token in hand offers its capstone.");

            yield return null;
            yield return null;

            Button rung = root.Palette.Rungs[0];
            Label name = rung.Q<Label>("Name");
            Label price = rung.Q<Label>("Price");

            Assert.That(name, Is.Not.Null, "A rung is a name over a price.");
            Assert.That(price, Is.Not.Null, "A rung is a name over a price.");
            Assert.That(price.text, Is.EqualTo(RosterNames.CapstoneToken()), "The longest price the surface carries.");

            // The rung's own laid-out width, which is the offer's, and not a copy
            // of the palette's constant: a narrower offer has to turn this red.
            float room = rung.resolvedStyle.width - rung.resolvedStyle.paddingLeft - rung.resolvedStyle.paddingRight;
            var tooWide = new StringBuilder();

            Assert.That(room, Is.GreaterThan(0f), "The rung has been laid out.");
            Assert.That(Width(price, price.text), Is.LessThanOrEqualTo(room), "The price fits a rung.");

            foreach (UnitType type in Types().Types)
            {
                if (type.Role != UnitRole.Placed)
                {
                    continue;
                }

                float width = Width(name, RosterNames.Of(type));

                if (width > room)
                {
                    tooWide.Append(RosterNames.Of(type)).Append(" (").Append(width.ToString("F0")).Append(") ");
                }
            }

            Assert.That(
                tooWide.ToString(),
                Is.Empty,
                "Names wider than the " + room.ToString("F0") + " units a rung has for one: " + tooWide);
        }

        [UnityTest]
        public IEnumerator EveryRootOnTheRosterFitsThePaletteBar()
        {
            MatchRoot root = Playfield();

            root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            var playerScreen = new RenderTexture(PlayerWidth, PlayerHeight, 24) { name = "Player screen" };

            try
            {
                root.Palette.Document.panelSettings.targetTexture = playerScreen;

                yield return null;
                yield return null;

                VisualElement bar = root.Palette.Document.rootVisualElement.Q<VisualElement>("Palette");
                IReadOnlyList<Button> entries = root.Palette.Entries;
                float barsRightEdge = bar.worldBound.xMax - bar.resolvedStyle.paddingRight;
                Button last = entries[entries.Count - 1];

                Assert.That(entries.Count, Is.EqualTo(root.Composing.Palette.Count), "One entry per root the round may build.");
                Assert.That(
                    bar.worldBound.width,
                    Is.EqualTo(RuntimePanel.ReferenceResolution.x).Within(1f),
                    "The premise: the bar is laid out at the width the player gives it.");
                Assert.That(
                    last.worldBound.xMax,
                    Is.LessThanOrEqualTo(barsRightEdge),
                    "The last entry, " + last.name + ", ends " + (last.worldBound.xMax - barsRightEdge).ToString("F0")
                    + " units past the bar's padding; every root has to fit the bar or the next one added clips quietly.");
            }
            finally
            {
                playerScreen.Release();
                Object.Destroy(playerScreen);
            }
        }

        /// <summary>
        /// The header's four fields sit in a row before the button, with the
        /// spacer between them still holding some width -- so the field added
        /// last has not pushed the button off the end or overlapped it.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHeadersFourFieldsFitBeforeTheButton()
        {
            MatchRoot root = Playfield();
            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            yield return null;
            yield return null;

            RunHeader header = loop.Header;
            VisualElement bar = header.Document.rootVisualElement.Q<VisualElement>("Header");
            VisualElement spacer = bar.Q<VisualElement>("Spacer");

            Assert.That(header.Tokens.text, Is.Not.Empty, "The fourth field is up.");
            Assert.That(header.Wave.worldBound.xMax, Is.LessThanOrEqualTo(header.Health.worldBound.xMin));
            Assert.That(header.Health.worldBound.xMax, Is.LessThanOrEqualTo(header.Gold.worldBound.xMin));
            Assert.That(header.Gold.worldBound.xMax, Is.LessThanOrEqualTo(header.Tokens.worldBound.xMin));
            Assert.That(header.Tokens.worldBound.xMax, Is.LessThanOrEqualTo(header.Action.worldBound.xMin));
            Assert.That(
                spacer.worldBound.width,
                Is.GreaterThan(0f),
                "The spacer is what keeps the fields and the button apart; at zero the next field pushes the button.");
            Assert.That(
                header.Action.worldBound.xMax,
                Is.LessThanOrEqualTo(bar.worldBound.xMax + 1f),
                "The button is still inside the bar.");
        }

        /// <summary>
        /// Asserts the bar called <paramref name="barName"/> on
        /// <paramref name="document"/> has a size, and that every piece of text
        /// on it has one too.
        /// </summary>
        private static void Measured(UIDocument document, string barName)
        {
            Assert.That(document, Is.Not.Null, barName + " has a panel at all.");

            VisualElement bar = document.rootVisualElement.Q<VisualElement>(barName);

            Assert.That(bar, Is.Not.Null, barName + " was built onto that panel.");
            Assert.That(bar.resolvedStyle.width, Is.GreaterThan(0f), barName + " has a width.");
            Assert.That(bar.resolvedStyle.height, Is.GreaterThan(0f), barName + " has a height.");

            int measured = 0;

            foreach (TextElement text in bar.Query<TextElement>().Build())
            {
                if (string.IsNullOrEmpty(text.text))
                {
                    continue;
                }

                string what = barName + "'s \"" + text.text + "\"";

                Assert.That(text.resolvedStyle.width, Is.GreaterThan(0f), what + " has a width.");
                Assert.That(text.resolvedStyle.height, Is.GreaterThan(0f), what + " has a height.");
                Assert.That(text.contentRect.width, Is.GreaterThan(0f), what + " fills its box.");

                measured++;
            }

            Assert.That(measured, Is.GreaterThan(0), barName + " has text on it to measure.");
        }

        /// <summary>The natural width of <paramref name="text"/> in <paramref name="label"/>'s own font and size.</summary>
        private static float Width(Label label, string text) =>
            label.MeasureTextSize(text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;

        /// <summary>
        /// A round opening on an empty board with as much gold as the caller
        /// says, priced and laddered out of the shipped content.
        /// </summary>
        private static ComposedRound Opening(int gold = 100, WaveScript carried = null, int tokens = 0)
        {
            UnitTypeTable types = Types();
            Ruleset rules = StreamingContent.ReadRuleset();

            return new ComposedRound(
                wave: 1,
                carried ?? WaveScript.Nothing,
                StreamingContent.ReadUpgrades(types),
                Purse.Holding(gold),
                tokens,
                CostTable.From(rules, types),
                types,
                StreamingContent.ReadMap(),
                Board.Empty);
        }

        /// <summary>One of every creep the roster has, as a wave already carried.</summary>
        private static WaveScript EveryCreep()
        {
            UnitTypeTable types = Types();
            var text = new StringBuilder();
            int tick = 0;

            foreach (UnitType type in types.Types)
            {
                if (type.Role == UnitRole.Moving)
                {
                    text.AppendLine("order  " + tick + "  " + type.Id + "  1  0");
                    tick += 1;
                }
            }

            return WaveScript.Parse("carried", text.ToString(), types);
        }

        private static int CreepCount()
        {
            int count = 0;

            foreach (UnitType type in Types().Types)
            {
                if (type.Role == UnitRole.Moving)
                {
                    count++;
                }
            }

            return count;
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
    }
}
