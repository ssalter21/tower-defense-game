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
    /// <b>Nothing bounds how many slots a wave carries.</b> A round sends
    /// whatever its purse reaches; the width that once widened at an anchor is
    /// gone with the anchors, so the wallet is the only scarcity on this side.
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
    /// What one build phase decided: what it built, and how the wave's slots
    /// were filled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is data and not a result.</b> Nothing here has been checked
    /// against the ladder, a board, a map or a purse -- <see cref="Resolve"/> is
    /// where all four happen, and it is public so that a stored command stream
    /// is validated against the same surface a live build phase is rather than
    /// against a second copy of the rules.
    /// </para>
    /// <para>
    /// <b>The actions sit beside the slots because a phase is one decision over
    /// one wallet.</b> A tower and a creep are two rows of one
    /// <see cref="CostTable"/> paid out of one <see cref="Purse"/>, so what a
    /// phase builds and what it sends are not two decisions that happen to
    /// share a round.
    /// </para>
    /// <para>
    /// <b>Nothing is taken and nothing is unlocked.</b> The forced pick, the
    /// menu it was drawn from and the rounds that widened it are gone; every
    /// creep in the roster is sendable from wave one, priced and nothing else.
    /// </para>
    /// <para>
    /// <b>A slot's position is its release order.</b> Slot one's creeps walk
    /// out first, slot two's behind them, and the wave is one column in the
    /// order the slots were filled. That is the vision's <i>you choose the
    /// order they come out in</i>, and until #191 this class did not honour it:
    /// every slot was given the same release tick, so the columns all began
    /// together and a slot's position meant nothing at all.
    /// </para>
    /// <para>
    /// <b>The filled slots used to ascend strictly by type id, and that rule is
    /// gone.</b> It existed to canonicalise an arrangement that was not a
    /// decision -- two spellings of one wave would have been two sets of bytes
    /// for one run. Once position is the release order the arrangement <i>is</i>
    /// the decision, and asserting an order over it would delete the lever the
    /// vision asked for. What survives is the half that was never about
    /// canonicalisation: <b>a creep may fill only one slot of a wave</b>, so a
    /// repeat is still a slot spent twice on one thing.
    /// </para>
    /// <para>
    /// Canonical bytes are not lost with it. The release offsets ascend
    /// strictly across filled slots, because every filled slot sends at least
    /// one creep and each creep takes the column for
    /// <see cref="Match.SpawnIntervalTicks"/> -- so the wave's orders are still
    /// unique and ascending on <c>(tick, type)</c>, which is what
    /// <see cref="WaveScript"/> and <see cref="WaveRecord"/> assert. The
    /// ordering became a consequence of the rule instead of a rule of its own.
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
        /// The tick the first creep of a build phase's wave releases on. Every
        /// creep behind it follows one spawn interval later, whether it is the
        /// next of its own slot or the first of the next slot -- so a wave is
        /// one column at one cadence, and a slot's position is where in that
        /// column its creeps stand.
        /// </summary>
        private const int FirstReleaseTick = 0;

        /// <summary>Which lane. The skeleton has one, and it is zero.</summary>
        private const int Corridor = 0;

        /// <summary>How many of one tower an action buys. An action names one cell.</summary>
        private const int OneTower = 1;

        private static readonly BuildAction[] NoActions = new BuildAction[0];

        private readonly WaveSlot[] _slots;

        private readonly BuildAction[] _actions;

        private BuildPhase(WaveSlot[] slots, BuildAction[] actions)
        {
            _slots = slots;
            _actions = actions;
        }

        /// <summary>The slots, in the order they were filled. Empty ones included.</summary>
        public IReadOnlyList<WaveSlot> Slots => _slots;

        /// <summary>What this phase does to the board, in the order it was written.</summary>
        public IReadOnlyList<BuildAction> Actions => _actions;

        /// <summary>What the wave's slots hold.</summary>
        public static BuildPhase Of(params WaveSlot[] slots)
        {
            if (slots is null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            var copied = new WaveSlot[slots.Length];

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = slots[index];
            }

            return new BuildPhase(copied, NoActions);
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

            return new BuildPhase(_slots, grown);
        }

        /// <summary>
        /// This phase sending a different wave, with the actions it already
        /// carries left where they are.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The wave's half of <see cref="With"/>, and it replaces where that
        /// one appends.</b> An action's position is the order it was written in
        /// and nothing else can be done to it, so appending is the whole of that
        /// verb. A slot's position is the release order -- so a wave is
        /// rearranged, emptied and regrown as well as extended, and there is no
        /// one edit an append could stand for.
        /// </para>
        /// <para>
        /// <b>It exists so that a screen composing a wave does not have to know
        /// how a phase is put together.</b> ADR-0051 has the client hold a phase
        /// in a local and price every change by resolving a candidate; without
        /// this the candidate had to be reassembled from
        /// <see cref="Of"/> and a replay of <see cref="Actions"/>, which is the
        /// view knowing this class's shape well enough to rebuild one -- and
        /// quietly dropping anything a phase gains that those two do not carry.
        /// </para>
        /// <para>
        /// Nothing is checked here, as nothing is checked in <see cref="Of"/>:
        /// a phase is data, and <see cref="Resolve"/> is where a wave meets the
        /// purse and the roster.
        /// </para>
        /// </remarks>
        public BuildPhase Sending(params WaveSlot[] slots)
        {
            if (slots is null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            var copied = new WaveSlot[slots.Length];

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = slots[index];
            }

            return new BuildPhase(copied, _actions);
        }

        /// <summary>
        /// Checks this decision against the round it was made in, and turns it
        /// into the board it leaves and the wave it composes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One walk, one way through the purse: the actions in the order they
        /// were written, then the wave's slots.</b> That is the order the bytes
        /// carry and the order it plays in. Pricing the slots first would
        /// quietly reorder what the author wrote -- the wave would be bought out
        /// of a purse the towers had not been taken out of yet, and a phase
        /// whose towers ate its wave would resolve.
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
        /// <b>A wave carries whatever it can afford.</b> Nothing bounds how many
        /// creep types a round may send and nothing has to be unlocked before it
        /// is bought: the purse is the only scarcity on the sending side.
        /// </para>
        /// <para>
        /// <b>The ladder is read here, and that is a reversal.</b>
        /// <c>content/upgrades.txt</c> long carried a standing claim that the
        /// simulation never walks one; it is now false by intent. A unit that is
        /// any edge's target cannot be <c>place</c>d and has to be reached by
        /// <c>upgrade</c> from the rung below it, which is the one prerequisite
        /// this game has. An upgrade still pays the target row's full price.
        /// </para>
        /// </remarks>
        /// <param name="wave">Which round this is, for the refusals to name.</param>
        /// <param name="ladder">The upgrade edges a <c>place</c> and an <c>upgrade</c> are both refused against.</param>
        /// <param name="purse">What the run has to spend.</param>
        /// <param name="costs">What everything is priced at, units and snapshots alike.</param>
        /// <param name="types">The roster an action's type id names a row of.</param>
        /// <param name="map">The map an action's cell is on, or is not.</param>
        /// <param name="board">What stands before this phase acts.</param>
        public Build Resolve(
            int wave,
            UpgradeLadder ladder,
            Purse purse,
            CostTable costs,
            UnitTypeTable types,
            HexMap map,
            Board board)
        {
            if (ladder is null)
            {
                throw new ArgumentNullException(nameof(ladder));
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

            Purse left = purse;
            Board built = board;

            for (int index = 0; index < _actions.Length; index++)
            {
                (built, left) = Applied(_actions[index], wave, built, left, costs, types, map, ladder);
            }

            // What the board cost, taken off the purse the actions left rather
            // than by pricing them a second time. The slots are bought below out
            // of what is left, so this is the whole of the defensive half.
            int defense = purse.Gold - left.Gold;
            var orders = new List<UnitOrder>();
            long spent = 0;
            var already = new List<int>();

            // Where in the column this slot's first creep stands: behind every
            // creep the slots above it send. An empty slot contributes nothing
            // and costs nothing, so banking a slot closes the gap rather than
            // leaving a hole in the wave.
            int ahead = 0;

            for (int index = 0; index < _slots.Length; index++)
            {
                WaveSlot slot = _slots[index];

                if (slot.IsEmpty)
                {
                    continue;
                }

                if (already.Contains(slot.TypeId))
                {
                    throw new SimulationException(
                        "A build phase at wave "
                        + wave.ToString(CultureInfo.InvariantCulture)
                        + " fills slot "
                        + (index + 1).ToString(CultureInfo.InvariantCulture)
                        + " with type id "
                        + slot.TypeId.ToString(CultureInfo.InvariantCulture)
                        + ", which a slot above it already sent. A creep fills at most one slot of a wave: "
                        + "two slots on one creep is a slot spent twice on one thing, and the same wave is "
                        + "spelled by putting the whole count in one of them. The slots may name their "
                        + "creeps in any order -- the order is the decision -- so this is all that is left "
                        + "of the rule that they ascend.");
                }

                already.Add(slot.TypeId);
                spent += costs.PriceOf(Purchase.Unit(slot.TypeId), slot.Count);
                orders.Add(new UnitOrder(
                    FirstReleaseTick + (ahead * Match.SpawnIntervalTicks),
                    types.Require(slot.TypeId, UnitRole.Moving, Filling(index, wave)),
                    slot.Count,
                    Corridor));

                ahead += slot.Count;
            }

            if (spent > left.Gold)
            {
                throw new SimulationException(
                    "A build phase at wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
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
                left,
                purse.Gold - left.Gold,
                defense,
                WaveScript.FromSlots(orders.ToArray()),
                built);
        }

        public override string ToString() =>
            (_actions.Length == 0
                ? string.Empty
                : string.Join(", ", Array.ConvertAll(_actions, action => action.ToString())) + ", ")
            + string.Join(" | ", Array.ConvertAll(_slots, slot => slot.ToString()));

        /// <summary>
        /// What a slot is called in a refusal: the round and which slot of it.
        /// </summary>
        private static string Filling(int index, int wave) =>
            "A build phase at wave "
            + wave.ToString(CultureInfo.InvariantCulture)
            + " fills slot "
            + (index + 1).ToString(CultureInfo.InvariantCulture);

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
            HexMap map,
            UpgradeLadder ladder)
        {
            string naming = Naming(action, wave);
            UnitType type = types.Require(action.TypeId, UnitRole.Placed, naming);

            if (action.Kind == ActionKind.Place)
            {
                // The one prerequisite this game has. A unit some edge points at
                // is a rung above another, and a rung is only worth being one if
                // the rung below has to be stood first -- so it is refused here
                // rather than priced, and reached by upgrading into.
                if (ladder.IsTargetOfAnEdge(action.TypeId))
                {
                    throw new SimulationException(
                        naming
                        + ". Type id "
                        + action.TypeId.ToString(CultureInfo.InvariantCulture)
                        + " is the target of an upgrade edge, so it is reached by upgrading the rung below "
                        + "it and never placed outright. A tier that can be bought without the tier under "
                        + "it is not a tier, it is a second row at a higher price.");
                }

                Footing footing = Footing.Of(map, type, action.Column, action.Row);

                if (!footing.Possible)
                {
                    throw new SimulationException(naming + ", " + footing.Fault);
                }
            }
            else if (ladder.IsTargetOfAnEdge(action.TypeId))
            {
                // The other half of the same prerequisite. Refusing the place
                // only says the row cannot be bought outright; without this,
                // every standing tower is a rung below it and the ladder ranks
                // nothing. A cell with nothing on it is left to Board.Upgrade,
                // which refuses it in the words that fit.
                UnitType? beneath = built.TypeOn(action.Column, action.Row);

                if (!(beneath is null) && !ladder.HasEdge(beneath.Id, action.TypeId))
                {
                    throw new SimulationException(
                        naming
                        + " into type id "
                        + action.TypeId.ToString(CultureInfo.InvariantCulture)
                        + ", where "
                        + beneath.Label
                        + " stands. The ladder carries no edge from that row to this one, and an upgrade "
                        + "climbs an edge or it is not an upgrade -- a tier reachable from anything "
                        + "standing is a tier with no tier under it, which is the thing refusing the "
                        + "place exists to prevent.");
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
            Purse purse,
            int spent,
            int defense,
            WaveScript wave,
            Board board)
        {
            Purse = purse;
            Spent = spent;
            Defense = defense;
            Wave = wave;
            Board = board;
        }

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
            "spent "
            + Spent.ToString(CultureInfo.InvariantCulture)
            + " of "
            + (Purse.Gold + Spent).ToString(CultureInfo.InvariantCulture)
            + " gold on "
            + Wave.TotalUnits.ToString(CultureInfo.InvariantCulture)
            + " units";
    }
}
