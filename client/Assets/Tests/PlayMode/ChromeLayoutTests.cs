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
    /// the wave bar that twelve had fitted (#282), so it is asserted against
    /// the roster as shipped -- every creep in the bar -- and not against the
    /// number that happened to fit today.
    /// </para>
    /// </remarks>
    public class ChromeLayoutTests : ViewTest
    {
        /// <summary>Enough to carry the whole roster into one wave, and to climb a ladder.</summary>
        private const int ADeepPurse = 100000;

        /// <summary>
        /// Build mode puts three bars up and watch mode two more. Each is
        /// asserted, because they are panels built independently and a fix
        /// that reached only the one somebody looked at would pass on that one.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryBarOfTheChromeMeasuresItsText()
        {
            MatchRoot root = Playfield();

            RunLoop loop = root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            // A runtime panel lays out when it is updated, so an assertion made
            // before a frame has passed reads zero off everything and fails for
            // the wrong reason.
            yield return null;
            yield return null;

            Measured(root.Loop.Header.Document, "Header");
            Measured(root.Palette.Document, "Palette");
            Measured(root.Wave.Document, "Wave");

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

    }
}
