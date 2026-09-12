using System.IO;
using NUnit.Framework;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The candidate set file, read and refused.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No committed set is read here.</b> The sets a sitting draws its
    /// sheets from are written for that sitting and removed with it, so what
    /// is left to test is the reader: that a file with several things wrong
    /// names every one of them at once, and that a file that is not there
    /// says so. The fixtures are written by the tests.
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

                    // Appended, so that the line numbers asserted below do not
                    // move every time this fixture grows.
                    "tower Knight Characters/Knight.fbx!__no-such-node__ - - Idle_A",
                    "tower Knight Characters/Knight.fbx Weapons/sword_1handed.fbx!blade - Idle_A",
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

                // A part the body does not have is a fault, and it lists
                // what the body does have -- because these node names are the
                // pack's rather than this project's and nothing else prints
                // them, so "no such child" on its own would leave a reader
                // with nowhere to go.
                Assert.That(thrown.Message, Does.Contain("__no-such-node__"));
                Assert.That(thrown.Message, Does.Contain("It carries: "));

                // And a part left out of a held prop, which is refused for the
                // reason a turn on a character is: a hand holds a whole prop,
                // so there is no part of one to leave behind.
                Assert.That(thrown.Message, Does.Contain("leaves a part out"));

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
    }
}
