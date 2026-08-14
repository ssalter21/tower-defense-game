using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// The two views of the Offence and Defence Results Screen, and the control
    /// that moves between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A round is two matches and this picks which one is drawn.</b> A wave
    /// is resolved against every opponent in both directions and both are
    /// scored — the offence is what feeds the purse, the defence is what health
    /// is spent on — so both fights have already happened by the time this bar
    /// is on screen. Pressing either button rebuilds a match that was resolved
    /// when the round was committed. See <see cref="RunLoop.Watch"/>, which is
    /// the whole of what a press does.
    /// </para>
    /// <para>
    /// <b>Defence is the default and it is the core loop.</b> You build towers,
    /// you press Done, and what you watch is those towers against somebody
    /// else's wave. Offence is the other half of the same round: the wave you
    /// composed, walking into somebody else's defence. It is reachable because
    /// it is already simulated and a player who spent a purse on a wave should
    /// be able to see what it did — but it is not what the screen opens on,
    /// which is #206.
    /// </para>
    /// <para>
    /// <b>The player's spelling and the simulation's are different on purpose.</b>
    /// The labels here are British — "Offence", "Defence" — like the rest of
    /// this project's prose. The simulation's identifiers are not:
    /// <c>Side.Attacking</c>, <c>Side.Defending</c> and <c>RoundOrders.Defense</c>
    /// are spelled the other way and stay that way, along with
    /// <c>content/defense.txt</c> and the golden files named after it. This bar
    /// is where the two meet, and the seam is cheaper than renaming a record
    /// format for a spelling.
    /// </para>
    /// <para>
    /// <b>It is not a mode and it is not a forecast.</b> Switching does not
    /// advance the run, does not touch the phase being composed and cannot
    /// reach the simulation with anything: what it asks for is a copy of a
    /// fight that is already over. ADR-0051.
    /// </para>
    /// <para>
    /// <b>Words, and no picture.</b> There is no icon, no glyph and no thumbnail
    /// standing in for a side — the same rule the wave bar and the tower palette
    /// live under, and the same open seam. No type ids on screen and none of the
    /// record's vocabulary either.
    /// </para>
    /// <para>
    /// <b>Built once and hidden rather than built per round.</b> The match under
    /// it is torn down and stood up again on every switch, so a bar that came
    /// and went with the match would be a bar destroying itself from inside its
    /// own button callback. It follows the loop instead, exactly as
    /// <see cref="RunHeader"/> does.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ResultsSwitch : MonoBehaviour
    {
        /// <summary>What the button that shows your towers says.</summary>
        public const string DefenceLabel = "Defence";

        /// <summary>What the button that shows your wave says.</summary>
        public const string OffenceLabel = "Offence";

        /// <summary>Under the header, over the playback bar and the board.</summary>
        private const int PanelSortingOrder = 2;

        /// <summary>How tall the row is.</summary>
        public const float BarHeight = 64f;

        private const float ButtonWidth = 156f;

        private const float ButtonHeight = 44f;

        private const int LabelSize = 22;

        /// <summary>What the view that is on screen is drawn in.</summary>
        private static readonly Color ShowingColor = new Color(0.36f, 0.46f, 0.6f, 1f);

        private RunLoop _loop;

        private PanelSettings _panel;

        /// <summary>The row the two buttons sit in.</summary>
        private VisualElement _row;

        /// <summary>Your towers against an opponent's wave. The default.</summary>
        public Button Defence { get; private set; }

        /// <summary>Your wave against an opponent's defence.</summary>
        public Button Offence { get; private set; }

        /// <summary>The panel the row is drawn on.</summary>
        public UIDocument Document { get; private set; }

        /// <summary>
        /// Builds the row under <paramref name="parent"/>, following
        /// <paramref name="loop"/>.
        /// </summary>
        public static ResultsSwitch Build(Transform parent, RunLoop loop)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (loop == null) throw new ArgumentNullException(nameof(loop));

            var host = new GameObject("ResultsSwitch");
            host.transform.SetParent(parent, worldPositionStays: false);

            var control = host.AddComponent<ResultsSwitch>();
            control.Assemble(host.AddComponent<UIDocument>(), loop);

            return control;
        }

        /// <summary>
        /// Puts the row back in step with the loop: up only while a round is
        /// being watched, and lit on whichever of the two is on screen.
        /// </summary>
        public void Follow()
        {
            bool watching = _loop.Mode == RunMode.Watching;

            _row.style.display = watching ? DisplayStyle.Flex : DisplayStyle.None;

            Defence.style.backgroundColor =
                _loop.WatchingAttack ? RuntimePanel.ControlColor : ShowingColor;

            Offence.style.backgroundColor =
                _loop.WatchingAttack ? ShowingColor : RuntimePanel.ControlColor;
        }

        /// <summary>Whether a screen point lands on this row rather than on the board behind it.</summary>
        public bool Covers(Vector2 screenPoint) => RuntimePanel.Covers(Document, screenPoint);

        private void Update() => Follow();

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        private void Assemble(UIDocument document, RunLoop loop)
        {
            _loop = loop;
            _panel = RuntimePanel.Settings("Results panel", PanelSortingOrder);

            Document = document;
            document.panelSettings = _panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            _row = Row();
            document.rootVisualElement.Add(_row);

            // Defence first, because it is what the screen opens on and what
            // the health beside it is spent on. The screen's name reads the
            // other way round; the order the player presses them in follows the
            // default rather than the title.
            Defence = AddButton(_row, DefenceLabel, () => loop.Watch(attacking: false));
            Offence = AddButton(_row, OffenceLabel, () => loop.Watch(attacking: true));

            Follow();
        }

        /// <summary>
        /// The row itself: tucked under the header, as wide as the two buttons
        /// and no wider, so the board behind it is covered by as little as
        /// possible.
        /// </summary>
        private static VisualElement Row()
        {
            var row = new VisualElement { name = "Results" };

            row.style.position = Position.Absolute;
            row.style.left = 0f;
            row.style.top = RunHeader.BarHeight;
            row.style.height = BarHeight;
            row.style.paddingLeft = RuntimePanel.Margin;
            row.style.paddingRight = RuntimePanel.Margin;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = RuntimePanel.BarColor;

            return row;
        }

        private static Button AddButton(VisualElement row, string label, Action pressed)
        {
            var button = new Button(pressed) { name = label, text = label };

            button.style.width = ButtonWidth;
            button.style.height = ButtonHeight;
            button.style.flexShrink = 0f;
            button.style.marginLeft = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.marginRight = RuntimePanel.ControlGap;
            button.style.color = RuntimePanel.LabelColor;
            button.style.fontSize = LabelSize;

            row.Add(button);

            return button;
        }
    }
}
