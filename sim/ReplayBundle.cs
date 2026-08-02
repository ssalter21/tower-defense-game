using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A whole match in one run of bytes: the seed, the map inlined, the defense
    /// and the wave. Everything needed to re-run it, with no registry and no
    /// assumption about where anything lives.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Self-contained on purpose.</b> A bundle that named its map by id would
    /// only replay on a machine that already had that map, under that id, with
    /// those exact contents -- three assumptions, each of which is somebody
    /// else's job to keep true. Inlining the parsed grid costs a hundred and
    /// thirty-five bytes here and makes handing somebody a replay a matter of
    /// handing them the bytes. It is the wrong trade at pool scale, which is why
    /// the defense and the wave keep their own ids and can be stored separately;
    /// it is the right trade for a replay.
    /// </para>
    /// <para>
    /// <b>The seed lives here, and this is the only record it could live in.</b>
    /// A record's id is the hash of its bytes, so a seed inside the defense would
    /// make rolling different dice a different defense -- orphaning every replay
    /// that pointed at the old one, and destroying the one property that makes
    /// the same defense runnable under ten seeds.
    /// </para>
    /// <para>
    /// <b>The header appears three times and the loader checks all three
    /// agree.</b> Thirty-six spare bytes buy the guarantee that a wave from one
    /// ruleset cannot be stapled to a defense from another: the two inner records
    /// carry their own simulation version and content hash, and a bundle whose
    /// three copies disagree is refused at read. That is a hard error rather than
    /// a replay refusal, because it is not a record from an older ruleset -- it
    /// is a record that contradicts itself.
    /// </para>
    /// <para>
    /// <b>Reading and replaying are different gates.</b> Reading needs a known
    /// format version and nothing else. Replaying needs, in addition, the
    /// simulation version, the content hash and the map hash all to match what is
    /// actually in front of it -- and when one does not, it refuses by name and
    /// leaves the record perfectly readable, so a defense whose ruleset has moved
    /// on can still be listed and drawn and shown as historical.
    /// </para>
    /// </remarks>
    public sealed class ReplayBundle
    {
        private readonly byte[] _ghostBytes;

        private readonly byte[] _waveBytes;

        private ReplayBundle(
            RecordHeader header,
            ulong seed,
            HexMap map,
            GhostRecord ghost,
            WaveRecord wave,
            byte[] ghostBytes,
            byte[] waveBytes)
        {
            Header = header;
            Seed = seed;
            Map = map;
            Ghost = ghost;
            Wave = wave;
            _ghostBytes = ghostBytes;
            _waveBytes = waveBytes;
        }

        /// <summary>Magic, format version, simulation version, content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>The seed the dice are started from.</summary>
        public ulong Seed { get; }

        /// <summary>
        /// The map, rebuilt from the inlined grid -- which means the corridor
        /// assertion has already run on it by the time anybody has this object.
        /// </summary>
        public HexMap Map { get; }

        /// <summary>The defense inside the bundle.</summary>
        public GhostRecord Ghost { get; }

        /// <summary>The wave inside the bundle.</summary>
        public WaveRecord Wave { get; }

        /// <summary>
        /// The defense's id: the hash of its own bytes, exactly as if it had been
        /// stored on its own. Derived here, never stored, so "this wave goes with
        /// this defense" is true by construction.
        /// </summary>
        public Hash64 GhostId => RecordId.Of(_ghostBytes);

        /// <summary>The wave's id, on the same terms.</summary>
        public Hash64 WaveId => RecordId.Of(_waveBytes);

        /// <summary>Records a live match, at the current format version.</summary>
        public static ReplayBundle Of(
            HexMap map,
            TowerLayout layout,
            WaveScript wave,
            UnitTypeTable types,
            ulong seed)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            GhostRecord ghost = GhostRecord.Of(map, layout, types);
            WaveRecord waveRecord = WaveRecord.Of(wave, types);

            return new ReplayBundle(
                RecordHeader.Current(RecordKind.Replay, types.ContentHash),
                seed,
                map,
                ghost,
                waveRecord,
                ghost.ToBytes(),
                waveRecord.ToBytes());
        }

        /// <summary>Reads a bundle from bytes. The read gate, and nothing else.</summary>
        public static ReplayBundle FromBytes(byte[] bytes) => FromBytes("replay bundle", bytes);

        /// <summary>Reads a bundle from bytes, naming them in any error message.</summary>
        public static ReplayBundle FromBytes(string record, byte[] bytes)
        {
            var cursor = new ByteCursor(record, bytes);
            RecordHeader header = RecordHeader.Read(cursor, RecordKind.Replay);

            switch (header.FormatVersion)
            {
                case 0:
                    return ReadVersion0(cursor, header);

                default:
                    throw cursor.Fault(
                        "is replay format version "
                        + header.FormatVersion.ToString(CultureInfo.InvariantCulture)
                        + ", which the read gate accepted and this reader has no branch for.");
            }
        }

        /// <summary>The bytes. Always the current format version -- there is one writer.</summary>
        public byte[] ToBytes()
        {
            byte[] cells = Map.ToCellBytes();

            var writer = new ByteWriter(
                RecordFormat.HeaderBytes + 12 + cells.Length + _ghostBytes.Length + _waveBytes.Length);

            Header.Write(writer);
            writer.U64(Seed);
            writer.U16("map width", Map.Width);
            writer.U16("map height", Map.Height);
            writer.Raw(cells);
            writer.Raw(_ghostBytes);
            writer.Raw(_waveBytes);

            return writer.ToArray();
        }

        /// <summary>
        /// The replay gate, and the match on the other side of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Three checks, each refusing by name and with both values: the
        /// simulation version against this build, the content hash against the
        /// tables handed in, and the map hash the defense recorded against the
        /// map actually inlined here. They are independent, so a record can fail
        /// exactly one of them and the message says which.
        /// </para>
        /// <para>
        /// <b>Nothing here quietly substitutes anything.</b> There is no
        /// migration, no defaulting of a field the record did not carry and no
        /// "replay it under today's numbers anyway". That last one is a real
        /// question people ask, and it has its own answer with its own name --
        /// <see cref="RestageUnderCurrentRules"/> -- which returns a result
        /// labelled as what it is. Doing it silently inside here is how a
        /// competitive record gets quietly corrupted.
        /// </para>
        /// </remarks>
        public Match Replay(UnitTypeTable types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (Header.SimVersion != SimulationVersion.Current)
            {
                throw new RetiredRecordException(
                    "simulation version",
                    "simulation version " + Header.SimVersion.ToString(CultureInfo.InvariantCulture),
                    "simulation version " + SimulationVersion.Current.ToString(CultureInfo.InvariantCulture));
            }

            if (Header.ContentHash != types.ContentHash)
            {
                throw new RetiredRecordException(
                    "content hash",
                    "content " + Header.ContentHash.ToString(),
                    "content " + types.ContentHash.ToString());
            }

            if (Ghost.MapHash != Map.MapHash)
            {
                throw new RetiredRecordException(
                    "map hash",
                    "map " + Ghost.MapHash.ToString(),
                    "map " + Map.MapHash.ToString());
            }

            return ToMatch(types);
        }

        /// <summary>
        /// Runs this record's defense and wave under the rules and numbers of
        /// today, whatever it was recorded against.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is not a replay and its result is not this record's result.</b>
        /// It exists because "how would that defense hold up under the current
        /// numbers?" is a real question, and because the only dangerous way to
        /// answer it is to let <see cref="Replay"/> quietly do it. So it is a
        /// separate operation, with a separate name, returning a
        /// <see cref="Restaging"/> that carries both rulesets and says in its own
        /// <c>ToString</c> that it is not a replay.
        /// </para>
        /// <para>
        /// The map hash is still enforced, and that is not an inconsistency. The
        /// other two gates ask "were these the same rules?", which is the
        /// question this operation is deliberately setting aside; the map hash
        /// asks "are these bytes internally consistent?", which nothing sets
        /// aside.
        /// </para>
        /// </remarks>
        public Restaging RestageUnderCurrentRules(UnitTypeTable types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (Ghost.MapHash != Map.MapHash)
            {
                throw new RetiredRecordException(
                    "map hash",
                    "map " + Ghost.MapHash.ToString(),
                    "map " + Map.MapHash.ToString());
            }

            return new Restaging(
                ToMatch(types),
                Header.SimVersion,
                Header.ContentHash,
                types.ContentHash);
        }

        public override string ToString() =>
            Header.ToString()
            + ", seed "
            + Seed.ToString(CultureInfo.InvariantCulture)
            + ", defense "
            + GhostId.ToString()
            + " against wave "
            + WaveId.ToString();

        private static ReplayBundle ReadVersion0(ByteCursor cursor, RecordHeader header)
        {
            ulong seed = cursor.U64("the seed");
            int width = cursor.U16("the map width");
            int height = cursor.U16("the map height");
            long cellCount = (long)width * height;

            if (cellCount > cursor.Remaining)
            {
                throw cursor.Fault(
                    "says its map is "
                    + width.ToString(CultureInfo.InvariantCulture)
                    + " by "
                    + height.ToString(CultureInfo.InvariantCulture)
                    + ", which is "
                    + cellCount.ToString(CultureInfo.InvariantCulture)
                    + " cells, and only "
                    + cursor.Remaining.ToString(CultureInfo.InvariantCulture)
                    + " bytes are left.");
            }

            byte[] cells = cursor.Raw("the map cells", (int)cellCount);

            // The same corridor assertion the text parser runs, on the same
            // code, so a replay cannot carry geometry that a map file could not.
            HexMap map = HexMap.FromCells(cursor.Record + " map", width, height, cells);

            int ghostStart = cursor.Position;
            GhostRecord ghost = GhostRecord.ReadFrom(cursor);
            int ghostLength = cursor.Position - ghostStart;

            int waveStart = cursor.Position;
            WaveRecord wave = WaveRecord.ReadFrom(cursor);
            int waveLength = cursor.Position - waveStart;

            cursor.ExpectEnd("bundle");

            CrossCheck(cursor, header, ghost.Header, "defense");
            CrossCheck(cursor, header, wave.Header, "wave");

            return new ReplayBundle(
                header,
                seed,
                map,
                ghost,
                wave,
                cursor.Slice(ghostStart, ghostLength),
                cursor.Slice(waveStart, waveLength));
        }

        /// <summary>
        /// The bundle and both inner records name one ruleset, or the bundle is
        /// refused. Format versions are deliberately not compared: they are
        /// counted per record kind, so a bundle at version 0 holding a defense at
        /// version 1 is ordinary rather than suspicious.
        /// </summary>
        private static void CrossCheck(ByteCursor cursor, RecordHeader outer, RecordHeader inner, string what)
        {
            if (outer.SimVersion != inner.SimVersion)
            {
                throw cursor.Fault(
                    "is stamped simulation version "
                    + outer.SimVersion.ToString(CultureInfo.InvariantCulture)
                    + " and the "
                    + what
                    + " inside it is stamped "
                    + inner.SimVersion.ToString(CultureInfo.InvariantCulture)
                    + ". A bundle that contradicts its own contents is refused outright: this is not a "
                    + "record from an older ruleset, it is a record assembled from two of them.");
            }

            if (outer.ContentHash != inner.ContentHash)
            {
                throw cursor.Fault(
                    "is stamped content "
                    + outer.ContentHash.ToString()
                    + " and the "
                    + what
                    + " inside it is stamped "
                    + inner.ContentHash.ToString()
                    + ". A wave from one ruleset stapled to a defense from another is unrunnable, and "
                    + "it is refused here rather than at replay so that nothing can ever hold one.");
            }
        }

        private Match ToMatch(UnitTypeTable types) =>
            new Match(Map, Ghost.ToLayout(types), Wave.ToScript(types), Seed);
    }

    /// <summary>
    /// The result of running a stored record's defense and wave under today's
    /// rules rather than the ones it was recorded under.
    /// </summary>
    /// <remarks>
    /// <b>Differently named, differently labelled, and never returned by
    /// <see cref="ReplayBundle.Replay"/>.</b> It carries both rulesets so that
    /// whatever displays it cannot present the outcome as the record's own. A
    /// silent substitution inside replay is the one way a competitive record gets
    /// corrupted without anybody doing anything wrong.
    /// </remarks>
    public sealed class Restaging
    {
        internal Restaging(Match match, uint recordedSimVersion, Hash64 recordedContent, Hash64 contentUsed)
        {
            Match = match;
            RecordedSimVersion = recordedSimVersion;
            RecordedContentHash = recordedContent;
            ContentHashUsed = contentUsed;
        }

        /// <summary>The match, ready to be advanced. It is a new match, not a replay of one.</summary>
        public Match Match { get; }

        /// <summary>The simulation version the record was made under.</summary>
        public uint RecordedSimVersion { get; }

        /// <summary>The simulation version it is actually being run under: this build's.</summary>
        public uint SimVersionUsed => SimulationVersion.Current;

        /// <summary>The content hash the record was made under.</summary>
        public Hash64 RecordedContentHash { get; }

        /// <summary>The content hash of the tables it is actually being run against.</summary>
        public Hash64 ContentHashUsed { get; }

        /// <summary>
        /// Whether today's rules and numbers happen to be the record's own. Even
        /// when they are, this is still not a replay -- it is a restaging that
        /// coincided with one, and calling it a replay would mean the label
        /// depended on the numbers rather than on what was asked for.
        /// </summary>
        public bool RulesetsCoincide =>
            RecordedSimVersion == SimVersionUsed && RecordedContentHash == ContentHashUsed;

        public override string ToString() =>
            "Restaged, not replayed: recorded under simulation version "
            + RecordedSimVersion.ToString(CultureInfo.InvariantCulture)
            + " and content "
            + RecordedContentHash.ToString()
            + ", run under simulation version "
            + SimVersionUsed.ToString(CultureInfo.InvariantCulture)
            + " and content "
            + ContentHashUsed.ToString()
            + ". The outcome is what this defense and this wave do today, and it is not this record's "
            + "result.";
    }
}
