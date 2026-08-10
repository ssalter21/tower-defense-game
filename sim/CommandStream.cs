using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One build phase as the record carries it: the wave it was decided in, and
    /// the decision -- the take, what it did to the board, and how the wave's
    /// slots were filled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing else is stored, because nothing else has to be.</b> The
    /// offering the take was made off is a pure function of the run's seed and
    /// the wave, so it is redrawn at load rather than carried; storing it would
    /// be a second copy of a derivation, free to disagree with the first.
    /// </para>
    /// <para>
    /// <b>The filled slots ascend strictly by type id here as well as at
    /// load.</b> A command that could be built and not read back would be a
    /// writer emitting bytes its own reader refuses, so the order is asserted
    /// where a command is made and asserted again where one is read.
    /// </para>
    /// <para>
    /// <b>The actions do not, and that is not an oversight.</b> Their order is
    /// the phase's own: a phase may upgrade what it just placed, and the
    /// placement ordinals depend on the sequence, so two orderings of the same
    /// two actions are two different runs. Nothing here sorts them and nothing
    /// asserts an order over them.
    /// </para>
    /// </remarks>
    public sealed class RecordCommand : IEquatable<RecordCommand>
    {
        private static readonly BuildAction[] NoActions = new BuildAction[0];

        private readonly WaveSlot[] _slots;

        private readonly BuildAction[] _actions;

        private RecordCommand(
            int wave,
            OptionKind take,
            int takeId,
            WaveSlot[] slots,
            BuildAction[] actions)
        {
            Wave = wave;
            Take = take;
            TakeId = takeId;
            _slots = slots;
            _actions = actions;
        }

        /// <summary>Which wave of the run this decision was made in. Counted from one.</summary>
        public int Wave { get; }

        /// <summary>Which half of that round's menu the take came off.</summary>
        public OptionKind Take { get; }

        /// <summary>Which option of that kind was taken.</summary>
        public int TakeId { get; }

        /// <summary>The slots, in the order they were filled. Empty ones included.</summary>
        public IReadOnlyList<WaveSlot> Slots => _slots;

        /// <summary>What this phase did to the board, in the order it was written.</summary>
        /// <remarks>
        /// <b>Command stream format version 0 has no field for these.</b> A
        /// command carrying actions writes the same bytes as one without them
        /// and reads back with none, so an authored script is the only thing
        /// that carries an action today. The format bump that stores them is
        /// where <see cref="ToPhase"/> starts handing them on as well.
        /// </remarks>
        public IReadOnlyList<BuildAction> Actions => _actions;

        /// <summary>A build phase's decision, stamped with the wave it was made in.</summary>
        public static RecordCommand Of(int wave, BuildPhase decision)
        {
            if (decision is null)
            {
                throw new ArgumentNullException(nameof(decision));
            }

            if (wave < 1)
            {
                throw new SimulationException(
                    "A command is stored for wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
                    + ". Waves are counted from one, so a wave below that is a round index nobody turned "
                    + "into a wave number, and it names a round no run ever plays.");
            }

            var slots = new WaveSlot[decision.Slots.Count];
            int previousTypeId = 0;

            for (int index = 0; index < slots.Length; index++)
            {
                WaveSlot slot = decision.Slots[index];

                if (!slot.IsEmpty)
                {
                    if (slot.TypeId <= previousTypeId)
                    {
                        throw new SimulationException(
                            "A command for wave "
                            + wave.ToString(CultureInfo.InvariantCulture)
                            + " fills slot "
                            + (index + 1).ToString(CultureInfo.InvariantCulture)
                            + " with type id "
                            + slot.TypeId.ToString(CultureInfo.InvariantCulture)
                            + ", at or below the "
                            + previousTypeId.ToString(CultureInfo.InvariantCulture)
                            + " a slot above it already sent. Filled slots ascend strictly by type id, and a "
                            + "command that could be written and not read back is a writer emitting bytes its "
                            + "own reader refuses.");
                    }

                    previousTypeId = slot.TypeId;
                }

                slots[index] = slot;
            }

            return new RecordCommand(wave, decision.Take, decision.TakeId, slots, NoActions);
        }

        /// <summary>
        /// The same, spelled out, for whoever is composing a decision rather
        /// than recording one that was made. The take is checked by
        /// <see cref="BuildPhase.Of"/>, which is where that rule lives.
        /// </summary>
        public static RecordCommand Of(int wave, OptionKind take, int takeId, params WaveSlot[] slots) =>
            Of(wave, BuildPhase.Of(take, takeId, slots));

        /// <summary>
        /// This command with one more action after the ones it already carries.
        /// </summary>
        /// <remarks>
        /// A new command rather than a moved one, for the reason
        /// <see cref="Board"/> is a value: a phase is composed a row at a time,
        /// and appending is the only thing anything does to the list -- there is
        /// no order for an insertion to find a place in.
        /// </remarks>
        public RecordCommand With(BuildAction action)
        {
            var grown = new BuildAction[_actions.Length + 1];

            for (int index = 0; index < _actions.Length; index++)
            {
                grown[index] = _actions[index];
            }

            grown[_actions.Length] = action;

            return new RecordCommand(Wave, Take, TakeId, _slots, grown);
        }

        public static bool operator ==(RecordCommand? a, RecordCommand? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(RecordCommand? a, RecordCommand? b) => !(a == b);

        /// <summary>
        /// The decision as the build phase surface wants it, with nothing
        /// reshaped: the three things stored are the three things
        /// <see cref="BuildPhase.Of"/> takes.
        /// </summary>
        public BuildPhase ToPhase() => BuildPhase.Of(Take, TakeId, _slots);

        public bool Equals(RecordCommand? other)
        {
            if (other is null
                || Wave != other.Wave
                || Take != other.Take
                || TakeId != other.TakeId
                || _slots.Length != other._slots.Length
                || _actions.Length != other._actions.Length)
            {
                return false;
            }

            for (int index = 0; index < _slots.Length; index++)
            {
                if (_slots[index] != other._slots[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < _actions.Length; index++)
            {
                if (_actions[index] != other._actions[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as RecordCommand);

        public override int GetHashCode() =>
            (((Wave * 31 ^ (int)Take) * 31 ^ TakeId) * 31 ^ _slots.Length) * 31 ^ _actions.Length;

        public override string ToString() =>
            "wave "
            + Wave.ToString(CultureInfo.InvariantCulture)
            + ": take "
            + Option.NameOf(Take)
            + " "
            + TakeId.ToString(CultureInfo.InvariantCulture)
            + ", "
            + (_actions.Length == 0
                ? string.Empty
                : string.Join(", ", Array.ConvertAll(_actions, action => action.ToString())) + ", ")
            + string.Join(" | ", Array.ConvertAll(_slots, slot => slot.ToString()));
    }

    /// <summary>
    /// A run's build phases in one run of bytes: the seed every draw in the run
    /// comes from, and the <c>(wave index, decision)</c> pairs the run consumes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the only route a player's decision takes into the
    /// simulation.</b> The view emits a command, the command goes into the
    /// record, and the record is what the run consumes -- so a played run is a
    /// replayable record and every playtest is also a determinism test. It is
    /// also what makes a submitted turn a command batch, which is the whole of
    /// what a submission barrier needs.
    /// </para>
    /// <para>
    /// <b>The seed is here for the same reason it is in a replay bundle.</b> A
    /// round's offering is drawn from the run's seed and the wave, so a stream
    /// without a seed would be checked against whichever menu the caller
    /// happened to draw -- and a take validated against a different offering is
    /// a decision read out of a different game.
    /// </para>
    /// <para>
    /// <b>Three content hashes, three gates.</b> The unit table's sits in the
    /// shared header where every kind carries it. The ruleset's and the anchor
    /// schedule's sit beside it because this is the first record kind whose
    /// meaning depends on them: the ruleset prices the wave, opens the purse and
    /// pays the interest, and the schedule decides where the anchors are and how
    /// wide a round's slots get. A stream replayed against either of them
    /// retuned is a confidently wrong result, so each is compared on its own and
    /// each refuses by its own name.
    /// </para>
    /// <para>
    /// <b>Reading and replaying are different gates.</b> Reading needs a known
    /// format version and canonical bytes. Replaying needs the simulation
    /// version and all three content hashes to match the run in front of it, and
    /// a stream that fails one of those is still perfectly readable.
    /// </para>
    /// </remarks>
    public sealed class CommandStream : IEquatable<CommandStream>
    {
        private readonly RecordCommand[] _commands;

        private CommandStream(
            RecordHeader header,
            Hash64 rulesetHash,
            Hash64 scheduleHash,
            ulong seed,
            RecordCommand[] commands)
        {
            Header = header;
            RulesetHash = rulesetHash;
            ScheduleHash = scheduleHash;
            Seed = seed;
            _commands = commands;
        }

        /// <summary>Magic, format version, simulation version, unit table content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>The hash of the parsed ruleset these decisions were made under.</summary>
        public Hash64 RulesetHash { get; }

        /// <summary>The hash of the parsed anchor schedule they were made against.</summary>
        public Hash64 ScheduleHash { get; }

        /// <summary>The seed every draw in the run is derived from.</summary>
        public ulong Seed { get; }

        /// <summary>The build phases, ascending strictly by wave. Asserted at load.</summary>
        public IReadOnlyList<RecordCommand> Commands => _commands;

        /// <summary>How many build phases there are.</summary>
        public int Count => _commands.Length;

        /// <summary>
        /// Records a run's build phases, at the current format version, stamped
        /// with the seed and the three tables that run is playing.
        /// </summary>
        /// <remarks>
        /// The stamps come off the run rather than off arguments beside it, so a
        /// stream cannot be stamped with one ruleset and made under another.
        /// </remarks>
        public static CommandStream Of(Run run, IReadOnlyList<RecordCommand> commands)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (commands is null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            if (commands.Count == 0)
            {
                throw new SimulationException(
                    "A command stream was recorded with no build phases in it. A stream is what a run "
                    + "consumes, and a run that decided nothing is a run nobody played.");
            }

            var copied = new RecordCommand[commands.Count];
            int previousWave = 0;

            for (int index = 0; index < copied.Length; index++)
            {
                RecordCommand command = commands[index]
                    ?? throw new SimulationException(
                        "The command at index "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " of a command stream is nothing at all. Every build phase of a run is a decision, "
                        + "and a round with no decision in it is a round the run never reached.");

                if (command.Wave <= previousWave)
                {
                    throw new SimulationException(
                        "A command stream stores wave "
                        + command.Wave.ToString(CultureInfo.InvariantCulture)
                        + " after wave "
                        + previousWave.ToString(CultureInfo.InvariantCulture)
                        + ". Build phases ascend strictly by wave, because a run plays them in the order they "
                        + "are stored and two decisions for one round is two runs written down as one.");
                }

                previousWave = command.Wave;
                copied[index] = command;
            }

            return new CommandStream(
                RecordHeader.Current(RecordKind.Command, run.Types.ContentHash),
                run.Rules.ContentHash,
                run.Schedule.ContentHash,
                run.Seed,
                copied);
        }

        /// <summary>
        /// The bytes of a stream that has been proved: recorded, read back from
        /// its own output, taken through the replay gate and played to the end
        /// before they are handed over.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nothing is returned that will not replay</b>, which is the rule
        /// the record verb of the command line already follows for a replay
        /// bundle. A stored stream that cannot be played is a stored stream
        /// nobody finds out about until they try.
        /// </para>
        /// <para>
        /// The run handed in is the one the stream is played into, so it has to
        /// be a run that has not started; it comes back played, and the rounds
        /// that proving the bytes produced come back beside them rather than
        /// being thrown away for whoever wants them to play the stream again.
        /// </para>
        /// </remarks>
        /// <param name="run">A fresh run, on the seed and the tables the stream is stamped with.</param>
        /// <param name="defense">What stands while each of the run's waves is sent.</param>
        /// <param name="commands">The build phases to record.</param>
        public static (byte[] Bytes, IReadOnlyList<RoundReport> Rounds) Recorded(
            Run run,
            TowerLayout defense,
            IReadOnlyList<RecordCommand> commands)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (run.Round != 0)
            {
                throw new SimulationException(
                    "A command stream was recorded into a run that has already played "
                    + run.Round.ToString(CultureInfo.InvariantCulture)
                    + " rounds. The bytes are proved by playing them, and a run part-way through one is a run "
                    + "whose remaining rounds are not the rounds the stream stores.");
            }

            byte[] bytes = Of(run, commands).ToBytes();

            return (bytes, FromBytes(bytes).Replay(run, defense));
        }

        /// <summary>Reads a command stream from bytes. The read gate, and nothing else.</summary>
        public static CommandStream FromBytes(byte[] bytes) => FromBytes("command stream", bytes);

        /// <summary>Reads a command stream from bytes, naming them in any error message.</summary>
        public static CommandStream FromBytes(string record, byte[] bytes)
        {
            var cursor = new ByteCursor(record, bytes);
            RecordHeader header = RecordHeader.Read(cursor, RecordKind.Command);

            CommandStream read;

            switch (header.FormatVersion)
            {
                case 0:
                    read = ReadVersion0(cursor, header);
                    break;

                default:
                    throw cursor.Fault(
                        "is command stream format version "
                        + header.FormatVersion.ToString(CultureInfo.InvariantCulture)
                        + ", which the read gate accepted and this reader has no branch for. The two lists "
                        + "have drifted apart, which is a fault in this build rather than in the record.");
            }

            cursor.ExpectEnd("commands");

            return read;
        }

        /// <summary>The bytes. Always the current format version -- there is one writer.</summary>
        public byte[] ToBytes()
        {
            int size = RecordFormat.HeaderBytes + 8 + 8 + 8 + 2;

            for (int index = 0; index < _commands.Length; index++)
            {
                size += RecordFormat.CommandBytes + (_commands[index].Slots.Count * RecordFormat.SlotBytes);
            }

            var writer = new ByteWriter(size);

            Header.Write(writer);
            writer.U64(RulesetHash.Value);
            writer.U64(ScheduleHash.Value);
            writer.U64(Seed);
            writer.U16("command count", _commands.Length);

            for (int index = 0; index < _commands.Length; index++)
            {
                RecordCommand command = _commands[index];

                writer.U16("command wave", command.Wave);
                writer.U8("command take kind", (int)command.Take);
                writer.U16("command take id", command.TakeId);
                writer.U16("command slot count", command.Slots.Count);

                for (int slot = 0; slot < command.Slots.Count; slot++)
                {
                    writer.U16("slot type id", command.Slots[slot].TypeId);
                    writer.U16("slot count", command.Slots[slot].Count);
                }
            }

            return writer.ToArray();
        }

        /// <summary>
        /// Walks the whole stream against the run in front of it and checks
        /// every decision, before a round is played.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Every failure here is a refusal and never a skip.</b> A take
        /// naming an option that round's offering did not carry, a slot naming a
        /// creep the run never unlocked, a slot beyond the round's width and a
        /// wave nobody can afford are all
        /// <see cref="BuildPhase.Resolve(Offering, Unlocks, Purse, CostTable)"/>
        /// refusing -- the same surface a live build phase is checked by, so
        /// there is one implementation of the rules and not two.
        /// </para>
        /// <para>
        /// <b>The one check that surface cannot make is the wave index.</b>
        /// <c>Resolve</c> is handed an offering and has no way to know which
        /// round is about to be played, so a decision made at wave seven and
        /// stored at wave three would resolve perfectly against wave three's
        /// menu. It is checked here, where both numbers are in hand.
        /// </para>
        /// <para>
        /// <b>Nothing is applied.</b> The unlocks and the purse are folded
        /// forward through local values exactly as a round moves them: a build
        /// phase's take, then the wave's own purchases, then what the wave pays.
        /// The run is untouched, so a stream can be checked and then refused
        /// without the run having moved, and nothing here is handed a defense to
        /// play a round with even by accident.
        /// </para>
        /// <para>
        /// <b>The purse this walk carries is a ceiling and not the run's own.</b>
        /// A wave's income includes the band its offense reached in the field,
        /// and what a round got past the field is a number only a resolved round
        /// has -- so the walk closes every wave at
        /// <see cref="Purse.CloseWaveAtBest"/>, the most the bands can pay. Every
        /// decision refused here is one no run could have afforded however well
        /// it played; a decision the ceiling admits is checked again, against the
        /// purse the round really holds, by the same
        /// <see cref="BuildPhase.Resolve(Offering, Unlocks, Purse, CostTable)"/>
        /// when the round is played. Bounded the other way -- at no bonus -- this
        /// would refuse waves the run affords perfectly well.
        /// </para>
        /// </remarks>
        /// <param name="run">The run these decisions are about to be played into.</param>
        public IReadOnlyList<Build> Check(Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            RequireRoundsLeftFor(run);

            var builds = new List<Build>();
            Unlocks unlocks = run.Unlocks;
            Purse purse = run.Purse;
            int round = run.Round;

            for (int index = 0; index < _commands.Length; index++)
            {
                RecordCommand command = _commands[index];
                round++;

                if (command.Wave != round)
                {
                    throw new SimulationException(
                        "Build phase "
                        + (index + 1).ToString(CultureInfo.InvariantCulture)
                        + " of a command stream is stored for wave "
                        + command.Wave.ToString(CultureInfo.InvariantCulture)
                        + " where the run is about to play round "
                        + round.ToString(CultureInfo.InvariantCulture)
                        + ". The wave index says which round a decision was made in, and playing it at "
                        + "another round would resolve it against an offering nobody was shown.");
                }

                Build build = command.ToPhase().Resolve(run.OfferingAt(round), unlocks, purse, run.Costs);

                unlocks = build.Unlocks;
                purse = build.Purse.CloseWaveAtBest(run.Rules).Purse;
                builds.Add(build);
            }

            return builds;
        }

        /// <summary>
        /// The replay gate, and the rounds on the other side of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Four stamps declared to <see cref="ReplayGate"/>: the simulation
        /// version against this build, and the unit table, the ruleset and the
        /// anchor schedule against the ones the run is playing. They are
        /// independent, so a stream can fail exactly one of them; a stream that
        /// fails several is named by the first declared.
        /// </para>
        /// <para>
        /// The seed is checked before any of them and refuses differently,
        /// because it is not a record that has gone historical -- it is a stream
        /// held up against a run that was never the one it stores.
        /// </para>
        /// <para>
        /// <b>The whole stream is checked before the first round is played.</b>
        /// A run that partially validates would resolve three rounds, refuse the
        /// fourth and leave an outcome somebody keeps. Everything a walk can
        /// settle without playing the run is settled there; what it cannot is
        /// how much a wave's own performance paid, so a decision affordable only
        /// under the best band the run did not reach is the one refusal that
        /// still lands mid-run.
        /// </para>
        /// <para>
        /// <b>Every round comes back.</b> What each one took, what its wave cost
        /// and what the wave paid are settled while it is played, so they are
        /// handed over rather than dropped -- a report of a stored run's
        /// economics is then a walk over these, and never a second play.
        /// </para>
        /// </remarks>
        /// <param name="run">The run to play, on the seed and the tables this stream is stamped with.</param>
        /// <param name="defense">What stands while each of the run's waves is sent.</param>
        public IReadOnlyList<RoundReport> Replay(Run run, TowerLayout defense)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (defense is null)
            {
                throw new ArgumentNullException(nameof(defense));
            }

            if (run.Seed != Seed)
            {
                throw new SimulationException(
                    "A command stream stores the run seeded "
                    + Seed.ToString(CultureInfo.InvariantCulture)
                    + " and it was handed the run seeded "
                    + run.Seed.ToString(CultureInfo.InvariantCulture)
                    + ". Every offering, filling and field in a run is derived from its seed, so these are "
                    + "two different runs and the decisions of one were read off the other's menus.");
            }

            ReplayGate.Require(
                Stamp.Of("simulation version", Header.SimVersion, SimulationVersion.Current),
                Stamp.Of("content", Header.ContentHash, run.Types.ContentHash),
                Stamp.Of("ruleset", RulesetHash, run.Rules.ContentHash),
                Stamp.Of("schedule", ScheduleHash, run.Schedule.ContentHash));

            Check(run);

            var rounds = new List<RoundReport>();

            for (int index = 0; index < _commands.Length; index++)
            {
                rounds.Add(run.Advance(_commands[index].ToPhase(), defense));
            }

            return rounds;
        }

        public bool Equals(CommandStream? other)
        {
            if (other is null
                || Header != other.Header
                || RulesetHash != other.RulesetHash
                || ScheduleHash != other.ScheduleHash
                || Seed != other.Seed
                || _commands.Length != other._commands.Length)
            {
                return false;
            }

            for (int index = 0; index < _commands.Length; index++)
            {
                if (_commands[index] != other._commands[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as CommandStream);

        public override int GetHashCode() => (Header.GetHashCode() * 31) ^ _commands.Length;

        public override string ToString() =>
            Header.ToString()
            + ", ruleset "
            + RulesetHash.ToString()
            + ", schedule "
            + ScheduleHash.ToString()
            + ", seed "
            + Seed.ToString(CultureInfo.InvariantCulture)
            + ", "
            + _commands.Length.ToString(CultureInfo.InvariantCulture)
            + " build phases";

        /// <summary>
        /// Version 0: <c>u64 ruleset_hash + u64 schedule_hash + u64 seed +
        /// u16 command_count + Command[]</c>, where a command is
        /// <c>u16 wave + u8 take_kind + u16 take_id + u16 slot_count</c>
        /// followed by that many <c>(u16 type_id, u16 count)</c> slots.
        /// </summary>
        /// <remarks>
        /// This branch never goes away; a later version gets a branch beside it.
        /// </remarks>
        private static CommandStream ReadVersion0(ByteCursor cursor, RecordHeader header)
        {
            ulong rulesetHash = cursor.U64("the ruleset hash");
            ulong scheduleHash = cursor.U64("the schedule hash");
            ulong seed = cursor.U64("the run seed");
            int count = cursor.U16("the command count");

            if (count == 0)
            {
                throw cursor.Fault(
                    "decides nothing at all. A stream is what a run consumes, and a run of no build phases "
                    + "is a run nobody played.");
            }

            var commands = new RecordCommand[count];
            int previousWave = 0;

            for (int index = 0; index < count; index++)
            {
                string what =
                    "build phase "
                    + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + count.ToString(CultureInfo.InvariantCulture);

                int wave = cursor.U16("the wave of " + what);
                int take = cursor.U8("the take kind of " + what);
                int takeId = cursor.U16("the take id of " + what);
                int slotCount = cursor.U16("the slot count of " + what);

                if (wave == 0)
                {
                    throw cursor.Fault(
                        what + " is stored for wave 0, and waves are counted from one.");
                }

                if (wave <= previousWave)
                {
                    throw cursor.Fault(
                        what
                        + " is stored for wave "
                        + wave.ToString(CultureInfo.InvariantCulture)
                        + ", at or below the "
                        + previousWave.ToString(CultureInfo.InvariantCulture)
                        + " above it. Build phases ascend strictly by wave: they are played in the order "
                        + "they are stored, and the order is asserted rather than sorted so that two "
                        + "identical runs cannot have two different sets of bytes.");
                }

                if (take != (int)OptionKind.Ordinary && take != (int)OptionKind.GameChanger)
                {
                    throw cursor.Fault(
                        what
                        + " takes option kind "
                        + take.ToString(CultureInfo.InvariantCulture)
                        + ", and the kinds an offering has halves for are "
                        + ((int)OptionKind.Ordinary).ToString(CultureInfo.InvariantCulture)
                        + " and "
                        + ((int)OptionKind.GameChanger).ToString(CultureInfo.InvariantCulture)
                        + ". A kind nothing declares scopes the take's id to a menu that does not exist.");
                }

                if (takeId == 0)
                {
                    throw cursor.Fault(
                        what
                        + " has take id 0. Every option on an offering carries an identity counted from "
                        + "one, so zero is a take nothing on any menu can answer.");
                }

                previousWave = wave;
                commands[index] = RecordCommand.Of(
                    wave, (OptionKind)take, takeId, ReadSlots(cursor, what, slotCount));
            }

            return new CommandStream(
                header,
                Hash64.FromValue(rulesetHash),
                Hash64.FromValue(scheduleHash),
                seed,
                commands);
        }

        /// <summary>
        /// One command's slots. <c>(0, 0)</c> is the empty slot; a type id
        /// without a count and a count without a type id are both refused,
        /// because leaving a slot empty already has exactly one spelling.
        /// </summary>
        private static WaveSlot[] ReadSlots(ByteCursor cursor, string what, int count)
        {
            var slots = new WaveSlot[count];
            int previousTypeId = 0;

            for (int index = 0; index < count; index++)
            {
                string which =
                    "slot "
                    + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + what;

                int typeId = cursor.U16("the type id of " + which);
                int units = cursor.U16("the count of " + which);

                if (typeId == 0 && units == 0)
                {
                    slots[index] = WaveSlot.Empty;
                    continue;
                }

                if (typeId == 0)
                {
                    throw cursor.Fault(
                        which
                        + " sends "
                        + units.ToString(CultureInfo.InvariantCulture)
                        + " of type id 0, and zero means no unit. An empty slot is spelled (0, 0), so a "
                        + "count against no creep is a slot with a hole in it.");
                }

                if (units == 0)
                {
                    throw cursor.Fault(
                        which
                        + " sends none of type id "
                        + typeId.ToString(CultureInfo.InvariantCulture)
                        + ". An empty slot is spelled (0, 0), so naming a creep zero times would be a "
                        + "second spelling of one wave and two sets of bytes for one run.");
                }

                if (typeId <= previousTypeId)
                {
                    throw cursor.Fault(
                        which
                        + " sends type id "
                        + typeId.ToString(CultureInfo.InvariantCulture)
                        + ", at or below the "
                        + previousTypeId.ToString(CultureInfo.InvariantCulture)
                        + " a slot above it already sent. Filled slots are out of canonical order: they "
                        + "ascend strictly by type id, asserted rather than sorted, because sorting would "
                        + "leave two identical waves with two different sets of bytes.");
                }

                previousTypeId = typeId;
                slots[index] = WaveSlot.Of(typeId, units);
            }

            return slots;
        }

        /// <summary>
        /// Refuses a stream with more build phases than the run has rounds left.
        /// A run bounded by nothing but its health -- the round cap lifted --
        /// has no such bound, so there is nothing to compare against.
        /// </summary>
        private void RequireRoundsLeftFor(Run run)
        {
            if (run.Waves == Purse.RoundCapLifted)
            {
                return;
            }

            int left = run.Waves - run.Round;

            if (_commands.Length <= left)
            {
                return;
            }

            throw new SimulationException(
                "A command stream holds "
                + _commands.Length.ToString(CultureInfo.InvariantCulture)
                + " build phases and the run has "
                + left.ToString(CultureInfo.InvariantCulture)
                + " rounds left of its "
                + run.Waves.ToString(CultureInfo.InvariantCulture)
                + ". The whole stream is checked before the first round is played, so a stream longer than "
                + "the run is refused here rather than after the rounds it did fit have already resolved.");
        }
    }
}
