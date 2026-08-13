using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// The only user interface this project has: a scrub bar, a speed control
    /// and a jump to the end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists to be the acid test, not to be a game.</b> The claim the
    /// whole architecture rests on — that the view is a pure function of
    /// simulation state — is otherwise something you can only agree with. Drag
    /// the slider backwards and the legs walk backwards, or they do not, and
    /// the bet is settled by a human in a second.
    /// </para>
    /// <para>
    /// <b>Nothing here can reach the simulation.</b> Every control on this bar
    /// goes to <see cref="PlaybackController"/> and asks for a tick; none of
    /// them is an input to a match. That is the same rule the camera rig lives
    /// under and it is checkable the same way — there is no argument, field or
    /// method here through which a mouse could change what happens.
    /// </para>
    /// <para>
    /// <b>Built in code, from constants, like the camera and the light.</b> The
    /// scene holds one empty root object and this is not authored into it: a
    /// panel dragged in would work, would look right, and would put the one
    /// piece of chrome this project has into serialized YAML that no diff can
    /// be read. The numbers below are chrome rather than playfield, which is
    /// why they are here rather than in <see cref="SceneFraming"/> or
    /// <see cref="MatchTuning"/> — change every one of them and neither the
    /// match nor the playfield looks any different.
    /// </para>
    /// <para>
    /// <b>UI Toolkit, and the scene runs no other UI system.</b> A runtime
    /// panel takes its pointer input from the Input System package directly, so
    /// there is no event system, no raycaster and no canvas anywhere in the
    /// scene — which is what keeps this project's one HUD on one set of
    /// controls, layout rules and hit-testing rules as it grows a header, a
    /// tower palette and a wave bar.
    /// </para>
    /// <para>
    /// <b>The panel has no target texture</b>, so it draws over everything
    /// after the cameras have finished. That is what keeps the committed match
    /// frames free of chrome without the capture tool having to know this class
    /// exists.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlaybackControls : MonoBehaviour
    {
        /// <summary>
        /// The speeds the fast-forward button cycles through, in the order it
        /// cycles them. Normal speed is first, so the button always comes back
        /// round to where it started.
        /// </summary>
        public static readonly float[] Speeds = { 1f, 2f, 4f, 8f };

        /// <summary>
        /// The theme's path inside <c>Resources</c>, without extension.
        /// </summary>
        /// <remarks>
        /// A <c>Resources</c> asset because this is loaded by code that has no
        /// scene to be handed it by — <see cref="Build"/> is called by a test
        /// fixture as often as by <see cref="MatchRoot"/> — and because it has
        /// to survive into a player build. Same reasoning, and the same
        /// exception to the objection, as
        /// <see cref="ResourcesMatchArtSource"/>.
        /// </remarks>
        private const string ThemeResourcePath = "RuntimeTheme";

        /// <summary>The resolution the bar is laid out at, and scaled from.</summary>
        private static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        private const float BarHeight = 88f;

        private const float Margin = 24f;

        private const float ButtonWidth = 132f;

        private const float ButtonHeight = 48f;

        private const float ButtonGap = 12f;

        private const float ReadoutWidth = 320f;

        private const float ScrubberHeight = 24f;

        private const int LabelSize = 22;

        private static readonly Color BarColor = new Color(0.06f, 0.07f, 0.09f, 0.86f);

        private static readonly Color ButtonColor = new Color(0.22f, 0.25f, 0.3f, 1f);

        private static readonly Color TrackColor = new Color(0.16f, 0.18f, 0.22f, 1f);

        private static readonly Color PlayedColor = new Color(0.45f, 0.68f, 0.85f, 1f);

        private static readonly Color HandleColor = new Color(0.92f, 0.94f, 0.97f, 1f);

        private static readonly Color LabelColor = new Color(0.9f, 0.92f, 0.95f, 1f);

        private readonly List<Button> _buttons = new List<Button>();

        private PlaybackController _playback;

        private PanelSettings _panel;

        private Button _pauseButton;

        private Button _speedButton;

        /// <summary>The stretch of scrub bar behind the handle.</summary>
        private VisualElement _played;

        private int _speedIndex;

        /// <summary>The scrub bar. Dragging it seeks.</summary>
        public SliderInt Scrubber { get; private set; }

        /// <summary>Which tick is on screen, in words.</summary>
        public Label Readout { get; private set; }

        /// <summary>The panel the bar is drawn on.</summary>
        public UIDocument Document { get; private set; }

        /// <summary>Every button on the bar, in the order they are laid out.</summary>
        public IReadOnlyList<Button> Buttons => _buttons;

        /// <summary>
        /// Builds the bar under <paramref name="parent"/>, driving
        /// <paramref name="playback"/>.
        /// </summary>
        public static PlaybackControls Build(Transform parent, PlaybackController playback)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (playback == null) throw new ArgumentNullException(nameof(playback));

            var host = new GameObject("PlaybackControls");
            host.transform.SetParent(parent, worldPositionStays: false);

            var controls = host.AddComponent<PlaybackControls>();
            controls.Assemble(host.AddComponent<UIDocument>(), playback);

            return controls;
        }

        /// <summary>
        /// Puts the bar back in step with the match. Called every frame after
        /// advancing, and by the buttons after a seek.
        /// </summary>
        /// <remarks>
        /// Written without notifying, so following the match is not mistaken
        /// for dragging the slider. The alternative — a flag saying "ignore the
        /// next callback" — is the same bug waiting for the frame the counts
        /// do not match.
        /// </remarks>
        public void Follow()
        {
            Scrubber.SetValueWithoutNotify(_playback.Tick);

            _played.style.width = Length.Percent(
                _playback.FinalTick > 0 ? 100f * _playback.Tick / _playback.FinalTick : 0f);

            Readout.text = string.Format(
                CultureInfo.InvariantCulture,
                "tick {0} / {1}{2}",
                _playback.Tick,
                _playback.FinalTick,
                _playback.IsFinished ? "  (resolved)" : string.Empty);
        }

        private void Update()
        {
            _playback.Advance(Time.deltaTime);
            Follow();
        }

        /// <summary>
        /// The settings object is made here rather than loaded, so it is
        /// destroyed here too — nothing else holds it and an orphaned one
        /// outlives the play session that made it.
        /// </summary>
        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        // ---------------------------------------------------------------
        // Building
        // ---------------------------------------------------------------

        private void Assemble(UIDocument document, PlaybackController playback)
        {
            _playback = playback;
            _panel = Panel();

            Document = document;
            document.panelSettings = _panel;

            // The bar picks; the rest of the screen does not, so a pointer
            // anywhere else reaches whatever is behind the panel.
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            VisualElement bar = Bar();
            document.rootVisualElement.Add(bar);

            _pauseButton = AddButton(bar, "Pause", TogglePause);
            _speedButton = AddButton(bar, SpeedLabel(1f), CycleSpeed);
            AddButton(bar, "To the end", JumpToTheEnd);

            Scrubber = AddScrubber(bar);
            Readout = AddReadout(bar);

            Follow();
        }

        /// <summary>
        /// The one panel, scaled from <see cref="ReferenceResolution"/> the way
        /// the uGUI bar before it was.
        /// </summary>
        /// <remarks>
        /// A theme style sheet is not decoration: it is where the default
        /// controls get their font and where the slider gets the absolute
        /// positioning its track and handle are laid out with. A panel without
        /// one draws a bar of invisible text and a slider with nothing to drag.
        /// </remarks>
        private static PanelSettings Panel()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "Playback panel";
            settings.themeStyleSheet = Theme();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = ReferenceResolution;
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;

            // 1 is height, matching the uGUI scaler this replaced: the bar is
            // anchored to the bottom edge and its height is the measurement
            // that has to stay put as the window changes shape.
            settings.match = 1f;

            return settings;
        }

        private static ThemeStyleSheet Theme()
        {
            var theme = Resources.Load<ThemeStyleSheet>(ThemeResourcePath);

            if (theme == null)
            {
                throw new InvalidOperationException(
                    "No theme style sheet at Resources/" + ThemeResourcePath
                    + ". It is committed, so a checkout without it is incomplete rather than "
                    + "unconfigured.");
            }

            return theme;
        }

        /// <summary>
        /// The bar itself: a row across the bottom of the screen, opaque, so a
        /// click on it stops at it rather than falling through onto the
        /// playfield behind.
        /// </summary>
        private static VisualElement Bar()
        {
            var bar = new VisualElement { name = "Bar" };

            bar.style.position = Position.Absolute;
            bar.style.left = 0f;
            bar.style.right = 0f;
            bar.style.bottom = 0f;
            bar.style.height = BarHeight;
            bar.style.paddingLeft = Margin;
            bar.style.paddingRight = Margin;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = BarColor;

            return bar;
        }

        private Button AddButton(VisualElement bar, string label, Action pressed)
        {
            var button = new Button(pressed) { text = label };

            button.style.width = ButtonWidth;
            button.style.height = ButtonHeight;
            button.style.flexShrink = 0f;
            button.style.backgroundColor = ButtonColor;
            GapAfter(button);
            Lettering(button);

            bar.Add(button);
            _buttons.Add(button);

            return button;
        }

        /// <summary>
        /// The scrub bar, filling whatever is left between the buttons and the
        /// readout.
        /// </summary>
        /// <remarks>
        /// Whole numbers, because the thing being scrubbed through is ticks and
        /// there is nothing between two of them to land on. That also bounds
        /// how often a drag seeks: one re-simulation per tick crossed, at most
        /// one a frame.
        /// </remarks>
        private SliderInt AddScrubber(VisualElement bar)
        {
            var slider = new SliderInt(0, _playback.FinalTick) { name = "Scrubber" };

            slider.style.flexGrow = 1f;
            slider.style.height = ScrubberHeight;
            GapAfter(slider);

            VisualElement track = PartOf(slider, BaseSlider<int>.trackerUssClassName);
            track.style.backgroundColor = TrackColor;

            VisualElement handle = PartOf(slider, BaseSlider<int>.draggerUssClassName);
            handle.style.backgroundColor = HandleColor;
            handle.style.width = ScrubberHeight;

            // What the handle's centre can actually reach: it stops half its
            // own width short of each end of the track. Measuring the played
            // stretch inside this rather than across the whole track is what
            // keeps the colour's edge under the handle at every tick.
            var travel = new VisualElement { name = "Travel", pickingMode = PickingMode.Ignore };
            travel.style.position = Position.Absolute;
            travel.style.left = ScrubberHeight * 0.5f;
            travel.style.right = ScrubberHeight * 0.5f;
            travel.style.top = 0f;
            travel.style.bottom = 0f;
            track.Add(travel);

            // Behind the handle rather than in front of it, and picking
            // nothing, so the stretch already played is a colour on the track
            // and not a second thing a pointer can land on.
            _played = new VisualElement { name = "Played", pickingMode = PickingMode.Ignore };
            _played.style.position = Position.Absolute;
            _played.style.left = 0f;
            _played.style.top = 0f;
            _played.style.bottom = 0f;
            _played.style.backgroundColor = PlayedColor;
            travel.Add(_played);

            slider.SetValueWithoutNotify(_playback.Tick);
            slider.RegisterValueChangedCallback(changed => Scrub(changed.newValue));

            bar.Add(slider);

            return slider;
        }

        private static Label AddReadout(VisualElement bar)
        {
            var readout = new Label { name = "Readout" };

            readout.style.width = ReadoutWidth;
            readout.style.flexShrink = 0f;
            readout.style.unityTextAlign = TextAnchor.MiddleRight;
            Lettering(readout);

            bar.Add(readout);

            return readout;
        }

        /// <summary>
        /// The gap after a control, and no margin anywhere else on it.
        /// </summary>
        /// <remarks>
        /// The theme gives its controls margins of their own. Clearing them is
        /// what makes the spacing along this row one number in this file rather
        /// than that number plus whatever the theme thought.
        /// </remarks>
        private static void GapAfter(VisualElement element)
        {
            element.style.marginLeft = 0f;
            element.style.marginTop = 0f;
            element.style.marginBottom = 0f;
            element.style.marginRight = ButtonGap;
        }

        /// <summary>The one text colour and size on the bar.</summary>
        private static void Lettering(VisualElement element)
        {
            element.style.color = LabelColor;
            element.style.fontSize = LabelSize;
        }

        /// <summary>
        /// One of the slider's own parts, by the class name the theme lays it
        /// out under.
        /// </summary>
        /// <remarks>
        /// Named rather than left alone, because the parts are the theme's and
        /// not this file's: a theme that stopped producing one would otherwise
        /// surface as a <c>NullReferenceException</c> from a style assignment,
        /// which says nothing about where to look.
        /// </remarks>
        private static VisualElement PartOf(SliderInt slider, string ussClassName)
        {
            VisualElement part = slider.Q(className: ussClassName);

            if (part == null)
            {
                throw new InvalidOperationException(
                    "The scrub bar has no part classed " + ussClassName + ", so Resources/"
                    + ThemeResourcePath + " is not the theme this bar is coloured against.");
            }

            return part;
        }

        // ---------------------------------------------------------------
        // What the controls do
        // ---------------------------------------------------------------

        /// <summary>
        /// The scrub bar moved. Every value change that is not
        /// <see cref="Follow"/> is a person dragging, and a person dragging is
        /// a seek.
        /// </summary>
        /// <remarks>
        /// <b>Dragging pauses.</b> Without it the drag and the clock fight over
        /// the same frame — the seek puts the match on the tick under the
        /// pointer, <see cref="Update"/> immediately walks it forward again,
        /// and which of the two the slider ends up showing depends on an update
        /// order nobody controls. Pausing removes the fight rather than
        /// arbitrating it, and it is what anybody who has dragged a video
        /// scrubber already expects.
        /// </remarks>
        private void Scrub(int tick)
        {
            Pause(true);
            _playback.SeekTo(tick);
        }

        private void TogglePause() => Pause(!_playback.IsPaused);

        private void Pause(bool paused)
        {
            _playback.IsPaused = paused;
            _pauseButton.text = paused ? "Play" : "Pause";
        }

        private void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % Speeds.Length;
            _playback.Speed = Speeds[_speedIndex];
            _speedButton.text = SpeedLabel(Speeds[_speedIndex]);
        }

        /// <summary>
        /// Jumps to the end. A seek like any other, which is the point: the
        /// match resolving instantly and the match being scrubbed are the same
        /// call with different arguments.
        /// </summary>
        private void JumpToTheEnd()
        {
            _playback.SeekToEnd();
            Follow();
        }

        private static string SpeedLabel(float speed) =>
            speed.ToString("0.##", CultureInfo.InvariantCulture) + "x speed";
    }
}
