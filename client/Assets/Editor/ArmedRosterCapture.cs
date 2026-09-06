using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Renders every unit in the roster holding what it holds, one PNG each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> <see cref="MatchFrameCapture"/> photographs the
    /// recorded match, and the recorded match is one defense:
    /// <c>content/defense.txt</c> puts four archers and two mages on the board
    /// and nothing else. A Soldier, a Skeleton and a Skeleton Warrior are all
    /// units the game can draw and that defense never contains, so no frame of
    /// that match will ever show one — which is exactly how the Soldier's sword
    /// went unreviewed on 14 August 2026. Changing the defense to get a
    /// photograph would re-freeze every committed golden for the sake of a
    /// picture; this renders the roster instead.
    /// </para>
    /// <para>
    /// <b>It draws through the real views.</b> A tower goes through
    /// <see cref="TowerView"/> and a creep through <see cref="CreepView"/>,
    /// with the art the scene is wired from, so what comes out is what the
    /// match draws and not a second approximation of it. Nothing here is an
    /// oracle: no test compares these, they are for looking at.
    /// </para>
    /// <para>
    /// <b>It also renders a named set that is not the roster.</b> Given
    /// <c>-rosterSet</c>, it draws whatever a <see cref="CandidateSet"/> file
    /// lists instead of the live rows — models nothing in
    /// <c>content/units.txt</c> points at yet, held, posed and coloured as a
    /// proposal says they would be, so a model can be signed off before a row
    /// exists to author it against. Same views, same sun, same framing; the only
    /// difference is where the list comes from.
    /// </para>
    /// <para>
    /// <c>-batchmode -executeMethod</c>, so it needs no editor session and no
    /// bridge — and therefore needs the editor CLOSED, because batchmode takes
    /// the project lock.
    /// </para>
    /// </remarks>
    public static class ArmedRosterCapture
    {
        private const string OutDirArgument = "-rosterOutDir";

        private const string WidthArgument = "-rosterWidth";

        private const string SetArgument = "-rosterSet";

        /// <summary>
        /// How many frames across a candidate's clip to draw into a filmstrip
        /// beside its still. One, the default, writes no strip.
        /// </summary>
        /// <remarks>
        /// <b>Because a clip cannot be judged from one frame of it.</b> The
        /// still this tool has always written is posed at
        /// <see cref="ClipPhase"/>, which is the strike of an attack — and at
        /// the strike, a chop and a diagonal slice are two pictures of a body
        /// holding a hammer. What tells them apart is the path the hammer takes
        /// to get there, and that is not in any one frame. So a set whose
        /// question is <i>which animation</i> asks for a strip, and whatever
        /// displays it plays the frames back.
        /// </remarks>
        private const string StripArgument = "-rosterStrip";

        private const string DefaultOutDir = "docs/frames/roster";

        /// <summary>How many tiles wide the candidate contact sheet is.</summary>
        /// <remarks>
        /// Six, matching <see cref="ArtPreviewCapture"/>'s turntable width, so
        /// two sheets from the two tools sit side by side at the same rhythm.
        /// </remarks>
        private const int SheetColumns = 6;

        /// <summary>How wide one tile of that sheet is, in pixels.</summary>
        /// <remarks>
        /// Rendered at this size rather than resampled down from the full
        /// frame: the camera is already standing where it needs to, so a second
        /// grab is cheaper than a filter and sharper than one.
        /// </remarks>
        private const int SheetTileWidth = 260;

        /// <summary>
        /// How far through its clip a candidate is posed, in [0,1].
        /// </summary>
        /// <remarks>
        /// Halfway, which is the strike of an attack, the far half of a stride
        /// and the middle of a hold. Frame zero is the wrong answer for all
        /// three: every clip in these banks starts from something close to the
        /// rest pose, so a sheet sampled at zero is thirty-one pictures of the
        /// same stance holding different props.
        /// </remarks>
        private const float ClipPhase = 0.5f;

        /// <summary>Portrait, because a unit is taller than it is wide.</summary>
        private const float FrameAspect = 3f / 4f;

        /// <summary>
        /// Three-quarter front: the angle a unit is most often seen from, and
        /// the one that shows both hands at once.
        /// </summary>
        /// <remarks>
        /// Past 180, because these models face +Z and a camera standing on -Z
        /// looking back at them photographs the backs of their heads. Which is
        /// what the first run of this tool did, and a bow's orientation cannot
        /// be judged from behind.
        /// </remarks>
        private const float ViewYawDegrees = 215f;

        private const float ViewPitchDegrees = 12f;

        /// <summary>How far back to stand, as a multiple of the unit's height.</summary>
        private const float DistanceInHeights = 2.2f;

        [MenuItem("Tools/Capture the armed roster")]
        public static void Run()
        {
            string outDir = BatchArguments.Value(OutDirArgument) ?? DefaultOutDir;
            string setFile = BatchArguments.Value(SetArgument);
            int width = ParseInt(BatchArguments.Value(WidthArgument), 700);
            int stripFrames = Mathf.Clamp(ParseInt(BatchArguments.Value(StripArgument), 1), 1, 60);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            // Read and resolve the whole set BEFORE the first render. A set of
            // thirty-one takes minutes to draw, and a typo found on entry
            // thirty is a typo found three minutes late.
            IReadOnlyList<CandidateSet.Candidate> candidates =
                setFile == null ? null : CandidateSet.Read(setFile);

            Directory.CreateDirectory(outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            var host = new GameObject("RosterCaptureRoot");
            var written = new List<string>();

            try
            {
                Camera camera = MakeCamera(host.transform);
                MakeSun(host.transform);

                if (candidates == null)
                {
                    DrawRoster(host.transform, camera, outDir, width, height, written);
                }
                else
                {
                    DrawSet(
                        host.transform, camera, candidates, setFile, outDir, width, height,
                        stripFrames, written);
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            foreach (string path in written)
            {
                Debug.Log("[roster] wrote " + path);
            }

            Debug.Log("[roster] " + written.Count + " file(s) drawn into " + outDir);
        }

        /// <summary>Every live row in <c>content/units.txt</c>, one PNG each.</summary>
        private static void DrawRoster(
            Transform host, Camera camera, string outDir, int width, int height, List<string> written)
        {
            MatchArt art = MatchSceneBuilder.Art();
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            foreach (UnitType type in types.Types)
            {
                string path = Path.Combine(
                    outDir,
                    "unit-" + type.Id.ToString("00", CultureInfo.InvariantCulture)
                    + "-" + type.Label + ".png");

                UnitArt unit = art.ArtFor(type.Id);
                var stand = new GameObject(type.Label);
                stand.transform.SetParent(host, worldPositionStays: false);

                try
                {
                    if (type.Role == UnitRole.Placed)
                    {
                        BuildTower(stand, type.Id, type, unit, 0f);
                    }
                    else
                    {
                        BuildCreep(
                            stand, unit, art.WalkClipFor(type.Id), art.DeathClipFor(type.Id), 0.25f);
                    }

                    ReportHeld(stand, "unit " + type.Id);
                    ReportBeside(stand, "unit " + type.Id);
                    Frame(camera, Measured(stand));

                    Write(path, Grab(camera, width, height));
                    written.Add(path);
                }
                finally
                {
                    Object.DestroyImmediate(stand);
                }
            }
        }

        /// <summary>
        /// A named set of candidates: one PNG each, one contact sheet, and a
        /// manifest saying which tile is which.
        /// </summary>
        /// <remarks>
        /// <b>The sheet carries no captions and the manifest is why.</b> Text
        /// drawn into a render texture in batchmode means a font asset, a
        /// runtime panel and several frames of layout to pump — machinery whose
        /// only output is a word already sitting in the PNG's own filename. The
        /// sheet is for comparing silhouettes at a glance; the manifest, in the
        /// same folder, says what row three column four is.
        /// </remarks>
        private static void DrawSet(
            Transform host,
            Camera camera,
            IReadOnlyList<CandidateSet.Candidate> candidates,
            string setFile,
            string outDir,
            int width,
            int height,
            int stripFrames,
            List<string> written)
        {
            // TowerView takes a UnitType, and a candidate has no row to have
            // one. It reads exactly two fields off it -- the windup and
            // backswing durations, in StretchedPhase -- and neither is reached
            // from Idle, which is the state every candidate is posed in. So a
            // live row stands in for the type, and nothing about that row
            // reaches the picture.
            UnitType standIn = FirstTower();

            int tileHeight = Mathf.Max(1, Mathf.RoundToInt(SheetTileWidth / FrameAspect));
            var tiles = new List<Texture2D>(candidates.Count);
            var manifest = new List<string>(candidates.Count);
            var strips = new List<string>(candidates.Count);

            try
            {
                for (var index = 0; index < candidates.Count; index++)
                {
                    CandidateSet.Candidate candidate = candidates[index];

                    string path = Path.Combine(
                        outDir,
                        "candidate-" + (index + 1).ToString("00", CultureInfo.InvariantCulture)
                        + "-" + candidate.Name + ".png");

                    UnitArt unit = ArtFor(candidate);

                    var stand = new GameObject(candidate.Name);
                    stand.transform.SetParent(host, worldPositionStays: false);

                    try
                    {
                        if (candidate.Side == CandidateSet.Side.Tower)
                        {
                            BuildTower(stand, 0, standIn, unit, ClipPhase);
                        }
                        else
                        {
                            BuildCreep(stand, unit, candidate.Clip, candidate.Clip, ClipPhase);
                        }

                        Hide(stand, candidate.Hidden);
                        ReportHeld(stand, candidate.Name);
                        ReportBeside(stand, candidate.Name);
                        Frame(camera, Measured(stand));

                        Write(path, Grab(camera, width, height));
                        written.Add(path);

                        tiles.Add(Grab(camera, SheetTileWidth, tileHeight));
                    }
                    finally
                    {
                        Object.DestroyImmediate(stand);
                    }

                    // THE STRIP IS DRAWN ONLY ONCE THE STILL'S BODY IS OFF THE
                    // STAGE. Both stands hang off the same host at the same
                    // local position, so a strip taken while the still was
                    // still standing came out with a motionless body
                    // superimposed on the moving one -- twelve frames of a
                    // candidate obscuring itself. Filmstrip refuses to draw
                    // with company now, but the ordering here is what actually
                    // keeps it alone.
                    if (stripFrames > 1)
                    {
                        string strip = Path.Combine(
                            outDir,
                            "strip-" + (index + 1).ToString("00", CultureInfo.InvariantCulture)
                            + "-" + candidate.Name + ".png");

                        Write(strip, Filmstrip(
                            host, camera, candidate, standIn, unit, stripFrames, width, height));
                        written.Add(strip);

                        strips.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0,-30} {1,3} {2,8:F3}",
                            candidate.Name,
                            stripFrames,
                            candidate.Clip == null ? 0f : candidate.Clip.length));
                    }

                    manifest.Add(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0,3}  r{1}c{2}  {3,-28} {4,-6} {5,-38} {6} | {7} | {8} | {9} | {10} | {11}",
                            index + 1,
                            (index / SheetColumns) + 1,
                            (index % SheetColumns) + 1,
                            candidate.Name,
                            candidate.Side == CandidateSet.Side.Tower ? "tower" : "creep",
                            candidate.ClipName,
                            candidate.ModelPath,
                            candidate.RightHandPath,
                            candidate.LeftHandPath,
                            candidate.TexturePath,
                            candidate.BesidePath,
                            candidate.Hidden.Count == 0
                                ? "whole body"
                                : "without " + string.Join(", ", candidate.Hidden)));
                }

                string sheet = Path.Combine(outDir, "candidates-sheet.png");
                Write(sheet, Stitch(tiles, SheetTileWidth, tileHeight));
                written.Add(sheet);
            }
            finally
            {
                foreach (Texture2D tile in tiles)
                {
                    Object.DestroyImmediate(tile);
                }
            }

            string manifestPath = Path.Combine(outDir, "candidates-manifest.txt");

            File.WriteAllLines(
                manifestPath,
                new[]
                {
                    "# " + candidates.Count + " candidates from " + setFile,
                    "# Tiles run left to right, " + SheetColumns + " to a row, in",
                    "# candidates-sheet.png; rNcM is the row and column of the tile.",
                    "#",
                    "#   n  tile  name                         side   clip"
                    + "                                   model | right hand | left hand | texture"
                    + " | beside | body",
                }.Concat(manifest).ToArray());

            written.Add(manifestPath);

            if (strips.Count == 0)
            {
                return;
            }

            // WHAT A PLAYER OF THESE STRIPS NEEDS AND CANNOT MEASURE. A strip is
            // a row of frames and says nothing about how fast they go by; the
            // clip's own length is the only honest answer, and it lives on the
            // AnimationClip rather than in any picture. Written beside them so a
            // page showing the strips can run each at the speed its clip was
            // authored at.
            //
            // THAT IS THE CLIP'S SPEED AND NOT THE TOWER'S. A swing in the match
            // is stretched to the row's windup and backswing tick budget, and
            // docs/roster.md carries a _ for both on every row this set is
            // about. So a strip played at the length below is the animation as
            // authored, which is what is being chosen -- not a prediction of how
            // fast the tower will do it.
            string stripIndex = Path.Combine(outDir, "strip-index.txt");

            File.WriteAllLines(
                stripIndex,
                new[]
                {
                    "# " + strips.Count + " filmstrips from " + setFile,
                    "# Each strip-NN-<name>.png is one row of frames, left to right,",
                    "# at phases i/frames of the clip -- so the last frame does NOT",
                    "# repeat the first and the row loops without a stutter.",
                    "#",
                    "#   name                       frames  seconds",
                }.Concat(strips).ToArray());

            written.Add(stripIndex);
        }

        /// <summary>
        /// A candidate's art bundle. A tower's three clips are all the one clip
        /// the set file named — it is posed in a single frame, and three
        /// references to that frame is what <see cref="UnitArt.IsPosed"/> asks
        /// for.
        /// </summary>
        /// <remarks>
        /// A creep takes no beside prop. The socket is a tower's, and the set
        /// reader refuses one on a creep line rather than leaving this to drop
        /// it silently.
        /// </remarks>
        private static UnitArt ArtFor(CandidateSet.Candidate candidate) =>
            candidate.Side == CandidateSet.Side.Tower
                ? UnitArt.Armed(
                    0,
                    candidate.Model,
                    MatchArt.TowerScale,
                    candidate.RightHand,
                    candidate.LeftHand,
                    candidate.Clip,
                    candidate.Clip,
                    candidate.Clip,
                    candidate.RightHandTilt,
                    candidate.LeftHandTilt,
                    default,
                    candidate.Texture,
                    BesideProp.OnTheNextTile(candidate.Beside, candidate.BesideScale))
                : UnitArt.Armed(
                    0,
                    candidate.Model,
                    MatchArt.CreepScale,
                    candidate.RightHand,
                    candidate.LeftHand,
                    null,
                    null,
                    null,
                    candidate.RightHandTilt,
                    candidate.LeftHandTilt,
                    default,
                    candidate.Texture);

        /// <summary>
        /// The tower, posed. Idle rather than an attack state, because Idle is
        /// the one state whose phase comes off the clip's own length instead of
        /// off the unit type's tick budget — which is what lets a candidate
        /// with no row be posed at all.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Through the shipped view, because a capture that instantiated the
        /// model itself would not be a picture of what the match draws — and
        /// attaching the weapons is the half being reviewed.
        /// </para>
        /// <para>
        /// Posed, not left in the bind pose. A character imports with its arms
        /// straight out, and a weapon parented to a hand that is pointing
        /// sideways tells you nothing about how the unit stands holding it —
        /// which is how a review of the first run of this tool got no further
        /// than "hard to say".
        /// </para>
        /// </remarks>
        private static void BuildTower(GameObject stand, int id, UnitType type, UnitArt art, float phase)
        {
            var tower = stand.AddComponent<TowerView>();

            if (!art.IsPosed)
            {
                tower.BuildStatic(id, type, art, Quaternion.identity);
                return;
            }

            tower.BuildAnimated(id, type, art, Quaternion.identity);

            float seconds = art.IdleClip.length * phase;

            tower.Pose(TowerState.Idle, Mathf.RoundToInt(seconds * Match.TicksPerSecond), null);
        }

        /// <summary>
        /// The creep, posed at <paramref name="phase"/> of its walk clip.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The two callers pass different phases, and both are right.</b>
        /// The live roster passes a quarter, which is mid-stride and where a
        /// carried shield stops being edge-on to everything; it has been that
        /// since the tool was written and the committed measurements in
        /// <c>Tests.Fixtures.ChosenArt</c> were taken in that pose, so moving it
        /// would silently invalidate them. A candidate passes
        /// <see cref="ClipPhase"/>, because a candidate's clip is often not a
        /// walk at all — it is whatever pose the proposal is asking about — and
        /// halfway is where an action clip is doing the action.
        /// </para>
        /// <para>
        /// <b>A candidate passes one clip for both slots.</b>
        /// <see cref="CreepView.Build"/> refuses a null death clip, and a
        /// candidate sheet never draws a death: the second slot is weighted
        /// zero in every frame this tool takes, so the same clip in both is a
        /// reference and not a second animation.
        /// </para>
        /// </remarks>
        private static void BuildCreep(
            GameObject stand, UnitArt art, AnimationClip walk, AnimationClip death, float phase)
        {
            CreepView creep = stand.AddComponent<CreepView>();

            creep.Build(art, walk, death);
            creep.Pose(
                Vector3.zero,
                Quaternion.identity,
                MatchTuning.HexesPerWalkCycle * phase,
                CreepState.Walking,
                0f);
        }

        /// <summary>
        /// A live placed row, borrowed as the type a candidate tower is built
        /// with. See <see cref="DrawSet"/> for why nothing about it shows.
        /// </summary>
        private static UnitType FirstTower()
        {
            foreach (UnitType type in StreamingContent.ReadUnitTypes().Types)
            {
                if (type.Role == UnitRole.Placed)
                {
                    return type;
                }
            }

            throw new IOException(
                "content/units.txt has no placed row, so there is no unit type to build a "
                + "candidate tower's view with.");
        }

        /// <summary>
        /// Takes the named mesh children out of the picture, for a candidate
        /// whose question is what the body looks like without them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Disabled on the built view, never on the asset.</b> The stand is
        /// thrown away at the end of the loop, so nothing here reaches disk and
        /// the next candidate gets the whole body back. Editing the imported
        /// prefab to answer a question about it would leave the question's
        /// answer standing in the project after the sheet was looked at.
        /// </para>
        /// <para>
        /// <b>The renderer goes, not the transform.</b> A hidden node may still
        /// be a parent — the Grave Robber's pouch sword hangs off the body's
        /// own hierarchy — and deactivating it would take its children with it.
        /// <see cref="CandidateSet"/> has already checked every name against
        /// the model, so a name reaching here matches something.
        /// </para>
        /// </remarks>
        private static void Hide(GameObject stand, IReadOnlyList<string> hidden)
        {
            if (hidden.Count == 0)
            {
                return;
            }

            foreach (Renderer renderer in stand.GetComponentsInChildren<Renderer>(true))
            {
                if (hidden.Contains(renderer.transform.name))
                {
                    renderer.enabled = false;
                }
            }
        }

        /// <summary>
        /// One candidate drawn at evenly spaced phases across its clip, laid
        /// left to right into a single strip.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The camera is framed once, on every phase at once.</b> Framing
        /// each frame on its own bounds would track the body — a chop would
        /// come out as a man standing still while the world moved under him,
        /// which is worse than the still it replaces. So every phase is posed
        /// and measured first, the bounds are unioned, and the camera is set
        /// from that and never moved again. A pose that reaches further than
        /// the rest therefore costs every frame a little size, which is the
        /// right trade: a swing that leaves the frame is the thing being
        /// looked for.
        /// </para>
        /// <para>
        /// <b>Phases run <c>i / count</c>, not <c>i / (count - 1)</c>.</b>
        /// These clips loop, so the last frame must not repeat the first — an
        /// eight-frame strip of a walk cycle that ends where it starts stutters
        /// on every lap.
        /// </para>
        /// </remarks>
        private static Texture2D Filmstrip(
            Transform host,
            Camera camera,
            CandidateSet.Candidate candidate,
            UnitType standIn,
            UnitArt art,
            int count,
            int width,
            int height)
        {
            var frames = new List<Texture2D>(count);
            Bounds union = default;
            var started = false;

            Alone(host, candidate.Name);

            for (var i = 0; i < count; i++)
            {
                GameObject stand = BuildAt(host, candidate, standIn, art, i / (float)count);

                try
                {
                    Bounds here = Measured(stand);

                    if (started)
                    {
                        union.Encapsulate(here);
                    }
                    else
                    {
                        union = here;
                        started = true;
                    }
                }
                finally
                {
                    Object.DestroyImmediate(stand);
                }
            }

            Frame(camera, union);

            try
            {
                for (var i = 0; i < count; i++)
                {
                    GameObject stand = BuildAt(host, candidate, standIn, art, i / (float)count);

                    try
                    {
                        frames.Add(Grab(camera, width, height));
                    }
                    finally
                    {
                        Object.DestroyImmediate(stand);
                    }
                }

                return Stitch(frames, width, height, count);
            }
            finally
            {
                foreach (Texture2D frame in frames)
                {
                    Object.DestroyImmediate(frame);
                }
            }
        }

        /// <summary>
        /// Refuses to draw a strip while anything else that draws a body is
        /// standing on the host.
        /// </summary>
        /// <remarks>
        /// <b>Because a second body does not look like a second body.</b> Every
        /// stand is parented to the same host at the same local position, so a
        /// strip taken while the single-still capture's stand was still alive
        /// came out as one candidate superimposed on a motionless copy of
        /// itself — which reads as a smeared model or a bad clip, not as two
        /// objects. It cost a round trip with the developer to find. The
        /// ordering in <see cref="DrawSet"/> is what keeps the stage clear;
        /// this is here so that if some future caller gets that ordering wrong
        /// the run stops and says so, instead of writing forty-seven strips
        /// nobody can read.
        /// </remarks>
        private static void Alone(Transform host, string subject)
        {
            int bodies = host.GetComponentsInChildren<TowerView>(true).Length
                + host.GetComponentsInChildren<CreepView>(true).Length;

            if (bodies != 0)
            {
                throw new IOException(
                    "Cannot draw the filmstrip for " + subject + ": " + bodies + " other body(ies) "
                    + "are still standing on the capture host. Every stand shares one position, so "
                    + "the strip would come out with them drawn on top of each other.");
            }
        }

        /// <summary>
        /// One candidate, built and posed at a phase of its clip, standing on
        /// its own.
        /// </summary>
        /// <remarks>
        /// <b>Built fresh per frame, and the cheaper way was wrong.</b> The
        /// first filmstrip posed one standing view over and over, which is what
        /// <see cref="SimDrivenAnimator"/> is for and costs a twelfth of the
        /// instantiation. What came back was a rigid body with a weapon
        /// sliding around it: the bones moved — a prop parented to a hand
        /// follows them exactly, which is why the hammer travelled — while the
        /// character's skinned mesh went on drawing the pose it was built in.
        /// A strip of that is worse than the still it replaces, because it
        /// looks like an answer. Building each frame the way the single-still
        /// path builds its one is the path already known to draw a posed body,
        /// so it is the path a strip takes too.
        /// </remarks>
        private static GameObject BuildAt(
            Transform host, CandidateSet.Candidate candidate, UnitType standIn, UnitArt art, float phase)
        {
            var stand = new GameObject(candidate.Name);
            stand.transform.SetParent(host, worldPositionStays: false);

            if (candidate.Side == CandidateSet.Side.Tower)
            {
                BuildTower(stand, 0, standIn, art, phase);
            }
            else
            {
                BuildCreep(stand, art, candidate.Clip, candidate.Clip, phase);
            }

            Hide(stand, candidate.Hidden);

            return stand;
        }

        /// <summary>Lays the tiles out into a grid, left to right, top to bottom.</summary>
        /// <remarks>
        /// The last row is padded with the sheet's own background rather than
        /// left transparent, so a short row reads as a short row and not as
        /// three candidates that failed to render.
        /// </remarks>
        private static Texture2D Stitch(
            List<Texture2D> tiles, int tileWidth, int tileHeight, int across = SheetColumns)
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(tiles.Count / (float)across));
            int columns = Mathf.Min(across, Mathf.Max(1, tiles.Count));

            var sheet = new Texture2D(
                columns * tileWidth, rows * tileHeight, TextureFormat.RGB24, false);

            var background = new Color[tileWidth * tileHeight];

            for (var pixel = 0; pixel < background.Length; pixel++)
            {
                background[pixel] = SceneFraming.BackgroundColor;
            }

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    int index = (row * across) + column;

                    // Unity's texture origin is bottom-left and a contact sheet
                    // is read top-left first, so the rows go in upside down.
                    sheet.SetPixels(
                        column * tileWidth,
                        (rows - 1 - row) * tileHeight,
                        tileWidth,
                        tileHeight,
                        index < tiles.Count ? tiles[index].GetPixels() : background);
                }
            }

            sheet.Apply();

            return sheet;
        }

        /// <summary>
        /// Where each held item's mesh sits relative to the hand holding it.
        /// </summary>
        /// <remarks>
        /// Orientation cannot be guessed from a render and then guessed again:
        /// the first guess at the staffs' half turn buried them in the body,
        /// because the correction assumed the pivot was at the grip. These are
        /// the numbers that say where the pivot actually is — the mesh's bounds
        /// expressed in the hand bone's own frame, so a positive Y means the
        /// item reaches up out of the fist and a negative Y means it hangs.
        /// </remarks>
        private static void ReportHeld(GameObject stand, string subject)
        {
            foreach (string bone in new[] { WeaponSocket.MeleeHand, WeaponSocket.OffHand })
            {
                Transform socket = WeaponSocket.FindBone(stand, bone);

                if (socket == null || socket.childCount == 0)
                {
                    continue;
                }

                Transform item = socket.GetChild(0);
                Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);

                if (renderers.Length == 0)
                {
                    continue;
                }

                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                foreach (Renderer renderer in renderers)
                {
                    Bounds world = renderer.bounds;

                    // Eight corners, each brought back into the bone's frame.
                    // The centre alone would not say which way a long thing
                    // points, and that is the whole question.
                    for (var corner = 0; corner < 8; corner++)
                    {
                        var offset = new Vector3(
                            (corner & 1) == 0 ? world.min.x : world.max.x,
                            (corner & 2) == 0 ? world.min.y : world.max.y,
                            (corner & 4) == 0 ? world.min.z : world.max.z);

                        Vector3 local = socket.InverseTransformPoint(offset);

                        min = Vector3.Min(min, local);
                        max = Vector3.Max(max, local);
                    }
                }

                Debug.Log(
                    $"[grip] {subject} {item.name} on {bone}: "
                    + $"min ({min.x:F2}, {min.y:F2}, {min.z:F2}) "
                    + $"max ({max.x:F2}, {max.y:F2}, {max.z:F2})");
            }
        }

        /// <summary>
        /// How tall the thing standing beside the character is against the
        /// character itself, and how far from it.
        /// </summary>
        /// <remarks>
        /// The one number a beside prop needs and a held prop does not: a
        /// weapon is authored beside the character that swings it and comes in
        /// at the right size, while a turret and a Forest Nature tree are
        /// scenery off other packs and come in at scenery's size. A ratio
        /// rather than an absolute height, because "does it read as standing
        /// beside him" is a comparison and the sheet is the other half of it.
        /// </remarks>
        private static void ReportBeside(GameObject stand, string subject)
        {
            var tower = stand.GetComponent<TowerView>();

            if (tower == null || tower.Beside == null)
            {
                return;
            }

            Bounds prop = Measured(tower.Beside);
            Bounds body = Measured(tower.Model);
            Vector3 fromRoot = prop.center - stand.transform.position;

            Debug.Log(
                $"[beside] {subject} {tower.Beside.name}: {prop.size.y:F2} m tall against the body's "
                + $"{body.size.y:F2} ({prop.size.y / Mathf.Max(body.size.y, 1e-4f):F2} of it), "
                + $"{prop.size.x:F2} x {prop.size.z:F2} on the ground, centred "
                + $"({fromRoot.x:F2}, {fromRoot.y:F2}, {fromRoot.z:F2}) from the root");
        }

        /// <summary>The world bounds of everything drawn under an object.</summary>
        private static Bounds Measured(GameObject drawn)
        {
            Renderer[] renderers = drawn.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return new Bounds(drawn.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Stands the camera off the unit's own measured size, so a creep drawn
        /// at a half and a tower drawn at one and a half both fill the frame.
        /// A fixed distance would photograph the scale difference instead of
        /// the weapon, and the scale difference already has its own test.
        /// </summary>
        private static void Frame(Camera camera, Bounds subject)
        {
            float reach = Mathf.Max(subject.size.magnitude, 0.01f);

            Quaternion look = Quaternion.Euler(ViewPitchDegrees, ViewYawDegrees, 0f);

            camera.transform.position = subject.center - (look * Vector3.forward * (reach * DistanceInHeights));
            camera.transform.rotation = look;
        }

        /// <summary>
        /// The camera every unit is shot through. It takes no size: the frame's
        /// shape is <see cref="FrameAspect"/> and the pixels are the render
        /// texture's, so a width and a height here would be two numbers nothing
        /// read and a swap nothing could see.
        /// </summary>
        private static Camera MakeCamera(Transform parent)
        {
            var holder = new GameObject("Camera");
            holder.transform.SetParent(parent, worldPositionStays: false);

            Camera camera = holder.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneFraming.BackgroundColor;
            camera.fieldOfView = SceneFraming.CameraFieldOfViewDegrees;
            camera.nearClipPlane = 0.01f;
            camera.aspect = FrameAspect;

            return camera;
        }

        /// <summary>
        /// The same sun the match uses, so a weapon's shading here is the
        /// shading it has on the board.
        /// </summary>
        private static void MakeSun(Transform parent)
        {
            var holder = new GameObject("Sun");
            holder.transform.SetParent(parent, worldPositionStays: false);

            Light sun = holder.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = SceneFraming.SunColor;
            sun.intensity = SceneFraming.SunIntensity;
            sun.shadowStrength = SceneFraming.SunShadowStrength;
            holder.transform.rotation = Quaternion.Euler(
                SceneFraming.SunPitchDegrees, SceneFraming.SunYawDegrees, 0f);
        }

        /// <summary>
        /// Encodes a texture to the path and then destroys it.
        /// </summary>
        /// <remarks>
        /// <b>A <see cref="Texture2D"/> made in script is not collected.</b> It
        /// is a managed handle on native memory, and the garbage collector will
        /// take the handle and leave the memory — so every frame this tool
        /// grabbed used to stay resident for the length of the run. That was
        /// nine textures when the tool only drew the roster, and is forty-one
        /// with a set of thirty-two: one full frame and one tile each, plus the
        /// sheet. Batchmode exits and the leak goes with it, which is exactly
        /// why nothing ever caught it.
        /// </remarks>
        private static void Write(string path, Texture2D frame)
        {
            try
            {
                File.WriteAllBytes(path, frame.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(frame);
            }
        }

        private static Texture2D Grab(Camera camera, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;

            // A warm-up render, thrown away, for the same reason
            // MatchFrameCapture does one: the first render in a fresh batchmode
            // editor lands before shaders and textures have resolved.
            camera.Render();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            frame.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);

            return frame;
        }

        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
    }
}
