using System.Collections.Generic;
using System.IO;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Drawing the board in the scene view: paint hexes, raise tiers, and see
    /// what the simulation will make of it while you do.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because the awkward part of a hand-built board is not
    /// typing it, it is not being able to see it.</b> <c>map.txt</c> is a
    /// character grid whose odd rows are shifted half a cell, so a corner drawn
    /// the obvious way touches three corridor cells and reads as a junction;
    /// tiers are a second grid of letters underneath; and none of that fails
    /// until the loader refuses the whole file. Here every one of those is a
    /// thing on screen: the tier is on the hex, the corridor is a numbered
    /// ribbon, and an illegal board says so in the simulation's own words while
    /// you are still drawing it.
    /// </para>
    /// <para>
    /// <b><c>map.txt</c> stays the artifact, and that is not a compromise.</b>
    /// The simulation, its tests, <c>simcli</c> and CI all read that file and
    /// none of them has an editor; the board's hash is stamped into
    /// <c>match.replay</c> and is what the replay gate checks. A board that
    /// lived in the scene would leave the headless run with nothing to run on.
    /// So this window is an editor for that file, and the file is what it
    /// writes.
    /// </para>
    /// <para>
    /// <b>The bake reports the chain rather than running it.</b> Changing the
    /// board invalidates everything computed from it — the defense, the record,
    /// the landmark table and the cell coordinates written into the simulation's
    /// own tests. Doing all that silently on every experiment would be worse
    /// than doing none of it, so the bake writes the map and then says, in
    /// order, what has to be run.
    /// </para>
    /// </remarks>
    public sealed class BoardEditorWindow : EditorWindow
    {
        /// <summary>What a click does.</summary>
        private enum Brush
        {
            Ground,
            Corridor,
            Spawn,
            Exit,
            Raise,
            Lower,
        }

        private const string MapPath = "content/map.txt";

        private BoardDraft _draft;

        private HexMap _parsed;

        private string _refusal;

        private Brush _brush = Brush.Corridor;

        private bool _showTiers = true;

        private bool _showRoute = true;

        private bool _showCoverage;

        private bool _showBuildable;

        private int _coverageType;

        private UnitTypeTable _types;

        private int _hoverColumn = -1;

        private int _hoverRow = -1;

        private bool _painting;

        [MenuItem("Tools/Board/Edit Map %#m")]
        public static void Open()
        {
            GetWindow<BoardEditorWindow>("Board").Reload();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnScene;

            if (_draft == null)
            {
                Reload();
            }
        }

        private void OnDisable() => SceneView.duringSceneGui -= OnScene;

        /// <summary>Reads the authored map and draws it.</summary>
        public void Reload()
        {
            string text = File.ReadAllText(Path.Combine(RepositoryRoot(), MapPath));

            _draft = BoardDraft.Of(StreamingContent.ReadMap(), text);
            _types = StreamingContent.ReadUnitTypes();

            Revalidate();
            Redraw();
        }

        private void OnGUI()
        {
            if (_draft == null)
            {
                Reload();
            }

            EditorGUILayout.LabelField(
                _draft.Width + " x " + _draft.Height, EditorStyles.boldLabel);

            _brush = (Brush)GUILayout.SelectionGrid(
                (int)_brush,
                new[] { "Ground", "Corridor", "Spawn", "Exit", "Raise", "Lower" },
                3);

            EditorGUILayout.Space();

            _showTiers = EditorGUILayout.ToggleLeft("Tiers and legality", _showTiers);
            _showRoute = EditorGUILayout.ToggleLeft("Route and distances", _showRoute);
            _showBuildable = EditorGUILayout.ToggleLeft("Buildable ground", _showBuildable);
            _showCoverage = EditorGUILayout.ToggleLeft("Tower coverage under the pointer", _showCoverage);

            if (_showCoverage && _types != null)
            {
                _coverageType = EditorGUILayout.Popup("Range of", _coverageType, PlaceableNames());
            }

            EditorGUILayout.Space();

            if (_parsed != null)
            {
                EditorGUILayout.HelpBox(Readout(), MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "This board will not load:\n\n" + _refusal
                    + "\n\nThat is the simulation's own refusal, word for word. Baking is off until it goes.",
                    MessageType.Error);
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(_parsed == null))
            {
                if (GUILayout.Button("Bake to " + MapPath))
                {
                    Bake();
                }
            }

            if (GUILayout.Button("Reload from " + MapPath))
            {
                Reload();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resize", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                int width = EditorGUILayout.IntField("Width", _draft.Width);
                int height = EditorGUILayout.IntField("Height", _draft.Height);

                if ((width != _draft.Width || height != _draft.Height) && width > 0 && height > 0)
                {
                    _draft = _draft.Resized(width, height);

                    Revalidate();
                    Redraw();
                }
            }
        }

        /// <summary>What the board is, in the terms that decide how it plays.</summary>
        private string Readout()
        {
            int climbs = 0;

            for (int step = 1; step < _parsed.Route.Count; step++)
            {
                if (_parsed.LevelAt(_parsed.Route[step]) != _parsed.LevelAt(_parsed.Route[step - 1]))
                {
                    climbs++;
                }
            }

            int live = 0;
            int dead = 0;

            for (int row = 0; row < _parsed.Height; row++)
            {
                for (int column = 0; column < _parsed.Width; column++)
                {
                    if (_parsed.CellAt(column, row) != MapCell.Ground)
                    {
                        continue;
                    }

                    if (AnythingReaches(column, row))
                    {
                        live++;
                    }
                    else
                    {
                        dead++;
                    }
                }
            }

            return _parsed.Route.Count + " route steps, " + climbs + " tier changes.\n"
                + live + " cells a tower could hold, " + dead + " no tower would ever stand on.";
        }

        // ---------------------------------------------------------------
        // Scene
        // ---------------------------------------------------------------

        private void OnScene(SceneView view)
        {
            if (_draft == null)
            {
                return;
            }

            Paint(view);
            Overlay();
        }

        /// <summary>
        /// Clicking and dragging on the board, through the same picking the game
        /// uses so the cell under the pointer is the cell the game would pick.
        /// </summary>
        private void Paint(SceneView view)
        {
            Event now = Event.current;
            Camera camera = view.camera;

            if (camera == null || _parsed == null)
            {
                return;
            }

            Vector2 point = HandleUtility.GUIPointToScreenPixelCoordinate(now.mousePosition);

            _hoverColumn = -1;
            _hoverRow = -1;

            if (HexPicking.TryPick(camera, point, _parsed, out int column, out int row))
            {
                _hoverColumn = column;
                _hoverRow = row;
            }

            if (now.alt || now.button != 0)
            {
                return;
            }

            if (now.type == EventType.MouseDown)
            {
                _painting = true;
            }

            if (now.type == EventType.MouseUp)
            {
                if (_painting)
                {
                    _painting = false;

                    Revalidate();
                    Redraw();
                    Repaint();
                }

                return;
            }

            bool stroke = now.type == EventType.MouseDown
                || (now.type == EventType.MouseDrag && _painting);

            if (!stroke || _hoverColumn < 0)
            {
                return;
            }

            Apply(_hoverColumn, _hoverRow);

            // Taking control stops the click also selecting whatever is under
            // it, which would otherwise drag a tree across the board every time
            // somebody painted a hex.
            GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
            now.Use();

            Revalidate();
            view.Repaint();
        }

        private void Apply(int column, int row)
        {
            switch (_brush)
            {
                case Brush.Ground:
                    _draft.Paint(column, row, MapCell.Ground);
                    break;

                case Brush.Corridor:
                    _draft.Paint(column, row, MapCell.Route);
                    break;

                case Brush.Spawn:
                    _draft.Paint(column, row, MapCell.Spawn);
                    break;

                case Brush.Exit:
                    _draft.Paint(column, row, MapCell.Exit);
                    break;

                case Brush.Raise:
                    _draft.Raise(column, row, _draft.LevelAt(column, row) + 1);
                    break;

                case Brush.Lower:
                    _draft.Raise(column, row, _draft.LevelAt(column, row) - 1);
                    break;
            }
        }

        /// <summary>Everything drawn over the board rather than in it.</summary>
        private void Overlay()
        {
            if (_showBuildable)
            {
                Buildable();
            }

            if (_showTiers)
            {
                Tiers();
            }

            if (_showRoute)
            {
                Route();
            }

            if (_showCoverage)
            {
                Coverage();
            }
        }

        /// <summary>
        /// The tier on every hex, and a red ring where a corridor cell has a
        /// number of corridor neighbours the map cannot accept.
        /// </summary>
        /// <remarks>
        /// <b>A hint, not the ruling.</b> The ruling is the parser's, in the
        /// window; this is here so that the cell causing it can be seen without
        /// counting characters. It catches the one mistake a hand drawing makes
        /// most — a corner one cell too tight, which touches three corridor cells
        /// because the odd rows are offset.
        /// </remarks>
        private void Tiers()
        {
            var label = new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleCenter };

            for (int row = 0; row < _draft.Height; row++)
            {
                for (int column = 0; column < _draft.Width; column++)
                {
                    int level = _draft.LevelAt(column, row);
                    Vector3 at = HexGeometry.ToWorld(column, row, level);

                    if (level > 0)
                    {
                        Handles.Label(at + (Vector3.up * 0.1f), new string('|', level), label);
                    }

                    if (_draft.CellAt(column, row) == MapCell.Ground)
                    {
                        continue;
                    }

                    int touching = _draft.CorridorNeighbours(column, row);

                    if (touching >= 1 && touching <= 2)
                    {
                        continue;
                    }

                    Handles.color = Color.red;
                    Handles.DrawWireDisc(at + (Vector3.up * 0.06f), Vector3.up, HexGeometry.Circumradius * 0.8f);
                    Handles.Label(at + (Vector3.up * 0.3f), touching + " ways", label);
                }
            }
        }

        /// <summary>
        /// The corridor as a ribbon, numbered every five steps, so how long a
        /// creep's walk is and where its middle falls are readable off the board.
        /// </summary>
        private void Route()
        {
            if (_parsed == null)
            {
                return;
            }

            var label = new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleCenter };

            Handles.color = new Color(1f, 0.85f, 0.3f, 0.9f);

            for (int step = 1; step < _parsed.Route.Count; step++)
            {
                Handles.DrawAAPolyLine(
                    6f,
                    HexGeometry.ToWorld(_parsed.Route[step - 1], _parsed.LevelAt(_parsed.Route[step - 1]))
                        + (Vector3.up * 0.08f),
                    HexGeometry.ToWorld(_parsed.Route[step], _parsed.LevelAt(_parsed.Route[step]))
                        + (Vector3.up * 0.08f));
            }

            for (int step = 0; step < _parsed.Route.Count; step += 5)
            {
                Handles.Label(
                    HexGeometry.ToWorld(_parsed.Route[step], _parsed.LevelAt(_parsed.Route[step]))
                        + (Vector3.up * 0.45f),
                    step.ToString(),
                    label);
            }
        }

        /// <summary>
        /// Which ground a tower could ever stand on: shaded where something in
        /// the roster can reach the corridor from it, and left bare where nothing
        /// can.
        /// </summary>
        /// <remarks>
        /// Asked of <see cref="Footing"/> rather than worked out here, so what is
        /// shaded is what the build phase will actually accept. Bare ground is
        /// where scenery is free — nothing will ever be built on it.
        /// </remarks>
        private void Buildable()
        {
            if (_parsed == null)
            {
                return;
            }

            for (int row = 0; row < _parsed.Height; row++)
            {
                for (int column = 0; column < _parsed.Width; column++)
                {
                    if (_parsed.CellAt(column, row) != MapCell.Ground || !AnythingReaches(column, row))
                    {
                        continue;
                    }

                    Handles.color = new Color(0.35f, 0.75f, 1f, 0.16f);
                    Handles.DrawSolidDisc(
                        HexGeometry.ToWorld(column, row, _parsed.LevelAt(column, row)) + (Vector3.up * 0.03f),
                        Vector3.up,
                        HexGeometry.Circumradius * 0.82f);
                }
            }
        }

        /// <summary>
        /// What a tower standing under the pointer would cover: its range, and
        /// every step of the corridor it can hit.
        /// </summary>
        /// <remarks>
        /// Through <see cref="Reach.Shoots"/>, which is the simulation's own
        /// arithmetic and carries the tier bonus with it — a tier is worth half a
        /// hex of reach, and a coverage overlay that drew a flat circle would be
        /// wrong on exactly the boards this editor exists to draw.
        /// </remarks>
        private void Coverage()
        {
            if (_parsed == null || _hoverColumn < 0 || _types == null)
            {
                return;
            }

            UnitType[] placeable = Placeable();

            if (placeable.Length == 0)
            {
                return;
            }

            UnitType type = placeable[Mathf.Clamp(_coverageType, 0, placeable.Length - 1)];
            Hex from = Hex.FromOddRowOffset(_hoverColumn, _hoverRow);
            int fromLevel = _parsed.LevelAt(_hoverColumn, _hoverRow);

            int hits = 0;

            for (int step = 0; step < _parsed.Route.Count; step++)
            {
                Hex cell = _parsed.Route[step];

                if (!Reach.Shoots(from, fromLevel, type.RangeMilliHex, cell, _parsed.LevelAt(cell)))
                {
                    continue;
                }

                hits++;

                Handles.color = new Color(1f, 0.4f, 0.35f, 0.5f);
                Handles.DrawSolidDisc(
                    HexGeometry.ToWorld(cell, _parsed.LevelAt(cell)) + (Vector3.up * 0.12f),
                    Vector3.up,
                    HexGeometry.Circumradius * 0.45f);
            }

            Vector3 centre = HexGeometry.ToWorld(_hoverColumn, _hoverRow, fromLevel);

            Handles.color = new Color(1f, 0.4f, 0.35f, 0.9f);
            Handles.DrawWireDisc(
                centre + (Vector3.up * 0.12f),
                Vector3.up,
                (type.RangeMilliHex / 1000f) * HexGeometry.AcrossFlats);

            Handles.Label(
                centre + (Vector3.up * 0.7f),
                type.Label + ": " + hits + " of " + _parsed.Route.Count + " steps",
                new GUIStyle(EditorStyles.whiteMiniLabel) { alignment = TextAnchor.MiddleCenter });
        }

        // ---------------------------------------------------------------
        // Plumbing
        // ---------------------------------------------------------------

        private bool AnythingReaches(int column, int row)
        {
            foreach (UnitType type in Placeable())
            {
                if (Footing.Of(_parsed, type, column, row).Sound)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The types a player can stand on the board: placed, and with a reach.
        /// </summary>
        /// <remarks>
        /// Range zero is the roster's way of saying no reach at all, and a
        /// coverage ring of nothing would be a circle of radius zero rather than
        /// an answer.
        /// </remarks>
        private UnitType[] Placeable()
        {
            var placeable = new List<UnitType>();

            foreach (UnitType type in _types.Types)
            {
                if (type.Role == UnitRole.Placed && type.RangeMilliHex > 0)
                {
                    placeable.Add(type);
                }
            }

            return placeable.ToArray();
        }

        private string[] PlaceableNames()
        {
            UnitType[] placeable = Placeable();
            var names = new string[placeable.Length];

            for (int index = 0; index < placeable.Length; index++)
            {
                names[index] = placeable[index].Label;
            }

            return names;
        }

        private void Revalidate() => _parsed = _draft.TryParse(out HexMap map, out _refusal) ? map : null;

        /// <summary>Redraws the board from the draft, where it can be loaded.</summary>
        private void Redraw()
        {
            if (_parsed != null)
            {
                BoardDressingTools.DressWith(_parsed);
            }
        }

        private void Bake()
        {
            string path = Path.Combine(RepositoryRoot(), MapPath);

            File.WriteAllText(path, _draft.ToText());
            AssetDatabase.Refresh();

            Debug.Log(
                "Baked " + MapPath + " (" + _draft.Width + " x " + _draft.Height + ", "
                + _parsed.Route.Count + " route steps).\n\n"
                + "This invalidates the artifacts computed from the board. Run, in order:\n"
                + "  1. tools/run-headless-match.ps1      -> recompute defense.txt against the new corridor\n"
                + "  2. tools/run-headless-match.ps1      -> re-record match.replay, which stamps the map hash\n"
                + "  3. tools/sync-streaming-content.ps1  -> copy the content into StreamingAssets\n"
                + "  4. dotnet test sim.tests             -> 17 files carry cell coordinates and will need them\n"
                + "  5. tools/run-editmode-tests.ps1 and run-playmode-tests.ps1\n\n"
                + "Nothing above is run for you: doing it on every experiment would rewrite committed "
                + "artifacts for a board you were only trying out.");
        }

        private static string RepositoryRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
    }
}
