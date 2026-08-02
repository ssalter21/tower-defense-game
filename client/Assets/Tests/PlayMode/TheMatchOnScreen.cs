#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using Sim;
using UnityEditor;
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
    /// The art is loaded here the way the scene builder loads it, and is
    /// deliberately <b>not</b> asked of the builder. A test that asked the
    /// builder which clips it chose could not catch the builder choosing the
    /// wrong ones, so the two lists are written out twice on purpose and a
    /// disagreement between them is a failure rather than a coincidence.
    /// </para>
    /// <para>
    /// Editor-only, because loading the art goes through
    /// <see cref="AssetDatabase"/>. Every test that uses it is editor-only for
    /// the same reason.
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

            var view = host.AddComponent<MatchView>();
            view.Begin(
                StreamingContent.ReadMap(),
                types,
                StreamingContent.ReadDefense(types),
                StreamingContent.ReadWave(types),
                Seed,
                Art());

            return view;
        }

        /// <summary>
        /// Steps the match, drawing every tick, until <paramref name="stop"/>
        /// says so or the match ends.
        /// </summary>
        public static void RunUntil(MatchView view, System.Func<bool> stop)
        {
            while (!view.IsFinished)
            {
                view.StepOneTick();
                view.Draw(1f);

                if (stop())
                {
                    return;
                }
            }
        }

        /// <summary>The models and clips the match is drawn with.</summary>
        public static MatchArt Art() =>
            MatchArt.Of(
                Load<GameObject>("Assets/Art/Characters/Skeleton_Warrior.fbx"),
                Clip("Assets/Art/Animations/Rig_Medium_MovementBasic.fbx", "Walking_A"),
                Clip("Assets/Art/Animations/Rig_Medium_General.fbx", "Death_A"),
                Load<GameObject>("Assets/Art/Characters/Ranger.fbx"),
                Load<GameObject>("Assets/Art/Weapons/bow_withString.fbx"),
                Clip("Assets/Art/Animations/Rig_Medium_CombatRanged.fbx", "Ranged_Bow_Idle"),
                Clip("Assets/Art/Animations/Rig_Medium_CombatRanged.fbx", "Ranged_Bow_Draw"),
                Clip("Assets/Art/Animations/Rig_Medium_CombatRanged.fbx", "Ranged_Bow_Release"),
                Load<GameObject>("Assets/Art/Buildings/building_tower_A_blue.fbx"));

        private static T Load<T>(string path)
            where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, $"nothing imported at {path}");

            return asset;
        }

        private static AnimationClip Clip(string bank, string name)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(bank)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == name);

            Assert.IsNotNull(clip, $"no clip '{name}' in {bank}");

            return clip;
        }
    }
}
#endif
