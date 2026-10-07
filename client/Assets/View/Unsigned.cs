using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    public static class Unsigned
    {
        public const string MarkName = "Unsigned";

        private const int MarkSize = 20;

        private static readonly Color MarkColor = new Color(1f, 0.72f, 0.3f, 1f);

        public static Label Mark(string words)
        {
            var mark = new Label(words + ": placeholder words, not signed")
            {
                name = MarkName,
                pickingMode = PickingMode.Ignore,
            };

            mark.style.color = MarkColor;
            mark.style.fontSize = MarkSize;
            mark.style.unityFontStyleAndWeight = FontStyle.Italic;
            mark.style.unityTextAlign = TextAnchor.MiddleCenter;
            mark.style.marginTop = RuntimePanel.ControlGap;
            mark.style.paddingLeft = RuntimePanel.ControlGap;
            mark.style.paddingRight = RuntimePanel.ControlGap;
            mark.style.backgroundColor = RuntimePanel.BarColor;

            return mark;
        }
    }
}
