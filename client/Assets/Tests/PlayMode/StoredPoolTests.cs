using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The client reading a folder of stored rounds: what it takes out of one,
    /// what it refuses, and that a run it builds is fought against what it read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The folder is the whole of the multiplayer loop at this step.</b>
    /// Every run somebody plays adds its rounds to it and every run somebody
    /// plays draws its opponents out of it, with no service in the middle. What
    /// the client owes that arrangement is the reading, so this fixture writes
    /// records with the simulation's own writer and reads them back through the
    /// view's own reader.
    /// </para>
    /// <para>
    /// <b>Nothing here touches the folder a person's run draws from.</b> A
    /// fixture that filled it would score somebody's next run against its
    /// records, and one that cleared it would delete what
    /// <c>tools/seed-pool.ps1</c> put there. Every folder below is under this
    /// fixture's own scratch, which is what
    /// <see cref="MatchRoot.BeginRun(ulong, string, MatchArt)"/> points a run's
    /// pool at.
    /// </para>
    /// </remarks>
    public class StoredPoolTests : ViewTest
    {
        /// <summary>A wall on the corridor, and a second one that is not the first.</summary>
        private const string OneWall = "tower   3     4    3";

        private const string AnotherWall = "tower   3     14   3";

        private string _folder;

        [SetUp]
        public void OpenAScratchFolder()
        {
            _folder = Path.Combine(Scratch(), "read");

            Clear(Scratch());
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void CloseTheFolders() => Clear(Scratch());

        /// <summary>
        /// The pool sits beside the shipped content, because a build is a folder
        /// somebody unzipped and streaming assets are what Unity copies into
        /// one.
        /// </summary>
        [Test]
        public void ThePoolSitsBesideTheShippedContent()
        {
            Assert.That(
                StreamingContent.PoolDirectory,
                Is.EqualTo(Path.Combine(StreamingContent.Directory, StreamingContent.PoolFolderName)));

            // And that is where a root's runs draw from until a caller names a
            // folder of its own, which is what keeps a fixture's opponents out
            // of a person's pool and a person's out of a fixture's.
            Assert.That(Playfield().PoolDirectory, Is.EqualTo(StreamingContent.PoolDirectory));

            // A checkout that has never been played into reads an empty pool
            // rather than throwing, which is what every fresh clone does.
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            Assert.That(
                StreamingContent.ReadPool(Path.Combine(_folder, "never-written"), Map(), types).Count,
                Is.Zero);
        }

        /// <summary>
        /// Every readable round in the folder is taken, filed under the stage it
        /// was played at.
        /// </summary>
        [Test]
        public void EveryReadableRoundIsTakenAndFiledUnderItsOwnStage()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            Write(Stored(types, OneWall, stage: 1));
            Write(Stored(types, AnotherWall, stage: 1));
            Write(Stored(types, OneWall, stage: 3));

            StoredRounds pool = StreamingContent.ReadPool(_folder, Map(), types);

            Assert.That(pool.Refusals, Is.Empty);
            Assert.That(pool.Count, Is.EqualTo(3));
            Assert.That(pool.Stages, Is.EqualTo(3));
            Assert.That(pool.ByStage[0].Count, Is.EqualTo(2));
            Assert.That(pool.ByStage[1].Count, Is.Zero, "Nobody played a second round into this folder.");
            Assert.That(pool.ByStage[2].Count, Is.EqualTo(1));
        }

        /// <summary>
        /// A file the reader cannot use is named and skipped, and everything
        /// beside it is still read.
        /// </summary>
        /// <remarks>
        /// A folder accumulates for as long as anybody plays, so a record from a
        /// format that has since moved is the ordinary case in one. A client
        /// that refused to start a run over a file it could not read would be a
        /// client nobody could play on the day a format moved.
        /// </remarks>
        [Test]
        public void AFileTheReaderCannotUseIsNamedAndSkipped()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            byte[] good = Stored(types, OneWall, stage: 1);
            byte[] stale = (byte[])good.Clone();

            // The format version, moved to one no reader has a branch for.
            stale[4] = 9;

            Write(good);
            Write(stale);
            File.WriteAllBytes(
                Path.Combine(_folder, "0000000000000000" + StreamingContent.PoolFileExtension),
                new byte[] { 1, 2, 3, 4 });

            StoredRounds pool = StreamingContent.ReadPool(_folder, Map(), types);

            Assert.That(pool.Count, Is.EqualTo(1), "The good record was still read.");
            Assert.That(pool.Refusals.Count, Is.EqualTo(2));
            Assert.That(
                pool.Refusals[0],
                Does.Contain("0000000000000000"),
                "A refusal names the record it refused.");
        }

        /// <summary>
        /// A run the client builds meets the folder's rounds, and the one the
        /// player watches is one of them.
        /// </summary>
        /// <remarks>
        /// <b>This is the whole point of the folder.</b> The watched opponent is
        /// the first slot of the field, and the first slots are the stage's
        /// stored rounds -- so a player with a pool watches somebody's wall
        /// rather than the canned one. What the round is scored on is still the
        /// average over all K, of which the rest here are the stand-in.
        /// </remarks>
        [Test]
        public void ARunTheClientBuildsWatchesAnOpponentOutOfTheFolder()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            MatchRoot root = Playfield();

            root.Build(Map());

            // Into the folder BeginRun is about to point this root's pool at,
            // which is under this fixture's scratch and not a person's.
            Write(PoolUnderScratch(), Stored(types, OneWall, stage: 1));

            root.BeginRun(TheMatchOnScreen.Seed, Scratch(), TheMatchOnScreen.Art());

            Assert.That(root.PoolDirectory, Is.EqualTo(PoolUnderScratch()));

            Run run = root.RunOn(TheMatchOnScreen.Seed);
            RoundReport report = run.Advance(BuildPhase.Of());

            Assert.That(report.Field.Drawn[RunLoop.WatchedOpponent], Is.Zero, "The stage's one stored round.");
            Assert.That(report.Field.Canned, Is.EqualTo(run.FieldSize - 1));

            // And the match the results screen would draw is against that wall
            // rather than against the canned field's six.
            Match watched = run.MatchAt(0, RunLoop.WatchedOpponent, attacking: true);

            Assert.That(
                watched.PullSnapshot().Towers.Count,
                Is.EqualTo(1),
                "One tower is what the stored round stood, and six is what the canned field stands.");
        }

        /// <summary>
        /// A run the client played puts its own rounds back into the pool, and
        /// they read back as opponents.
        /// </summary>
        /// <remarks>
        /// <b>This is the half that closes the loop.</b> A run draws its
        /// opponents out of a folder and adds its own rounds to it, so the
        /// population grows by being played and no service is in the middle. A
        /// round that stood no wall is not stored and says so, which is why the
        /// count below is not the round count.
        /// </remarks>
        [Test]
        public void ARunTheClientPlayedPutsItsOwnRoundsBackIntoThePool()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            MatchRoot root = Playfield();

            root.Build(Map());

            Run run = root.RunOn(TheMatchOnScreen.Seed);

            // One round that builds nothing and sends nothing, and one that
            // does both: a stored round is a wall and a wave, so only the
            // second of them can be filed.
            run.Advance(BuildPhase.Of());
            run.Advance(BuildPhase
                .Of(WaveSlot.Of(TheFirstCreep(types).Id, 1))
                .With(BuildAction.Of(ActionKind.Place, TheFirstTower(types).Id, 4, 3)));

            IReadOnlyList<string> said = WrittenRun.Stored(run, Map(), types, _folder);

            Assert.That(said.Count, Is.EqualTo(2), "Every round is accounted for, stored or not.");
            Assert.That(said[0], Does.StartWith("Not stored"), "Round one stood no wall.");
            Assert.That(said[1], Does.StartWith("Stored"));

            StoredRounds read = StreamingContent.ReadPool(_folder, Map(), types);

            Assert.That(read.Refusals, Is.Empty, "What the client wrote, the client reads.");
            Assert.That(read.Count, Is.EqualTo(1));
            Assert.That(read.ByStage[1].Count, Is.EqualTo(1), "Filed under the stage it was played at.");
        }

        /// <summary>Where a fixture's own runs draw their opponents from.</summary>
        private string PoolUnderScratch() =>
            Path.Combine(Scratch(), StreamingContent.PoolFolderName);

        /// <summary>The roster's first walker, which is what the round above sends.</summary>
        private static UnitType TheFirstCreep(UnitTypeTable types) => First(types, UnitRole.Moving);

        /// <summary>The roster's first placeable, which is what it builds.</summary>
        private static UnitType TheFirstTower(UnitTypeTable types) => First(types, UnitRole.Placed);

        /// <summary>The first row of the roster in this role.</summary>
        private static UnitType First(UnitTypeTable types, UnitRole role)
        {
            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Role == role)
                {
                    return types.Types[index];
                }
            }

            throw new InvalidOperationException("The shipped roster has no " + role + " row in it.");
        }

        /// <summary>One stored round: a wall of this shape and the canned wave, at a stage.</summary>
        private static byte[] Stored(UnitTypeTable types, string wall, int stage) =>
            RoundRecord
                .Of(
                    Map(),
                    TowerLayout.Parse("wall", wall, types),
                    StreamingContent.ReadField(types),
                    types,
                    stage,
                    GhostRecord.NoMapHandle)
                .ToBytes();

        /// <summary>The shipped board, which is what a stored round is pinned to.</summary>
        private static HexMap Map() => StreamingContent.ReadMap();

        /// <summary>The record, in the scratch folder, under the name the reader insists on.</summary>
        private void Write(byte[] bytes) => Write(_folder, bytes);

        /// <summary>The same, in a folder the caller names.</summary>
        private static void Write(string folder, byte[] bytes)
        {
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(
                Path.Combine(folder, StoredRounds.NameOf(bytes) + StreamingContent.PoolFileExtension),
                bytes);
        }

        /// <summary>The folder and everything in it, gone.</summary>
        private static void Clear(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
