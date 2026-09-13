using UnityEngine.UIElements;

namespace View.Editor.ChromeCandidates
{
    internal static class WaveRow
    {
        public static VisualElement Of(MatchRoot root) =>
            root.Wave.Document.rootVisualElement.Q<VisualElement>("Wave");
    }

    public sealed class WaveBarScrolls : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement row = WaveRow.Of(root);

            var scroller = new ScrollView(ScrollViewMode.Horizontal) { name = "Scroller" };
            scroller.style.flexGrow = 1f;
            scroller.style.height = WaveBar.BarHeight;
            scroller.horizontalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            scroller.contentContainer.style.flexDirection = FlexDirection.Row;
            scroller.contentContainer.style.alignItems = Align.Center;

            while (row.childCount > 0)
            {
                VisualElement box = row[0];
                row.Remove(box);
                scroller.Add(box);
            }

            row.style.paddingTop = 0f;
            row.style.paddingBottom = 0f;
            row.Add(scroller);
        }
    }

    public sealed class WaveBarWraps : UiPreviewCapture.IUiPreviewLayout
    {
        private const float LineGap = 12f;

        public void Build(MatchRoot root, RunLoop loop)
        {
            VisualElement row = WaveRow.Of(root);

            row.style.height = StyleKeyword.Auto;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignContent = Align.FlexStart;
            row.style.paddingTop = LineGap;

            for (int index = 0; index < row.childCount; index++)
            {
                row[index].style.marginBottom = LineGap;
            }
        }
    }

    public sealed class WaveBarShrinks : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop) =>
            ShrunkBoxes.Lay(WaveRow.Of(root), boxWidth: 128f, emptyWidth: 60f, gap: 8f, nameSize: 18, countSize: 14);
    }

    public sealed class WaveBarShrinksToFit : UiPreviewCapture.IUiPreviewLayout
    {
        public void Build(MatchRoot root, RunLoop loop) =>
            ShrunkBoxes.Lay(WaveRow.Of(root), boxWidth: 96f, emptyWidth: 60f, gap: 8f, nameSize: 13, countSize: 12);
    }

    internal static class ShrunkBoxes
    {
        public static void Lay(
            VisualElement row, float boxWidth, float emptyWidth, float gap, int nameSize, int countSize)
        {
            for (int index = 0; index < row.childCount; index++)
            {
                VisualElement box = row[index];
                bool empty = box.name == "Empty box";

                box.style.width = empty ? emptyWidth : boxWidth;
                box.style.marginRight = gap;

                Label name = box.Q<Label>("Name");
                Label sending = box.Q<Label>("Sending");

                if (name != null) name.style.fontSize = nameSize;
                if (sending != null) sending.style.fontSize = countSize;
            }
        }
    }
}
