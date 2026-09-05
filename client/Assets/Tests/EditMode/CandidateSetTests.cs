using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The candidate set file, read and resolved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The committed set is the test that matters.</b>
    /// <c>docs/roster-expansion-candidates.txt</c> names a model, up to two
    /// props and a clip on each of its thirty-two lines, all by path, and the
    /// whole point of it is that somebody can run the capture and look at the
    /// result. None of those references is checked
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
        /// How many lines it carries: the proposal's 31 assigned characters,
        /// plus the Marksman a second time holding the crossbow the proposal
        /// asks about instead of his rifle.
        /// </summary>
        private const int CommittedEntries = 32;

        [Test]
        public void TheCommittedSetResolvesEveryModelPropAndClip()
        {
            IReadOnlyList<CandidateSet.Candidate> candidates =
                CandidateSet.Read(Path.Combine(RepositoryRoot(), CommittedSet));

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

            IReadOnlyList<CandidateSet.Candidate> candidates =
                CandidateSet.Read(Path.Combine(RepositoryRoot(), CommittedSet));

            foreach (string name in largeRigged)
            {
                CandidateSet.Candidate candidate = Named(candidates, name);

                Assert.That(
                    candidate.ClipName,
                    Does.StartWith("Rig_Large_"),
                    name + " is on the Large rig and its clip must say which Large bank it is from");
            }
        }

        [Test]
        public void APropCarriesItsOwnTurn()
        {
            IReadOnlyList<CandidateSet.Candidate> candidates =
                CandidateSet.Read(Path.Combine(RepositoryRoot(), CommittedSet));

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
                    "tower Ghost Kaykit/adventurers/Ghost.fbx - - Idle_A",
                    "creep Knight Characters/Knight.fbx - - Walking_Z",
                    "sideways Knight Characters/Knight.fbx - - Idle_A",
                    "tower Knight Characters/Knight.fbx - Idle_A",
                    "tower Knight Characters/Knight.fbx Weapons/sword_1handed.fbx@up - Idle_A",
                });

            try
            {
                IOException thrown = Assert.Throws<IOException>(() => CandidateSet.Read(path));

                Assert.That(thrown.Message, Does.Contain("Kaykit/adventurers/Ghost.fbx"));
                Assert.That(thrown.Message, Does.Contain("Walking_Z"));
                Assert.That(thrown.Message, Does.Contain("'sideways'"));
                Assert.That(thrown.Message, Does.Contain("5 fields, not 6"));
                Assert.That(thrown.Message, Does.Contain("@0,180,0"));

                // Line numbers, because a fault in a thirty-two line file is
                // only actionable if it says which line.
                Assert.That(thrown.Message, Does.Contain(":3:"));
                Assert.That(thrown.Message, Does.Contain(":7:"));
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

        private static CandidateSet.Candidate Named(
            IReadOnlyList<CandidateSet.Candidate> candidates, string name)
        {
            foreach (CandidateSet.Candidate candidate in candidates)
            {
                if (candidate.Name == name)
                {
                    return candidate;
                }
            }

            throw new AssertionException("The committed set has no candidate called " + name + ".");
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
