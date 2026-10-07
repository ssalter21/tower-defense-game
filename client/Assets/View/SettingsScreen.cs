using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    [DisallowMultipleComponent]
    public sealed class SettingsScreen : MonoBehaviour
    {
        public const string CostName = "Cost";

        public const string PlayerNameLabel = "Player name";

        public const string LobbyFolderLabel = "Lobby folder";

        public const string DisplayLabel = "Display";

        public const string ResolutionLabel = "Resolution";

        public const string BackLabel = "Back";

        public const string FullScreenChoice = "Full screen";

        public const string WindowedChoice = "Windowed";

        public const string PlayerNameCost =
            "Not honoured yet. Kept until the game closes, and nothing reads it: it reaches the game when "
            + "the lobby folder writes a name on each stored round.";

        public const string LobbyFolderCost =
            "Honoured. The next run takes its opponents from this folder's pool and writes its rounds and "
            + "its script here. Kept until the game closes; remembering it costs a saved preference.";

        public const string DisplayCost =
            "Honoured the moment it changes, and the player remembers it between launches.";

        public const string ResolutionCost =
            "Honoured the moment it changes, and the player remembers it between launches. Windowed, the "
            + "window keeps this size: letting it be dragged is one project setting.";

        private const int PanelSortingOrder = 5;

        private const float CardWidth = 1180f;

        private const float CardPadding = 40f;

        private const float NameWidth = 260f;

        private const float RowGap = 28f;

        private const int NameFontSize = 28;

        private const int CostFontSize = 20;

        private const float BackWidth = 240f;

        private const float BackHeight = 64f;

        private static readonly Color CardColor = new Color(0.06f, 0.07f, 0.09f, 0.96f);

        private static readonly Color CostColor = new Color(0.62f, 0.66f, 0.72f, 1f);

        private readonly List<VisualElement> _rows = new List<VisualElement>();

        private LaunchSettings _launch;

        private PanelSettings _panel;

        private VisualElement _screen;

        private Resolution[] _resolutions;

        public event Action Closed;

        public TextField PlayerName { get; private set; }

        public TextField LobbyFolder { get; private set; }

        public DropdownField Display { get; private set; }

        public DropdownField ScreenSize { get; private set; }

        public Button Back { get; private set; }

        public IReadOnlyList<VisualElement> Rows => _rows;

        public UIDocument Document { get; private set; }

        public bool IsShown => _screen.style.display != DisplayStyle.None;

        public static SettingsScreen Build(Transform parent, LaunchSettings launch)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (launch == null) throw new ArgumentNullException(nameof(launch));

            var host = new GameObject("SettingsScreen");
            host.transform.SetParent(parent, worldPositionStays: false);

            var screen = host.AddComponent<SettingsScreen>();
            screen._launch = launch;
            screen.Assemble(host.AddComponent<UIDocument>());

            return screen;
        }

        public void Open()
        {
            PlayerName.SetValueWithoutNotify(_launch.PlayerName);
            LobbyFolder.SetValueWithoutNotify(_launch.LobbyFolder);
            _screen.style.display = DisplayStyle.Flex;
        }

        public void Close()
        {
            _screen.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        private void Assemble(UIDocument document)
        {
            _panel = RuntimePanel.Settings("Settings panel", PanelSortingOrder);

            Document = document;
            document.panelSettings = _panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            _screen = RuntimePanel.Backdrop("SettingsBackdrop");
            _screen.style.display = DisplayStyle.None;
            document.rootVisualElement.Add(_screen);

            VisualElement card = Card();
            _screen.Add(card);

            PlayerName = new TextField { value = _launch.PlayerName };
            PlayerName.RegisterValueChangedCallback(changed => _launch.PlayerName = changed.newValue);
            AddRow(card, "PlayerName", PlayerNameLabel, PlayerName, PlayerNameCost);

            LobbyFolder = new TextField { value = _launch.LobbyFolder };
            LobbyFolder.RegisterValueChangedCallback(changed => _launch.LobbyFolder = changed.newValue);
            AddRow(card, "LobbyFolder", LobbyFolderLabel, LobbyFolder, LobbyFolderCost);

            Display = new DropdownField(
                new List<string> { FullScreenChoice, WindowedChoice },
                Screen.fullScreenMode == FullScreenMode.Windowed ? WindowedChoice : FullScreenChoice);
            Display.RegisterValueChangedCallback(changed => Screen.fullScreenMode = ModeFor(changed.newValue));
            AddRow(card, "Display", DisplayLabel, Display, DisplayCost);

            _resolutions = OfferedResolutions();
            ScreenSize = new DropdownField(_resolutions.Select(Text).ToList(), CurrentSizeIndex());
            ScreenSize.RegisterValueChangedCallback(_ => ApplySize(_resolutions[ScreenSize.index]));
            AddRow(card, "Resolution", ResolutionLabel, ScreenSize, ResolutionCost);

            Back = RuntimePanel.ControlButton("Back", BackLabel, Close, BackWidth, BackHeight, NameFontSize);
            Back.style.alignSelf = Align.Center;
            Back.style.marginTop = RowGap;
            card.Add(Back);

            card.Add(Unsigned.Mark(string.Join(", ", PlayerNameLabel, LobbyFolderLabel, DisplayLabel,
                FullScreenChoice, WindowedChoice, ResolutionLabel, BackLabel)));
        }

        private static FullScreenMode ModeFor(string choice) =>
            choice == WindowedChoice ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;

        private static void ApplySize(Resolution size) =>
            Screen.SetResolution(size.width, size.height, Screen.fullScreenMode);

        private static Resolution[] OfferedResolutions()
        {
            var current = new Resolution { width = Screen.width, height = Screen.height };

            return Screen.resolutions
                .Append(current)
                .GroupBy(size => (size.width, size.height))
                .Select(sizes => sizes.First())
                .OrderBy(size => size.width)
                .ThenBy(size => size.height)
                .ToArray();
        }

        private int CurrentSizeIndex() =>
            Array.FindIndex(_resolutions, size => size.width == Screen.width && size.height == Screen.height);

        private static string Text(Resolution size) => size.width + " x " + size.height;

        private void AddRow(VisualElement card, string name, string text, VisualElement control, string cost)
        {
            var row = new VisualElement { name = name };
            row.style.marginBottom = RowGap;

            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.style.flexDirection = FlexDirection.Row;
            line.style.alignItems = Align.Center;

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.width = NameWidth;
            label.style.color = RuntimePanel.LabelColor;
            label.style.fontSize = NameFontSize;

            control.style.flexGrow = 1f;
            control.style.fontSize = NameFontSize;

            var costLabel = new Label(cost) { name = CostName, pickingMode = PickingMode.Ignore };
            costLabel.style.marginLeft = NameWidth;
            costLabel.style.color = CostColor;
            costLabel.style.fontSize = CostFontSize;
            costLabel.style.whiteSpace = WhiteSpace.Normal;

            line.Add(label);
            line.Add(control);
            row.Add(line);
            row.Add(costLabel);
            card.Add(row);

            _rows.Add(row);
        }

        private static VisualElement Card()
        {
            var card = new VisualElement { name = "Settings" };

            card.style.width = CardWidth;
            card.style.paddingLeft = CardPadding;
            card.style.paddingRight = CardPadding;
            card.style.paddingTop = CardPadding;
            card.style.paddingBottom = CardPadding;
            card.style.backgroundColor = CardColor;

            return card;
        }
    }
}
