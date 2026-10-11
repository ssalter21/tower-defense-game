using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace View.Editor.ChromeCandidates
{
    internal static class HexWheelStage
    {
        private const string Subject = "archer";

        private const int PurseLeftAfterTheWave = 35;

        private const int CreepsOnTheWave = 2;

        private const float ClearOfTheWheel = 40f;

        public static UnitType Archer(ComposedRound round)
        {
            foreach (UnitType tower in round.Palette)
            {
                if (string.Equals(tower.Label, Subject, StringComparison.OrdinalIgnoreCase))
                {
                    return tower;
                }
            }

            throw new InvalidOperationException("No " + Subject + " on the palette, so the wheel has no hex to open on.");
        }

        public static void TheHex(MatchRoot root, out int column, out int row)
        {
            if (!UiPreviewCapture.FirstLegalCell(root.Composing, Archer(root.Composing), out column, out row))
            {
                throw new InvalidOperationException("No cell would take an " + Subject + ", so there is no empty hex to open the wheel on.");
            }
        }

        public static HexWheel Wheel(MatchRoot root) =>
            HexWheel.Build(root.transform, root.CameraRig.Camera, root.Map);

        public static void SpendOnTheWave(MatchRoot root)
        {
            ComposedRound round = root.Composing;

            while (round.Gold > PurseLeftAfterTheWave)
            {
                int before = round.Gold;
                UiPreviewCapture.Send(root, 1);

                if (round.Gold == before)
                {
                    throw new InvalidOperationException(
                        "No creep is left to send at " + before + " gold, so the purse cannot be brought to "
                        + PurseLeftAfterTheWave + " and the every-root and affordable-root frames would agree.");
                }
            }
        }

        public static void ComposeOnTheBar(MatchRoot root) => UiPreviewCapture.Send(root, CreepsOnTheWave);

        public static void RequireWedges(HexWheel wheel, string what)
        {
            if (wheel.Wedges.Count == 0)
            {
                throw new InvalidOperationException("The wheel opened with no wedges on " + what + ", so the frame would show a hex with nothing around it.");
            }
        }

        public static bool OutsideTheWheel(MatchRoot root, int column, int row, float reach, out int otherColumn, out int otherRow)
        {
            ComposedRound round = root.Composing;
            UnitType archer = Archer(round);
            Camera camera = root.CameraRig.Camera;
            Vector2 centre = PanelPoint(camera, root.Map, column, row);

            otherColumn = 0;
            otherRow = 0;

            bool any = false;
            float nearest = float.MaxValue;

            for (int candidateRow = 0; candidateRow < root.Map.Height; candidateRow++)
            {
                for (int candidateColumn = 0; candidateColumn < root.Map.Width; candidateColumn++)
                {
                    if (!round.Allows(BuildAction.Of(ActionKind.Place, archer.Id, candidateColumn, candidateRow)))
                    {
                        continue;
                    }

                    Vector2 point = PanelPoint(camera, root.Map, candidateColumn, candidateRow);
                    float away = Vector2.Distance(point, centre);

                    if (away < reach + ClearOfTheWheel || away >= nearest || !OnScreen(point))
                    {
                        continue;
                    }

                    nearest = away;
                    otherColumn = candidateColumn;
                    otherRow = candidateRow;
                    any = true;
                }
            }

            return any;
        }

        public static void OpenAgainOnceLaidOut(WaveBar bar, int index)
        {
            VisualElement box = bar.Boxes[index];
            EventCallback<GeometryChangedEvent> laidOut = null;

            laidOut = _ =>
            {
                box.UnregisterCallback(laidOut);
                bar.Open(index);
            };

            box.RegisterCallback(laidOut);
        }

        private static Vector2 PanelPoint(Camera camera, HexMap map, int column, int row)
        {
            Vector3 viewport = camera.WorldToViewportPoint(HexGeometry.ToWorld(column, row, map.LevelAt(column, row)));
            float height = RuntimePanel.ReferenceResolution.y;
            float width = height * camera.aspect;

            return new Vector2(viewport.x * width, (1f - viewport.y) * height);
        }

        private static bool OnScreen(Vector2 point)
        {
            float height = RuntimePanel.ReferenceResolution.y;

            return point.x > 0f && point.y > TowerPalette.BarHeight && point.y < height - TowerPalette.BarHeight - WaveBar.BarHeight;
        }
    }

    public sealed class HexWheelEveryRoot : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.TheHex(root, out int column, out int row);
            HexWheelStage.SpendOnTheWave(root);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, HexWheelChoices.EveryRoot(root.Composing));
            HexWheelStage.RequireWedges(wheel, "an empty hex");
        }
    }

    public sealed class HexWheelAffordableRoots : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.TheHex(root, out int column, out int row);
            HexWheelStage.SpendOnTheWave(root);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, HexWheelChoices.AffordableRoots(root.Composing));
            HexWheelStage.RequireWedges(wheel, "an empty hex");

            if (wheel.Wedges.Count == root.Composing.Palette.Count)
            {
                throw new InvalidOperationException(
                    "The purse covers every root at " + root.Composing.Gold + " gold, so this frame would be the every-root frame again.");
            }
        }
    }

    public sealed class HexWheelRootsWithLadders : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.TheHex(root, out int column, out int row);
            HexWheelStage.SpendOnTheWave(root);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, HexWheelChoices.RootsWithLadders(root.Composing, loop.Run.Ladder, loop.Run.Types));
            HexWheelStage.RequireWedges(wheel, "an empty hex");
        }
    }

    public sealed class HexWheelOccupied : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            ComposedRound round = root.Composing;

            HexWheelStage.TheHex(root, out int column, out int row);
            round.Do(BuildAction.Of(ActionKind.Place, HexWheelStage.Archer(round).Id, column, row));
            root.Building.Follow();
            root.Palette.Follow();

            HexWheel wheel = HexWheelStage.Wheel(root);
            IReadOnlyList<HexWheelWedge> wedges = HexWheelChoices.Upgrades(round, loop.Run.Ladder, column, row);
            wheel.Open(column, row, wedges);
            HexWheelStage.RequireWedges(wheel, "the archer");

            if (!wedges[wedges.Count - 1].Undesigned)
            {
                throw new InvalidOperationException("The archer's wheel has no capstone place on it, so the frame would not show where the capstone goes.");
            }
        }
    }

    public sealed class HexWheelWaveOnTheBar : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.TheHex(root, out int column, out int row);
            HexWheelStage.ComposeOnTheBar(root);

            int next = root.Composing.Slots.Count;
            root.Wave.Open(next);

            if (!root.Wave.IsListing)
            {
                throw new InvalidOperationException("The wave bar did not open its list, so the frame would not show the wave being composed on it.");
            }

            HexWheelStage.OpenAgainOnceLaidOut(root.Wave, next);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, HexWheelChoices.EveryRoot(root.Composing));
            HexWheelStage.RequireWedges(wheel, "a buildable hex");
        }
    }

    public sealed class HexWheelWaveOnTheEntrance : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.ComposeOnTheBar(root);
            root.Wave.Document.rootVisualElement.style.display = DisplayStyle.None;

            Hex.ToOddRowOffset(root.Map.Spawn, out int column, out int row);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, HexWheelChoices.Creeps(root.Composing));
            HexWheelStage.RequireWedges(wheel, "the entrance hex");
        }
    }

    public sealed class HexWheelOnHover : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.TheHex(root, out int column, out int row);
            root.Building.Lit(column, row);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, HexWheelChoices.EveryRoot(root.Composing), showsCovered: true);
            HexWheelStage.RequireWedges(wheel, "the hovered hex");
        }
    }

    public sealed class HexWheelOnClick : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            HexWheelStage.TheHex(root, out int column, out int row);
            IReadOnlyList<HexWheelWedge> wedges = HexWheelChoices.EveryRoot(root.Composing);

            if (!HexWheelStage.OutsideTheWheel(root, column, row, HexWheel.OuterRadiusOf(wedges), out int hoverColumn, out int hoverRow))
            {
                throw new InvalidOperationException("No buildable hex lies clear of the wheel, so the hover could not be shown beside it.");
            }

            root.Building.Lit(hoverColumn, hoverRow);

            HexWheel wheel = HexWheelStage.Wheel(root);
            wheel.Open(column, row, wedges);
            HexWheelStage.RequireWedges(wheel, "the clicked hex");
        }
    }
}
