using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace View.Editor
{
    /// <summary>
    /// Renders candidate chrome to PNGs, so a layout can be chosen by looking at
    /// it rather than by reading a description of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same argument as <see cref="ArtPreviewCapture"/>, one seam
    /// over.</b> Art is never picked from a filename here because "Idle_A" and
    /// "Skeletons_Idle" are the same string to everyone and two different poses
    /// to nobody. A layout has the identical failure: "the purse goes in the
    /// header" and "the purse goes over the palette" are two sentences that
    /// agree with each other and two screens that do not. This is the thing put
    /// in front of the developer so the choosing is a choice.
    /// </para>
    /// <para>
    /// <b>It draws the real chrome over the real board.</b> The playfield is a
    /// real <see cref="MatchRoot"/> on the committed map, the run is a real
    /// <see cref="RunLoop"/> holding a real <see cref="ComposedRound"/>, and the
    /// prices, names and purse on screen are what the content files say. A
    /// mockup with invented numbers on it is a picture of a game this project
    /// does not ship, and the numbers are most of what a build phase looks
    /// like.
    /// </para>
    /// <para>
    /// <b>These are documentation, not an oracle</b> — the call
    /// <c>docs/frames/README.md</c> already makes for match frames. Nothing
    /// compares a sheet to anything and nothing fails if one changes. What
    /// catches broken chrome is <c>Tests.PlayMode/ChromeLayoutTests</c>.
    /// </para>
    /// <para>
    /// <b>It runs in play mode, and that is measured rather than chosen.</b> In
    /// an edit-mode batchmode editor a runtime panel never lays out: a bar built
    /// there resolves to <c>NaN</c> by <c>NaN</c> and renders zero pixels. In
    /// play mode the same bar measures 399 by 199.5 and draws. So this enters
    /// play mode, which is also why it must not be launched with <c>-quit</c> —
    /// it exits the editor itself when the last sheet is written.
    /// </para>
    /// <para>
    /// <b>Everything is drawn to one render texture, because a batchmode screen
    /// cannot be photographed.</b> <see cref="ScreenCapture"/> returns null
    /// there and <see cref="Screen.SetResolution"/> is ignored. So the camera
    /// and every bar are pointed at a single texture at the sheet's own size:
    /// the camera clears it and draws the board, the bars draw over it without
    /// clearing, and the frame composites itself exactly as it would on a
    /// screen.
    /// </para>
    /// <para>
    /// <b>And the play mode resolution is set as well as the texture, which
    /// looks redundant and is not.</b> A batchmode editor's screen is 640 by
    /// 480, and <see cref="RuntimePanelUtils.CameraTransformWorldToPanel"/> —
    /// which is how the upgrade offer finds the hex it is pinned to — reads that
    /// screen rather than the surface being rendered. Left at 640 by 480 the
    /// ladder lands 44 pixels above the top of a 900-pixel sheet, and what gets
    /// written is a perfectly reasonable-looking build phase with no offer on
    /// it. <see cref="PlayModeWindow.SetCustomRenderingResolution"/> is what
    /// moves it; <see cref="Screen"/> goes on reporting 640 by 480 for the rest
    /// of that frame, so the evidence that it worked is the offer's own
    /// position and not what the screen says.
    /// </para>
    /// <para>
    /// <b>One surface rather than two, and that is a correctness matter and not
    /// a tidiness one.</b> The upgrade offer is pinned to a hex by converting a
    /// world point through the camera and then into panel coordinates, so it is
    /// only in the right place when the camera's pixel rect and the panel's
    /// target are the same surface. Rendering the board to one texture and the
    /// bars to another put the ladder 44 pixels above the top of the sheet,
    /// which reads as chrome that does not work rather than as a capture that
    /// does not.
    /// </para>
    /// </remarks>
    [InitializeOnLoad]
    public static class UiPreviewCapture
    {
        private const string SpecArgument = "-uiPreviewSpec";

        /// <summary>
        /// Where the spec path is left for the other side of the domain reload
        /// that entering play mode performs. <see cref="SessionState"/> rather
        /// than a static, because a static is exactly what that reload clears.
        /// </summary>
        private const string SpecKey = "UiPreviewCapture.Spec";

        private const string PendingKey = "UiPreviewCapture.Pending";

        /// <summary>
        /// The aspect every sheet is rendered at, and the aspect the camera is
        /// framed for. Chrome is anchored to the bottom and top edges of a
        /// 16-by-9 panel, so a square sheet would be a picture of a window
        /// nobody plays in.
        /// </summary>
        private const float FrameAspect = 16f / 9f;

        /// <summary>
        /// How many frames a shot is left to settle before it is read.
        /// </summary>
        /// <remarks>
        /// A runtime panel lays out when it is updated, so a capture taken on
        /// the frame the bars were built reads zero off every one of them — the
        /// same reason <c>ChromeLayoutTests</c> yields twice before it asserts.
        /// Eight rather than two because a panel pointed at a render texture has
        /// to repaint into it as well as lay out, and a settle that is too short
        /// fails as an empty sheet, which looks like a layout that drew nothing.
        /// </remarks>
        private const int SettleFrames = 8;

        private static Spec _spec;

        private static Manifest _manifest;

        private static List<string> _failures;

        private static int _shot;

        private static int _frames;

        private static GameObject _host;

        /// <summary>The one surface a sheet is drawn on: board first, bars over it.</summary>
        private static RenderTexture _sheet;

        private static readonly List<PanelSettings> _redirected = new List<PanelSettings>();

        /// <summary>This shot's bars, kept so the last frame can repaint them.</summary>
        private static readonly List<UIDocument> _bars = new List<UIDocument>();

        static UiPreviewCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// What a candidate layout implements. Named in a spec by type name and
        /// found by reflection, so a candidate can be a scratch file staged into
        /// the project for one run and deleted afterwards — nothing unchosen is
        /// ever committed, so nothing unchosen may be hard-coded here.
        /// </summary>
        public interface IUiPreviewLayout
        {
            /// <summary>
            /// Rearranges, replaces or adds to the chrome the run has already
            /// put up. Called after the state below has been applied, so what it
            /// is handed is a build phase with real money spent on it.
            /// </summary>
            void Build(MatchRoot root, RunLoop loop);
        }

        [Serializable]
        private sealed class Spec
        {
            /// <summary>Absolute directory the PNGs and the manifest are written to.</summary>
            public string outDir;

            /// <summary>Pixel width of a sheet. Height follows from the aspect.</summary>
            public int width = 1600;

            public ShotSpec[] shots = Array.Empty<ShotSpec>();
        }

        [Serializable]
        private sealed class ShotSpec
        {
            public string id;

            public string label;

            /// <summary>
            /// Which moment of a run to draw: <c>build</c>, <c>build-placed</c>
            /// or <c>build-offer</c>. See <see cref="ApplyState"/>.
            /// </summary>
            public string state = "build";

            /// <summary>
            /// The <see cref="IUiPreviewLayout"/> to run, by type name. Empty
            /// means the chrome the game ships, which is the baseline every
            /// candidate is being compared against.
            /// </summary>
            public string candidate;

            /// <summary>
            /// The tower a composed state buys, by its label in
            /// <c>content/units.txt</c>. Empty means the cheapest thing the
            /// opening purse can afford.
            /// </summary>
            /// <remarks>
            /// It is named rather than inferred because which tower is standing
            /// decides what the chrome has to draw: the ladder has one edge in
            /// it today, so a shot of the upgrade offer is a shot of an archer
            /// and of nothing else. A tool that picked for you would produce a
            /// sheet with no offer on it and no way to tell why.
            /// </remarks>
            public string place;

            public string notes;
        }

        [Serializable]
        private sealed class Manifest
        {
            public List<ManifestShot> shots = new List<ManifestShot>();
        }

        [Serializable]
        private sealed class ManifestShot
        {
            public string id;
            public string label;
            public string state;
            public string candidate;
            public string png;
            public string notes;
        }

        public static void Run()
        {
            string specPath = BatchArguments.Value(SpecArgument);

            if (string.IsNullOrEmpty(specPath))
            {
                throw new InvalidDataException("UiPreviewCapture needs " + SpecArgument + " <path>.");
            }

            if (!File.Exists(specPath))
            {
                throw new FileNotFoundException("No spec at " + specPath, specPath);
            }

            SessionState.SetString(SpecKey, Path.GetFullPath(specPath));
            SessionState.SetBool(PendingKey, true);

            Debug.Log("UiPreviewCapture: entering play mode for " + specPath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            // Cleared first: a shot that throws must not leave a play-mode entry
            // armed for whatever the developer opens the editor for next.
            SessionState.SetBool(PendingKey, false);

            try
            {
                _spec = JsonUtility.FromJson<Spec>(File.ReadAllText(SessionState.GetString(SpecKey, string.Empty)));

                if (_spec == null || _spec.shots == null || _spec.shots.Length == 0)
                {
                    throw new InvalidDataException("The spec names no shots.");
                }

                // Relative to the repository rather than to whatever directory
                // the editor was launched from, so a committed spec means the
                // same place wherever it is run from.
                _spec.outDir = Path.GetFullPath(
                    Path.IsPathRooted(_spec.outDir)
                        ? _spec.outDir
                        : Path.Combine(Application.dataPath, "..", "..", _spec.outDir));

                Directory.CreateDirectory(_spec.outDir);

                int width = Mathf.Max(320, _spec.width);
                int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

                // The screen as well as the texture. See the note on this class:
                // the offer is pinned through a conversion that reads the screen,
                // and a batchmode screen is 640 by 480 until this is called.
                PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, "UI preview");

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

                _sheet = new RenderTexture(width, height, 24) { name = "UiPreviewSheet" };
                _sheet.Create();

                _manifest = new Manifest();
                _failures = new List<string>();
                _shot = 0;

                BeginShot();
                EditorApplication.update += Tick;
            }
            catch (Exception error)
            {
                Debug.LogError("UiPreviewCapture: could not start -- " + error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Stands a whole playfield up for one shot, applies its state and hands
        /// it to its candidate.
        /// </summary>
        /// <remarks>
        /// A playfield per shot rather than one reused across all of them: a
        /// build phase is a purse being spent, so a shot that ran after another
        /// would be drawing whatever money the one before it left behind.
        /// </remarks>
        private static void BeginShot()
        {
            ShotSpec shot = _spec.shots[_shot];

            _host = new GameObject("UiPreviewRoot");
            _redirected.Clear();
            _bars.Clear();

            ReplayBundle record = StreamingContent.ReadRecordedMatch();

            var root = _host.AddComponent<MatchRoot>();
            root.Build(record.Map);

            RunLoop loop = root.BeginRun(record.Seed, Path.GetTempPath(), MatchSceneBuilder.Art());

            Camera camera = root.CameraRig.Camera;
            camera.backgroundColor = SceneFraming.BackgroundColor;

            // Framed for the sheet rather than for whatever aspect a headless
            // editor reports, which is 640 by 480 and never rendered.
            camera.aspect = FrameAspect;

            // Pointed at the sheet for the whole shot rather than for the read,
            // because the offer is positioned off the camera's pixel rect every
            // frame -- and until this line that rect is the batchmode screen,
            // 640 by 480, which is neither the sheet's size nor its shape.
            camera.targetTexture = _sheet;

            root.CameraRig.Reframe(root.Floor.WorldBounds);
            root.CameraRig.PointAt(
                SceneFraming.CameraDefaultYawDegrees,
                SceneFraming.CameraDefaultPitchDegrees,
                root.CameraRig.FramedDistance);

            ApplyState(shot, root, loop);
            ApplyCandidate(shot, root, loop);

            Redirect(root, loop);

            _frames = 0;
        }

        /// <summary>
        /// Puts the run into the moment this shot is about.
        /// </summary>
        /// <remarks>
        /// Every state here is reached through the same methods the pointer goes
        /// through — <see cref="ComposedRound.Do"/> and
        /// <see cref="TowerPalette.Offer"/> — so a state that the rules would
        /// refuse a player is a state this cannot reach either. A sheet showing
        /// an arrangement the game cannot produce is worse than no sheet, since
        /// it gets chosen and then cannot be built.
        /// </remarks>
        private static void ApplyState(ShotSpec shot, MatchRoot root, RunLoop loop)
        {
            switch (shot.state)
            {
                case "build":
                    return;

                case "build-placed":
                    Place(root, shot.place, out _, out _);
                    Send(root, 2);
                    return;

                case "build-offer":
                    Place(root, shot.place, out int column, out int row);
                    root.Palette.Offer(column, row);
                    root.Palette.Follow();

                    // Refused rather than drawn. An offer that did not open
                    // renders as an ordinary build phase, so a sheet of it is a
                    // picture of the wrong thing that looks entirely reasonable
                    // -- which is the species of failure this project keeps
                    // deleting.
                    if (!root.Palette.IsOffering)
                    {
                        throw new InvalidOperationException(
                            "The ladder did not open on the "
                            + root.Composing.StandingOn(column, row).Label
                            + " at (" + column + ", " + row + "). Either nothing it becomes is authored "
                            + "in content/upgrades.txt, or the purse cannot cover the rung: "
                            + root.Composing.Gold + " gold is left.");
                    }

                    return;

                default:
                    throw new InvalidDataException(
                        "No state called \"" + shot.state + "\". It is build, build-placed or build-offer.");
            }
        }

        /// <summary>
        /// Buys one tower - the one <paramref name="label"/> names, or the
        /// cheapest the opening purse can cover - and reports the cell it stands
        /// on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One tower, and the number is load-bearing.</b> The purse is
        /// single, so every tower bought is a rung nobody can afford and a creep
        /// nobody can send: three of them spend an opening hundred down to
        /// thirty, at which point <see cref="TowerPalette.Offer"/> correctly
        /// refuses to open - a tower with no affordable upgrade offers none. A
        /// sheet meant to show the ladder would have shown an empty board
        /// instead, and looked like a bug in the offer.
        /// </para>
        /// <para>
        /// <b>Then the board is told, because nothing tells it.</b>
        /// <see cref="BuildBoard.Follow"/> is called after a change rather than
        /// on a frame, so a placement made straight on the round is real, paid
        /// for and invisible until this line runs - which is exactly what the
        /// first sheet off this tool showed.
        /// </para>
        /// </remarks>
        private static void Place(MatchRoot root, string label, out int column, out int row)
        {
            ComposedRound round = root.Composing;
            UnitType chosen = null;

            foreach (UnitType tower in round.Palette)
            {
                if (!string.IsNullOrWhiteSpace(label))
                {
                    if (string.Equals(tower.Label, label, StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = tower;
                        break;
                    }

                    continue;
                }

                if (round.CanAfford(tower))
                {
                    chosen = tower;
                    break;
                }
            }

            if (chosen == null)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(label)
                        ? "The opening purse could not place a single tower, so a composed shot would "
                          + "be an empty board with a caption claiming otherwise."
                        : "No tower called \"" + label + "\" is on the palette. The labels are the ones "
                          + "in content/units.txt.");
            }

            if (!round.CanAfford(chosen))
            {
                throw new InvalidOperationException(
                    "The purse cannot cover a " + chosen.Label + " at " + round.PriceOf(chosen)
                    + " gold; it holds " + round.Gold + ".");
            }

            if (!FirstLegalCell(round, chosen, out column, out row))
            {
                throw new InvalidOperationException(
                    "No cell on this map accepts a " + chosen.Label + ". A unit some edge of "
                    + "content/upgrades.txt points at is refused to place and reached by upgrading "
                    + "into.");
            }

            round.Do(BuildAction.Of(ActionKind.Place, chosen.Id, column, row));

            root.Building.Follow();
            root.Palette.Follow();
        }

        /// <summary>
        /// Composes the front of the wave: <paramref name="slots"/> creeps, sent
        /// through the bar's own methods.
        /// </summary>
        /// <remarks>
        /// Through <see cref="WaveBar.Open"/> and <see cref="WaveBar.Choose"/>
        /// rather than through <see cref="ComposedRound.Send"/>, because the bar
        /// redraws on the choosing and not on the round — a send made behind it
        /// is a creep the wave has and the picture does not. The wave opens
        /// empty, so the index sent to is always the box past the last one
        /// filled.
        /// </remarks>
        private static void Send(MatchRoot root, int slots)
        {
            ComposedRound round = root.Composing;

            for (int sent = 0; sent < slots; sent++)
            {
                int index = round.Slots.Count;
                IReadOnlyList<UnitType> sendable = round.Sendable(index);

                if (sendable.Count == 0)
                {
                    break;
                }

                UnitType creep = sendable[sent % sendable.Count];

                if (round.PriceOf(creep) > round.Gold)
                {
                    break;
                }

                root.Wave.Open(index);
                root.Wave.Choose(creep);
            }

            root.Wave.Close();
            root.Palette.Follow();
        }

        /// <summary>
        /// A legal cell for <paramref name="tower"/>, as near the middle of the
        /// board as the rules allow.
        /// </summary>
        /// <remarks>
        /// Nearest the middle rather than first in the scan, which is a
        /// framing decision and not a rules one: the first legal cell is a
        /// corner, and a sheet of the upgrade offer pinned to a corner is a
        /// sheet of the offer half off the screen. Every candidate is still one
        /// <see cref="ComposedRound.Allows"/> accepted.
        /// </remarks>
        private static bool FirstLegalCell(ComposedRound round, UnitType tower, out int column, out int row)
        {
            HexMap map = round.Map;

            float middleColumn = (map.Width - 1) / 2f;
            float middleRow = (map.Height - 1) / 2f;

            column = 0;
            row = 0;

            bool any = false;
            float nearest = float.MaxValue;

            for (int candidateRow = 0; candidateRow < map.Height; candidateRow++)
            {
                for (int candidateColumn = 0; candidateColumn < map.Width; candidateColumn++)
                {
                    if (!round.Allows(BuildAction.Of(ActionKind.Place, tower.Id, candidateColumn, candidateRow)))
                    {
                        continue;
                    }

                    float away =
                        ((candidateColumn - middleColumn) * (candidateColumn - middleColumn))
                        + ((candidateRow - middleRow) * (candidateRow - middleRow));

                    if (away >= nearest)
                    {
                        continue;
                    }

                    nearest = away;
                    column = candidateColumn;
                    row = candidateRow;
                    any = true;
                }
            }

            return any;
        }

        private static void ApplyCandidate(ShotSpec shot, MatchRoot root, RunLoop loop)
        {
            if (string.IsNullOrWhiteSpace(shot.candidate))
            {
                return;
            }

            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(Types)
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.FullName, shot.candidate, StringComparison.Ordinal)
                    || string.Equals(candidate.Name, shot.candidate, StringComparison.Ordinal));

            if (type == null)
            {
                throw new InvalidDataException(
                    "No layout type called \"" + shot.candidate + "\" is loaded. A candidate is staged "
                    + "into the project for a run; if it was deleted, the spec still names it.");
            }

            if (!typeof(IUiPreviewLayout).IsAssignableFrom(type))
            {
                throw new InvalidDataException(
                    shot.candidate + " does not implement " + nameof(IUiPreviewLayout) + ".");
            }

            ((IUiPreviewLayout)Activator.CreateInstance(type)).Build(root, loop);
        }

        private static IEnumerable<Type> Types(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException loaded)
            {
                return loaded.Types.Where(type => type != null);
            }
        }

        /// <summary>
        /// Points every bar the run put up at the shared chrome texture.
        /// </summary>
        /// <remarks>
        /// <b>Every panel, and none of them clearing.</b> The bars are separate
        /// panels with separate settings by construction — see
        /// <see cref="RuntimePanel"/> — so they arrive at one texture one after
        /// another in sorting order, and a panel that cleared as it drew would
        /// leave a sheet containing only whichever bar happened to sort last.
        /// </remarks>
        private static void Redirect(MatchRoot root, RunLoop loop)
        {
            foreach (UIDocument document in Chrome(root, loop))
            {
                _bars.Add(document);

                PanelSettings settings = document.panelSettings;

                if (settings == null || _redirected.Contains(settings))
                {
                    continue;
                }

                settings.targetTexture = _sheet;
                settings.clearColor = false;
                _redirected.Add(settings);

                document.rootVisualElement?.MarkDirtyRepaint();
            }
        }

        private static IEnumerable<UIDocument> Chrome(MatchRoot root, RunLoop loop)
        {
            if (loop?.Header?.Document != null) { yield return loop.Header.Document; }
            if (root.Palette?.Document != null) { yield return root.Palette.Document; }
            if (root.Wave?.Document != null) { yield return root.Wave.Document; }
            if (root.Controls?.Document != null) { yield return root.Controls.Document; }
            if (loop?.Switch?.Document != null) { yield return loop.Switch.Document; }
        }

        private static void Tick()
        {
            _frames++;

            if (_frames < SettleFrames)
            {
                return;
            }

            ShotSpec shot = _spec.shots[_shot];

            try
            {
                Capture(shot);
            }
            catch (Exception error)
            {
                Debug.LogError("UiPreviewCapture: " + shot.id + " failed -- " + error);
                _failures.Add(shot.id + ": " + error.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(_host);
                _host = null;
            }

            _shot++;

            if (_shot < _spec.shots.Length)
            {
                try
                {
                    BeginShot();
                }
                catch (Exception error)
                {
                    Debug.LogError("UiPreviewCapture: could not stand up " + _spec.shots[_shot].id
                        + " -- " + error);
                    _failures.Add(_spec.shots[_shot].id + ": " + error.Message);
                    EditorApplication.update -= Tick;
                    Finish();
                }

                return;
            }

            EditorApplication.update -= Tick;
            Finish();
        }

        private static void Capture(ShotSpec shot)
        {
            var root = _host.GetComponent<MatchRoot>();

            // Nothing renders here. The camera and the bars have been drawing to
            // the sheet every frame since this shot was stood up, in that order,
            // and rendering the camera again by hand would clear the surface and
            // leave the board with no chrome on it.
            Empty(root, shot);


            Texture2D sheet = Read(_sheet);

            string png = shot.id + ".png";
            string path = Path.Combine(_spec.outDir, png);
            File.WriteAllBytes(path, sheet.EncodeToPNG());

            _manifest.shots.Add(new ManifestShot
            {
                id = shot.id,
                label = shot.label,
                state = shot.state,
                candidate = string.IsNullOrWhiteSpace(shot.candidate) ? "as-built" : shot.candidate,
                png = png,
                notes = shot.notes,
            });

            Debug.Log("UiPreviewCapture: " + shot.id + " -> " + path);
        }

        /// <summary>
        /// Refuses a sheet whose chrome measured out as nothing.
        /// </summary>
        /// <remarks>
        /// The same assertion <c>ChromeLayoutTests</c> makes, and for the same
        /// reason: where the text engine cannot measure, every label resolves to
        /// zero by zero and the bar it sits in collapses. A collapsed bar and a
        /// bar somebody deliberately made empty look identical on a PNG, so this
        /// is refused rather than written.
        /// </remarks>
        private static void Empty(MatchRoot root, ShotSpec shot)
        {
            foreach (UIDocument bar in _bars)
            {
                VisualElement panel = bar.rootVisualElement;

                if (panel == null)
                {
                    continue;
                }

                // A bar that is up but not shown -- the results switch during a
                // build phase -- measures zero on every string it carries, and
                // is meant to. Only what is displayed is judged.
                if (!Displayed(panel))
                {
                    continue;
                }

                bool wanted = false;
                bool measured = false;

                foreach (TextElement text in panel.Query<TextElement>().Build())
                {
                    if (string.IsNullOrEmpty(text.text))
                    {
                        continue;
                    }

                    wanted = true;

                    if (text.resolvedStyle.width > 0f && text.resolvedStyle.height > 0f)
                    {
                        measured = true;

                        break;
                    }
                }

                if (wanted && !measured)
                {
                    throw new InvalidOperationException(
                        "Every string on " + bar.gameObject.name + " measured zero by zero, so the "
                        + "sheet would show a collapsed bar and nothing would say why.");
                }
            }
        }

        /// <summary>Whether anything on this panel is on screen at all.</summary>
        private static bool Displayed(VisualElement panel)
        {
            for (int index = 0; index < panel.childCount; index++)
            {
                if (panel[index].resolvedStyle.display != DisplayStyle.None)
                {
                    return true;
                }
            }

            return false;
        }

        private static Texture2D Read(RenderTexture texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;

            var read = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            read.Apply();

            RenderTexture.active = previous;
            return read;
        }

        private static void Finish()
        {
            int code = 0;

            try
            {
                string manifest = Path.Combine(_spec.outDir, "manifest.json");
                File.WriteAllText(manifest, JsonUtility.ToJson(_manifest, true));
                Debug.Log("UiPreviewCapture: wrote " + _manifest.shots.Count + " sheet(s) and " + manifest);

                // Loud, and last, for ArtPreviewCapture's reason: a sheet that
                // silently did not render is an option the developer never gets
                // offered, and a missing option is invisible in a way a broken
                // one is not.
                if (_failures.Count > 0)
                {
                    Debug.LogError(
                        "UiPreviewCapture could not render " + _failures.Count + " shot(s):\n  "
                        + string.Join("\n  ", _failures));
                    code = 1;
                }
            }
            catch (Exception error)
            {
                Debug.LogError("UiPreviewCapture: could not write the manifest -- " + error);
                code = 1;
            }

            EditorApplication.Exit(code);
        }
    }
}
