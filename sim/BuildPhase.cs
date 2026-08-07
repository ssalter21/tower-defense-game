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
    /// banks the sauce at the ruleset's interest, so leaving one empty is an
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
    /// What one build phase decided: the option taken, and how the wave's slots
    /// were filled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is data and not a result.</b> Nothing here has been checked
    /// against an offering, a set of unlocks, a slot width or a purse --
    /// <see cref="Resolve"/> is where all four happen, and it is public so that
    /// a stored command stream is validated against the same surface a live
    /// build phase is rather than against a second copy of the rules.
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

        private readonly WaveSlot[] _slots;

        private BuildPhase(OptionKind take, int takeId, WaveSlot[] slots)
        {
            Take = take;
            TakeId = takeId;
            _slots = slots;
        }

        /// <summary>Which half of the menu this round's take came off.</summary>
        public OptionKind Take { get; }

        /// <summary>Which option of that kind was taken.</summary>
        public int TakeId { get; }

        /// <summary>The slots, in the order they were filled. Empty ones included.</summary>
        public IReadOnlyList<WaveSlot> Slots => _slots;

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

            return new BuildPhase(take, takeId, copied);
        }

        /// <summary>
        /// Checks this decision against the round it was made in, and turns it
        /// into the wave it composes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every failure here is a refusal and never a skip</b>, on the rule
        /// the wave loader already applies to an unknown type id: a run that
        /// partially validates cannot produce a confidently wrong result,
        /// because a result it produced is a result somebody will keep.
        /// </para>
        /// <para>
        /// <b>Unlocking happens before buying</b>, so the creep this round's
        /// take just unlocked may be fielded in this round's wave. The two are
        /// one decision over one purse.
        /// </para>
        /// </remarks>
        /// <param name="offering">The round's public menu, and the width it carries.</param>
        /// <param name="unlocks">What the run may field, before this round's take.</param>
        /// <param name="purse">What the run has to spend.</param>
        /// <param name="costs">What everything is priced at, units and snapshots alike.</param>
        public Build Resolve(Offering offering, Unlocks unlocks, Purse purse, CostTable costs)
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
                orders.Add(new UnitOrder(ReleaseTick, TypeOf(after, slot.TypeId), slot.Count, Corridor));
            }

            if (spent > purse.Sauce)
            {
                throw new SimulationException(
                    "A build phase at wave "
                    + offering.Wave.ToString(CultureInfo.InvariantCulture)
                    + " buys "
                    + spent.ToString(CultureInfo.InvariantCulture)
                    + " sauce of creeps out of a purse holding "
                    + purse.Sauce.ToString(CultureInfo.InvariantCulture)
                    + ". There is no credit in this economy, so a wave nobody can afford is refused where "
                    + "the decision is read rather than borrowed against -- and the whole wave is priced "
                    + "before a coin moves, so a purse is never left part-spent on a wave that was never "
                    + "legal.");
            }

            Purse left = purse;

            for (int index = 0; index < orders.Count; index++)
            {
                left = left.Spend(costs, Purchase.Unit(orders[index].TypeId), orders[index].Count);
            }

            return new Build(taken, after, left, (int)spent, WaveScript.FromSlots(orders.ToArray()));
        }

        public override string ToString() =>
            "take "
            + Option.NameOf(Take)
            + " "
            + TakeId.ToString(CultureInfo.InvariantCulture)
            + ", "
            + string.Join(" | ", Array.ConvertAll(_slots, slot => slot.ToString()));

        /// <summary>
        /// The unit row behind an unlocked type id. An unlock carries the row it
        /// was drawn against, so this resolves out of what was taken rather than
        /// out of a table handed in beside it.
        /// </summary>
        private static UnitType TypeOf(Unlocks unlocks, int typeId)
        {
            IReadOnlyList<Option> taken = unlocks.Taken;

            for (int index = 0; index < taken.Count; index++)
            {
                if (taken[index].TypeId == typeId)
                {
                    return taken[index].Type;
                }
            }

            throw new SimulationException(
                "Type id "
                + typeId.ToString(CultureInfo.InvariantCulture)
                + " is unlocked and has no unit row behind it, which cannot happen: an unlock is an option "
                + "and an option carries the row it was drawn from.");
        }
    }

    /// <summary>
    /// What a build phase came to: what was taken, what it left, and the wave it
    /// composed.
    /// </summary>
    /// <remarks>
    /// Returned rather than applied, so that validating a stored decision and
    /// playing a live one are the same call. Whoever wants the round resolved
    /// hands <see cref="Wave"/> to a <see cref="RoundOrders"/> alongside the
    /// defense that stands.
    /// </remarks>
    public sealed class Build
    {
        internal Build(Option taken, Unlocks unlocks, Purse purse, int spent, WaveScript wave)
        {
            Taken = taken;
            Unlocks = unlocks;
            Purse = purse;
            Spent = spent;
            Wave = wave;
        }

        /// <summary>The option this build phase took off the offering.</summary>
        public Option Taken { get; }

        /// <summary>What the run may field afterwards, this round's take included.</summary>
        public Unlocks Unlocks { get; }

        /// <summary>The purse after the wave was bought.</summary>
        public Purse Purse { get; }

        /// <summary>What the wave cost, in sauce.</summary>
        public int Spent { get; }

        /// <summary>The wave the filled slots compose. Empty where every slot was left so.</summary>
        public WaveScript Wave { get; }

        public override string ToString() =>
            "took "
            + Taken.ToString()
            + ", spent "
            + Spent.ToString(CultureInfo.InvariantCulture)
            + " of "
            + (Purse.Sauce + Spent).ToString(CultureInfo.InvariantCulture)
            + " sauce on "
            + Wave.TotalUnits.ToString(CultureInfo.InvariantCulture)
            + " units";
    }
}
