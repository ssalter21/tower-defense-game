using System;
using System.Collections.Generic;
using System.IO;
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
        public void AnOverrideStandsInFrontOfOneMemberAndLeavesTheRest()
        {
            EffectLookFile candidate = EffectLookFile.Parse(
                "one-member",
                new[] { "AuraDiscAlpha 0.5", "HasteRingColor 1,0,0" });

            Assert.That(candidate.Look.AuraDiscAlpha, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(candidate.Look.HasteRingColor, Is.EqualTo(Color.red));

            Assert.That(
                candidate.Look.AuraDiscThickness,
                Is.EqualTo(MatchTuning.AuraDiscThickness).Within(0.0001f),
                "A member nobody named still answers out of MatchTuning.");

            Assert.That(candidate.Look.OverriddenCount, Is.EqualTo(2));
            Assert.That(candidate.Look.IsShipped, Is.False);
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
    }
}
