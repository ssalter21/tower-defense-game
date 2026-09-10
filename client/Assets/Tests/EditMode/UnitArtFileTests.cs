using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The candidate art file, and the beside-prop group grammar it shares with
    /// the candidate set file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What is worth asserting here is that a candidate that moved nothing
    /// is refused.</b> The whole failure this reader exists to avoid is a
    /// three-minute batchmode run that exits 0 and writes a picture of the
    /// SHIPPED look under a candidate's filename — two frames that came out
    /// identical because both files were misspelt is a picture answering a
    /// question nobody asked, and this repository has been bitten by that
    /// species twice on this map alone. So every reference resolves or the read
    /// throws, an unknown directive is a fault rather than a shrug, and a file
    /// with no directive at all is refused outright.
    /// </para>
    /// <para>
    /// <b>The renders themselves are not asserted and must not be.</b> Nothing
    /// compares the PNGs to anything; they are documentation for a person to
    /// look at, and a golden image of a candidate nobody has signed off yet
    /// would be pinning the proposal rather than testing it. That is
    /// <see cref="CandidateSetTests"/>'s rule and it is this one's.
    /// </para>
    /// </remarks>
    public class UnitArtFileTests
    {
        /// <summary>The Mortar, whose turret the committed candidates resize.</summary>
        private const int Mortar = 37;

        /// <summary>The Bishop, whose tome the committed candidates move.</summary>
        private const int Bishop = 24;

        /// <summary>A path under <c>Assets/Art</c> that every candidate file names.</summary>
        private const string Turret = "Kaykit/adventurers/turret_base.fbx";

        /// <summary>The crate that stands beside it in the group candidates.</summary>
        private const string Crate = "Kaykit/adventurers/ammo_crate.fbx";

        [Test]
        public void EveryCommittedCandidateResolvesEveryReference()
        {
            foreach (string path in CommittedCandidates())
            {
                UnitArtFile candidate = UnitArtFile.Read(path);

                Assert.That(
                    candidate.Changes, Is.Not.Empty, Path.GetFileName(path) + " moves nothing");
                Assert.That(
                    candidate.Label,
                    Is.Not.Null.And.Not.Empty,
                    Path.GetFileName(path) + " has no label, so a sheet cannot say what it is");
                Assert.That(
                    candidate.Question,
                    Is.Not.Null.And.Not.Empty,
                    Path.GetFileName(path) + " names no question, so a frame of it says nothing");
            }
        }

        /// <summary>
        /// A file that names no directive is refused rather than read as an
        /// empty candidate.
        /// </summary>
        /// <remarks>
        /// The loud half of the reason this class exists: an empty candidate
        /// applied to the wired art hands back the wired art, and the capture
        /// then writes the shipped look under the candidate's filename.
        /// </remarks>
        [Test]
        public void AFileThatMovesNothingIsRefused()
        {
            string path = TempFile(
                "# a candidate that forgot to say anything",
                "label     nothing at all",
                "question  what if nothing moved");

            IOException thrown = Assert.Throws<IOException>(() => UnitArtFile.Read(path));

            Assert.That(thrown.Message, Does.Contain("moves nothing"));
        }

        [Test]
        public void AnUnknownDirectiveIsAFaultAndNotAShrug()
        {
            string path = TempFile("bezide 37 " + Turret);

            IOException thrown = Assert.Throws<IOException>(() => UnitArtFile.Read(path));

            Assert.That(thrown.Message, Does.Contain("bezide"));
            Assert.That(thrown.Message, Does.Contain(UnitArtFile.BesideDirective));
        }

        [Test]
        public void APathThatNamesNothingOnDiskIsAFault()
        {
            string path = TempFile("beside 37 Kaykit/adventurers/turret_that_is_not_there.fbx");

            IOException thrown = Assert.Throws<IOException>(() => UnitArtFile.Read(path));

            Assert.That(thrown.Message, Does.Contain("nothing imported at"));
        }

        [Test]
        public void EveryFaultInAFileIsReportedAtOnce()
        {
            string path = TempFile(
                "beside 37 Kaykit/adventurers/not_a_turret.fbx",
                "atlas  37 Kaykit/adventurers/not_an_atlas.png",
                "hand   37 sideways " + Turret);

            IOException thrown = Assert.Throws<IOException>(() => UnitArtFile.Read(path));

            Assert.That(thrown.Message, Does.Contain("3 fault(s)"));
        }

        /// <summary>
        /// A group of one is the prop itself, so every set file written before
        /// groups existed draws exactly what it drew.
        /// </summary>
        /// <remarks>
        /// A wrapper would put an extra transform under the tower root for no
        /// picture anybody asked for, and the six committed sheets would stop
        /// being comparable with the ones taken after it.
        /// </remarks>
        [Test]
        public void OneNamedPropIsThePropItselfAndCarriesItsOwnSize()
        {
            var faults = new List<string>();

            GameObject drawn = BesideGroup.Parse(
                "test:1", Turret + "*1.5", faults, out float scale);

            Assert.That(faults, Is.Empty);
            Assert.That(scale, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(drawn.name, Is.Not.EqualTo(BesideGroup.CompositeName));
        }

        [Test]
        public void TwoPropsJoinedStandUnderOneRootAtTheOffsetsTheFileGave()
        {
            var faults = new List<string>();

            GameObject drawn = BesideGroup.Parse(
                "test:1", Turret + "*1~0,0+" + Crate + "*1~0.65,0.35", faults, out float scale);

            Assert.That(faults, Is.Empty);
            Assert.That(drawn.name, Is.EqualTo(BesideGroup.CompositeName));
            Assert.That(drawn.transform.childCount, Is.EqualTo(2));

            // The group is drawn at 1 and its members carry their own sizes: a
            // size on the group would multiply into every member and stop being
            // the number the file wrote against one.
            Assert.That(scale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(drawn.transform.GetChild(0).localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(drawn.transform.GetChild(1).localPosition.x, Is.EqualTo(0.65f).Within(0.001f));
            Assert.That(drawn.transform.GetChild(1).localPosition.z, Is.EqualTo(0.35f).Within(0.001f));

            Object.DestroyImmediate(drawn);
        }

        /// <summary>
        /// Two props at one offset is one prop inside another, which reads as a
        /// broken import rather than as a candidate.
        /// </summary>
        [Test]
        public void TwoPropsInTheSamePlaceAreRefused()
        {
            var faults = new List<string>();

            GameObject drawn = BesideGroup.Parse(
                "test:1", Turret + "+" + Crate, faults, out _);

            Assert.That(drawn, Is.Null);
            Assert.That(faults, Has.Count.EqualTo(1));
            Assert.That(faults[0], Does.Contain("stand in the same place"));
        }

        /// <summary>
        /// An offset moves a member within a group; a lone prop stands where
        /// the socket puts it, so one carrying an offset is a file saying
        /// something the view will not do.
        /// </summary>
        [Test]
        public void ALonePropCarryingAnOffsetIsRefused()
        {
            var faults = new List<string>();

            GameObject drawn = BesideGroup.Parse("test:1", Turret + "~1,0", faults, out _);

            Assert.That(drawn, Is.Null);
            Assert.That(faults, Has.Count.EqualTo(1));
            Assert.That(faults[0], Does.Contain("stands where the socket puts it"));
        }

        /// <summary>
        /// A candidate naming a row the wired art does not carry is a mistyped
        /// unit id, and a candidate that moved nothing draws the shipped look
        /// under its own name.
        /// </summary>
        [Test]
        public void ARowTheArtDoesNotCarryIsRefusedWhenApplied()
        {
            string path = TempFile("beside 9999 " + Turret);
            UnitArtFile candidate = UnitArtFile.Read(path);

            IOException thrown = Assert.Throws<IOException>(
                () => candidate.Applied(OneRow(Mortar)));

            Assert.That(thrown.Message, Does.Contain("9999"));
        }

        /// <summary>
        /// Two directives for the same row fold into one moved row, so a file
        /// may spell a prop and an anchor on two lines.
        /// </summary>
        [Test]
        public void TwoDirectivesForOneRowFoldIntoOneChange()
        {
            string path = TempFile(
                "hand   " + Bishop + " left Kaykit/mystery-monthly-series-6/cleric/Cleric_Tome.fbx",
                "anchor " + Bishop + " Cleric_Tome");

            UnitArtFile candidate = UnitArtFile.Read(path);

            Assert.That(candidate.Changes, Has.Count.EqualTo(1));
            Assert.That(candidate.Changes[0].UnitId, Is.EqualTo(Bishop));
            Assert.That(candidate.Changes[0].LeftHand, Is.Not.Null);
            Assert.That(candidate.Changes[0].Anchor, Is.Not.Null);
        }

        /// <summary>
        /// A row the candidate does not name is handed back untouched, so a
        /// frame of one candidate is a frame of the shipped board with one
        /// thing moved on it.
        /// </summary>
        [Test]
        public void ARowTheCandidateDoesNotNameIsUntouched()
        {
            string path = TempFile("beside " + Mortar + " " + Turret + "*1.5");
            UnitArtFile candidate = UnitArtFile.Read(path);

            MatchArt wired = OneRow(Mortar);
            MatchArt moved = candidate.Applied(wired);

            Assert.That(moved.Units, Has.Count.EqualTo(wired.Units.Count));
            Assert.That(
                moved.ArtFor(Mortar).Beside.Scale, Is.EqualTo(1.5f).Within(0.0001f));
        }

        /// <summary>Every candidate art file the branch commits, by absolute path.</summary>
        private static IEnumerable<string> CommittedCandidates()
        {
            string folder = Path.Combine(RepositoryRoot(), "docs", "frames", "rung-candidates");

            Assert.That(
                Directory.Exists(folder),
                Is.True,
                "No candidate art at " + folder + ". The four rungs of issue #281 are drawn from it.");

            string[] files = Directory.GetFiles(folder, "*.txt");

            Assert.That(files, Is.Not.Empty, "No candidate art files in " + folder);

            return files;
        }

        /// <summary>
        /// A one-row bundle to apply a candidate to, so a test about the reader
        /// does not need the whole scene builder's table.
        /// </summary>
        /// <remarks>
        /// The two shared clips are empty rather than null because
        /// <see cref="MatchArt.CreepWalkClip"/> throws by name on a null, and
        /// <see cref="UnitArtFile.Applied"/> reads both to carry them onto the
        /// copy it hands back.
        /// </remarks>
        private static MatchArt OneRow(int unitId) =>
            MatchArt.Of(
                new[] { UnitArt.Of(unitId, new GameObject("body"), 1f) },
                new AnimationClip(),
                new AnimationClip());

        /// <summary>A candidate file written for one test and deleted with the run.</summary>
        private static string TempFile(params string[] lines)
        {
            string path = Path.Combine(
                Path.GetTempPath(), "unit-art-" + Path.GetRandomFileName() + ".txt");

            File.WriteAllLines(path, lines);

            return path;
        }

        private static string RepositoryRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    }
}
