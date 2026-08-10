using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One wave slot: a creep type and a count, or nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Slots are the scarcity that stands in for a second wallet.</b> A slot
    /// spent on a cheap column is a slot not spent on a heavy unit, and how many
    /// a round has is <see cref="Offering.WaveSlots"/> and nothing else.
    /// </para>
    /// <para>
    /// <b>An empty slot is a position rather than an omission.</b> Not sending
    /// banks the gold at the ruleset's interest, so leaving one empty is an
    /// investment measured against every purchase that would have used it.
    /// <c>default</c> is <see cref="Empty"/>, so a slot nobody filled in is the
    /// empty one rather than one creep of a type that does not exist.
    /// </para>
    /// <para>
    /// The counts are bounded where the wave record's are, because a slot
    /// becomes one line of a wave: <c>u16 type_id</c>, <c>u16 count</c>.
    /// </para>
    /// </remarks>
    public readonly struct WaveSlot : IEquatable<WaveSlot>
    {
        /// <summary>The largest a type id or a count may be. Both are <c>u16</c> in a wave record.</summary>
        public const int Largest = 65535;

        private WaveSlot(int typeId, int count)
        {
            TypeId = typeId;
            Count = count;
        }

        /// <summary>A slot left empty.</summary>
        public static WaveSlot Empty => default;

        /// <summary>Which creep this slot sends, or zero where it is empty.</summary>
        public int TypeId { get; }

        /// <summary>How many of them, or zero where it is empty.</summary>
        public int Count { get; }

        /// <summary>Whether this slot sends nothing.</summary>
        public bool IsEmpty => Count == 0;

        /// <summary>This many of one creep type.</summary>
        public static WaveSlot Of(int typeId, int count)
        {
            if (typeId < 1 || typeId > Largest)
            {
                throw new SimulationException(
                    "A wave slot was filled with type id "
                    + typeId.ToString(CultureInfo.InvariantCulture)
                    + ". A filled slot names one row of the unit table, and a slot that names none of them "
                    + "is spelled WaveSlot.Empty rather than as a type nothing defines.");
            }

            if (count < 1 || count > Largest)
            {
                throw new SimulationException(
                    "A wave slot was filled with "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + " of type id "
                    + typeId.ToString(CultureInfo.InvariantCulture)
                    + ". A filled slot sends between 1 and "
                    + Largest.ToString(CultureInfo.InvariantCulture)
                    + " creeps; a slot that sends none is spelled WaveSlot.Empty, so that leaving one empty "
                    + "and naming a creep zero times cannot be two spellings of one wave.");
            }

            return new WaveSlot(typeId, count);
        }

        public static bool operator ==(WaveSlot a, WaveSlot b) => a.Equals(b);

        public static bool operator !=(WaveSlot a, WaveSlot b) => !a.Equals(b);

        public bool Equals(WaveSlot other) => TypeId == other.TypeId && Count == other.Count;

        public override bool Equals(object? obj) => obj is WaveSlot other && Equals(other);

        public override int GetHashCode() => (TypeId << 16) ^ Count;

        public override string ToString() =>
            IsEmpty
                ? "empty"
                : Count.ToString(CultureInfo.InvariantCulture)
                    + " of type "
                    + TypeId.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// What one build phase decided: the option taken, what it built, and how
    /// the wave's slots were filled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is data and not a result.</b> Nothing here has been checked
    /// against an offering, a set of unlocks, a slot width, a board, a map or a
    /// purse -- <see cref="Resolve"/> is where all six happen, and it is public
    /// so that a stored command stream is validated against the same surface a
    /// live build phase is rather than against a second copy of the rules.
    /// </para>
    /// <para>
    /// <b>The actions sit beside the slots because a phase is one decision over
    /// one wallet.</b> A tower and a creep are two rows of one
    /// <see cref="CostTable"/> paid out of one <see cref="Purse"/>, so what a
    /// phase builds and what it sends are not two decisions that happen to
    /// share a round.
    /// </para>
    /// <para>
    /// <b>One take per build phase, and it is not optional.</b> Unlocking is
    /// free, so declining would be a decision nothing rewards; a round's take is
    /// which of the menu, never whether.
    /// </para>
    /// <para>
    /// <b>The filled slots ascend strictly by type id.</b> A slot becomes one
    /// line of a wave, and a wave's lines ascend and are unique on
    /// <c>(tick, type)</c> -- asserted rather than sorted, for the reason
    /// <see cref="WaveScript"/> gives: sorting would leave two identical waves
    /// with two different sets of bytes. It is also what makes two slots on one
    /// creep a refusal rather than a slot silently spent twice.
    /// </para>
    /// <para>
    /// <b>The actions do not ascend by anything, and that is not an
    /// oversight.</b> Their order is meaning -- a phase may upgrade what it has
    /// just placed, and the placement ordinals fall out of the sequence -- so
    /// the same two actions the other way round are a different run rather than
    /// a second spelling of one.
    /// </para>
    /// </remarks>
    public sealed class BuildPhase
    {
        /// <summary>
        /// The tick every slot of a build phase's wave releases on. A build
        /// phase composes what is sent rather than when, so the whole wave
        /// leaves at once and the ordering a wave record asserts falls to the
        /// type ids alone.
        /// </summary>
        private const int ReleaseTick = 0;

        /// <summary>Which lane. The skeleton has one, and it is zero.</summary>
        private const int Corridor = 0;

        /// <summary>How many of one tower an action buys. An action names one cell.</summary>
        private const int OneTower = 1;

        private static readonly BuildAction[] NoActions = new BuildAction[0];

        private readonly WaveSlot[] _slots;

        private readonly BuildAction[] _actions;

        private BuildPhase(OptionKind take, int takeId, WaveSlot[] slots, BuildAction[] actions)
        {
            Take = take;
            TakeId = takeId;
            _slots = slots;
            _actions = actions;
        }

        /// <summary>Which half of the menu this round's take came off.</summary>
        public OptionKind Take { get; }

        /// <summary>Which option of that kind was taken.</summary>
        public int TakeId { get; }

        /// <summary>The slots, in the order they were filled. Empty ones included.</summary>
        public IReadOnlyList<WaveSlot> Slots => _slots;

        /// <summary>What this phase does to the board, in the order it was written.</summary>
        public IReadOnlyList<BuildAction> Actions => _actions;

        /// <summary>What was taken, and what the wave's slots hold.</summary>
        public static BuildPhase Of(OptionKind take, int takeId, params WaveSlot[] slots)
        {
            if (slots is null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (takeId < 1)
            {
                throw new SimulationException(
                    "A build phase takes "
                    + Option.NameOf(take)
                    + " "
                    + takeId.ToString(CultureInfo.InvariantCulture)
                    + ". Every option on an offering carries an identity counted from one, so an id below "
                    + "that is a take nothing on any menu can answer.");
            }

            var copied = new WaveSlot[slots.Length];

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = slots[index];
            }

            return new BuildPhase(take, takeId, copied, NoActions);
        }

        /// <summary>
        /// This phase with one more action after the ones it already carries.
        /// </summary>
        /// <remarks>
        /// The one way an action reaches a decision, and it appends: a phase's
        /// actions are in the order they were written, so there is no position
        /// for an insertion to be given.
        /// </remarks>
        public BuildPhase With(BuildAction action)
        {
            var grown = new BuildAction[_actions.Length + 1];

            for (int index = 0; index < _actions.Length; index++)
            {
                grown[index] = _actions[index];
            }

            grown[_actions.Length] = action;

            return new BuildPhase(Take, TakeId, _slots, grown);
        }

        /// <summary>
        /// Checks this decision against the round it was made in, and turns it
        /// into the board it leaves and the wave it composes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One walk, one way through the purse: the take, then the actions
        /// in the order they were written, then the wave's slots.</b> That is
        /// the order the bytes carry and the order it plays in. Pricing the
        /// slots first would quietly reorder what the author wrote -- the wave
        /// would be bought out of a purse the towers had not been taken out of
        /// yet, and a phase whose towers ate its wave would resolve.
        /// </para>
        /// <para>
        /// <b>Every failure here is a refusal and never a skip</b>, on the rule
        /// the wave loader already applies to an unknown type id: a run that
        /// partially validates cannot produce a confidently wrong result,
        /// because a result it produced is a result somebody will keep. That
        /// covers the wave a phase can no longer afford after building: it is
        /// refused whole rather than emptied, because a run where the towers
        /// ate the wave is a decision and the author's script has to add up.
        /// </para>
        /// <para>
        /// <b>Unlocking happens before buying</b>, so the creep this round's
        /// take just unlocked may be fielded in this round's wave. The two are
        /// one decision over one purse.
        /// </para>
        /// <para>
        /// <b>An upgrade pays the target row's full price and may name any
        /// placeable type.</b> No ladder is read here, so
        /// <c>content/upgrades.txt</c>'s standing claim that the simulation
        /// never walks one is intact.
        /// </para>
        /// </remarks>
        /// <param name="offering">The round's public menu, and the width it carries.</param>
        /// <param name="unlocks">What the run may field, before this round's take.</param>
        /// <param name="purse">What the run has to spend.</param>
        /// <param name="costs">What everything is priced at, units and snapshots alike.</param>
        /// <param name="types">The roster an action's type id names a row of.</param>
        /// <param name="map">The map an action's cell is on, or is not.</param>
        /// <param name="board">What stands before this phase acts.</param>
        public Build Resolve(
            Offering offering,
            Unlocks unlocks,
            Purse purse,
            CostTable costs,
            UnitTypeTable types,
            HexMap map,
            Board board)
        {
            if (offering is null)
            {
                throw new ArgumentNullException(nameof(offering));
            }

            if (unlocks is null)
            {
                throw new ArgumentNullException(nameof(unlocks));
            }

            if (purse is null)
            {
                throw new ArgumentNullException(nameof(purse));
            }

            if (costs is null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            Option taken = offering.Take(Take, TakeId);
            Unlocks after = unlocks.With(taken);

            if (_slots.Length > offering.WaveSlots)
            {
                throw new SimulationException(
                    "A build phase at wave "
                    + offering.Wave.ToString(CultureInfo.InvariantCulture)
                    + " fills "
                    + _slots.Length.ToString(CultureInfo.InvariantCulture)
                    + " slots where that round has "
                    + offering.WaveSlots.ToString(CultureInfo.InvariantCulture)
                    + ". Slot width is derived from the anchor schedule and widens only at anchors, and it "
                    + "is the scarcity that stands in for a second wallet -- so a slot beyond the round's "
                    + "width is refused rather than dropped, which would send a wave nobody composed.");
            }

            Purse left = purse;
            Board built = board;

            for (int index = 0; index < _actions.Length; index++)
            {
                (built, left) = Applied(_actions[index], offering.Wave, built, left, costs, types, map);
            }

            // What the board cost, taken off the purse the actions left rather
            // than by pricing them a second time. The slots are bought below out
            // of what is left, so this is the whole of the defensive half.
            int defense = purse.Gold - left.Gold;
            var orders = new List<UnitOrder>();
            long spent = 0;
            int previousTypeId = 0;

            for (int index = 0; index < _slots.Length; index++)
            {
                WaveSlot slot = _slots[index];

                if (slot.IsEmpty)
                {
                    continue;
                }

                if (!after.Has(slot.TypeId))
                {
                    throw new SimulationException(
                        "A build phase at wave "
                        + offering.Wave.ToString(CultureInfo.InvariantCulture)
                        + " fills slot "
                        + (index + 1).ToString(CultureInfo.InvariantCulture)
                        + " with type id "
                        + slot.TypeId.ToString(CultureInfo.InvariantCulture)
                        + ", which this run never unlocked. It holds "
                        + after.ToString()
                        + ". What may be fielded is bounded by what was chosen, so a creep nobody took is "
                        + "refused rather than bought -- an unlock gate that let one purchase through is a "
                        + "gate nobody has.");
                }

                if (slot.TypeId <= previousTypeId)
                {
                    throw new SimulationException(
                        "A build phase at wave "
                        + offering.Wave.ToString(CultureInfo.InvariantCulture)
                        + " fills slot "
                        + (index + 1).ToString(CultureInfo.InvariantCulture)
                        + " with type id "
                        + slot.TypeId.ToString(CultureInfo.InvariantCulture)
                        + ", at or below the "
                        + previousTypeId.ToString(CultureInfo.InvariantCulture)
                        + " a slot above it already sent. Filled slots ascend strictly by type id, which "
                        + "makes a repeated creep a slot spent twice on one thing and keeps two identical "
                        + "waves from having two different sets of bytes.");
                }

                previousTypeId = slot.TypeId;
                spent += costs.PriceOf(Purchase.Unit(slot.TypeId), slot.Count);
                orders.Add(new UnitOrder(ReleaseTick, after.TypeOf(slot.TypeId), slot.Count, Corridor));
            }

            if (spent > left.Gold)
            {
                throw new SimulationException(
                    "A build phase at wave "
                    + offering.Wave.ToString(CultureInfo.InvariantCulture)
                    + " buys "
                    + spent.ToString(CultureInfo.InvariantCulture)
                    + " gold of creeps out of a purse holding "
                    + left.Gold.ToString(CultureInfo.InvariantCulture)
                    + ". There is no credit in this economy, so a wave nobody can afford is refused where "
                    + "the decision is read rather than borrowed against -- and the whole wave is priced "
                    + "before a coin moves, so a purse is never left part-spent on a wave that was never "
                    + "legal. The phase has already paid for what it built, so a phase whose towers ate "
                    + "its wave is refused whole rather than sent short: the script has to add up.");
            }

            for (int index = 0; index < orders.Count; index++)
            {
                left = left.Spend(costs, Purchase.Unit(orders[index].TypeId), orders[index].Count);
            }

            return new Build(
                taken,
                after,
                left,
                purse.Gold - left.Gold,
                defense,
                WaveScript.FromSlots(orders.ToArray()),
                built);
        }

        public override string ToString() =>
            "take "
            + Option.NameOf(Take)
            + " "
            + TakeId.ToString(CultureInfo.InvariantCulture)
            + ", "
            + (_actions.Length == 0
                ? string.Empty
                : string.Join(", ", Array.ConvertAll(_actions, action => action.ToString())) + ", ")
            + string.Join(" | ", Array.ConvertAll(_slots, slot => slot.ToString()));

        /// <summary>
        /// One action: the board it leaves behind, and the purse it leaves
        /// behind.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Three of the refusals here are the board's own and are reached
        /// rather than restated</b> -- see <see cref="Standing"/>. Rewriting
        /// them would be a second copy of a rule, free to disagree with the
        /// first.
        /// </para>
        /// <para>
        /// <b>Only the position is required, not the prospects.</b>
        /// <see cref="Footing.Possible"/> is refused because a cell off the
        /// grid or inside the corridor is a position that could not have
        /// happened; <see cref="Footing.ReachesRoute"/> is not, because
        /// building somewhere useless is a decision a player is allowed to
        /// make. An upgrade asks neither: the cell already holds a placement,
        /// so it was answered when that placement was made.
        /// </para>
        /// <para>
        /// <b>The position first and then the price.</b> Where an action is
        /// both illegal and unaffordable, what it is told is that no such
        /// action exists -- a price is a thing a script can fix by banking a
        /// round, and a cell that cannot hold a tower is not.
        /// </para>
        /// <para>
        /// <b>Paid as it is applied, in the order it was written.</b> A phase
        /// carries no credit between its own actions, so the second of two
        /// towers is priced against the purse the first one left.
        /// </para>
        /// </remarks>
        private static (Board Built, Purse Left) Applied(
            BuildAction action,
            int wave,
            Board built,
            Purse left,
            CostTable costs,
            UnitTypeTable types,
            HexMap map)
        {
            string naming = Naming(action, wave);
            UnitType type = types.Require(action.TypeId, UnitRole.Placed, naming);

            if (action.Kind == ActionKind.Place)
            {
                Footing footing = Footing.Of(map, type, action.Column, action.Row);

                if (!footing.Possible)
                {
                    throw new SimulationException(naming + ", " + footing.Fault);
                }
            }

            Board after = Standing(built, action, type, wave);
            int price = costs.PriceOf(Purchase.Unit(action.TypeId), OneTower);

            if (price > left.Gold)
            {
                throw new SimulationException(
                    naming
                    + " for "
                    + price.ToString(CultureInfo.InvariantCulture)
                    + " gold out of a purse holding "
                    + left.Gold.ToString(CultureInfo.InvariantCulture)
                    + ". A phase pays for what it builds as it builds it, in the order the actions were "
                    + "written, and there is no credit in this economy -- so an action nobody can afford "
                    + "is refused rather than dropped from a phase somebody else's numbers then describe.");
            }

            return (after, left.Spend(costs, Purchase.Unit(action.TypeId), OneTower));
        }

        /// <summary>
        /// The board one action leaves, with the round it was refused in named
        /// in front of whatever the board said.
        /// </summary>
        /// <remarks>
        /// The three refusals reachable through here -- a <c>place</c> on an
        /// occupied cell, an <c>upgrade</c> on an empty one, and an
        /// <c>upgrade</c> to the type already standing -- are the board's own
        /// rules, and which round asked is this phase's. Rewrapped rather than
        /// restated, on the arrangement a content file's line number is added
        /// by: moving a rule moves the one copy of it.
        /// </remarks>
        private static Board Standing(Board built, BuildAction action, UnitType type, int wave)
        {
            try
            {
                return action.Kind == ActionKind.Place
                    ? built.Place(type, action.Column, action.Row)
                    : built.Upgrade(type, action.Column, action.Row);
            }
            catch (SimulationException refused)
            {
                throw new SimulationException(
                    "A build phase at wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
                    + " cannot act. "
                    + refused.Message);
            }
        }

        /// <summary>
        /// What an action is called in a refusal: the round, the verb and the
        /// cell. It is the subject of every sentence this phase refuses an
        /// action with, and the clause <see cref="Footing.Fault"/> follows.
        /// </summary>
        private static string Naming(BuildAction action, int wave) =>
            "A build phase at wave "
            + wave.ToString(CultureInfo.InvariantCulture)
            + (action.Kind == ActionKind.Place ? " places at column " : " upgrades at column ")
            + action.Column.ToString(CultureInfo.InvariantCulture)
            + ", row "
            + action.Row.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// What a build phase came to: what was taken, what it built, what it left,
    /// and the wave it composed.
    /// </summary>
    /// <remarks>
    /// Returned rather than applied, so that validating a stored decision and
    /// playing a live one are the same call. Whoever wants the round resolved
    /// hands <see cref="Wave"/> to a <see cref="RoundOrders"/> alongside the
    /// layout <see cref="Board"/> derives -- which is the built board, because
    /// the purse walks the take, then the actions, then the slots, so what this
    /// round's incoming waves meet is what this round built.
    /// </remarks>
    public sealed class Build
    {
        internal Build(
            Option taken,
            Unlocks unlocks,
            Purse purse,
            int spent,
            int defense,
            WaveScript wave,
            Board board)
        {
            Taken = taken;
            Unlocks = unlocks;
            Purse = purse;
            Spent = spent;
            Defense = defense;
            Wave = wave;
            Board = board;
        }

        /// <summary>The option this build phase took off the offering.</summary>
        public Option Taken { get; }

        /// <summary>What the run may field afterwards, this round's take included.</summary>
        public Unlocks Unlocks { get; }

        /// <summary>The purse after the phase built and the wave was bought.</summary>
        public Purse Purse { get; }

        /// <summary>
        /// What the phase cost, in gold: what it built and what it sends. One
        /// number because there is one wallet.
        /// </summary>
        public int Spent { get; }

        /// <summary>
        /// The part of <see cref="Spent"/> that stands on the board: what the
        /// placements and the upgrades came to.
        /// </summary>
        /// <remarks>
        /// One bill and two halves, said as a total and a part rather than as
        /// two totals, so nothing holding this has to add them up to get what
        /// the purse moved by. What the wave cost is the difference, and it is
        /// what the cost-efficiency column of a balance report is per -- see
        /// <c>docs/adr/0041</c>.
        /// </remarks>
        public int Defense { get; }

        /// <summary>The wave the filled slots compose. Empty where every slot was left so.</summary>
        public WaveScript Wave { get; }

        /// <summary>The board this phase left behind. The one it was handed, where it acted none.</summary>
        public Board Board { get; }

        public override string ToString() =>
            "took "
            + Taken.ToString()
            + ", spent "
            + Spent.ToString(CultureInfo.InvariantCulture)
            + " of "
            + (Purse.Gold + Spent).ToString(CultureInfo.InvariantCulture)
            + " gold on "
            + Wave.TotalUnits.ToString(CultureInfo.InvariantCulture)
            + " units";
    }
}
