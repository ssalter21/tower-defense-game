using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// One match, drawn, with nobody watching it — the scaffolding every
    /// view-side test starts from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The art comes through <see cref="MatchArtSource"/>, and is deliberately
    /// <b>not</b> asked of the scene builder. A test that asked the builder
    /// which clips it chose could not catch the builder choosing the wrong ones,
    /// so the two lists are written out twice on purpose — the fixture's is
    /// <c>Tests.ArtSource.ChosenArt</c> — and a disagreement between them is a
    /// failure rather than a coincidence.
    /// </para>
    /// <para>
    /// <b>Not editor-only, and that is the point.</b> This used to load the art
    /// through <c>AssetDatabase</c>, which put the whole file behind
    /// <c>#if UNITY_EDITOR</c> and every fixture that used it with it. Built for
    /// anything but the editor those classes yielded no tests and the run
    /// reported green.
    /// </para>
    /// </remarks>
    public static class TheMatchOnScreen
    {
        /// <summary>
        /// The seed the watched match is played with, and the one the scene
        /// serializes. Not a simulation rule — the same match every time.
        /// </summary>
        public const ulong Seed = 1;

        /// <summary>A match, drawn, hung off <paramref name="host"/>.</summary>
        public static MatchView Begin(GameObject host)
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            return Begin(
                host,
                StreamingContent.ReadMap(),
                types,
                StreamingContent.ReadDefense(types),
                StreamingContent.ReadWave(types),
                Seed);
        }

        /// <summary>
        /// A match, drawn, on the four things and the seed the caller names
        /// rather than the shipped ones.
        /// </summary>
        /// <remarks>
        /// For the fixture that watches a match somebody else recorded — the
        /// parity run reads the map, the defense, the wave and the seed out of
        /// <c>content/match.replay</c>, so what it draws is the command line's
        /// match and not this file's. The type table is the exception and comes
        /// from the shipped content, because it is what the record is replayed
        /// <i>against</i> rather than something stored in it: the replay gate
        /// exists to refuse a record whose content hash is not this build's,
        /// and a table taken out of the record would have nothing to refuse.
        /// The art stays here, because which models and clips a match is drawn
        /// with is the one thing this class is for.
        /// </remarks>
        public static MatchView Begin(
            GameObject host,
            HexMap map,
            UnitTypeTable types,
            TowerLayout defense,
            WaveScript wave,
            ulong seed)
        {
            var view = host.AddComponent<MatchView>();
            view.Begin(map, types, defense, wave, seed, Art());

            return view;
        }

        /// <summary>The models and clips the match is drawn with.</summary>
        public static MatchArt Art() => MatchArtSource.Load();
    }
}
