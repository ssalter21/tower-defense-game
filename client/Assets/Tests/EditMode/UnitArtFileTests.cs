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

        /// <summary>A candidate file written for one test and deleted with the run.</summary>
        private static string TempFile(params string[] lines)
        {
            string path = Path.Combine(
                Path.GetTempPath(), "unit-art-" + Path.GetRandomFileName() + ".txt");

            File.WriteAllLines(path, lines);

            return path;
        }
    }
}
