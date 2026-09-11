using System.Globalization;
using Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace View.Editor.ChromeCandidates
{
    internal readonly struct TokenTally
    {
        public TokenTally(MatchRoot root, RunLoop loop)
        {
            Held = root.Composing.CapstoneTokens;
            Granted = Run.CapstoneTokensGrantedThrough(loop.Wave);
            Spent = Granted - Held;
            ToCome = Run.CapstoneTokenRounds.Count - Granted;
            NextGrantWave = 0;

            foreach (int wave in Run.CapstoneTokenRounds)
            {
                if (wave > loop.Wave)
                {
                    NextGrantWave = wave;
                    break;
                }
            }
        }

        public int Held { get; }

        public int Granted { get; }

        public int Spent { get; }

        public int ToCome { get; }

        public int NextGrantWave { get; }

        public string Sentence() =>
            "Capstone tokens  " + N(Held) + " held  ·  " + N(Spent) + " spent  ·  " + N(ToCome) + " to come";

        public string HeldSentence() =>
            Held == 1 ? "1 capstone token held" : N(Held) + " capstone tokens held";

        public string WhyTheLineStops() =>
            NextGrantWave > 0
                ? "needs a capstone token  ·  next at wave " + N(NextGrantWave)
                : "needs a capstone token  ·  none to come";

        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    internal static class HeaderBar
    {
        public static VisualElement Of(RunLoop loop) =>
            loop.Header.Document.rootVisualElement.Q<VisualElement>("Header");

        public static int IndexOfGold(VisualElement bar) => bar.IndexOf(bar.Q<Label>("Gold"));
    }

    public sealed class TokensInTheHeader : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement bar = HeaderBar.Of(loop);
            var tally = new TokenTally(root, loop);

            var field = new Label { name = "Tokens", text = tally.Sentence(), pickingMode = PickingMode.Ignore };
            field.style.marginRight = 48f;
            field.style.color = RuntimePanel.LabelColor;
            field.style.fontSize = 24;
            field.style.unityTextAlign = TextAnchor.MiddleLeft;

            bar.Insert(HeaderBar.IndexOfGold(bar) + 1, field);
        }
    }

    public sealed class TokensAsPipsInTheHeader : UiPreviewCapture.IUiPreviewLayout
    {
        private const float Pip = 18f;

        private static readonly Color SpentColor = new Color(0.22f, 0.25f, 0.3f, 1f);

        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement bar = HeaderBar.Of(loop);
            var tally = new TokenTally(root, loop);

            var group = new VisualElement { name = "Tokens", pickingMode = PickingMode.Ignore };
            group.style.flexDirection = FlexDirection.Row;
            group.style.alignItems = Align.Center;
            group.style.marginRight = 48f;

            var word = new Label { name = "Word", text = "Tokens", pickingMode = PickingMode.Ignore };
            word.style.color = RuntimePanel.LabelColor;
            word.style.fontSize = 24;
            word.style.marginRight = 12f;
            group.Add(word);

            for (int index = 0; index < Run.CapstoneTokenRounds.Count; index++)
            {
                group.Add(PipFor(index < tally.Held ? PipState.Held : index < tally.Granted ? PipState.Spent : PipState.ToCome));
            }

            bar.Insert(HeaderBar.IndexOfGold(bar) + 1, group);
        }

        private enum PipState { Held, Spent, ToCome }

        private static VisualElement PipFor(PipState state)
        {
            var pip = new VisualElement { pickingMode = PickingMode.Ignore };
            pip.style.width = Pip;
            pip.style.height = Pip;
            pip.style.marginRight = 6f;
            pip.style.borderTopLeftRadius = Pip * 0.5f;
            pip.style.borderTopRightRadius = Pip * 0.5f;
            pip.style.borderBottomLeftRadius = Pip * 0.5f;
            pip.style.borderBottomRightRadius = Pip * 0.5f;
            pip.style.borderTopWidth = 2f;
            pip.style.borderBottomWidth = 2f;
            pip.style.borderLeftWidth = 2f;
            pip.style.borderRightWidth = 2f;
            pip.style.borderTopColor = RuntimePanel.LabelColor;
            pip.style.borderBottomColor = RuntimePanel.LabelColor;
            pip.style.borderLeftColor = RuntimePanel.LabelColor;
            pip.style.borderRightColor = RuntimePanel.LabelColor;

            switch (state)
            {
                case PipState.Held:
                    pip.style.backgroundColor = RuntimePanel.LabelColor;
                    break;
                case PipState.Spent:
                    pip.style.backgroundColor = SpentColor;
                    break;
                case PipState.ToCome:
                    pip.style.backgroundColor = Color.clear;
                    pip.style.opacity = 0.45f;
                    break;
            }

            return pip;
        }
    }

    public sealed class TokensAtTheLadder : UiPreviewCapture.IUiPreviewLayout
    {
        private const float Width = 208f;

        private const float AnchorHeight = 2.2f;

        private static readonly Color QuietColor = new Color(0.68f, 0.72f, 0.78f, 1f);

        public void Build(MatchRoot root, RunLoop loop)
        {
            var tally = new TokenTally(root, loop);

            if (root.Palette.IsOffering)
            {
                VisualElement offer = OfferPanel.Of(root);
                offer.Insert(0, Line(tally.HeldSentence()));
                return;
            }

            if (!StoppedLine(root, loop, out int column, out int row, out UnitType top))
            {
                return;
            }

            var panel = new VisualElement { name = "Stopped" };
            panel.style.position = Position.Absolute;
            panel.style.width = Width;
            panel.style.backgroundColor = RuntimePanel.BarColor;
            panel.Add(Line(RosterNames.Of(top)));
            panel.Add(Line(tally.WhyTheLineStops()));

            VisualElement host = root.Palette.Document.rootVisualElement;
            host.Add(panel);

            Camera camera = root.CameraRig.Camera;
            Vector3 world = HexGeometry.ToWorld(column, row, root.Map.LevelAt(column, row))
                + (Vector3.up * AnchorHeight);

            panel.schedule.Execute(() =>
            {
                Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(host.panel, world, camera);
                panel.style.left = point.x - (Width * 0.5f);
                panel.style.top = point.y;
            }).Every(16);
        }

        private static Label Line(string text)
        {
            var line = new Label { text = text, pickingMode = PickingMode.Ignore };
            line.style.height = 40f;
            line.style.paddingLeft = 12f;
            line.style.paddingRight = 12f;
            line.style.color = QuietColor;
            line.style.fontSize = 18;
            line.style.unityTextAlign = TextAnchor.MiddleLeft;
            line.style.backgroundColor = RuntimePanel.ControlColor;
            return line;
        }

        private static bool StoppedLine(MatchRoot root, RunLoop loop, out int column, out int row, out UnitType top)
        {
            ComposedRound round = root.Composing;
            HexMap map = round.Map;

            for (row = 0; row < map.Height; row++)
            {
                for (column = 0; column < map.Width; column++)
                {
                    UnitType standing = round.StandingOn(column, row);

                    if (standing is null || round.UpgradesOn(column, row).Count > 0)
                    {
                        continue;
                    }

                    foreach (UpgradeEdge edge in loop.Run.Ladder.Edges)
                    {
                        if (edge.From == standing.Id && loop.Run.Ladder.IsCapstoneEdge(edge.From, edge.To))
                        {
                            top = loop.Run.Types.ById(edge.To);
                            return true;
                        }
                    }
                }
            }

            column = -1;
            row = -1;
            top = null;
            return false;
        }
    }
}
