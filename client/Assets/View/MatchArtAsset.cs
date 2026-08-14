using UnityEngine;

namespace View
{
    /// <summary>
    /// A <see cref="MatchArt"/> bundle as an asset, so a build can carry one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A model and a scale per unit type, plus the shared clips, and nothing
    /// else. It exists because a
    /// <see cref="MatchArt"/> on its own is a plain serializable class — it
    /// lives inside whatever holds it, and in the game that is the generated
    /// scene's root object. A test build has no such object, so this is the
    /// thing that holds one instead.
    /// </para>
    /// <para>
    /// <b>Generated, not authored.</b> It is written by
    /// <c>tools/build-test-assets.ps1</c> from the paths the test fixture
    /// chose, and committed beside the change that caused it, exactly like the
    /// streaming copy of the content. Nothing picks art here; there is no
    /// inspector workflow and no <c>CreateAssetMenu</c>, because an asset
    /// somebody could make by hand is an asset that can disagree with the paths
    /// it was supposed to come from.
    /// </para>
    /// </remarks>
    public sealed class MatchArtAsset : ScriptableObject
    {
        [SerializeField]
        private MatchArt art;

        /// <summary>The bundle. Its own accessors throw by name if a field is unfilled.</summary>
        public MatchArt Art => art;

        /// <summary>Wraps a bundle a caller already has, for the generator.</summary>
        public static MatchArtAsset Holding(MatchArt bundle)
        {
            var asset = CreateInstance<MatchArtAsset>();
            asset.art = bundle;

            return asset;
        }
    }
}
