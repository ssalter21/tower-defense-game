using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One build phase as the record carries it: the wave it was decided in, and
    /// the decision -- what it did to the board, and how the wave's slots were
    /// filled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing else is stored, because nothing else has to be.</b> The round
    /// a decision was made in -- its purse, its board, the field it is fought
    /// against -- is a pure function of the run's seed and the phases before it,
    /// so it is replayed rather than carried; storing it would be a second copy
    /// of a derivation, free to disagree with the first.
    /// </para>
    /// <para>
    /// <b>The slots are in the order they were filled, and that order is the
    /// decision.</b> A slot's position is when its creeps walk out, so the same
    /// two slots the other way round are a different wave rather than a second
    /// spelling of one -- which is why nothing here sorts them and nothing
    /// asserts an ascending order over them. Until format 3 they had to ascend
    /// strictly by type id, because until then position meant nothing and the
    /// arrangement needed canonicalising.
    /// </para>
    /// <para>
    /// <b>What is asserted is that a creep fills at most one slot</b>, here as
    /// well as at load. A command that could be built and not read back would be
    /// a writer emitting bytes its own reader refuses, so the rule is checked
    /// where a command is made and checked again where one is read.
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
        private readonly WaveSlot[] _slots;

        private readonly BuildAction[] _actions;

        private RecordCommand(
            int wave,
            WaveSlot[] slots,
            BuildAction[] actions)
        {
            Wave = wave;
            _slots = slots;
            _actions = actions;
        }

        /// <summary>Which wave of the run this decision was made in. Counted from one.</summary>
        public int Wave { get; }

        /// <summary>The slots, in the order they were filled. Empty ones included.</summary>
        public IReadOnlyList<WaveSlot> Slots => _slots;

        /// <summary>What this phase did to the board, in the order it was written.</summary>
        /// <remarks>
        /// Stored from command stream format version 1, in the order they are
        /// held here. A version-0 stream has no field for them and reads back
        /// with none, so <see cref="ToPhase"/> hands on a phase that builds
        /// nothing -- which is a decision, and the one every version-0 stream
        /// made.
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
            var already = new List<int>();

            for (int index = 0; index < slots.Length; index++)
            {
                WaveSlot slot = decision.Slots[index];

                if (!slot.IsEmpty)
                {
                    if (already.Contains(slot.TypeId))
                    {
                        throw new SimulationException(
                            "A command for wave "
                            + wave.ToString(CultureInfo.InvariantCulture)
                            + " fills slot "
                            + (index + 1).ToString(CultureInfo.InvariantCulture)
                            + " with type id "
                            + slot.TypeId.ToString(CultureInfo.InvariantCulture)
                            + ", which a slot above it already sent. A creep fills at most one slot of a "
                            + "wave, and a command that could be written and not read back is a writer "
                            + "emitting bytes its own reader refuses.");
                    }

                    already.Add(slot.TypeId);
                }

                slots[index] = slot;
            }

            // The actions travel across in the order the phase holds them and
            // are asserted against nothing, because their order is the phase's
            // own meaning. A phase whose actions were dropped here would record
            // a run that built nothing and replay as a different run.
            var actions = new BuildAction[decision.Actions.Count];

            for (int index = 0; index < actions.Length; index++)
            {
                actions[index] = decision.Actions[index];
            }

            return new RecordCommand(wave, slots, actions);
        }

        /// <summary>
        /// The same, spelled out, for whoever is composing a decision rather
        /// than recording one that was made.
        /// </summary>
        public static RecordCommand Of(int wave, params WaveSlot[] slots) =>
            Of(wave, BuildPhase.Of(slots));

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

            return new RecordCommand(Wave, _slots, grown);
        }

        public static bool operator ==(RecordCommand? a, RecordCommand? b) =>
            a is null ? b is null : a.Equals(b);

        public static bool operator !=(RecordCommand? a, RecordCommand? b) => !(a == b);

        /// <summary>
        /// The decision as the build phase surface wants it, with nothing
        /// reshaped: the three things stored are the three things a phase is.
        /// </summary>
        public BuildPhase ToPhase()
        {
            BuildPhase phase = BuildPhase.Of(_slots);

            for (int index = 0; index < _actions.Length; index++)
            {
                phase = phase.With(_actions[index]);
            }

            return phase;
        }

        public bool Equals(RecordCommand? other)
        {
            if (other is null
                || Wave != other.Wave
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
            (Wave * 31 ^ _slots.Length) * 31 ^ _actions.Length;

        public override string ToString() =>
            "wave "
            + Wave.ToString(CultureInfo.InvariantCulture)
            + ": "
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
    /// run's field is drawn from its seed and the round, so a stream without a
    /// seed would be replayed against whichever opponents the caller happened to
    /// draw -- and a wave scored against a different field is a decision read
    /// out of a different game.
    /// </para>
    /// <para>
    /// <b>Three content hashes, three gates.</b> The unit table's sits in the
    /// shared header where every kind carries it. The ruleset's and the upgrade
    /// ladder's sit beside it because this is the first record kind whose
    /// meaning depends on them: the ruleset prices the wave, opens the purse and
    /// pays the interest, and the ladder decides what a placement standing on a
    /// hex may be swapped into. A stream replayed against either of them
    /// re-authored is a confidently wrong result, so each is compared on its own
    /// and each refuses by its own name.
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
            Hash64 ladderHash,
            ulong seed,
            RecordCommand[] commands)
        {
            Header = header;
            RulesetHash = rulesetHash;
            LadderHash = ladderHash;
            Seed = seed;
            _commands = commands;
        }

        /// <summary>Magic, format version, simulation version, unit table content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>The hash of the parsed ruleset these decisions were made under.</summary>
        public Hash64 RulesetHash { get; }

        /// <summary>The hash of the parsed upgrade ladder they were made against.</summary>
        public Hash64 LadderHash { get; }

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
                run.Ladder.ContentHash,
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
        /// <param name="commands">The build phases to record.</param>
        public static (byte[] Bytes, IReadOnlyList<RoundReport> Rounds) Recorded(
            Run run,
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

            return (bytes, FromBytes(bytes).Replay(run));
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

                case 1:
                    read = ReadVersion1(cursor, header);
                    break;

                case 2:
                    read = ReadVersion2(cursor, header);
                    break;

                case 3:
                    read = ReadVersion3(cursor, header);
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
                size += RecordFormat.CommandBytes
                    + (_commands[index].Actions.Count * RecordFormat.ActionBytes)
                    + (_commands[index].Slots.Count * RecordFormat.SlotBytes);
            }

            var writer = new ByteWriter(size);

            Header.Write(writer);
            writer.U64(RulesetHash.Value);
            writer.U64(LadderHash.Value);
            writer.U64(Seed);
            writer.U16("command count", _commands.Length);

            for (int index = 0; index < _commands.Length; index++)
            {
                RecordCommand command = _commands[index];

                writer.U16("command wave", command.Wave);
                writer.U16("command action count", command.Actions.Count);

                for (int action = 0; action < command.Actions.Count; action++)
                {
                    writer.U8("action kind", (int)command.Actions[action].Kind);
                    writer.U16("action type id", command.Actions[action].TypeId);
                    writer.I16("action column", command.Actions[action].Column);
                    writer.I16("action row", command.Actions[action].Row);
                }

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
        /// <b>Every failure here is a refusal and never a skip.</b> A place
        /// naming a unit some edge of the ladder targets, an action on a cell no
        /// tower could stand on, two slots of one wave on one creep and a wave
        /// nobody can afford are all
        /// <see cref="BuildPhase.Resolve(int, WaveScript, UpgradeLadder, Purse, CostTable, UnitTypeTable, HexMap, Board)"/>
        /// refusing -- the same surface a live build phase is checked by, so
        /// there is one implementation of the rules and not two.
        /// </para>
        /// <para>
        /// <b>The board is folded forward, so each phase is checked against
        /// what the phase before it built.</b> A stream whose fourth round
        /// places on a cell its second round took is refused here rather than
        /// mid-run, and a stream whose fourth round upgrades what its second
        /// round placed is admitted here rather than refused for a cell that
        /// would not have been empty by then.
        /// </para>
        /// <para>
        /// <b>The one check that surface cannot make is the wave index.</b>
        /// <c>Resolve</c> is handed the round about to be played and never the
        /// one the decision was stored for, so it cannot see the two disagree: a
        /// decision made at wave seven and stored at wave three would resolve
        /// perfectly. It is checked here, where both numbers are in hand.
        /// </para>
        /// <para>
        /// <b>Nothing is applied.</b> The two things a round moves -- the purse
        /// and the board -- are folded forward through local values exactly as a
        /// round moves them: what a build phase builds, then the wave's own
        /// purchases, then what the wave pays. The run is untouched, so a stream can be checked and then
        /// refused without the run having moved. The board the walk carries is
        /// a value like the other one -- <see cref="Board.Place"/> and
        /// <see cref="Board.Upgrade"/> return new boards -- so folding one
        /// forward here cannot reach the run's.
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
        /// <see cref="BuildPhase.Resolve(int, WaveScript, UpgradeLadder, Purse, CostTable, UnitTypeTable, HexMap, Board)"/>
        /// when the round is played. Bounded the other way -- at no bonus -- this
        /// would refuse waves the run affords perfectly well.
        /// </para>
        /// <para>
        /// <b>With the board folded, that ceiling is the last thing a decision
        /// can be refused for after a round has resolved.</b> Everything else a
        /// stored decision can be wrong about -- the ladder, the cell, the wave
        /// index -- is settled here, over values that do not depend on how a
        /// round played.
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
            Purse purse = run.Purse;
            Board board = run.Board;

            // Folded forward exactly as the purse and the board are, and for the
            // same reason: what a stored decision is charged depends on what the
            // decisions before it left standing.
            WaveScript carried = run.Carrying;
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
                        + ". The wave index says which round a decision was made in, and a decision "
                        + "played at another round is a round nobody decided.");
                }

                Build build = command.ToPhase().Resolve(
                    round, carried, run.Ladder, purse, run.Costs, run.Types, run.Map, board);

                purse = build.Purse.CloseWaveAtBest(run.Rules).Purse;
                board = build.Board;
                carried = build.Wave;
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
        /// upgrade ladder against the ones the run is playing. They are
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
        public IReadOnlyList<RoundReport> Replay(Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (run.Seed != Seed)
            {
                throw new SimulationException(
                    "A command stream stores the run seeded "
                    + Seed.ToString(CultureInfo.InvariantCulture)
                    + " and it was handed the run seeded "
                    + run.Seed.ToString(CultureInfo.InvariantCulture)
                    + ". A run's field is derived from its seed, so these are "
                    + "two different runs and the decisions of one were read off the other's menus.");
            }

            ReplayGate.Require(
                Stamp.Of("simulation version", Header.SimVersion, SimulationVersion.Current),
                Stamp.Of("content", Header.ContentHash, run.Types.ContentHash),
                Stamp.Of("ruleset", RulesetHash, run.Rules.ContentHash),
                Stamp.Of("ladder", LadderHash, run.Ladder.ContentHash));

            Check(run);

            var rounds = new List<RoundReport>();

            for (int index = 0; index < _commands.Length; index++)
            {
                rounds.Add(run.Advance(_commands[index].ToPhase()));
            }

            return rounds;
        }

        public bool Equals(CommandStream? other)
        {
            if (other is null
                || Header != other.Header
                || RulesetHash != other.RulesetHash
                || LadderHash != other.LadderHash
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
            + ", ladder "
            + LadderHash.ToString()
            + ", seed "
            + Seed.ToString(CultureInfo.InvariantCulture)
            + ", "
            + _commands.Length.ToString(CultureInfo.InvariantCulture)
            + " build phases";

        /// <summary>
        /// Version 0: <c>u64 ruleset_hash + u64 schedule_hash + u64 seed +
        /// u16 command_count + Command[]</c>, where a command is
        /// <c>u16 wave + u8 take_kind + u16 take_id + u16 slot_count</c>
        /// followed by that many <c>(u16 type_id, u16 count)</c> slots. No
        /// action run, so every build phase reads back having built nothing.
        /// The take is read past and dropped -- see <see cref="ReadVersion2"/>.
        /// </summary>
        /// <remarks>
        /// <b>This branch never goes away.</b> Version 1 sits beside it and this
        /// one keeps reading version-0 streams forever.
        /// <c>content/golden/command-0.commands</c> is the evidence: a real
        /// recorded stream, kept so that deleting this branch is a red gate
        /// rather than a quiet loss.
        /// </remarks>
        private static CommandStream ReadVersion0(ByteCursor cursor, RecordHeader header) =>
            ReadCommands(cursor, header, storesActions: false, storesTake: true);

        /// <summary>
        /// Version 1: the same, with <c>u16 action_count</c> and that many
        /// <c>(u8 kind, u16 type_id, i16 column, i16 row)</c> actions in each
        /// command, between the take and the slot count.
        /// </summary>
        private static CommandStream ReadVersion1(ByteCursor cursor, RecordHeader header) =>
            ReadCommands(cursor, header, storesActions: true, storesTake: true);

        /// <summary>
        /// Version 2: the same as version 1 with <c>u8 take_kind</c> and
        /// <c>u16 take_id</c> gone from every command, and the third stamp
        /// naming the upgrade ladder rather than the anchor schedule.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The take went because the gate did.</b> Nothing is unlocked, so a
        /// round has nothing to take and a field for it would be two bytes
        /// every writer had to invent a value for.
        /// </para>
        /// <para>
        /// <b>The third stamp changed meaning rather than moving.</b> It stood
        /// for <c>content/schedule.txt</c>, which no longer exists; it now
        /// stands for <c>content/upgrades.txt</c>, which the simulation began
        /// reading in the same change. Same offset, same width, different
        /// content -- which is exactly what a format version is for, and why a
        /// version-1 stream cannot be replayed against a version-2 run by
        /// accident.
        /// </para>
        /// </remarks>
        private static CommandStream ReadVersion2(ByteCursor cursor, RecordHeader header) =>
            ReadCommands(cursor, header, storesActions: true, storesTake: false);

        /// <summary>
        /// Version 3: the same bytes as version 2, and a different meaning for
        /// the order they are in. A slot's position is its release order, so
        /// slot one's creeps walk out first and the wave is a sequence rather
        /// than a set.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A version bump with no field in it, which is the point.</b>
        /// Nothing moved and nothing was added: the layout of a version-2
        /// command and a version-3 command is byte for byte the same. What
        /// changed is what the slot order means, and a reader cannot tell the
        /// two apart by looking -- an ascending version-2 wave and an ascending
        /// version-3 wave are the same bytes describing two different fights.
        /// That is exactly the case a format version exists for, and the
        /// alternative was a stream that replays into a confidently wrong
        /// result while validating.
        /// </para>
        /// <para>
        /// <b>The simulation version moved with it</b>, from 2 to 3, because
        /// the release schedule a wave resolves to is behaviour rather than
        /// layout -- the same bump the spawn cadence took when it went from 15
        /// ticks to 45. Streams at 0, 1 and 2 stay readable forever and stop
        /// being replayable here, which is the split
        /// <c>docs/adr/0009-three-identity-fields.md</c> draws.
        /// </para>
        /// </remarks>
        private static CommandStream ReadVersion3(ByteCursor cursor, RecordHeader header) =>
            ReadCommands(cursor, header, storesActions: true, storesTake: false);

        /// <summary>
        /// The body both versions share, told whether the bytes in front of it
        /// carry an action run.
        /// </summary>
        /// <remarks>
        /// Shared because it is the same bytes and not because it is
        /// convenient: a copy per branch would let the wave-order and take
        /// checks drift between them, and the version that lost one would go on
        /// loading streams the other refuses.
        /// </remarks>
        private static CommandStream ReadCommands(
            ByteCursor cursor,
            RecordHeader header,
            bool storesActions,
            bool storesTake)
        {
            ulong rulesetHash = cursor.U64("the ruleset hash");
            ulong ladderHash = cursor.U64("the ladder hash");
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

                // Read past and dropped rather than kept. A version-0 or
                // version-1 stream carries a take, and the gate it was taken
                // off no longer exists to check it against -- so the bytes are
                // consumed so the cursor stays aligned, and the decision they
                // described replays as the one thing it still is: the slots and
                // the actions beside them. The two checks that used to stand
                // here asked whether the take named a menu, and there is no
                // menu to name.
                if (storesTake)
                {
                    cursor.U8("the take kind of " + what);
                    cursor.U16("the take id of " + what);
                }

                previousWave = wave;

                int actionCount = storesActions ? cursor.U16("the action count of " + what) : 0;
                BuildAction[] actions = ReadActions(cursor, what, actionCount);
                int slotCount = cursor.U16("the slot count of " + what);

                RecordCommand command = RecordCommand.Of(
                    wave, ReadSlots(cursor, what, slotCount));

                for (int action = 0; action < actions.Length; action++)
                {
                    command = command.With(actions[action]);
                }

                commands[index] = command;
            }

            return new CommandStream(
                header,
                Hash64.FromValue(rulesetHash),
                Hash64.FromValue(ladderHash),
                seed,
                commands);
        }

        /// <summary>
        /// One command's defensive actions, in the order they were stored.
        /// </summary>
        /// <remarks>
        /// <para>
        /// No order is asserted over them, which is the opposite of the slots
        /// below: a phase may upgrade what it has just placed and the placement
        /// ordinals fall out of the sequence, so the same two actions the other
        /// way round are a different run rather than a second spelling of one.
        /// </para>
        /// <para>
        /// What an action may say is <see cref="BuildAction.Of"/>'s rule and
        /// where it sits in the record is this side's, so a refusal from there
        /// is rewrapped rather than reimplemented -- one implementation of each
        /// rule, and damaged bytes reported as a fault in the record rather
        /// than in this program.
        /// </para>
        /// <para>
        /// What the cell names is nobody's business here. A column and a row
        /// are read as <c>i16</c> and every <c>i16</c> is a cell some map might
        /// have, so whether one is on this map is a question for whatever
        /// applies the action.
        /// </para>
        /// </remarks>
        private static BuildAction[] ReadActions(ByteCursor cursor, string what, int count)
        {
            var actions = new BuildAction[count];

            for (int index = 0; index < count; index++)
            {
                string which =
                    "action "
                    + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + what;

                int kind = cursor.U8("the kind of " + which);
                int typeId = cursor.U16("the type id of " + which);
                int column = cursor.I16("the column of " + which);
                int row = cursor.I16("the row of " + which);

                try
                {
                    actions[index] = BuildAction.Of((ActionKind)kind, typeId, column, row);
                }
                catch (SimulationException refused)
                {
                    throw cursor.Fault(which + " cannot be read. " + refused.Message);
                }
            }

            return actions;
        }

        /// <summary>
        /// One command's slots, in the order they were filled -- which from
        /// format 3 is the order the creeps walk out in. <c>(0, 0)</c> is the
        /// empty slot; a type id without a count and a count without a type id
        /// are both refused, because leaving a slot empty already has exactly
        /// one spelling.
        /// </summary>
        /// <remarks>
        /// <b>No ascending order is asserted, and one was until format 3.</b>
        /// Position is the decision now, so sorting or refusing an arrangement
        /// would delete the lever rather than canonicalise it. Every stream at
        /// format 0, 1 or 2 was written under the old rule and so ascends
        /// anyway; nothing that could be read before is refused here.
        /// </remarks>
        private static WaveSlot[] ReadSlots(ByteCursor cursor, string what, int count)
        {
            var slots = new WaveSlot[count];
            var already = new List<int>();

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

                if (already.Contains(typeId))
                {
                    throw cursor.Fault(
                        which
                        + " sends type id "
                        + typeId.ToString(CultureInfo.InvariantCulture)
                        + ", which a slot above it already sent. A creep fills at most one slot of a wave: "
                        + "the same wave is spelled by putting the whole count in one slot, so two slots on "
                        + "one creep would be two sets of bytes for one run.");
                }

                already.Add(typeId);
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
