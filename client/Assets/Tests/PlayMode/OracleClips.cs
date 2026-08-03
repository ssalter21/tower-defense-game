using System;
using UnityEngine;

namespace Tests.PlayMode
{
    /// <summary>
    /// The synthetic clips with an exact analytic answer, and the numbers that
    /// answer is made of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sampling tests need a clip whose correct pose at any time is a number
    /// rather than a judgement — <i>x = 10t</i> on one bone, so "is the pose
    /// right" is an assertion. They used to build one per test with
    /// <see cref="AnimationClip.SetCurve"/>, which <b>does not work outside the
    /// editor</b>: on a non-legacy clip it is an editor-only call, and in a
    /// player it quietly leaves an empty clip that poses everything at zero.
    /// Fourteen tests failed against their own oracle the first time this suite
    /// was run in a player.
    /// </para>
    /// <para>
    /// So the clips are authored by <c>Tests.Fixtures.GeneratedTestAssets</c>
    /// where <c>SetCurve</c> works, committed, and loaded here through
    /// <see cref="Resources"/> — which behaves the same in both places, so there
    /// is no adapter and no second path to get wrong.
    /// </para>
    /// <para>
    /// <b>The numbers live here rather than in the generator</b>, because the
    /// generator writes what this file says the oracle is, and a second copy of
    /// <i>10</i> could disagree with the curve it was supposed to describe.
    /// </para>
    /// </remarks>
    public static class OracleClips
    {
        /// <summary>The folder inside <c>Resources</c> the clips are written to.</summary>
        public const string Folder = "TestClips";

        /// <summary>The child transform the one animated channel is on.</summary>
        public const string BoneName = "Bone";

        /// <summary>How long every oracle clip runs, in seconds.</summary>
        public const float Length = 1.0f;

        /// <summary>How far <see cref="Linear"/> travels across its length.</summary>
        public const float Travel = 10.0f;

        /// <summary>A straight line: <c>localPosition.x</c> from 0 to <see cref="Travel"/>.</summary>
        public const string Linear = "OracleLinear";

        /// <summary>Holds <c>localPosition.x</c> at 0. One end of a blend.</summary>
        public const string ConstantZero = "OracleConstantZero";

        /// <summary>Holds <c>localPosition.x</c> at <see cref="Travel"/>. The other end.</summary>
        public const string ConstantTravel = "OracleConstantTravel";

        /// <summary>One of the committed clips, by name.</summary>
        public static AnimationClip Load(string name)
        {
            var clip = Resources.Load<AnimationClip>(Folder + "/" + name);

            if (clip == null)
            {
                throw new InvalidOperationException(
                    "No oracle clip at Resources/" + Folder + "/" + name + ". They are generated and "
                    + "committed — run tools/build-test-assets.ps1 and commit what it writes.");
            }

            return clip;
        }
    }
}
