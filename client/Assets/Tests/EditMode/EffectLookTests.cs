using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// The candidate look and the file it is read from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What is worth asserting here is that the shipped picture did not
    /// move.</b> <see cref="EffectLook"/> stands between
    /// <see cref="MatchDecorations"/> and <see cref="MatchTuning"/> so a capture
    /// can photograph an alternative, and the one way that could go wrong
    /// silently is a member that stops answering out of the file the game ships
    /// from. Ninety members is too many to eyeball, so they are walked.
    /// </para>
    /// <para>
    /// <b>The renders themselves are not asserted and must not be.</b> Nothing
    /// compares the PNGs to anything; they are documentation for a person to
    /// look at, and a golden image of a candidate nobody has signed off yet
    /// would be pinning the proposal rather than testing it. The same call
    /// <c>CandidateSetTests</c> makes.
    /// </para>
    /// </remarks>
    public class EffectLookTests
    {
        /// <summary>Where the committed candidates live, relative to the repository root.</summary>
        private const string CandidateFolder = "docs/frames/effect-candidates";

        /// <summary>The repository root, which is the folder above the Unity project.</summary>
        private static string RepoRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        [Test]
        public void TheShippedLookIsMatchTuningAndNothingElse()
        {
            Assert.That(EffectLook.Shipped.IsShipped, Is.True, "The shipped look overrides nothing.");
            Assert.That(EffectLook.Shipped.OverriddenCount, Is.Zero);

            var moved = new List<string>();

            foreach (KeyValuePair<string, Type> member in EffectLookFile.Members)
            {
                FieldInfo constant = typeof(MatchTuning).GetField(
                    member.Key, BindingFlags.Public | BindingFlags.Static);

                PropertyInfo colour = typeof(MatchTuning).GetProperty(
                    member.Key, BindingFlags.Public | BindingFlags.Static);

                if (constant == null && colour == null)
                {
                    // A candidate axis with no constant behind it. Those are
                    // asserted by name below, because what they default to is
                    // the whole of what makes them look rather than behaviour.
                    continue;
                }

                object shipped = constant != null ? constant.GetValue(null) : colour.GetValue(null);
                object read = typeof(EffectLook)
                    .GetProperty(member.Key, BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(EffectLook.Shipped);

                if (!Equals(shipped, read))
                {
                    moved.Add(member.Key + ": MatchTuning says " + shipped + ", the look says " + read);
                }
            }

            Assert.That(moved, Is.Empty, "These members stopped answering out of MatchTuning:\n" + string.Join("\n", moved));
        }

        [Test]
        public void TheAxesWithNoConstantShipAsThePictureTheGameAlreadyDraws()
        {
            EffectLook shipped = EffectLook.Shipped;

            Assert.That(
                shipped.HasteEffectTint,
                Is.EqualTo(shipped.SpeedEffectTint),
                "One colour covers both signs of a speed modifier until somebody signs otherwise.");

            Assert.That(
                shipped.BothModifiersTint,
                Is.EqualTo(shipped.SpeedEffectTint),
                "A body carrying both modifiers is drawn as the speed one.");

            Assert.That(shipped.TowerMarksShown, Is.False, "A tower carrying a modifier is not drawn.");
            Assert.That(shipped.UnitBarCrossed, Is.False, "There is one bar and it does not turn.");
            Assert.That(shipped.UnitBarClamped, Is.False, "Both segments are shares of the authored health.");
        }

        [Test]
        public void AnOverrideStandsInFrontOfOneMemberAndLeavesTheRest()
        {
            EffectLookFile candidate = EffectLookFile.Parse(
                "one-member",
                new[] { "HasteRingDiameter 2.5", "HasteRingColor 1,0,0" });

            Assert.That(candidate.Look.HasteRingDiameter, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(candidate.Look.HasteRingColor, Is.EqualTo(Color.red));

            Assert.That(
                candidate.Look.HasteRingHeight,
                Is.EqualTo(MatchTuning.HasteRingHeight).Within(0.0001f),
                "A member nobody named still answers out of MatchTuning.");

            Assert.That(candidate.Look.OverriddenCount, Is.EqualTo(2));
            Assert.That(candidate.Look.IsShipped, Is.False);
        }

        [Test]
        public void AMemberThatCountsThingsReadsItsOverrideRounded()
        {
            EffectLookFile candidate = EffectLookFile.Parse("rounded", new[] { "WardDomeRibs 11.5" });

            Assert.That(candidate.Look.WardDomeRibs, Is.EqualTo(12));
        }

        [Test]
        public void AFlagTakesTrueOrOne()
        {
            Assert.That(
                EffectLookFile.Parse("word", new[] { "UnitBarCrossed true" }).Look.UnitBarCrossed,
                Is.True);

            Assert.That(
                EffectLookFile.Parse("number", new[] { "UnitBarCrossed 1" }).Look.UnitBarCrossed,
                Is.True);

            Assert.That(
                EffectLookFile.Parse("off", new[] { "UnitBarCrossed false" }).Look.UnitBarCrossed,
                Is.False);
        }

        [Test]
        public void AColourReadsAsChannelsOrAsHex()
        {
            Assert.That(
                EffectLookFile.Parse("channels", new[] { "SpeedEffectTint 0.5,0.25,1" }).Look.SpeedEffectTint,
                Is.EqualTo(new Color(0.5f, 0.25f, 1f, 1f)));

            Color hex = EffectLookFile.Parse("hex", new[] { "SpeedEffectTint #FF0000" }).Look.SpeedEffectTint;

            Assert.That(hex.r, Is.EqualTo(1f).Within(0.01f));
            Assert.That(hex.g, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void AHashOpensACommentOnlyAtTheStartOfALine()
        {
            // Both of these were broken by a comment stripper that cut at the
            // first hash: a colour in hex became a key with no value, and a
            // question naming the issue it came from lost its own sentence.
            EffectLookFile candidate = EffectLookFile.Parse(
                "hashes",
                new[]
                {
                    "# a whole-line comment",
                    "question What should this be, given that #266 named no shape?",
                    "SpeedEffectTint #00FF00",
                });

            Assert.That(candidate.Look.SpeedEffectTint.g, Is.EqualTo(1f).Within(0.01f));
            StringAssert.Contains("named no shape", candidate.Question);
            StringAssert.Contains("#266", candidate.Question);
        }

        [Test]
        public void AMisspeltMemberIsAFaultThatNamesTheNearMisses()
        {
            FormatException fault = Assert.Throws<FormatException>(
                () => EffectLookFile.Parse("typo", new[] { "HasteRingDiamter 2" }));

            StringAssert.Contains("HasteRingDiamter", fault.Message);
            StringAssert.Contains("HasteRing", fault.Message);
        }

        [Test]
        public void ASignatureLineMovesOneRowAndLeavesEveryOtherAsItWasBound()
        {
            EffectLookFile candidate = EffectLookFile.Parse(
                "moved", new[] { "signature 7 OvergrowthRoots" });

            Assert.That(candidate.Bubbles[7], Is.EqualTo(BubbleSignature.OvergrowthRoots));

            var walk = new AnimationClip();
            var death = new AnimationClip();

            // Real models, because MatchArt refuses a row whose art is not
            // complete and answers "no art for unit 7" while listing unit 7 --
            // which is a confusing way to be told the scale is zero.
            var mageBody = new GameObject("SkeletonMage");
            var witchBody = new GameObject("Witch");

            try
            {
                MatchArt art = MatchArt.Of(
                    new[]
                    {
                        UnitArt.Armed(
                            7, mageBody, 1f, null, null, null, null, null,
                            bubbleSignature: BubbleSignature.HasteRing),
                        UnitArt.Armed(
                            44, witchBody, 1f, null, null, null, null, null,
                            bubbleSignature: BubbleSignature.HexPlates),
                    },
                    walk,
                    death);

                MatchArt applied = candidate.Applied(art);

                Assert.That(
                    applied.ArtFor(7).Signature.Bubble,
                    Is.EqualTo(BubbleSignature.OvergrowthRoots),
                    "The row the candidate named draws the candidate's shape.");

                Assert.That(
                    applied.ArtFor(44).Signature.Bubble,
                    Is.EqualTo(BubbleSignature.HexPlates),
                    "Every other row draws what the scene builder bound.");

                Assert.That(
                    art.ArtFor(7).Signature.Bubble,
                    Is.EqualTo(BubbleSignature.HasteRing),
                    "The art handed in is a copy source and is not itself moved.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(walk);
                UnityEngine.Object.DestroyImmediate(death);
                UnityEngine.Object.DestroyImmediate(mageBody);
                UnityEngine.Object.DestroyImmediate(witchBody);
            }
        }

        [Test]
        public void AShapeThatDoesNotExistIsAFaultThatListsTheOnesThatDo()
        {
            FormatException fault = Assert.Throws<FormatException>(
                () => EffectLookFile.Parse("nonsense", new[] { "signature 7 Sparkles" }));

            StringAssert.Contains("Sparkles", fault.Message);
            StringAssert.Contains(nameof(BubbleSignature.HasteRing), fault.Message);
        }

        [Test]
        public void EveryCommittedCandidateReadsAndNamesItsQuestion()
        {
            string folder = Path.Combine(RepoRoot, CandidateFolder);

            Assert.That(Directory.Exists(folder), Is.True, "No candidates at " + folder);

            string[] files = Directory.GetFiles(folder, "*.txt");

            Assert.That(files, Is.Not.Empty, "The folder is there and holds no candidate.");

            foreach (string file in files)
            {
                EffectLookFile candidate = EffectLookFile.Read(file);

                Assert.That(
                    candidate.Question,
                    Is.Not.Empty,
                    Path.GetFileName(file) + " says what it changes and not what it is asking.");

                Assert.That(
                    candidate.Label,
                    Is.Not.Empty,
                    Path.GetFileName(file) + " has no label, so a sheet cannot say which tile it is.");
            }
        }

        [Test]
        public void TheCommittedCandidatesBetweenThemCoverEveryQuestionTheTicketNamed()
        {
            string folder = Path.Combine(RepoRoot, CandidateFolder);

            string[] names = Directory
                .GetFiles(folder, "*.txt")
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();

            // One per row of the four creep auras, one for the shared ring, one
            // for the Blessing, one for telling a slow from a haste, and one
            // each for the four other things #254 left standing.
            foreach (string wanted in new[]
            {
                "haste", "ward", "hex", "frost", "ring", "blessing", "speed",
                "both-modifiers", "tower-marks", "bar-crossed", "bar-clamped",
            })
            {
                Assert.That(
                    names.Any(name => name.StartsWith(wanted, StringComparison.Ordinal)),
                    Is.True,
                    "Nothing in " + CandidateFolder + " is a candidate for '" + wanted + "'.");
            }
        }
    }
}
