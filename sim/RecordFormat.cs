using System;
using System.Globalization;

namespace Sim
{
    /// <summary>Which of the five record kinds a run of bytes is.</summary>
    public enum RecordKind
    {
        /// <summary>A defense: the towers, and the map they were placed on, by hash.</summary>
        Ghost = 0,

        /// <summary>A wave: what gets sent, when, and how many.</summary>
        Wave = 1,

        /// <summary>A replay: a seed, an inlined map, a defense and a wave, self-contained.</summary>
        Replay = 2,

        /// <summary>
        /// A command stream: a run's seed and its build phases as
        /// <c>(wave index, decision)</c> pairs.
        /// </summary>
        Command = 3,

        /// <summary>
        /// A stored round: the stage it was played at, the wall that stood and
        /// the wave that walked.
        /// </summary>
        Round = 4,
    }

    /// <summary>
    /// The layout side of the record format: the magic tags, the shared header,
    /// and which format versions each record kind has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Format versions are counted per record kind.</b> A single global
    /// counter was the obvious arrangement and it is wrong: editing the wave
    /// layout would bump every stored defense's version too, so every defense
    /// would look newer than it is and readers would branch on versions that
    /// never changed anything about a defense. One counter per kind, one
    /// history each, and each one moves only when its own bytes move.
    /// </para>
    /// <para>
    /// <b>Magic before version, version before everything else.</b> Four bytes
    /// of magic buy an unambiguous "you handed me a wave where a defense was
    /// expected" and a hexdump a person can read. The format version comes next
    /// because it is the field that says how to parse the rest of the header --
    /// including where the simulation version is. A reader that read the
    /// simulation version first would have parsed something before it knew which
    /// layout it was looking at, which is unfixable once records exist.
    /// </para>
    /// <para>
    /// <b>One writer, many readers.</b> The writer emits
    /// <see cref="CurrentVersionOf"/> and nothing else, ever. History lives in
    /// the reader, where one branch per version is a list that grows by one; if
    /// the writer had history too, the pairs would multiply. So
    /// <c>write(read(old_bytes))</c> deliberately does not reproduce the old
    /// bytes, and the byte-identity round trip is asserted on the current format
    /// alone.
    /// </para>
    /// </remarks>
    public static class RecordFormat
    {
        /// <summary>
        /// The shared header: 4 magic + 2 format version + 4 simulation version
        /// + 8 content hash. Identical in all four kinds.
        /// </summary>
        public const int HeaderBytes = 18;

        /// <summary>Bytes per tower in a defense: <c>u16 type_id + i16 q + i16 r</c>.</summary>
        public const int TowerBytes = 6;

        /// <summary>
        /// Bytes per order in a wave: <c>u32 tick_offset + u16 type_id +
        /// u16 count + u8 corridor</c>.
        /// </summary>
        public const int OrderBytes = 9;

        /// <summary>
        /// The fixed part of one build phase in a command stream:
        /// <c>u16 wave + u16 action_count + u16 slot_count</c> from format
        /// version 2, which is what this constant sizes. The actions follow the action count,
        /// <see cref="ActionBytes"/> each, and the slots follow the slot count,
        /// <see cref="SlotBytes"/> each.
        /// </summary>
        /// <remarks>
        /// Both counts are <c>u16</c>, and the action one is not narrower than
        /// the slot one because a phase performs as many actions as its gold
        /// pays for -- the list is bounded by a purse rather than by a round's
        /// width.
        /// </remarks>
        public const int CommandBytes = 6;

        /// <summary>
        /// Bytes per defensive action in a command stream: <c>u8 kind +
        /// u16 type_id + i16 column + i16 row</c>.
        /// </summary>
        /// <remarks>
        /// A tower entry plus a kind byte, and the cell is the column and row
        /// <c>content/map.txt</c> is written in rather than the axial pair
        /// <see cref="TowerBytes"/> carries. That is what
        /// <see cref="BuildAction"/> holds, and converting on the way past
        /// would need the map -- which is exactly what a reader does not have.
        /// </remarks>
        public const int ActionBytes = 7;

        /// <summary>
        /// Bytes per wave slot in a command stream: <c>u16 type_id +
        /// u16 count</c>, with <c>(0, 0)</c> meaning empty.
        /// </summary>
        public const int SlotBytes = 4;

        /// <summary>
        /// The defense layout, version 1: the version-0 fields with a
        /// <c>u16 map_id</c> added after the map hash.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Version 0 had no map handle, and version 1 is the bump that added
        /// one.</b> Version 0 pinned its geometry by <c>u64 map_hash</c> alone,
        /// which answers the only question a replay actually asks -- what
        /// geometry did this run on, and can I prove it is unchanged -- under
        /// every theory of where maps come from. The handle was held back so
        /// that adding it would be a real format bump rather than a rehearsal
        /// with an invented field, and this is that bump.
        /// </para>
        /// <para>
        /// <b>The version-0 branch defaults the field, and that is only legal
        /// because the field is not simulation-affecting.</b> A version-0
        /// defense reads back with
        /// <see cref="GhostRecord.NoMapHandle"/> and replays to exactly the
        /// result it always did: nothing in the tick loop can see a map handle,
        /// because geometry reaches the simulation as the inlined grid and is
        /// checked by hash. A defaulted handle is therefore a record that does
        /// not say which map it was, which is the truth about it.
        /// </para>
        /// <para>
        /// <b>A simulation-affecting field may not be defaulted, ever</b>, and
        /// the distinction is the whole reason this particular field was chosen
        /// to rehearse on. Had version 1 added, say, a per-tower facing that the
        /// targeting rule consulted, there would be no value the version-0
        /// branch could supply: every choice invents an input the recorded run
        /// never had, so the replay would be a confidently wrong answer that
        /// still validates. The only honest reader for that field is one that
        /// refuses -- either by leaving the old records readable and unrunnable,
        /// or by retiring them at the replay gate the way a moved simulation
        /// version does. Defaulting is a decision about a field, not a policy
        /// about old records, and the test for it is whether a replay's result
        /// can depend on the value.
        /// </para>
        /// </remarks>
        public const int GhostVersion = 1;

        /// <summary>The wave layout, version 0.</summary>
        public const int WaveVersion = 0;

        /// <summary>
        /// The stored-round layout, version 0: <c>u16 stage</c>, a whole
        /// defense record and a whole wave record, in that order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It inlines the two records rather than restating their fields.</b>
        /// A stored round is a wall and a wave, and both of those already have
        /// a reader, a canonical order and a format version of their own -- so
        /// this kind carries their bytes and neither the tower loop nor the
        /// order loop exists twice. It is the arrangement
        /// <see cref="ReplayVersion"/> uses for the same two halves, and the
        /// cross-check that the three headers name one ruleset comes with it.
        /// </para>
        /// <para>
        /// <b>The stage is the field this kind adds, and it is the whole reason
        /// the kind exists.</b> A pool is drawn from per stage -- a run at its
        /// seventh round meets rounds recorded at a seventh -- so a stored
        /// round that did not say which one it was played at could be drawn
        /// against any of them.
        /// </para>
        /// <para>
        /// <b>The map is named by the defense inside, and not again here.</b>
        /// A defense already carries the map hash that pins its geometry and
        /// the handle that looks one up, so a second copy would be a second
        /// thing to keep in agreement -- the argument
        /// <see cref="ReplayBundle"/> makes about the handle, one level up.
        /// </para>
        /// </remarks>
        public const int RoundVersion = 0;

        /// <summary>
        /// The replay bundle layout, version 2: the version-1 fields with a
        /// second plane of map bytes -- one level per cell, row-major -- after
        /// the cells.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Version 2 is the bump the level layer cost.</b> A bundle inlines
        /// its map, and a map is now two planes rather than one, so the bytes
        /// after <c>u16 height</c> are twice as many. Nothing about that is
        /// optional: the level of a hex is what a tower's reach across a fold
        /// is measured against, so a bundle that carried only the terrain would
        /// be a record of a board nobody played on.
        /// </para>
        /// <para>
        /// <b>A version-0 or version-1 bundle reads back on the flat, and that
        /// is what those bytes say rather than a value invented for them.</b>
        /// Every map that existed before this version was flat -- there was no
        /// second tier to stand on and no notation for one -- so the level
        /// plane a legacy branch supplies is the height that record was
        /// actually played at. It is the case <see cref="GhostVersion"/> draws
        /// the line around from the other side: the reader is not choosing a
        /// value for a field the record left open, it is stating the only
        /// height the world had.
        /// </para>
        /// <para>
        /// <b>Their map hashes are compared under the layout they were stamped
        /// at.</b> <c>hex-map/1</c> folded the terrain alone and
        /// <c>hex-map/2</c> folds the terrain and the levels, so the two are
        /// answers to different questions rather than two answers to one. See
        /// <see cref="HexMap.MapHashUnder"/>.
        /// </para>
        /// <para>
        /// <b>Version 0 carries no ruleset stamp, and version 1 is the bump
        /// that adds one.</b> Every landing reads the matrix cell, the armour
        /// denominator, the armour percentage and the damage floor off the
        /// ruleset a bundle is replayed against, so the stamp is what stops one
        /// retuned number producing a different match under a record's name
        /// while the simulation version, the content hash and the map hash all
        /// agree.
        /// </para>
        /// <para>
        /// <b>The version-0 branch supplies nothing, and a version-0 bundle is
        /// retired at the ruleset gate.</b> This is the case
        /// <see cref="GhostVersion"/> names as the one a reader may not default:
        /// a replay's result depends on every number in the ruleset, so any
        /// value the branch invented would be an input the recorded run never
        /// had. Those bundles stay readable, listable and restageable forever
        /// and they no longer replay. See
        /// <c>docs/adr/0047-a-bundle-stamps-its-ruleset.md</c>.
        /// </para>
        /// </remarks>
        public const int ReplayVersion = 2;

        /// <summary>
        /// The command stream layout, version 3: a build phase is
        /// <c>u16 wave + u16 action_count + Action[] + u16 slot_count +
        /// Slot[]</c>, and a slot's position in that run is the order its creeps
        /// walk out in. Counted on its own, so a stored defense, wave or bundle
        /// carries no version this kind moved.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Version 0 stored no defensive actions, and version 1 is the bump
        /// that stores them.</b> A build phase decides two things -- what the
        /// round takes off its menu and what it builds -- and version 0 wrote
        /// down only the first, so a phase that placed a tower and one that
        /// placed nothing were the same bytes.
        /// </para>
        /// <para>
        /// <b>The actions sit after the take and before the slots</b>, which is
        /// the order a round plays them in: the take and the defensive build
        /// both happen before the wave is sent, and the slots are the wave.
        /// Either position parses -- each run carries its own count -- so what
        /// the order buys is a hexdump that explains itself.
        /// </para>
        /// <para>
        /// <b>Version 2 is the bump that deleted the take</b>, when the gates it
        /// was taken off went, and turned the third header stamp from an anchor
        /// schedule hash into an upgrade ladder hash -- same offset, same width,
        /// different content.
        /// </para>
        /// <para>
        /// <b>Version 3 is the bump with no field in it.</b> A version-3 command
        /// is byte for byte a version-2 command; what changed is that a slot's
        /// position became its release order, so the same bytes describe a
        /// different fight. A reader cannot tell the two apart by looking, which
        /// is the whole reason the version had to move: the alternative is a
        /// version-2 stream replaying into a confidently wrong result while
        /// passing every gate. See <see cref="CommandStream"/>'s version-3
        /// branch.
        /// </para>
        /// <para>
        /// <b>A version-0 stream reads back with no actions, and that is what
        /// those bytes say rather than a value invented for them.</b> The
        /// distinction <see cref="GhostVersion"/> draws is between a field a
        /// reader can supply honestly and one it cannot, and an absent run of
        /// actions is not a defaulted field at all: the record carries none,
        /// so a phase read from it built nothing. What that costs is that the
        /// board such a stream is replayed onto is the board the run is handed,
        /// which is why <c>content/golden/command-0.commands</c> is read and
        /// never held against an outcome.
        /// </para>
        /// </remarks>
        public const int CommandVersion = 3;

        /// <summary>The four bytes a record of this kind begins with.</summary>
        public static string MagicOf(RecordKind kind)
        {
            switch (kind)
            {
                case RecordKind.Ghost:
                    return "GHST";

                case RecordKind.Wave:
                    return "WAVE";

                case RecordKind.Replay:
                    return "RPLY";

                case RecordKind.Command:
                    return "CMDS";

                case RecordKind.Round:
                    return "RUND";

                default:
                    throw NoSuchKind(kind);
            }
        }

        /// <summary>What a record of this kind is called, in a message.</summary>
        public static string NameOf(RecordKind kind)
        {
            switch (kind)
            {
                case RecordKind.Ghost:
                    return "defense record";

                case RecordKind.Wave:
                    return "wave record";

                case RecordKind.Replay:
                    return "replay bundle";

                case RecordKind.Command:
                    return "command stream";

                case RecordKind.Round:
                    return "stored round";

                default:
                    throw NoSuchKind(kind);
            }
        }

        /// <summary>
        /// The row a stored type id names, required to play that half of the
        /// loop.
        /// </summary>
        /// <remarks>
        /// The rule is <see cref="UnitTypeTable.Require"/>'s and the record's
        /// name is this side's, so the refusal is rewrapped rather than
        /// reimplemented. Reading bytes stays an all-or-nothing gate: a record
        /// naming a type this table has never heard of is refused whole, never
        /// read with the row dropped.
        /// </remarks>
        internal static UnitType RequireType(
            RecordKind kind,
            UnitTypeTable types,
            int id,
            UnitRole role,
            string what)
        {
            try
            {
                return types.Require(id, role, what);
            }
            catch (SimulationException refused)
            {
                throw new RecordException(NameOf(kind), refused.Message);
            }
        }

        /// <summary>
        /// The only version the writer emits for this kind. See the remarks on
        /// <see cref="RecordFormat"/> for why there is only one.
        /// </summary>
        public static int CurrentVersionOf(RecordKind kind)
        {
            switch (kind)
            {
                case RecordKind.Ghost:
                    return GhostVersion;

                case RecordKind.Wave:
                    return WaveVersion;

                case RecordKind.Replay:
                    return ReplayVersion;

                case RecordKind.Command:
                    return CommandVersion;

                case RecordKind.Round:
                    return RoundVersion;

                default:
                    throw NoSuchKind(kind);
            }
        }

        /// <summary>
        /// Whether this reader has a branch for that version of that kind.
        /// </summary>
        /// <remarks>
        /// Spelled out one version at a time rather than as
        /// <c>version &lt;= current</c>, because these are the branches that
        /// exist rather than the branches that ought to. A version that was
        /// skipped, or a branch somebody deleted, has to show up here as an
        /// unknown version and a loud refusal -- not as a number that passes an
        /// inequality and then falls through a switch.
        /// </remarks>
        public static bool IsKnown(RecordKind kind, int formatVersion)
        {
            switch (kind)
            {
                case RecordKind.Ghost:
                    // Version 0 is not a legacy path being tolerated. It is a
                    // version of this format, it has a golden record committed
                    // against it forever, and its branch is expected to be here
                    // for as long as any version-0 bytes exist anywhere.
                    return formatVersion == 0 || formatVersion == 1;

                case RecordKind.Wave:
                    return formatVersion == 0;

                case RecordKind.Replay:
                    // Version 0 is here on the same terms the defense's is: a
                    // golden bundle is committed against it forever, and the
                    // branch stays for as long as any version-0 bytes exist.
                    // That it can no longer pass the replay gate is a decision
                    // about the ruleset field and not about reading the bytes.
                    //
                    // Version 1 is here on the same terms, and version 2 is the
                    // one the writer emits: the level plane. Both older
                    // branches read a map on the flat, which is the height
                    // every board had before the plane existed.
                    return formatVersion == 0 || formatVersion == 1 || formatVersion == 2;

                case RecordKind.Command:
                    // Version 0 is here on the terms the defense's and the
                    // bundle's are: content/golden/command-0.commands is
                    // committed against it forever, and the branch stays for as
                    // long as any version-0 bytes exist. It reads a stream
                    // whose build phases carry no actions, which is what those
                    // bytes say.
                    //
                    // Version 1 is here on the same terms, against
                    // content/golden/command-1.commands. Both of them carry a
                    // take off a menu that no longer exists; the bytes are read
                    // past so the cursor stays aligned and the decision replays
                    // as its slots and its actions. See
                    // CommandStream.ReadVersion2.
                    //
                    // Version 2 is here against content/golden/command-2.commands,
                    // and it is the one branch whose bytes a later version also
                    // accepts: a version-3 command has the same layout and a
                    // different meaning for its slot order. Reading it is not
                    // the same as replaying it, which is the point of keeping
                    // the branch rather than folding it into version 3.
                    return formatVersion == 0
                        || formatVersion == 1
                        || formatVersion == 2
                        || formatVersion == 3;

                case RecordKind.Round:
                    return formatVersion == 0;

                default:
                    throw NoSuchKind(kind);
            }
        }

        /// <summary>The kind whose magic these four characters are, if any.</summary>
        internal static bool TryKindOfMagic(string magic, out RecordKind kind)
        {
            if (string.Equals(magic, MagicOf(RecordKind.Ghost), StringComparison.Ordinal))
            {
                kind = RecordKind.Ghost;
                return true;
            }

            if (string.Equals(magic, MagicOf(RecordKind.Wave), StringComparison.Ordinal))
            {
                kind = RecordKind.Wave;
                return true;
            }

            if (string.Equals(magic, MagicOf(RecordKind.Replay), StringComparison.Ordinal))
            {
                kind = RecordKind.Replay;
                return true;
            }

            if (string.Equals(magic, MagicOf(RecordKind.Command), StringComparison.Ordinal))
            {
                kind = RecordKind.Command;
                return true;
            }

            if (string.Equals(magic, MagicOf(RecordKind.Round), StringComparison.Ordinal))
            {
                kind = RecordKind.Round;
                return true;
            }

            kind = RecordKind.Ghost;
            return false;
        }

        private static ArgumentOutOfRangeException NoSuchKind(RecordKind kind) =>
            new ArgumentOutOfRangeException(
                nameof(kind),
                "There are five record kinds and "
                + ((int)kind).ToString(CultureInfo.InvariantCulture)
                + " is not one of them.");
    }
}
