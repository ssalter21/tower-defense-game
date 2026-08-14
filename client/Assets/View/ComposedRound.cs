using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>
        /// One of something: what an action buys, and what a box is filled
        /// with. An action names one cell, and a box that is handed a creep
        /// starts at one of it.
        /// </summary>
        private const int One = 1;

        private readonly UpgradeLadder _ladder;

        private readonly Purse _opening;

        private readonly CostTable _costs;

        private readonly UnitTypeTable _types;

        private readonly HexMap _map;

        private readonly Board _standing;

        private readonly UnitType[] _palette;

        private readonly UnitType[] _roster;

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
            _roster = Walkers(types, costs);

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
        /// in <c>simcli/RoundFrame.cs</c>. See <see cref="Cheapest"/>.
        /// </remarks>
        public IReadOnlyList<UnitType> Palette => _palette;

        /// <summary>
        /// Every creep the roster can send, cheapest first. What a box's list is
        /// drawn from before legality narrows it.
        /// </summary>
        /// <remarks>
        /// <b>All of them, from wave one.</b> There are no unlocks: the
        /// offering, the take and the rounds that widened them came off with
        /// #179, so what a wave may carry is the roster and the only question
        /// left is price. Same ordering rule as <see cref="Palette"/> for the
        /// same reason -- a list that reshuffled itself as the purse moved would
        /// make the thing under the pointer depend on what was last bought.
        /// </remarks>
        public IReadOnlyList<UnitType> Roster => _roster;

        /// <summary>
        /// The wave as it has been composed: one slot per box, in the order they
        /// arrive in.
        /// </summary>
        /// <remarks>
        /// <b>Never an empty slot.</b> The rules model one -- a phase may carry
        /// a slot nobody filled in, and <see cref="BuildPhase.Resolve"/> skips
        /// it -- and nothing on this side produces one: emptying a box takes it
        /// out of the row and closes the gap. That is a narrowing in what the
        /// screen composes rather than a change to what a phase may say, which
        /// is why nothing about it moved in <c>sim</c>. See #197.
        /// </remarks>
        public IReadOnlyList<WaveSlot> Slots => _phase.Slots;

        /// <summary>What one of these costs, out of the table a purchase is actually priced by.</summary>
        public int PriceOf(UnitType type) => PriceOf(type, One);

        /// <summary>What this many of them come to, out of that same table.</summary>
        public int PriceOf(UnitType type, int count)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _costs.PriceOf(Purchase.Unit(type.Id), count);
        }

        /// <summary>
        /// What the box at <paramref name="index"/> is sending. Never null,
        /// because a composed wave carries no empty slot.
        /// </summary>
        public UnitType CreepIn(int index) => _types.ById(Slots[index].TypeId);

        /// <summary>
        /// The creeps that may fill the box at <paramref name="index"/>, in
        /// roster order. What that box's list offers, and the whole of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Three rules, none of them written here.</b> Every one is answered
        /// by resolving a candidate wave and throwing the <see cref="Build"/>
        /// away, exactly as <see cref="Allows"/> does for the board: a creep the
        /// purse cannot cover once the towers have been paid for is absent, and
        /// so is one another box already sends -- <see cref="BuildPhase.Resolve"/>
        /// refuses a duplicate, so the list simply does not offer one. There is
        /// no copy of either rule over here to disagree with <c>sim</c>.
        /// </para>
        /// <para>
        /// <b>The creep already in the box is not in its own list.</b> Choosing
        /// it would be an allocation that allocates nothing, and it would arrive
        /// as a change of count nobody asked for -- see <see cref="Send"/>,
        /// which fills a box with one. Raising a count is
        /// <see cref="SendMore"/>, and it is a separate affordance because it is
        /// a separate decision.
        /// </para>
        /// <para>
        /// <paramref name="index"/> may be one past the last filled box, which
        /// is the trailing empty one: filling it appends a slot and the row
        /// grows by the box behind it. Nothing bounds how far that goes, which
        /// is <c>sim/BuildPhase.cs</c>'s "nothing bounds how many slots a wave
        /// carries" said on screen.
        /// </para>
        /// </remarks>
        public IReadOnlyList<UnitType> Sendable(int index)
        {
            var creeps = new List<UnitType>();

            if (index < 0 || index > Slots.Count)
            {
                return creeps;
            }

            int standing = index < Slots.Count ? Slots[index].TypeId : 0;

            foreach (UnitType creep in _roster)
            {
                if (creep.Id != standing && Resolves(Rewritten(index, WaveSlot.Of(creep.Id, One))))
                {
                    creeps.Add(creep);
                }
            }

            return creeps;
        }

        /// <summary>
        /// Whether the box at <paramref name="index"/> could send one more than
        /// it does. False where there is no such box.
        /// </summary>
        public bool CanSendMore(int index) =>
            index >= 0
            && index < Slots.Count
            && Resolves(Rewritten(index, WaveSlot.Of(Slots[index].TypeId, Slots[index].Count + One)));

        /// <summary>
        /// Puts one of <paramref name="creep"/> in the box at
        /// <paramref name="index"/>, appending a box where that is one past the
        /// end.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One of them, never the count the box was holding.</b> A box
        /// holding three of something and handed something dearer would either
        /// refuse or spend the round's purse on a click that read as "this one
        /// instead". Starting at one is the reading that is affordable wherever
        /// the creep was offered at all, because one of it is exactly what
        /// <see cref="Sendable"/> priced.
        /// </para>
        /// <para>
        /// The refusal is not caught, for the reason it is not caught in
        /// <see cref="Do"/>: every box asks <see cref="Sendable"/> first, so a
        /// creep arriving here that does not resolve means something offered
        /// what it should not have.
        /// </para>
        /// </remarks>
        public void Send(int index, UnitType creep)
        {
            if (creep is null)
            {
                throw new ArgumentNullException(nameof(creep));
            }

            // One past the end is the trailing empty box and appends. Anything
            // further along is a box that does not exist, and it is refused
            // rather than clamped: every caller of these four asks Sendable or
            // CanSendMore first, so an index arriving here that names nothing
            // means something on screen counted the row wrong, which wants a
            // stack trace and not a wave quietly composed at the wrong position.
            Bounded(index, Slots.Count);
            Compose(Rewritten(index, WaveSlot.Of(creep.Id, One)));
        }

        /// <summary>Sends one more of what the box at <paramref name="index"/> holds.</summary>
        public void SendMore(int index)
        {
            Bounded(index, Slots.Count - One);
            Compose(Rewritten(index, WaveSlot.Of(Slots[index].TypeId, Slots[index].Count + One)));
        }

        /// <summary>
        /// Sends one fewer. At one this empties the box, which takes it out of
        /// the row and closes the gap behind it.
        /// </summary>
        /// <remarks>
        /// Lowering a count is never refused -- it is the same wave for less
        /// gold -- so there is no offering call to ask first, and the resolve
        /// this goes through is what re-prices the purse rather than what
        /// permits it.
        /// </remarks>
        public void SendFewer(int index)
        {
            Bounded(index, Slots.Count - One);

            WaveSlot slot = Slots[index];

            if (slot.Count <= One)
            {
                SendNone(index);

                return;
            }

            Compose(Rewritten(index, WaveSlot.Of(slot.TypeId, slot.Count - One)));
        }

        /// <summary>
        /// Takes the box at <paramref name="index"/> out of the row. What is
        /// behind it closes up, so a composed wave never carries a hole.
        /// </summary>
        public void SendNone(int index)
        {
            Bounded(index, Slots.Count - One);
            Compose(Without(index));
        }

        /// <summary>
        /// Moves the box at <paramref name="from"/> to <paramref name="to"/>,
        /// which is what dragging one does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is a decision and not a tidy-up.</b> A slot's position is its
        /// release order -- ADR-0051, and <c>sim/BuildPhase.cs</c> -- so the box
        /// dragged to the front is the creep that walks out first, and the same
        /// creeps in another order are a different round rather than a second
        /// spelling of one.
        /// </para>
        /// <para>
        /// It cannot be refused. Rearranging changes neither the bill nor which
        /// creeps are sent, so a wave that resolved resolves in every order. The
        /// resolve below is what re-prices what is on screen rather than what
        /// permits the move, and it is here because every other verb goes
        /// through it and a second path that skipped it is the one that would
        /// leave the purse stale.
        /// </para>
        /// </remarks>
        public void Rearrange(int from, int to)
        {
            if (from < 0 || from >= Slots.Count || to < 0 || to >= Slots.Count || from == to)
            {
                return;
            }

            WaveSlot moved = Slots[from];
            WaveSlot[] shortened = Without(from);
            var rearranged = new WaveSlot[shortened.Length + 1];

            for (int index = 0; index < rearranged.Length; index++)
            {
                rearranged[index] = index == to ? moved : shortened[index < to ? index : index - 1];
            }

            Compose(rearranged);
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
        /// Whether a wave would resolve on this phase's actions. The wave half
        /// of <see cref="Allows"/>, and the same discarded candidate.
        /// </summary>
        private bool Resolves(WaveSlot[] slots)
        {
            try
            {
                Resolve(_phase.Sending(slots));

                return true;
            }
            catch (SimulationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Takes a composed wave, and takes the purse and the board that follow
        /// from it. The one write the wave verbs above all end at.
        /// </summary>
        /// <remarks>
        /// The refusal is not caught, for the reason it is not caught in
        /// <see cref="Do"/>. Everything that reaches here either asked
        /// <see cref="Sendable"/> or <see cref="CanSendMore"/> first, or is a
        /// move that spends nothing.
        /// </remarks>
        private void Compose(WaveSlot[] slots)
        {
            BuildPhase sending = _phase.Sending(slots);

            _resolved = Resolve(sending);
            _phase = sending;
        }

        /// <summary>
        /// Refuses a box that is not in the row. <paramref name="last"/> is the
        /// furthest position the verb accepts.
        /// </summary>
        private static void Bounded(int index, int last)
        {
            if (index < 0 || index > last)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "There is no such box in the composed wave. The row runs from 0 to "
                    + last.ToString(CultureInfo.InvariantCulture)
                    + " for this verb.");
            }
        }

        /// <summary>
        /// The composed wave with one box's slot replaced, or with a box
        /// appended where <paramref name="index"/> is one past the end.
        /// </summary>
        private WaveSlot[] Rewritten(int index, WaveSlot slot)
        {
            IReadOnlyList<WaveSlot> slots = Slots;
            var rewritten = new WaveSlot[index < slots.Count ? slots.Count : slots.Count + 1];

            for (int at = 0; at < rewritten.Length; at++)
            {
                rewritten[at] = at == index ? slot : slots[at];
            }

            return rewritten;
        }

        /// <summary>The composed wave with one box taken out and the gap closed.</summary>
        private WaveSlot[] Without(int index)
        {
            IReadOnlyList<WaveSlot> slots = Slots;
            var shortened = new WaveSlot[slots.Count - 1];

            for (int at = 0; at < shortened.Length; at++)
            {
                shortened[at] = slots[at < index ? at : at + 1];
            }

            return shortened;
        }

        /// <summary>
        /// Every tower the roster can stand on a cell outright, cheapest first.
        /// </summary>
        /// <remarks>
        /// A row some edge of the ladder points at is left off, because it is
        /// refused to <c>place</c> and reached by upgrading the rung below it.
        /// </remarks>
        private static UnitType[] Buildable(UnitTypeTable types, UpgradeLadder ladder, CostTable costs) =>
            Cheapest(types, costs, type => type.Role == UnitRole.Placed && !ladder.IsTargetOfAnEdge(type.Id));

        /// <summary>Every creep the roster can send, cheapest first.</summary>
        /// <remarks>
        /// No filter beyond the role. A tower is left off the palette when the
        /// ladder points at it, because placing one is refused; there is no
        /// equivalent on this side -- a wave has no prerequisites and no
        /// unlocks, so every walking row is sendable and price is the only
        /// question. See ADR-0051 and #179.
        /// </remarks>
        private static UnitType[] Walkers(UnitTypeTable types, CostTable costs) =>
            Cheapest(types, costs, type => type.Role == UnitRole.Moving);

        /// <summary>
        /// The rows of a roster that <paramref name="wanted"/> keeps, cheapest
        /// first and then by id so the order is settled.
        /// </summary>
        /// <remarks>
        /// One ordering for both lists, because they are the same list read two
        /// ways: the palette and the wave bar are the two halves of one purse,
        /// and a bar that sorted by price beside one that sorted by id would be
        /// two answers to a question nobody asked twice. Same rule and the same
        /// order as the command line's panel in <c>simcli/RoundFrame.cs</c>.
        /// </remarks>
        private static UnitType[] Cheapest(
            UnitTypeTable types, CostTable costs, Func<UnitType, bool> wanted)
        {
            var rows = new List<UnitType>();

            for (int index = 0; index < types.Count; index++)
            {
                UnitType type = types.Types[index];

                if (wanted(type))
                {
                    rows.Add(type);
                }
            }

            rows.Sort((left, right) =>
            {
                int byPrice = costs.PriceOf(Purchase.Unit(left.Id), One)
                    .CompareTo(costs.PriceOf(Purchase.Unit(right.Id), One));

                return byPrice != 0 ? byPrice : left.Id.CompareTo(right.Id);
            });

            return rows.ToArray();
        }
    }
}
