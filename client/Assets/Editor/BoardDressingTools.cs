using System.Collections.Generic;
using System.IO;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// The board, drawn into the open scene so somebody can move things on it,
    /// and written back out again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The preview is never saved, and that is what keeps the scene
    /// generated.</b> Every object it makes carries
    /// <see cref="HideFlags.DontSave"/>, so it is in the hierarchy, selectable,
    /// draggable and lit like anything else — and Unity will not write a byte of
    /// it into <c>Match.unity</c>. The scene stays one empty root, with nothing
    /// in it worth hand-editing and nothing in it a merge could lose, which is
    /// the rule this tool had to work around rather than through.
    /// </para>
    /// <para>
    /// <b>So the scene is not where the work is kept — the bake is.</b> Move a
    /// tree, delete a grove, drop a duplicate somewhere else, then
    /// <c>Bake</c>: what changed goes to <c>content/dressing.txt</c> as text,
    /// and the preview is redrawn from it. Close the scene without baking and
    /// the moving is gone. That is a sharper edge than an autosave would be, and
    /// it is the one that keeps a hand-placed board from being a thing that
    /// exists only on one person's disk.
    /// </para>
    /// <para>
    /// <b>A redraw carries the work, it does not discard it.</b> Both tools draw
    /// into the one preview and the board editor redraws it after every stroke,
    /// so the board is torn down and rebuilt constantly. What is standing is read
    /// back before each teardown and drawn again from that, which is why moving a
    /// tree and then painting a hex keeps the tree. To go back to the committed
    /// file, <c>Clear</c> first: with nothing standing there is nothing to carry.
    /// </para>
    /// <para>
    /// <b>Only what differs is written.</b> A cell the generator would have
    /// dressed exactly as it now stands produces no line, so the file stays the
    /// list of exceptions rather than a dump of the board — and turning a
    /// setting up still re-dresses everything nobody has spoken for.
    /// </para>
    /// </remarks>
    public static class BoardDressingTools
    {
        /// <summary>Where the authored overrides live, from the repository root.</summary>
        public const string DressingPath = "content/dressing.txt";

        private const string PreviewName = "Board Preview";

        /// <summary>
        /// How far two placements may differ and still count as the same one, in
        /// metres and degrees.
        /// </summary>
        /// <remarks>
        /// A hair, but not zero. The preview's transforms are floats that have
        /// been through a rotation and back, and a bake that wrote a line for
        /// every cell whose tree had drifted by a micron would produce a file of
        /// two hundred exceptions the first time anybody pressed the button.
        /// </remarks>
        private const float Same = 0.002f;

        [MenuItem("Tools/Board/Dress %#d")]
        public static void Dress()
        {
            Selection.activeGameObject = DressWith(StreamingContent.ReadMap());

            Debug.Log(
                "Board preview drawn. Move what you like, then Tools > Board > Bake. "
                + "It is not saved with the scene — closing without baking loses it. "
                + "Drawing it again keeps what you have moved; Clear first to go back to "
                + DressingPath + ".");
        }

        /// <summary>
        /// Draws the preview from a map handed in rather than read from the
        /// content directory.
        /// </summary>
        /// <remarks>
        /// <b>For the board editor, whose map is not on disk yet.</b> A board
        /// being drawn exists only in a draft until somebody bakes it, and a
        /// preview that could only show the committed file would show the board
        /// as it was before the last twenty clicks. Nothing is selected here
        /// either — the editor repaints on every stroke, and stealing the
        /// selection each time would fight the person using it.
        /// </remarks>
        public static GameObject DressWith(HexMap map)
        {
            // Read before the teardown, because the teardown is what would
            // otherwise lose it.
            BoardDressing carried = Carried();

            Clear();

            var host = new GameObject(PreviewName);

            HexFloor.Build(
                host.transform,
                map,
                MatchSceneBuilder.Tiles(),
                MatchSceneBuilder.Scenery(),
                Settings(),
                carried ?? Authored());

            Hide(host.transform);

            return host;
        }

        /// <summary>
        /// What the preview standing in the scene holds, as overrides, or null
        /// when there is no preview to carry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is what stops a redraw eating somebody's afternoon.</b> Both
        /// tools draw into the one preview and the board editor redraws it after
        /// every stroke, so without this, moving a tree and then painting a
        /// single hex threw the tree back where the generator wanted it. Reading
        /// the standing board first and drawing the new one from that makes a
        /// redraw carry the work instead of discarding it.
        /// </para>
        /// <para>
        /// <b>Diffed against the map the floor was drawn from, not the one about
        /// to be drawn.</b> A stroke changes what the generator would produce, so
        /// measuring the old board against the new map would call every cell the
        /// stroke touched an override and pin the generator's own scenery into
        /// the file as if a person had placed it. <see cref="HexFloor.Map"/> is
        /// the board that is actually standing there, and it is the only honest
        /// baseline for what somebody moved on it.
        /// </para>
        /// <para>
        /// What comes back is exactly what <see cref="Bake"/> would write, so
        /// carrying forward and baking cannot disagree about what counts as
        /// moved.
        /// </para>
        /// </remarks>
        private static BoardDressing Carried()
        {
            GameObject[] previews = Previews();

            if (previews.Length == 0)
            {
                return null;
            }

            HexFloor floor = previews[0].GetComponentInChildren<HexFloor>();

            // A script reload clears the floor's fields without destroying its
            // objects, so Map comes back null on a preview that is already stale.
            // Nothing there is worth carrying and the file is the better answer.
            if (floor == null || floor.Map == null)
            {
                return null;
            }

            return BoardDressing.Parse(DressingPath, TextFor(floor, floor.Map, Settings()));
        }

        /// <summary>
        /// Takes the preview down.
        /// </summary>
        /// <remarks>
        /// <b>And throws away whatever was moved on it.</b> Drawing the board
        /// again carries unbaked work forward (<see cref="Carried"/>), so this is
        /// the only way back to <c>content/dressing.txt</c> — and the only way to
        /// lose an afternoon in one click. There is no prompt, because a menu
        /// item that argued with the person who chose it would be worse.
        /// </remarks>
        [MenuItem("Tools/Board/Clear")]
        public static void Clear()
        {
            foreach (GameObject standing in Previews())
            {
                Object.DestroyImmediate(standing);
            }
        }

        [MenuItem("Tools/Board/Bake")]
        public static void Bake()
        {
            GameObject[] previews = Previews();

            if (previews.Length == 0)
            {
                Debug.LogWarning("Nothing to bake: there is no board preview. Tools > Board > Dress first.");

                return;
            }

            HexFloor floor = previews[0].GetComponentInChildren<HexFloor>();

            if (floor == null || floor.Map == null)
            {
                Debug.LogWarning("The board preview has no floor on it. Draw it again.");

                return;
            }

            // The board that is standing, not the one on disk. They are the same
            // thing until somebody opens the map editor, and then they are not:
            // baking a draft board against the committed map would measure the
            // scenery against a corridor that is not the one under it.
            HexMap map = floor.Map;

            string text = TextFor(floor, map, Settings());
            string path = Path.Combine(RepositoryRoot(), DressingPath);

            File.WriteAllText(path, text);

            Debug.Log(
                "Baked " + BoardDressing.Parse(DressingPath, text).CellCount + " cell exceptions to "
                + DressingPath + ". Run tools/sync-streaming-content.ps1 and commit both copies.");

            AssetDatabase.Refresh();

            // Cleared first so the redraw comes from the file rather than from
            // the preview it would otherwise carry forward. Reading back what
            // was just written is the round trip, checked for free; carrying the
            // board forward would draw the same objects again and prove nothing.
            Clear();
            DressWith(map);
        }

        /// <summary>
        /// What a bake of this floor would write.
        /// </summary>
        /// <remarks>
        /// <b>Split out of <see cref="Bake"/> so the round trip can be tested
        /// without a file.</b> Draw a board, change nothing, and this must
        /// return a file with no lines in it: a bake that wrote an exception for
        /// every cell because a float had come back a micron out would look
        /// exactly like working, right up until the diff.
        /// </remarks>
        public static string TextFor(HexFloor floor, HexMap map, DressingSettings settings)
        {
            List<SceneryPlacement> standing = Standing(floor, map);
            List<SceneryPlacement> generated = BoardScenery.For(map, settings, null);

            var kept = new List<SceneryPlacement>();
            var cleared = new List<(int Column, int Row)>();
            var sky = new List<SceneryPlacement>();

            foreach (SceneryPlacement cloud in standing)
            {
                if (cloud.Group == SceneryGroup.Cloud)
                {
                    sky.Add(cloud);
                }
            }

            bool skyMoved = !SameSet(sky, OnlyClouds(generated));

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    List<SceneryPlacement> now = OnCell(standing, column, row);
                    List<SceneryPlacement> was = OnCell(generated, column, row);

                    if (SameSet(now, was))
                    {
                        continue;
                    }

                    if (now.Count == 0)
                    {
                        cleared.Add((column, row));
                    }
                    else
                    {
                        kept.AddRange(now);
                    }
                }
            }

            return BoardDressing.Write(kept, cleared, skyMoved ? sky : null);
        }

        /// <summary>
        /// What is standing on the board right now, read out of the preview's
        /// transforms rather than out of the chooser.
        /// </summary>
        /// <remarks>
        /// Local position and rotation, because the piece hangs off a host
        /// standing at the cell's centre — so a piece dragged in the scene view
        /// gives up its new offset without anybody converting a world position
        /// back through a tier.
        /// </remarks>
        private static List<SceneryPlacement> Standing(HexFloor floor, HexMap map)
        {
            var standing = new List<SceneryPlacement>();

            foreach (ScenerySignature piece in floor.GetComponentsInChildren<ScenerySignature>(includeInactive: true))
            {
                Transform at = piece.transform;
                Vector3 local = at.localPosition;

                if (!TryCellOf(floor, map, at.parent, out int column, out int row))
                {
                    // A cloud, or something reparented out of a cell. Either way
                    // it carries no cell and belongs to the sky block.
                    standing.Add(new SceneryPlacement(
                        SceneryGroup.Cloud, piece.Variant, 0, 0,
                        local.x, local.y, local.z, at.localEulerAngles.y, at.localScale.x));

                    continue;
                }

                standing.Add(new SceneryPlacement(
                    piece.Group, piece.Variant, column, row,
                    local.x, local.y, local.z, at.localEulerAngles.y, at.localScale.x));
            }

            return standing;
        }

        private static bool TryCellOf(HexFloor floor, HexMap map, Transform host, out int column, out int row)
        {
            column = 0;
            row = 0;

            if (host == null)
            {
                return false;
            }

            for (int at = 0; at < map.Height; at++)
            {
                for (int on = 0; on < map.Width; on++)
                {
                    GameObject standing = floor.SceneryAt(on, at);

                    if (standing != null && standing.transform == host)
                    {
                        column = on;
                        row = at;

                        return true;
                    }
                }
            }

            return false;
        }

        private static List<SceneryPlacement> OnCell(List<SceneryPlacement> all, int column, int row)
        {
            var on = new List<SceneryPlacement>();

            foreach (SceneryPlacement placement in all)
            {
                if (placement.Group != SceneryGroup.Cloud
                    && placement.Column == column
                    && placement.Row == row)
                {
                    on.Add(placement);
                }
            }

            return on;
        }

        private static List<SceneryPlacement> OnlyClouds(List<SceneryPlacement> all)
        {
            var clouds = new List<SceneryPlacement>();

            foreach (SceneryPlacement placement in all)
            {
                if (placement.Group == SceneryGroup.Cloud)
                {
                    clouds.Add(placement);
                }
            }

            return clouds;
        }

        /// <summary>
        /// Whether two sets of pieces are the same board, ignoring the order
        /// they happen to be listed in.
        /// </summary>
        private static bool SameSet(List<SceneryPlacement> left, List<SceneryPlacement> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var taken = new bool[right.Count];

            foreach (SceneryPlacement one in left)
            {
                bool found = false;

                for (int index = 0; index < right.Count; index++)
                {
                    if (taken[index] || !Alike(one, right[index]))
                    {
                        continue;
                    }

                    taken[index] = true;
                    found = true;

                    break;
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Alike(SceneryPlacement left, SceneryPlacement right) =>
            left.Group == right.Group
            && left.Variant == right.Variant
            && Mathf.Abs(left.OffsetX - right.OffsetX) < Same
            && Mathf.Abs(left.OffsetY - right.OffsetY) < Same
            && Mathf.Abs(left.OffsetZ - right.OffsetZ) < Same
            && Mathf.Abs(Mathf.DeltaAngle(left.Turn, right.Turn)) < 0.5f
            && Mathf.Abs(left.Scale - right.Scale) < Same;

        /// <summary>The settings the scene carries, or the shipped ones.</summary>
        /// <remarks>
        /// Public so a test can measure a board against the same settings the
        /// tools drew it with. A test that assumed the defaults would pass or
        /// fail on whether anybody had touched the asset.
        /// </remarks>
        public static DressingSettings Settings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:BoardDressingAsset"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<BoardDressingAsset>(
                    AssetDatabase.GUIDToAssetPath(guid));

                if (asset != null)
                {
                    return asset.Settings();
                }
            }

            return DressingSettings.Default;
        }

        /// <summary>
        /// The overrides as authored, read from <c>content/</c> rather than from
        /// the streaming copy.
        /// </summary>
        /// <remarks>
        /// The authored file is the one a bake writes and a human edits; the
        /// streaming copy is generated from it. Previewing the copy would mean
        /// the editor showed something a step behind whatever was just baked,
        /// until somebody remembered to run the sync.
        /// </remarks>
        private static BoardDressing Authored()
        {
            string path = Path.Combine(RepositoryRoot(), DressingPath);

            return File.Exists(path)
                ? BoardDressing.Parse(DressingPath, File.ReadAllText(path))
                : BoardDressing.Empty;
        }

        private static string RepositoryRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

        /// <summary>
        /// The previews standing in the open scene.
        /// </summary>
        /// <remarks>
        /// <b>FindObjectsOfTypeAll, and it has to be.</b> The ordinary find does
        /// not return objects carrying <see cref="HideFlags.DontSave"/>, which
        /// is every object this tool makes — so Clear found nothing, cleared
        /// nothing, and each Dress quietly stacked another whole board on top of
        /// the last. The scene check is what keeps prefabs and imported assets,
        /// which this call also reaches, out of the answer.
        /// </remarks>
        private static GameObject[] Previews()
        {
            var found = new List<GameObject>();

            foreach (GameObject standing in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (standing.name == PreviewName
                    && standing.transform.parent == null
                    && standing.scene.IsValid())
                {
                    found.Add(standing);
                }
            }

            return found.ToArray();
        }

        /// <summary>
        /// Keeps the whole preview out of the saved scene.
        /// </summary>
        /// <remarks>
        /// Set on every object rather than only the root, because
        /// <see cref="HideFlags"/> do not descend and a child without them is a
        /// child Unity writes into the scene file — which is how a "preview"
        /// becomes a merge conflict.
        /// </remarks>
        private static void Hide(Transform at)
        {
            at.gameObject.hideFlags = HideFlags.DontSave;

            for (int index = 0; index < at.childCount; index++)
            {
                Hide(at.GetChild(index));
            }
        }
    }
}
