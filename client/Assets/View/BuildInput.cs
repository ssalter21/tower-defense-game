using System;
using Sim;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// The pointer and the number row: the one place a click on the board turns
    /// into a <see cref="BuildAction"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No input reaches the simulation.</b> What a click produces is an
    /// action appended to a <see cref="BuildPhase"/> held in a local; the phase
    /// reaches the run as a stored command when the player commits, and by no
    /// other route. Nothing in this file touches a tick. See ADR-0039 and
    /// ADR-0051.
    /// </para>
    /// <para>
    /// <b>The bindings are the halves the camera left free.</b>
    /// <see cref="OrbitCameraRig"/> orbits on the right button and flies on the
    /// letter keys precisely so that the left button and the number row are
    /// available here — see the remarks on its <c>Update</c>. Those are the two
    /// this class reads and it reads nothing else.
    /// </para>
    /// <para>
    /// <b>Two verbs, and which one a click is comes from the cell.</b> A
    /// <c>place</c> is chosen from the palette and then pointed at an empty hex.
    /// An <c>upgrade</c> names its target by hex, so it goes the other way
    /// round: clicking the tower standing there is what asks for its ladder, and
    /// the rungs are offered at that hex. A cell that already holds a placement
    /// therefore never places, whatever is selected — which is also what the
    /// rules say, so the two agree by asking rather than by being written twice.
    /// </para>
    /// <para>
    /// <b>The devices are read in one method and everything else takes
    /// arguments.</b> <see cref="Point"/>, <see cref="Click"/> and
    /// <see cref="Shortcut"/> are what the class actually does, and
    /// <see cref="Update"/> is the only thing that turns a mouse into any of
    /// them — the same split the camera rig uses, and what lets a test drive a
    /// placement from a screen coordinate with no device attached.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildInput : MonoBehaviour
    {
        private ComposedRound _round;

        private TowerPalette _palette;

        private BuildBoard _board;

        private Camera _camera;

        private UIDocument _otherChrome;

        /// <summary>The decision being composed. The only thing this class writes to.</summary>
        public ComposedRound Round => _round;

        /// <summary>The camera the board is picked through.</summary>
        public Camera Camera => _camera;

        /// <summary>
        /// Builds the input under <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The one root object.</param>
        /// <param name="round">The decision a click is appended to.</param>
        /// <param name="palette">What is selected, and where a ladder is offered.</param>
        /// <param name="board">What is drawn, and which hex lights.</param>
        /// <param name="camera">The camera a screen point is cast through.</param>
        /// <param name="otherChrome">
        /// Any other panel on screen — the playback bar — so that a click on it
        /// does not also land on the board behind it.
        /// </param>
        public static BuildInput Build(
            Transform parent,
            ComposedRound round,
            TowerPalette palette,
            BuildBoard board,
            Camera camera,
            UIDocument otherChrome)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (board == null) throw new ArgumentNullException(nameof(board));

            var host = new GameObject("BuildInput");
            host.transform.SetParent(parent, worldPositionStays: false);

            var input = host.AddComponent<BuildInput>();

            input._round = round;
            input._palette = palette;
            input._board = board;
            input._camera = camera;
            input._otherChrome = otherChrome;

            palette.UpgradeChosen += input.Upgrade;

            return input;
        }

        /// <summary>
        /// The pointer moved. Lights the hex under it where the selected tower
        /// could stand there, and nowhere else.
        /// </summary>
        /// <remarks>
        /// <b>What lights is what the rules accept.</b> The question is asked of
        /// <see cref="ComposedRound.Allows"/>, which resolves the candidate
        /// phase and throws the <see cref="Sim.Build"/> away — so an unaffordable
        /// tower lights nothing, a corridor cell lights nothing, an occupied cell
        /// lights nothing, and none of those three is written down here.
        /// </remarks>
        public void Point(Vector2 screenPoint)
        {
            if (_palette.Selected == null
                || IsOverChrome(screenPoint)
                || !TryPick(screenPoint, out int column, out int row)
                || !_round.Allows(Placing(_palette.Selected, column, row)))
            {
                _board.Unlit();

                return;
            }

            _board.Lit(column, row);
        }

        /// <summary>
        /// The left button went down at a point on the screen. Returns true when
        /// the round changed.
        /// </summary>
        public bool Click(Vector2 screenPoint)
        {
            if (IsOverChrome(screenPoint))
            {
                return false;
            }

            if (!TryPick(screenPoint, out int column, out int row))
            {
                // Off the board. A click on nothing puts the ladder away, which
                // is the only way to dismiss it without buying a rung.
                _palette.CloseOffer();

                return false;
            }

            if (!(_round.StandingOn(column, row) is null))
            {
                // An upgrade names its target by hex, so the tower standing here
                // is what decides which rungs exist. The palette closes the offer
                // itself where none of them resolves.
                _palette.Offer(column, row);

                return false;
            }

            _palette.CloseOffer();

            UnitType chosen = _palette.Selected;

            if (chosen == null)
            {
                return false;
            }

            BuildAction placing = Placing(chosen, column, row);

            if (!_round.Allows(placing))
            {
                return false;
            }

            _round.Do(placing);
            Redraw();

            return true;
        }

        /// <summary>
        /// A number key was pressed: the entry at that place in the palette,
        /// counted from zero.
        /// </summary>
        public void Shortcut(int index)
        {
            _palette.SelectAt(index);
            _palette.CloseOffer();
        }

        /// <summary>
        /// Which cell of the map is under a screen point. Public because it is
        /// the whole of what "the board is clickable" means, and a test drives it
        /// from angles a mouse cannot be put at.
        /// </summary>
        public bool TryPick(Vector2 screenPoint, out int column, out int row) =>
            HexPicking.TryPick(_camera, screenPoint, _round.Map, out column, out row);

        /// <summary>
        /// Reads the mouse and the number row. The left button and the digits,
        /// and nothing else — every other binding on the screen belongs to the
        /// camera or to the playback bar.
        /// </summary>
        private void Update()
        {
            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();

                Point(pointer);

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Click(pointer);
                }
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            // Digit1 to Digit9 are consecutive in the key enumeration, so the
            // row is walked rather than listed. Writing the nine out would be
            // nine places for the tenth to be forgotten.
            for (int index = 0; index < TowerPalette.ShortcutCount; index++)
            {
                if (keyboard[Key.Digit1 + index].wasPressedThisFrame)
                {
                    Shortcut(index);
                }
            }
        }

        private void OnDestroy()
        {
            if (_palette != null)
            {
                _palette.UpgradeChosen -= Upgrade;
            }
        }

        /// <summary>A rung of an open offer was clicked.</summary>
        /// <remarks>
        /// The offer only ever holds rungs that resolved, so this appends
        /// without asking again — and if that ever stops being true the
        /// refusal comes out of <see cref="ComposedRound.Do"/> with the
        /// sentence the rules would have refused it in, which is what ADR-0051
        /// keeps a refusal reachable for.
        /// </remarks>
        private void Upgrade(int column, int row, UnitType into)
        {
            _round.Do(BuildAction.Of(ActionKind.Upgrade, into.Id, column, row));
            _palette.CloseOffer();
            Redraw();
        }

        private static BuildAction Placing(UnitType tower, int column, int row) =>
            BuildAction.Of(ActionKind.Place, tower.Id, column, row);

        private void Redraw()
        {
            _board.Follow();
            _palette.Follow();
        }

        /// <summary>
        /// Whether a screen point lands on a panel rather than on the board.
        /// Asked of the panels themselves, so no rectangle over here has to be
        /// kept level with a bar over there.
        /// </summary>
        private bool IsOverChrome(Vector2 screenPoint) =>
            _palette.Covers(screenPoint) || TowerPalette.Covers(_otherChrome, screenPoint);
    }
}
