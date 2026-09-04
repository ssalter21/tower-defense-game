using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A stored round: the stage it was played at, the wall that stood and the
    /// wave that walked, in one run of bytes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A wall and a wave, because that is what a round is to somebody
    /// else.</b> <see cref="RoundOrders"/> is the pair a run resolves against,
    /// and this is that pair written down. A defense on its own would enter a
    /// field with nothing to send, and half of every pairing -- the direction a
    /// run spends health on -- would be an opponent standing still.
    /// </para>
    /// <para>
    /// <b>The stage is the field this kind adds.</b> A pool is drawn from per
    /// stage, so a stored round says which one it was played at and is drawn
    /// against runs standing at that one. Stages are counted from one, as a
    /// wave is: zero is refused rather than read as the first, because a round
    /// nobody played is not a round anybody should meet.
    /// </para>
    /// <para>
    /// <b>Both halves are inlined whole, headers and all.</b> A defense and a
    /// wave each have a reader, a canonical order and a format version of their
    /// own; carrying their bytes means the tower loop and the order loop exist
    /// once, and it means the cross-check that all three headers name one
    /// ruleset comes with them. It is the arrangement
    /// <see cref="ReplayBundle"/> uses for the same two halves.
    /// </para>
    /// <para>
    /// <b>The map is named by the defense and not again here</b>, and the seed
    /// is nowhere at all. A defense already carries the hash that pins the
    /// geometry and the handle that looks a map up; a seed belongs to the run
    /// that played the round, and putting one here would make rolling different
    /// dice a different stored round. See
    /// <c>docs/adr/0057-a-stored-round-is-a-wall-and-a-wave-at-a-stage.md</c>.
    /// </para>
    /// </remarks>
    public sealed class RoundRecord
    {
        /// <summary>The first stage a round can be recorded at. Stages count from one.</summary>
        public const int FirstStage = 1;

        /// <summary>The deepest stage the <c>u16</c> stage field can hold.</summary>
        public const int DeepestStage = 65535;

        private RoundRecord(RecordHeader header, int stage, GhostRecord ghost, WaveRecord wave)
        {
            Header = header;
            Stage = stage;
            Ghost = ghost;
            Wave = wave;
        }

        /// <summary>Magic, format version, simulation version, content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>Which round of a run this was, counted from one.</summary>
        public int Stage { get; }

        /// <summary>The wall that stood.</summary>
        public GhostRecord Ghost { get; }

        /// <summary>The wave that walked.</summary>
        public WaveRecord Wave { get; }

        /// <summary>The hash of the parsed grid the wall was built on.</summary>
        public Hash64 MapHash => Ghost.MapHash;

        /// <summary>
        /// Which map the wall claims to be on, or
        /// <see cref="GhostRecord.NoMapHandle"/> where it does not say.
        /// </summary>
        public int MapHandle => Ghost.MapHandle;

        /// <summary>Records a live round, at the current format version.</summary>
        /// <param name="map">The board the wall stands on, by hash.</param>
        /// <param name="defense">The wall, in canonical order.</param>
        /// <param name="wave">What was sent.</param>
        /// <param name="types">The roster both halves are read against.</param>
        /// <param name="stage">Which round of the run this was, counted from one.</param>
        /// <param name="mapHandle">Which map the wall claims to be on, for looking one up.</param>
        public static RoundRecord Of(
            HexMap map,
            TowerLayout defense,
            WaveScript wave,
            UnitTypeTable types,
            int stage,
            int mapHandle)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            RequireStage(stage);

            return new RoundRecord(
                RecordHeader.Current(RecordKind.Round, types.ContentHash),
                stage,
                GhostRecord.Of(map, defense, types, mapHandle),
                WaveRecord.Of(wave, types));
        }

        /// <summary>Reads a stored round from bytes, naming them in any error message.</summary>
        public static RoundRecord FromBytes(string record, byte[] bytes)
        {
            var cursor = new ByteCursor(record, bytes);
            RecordHeader header = RecordHeader.Read(cursor, RecordKind.Round);

            switch (header.FormatVersion)
            {
                case 0:
                    return ReadVersion0(cursor, header);

                default:
                    throw cursor.Fault(
                        "is stored round format version "
                        + header.FormatVersion.ToString(CultureInfo.InvariantCulture)
                        + ", which the read gate accepted and this reader has no branch for. The two "
                        + "lists have drifted apart, which is a fault in this build rather than in the "
                        + "record.");
            }
        }

        /// <summary>The bytes. Always the current format version -- there is one writer.</summary>
        public byte[] ToBytes()
        {
            byte[] ghost = Ghost.ToBytes();
            byte[] wave = Wave.ToBytes();

            var writer = new ByteWriter(RecordFormat.HeaderBytes + 2 + ghost.Length + wave.Length);

            Header.Write(writer);
            writer.U16("stage", Stage);
            writer.Raw(ghost);
            writer.Raw(wave);

            return writer.ToArray();
        }

        /// <summary>
        /// The round as a pool wants it: the pair a field is resolved against,
        /// resolved against a type table.
        /// </summary>
        /// <remarks>
        /// Both halves refuse a type id this table has never heard of, and
        /// neither drops the row -- a stored round read with a tower or an order
        /// missing is an opponent nobody played against.
        /// </remarks>
        public RoundOrders ToOrders(UnitTypeTable types) =>
            RoundOrders.Of(Ghost.ToLayout(types), Wave.ToScript(types));

        public override string ToString() =>
            Header.ToString()
            + ", stage "
            + Stage.ToString(CultureInfo.InvariantCulture)
            + ", "
            + Ghost.Count.ToString(CultureInfo.InvariantCulture)
            + " towers and "
            + Wave.Count.ToString(CultureInfo.InvariantCulture)
            + " orders on map "
            + MapHash.ToString();

        /// <summary>
        /// Version 0: <c>u16 stage</c>, then a whole defense record and a whole
        /// wave record.
        /// </summary>
        private static RoundRecord ReadVersion0(ByteCursor cursor, RecordHeader header)
        {
            int stage = cursor.U16("the stage");

            if (stage < FirstStage)
            {
                throw cursor.Fault(
                    "was played at stage "
                    + stage.ToString(CultureInfo.InvariantCulture)
                    + ". A run counts its rounds from one, so stage zero is a round nobody played rather "
                    + "than the first one.");
            }

            GhostRecord ghost = GhostRecord.ReadFrom(cursor);
            WaveRecord wave = WaveRecord.ReadFrom(cursor);

            cursor.ExpectEnd("stored round");

            CrossCheck(cursor, header, ghost.Header, "defense");
            CrossCheck(cursor, header, wave.Header, "wave");

            return new RoundRecord(header, stage, ghost, wave);
        }

        /// <summary>
        /// The record and both halves inside it name one ruleset, or the record
        /// is refused. Format versions are deliberately not compared: they are
        /// counted per record kind, so a stored round at version 0 holding a
        /// defense at version 1 is ordinary rather than suspicious.
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
                    + ". A stored round that contradicts its own contents is refused outright: this is "
                    + "not a record from an older ruleset, it is a record assembled from two of them.");
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
                    + ". A wave from one roster stapled to a wall from another is an opponent nobody "
                    + "played, and it is refused here rather than at the draw that would meet it.");
            }
        }

        /// <summary>A stage a run could have reached, refused if it is not one.</summary>
        private static void RequireStage(int stage)
        {
            if (stage >= FirstStage && stage <= DeepestStage)
            {
                return;
            }

            throw new SimulationException(
                "A round was recorded at stage "
                + stage.ToString(CultureInfo.InvariantCulture)
                + ". A run counts its rounds from one and the record stores the stage as a u16, so a "
                + "round outside "
                + FirstStage.ToString(CultureInfo.InvariantCulture)
                + " to "
                + DeepestStage.ToString(CultureInfo.InvariantCulture)
                + " is a stage no run reached rather than a deep one.");
        }
    }
}
