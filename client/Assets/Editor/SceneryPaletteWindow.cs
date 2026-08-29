using System;
using System.Collections.Generic;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// The imported collection, as a list somebody can search and place from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because four thousand models are not reachable any other
    /// way.</b> The board's generated scenery comes out of five families and a
    /// variant number, which works while a family has six models in it and is
    /// unusable at four thousand -- nobody counts to 2,113. So a person picks by
    /// name here, the piece is drawn into the preview like any other, and
    /// <c>Tools > Board > Bake</c> writes it as a <c>model</c> line.
    /// </para>
    /// <para>
    /// <b>It places into the preview, not into the file.</b> Writing a line and
    /// re-dressing would be the shorter code and the wrong shape: the rule in
    /// this project is that the scene is where the moving happens and the bake
    /// is where it is kept, and a palette that edited the file directly would be
    /// a second way to author a board that skips the one button everything else
    /// goes through.
    /// </para>
    /// </remarks>
    public sealed class SceneryPaletteWindow : EditorWindow
    {
        /// <summary>
        /// How many matches are listed at once.
        /// </summary>
        /// <remarks>
        /// A cap rather than a scroll over everything, because drawing four
        /// thousand rows costs a frame every repaint and the person typing is
        /// narrowing anyway. The count of what was left out is shown, so a
        /// search that is still too broad says so rather than looking finished.
        /// </remarks>
        private const int Listed = 200;

        private string _search = string.Empty;

        private int _column;

        private int _row;

        private Vector2 _scroll;

        private string[] _names = Array.Empty<string>();

        private string _picked;

        [MenuItem("Tools/Board/Scenery %#s")]
        public static void Open()
        {
            SceneryPaletteWindow window = GetWindow<SceneryPaletteWindow>("Scenery");
            window.Reload();
            window.Show();
        }

        private void OnEnable()
        {
            if (_names.Length == 0)
            {
                Reload();
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_names.Length + " models", GUILayout.Width(90f));

                if (GUILayout.Button("Reload", GUILayout.Width(60f)))
                {
                    Reload();
                }
            }

            _search = EditorGUILayout.TextField("Search", _search);

            using (new EditorGUILayout.HorizontalScope())
            {
                _column = EditorGUILayout.IntField("Column", _column);
                _row = EditorGUILayout.IntField("Row", _row);
            }

            EditorGUILayout.LabelField(
                "Picked", string.IsNullOrEmpty(_picked) ? "nothing yet" : _picked);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_picked)))
            {
                if (GUILayout.Button("Place on " + _column + "," + _row))
                {
                    Place(_picked, _column, _row);
                }
            }

            EditorGUILayout.Space();

            List<string> matches = Matching();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int index = 0; index < matches.Count && index < Listed; index++)
            {
                if (GUILayout.Button(matches[index], EditorStyles.miniButton))
                {
                    _picked = matches[index];
                }
            }

            if (matches.Count > Listed)
            {
                EditorGUILayout.HelpBox(
                    (matches.Count - Listed) + " more match. Type more of the name.",
                    MessageType.Info);
            }

            if (matches.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    _names.Length == 0
                        ? "Nothing is imported under " + SceneryCatalogue.Root + "."
                        : "No model matches '" + _search + "'.",
                    MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Reload() => _names = SceneryCatalogue.Names();

        private List<string> Matching()
        {
            var matches = new List<string>();

            foreach (string name in _names)
            {
                if (string.IsNullOrEmpty(_search)
                    || name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(name);
                }
            }

            return matches;
        }

        /// <summary>
        /// Stands one model on a cell of the preview, at the cell's centre and
        /// its authored size, for somebody to drag from there.
        /// </summary>
        /// <remarks>
        /// Selected on the way out, because the next thing anybody does after
        /// placing a thing is move it, and hunting a new object down a hierarchy
        /// of two hundred is the step that makes a palette not worth using.
        /// </remarks>
        private static void Place(string name, int column, int row)
        {
            HexFloor floor = Floor();

            if (floor == null)
            {
                Debug.LogWarning(
                    "There is no board preview to place into. Tools > Board > Dress first.");

                return;
            }

            HexMap map = floor.Map;

            if (column < 0 || column >= map.Width || row < 0 || row >= map.Height)
            {
                Debug.LogWarning(
                    column + "," + row + " is not on this board, which is "
                    + map.Width + " by " + map.Height + ".");

                return;
            }

            GameObject piece = floor.Stand(
                SceneryPlacement.Named(name, column, row, 0f, 0f, 0f, 0f, 1f),
                MatchSceneBuilder.Scenery().With(SceneryCatalogue.Bind(new[] { name })));

            if (piece == null)
            {
                Debug.LogWarning(
                    name + " could not be drawn. It may not be imported, or the pack may ship "
                    + "more than one texture in its folder, which leaves the atlas ambiguous.");

                return;
            }

            // The preview is never saved, and a piece added to it must not be
            // either -- the whole point of the flag is that Match.unity stays one
            // empty root.
            piece.hideFlags = HideFlags.DontSave;

            foreach (Transform child in piece.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.hideFlags = HideFlags.DontSave;
            }

            Selection.activeGameObject = piece;

            Debug.Log(
                "Placed " + name + " on " + column + "," + row
                + ". Move it, then Tools > Board > Bake.");
        }

        private static HexFloor Floor()
        {
            foreach (HexFloor floor in Resources.FindObjectsOfTypeAll<HexFloor>())
            {
                if (floor.gameObject.scene.IsValid() && floor.Map != null)
                {
                    return floor;
                }
            }

            return null;
        }
    }
}
