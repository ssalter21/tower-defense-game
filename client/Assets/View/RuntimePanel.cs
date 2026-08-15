using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// What every piece of chrome in this project is built on: one runtime
    /// panel, scaled the same way, coloured from the same few values, and asked
    /// the same question about where the pointer is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One panel recipe, because two would drift.</b> The playback bar and
    /// the tower palette are separate objects with separate lifetimes, and each
    /// carries a <see cref="PanelSettings"/> of its own — but a scale mode, a
    /// reference resolution and a match axis that disagreed between them would
    /// show up as two bars that come apart as the window changes shape, which is
    /// the kind of defect nobody sees until somebody resizes.
    /// </para>
    /// <para>
    /// <b>Built in code, from constants, like the camera and the light.</b> The
    /// scene holds one empty root object and none of this is authored into it: a
    /// panel dragged in would work, would look right, and would put this
    /// project's chrome into serialized YAML whose diffs cannot be read. The
    /// numbers here are chrome rather than playfield, which is why they are not
    /// in <see cref="SceneFraming"/> or <see cref="MatchTuning"/> — change every
    /// one and neither the match nor the playfield looks any different. The one
    /// asset in the way of that is the settings <see cref="LoadTextData"/>
    /// loads, which no panel is built from and which decides nothing about a
    /// layout.
    /// </para>
    /// <para>
    /// <b>UI Toolkit, and the scene runs no other UI system.</b> A runtime panel
    /// takes its pointer input from the Input System package directly, so there
    /// is no event system, no raycaster and no canvas anywhere in the scene.
    /// </para>
    /// </remarks>
    public static class RuntimePanel
    {
        /// <summary>
        /// The theme's path inside <c>Resources</c>, without extension.
        /// </summary>
        /// <remarks>
        /// A <c>Resources</c> asset because this is loaded by code that has no
        /// scene to be handed it by — the bars are built by a test fixture as
        /// often as by <see cref="MatchRoot"/> — and because it has to survive
        /// into a player build. Same reasoning, and the same exception to the
        /// objection, as <see cref="ResourcesMatchArtSource"/>.
        /// </remarks>
        public const string ThemeResourcePath = "RuntimeTheme";

        /// <summary>
        /// The committed settings asset's path inside <c>Resources</c>, without
        /// extension. Written by <c>tools/build-panel-settings.ps1</c>, loaded
        /// by <see cref="LoadTextData"/>.
        /// </summary>
        public const string SettingsResourcePath = "RuntimePanelSettings";

        /// <summary>The resolution every bar is laid out at, and scaled from.</summary>
        public static readonly Vector2Int ReferenceResolution = new Vector2Int(1920, 1080);

        /// <summary>The margin at the ends of a bar.</summary>
        public const float Margin = 24f;

        /// <summary>The gap between two controls in a row.</summary>
        public const float ControlGap = 12f;

        /// <summary>What a bar itself is drawn in — dark, and not quite opaque.</summary>
        public static Color BarColor => new Color(0.06f, 0.07f, 0.09f, 0.86f);

        /// <summary>What a button on a bar is drawn in.</summary>
        public static Color ControlColor => new Color(0.22f, 0.25f, 0.3f, 1f);

        /// <summary>The one text colour on the chrome.</summary>
        public static Color LabelColor => new Color(0.9f, 0.92f, 0.95f, 1f);

        /// <summary>
        /// One panel's settings, made rather than loaded — so whoever made it
        /// destroys it, and an orphaned one cannot outlive the play session.
        /// </summary>
        /// <param name="name">What it is called in a profiler and a leak report.</param>
        /// <param name="sortingOrder">
        /// Which panel draws over which. Bars that do not overlap can share an
        /// order; anything that floats over one needs a higher one.
        /// </param>
        public static PanelSettings Settings(string name, int sortingOrder = 0)
        {
            LoadTextData();

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = name;
            settings.themeStyleSheet = Theme();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = ReferenceResolution;
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.sortingOrder = sortingOrder;

            // 1 is height, matching the uGUI scaler the playback bar replaced:
            // every bar is anchored to the bottom edge and stacked on the one
            // below it, so height is the measurement that has to stay put as the
            // window changes shape.
            settings.match = 1f;

            return settings;
        }

        /// <summary>
        /// Loads the committed <see cref="PanelSettings"/> asset, and does
        /// nothing with it. Loading it is what gives the text engine the ICU
        /// data it shapes and measures every string through.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The call looks removable and is not.</b> Nothing is built from
        /// what it returns. Without it a player build measures every string as
        /// nothing: each label resolves to zero by zero, every bar of the HUD
        /// collapses, and what is drawn stops agreeing with what is hit-tested.
        /// The editor is immune, because its own process has the data loaded
        /// either way, so the run that fails when this goes is
        /// <c>tools/run-player-tests.ps1</c> and
        /// <c>Tests.PlayMode.ChromeLayoutTests</c> is the fixture.
        /// </para>
        /// <para>
        /// <b>Committing the asset is not enough on its own — that was
        /// measured.</b> A build carrying the asset with nothing loading it
        /// failed 36 of 132 player tests on the ICU error; the same build
        /// loading it and discarding it passed. The measurement and the engine's
        /// side of it are in
        /// <c>docs/research/a-player-build-measures-no-text-without-a-panelsettings-asset.md</c>,
        /// and what the asset costs is
        /// <c>docs/adr/0053-the-chrome-loads-one-settings-asset-for-its-text-data.md</c>.
        /// </para>
        /// <para>
        /// Written by <c>tools/build-panel-settings.ps1</c> rather than by hand,
        /// because what it carries is a reference into the engine's built-in
        /// resources that no hand-authored YAML could name.
        /// </para>
        /// </remarks>
        public static void LoadTextData() =>
            Required<PanelSettings>(SettingsResourcePath, "panel settings");

        /// <summary>
        /// The theme style sheet every panel is laid out against.
        /// </summary>
        /// <remarks>
        /// Not decoration: it is where the default controls get their font and
        /// where a slider gets the absolute positioning its track and handle are
        /// laid out with. A panel without one draws a bar of invisible text and a
        /// slider with nothing to drag.
        /// </remarks>
        public static ThemeStyleSheet Theme() =>
            Required<ThemeStyleSheet>(ThemeResourcePath, "theme style sheet");

        /// <summary>
        /// The asset at <paramref name="path"/> inside <c>Resources</c>, or a
        /// throw naming <paramref name="what"/> it was.
        /// </summary>
        /// <remarks>
        /// Both assets under <c>Resources</c> are committed, so a checkout
        /// missing one is incomplete rather than unconfigured — which is a
        /// different thing to tell whoever hits it than a null.
        /// </remarks>
        private static T Required<T>(string path, string what) where T : UnityEngine.Object
        {
            var asset = Resources.Load<T>(path);

            if (asset == null)
            {
                throw new InvalidOperationException(
                    "No " + what + " at Resources/" + path
                    + ". It is committed, so a checkout without it is incomplete rather than "
                    + "unconfigured.");
            }

            return asset;
        }

        /// <summary>
        /// Whether a screen point lands on something this panel picks, rather
        /// than on the board behind it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Asked of the panel, never of a rectangle.</b> A copy over here of
        /// where the chrome is would be wrong the first time a control moved, and
        /// the upgrade offer pinned to a hex has no fixed rectangle to compare
        /// against at all.
        /// </para>
        /// <para>
        /// Every panel in this project puts <see cref="PickingMode.Ignore"/> on
        /// its root, so this is false everywhere except on an actual control.
        /// </para>
        /// <para>
        /// <b>The vertical flip is this method's, and it was measured rather
        /// than assumed.</b> The input system reports a pointer with the origin
        /// at the bottom left and a panel is laid out from the top left, and
        /// <see cref="RuntimePanelUtils.ScreenToPanel"/> does <i>not</i> turn one
        /// into the other — it divides by the panel's scale and nothing else. On
        /// a 640 by 480 window over a panel 1080 high, screen <c>y = 408</c>
        /// came back as panel <c>y = 918</c>, which is the bar along the bottom
        /// of the panel reached from near the top of the screen. Without the
        /// flip the guard is exactly upside down: a click on the palette falls
        /// through onto the board behind it and a click on the sky is swallowed,
        /// and both look like nothing rather than like a bug. A play-mode test
        /// pins it at both ends, because an assertion at one end alone passes
        /// just as well when the panel was never laid out at all.
        /// </para>
        /// </remarks>
        public static bool Covers(UIDocument document, Vector2 screenPoint)
        {
            if (document == null)
            {
                return false;
            }

            VisualElement root = document.rootVisualElement;

            if (root?.panel == null)
            {
                return false;
            }

            return root.panel.Pick(RuntimePanelUtils.ScreenToPanel(root.panel, Downwards(screenPoint)))
                != null;
        }

        /// <summary>
        /// A screen point with its <c>y</c> measured down from the top, which is
        /// the direction a panel's own coordinates run.
        /// </summary>
        public static Vector2 Downwards(Vector2 screenPoint) =>
            new Vector2(screenPoint.x, Screen.height - screenPoint.y);
    }
}
