using System;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The parity run: one match played inside the engine, with the renderer
    /// attached, producing the command-line runner's trace tick for tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two claims in one run, and neither is provable without the other.</b>
    /// The first is that the architecture survives the engine's runtime — the
    /// same fixed-point arithmetic, the same dice, the same tie-breaks, under
    /// Unity's Mono rather than under the command line's .NET. The second is
    /// that the view has no back-channel into the simulation: a whole match is
    /// drawn beside it, and the numbers underneath come out unchanged.
    /// </para>
    /// <para>
    /// <b>The renderer is attached and the view is posed on every tick.</b> A
    /// run that stepped the simulation without drawing would prove the first
    /// claim and nothing at all about the second, which is why
    /// <see cref="PlayAndDiff"/> goes through <see cref="ViewTest.RunUntil"/> —
    /// which calls <see cref="MatchView.Draw"/> per tick — rather than stepping
    /// the match and drawing once at the end. The runner behind this
    /// (<c>tools/run-unity-tests.ps1</c>) deliberately does not pass
    /// <c>-nographics</c> for the same reason.
    /// </para>
    /// <para>
    /// <b>What that does and does not mean, said exactly.</b> This is a
    /// <c>[Test]</c> rather than a <c>[UnityTest]</c>, like every other
    /// full-match fixture here, so all 1,852 ticks happen inside one editor
    /// frame: the graphics device is attached, every view object is built, and
    /// every creep, tower and projectile is posed for every tick — but no
    /// camera frame is presented per tick. That is the right trade, because
    /// what the back-channel claim is about is the view <i>computing</i>
    /// against the simulation, and a presented frame adds no reads of it.
    /// </para>
    /// <para>
    /// <b>What it is diffed against is the committed trace, and that is the
    /// command line's own output.</b> <c>content/golden-trace.txt</c> is
    /// written by <c>tools/run-headless-match.ps1 -Regenerate</c> and re-checked
    /// against a fresh command-line run by the build gate on every push, so it
    /// cannot quietly stop being what the runner produces. Shelling out to
    /// <c>dotnet</c> from inside a batch-mode editor would buy nothing over
    /// that and would make this tier depend on a toolchain the engine does not
    /// have.
    /// </para>
    /// <para>
    /// <b>The match is the record's, not the fixture's.</b> Every other test in
    /// this folder watches <see cref="TheMatchOnScreen.Seed"/>; this one reads
    /// <c>content/match.replay</c> — the same bytes the command line replayed —
    /// and takes the seed, the map, the defense and the wave out of it. A seed
    /// written down here would be a second copy of a number that lives in the
    /// record, and the day somebody re-recorded with a different one this would
    /// go red for a reason that had nothing to do with parity.
    /// </para>
    /// <para>
    /// <b>Needs the repository on disk</b>, and it is the only fixture here that
    /// does. The art comes through <see cref="MatchArtSource"/> like everything
    /// else, but the trace and the record are read out of <c>content/</c>
    /// relative to the project — deliberately not shipped through streaming
    /// assets, because a test oracle is not content a player reads. Run
    /// somewhere without a checkout, these two skip themselves by name rather
    /// than failing on a path that was never going to be there.
    /// </para>
    /// </remarks>
    public class ParityRunTests : ViewTest
    {
        /// <summary>The bolt tower, whose damage the poison changes.</summary>
        private const int BoltTowerTypeId = 3;

        /// <summary>Where <c>dmgMax</c> sits in a <c>unit</c> row.</summary>
        private const int DamageMaxField = 11;

        // ---------------------------------------------------------------
        // The parity run
        // ---------------------------------------------------------------

        /// <summary>
        /// A whole match, drawn, inside the engine — and every tick of it has
        /// the state hash the command-line runner recorded for that tick.
        /// </summary>
        [Test]
        public void TheEngineProducesTheCommandLineRunnersTraceTickForTick()
        {
            GoldenTrace trace = TheCommandLineTrace();
            ReplayBundle record = TheRecordedMatch();
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            // The replay gate, run here rather than assumed: the simulation
            // version, the content hash and the map hash all have to be this
            // build's or the record is refused by name. The match it returns is
            // dropped -- the view builds its own, because a view that cannot
            // build a match cannot seek -- so what this line buys is the
            // refusal, and the refusal is the point.
            record.Replay(types);

            MatchView view = BeginTheRecordedMatch(record, types);

            int finalTick = PlayAndDiff(view, trace);

            // The one assertion that can catch a run which stopped early, and
            // deliberately the only one: asking whether the view is finished
            // afterwards could not fail, because being finished is what ends
            // the loop. A check that can never fail would go green through
            // every regression it was put there to catch.
            Assert.That(finalTick, Is.EqualTo(trace.FinalTick),
                $"the engine finished on tick {finalTick} and the command line finished on "
                + $"{trace.FinalTick}. Every tick they both have agrees, so this is not a divergence "
                + "in the arithmetic -- it is one run stopping somewhere the other did not.");
        }

        // ---------------------------------------------------------------
        // The poison
        // ---------------------------------------------------------------

        /// <summary>
        /// The permanent positive control: one number of the view's own reaches
        /// the simulation, and the diff catches it, naming the tick.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The poison is not a write, because there is no write to make.</b>
        /// The intended shape of this control was a view-side write into
        /// simulation state, and the seam does not admit one: nothing public in
        /// the simulation assembly has a setter, every snapshot is a readonly
        /// struct behind an <see cref="System.Collections.Generic.IReadOnlyList{T}"/>,
        /// and the map, the defense, the wave and the type table are all
        /// immutable once parsed. A test that wrote into simulation state would
        /// not compile, which is the strongest form of the claim and the reason
        /// this control had to be built out of the one thing the view genuinely
        /// does decide: what it hands the simulation when it builds a match.
        /// </para>
        /// <para>
        /// <b>So the poison is a number.</b> The bolt tower's top damage roll
        /// goes up by one, in the table the view hands over, and nothing else
        /// changes — the same record, the same seed, the same map, the same
        /// defense, the same wave. That is the realistic shape of the failure
        /// this tier exists to catch: a value the view had for its own reasons
        /// leaking into the run. The two runs stay identical until the first
        /// bolt actually fires, a handful of ticks in — the wave releases into
        /// a corridor the bolt towers already cover, so the dice are consulted
        /// almost immediately. The ticks before it agree, and that is what
        /// makes the tick the diff names a fact about the poison rather than
        /// about the setup.
        /// </para>
        /// <para>
        /// <b>The tick it is caught on is logged rather than asserted.</b>
        /// Which tick the first shot lands on is a fact about the content, and
        /// a content change moves it legitimately — the committed trace is
        /// regenerated when that happens. A number written down here would go
        /// stale quietly, so what is asserted is the property that has to hold
        /// (the runs agreed until the poison could take effect, and then did
        /// not) and the tick itself is printed for whoever is reading the log.
        /// </para>
        /// <para>
        /// <b>It stays, rather than being introduced and removed.</b> A poison
        /// that was run once and deleted proves the diff could fail on the
        /// afternoon somebody watched it. This one proves it on every run, and
        /// it is the row that still knows what the parity run was for on the
        /// day somebody makes the comparison vacuous.
        /// </para>
        /// </remarks>
        [Test]
        public void ANumberOfTheViewsOwnReachingTheSimulationIsCaughtByTheDiff()
        {
            GoldenTrace trace = TheCommandLineTrace();
            ReplayBundle record = TheRecordedMatch();

            UnitTypeTable shipped = StreamingContent.ReadUnitTypes();
            UnitTypeTable poisoned = UnitTypeTable.Parse(
                StreamingContent.UnitsFileName, TheUnitTableWithOneNumberChanged());

            // A poison that quietly stopped poisoning would leave a test that
            // passes whatever the diff does, which is the exact species of
            // green this file exists to rule out. Asserted against the parsed
            // table rather than the text, so this is also what pins the column
            // index: the day a field is inserted into a unit row, the edit
            // lands somewhere other than the top damage roll and this fires
            // instead of the poison silently changing a different number.
            Assert.That(
                poisoned.ById(BoltTowerTypeId).DamageMax,
                Is.Not.EqualTo(shipped.ById(BoltTowerTypeId).DamageMax),
                $"editing field {DamageMaxField} of the bolt row left its top damage roll alone, so "
                + "either the poison changed nothing or the unit table's columns have moved under it");

            MatchView view = BeginTheRecordedMatch(record, poisoned);

            DesyncException desync = Assert.Throws<DesyncException>(
                () => PlayAndDiff(view, trace),
                "a match played with a damage number the record never carried produced the record's "
                + "own trace, so the comparison is not comparing anything");

            Assert.That(desync.Tick, Is.GreaterThan(0),
                "the two runs differed on tick zero, before either had advanced at all, so what this "
                + "caught is a difference in how the match was set up rather than the changed number "
                + "taking effect as it ran");

            Debug.Log(
                $"[poison] the diff caught one changed damage number at tick {desync.Tick}: "
                + $"the trace says {desync.Expected} and the poisoned run says {desync.Actual}");
        }

        // ---------------------------------------------------------------
        // The comparison itself
        // ---------------------------------------------------------------

        /// <summary>
        /// Plays <paramref name="view"/> to the end, drawing every tick, and
        /// checks each tick's state hash against <paramref name="trace"/>.
        /// Returns the tick it finished on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Shared by the parity run and its poison on purpose: a positive
        /// control that went through a second copy of this loop would be
        /// evidence about the copy. For the same reason the stepping and
        /// drawing is <see cref="ViewTest.RunUntil"/> rather than a loop
        /// written again here — that one already steps, draws every tick and
        /// stops at the end of the match, and a second copy of it would be the
        /// thing this method's own argument is against.
        /// </para>
        /// <para>
        /// The comparison is <see cref="GoldenTrace.Check"/>, which throws
        /// naming the tick, so the run stops on the first tick that differs
        /// rather than at the end — before the difference has had the rest of
        /// the match to contaminate everything downstream. A run that went
        /// <i>past</i> the trace's last tick is refused by
        /// <see cref="GoldenTrace.At"/>, whose message names the range.
        /// </para>
        /// </remarks>
        private static int PlayAndDiff(MatchView view, GoldenTrace trace)
        {
            // Tick zero, before anything has been advanced. The trace starts
            // there, so the comparison has to as well.
            trace.Check(view.Match.Tick, view.Match.StateHash);

            RunUntil(view, () =>
            {
                trace.Check(view.Match.Tick, view.Match.StateHash);

                // Never stops early: the parity claim is about every tick of
                // the match, so the only thing that ends this run is the match
                // ending, or a tick that disagrees throwing out of the check
                // above.
                return false;
            });

            return view.Match.Tick;
        }

        // ---------------------------------------------------------------
        // What the run is made of
        // ---------------------------------------------------------------

        /// <summary>
        /// The recorded match, drawn with the real art, on the type table the
        /// caller hands in.
        /// </summary>
        /// <remarks>
        /// The type table is the argument because it is the one the poison
        /// substitutes. Everything else comes off the record's own bytes, so
        /// the poisoned run and the honest one differ in exactly one thing.
        /// </remarks>
        private MatchView BeginTheRecordedMatch(ReplayBundle record, UnitTypeTable types) =>
            TheMatchOnScreen.Begin(
                Spawn(GetType().Name),
                record.Map,
                types,
                record.Ghost.ToLayout(types),
                record.Wave.ToScript(types),
                record.Seed);

        /// <summary>The bytes the command line replayed.</summary>
        private static ReplayBundle TheRecordedMatch() =>
            ReplayBundle.FromBytes("content/match.replay", InTheRepository("match.replay"));

        /// <summary>The trace a real command-line run of those bytes produced.</summary>
        private static GoldenTrace TheCommandLineTrace() =>
            GoldenTrace.ParseUtf8("content/golden-trace.txt", InTheRepository("golden-trace.txt"));

        /// <summary>
        /// One file out of the repository's authored <c>content/</c>, rather
        /// than out of the copy beside the player.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Deliberately not shipped through <c>StreamingAssets</c>. The trace
        /// and the record are a test oracle, not content a player reads, and
        /// putting fifty kilobytes of hashes in the build to be read by nothing
        /// would be paying for the wrong thing.
        /// </para>
        /// <para>
        /// <b>Skipped rather than failed where there is no repository</b> — a
        /// player built out of this project has no path back to the checkout it
        /// came from, and never did. Ignoring by name says that; a red assertion
        /// about a missing file would send somebody looking for a bug in the
        /// parity run. The skip is on <c>Application.isEditor</c> and not on
        /// whether the file happens to be there, because "the oracle is missing"
        /// inside a checkout is a real failure and has to stay one.
        /// </para>
        /// <para>
        /// The simulation is handed bytes and never a path, here as everywhere:
        /// opening the file is this side's job and parsing it is the
        /// simulation's.
        /// </para>
        /// </remarks>
        private static byte[] InTheRepository(string fileName)
        {
            if (!Application.isEditor)
            {
                Assert.Ignore(
                    "the parity oracle lives in the repository's content/ folder, and a player has no "
                    + "path back to the checkout it was built from");
            }

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string path = Path.Combine(root, "content", fileName);

            Assert.That(File.Exists(path), Is.True,
                $"nothing at {path}. The parity run diffs against what the command-line runner "
                + "produced, so it needs the repository's own content -- regenerate it with "
                + "tools/run-headless-match.ps1 -Regenerate.");

            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// The shipped unit table with the bolt tower's top damage roll one
        /// higher, and nothing else touched.
        /// </summary>
        /// <remarks>
        /// Read off the shipped text and edited by field rather than written
        /// out here, so the poisoned table cannot silently become a table that
        /// differs from the real one in some second way nobody intended.
        /// </remarks>
        private static string TheUnitTableWithOneNumberChanged()
        {
            string text = Encoding.UTF8.GetString(
                StreamingContent.Read(StreamingContent.UnitsFileName));

            var rebuilt = new StringBuilder();

            foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
            {
                string[] fields = line.Split(
                    new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                bool isTheBoltRow =
                    fields.Length > DamageMaxField
                    && string.Equals(fields[0], "unit", StringComparison.Ordinal)
                    && string.Equals(
                        fields[1],
                        BoltTowerTypeId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal);

                if (isTheBoltRow)
                {
                    int was = int.Parse(fields[DamageMaxField], CultureInfo.InvariantCulture);
                    fields[DamageMaxField] = (was + 1).ToString(CultureInfo.InvariantCulture);

                    rebuilt.Append(string.Join(" ", fields)).Append('\n');
                }
                else
                {
                    rebuilt.Append(line).Append('\n');
                }
            }

            return rebuilt.ToString();
        }
    }
}
