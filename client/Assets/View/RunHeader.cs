using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// The one bar that is up in every mode: which wave this is, how much
    /// health is left, how much gold there is, and the one button that moves
    /// the run on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three fields, and it is the same three in both modes.</b> The header
    /// is anchored to the top of the screen and the playback controls are along
    /// the bottom, so entering and leaving watch mode adds and removes chrome
    /// <i>beneath</i> this bar and never moves it. A header that reflowed as the
    /// mode changed would make the one thing on screen that is meant to be
    /// constant the thing that jumps.
    /// </para>
    /// <para>
    /// <b>Gold carries the weight a slot count used to.</b> #179 took the take
    /// and the wave's slot bound out of the rules, so nothing bounds a wave
    /// except what the purse can buy — which makes the gold figure the thing a
    /// player composes against, and the reason it is on the bar rather than
    /// beside the palette. What the wave itself is doing is the wave bar's job
    /// (#197), which is on screen beside this in build mode.
    /// </para>
    /// <para>
    /// <b>No forecast, in any mode.</b> Every number here is something the run
    /// has already settled or something the composed phase already costs.
    /// Nothing on this bar says what a round is going to come to. See ADR-0051.
    /// </para>
    /// <para>
    /// <b>It draws and it does not decide.</b> The wording of the button, what
    /// pressing it does and whether the run is over are all
    /// <see cref="RunLoop"/>'s; this asks it on every frame and lays out what
    /// comes back.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RunHeader : MonoBehaviour
    {
        /// <summary>Over the palette, the wave bar and the playback bar.</summary>
        private const int PanelSortingOrder = 3;

        /// <summary>How tall the bar is.</summary>
        public const float BarHeight = 72f;

        private const float ButtonWidth = 180f;

        private const float ButtonHeight = 48f;

        private const float FieldGap = 48f;

        private const int FieldSize = 24;

        private const int EndingSize = 30;

        private const float EndingWidth = 720f;

        private const float EndingPadding = 32f;

        private static readonly Color EndingColor = new Color(0.06f, 0.07f, 0.09f, 0.94f);

        private RunLoop _loop;

        private PanelSettings _panel;

        private VisualElement _ending;

        /// <summary>Which wave is on screen, and how many there are.</summary>
        public Label Wave { get; private set; }

        /// <summary>What is left of the health pool.</summary>
        public Label Health { get; private set; }

        /// <summary>What there is to spend.</summary>
        public Label Gold { get; private set; }

        /// <summary>The one button. What it says is the loop's to decide.</summary>
        public Button Action { get; private set; }

        /// <summary>What the run came to, shown once it is over.</summary>
        public Label Ending { get; private set; }

        /// <summary>The panel the bar is drawn on.</summary>
        public UIDocument Document { get; private set; }

        /// <summary>Builds the bar under <paramref name="parent"/>, following <paramref name="loop"/>.</summary>
        public static RunHeader Build(Transform parent, RunLoop loop)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (loop == null) throw new ArgumentNullException(nameof(loop));

            var host = new GameObject("RunHeader");
            host.transform.SetParent(parent, worldPositionStays: false);

            var header = host.AddComponent<RunHeader>();
            header.Assemble(host.AddComponent<UIDocument>(), loop);

            return header;
        }

        /// <summary>
        /// Puts the bar back in step with the run. Called every frame, and by
        /// the loop the moment a mode changes so that the bar never shows the
        /// mode it was in for one frame after leaving it.
        /// </summary>
        public void Follow()
        {
            Wave.text = "Wave "
                + Number(_loop.Wave)
                + " of "
                + Number(_loop.Run.Waves);

            Health.text = "Health "
                + Number(_loop.Run.Health)
                + " of "
                + Number(_loop.Run.Outcome.HealthPoolGold);

            Gold.text = RosterNames.Gold(_loop.Gold);

            string label = _loop.ActionLabel;

            Action.text = label;
            Action.style.display = label.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            Ending.text = _loop.EndingText;
            _ending.style.display = Ending.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Whether a screen point lands on this bar rather than on the board behind it.</summary>
        public bool Covers(Vector2 screenPoint) => RuntimePanel.Covers(Document, screenPoint);

        private void Update() => Follow();

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        private void Assemble(UIDocument document, RunLoop loop)
        {
            _loop = loop;
            _panel = RuntimePanel.Settings("Header panel", PanelSortingOrder);

            Document = document;
            document.panelSettings = _panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            VisualElement bar = Bar();
            document.rootVisualElement.Add(bar);

            Wave = AddField(bar, "Wave");
            Health = AddField(bar, "Health");
            Gold = AddField(bar, "Gold");

            // Pushes the button to the far end, so the three fields read as one
            // group and the thing that moves the run on is nowhere near them.
            var spacer = new VisualElement { name = "Spacer", pickingMode = PickingMode.Ignore };
            spacer.style.flexGrow = 1f;
            bar.Add(spacer);

            Action = AddAction(bar, loop);

            _ending = EndingPanel();
            Ending = AddEnding(_ending);
            document.rootVisualElement.Add(_ending);

            Follow();
        }

        /// <summary>
        /// The bar itself: a row across the top of the screen, opaque, so a
        /// click on it stops at it rather than falling through onto the
        /// playfield behind.
        /// </summary>
        private static VisualElement Bar()
        {
            var bar = new VisualElement { name = "Header" };

            bar.style.position = Position.Absolute;
            bar.style.left = 0f;
            bar.style.right = 0f;
            bar.style.top = 0f;
            bar.style.height = BarHeight;
            bar.style.paddingLeft = RuntimePanel.Margin;
            bar.style.paddingRight = RuntimePanel.Margin;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = RuntimePanel.BarColor;

            return bar;
        }

        private static Label AddField(VisualElement bar, string name)
        {
            var field = new Label { name = name, pickingMode = PickingMode.Ignore };

            field.style.marginRight = FieldGap;
            field.style.color = RuntimePanel.LabelColor;
            field.style.fontSize = FieldSize;
            field.style.unityTextAlign = TextAnchor.MiddleLeft;

            bar.Add(field);

            return field;
        }

        private static Button AddAction(VisualElement bar, RunLoop loop)
        {
            var button = new Button(loop.Press) { name = "Action" };

            button.style.width = ButtonWidth;
            button.style.height = ButtonHeight;
            button.style.flexShrink = 0f;
            button.style.marginLeft = RuntimePanel.ControlGap;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.marginRight = 0f;
            button.style.backgroundColor = RuntimePanel.ControlColor;
            button.style.color = RuntimePanel.LabelColor;
            button.style.fontSize = FieldSize;

            bar.Add(button);

            return button;
        }

        /// <summary>
        /// Where the run's last frame is drawn: the middle of the screen, under
        /// the header and over everything else.
        /// </summary>
        private static VisualElement EndingPanel()
        {
            var panel = new VisualElement { name = "Ending" };

            panel.style.position = Position.Absolute;
            panel.style.left = 0f;
            panel.style.right = 0f;
            panel.style.top = 0f;
            panel.style.bottom = 0f;
            panel.style.alignItems = Align.Center;
            panel.style.justifyContent = Justify.Center;
            panel.style.display = DisplayStyle.None;

            return panel;
        }

        private static Label AddEnding(VisualElement panel)
        {
            var ending = new Label { name = "EndingText", pickingMode = PickingMode.Ignore };

            ending.style.maxWidth = EndingWidth;
            ending.style.paddingLeft = EndingPadding;
            ending.style.paddingRight = EndingPadding;
            ending.style.paddingTop = EndingPadding;
            ending.style.paddingBottom = EndingPadding;
            ending.style.backgroundColor = EndingColor;
            ending.style.color = RuntimePanel.LabelColor;
            ending.style.fontSize = EndingSize;
            ending.style.unityTextAlign = TextAnchor.MiddleCenter;
            ending.style.whiteSpace = WhiteSpace.Normal;

            panel.Add(ending);

            return ending;
        }

        /// <summary>One integer, under the one culture this project formats with.</summary>
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
