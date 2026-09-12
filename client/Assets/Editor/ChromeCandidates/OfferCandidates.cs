using UnityEngine;
using UnityEngine.UIElements;

namespace View.Editor.ChromeCandidates
{
    internal static class OfferPanel
    {
        public const string RungSeparator = "   ";

        public static VisualElement Of(MatchRoot root) =>
            root.Palette.Document.rootVisualElement.Q<VisualElement>("Offer");
    }

    public sealed class OfferFitsItsLongestRung : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement offer = OfferPanel.Of(root);

            offer.style.width = StyleKeyword.Auto;
            offer.style.minWidth = 208f;

            for (int index = 0; index < offer.childCount; index++)
            {
                offer[index].style.paddingLeft = 16f;
                offer[index].style.paddingRight = 16f;
            }
        }
    }

    public sealed class OfferIsWider : UiPreviewCapture.IUiPreviewLayout
    {
        private const float Width = 336f;

        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement offer = OfferPanel.Of(root);

            offer.style.width = Width;
            offer.style.marginLeft = -(Width - 208f) * 0.5f;
        }
    }

    public sealed class OfferPricesBeneathTheName : UiPreviewCapture.IUiPreviewLayout
    {
        private static readonly Color QuietColor = new Color(0.68f, 0.72f, 0.78f, 1f);

        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement offer = OfferPanel.Of(root);

            foreach (Button rung in offer.Query<Button>().Build())
            {
                int split = rung.text.IndexOf(OfferPanel.RungSeparator, System.StringComparison.Ordinal);

                if (split < 0)
                {
                    continue;
                }

                string name = rung.text.Substring(0, split);
                string price = rung.text.Substring(split + OfferPanel.RungSeparator.Length);

                rung.text = string.Empty;
                rung.style.height = 64f;
                rung.style.flexDirection = FlexDirection.Column;
                rung.style.justifyContent = Justify.Center;

                var above = new Label { name = "Name", text = name, pickingMode = PickingMode.Ignore };
                above.style.color = RuntimePanel.LabelColor;
                above.style.fontSize = 18;
                above.style.unityTextAlign = TextAnchor.MiddleCenter;

                var beneath = new Label { name = "Price", text = price, pickingMode = PickingMode.Ignore };
                beneath.style.color = QuietColor;
                beneath.style.fontSize = 15;
                beneath.style.unityTextAlign = TextAnchor.MiddleCenter;

                rung.Add(above);
                rung.Add(beneath);
            }
        }
    }
}
