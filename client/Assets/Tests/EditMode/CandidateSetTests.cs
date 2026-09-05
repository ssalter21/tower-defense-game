using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The candidate set file, read and resolved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The committed sets are the test that matters.</b>
    /// <c>docs/roster-expansion-candidates.txt</c> names a model, up to two
    /// held props, a clip, sometimes an atlas and sometimes a thing standing
    /// beside it on each of its thirty-five lines, and
    /// <c>docs/roster-expansion-beside-candidates.txt</c> is ten more lines
    /// about that last field. All by path, and the whole point of both is that
    /// somebody can run the capture and look at the result. None of those references is checked
    /// by a compiler and none of them is checked by the build gate, so the day
    /// a model is moved out of <c>Assets/Art/Kaykit</c> the file silently stops
    /// naming it and the failure surfaces as a three-minute batchmode run that
    /// throws. Resolving the file here turns that into a red test in a suite
    /// that runs in seconds.
    /// </para>
    /// <para>
    /// <b>The renders themselves are not asserted and must not be.</b> Nothing
    /// compares the PNGs to anything; they are documentation for a person to
    /// look at, and a golden image of a candidate nobody has signed off yet
    /// would be pinning the proposal rather than testing it.
    /// </para>
    /// </remarks>
    public class CandidateSetTests
    {
        /// <summary>
        /// The set the roster expansion put up for approval, relative to the
        /// repository root.
        /// </summary>
        private const string CommittedSet = "docs/roster-expansion-candidates.txt";

        /// <summary>
        /// The set that asks how big a thing standing on the ground should be,
        /// relative to the repository root.
        /// </summary>
        private const string BesideSet = "docs/roster-expansion-beside-candidates.txt";

        /// <summary>
        /// How many lines that one carries: the pack's six Color8 bare trees
        /// beside the Druid, and the turret, the ammo crate, the statue and the
        /// font beside theirs.
        /// </summary>
        private const int BesideEntries = 10;

        /// <summary>
        /// How many lines it carries: the proposal's 31 assigned characters,
        /// plus the Marksman a second time holding the crossbow the proposal
        /// asks about instead of his rifle, plus the Ninja three more times
        /// for the three atlases beyond the one his model imports wearing.
        /// </summary>
        /// <remarks>
        /// Written out by hand, the way <see cref="RosterNamesTests"/> writes
        /// out the roster: the point is to catch a line that fell out of the
        /// file rather than to re-count the file against itself. Editing the
        /// set is expected and turns this red once; the way back to green is to
        /// change this number, which is a person saying the line was meant to
        /// go.
        /// </remarks>
        private const int CommittedEntries = 35;

        [Test]
        public void TheCommittedSetResolvesEveryModelPropAndClip()
        {
            IReadOnlyList<CandidateSet.Candidate> candidates = TheCommittedSet();

            Assert.That(candidates, Has.Count.EqualTo(CommittedEntries));

            foreach (CandidateSet.Candidate candidate in candidates)
            {
                Assert.That(candidate.Model, Is.Not.Null, candidate.Name + " has no model");
                Assert.That(candidate.Clip, Is.Not.Null, candidate.Name + " has no clip");
            }
        }

        /// <summary>
        /// A Large-rig character must take a Large-rig clip, and the set file's
        /// bank qualifier is the only thing that can say so.
        /// </summary>
        /// <remarks>
        /// <c>Walking_A</c> exists in both rigs. An unqualified name finds the
        /// Medium one, which drives a Large skeleton into a shape that reads as
        /// the model being wrong — the quiet failure this qualifier exists to
        /// prevent. Asserted on the committed set rather than on a fixture,
        /// because it is the committed set that would be wrong.
        /// </remarks>
        [Test]
        public void EveryLargeRigCandidateNamesALargeRigBank()
        {
            var largeRigged = new[]
            {
                "Barbarian_Large", "Skeleton_Golem", "BlackKnight", "FrostGolem", "Monstrosity",
            };

            IReadOnlyList<CandidateSet.Candidate> candidates = TheCommittedSet();

            foreach (string name in largeRigged)
            {
                // Absent rather than required. The set file exists to be edited
                // -- dropping a candidate is the expected way to answer "not
                // that one" -- so a name that has gone is not a failure. What
                // is a failure is one that is still there posed by a Medium
                // clip.
                CandidateSet.Candidate candidate = MaybeNamed(candidates, name);

                if (candidate == null)
                {
                    continue;
                }

                Assert.That(
                    candidate.ClipName,
                    Does.StartWith("Rig_Large_"),
                    name + " is on the Large rig and its clip must say which Large bank it is from");
            }
        }

        /// <summary>
        /// A line may name the atlas its body wears, and a line that does not
        /// resolves to none rather than to something.
        /// </summary>
        /// <remarks>
        /// The colour is the first of the three materials a tier is told apart
        /// by, and it is the one a picture of a model and two props cannot
        /// show: the character comes out wearing the atlas it imported with,
        /// which is the rung below it.
        /// </remarks>
        [Test]
        public void ALineMayNameTheAtlasItsBodyWears()
        {
            IReadOnlyList<CandidateSet.Candidate> candidates = TheCommittedSet();

            Assert.That(
                Named(candidates, "Knight").Texture,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Art/Kaykit/adventurers/knight_texture_alt_B.png")));

            Assert.That(Named(candidates, "Barbarian").Texture, Is.Null);
        }

        [Test]
        public void APropCarriesItsOwnTurn()
        {
            IReadOnlyList<CandidateSet.Candidate> candidates = TheCommittedSet();

            // Every weapon in these packs is authored for the right hand, so
            // the one bow in the off hand needs the half turn or it draws with
            // its string facing the target.
            Assert.That(Named(candidates, "Ranger").LeftHandTilt, Is.EqualTo(new Vector3(0f, 180f, 0f)));

            // And the staff hangs head-down out of the fist without its quarter
            // turn about Z.
            Assert.That(
                Named(candidates, "Skeleton_Mage").RightHandTilt,
                Is.EqualTo(new Vector3(0f, 0f, -90f)));

            Assert.That(Named(candidates, "Knight").RightHandTilt, Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// A line may name something that stands on the ground beside the
        /// tower, at a size of its own, and a line that does not resolves to
        /// none rather than to something.
        /// </summary>
        /// <remarks>
        /// <b>The size is the half that cannot be left out.</b> A held prop is
        /// authored beside the character that holds it and comes in at the
        /// right size; a Forest Nature tree is authored for a forest and comes
        /// in taller than the tower it is meant to stand next to. So the field
        /// carries a number as well as a path, and a line that names no number
        /// draws the prop as imported.
        /// </remarks>
        [Test]
        public void ALineMayNameWhatStandsBesideItAndHowBig()
        {
            IReadOnlyList<CandidateSet.Candidate> beside = TheBesideSet();

            Assert.That(beside, Has.Count.EqualTo(BesideEntries));

            foreach (CandidateSet.Candidate candidate in beside)
            {
                Assert.That(candidate.Beside, Is.Not.Null,
                    candidate.Name + " is in the beside set with nothing standing beside it");

                Assert.That(candidate.BesideScale, Is.GreaterThan(0f),
                    candidate.Name + " draws its beside prop at no size at all");
            }

            Assert.That(
                Named(beside, "Druid_Tree_1_C").Beside,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Art/Kaykit/forest-nature/Color8/Tree_Bare_1_C_Color8.fbx")),
                "the bare weirwood signed on 5 September 2026 is not in the set that photographs it");

            // Scenery off another pack takes a correction; a prop authored in
            // the character's own pack does not. Both halves, because a set
            // that scaled everything or nothing would say nothing about why the
            // number is per prop.
            Assert.That(Named(beside, "Druid_Tree_1_C").BesideScale, Is.LessThan(1f));
            Assert.That(Named(beside, "Engineer_Turret").BesideScale, Is.EqualTo(1f));

            // And the Engineer's turret is on his line in the roster set too,
            // because it is his tier-1 look rather than a separate question.
            Assert.That(Named(TheCommittedSet(), "Engineer").Beside, Is.Not.Null);

            Assert.That(Named(TheCommittedSet(), "Knight").Beside, Is.Null);
        }

        /// <summary>
        /// A file with several faults reports all of them, once, rather than
        /// the first one per run.
        /// </summary>
        /// <remarks>
        /// The capture takes minutes; a reader who has to fix one typo per run
        /// is a reader who stops using the tool. This is the behaviour that
        /// makes a thirty-two line file editable by hand.
        /// </remarks>
        [Test]
        public void EveryFaultInAFileIsNamedAtOnceAndNothingResolves()
        {
            string path = Path.Combine(Path.GetTempPath(), "candidate-set-faults.txt");

            File.WriteAllLines(
                path,
                new[]
                {
                    "# a comment, and a blank line, both skipped",
                    string.Empty,
                    // A name no import can ever produce, so this fixture does
                    // not quietly stop testing the day somebody adds a Ghost.
                    "tower Ghost Characters/__no-such-model__.fbx - - Idle_A",
                    "creep Knight Characters/Knight.fbx - - Walking_Z",
                    "sideways Knight Characters/Knight.fbx - - Idle_A",
                    "tower Knight Characters/Knight.fbx - Idle_A",
                    "tower Knight Characters/Knight.fbx - - Idle_A Kaykit/__no-such-atlas__.png",
                    "tower Knight Characters/Knight.fbx Weapons/sword_1handed.fbx@up - Idle_A",
                    "tower Knight Characters/Knight.fbx@0,90,0 - - Idle_A",
                    "tower Knight Characters/Knight.fbx - - Idle_A - Kaykit/__no-such-prop__.fbx",
                    "tower Knight Characters/Knight.fbx - - Idle_A - "
                    + "Kaykit/adventurers/turret_base.fbx*huge",
                    "tower Knight Characters/Knight.fbx - - Idle_A - "
                    + "Kaykit/adventurers/turret_base.fbx@0,90,0",
                    "creep Knight Characters/Knight.fbx - - Walking_A - "
                    + "Kaykit/adventurers/turret_base.fbx",
                });

            try
            {
                IOException thrown = Assert.Throws<IOException>(() => CandidateSet.Read(path));

                Assert.That(thrown.Message, Does.Contain("Characters/__no-such-model__.fbx"));
                Assert.That(thrown.Message, Does.Contain("Walking_Z"));
                Assert.That(thrown.Message, Does.Contain("'sideways'"));
                Assert.That(thrown.Message, Does.Contain("5 fields, not 6, 7 or 8"));
                Assert.That(thrown.Message, Does.Contain("Kaykit/__no-such-atlas__.png"));
                Assert.That(thrown.Message, Does.Contain("@0,180,0"));

                // A turn on the character rather than on a held prop is a
                // fault and not a thing quietly ignored.
                Assert.That(thrown.Message, Does.Contain("carries a turn"));

                // The beside column, whose three ways of being wrong all draw
                // silently if they are not refused: a prop that is not there, a
                // size that is not a number, and a turn on a thing standing on
                // the floor.
                Assert.That(thrown.Message, Does.Contain("Kaykit/__no-such-prop__.fbx"));
                Assert.That(thrown.Message, Does.Contain("the size after '*' is not a number"));

                // And says it once. A size that will not parse leaves nothing
                // behind to range-check, so a second line about the zero it did
                // not write is noise about a typo already named.
                Assert.That(thrown.Message, Does.Not.Contain("is drawn at 0"),
                    "one mistake reported twice, which is how a file of these stops being readable");

                // And a beside prop on a creep, which CreepView would draw
                // nothing at all for.
                Assert.That(thrown.Message, Does.Contain("stands beside a creep"));

                // Line numbers, because a fault in a file this long is only
                // actionable if it says which line.
                Assert.That(thrown.Message, Does.Contain(":3:"));
                Assert.That(thrown.Message, Does.Contain(":9:"));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void AMissingFileSaysSoRatherThanReadingNothing()
        {
            string path = Path.Combine(Path.GetTempPath(), "candidate-set-not-here.txt");

            Assert.That(
                Assert.Throws<IOException>(() => CandidateSet.Read(path)).Message,
                Does.Contain(path));
        }

        private static IReadOnlyList<CandidateSet.Candidate> TheCommittedSet() =>
            CandidateSet.Read(Path.Combine(RepositoryRoot(), CommittedSet));

        private static IReadOnlyList<CandidateSet.Candidate> TheBesideSet() =>
            CandidateSet.Read(Path.Combine(RepositoryRoot(), BesideSet));

        private static CandidateSet.Candidate Named(
            IReadOnlyList<CandidateSet.Candidate> candidates, string name) =>
            MaybeNamed(candidates, name)
            ?? throw new AssertionException(
                "The committed set has no candidate called " + name + ".");

        private static CandidateSet.Candidate MaybeNamed(
            IReadOnlyList<CandidateSet.Candidate> candidates, string name)
        {
            foreach (CandidateSet.Candidate candidate in candidates)
            {
                if (candidate.Name == name)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// The repository root, walked up from the Unity project. The same two
        /// steps <see cref="MatchContentTests"/> takes, for the same reason:
        /// <c>docs/</c> is a sibling of <c>client/</c>, not part of it.
        /// </summary>
        private static string RepositoryRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    }
}
