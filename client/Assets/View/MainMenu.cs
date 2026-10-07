using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace View
{
    [DisallowMultipleComponent]
    public sealed class MainMenu : MonoBehaviour
    {
        public const string StartLabel = "Start a run";

        public const string SettingsLabel = "Settings";

        public const string QuitLabel = "Quit";

        private const int PanelSortingOrder = 4;

        private const float ButtonWidth = 420f;

        private const float ButtonHeight = 72f;

        private const int ButtonFontSize = 32;

        private MatchRoot _root;

        private ulong _seed;

        private MatchArt _art;

        private LaunchSettings _launch;

        private PanelSettings _panel;

        private VisualElement _screen;

        public Button StartButton { get; private set; }

        public Button SettingsButton { get; private set; }

        public Button QuitButton { get; private set; }

        public SettingsScreen Settings { get; private set; }

        public UIDocument Document { get; private set; }

        public bool IsShown => _screen.style.display != DisplayStyle.None;

        private bool RunIsGoing => _root.Loop != null;

        public static MainMenu Build(MatchRoot root, ulong seed, MatchArt art, LaunchSettings launch)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (art == null) throw new ArgumentNullException(nameof(art));
            if (launch == null) throw new ArgumentNullException(nameof(launch));

            var host = new GameObject("MainMenu");
            host.transform.SetParent(root.transform, worldPositionStays: false);

            var menu = host.AddComponent<MainMenu>();
            menu._root = root;
            menu._seed = seed;
            menu._art = art;
            menu._launch = launch;
            menu.Assemble(host.AddComponent<UIDocument>());

            return menu;
        }

        public void StartRun()
        {
            Show(false);
            _root.BeginRun(_seed, _launch.LobbyFolder, _art);
        }

        public void OpenSettings()
        {
            Show(false);
            Settings.Open();
        }

        public void Quit() => Application.Quit();

        private void Update()
        {
            if (RunIsGoing && EscapePressed())
            {
                ToggleSettings();
            }

            if (_root.Pointer != null)
            {
                _root.Pointer.enabled = !Settings.IsShown;
            }
        }

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        private static bool EscapePressed() =>
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        private void ToggleSettings()
        {
            if (Settings.IsShown)
            {
                Settings.Close();
            }
            else
            {
                Settings.Open();
            }
        }

        private void Show(bool shown) =>
            _screen.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        private void Assemble(UIDocument document)
        {
            _panel = RuntimePanel.Settings("Main menu panel", PanelSortingOrder);

            Document = document;
            document.panelSettings = _panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            _screen = RuntimePanel.Backdrop("Menu");
            document.rootVisualElement.Add(_screen);

            StartButton = AddButton(_screen, "Start", StartLabel, StartRun);
            SettingsButton = AddButton(_screen, "Settings", SettingsLabel, OpenSettings);
            QuitButton = AddButton(_screen, "Quit", QuitLabel, Quit);
            _screen.Add(Unsigned.Mark(StartLabel + ", " + SettingsLabel + ", " + QuitLabel));

            Settings = SettingsScreen.Build(_root.transform, _launch);
            Settings.Closed += () => Show(!RunIsGoing);
        }


        private static Button AddButton(VisualElement screen, string name, string words, Action pressed)
        {
            var button = new Button(pressed) { name = name, text = words };

            button.style.width = ButtonWidth;
            button.style.height = ButtonHeight;
            button.style.marginBottom = RuntimePanel.ControlGap;
            button.style.backgroundColor = RuntimePanel.ControlColor;
            button.style.color = RuntimePanel.LabelColor;
            button.style.fontSize = ButtonFontSize;

            screen.Add(button);

            return button;
        }
    }
}
