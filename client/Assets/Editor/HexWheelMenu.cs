using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    public static class HexWheelMenu
    {
        [MenuItem("Tools/Hex Wheel/Open on a hex")]
        public static void Open()
        {
            MatchRoot root = Object.FindAnyObjectByType<MatchRoot>();

            if (!EditorApplication.isPlaying || root == null || root.Composing == null)
            {
                Debug.LogWarning("The hex wheel opens on a build phase: enter play mode and start a run first.");

                return;
            }

            ComposedRound round = root.Composing;

            if (!TheHex(root, round, out int column, out int row))
            {
                Debug.LogWarning("No hex on this board would take a tower, so there is nowhere to open the wheel.");

                return;
            }

            HexWheel wheel = root.GetComponentInChildren<HexWheel>();

            if (wheel == null)
            {
                wheel = HexWheel.Build(root.transform, root.CameraRig.Camera, root.Map);
            }

            UnitType standing = round.StandingOn(column, row);

            wheel.Open(
                column,
                row,
                standing is null
                    ? HexWheelChoices.EveryRoot(round)
                    : HexWheelChoices.Upgrades(round, root.Run.Ladder, column, row));
        }

        [MenuItem("Tools/Hex Wheel/Close")]
        public static void Close()
        {
            HexWheel wheel = Object.FindAnyObjectByType<HexWheel>();

            if (wheel != null)
            {
                wheel.Close();
            }
        }

        private static bool TheHex(MatchRoot root, ComposedRound round, out int column, out int row)
        {
            if (root.Building != null && root.Building.IsLit)
            {
                column = root.Building.LitColumn;
                row = root.Building.LitRow;

                return true;
            }

            foreach (UnitType tower in round.Palette)
            {
                if (UiPreviewCapture.FirstLegalCell(round, tower, out column, out row))
                {
                    return true;
                }
            }

            column = 0;
            row = 0;

            return false;
        }
    }
}
