using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using View;

namespace Tests.PlayMode
{
    public sealed class WrapperTests : ViewTest
    {
        [Test]
        public void TheMenuIsUpAndNoRunHasStarted()
        {
            MatchRoot root = Playfield();
            MainMenu menu = OpenMenu(root);

            Assert.That(root.Loop, Is.Null);
            Assert.That(menu.IsShown, Is.True);
            Assert.That(menu.Settings.IsShown, Is.False);
            Assert.That(menu.StartButton.text, Is.Not.Empty);
            Assert.That(menu.SettingsButton.text, Is.Not.Empty);
            Assert.That(menu.QuitButton.text, Is.Not.Empty);
        }

        [Test]
        public void StartingARunTakesTheMenuDownAndOpensTheFirstBuildPhase()
        {
            MatchRoot root = Playfield();
            MainMenu menu = OpenMenu(root);

            menu.StartRun();

            Assert.That(root.Loop, Is.Not.Null);
            Assert.That(root.Loop.Mode, Is.EqualTo(RunMode.Building));
            Assert.That(menu.IsShown, Is.False);
        }

        [Test]
        public void TheRunKeepsItsRoundsInTheLobbyFolder()
        {
            MatchRoot root = Playfield();
            MainMenu menu = OpenMenu(root);

            menu.StartRun();

            Assert.That(root.PoolDirectory, Is.EqualTo(Path.Combine(Scratch(), StreamingContent.PoolFolderName)));
        }

        [Test]
        public void SettingsOpenFromTheMenuAndCloseBackToIt()
        {
            MatchRoot root = Playfield();
            MainMenu menu = OpenMenu(root);

            menu.OpenSettings();

            Assert.That(menu.Settings.IsShown, Is.True);
            Assert.That(menu.IsShown, Is.False);

            menu.Settings.Close();

            Assert.That(menu.Settings.IsShown, Is.False);
            Assert.That(menu.IsShown, Is.True);
        }

        [UnityTest]
        public IEnumerator SettingsOpenedDuringARunHoldTheBoardStillAndCloseBackToTheMatch()
        {
            MatchRoot root = Playfield();
            MainMenu menu = OpenMenu(root);
            menu.StartRun();

            menu.OpenSettings();
            yield return null;

            Assert.That(root.Pointer.enabled, Is.False);

            menu.Settings.Close();
            yield return null;

            Assert.That(root.Pointer.enabled, Is.True);
            Assert.That(menu.IsShown, Is.False);
        }

        [UnityTest]
        public IEnumerator ANameTypedIntoSettingsIsKeptForTheLaunch()
        {
            MatchRoot root = Playfield();
            var launch = new LaunchSettings { LobbyFolder = Scratch() };
            MainMenu menu = root.OpenMenu(TheMatchOnScreen.Seed, TheMatchOnScreen.Art(), launch);

            menu.OpenSettings();
            menu.Settings.PlayerName.value = "Player one";
            menu.Settings.LobbyFolder.value = Path.Combine(Scratch(), "lobby");
            yield return null;

            Assert.That(launch.PlayerName, Is.EqualTo("Player one"));
            Assert.That(launch.LobbyFolder, Is.EqualTo(Path.Combine(Scratch(), "lobby")));
        }

        [Test]
        public void EverySettingShownSaysWhatHonouringItCosts()
        {
            MatchRoot root = Playfield();
            MainMenu menu = OpenMenu(root);

            menu.OpenSettings();

            foreach (VisualElement row in menu.Settings.Rows)
            {
                Label cost = row.Q<Label>(SettingsScreen.CostName);

                Assert.That(cost, Is.Not.Null, row.name + " has no cost.");
                Assert.That(cost.text, Is.Not.Empty, row.name + " has no cost.");
            }

            Assert.That(menu.Settings.Rows.Count, Is.GreaterThanOrEqualTo(2));
        }

        private MainMenu OpenMenu(MatchRoot root) =>
            root.OpenMenu(
                TheMatchOnScreen.Seed,
                TheMatchOnScreen.Art(),
                new LaunchSettings { LobbyFolder = Scratch() });
    }
}
