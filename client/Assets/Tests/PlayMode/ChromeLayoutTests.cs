using System.Collections;
using NUnit.Framework;
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
    /// </remarks>
    public class ChromeLayoutTests : ViewTest
    {
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

        private MatchRoot Playfield() =>
            Spawn(SceneFraming.RootObjectName).AddComponent<MatchRoot>();

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
    }
}
