using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
    /// canvas dragged in would work, would look right, and would put the one
    /// piece of chrome this project has into serialized YAML that no diff can
    /// be read. The numbers below are chrome rather than playfield, which is
    /// why they are here rather than in <see cref="SceneFraming"/> or
    /// <see cref="MatchTuning"/> — change every one of them and neither the
    /// match nor the playfield looks any different.
    /// </para>
    /// <para>
    /// <b>uGUI with the Input System's UI module, and not IMGUI.</b> That is
    /// forced rather than preferred: this project is set to the new input
    /// system alone, and the Input System cannot generate input for IMGUI at
    /// all. An <c>OnGUI</c> bar would compile, draw, and never respond to a
    /// click.
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

        /// <summary>The resolution the bar is laid out at, and scaled from.</summary>
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

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

        private Text _pauseLabel;

        private Text _speedLabel;

        private int _speedIndex;

        /// <summary>The scrub bar. Dragging it seeks.</summary>
        public Slider Scrubber { get; private set; }

        /// <summary>Which tick is on screen, in words.</summary>
        public Text Readout { get; private set; }

        /// <summary>Every button on the bar, in the order they are laid out.</summary>
        public IReadOnlyList<Button> Buttons => _buttons;

        /// <summary>The playback this bar drives.</summary>
        public PlaybackController Playback => _playback;

        /// <summary>
        /// Builds the bar under <paramref name="parent"/>, driving
        /// <paramref name="playback"/>.
        /// </summary>
        public static PlaybackControls Build(Transform parent, PlaybackController playback)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (playback == null) throw new ArgumentNullException(nameof(playback));

            var host = new GameObject("PlaybackControls", typeof(RectTransform));
            host.transform.SetParent(parent, worldPositionStays: false);

            var canvas = host.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Overlay, so the bar never lands in a camera's render texture --
            // which is what keeps the committed match frames free of chrome
            // without the capture tool having to know this class exists.
            var scaler = host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 1f;

            host.AddComponent<GraphicRaycaster>();

            var controls = host.AddComponent<PlaybackControls>();
            controls.Assemble(playback);

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

        // ---------------------------------------------------------------
        // Building
        // ---------------------------------------------------------------

        private void Assemble(PlaybackController playback)
        {
            _playback = playback;

            EnsureEventSystem();

            RectTransform bar = Bar();

            float x = Margin;
            x = AddButton(bar, "Pause", x, out _pauseLabel, TogglePause);
            x = AddButton(bar, SpeedLabel(1f), x, out _speedLabel, CycleSpeed);
            x = AddButton(bar, "To the end", x, out _, JumpToTheEnd);

            Readout = AddReadout(bar);
            Scrubber = AddScrubber(bar, x, Margin + ReadoutWidth + ButtonGap);

            Follow();
        }

        /// <summary>
        /// The one event system. Built here rather than authored, and only if
        /// the scene has none — a second one logs a warning and swallows every
        /// click between them.
        /// </summary>
        /// <remarks>
        /// The module assigns itself the Input System package's default UI
        /// actions when it is enabled, so there is no actions asset to author,
        /// import or keep in step with anything.
        /// </remarks>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var host = new GameObject("EventSystem");
            host.transform.SetParent(transform, worldPositionStays: false);
            host.AddComponent<EventSystem>();
            host.AddComponent<InputSystemUIInputModule>();
        }

        private RectTransform Bar()
        {
            RectTransform bar = Rect("Bar", transform);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, BarHeight);

            // Opaque to the raycaster, so a click on the bar stops at the bar
            // rather than falling through onto the playfield behind it.
            Fill(bar, BarColor);

            return bar;
        }

        /// <summary>
        /// One button at <paramref name="x"/> from the left, returning where
        /// the next one starts.
        /// </summary>
        private float AddButton(RectTransform bar, string label, float x, out Text text, Action pressed)
        {
            RectTransform host = Rect("Button " + label, bar);
            host.anchorMin = new Vector2(0f, 0.5f);
            host.anchorMax = new Vector2(0f, 0.5f);
            host.pivot = new Vector2(0f, 0.5f);
            host.anchoredPosition = new Vector2(x, 0f);
            host.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            var button = host.gameObject.AddComponent<Button>();
            button.targetGraphic = Fill(host, ButtonColor);
            button.onClick.AddListener(() => pressed());

            text = Label(host, label, TextAnchor.MiddleCenter);

            _buttons.Add(button);

            return x + ButtonWidth + ButtonGap;
        }

        private Text AddReadout(RectTransform bar)
        {
            RectTransform host = Rect("Readout", bar);
            host.anchorMin = new Vector2(1f, 0.5f);
            host.anchorMax = new Vector2(1f, 0.5f);
            host.pivot = new Vector2(1f, 0.5f);
            host.anchoredPosition = new Vector2(-Margin, 0f);
            host.sizeDelta = new Vector2(ReadoutWidth, ButtonHeight);

            return Label(host, string.Empty, TextAnchor.MiddleRight);
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
        private Slider AddScrubber(RectTransform bar, float left, float right)
        {
            RectTransform host = Rect("Scrubber", bar);
            host.anchorMin = new Vector2(0f, 0.5f);
            host.anchorMax = new Vector2(1f, 0.5f);
            host.pivot = new Vector2(0.5f, 0.5f);
            host.offsetMin = new Vector2(left, -ScrubberHeight * 0.5f);
            host.offsetMax = new Vector2(-right, ScrubberHeight * 0.5f);

            Fill(host, TrackColor);

            RectTransform fillArea = Stretch(Rect("Fill Area", host), ScrubberHeight * 0.5f);
            RectTransform fill = Rect("Fill", fillArea);
            fill.sizeDelta = Vector2.zero;
            Fill(fill, PlayedColor);

            RectTransform handleArea = Stretch(Rect("Handle Slide Area", host), ScrubberHeight * 0.5f);
            RectTransform handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(ScrubberHeight, 0f);
            Image handleImage = Fill(handle, HandleColor);

            var slider = host.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0f;
            slider.maxValue = _playback.FinalTick;
            slider.SetValueWithoutNotify(_playback.Tick);
            slider.onValueChanged.AddListener(Scrub);

            return slider;
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
        /// pointer, <see cref="Advance"/> immediately walks it forward again,
        /// and which of the two the slider ends up showing depends on an update
        /// order nobody controls. Pausing removes the fight rather than
        /// arbitrating it, and it is what anybody who has dragged a video
        /// scrubber already expects.
        /// </remarks>
        private void Scrub(float value)
        {
            Pause(true);
            _playback.SeekTo(Mathf.RoundToInt(value));
        }

        private void TogglePause() => Pause(!_playback.IsPaused);

        private void Pause(bool paused)
        {
            _playback.IsPaused = paused;
            _pauseLabel.text = paused ? "Play" : "Pause";
        }

        private void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % Speeds.Length;
            _playback.Speed = Speeds[_speedIndex];
            _speedLabel.text = SpeedLabel(Speeds[_speedIndex]);
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

        // ---------------------------------------------------------------
        // Plumbing
        // ---------------------------------------------------------------

        private static RectTransform Rect(string name, Transform parent)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, worldPositionStays: false);

            return (RectTransform)host.transform;
        }

        /// <summary>Stretches a rect over its parent, inset vertically.</summary>
        private static RectTransform Stretch(RectTransform rect, float horizontalInset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalInset, 0f);
            rect.offsetMax = new Vector2(-horizontalInset, 0f);

            return rect;
        }

        private static Image Fill(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            return image;
        }

        /// <summary>
        /// A label filling its host. The font is the engine's built-in one —
        /// the same one uGUI reaches for itself — so there is no font asset to
        /// import and none to be missing from a build.
        /// </summary>
        private static Text Label(RectTransform parent, string content, TextAnchor alignment)
        {
            RectTransform rect = Stretch(Rect("Label", parent), 0f);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = LabelSize;
            text.color = LabelColor;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;

            return text;
        }
    }
}
