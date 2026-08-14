using System;
using System.Collections.Generic;
using Sim;

namespace View
{
    /// <summary>
    /// The round the player is composing: a <see cref="BuildPhase"/> in a local,
    /// and the pure calls that say what may be added to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is ADR-0051's shape, and it is the whole of the client's side of
    /// the rules.</b> The client holds a <see cref="Run"/>, composes a phase as
    /// the player clicks, prices every change by calling
    /// <see cref="BuildPhase.Resolve"/> and throwing the <see cref="Build"/>
    /// away, and hands the run nothing until the player says they are done.
    /// Committing is <see cref="Run.Advance"/> and belongs to the run loop
    /// (#198); nothing in here moves a run.
    /// </para>
    /// <para>
    /// <b>Legality is asked, never restated.</b> Every question this class
    /// answers — may this tower stand here, may this one be upgraded into that
    /// one, what is left in the purse — is answered by resolving a candidate
    /// phase and reading or discarding what comes back. There is no copy of the
    /// placement rules over here to disagree with <c>sim</c>: a refusal is a
    /// <see cref="SimulationException"/> from the same call that would refuse a
    /// stored command stream. That is what makes prevention safe to rely on —
    /// see <see cref="Allows"/>.
    /// </para>
    /// <para>
    /// <b>Nothing is cached that could go stale.</b> The class holds the phase
    /// and the opening state of the round, and re-resolves whenever the phase
    /// changes; the board and the gold it reports are read off that resolution
    /// rather than kept alongside it. ADR-0051 rejected a client-side purse and
    /// board synchronised with the run for exactly the reason a second copy is
    /// always rejected here.
    /// </para>
    /// <para>
    /// <b>Prevention covers legality and stops there.</b> A placement that is
    /// legal and unwise is offered. Nothing in this class computes an outcome,
    /// in any mode.
    /// </para>
    /// </remarks>
    public sealed class ComposedRound
    {
        /// <summary>How many of one tower an action buys. An action names one cell.</summary>
        private const int OneTower = 1;

        private readonly UpgradeLadder _ladder;

        private readonly Purse _opening;

        private readonly CostTable _costs;

        private readonly UnitTypeTable _types;

        private readonly HexMap _map;

        private readonly Board _standing;

        private readonly UnitType[] _palette;

        private BuildPhase _phase;

        private Build _resolved;

        /// <summary>
        /// A round composed against everything a <see cref="BuildPhase"/> is
        /// resolved against.
        /// </summary>
        /// <param name="wave">Which round this is, as <see cref="Run.Advance"/> counts them.</param>
        /// <param name="ladder">The upgrade edges. What may be placed and what may be climbed into.</param>
        /// <param name="purse">What the round opens with.</param>
        /// <param name="costs">What everything is priced at.</param>
        /// <param name="types">The roster.</param>
        /// <param name="map">The playfield a cell is on, or is not.</param>
        /// <param name="board">What stands before this round acts.</param>
        public ComposedRound(
            int wave,
            UpgradeLadder ladder,
            Purse purse,
            CostTable costs,
            UnitTypeTable types,
            HexMap map,
            Board board)
        {
            Wave = wave;
            _ladder = ladder ?? throw new ArgumentNullException(nameof(ladder));
            _opening = purse ?? throw new ArgumentNullException(nameof(purse));
            _costs = costs ?? throw new ArgumentNullException(nameof(costs));
            _types = types ?? throw new ArgumentNullException(nameof(types));
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _standing = board ?? throw new ArgumentNullException(nameof(board));
            _palette = Buildable(types, ladder, costs);

            _phase = BuildPhase.Of();
            _resolved = Resolve(_phase);
        }

        /// <summary>
        /// The round <paramref name="run"/> is about to play, composed against
        /// the state it has now.
        /// </summary>
        /// <remarks>
        /// The run is asked rather than copied. <see cref="Run.Advance"/>
        /// resolves the phase it is handed at <c>Round + 1</c> against the same
        /// board and purse read here, so the phase this composes is the phase
        /// that run will accept.
        /// </remarks>
        public static ComposedRound For(Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return new ComposedRound(
                run.Round + 1, run.Ladder, run.Purse, run.Costs, run.Types, run.Map, run.Board);
        }

        /// <summary>Which round of the run this is.</summary>
        public int Wave { get; }

        /// <summary>The playfield being built on.</summary>
        public HexMap Map => _map;

        /// <summary>
        /// What has been composed so far. What a commit hands to
        /// <see cref="Run.Advance"/>, unchanged.
        /// </summary>
        public BuildPhase Phase => _phase;

        /// <summary>What would be standing if this round were committed now.</summary>
        public Board Board => _resolved.Board;

        /// <summary>What would be left in the purse if it were committed now.</summary>
        public int Gold => _resolved.Purse.Gold;

        /// <summary>
        /// Every tower that may be stood on an empty cell outright, cheapest
        /// first. What the palette lists.
        /// </summary>
        /// <remarks>
        /// A row some edge of the ladder points at is left off, because it is
        /// refused to <c>place</c> and reached by upgrading the rung below it —
        /// so listing it would be offering an action the rules turn down. Same
        /// list, same order and the same reasoning as the command line's panel
        /// in <c>simcli/RoundFrame.cs</c>.
        /// </remarks>
        public IReadOnlyList<UnitType> Palette => _palette;

        /// <summary>What one of these costs, out of the table a purchase is actually priced by.</summary>
        public int PriceOf(UnitType type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _costs.PriceOf(Purchase.Unit(type.Id), OneTower);
        }

        /// <summary>Whether the purse could cover one of these, as it stands.</summary>
        public bool CanAfford(UnitType type) => PriceOf(type) <= Gold;

        /// <summary>
        /// What stands on a cell of the composed board, or null where nothing
        /// does. Includes what this round has placed and not yet committed.
        /// </summary>
        public UnitType StandingOn(int column, int row) => Board.TypeOn(column, row);

        /// <summary>
        /// Whether the rules would accept <paramref name="action"/> as the next
        /// thing this round does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The refusal is the answer, and it is thrown away.</b> The
        /// candidate phase is this one with the action appended, resolved
        /// against the round's opening state — the identical call a stored
        /// command stream is validated by. So a hex that does not light is a hex
        /// <see cref="BuildPhase.Resolve"/> would have refused, in the exact
        /// words it would have refused it in, and prevention cannot drift away
        /// from refusal because there is only one of them.
        /// </para>
        /// <para>
        /// <b>What is prevented is what the rules refuse, and nothing more.</b>
        /// An action that is legal and bad resolves, so it is offered. See
        /// ADR-0051.
        /// </para>
        /// </remarks>
        public bool Allows(BuildAction action)
        {
            try
            {
                Resolve(_phase.With(action));

                return true;
            }
            catch (SimulationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Adds <paramref name="action"/> to the round.
        /// </summary>
        /// <remarks>
        /// The refusal is not caught. Every affordance on screen asks
        /// <see cref="Allows"/> first, so an action arriving here that does not
        /// resolve means something offered what it should not have — which is a
        /// defect in the view and wants a stack trace, not a shrug. ADR-0051
        /// makes that assertion the reason prevention is safe to rely on.
        /// </remarks>
        public void Do(BuildAction action)
        {
            BuildPhase grown = _phase.With(action);

            _resolved = Resolve(grown);
            _phase = grown;
        }

        /// <summary>
        /// The rungs the tower on a cell may be upgraded into right now, in the
        /// order the ladder carries them.
        /// </summary>
        /// <remarks>
        /// Only what resolves: an edge whose target this round cannot afford is
        /// not offered, and neither is anything on a cell with nothing standing
        /// on it. A tower with no affordable upgrade offers none, which is what
        /// ADR-0051's prevention means at a hex.
        /// </remarks>
        public IReadOnlyList<UnitType> UpgradesOn(int column, int row)
        {
            var rungs = new List<UnitType>();
            UnitType standing = StandingOn(column, row);

            if (standing is null)
            {
                return rungs;
            }

            for (int index = 0; index < _ladder.Count; index++)
            {
                UpgradeEdge edge = _ladder.Edges[index];

                if (edge.From != standing.Id)
                {
                    continue;
                }

                if (Allows(BuildAction.Of(ActionKind.Upgrade, edge.To, column, row)))
                {
                    rungs.Add(_types.ById(edge.To));
                }
            }

            return rungs;
        }

        /// <summary>
        /// One resolution of a candidate, against the round's opening state.
        /// The single call every question in this class is answered by.
        /// </summary>
        private Build Resolve(BuildPhase phase) =>
            phase.Resolve(Wave, _ladder, _opening, _costs, _types, _map, _standing);

        /// <summary>
        /// Every tower the roster can stand on a cell outright, cheapest first,
        /// then by id so the order is settled.
        /// </summary>
        private static UnitType[] Buildable(UnitTypeTable types, UpgradeLadder ladder, CostTable costs)
        {
            var towers = new List<UnitType>();

            for (int index = 0; index < types.Count; index++)
            {
                UnitType type = types.Types[index];

                if (type.Role == UnitRole.Placed && !ladder.IsTargetOfAnEdge(type.Id))
                {
                    towers.Add(type);
                }
            }

            towers.Sort((left, right) =>
            {
                int byPrice = costs.PriceOf(Purchase.Unit(left.Id), OneTower)
                    .CompareTo(costs.PriceOf(Purchase.Unit(right.Id), OneTower));

                return byPrice != 0 ? byPrice : left.Id.CompareTo(right.Id);
            });

            return towers.ToArray();
        }
    }
}
