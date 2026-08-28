using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The board as it is being built: the towers this round has composed, and
    /// the one hex under the pointer that would take another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This draws a decision, not a match.</b> ADR-0007 says the snapshot is
    /// the only thing that may draw game state, and that stands untouched —
    /// <see cref="MatchView"/> reads snapshots and nothing else. What is on
    /// screen here is a <see cref="Board"/> the player is composing, which no
    /// tick has yet seen and which will only ever reach the simulation as a
    /// stored command. See ADR-0051.
    /// </para>
    /// <para>
    /// <b>Redrawn by subtraction from the board, like everything else here.</b>
    /// A placement keeps its ordinal across an upgrade — see
    /// <c>docs/adr/0048-a-board-is-not-a-layout.md</c> — so a view is bound to
    /// that ordinal and only rebuilt when the type standing on it changes.
    /// Nothing sends this class a "tower placed" message; it compares what it
    /// drew against what the round says and closes the difference, which is what
    /// makes an undo, a reload or a mode change need no handling at all.
    /// </para>
    /// <para>
    /// <b>A placement is never taken off, so nothing here removes one.</b> A
    /// build phase places and upgrades; it has no verb that clears a cell, and
    /// this object is bound to one composed round for its whole life. So the
    /// comparison above only ever finds cells to draw or bodies to swap, and a
    /// sweep for placements that vanished would be machinery for a case the
    /// rules cannot produce.
    /// </para>
    /// <para>
    /// <b>The lit hex is prevention made visible and never a forecast.</b> It
    /// lights where <see cref="ComposedRound.Allows"/> resolved and nowhere
    /// else, so what the player is shown is exactly what the rules accept — no
    /// more, because a legal placement that is unwise still lights.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BuildBoard : MonoBehaviour
    {
        private readonly Dictionary<int, TowerView> _towers = new Dictionary<int, TowerView>();

        private readonly Dictionary<int, int> _drawnTypes = new Dictionary<int, int>();

        private ComposedRound _round;

        private MatchArt _art;

        private RoutePath _route;

        private HexMap _map;

        private HexFloor _floor;

        private Transform _towerParent;

        private Material _lightMaterial;

        /// <summary>The hex that lights under the pointer. Always present, often hidden.</summary>
        private GameObject _light;

        /// <summary>The towers this round has composed, by their placement ordinal.</summary>
        public IReadOnlyDictionary<int, TowerView> Towers => _towers;

        /// <summary>Whether a hex is lit right now.</summary>
        public bool IsLit => _light != null && _light.activeSelf;

        /// <summary>The lit cell, meaningless while <see cref="IsLit"/> is false.</summary>
        public int LitColumn { get; private set; }

        /// <summary>The lit cell, meaningless while <see cref="IsLit"/> is false.</summary>
        public int LitRow { get; private set; }

        /// <summary>
        /// Builds the layer under <paramref name="parent"/>, drawing what
        /// <paramref name="round"/> has composed.
        /// </summary>
        /// <param name="parent">The one root object.</param>
        /// <param name="round">The decision being composed.</param>
        /// <param name="art">The models every tower is drawn with.</param>
        /// <param name="route">The corridor, which is what a tower faces.</param>
        /// <param name="tile">The floor tile mesh, which the lit hex is a copy of.</param>
        public static BuildBoard Build(
            Transform parent,
            ComposedRound round,
            MatchArt art,
            RoutePath route,
            Mesh tile,
            HexMap map,
            HexFloor floor)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (art == null) throw new ArgumentNullException(nameof(art));
            if (route == null) throw new ArgumentNullException(nameof(route));

            var host = new GameObject("BuildBoard");
            host.transform.SetParent(parent, worldPositionStays: false);

            var board = host.AddComponent<BuildBoard>();
            board.Assemble(round, art, route, tile, map, floor);

            return board;
        }

        /// <summary>
        /// Puts what is drawn back in step with the composed board. Called after
        /// every change; cheap where nothing moved, because it compares before
        /// it builds.
        /// </summary>
        public void Follow()
        {
            Board board = _round.Board;

            for (int index = 0; index < board.Count; index++)
            {
                Placement placement = board.Placements[index];

                if (_drawnTypes.TryGetValue(placement.Id, out int drawn) && drawn == placement.Type.Id)
                {
                    continue;
                }

                // An upgrade keeps the ordinal and changes the body standing on
                // it, so the old model goes and the new one takes its place.
                Remove(placement.Id);
                Draw(placement);
            }

            // A tower and a grove want the same hex, so the grove gives way.
            // Done here rather than in Draw because this is the one place that
            // knows the whole board, and telling the floor the whole of it is
            // what lets a round that has been reloaded or undone come out right
            // without this class remembering what it cleared last time.
            if (_floor != null)
            {
                _floor.ClearSceneryUnder(Standing(board));
            }
        }

        /// <summary>Every cell the composed board has a tower on.</summary>
        private static IEnumerable<(int Column, int Row)> Standing(Board board)
        {
            for (int index = 0; index < board.Count; index++)
            {
                Placement placement = board.Placements[index];

                yield return (placement.Column, placement.Row);
            }
        }

        /// <summary>Lights one hex. What the pointer is over, where the rules allow it.</summary>
        public void Lit(int column, int row)
        {
            LitColumn = column;
            LitRow = row;

            _light.transform.localPosition =
                HexGeometry.ToWorld(column, row, _map.LevelAt(column, row))
                + (Vector3.up * MatchTuning.BuildLightHeight);
            _light.SetActive(true);
        }

        /// <summary>Takes the light off the board.</summary>
        public void Unlit() => _light.SetActive(false);

        /// <summary>
        /// The materials and the mesh are made here rather than loaded, so they
        /// are destroyed here too — an orphaned one outlives the play session
        /// that made it.
        /// </summary>
        private void OnDestroy()
        {
            if (_lightMaterial != null) Destroy(_lightMaterial);
        }

        private void Assemble(
            ComposedRound round,
            MatchArt art,
            RoutePath route,
            Mesh tile,
            HexMap map,
            HexFloor floor)
        {
            _round = round;
            _art = art;
            _route = route;
            _map = map;
            _floor = floor;

            _towerParent = new GameObject("Towers").transform;
            _towerParent.SetParent(transform, worldPositionStays: false);

            _lightMaterial = ViewMaterials.Create("HexLight", MatchTuning.BuildLightColor);
            _light = MakeLight(tile, _lightMaterial);
            _light.transform.SetParent(transform, worldPositionStays: false);
            _light.SetActive(false);

            Follow();
        }

        /// <summary>
        /// The lit hex: one more copy of the floor tile, a hair above the floor,
        /// casting nothing.
        /// </summary>
        /// <remarks>
        /// The tile mesh rather than an outline, because it is the same shape as
        /// the cell it is over by construction — an outline would be a second
        /// description of a hexagon, and the first thing it would disagree with
        /// is the floor under it. It casts no shadow: it is chrome, and chrome
        /// that threw a shadow onto the board would read as a thing standing
        /// there.
        /// </remarks>
        private static GameObject MakeLight(Mesh tile, Material material)
        {
            var host = new GameObject("HexLight");

            host.AddComponent<MeshFilter>().sharedMesh = tile;

            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;

            return host;
        }

        /// <summary>
        /// One tower of the composed board, drawn the way the match draws one:
        /// the unit type's own model, at the size its art says, facing the
        /// corridor.
        /// </summary>
        private void Draw(Placement placement)
        {
            var host = new GameObject("Built " + placement.Id + " " + placement.Type.Label);
            host.transform.SetParent(_towerParent, worldPositionStays: false);
            host.transform.localPosition =
                HexGeometry.ToWorld(placement.Column, placement.Row, _map.LevelAt(placement.Column, placement.Row));

            var view = host.AddComponent<TowerView>();
            Quaternion resting = _route.FacingFrom(host.transform.localPosition);
            UnitArt art = _art.ArtFor(placement.Type.Id);

            if (art.IsPosed)
            {
                view.BuildAnimated(placement.Id, placement.Type, art, resting);
            }
            else
            {
                view.BuildStatic(placement.Id, placement.Type, art, resting);
            }

            _towers[placement.Id] = view;
            _drawnTypes[placement.Id] = placement.Type.Id;
        }

        private void Remove(int id)
        {
            if (_towers.TryGetValue(id, out TowerView view) && view != null)
            {
                Destroy(view.gameObject);
            }

            _towers.Remove(id);
            _drawnTypes.Remove(id);
        }
    }
}
