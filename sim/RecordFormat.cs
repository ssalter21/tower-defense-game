using System;
using System.Globalization;

namespace Sim
{
    /// <summary>Which of the three record kinds a run of bytes is.</summary>
    public enum RecordKind
    {
        /// <summary>A defense: the towers, and the map they were placed on, by hash.</summary>
        Ghost = 0,

        /// <summary>A wave: what gets sent, when, and how many.</summary>
        Wave = 1,

        /// <summary>A replay: a seed, an inlined map, a defense and a wave, self-contained.</summary>
        Replay = 2,
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
    /// never changed anything about a defense. Three counters, three histories,
    /// and each one only moves when its own bytes move.
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
        /// + 8 content hash. Identical in all three kinds.
        /// </summary>
        public const int HeaderBytes = 18;

        /// <summary>Bytes per tower in a defense: <c>u16 type_id · i16 q · i16 r</c>.</summary>
        public const int TowerBytes = 6;

        /// <summary>
        /// Bytes per order in a wave: <c>u32 tick_offset · u16 type_id ·
        /// u16 count · u8 corridor</c>.
        /// </summary>
        public const int OrderBytes = 9;

        /// <summary>
        /// The defense layout, version 0.
        /// </summary>
        /// <remarks>
        /// <b>Version 0 deliberately has no map handle in it.</b> The record
        /// pins its geometry by <c>u64 map_hash</c> alone, which answers the only
        /// question a replay actually asks -- what geometry did this run on, and
        /// can I prove it is unchanged -- under every theory of where maps come
        /// from. A <c>u16 map_id</c> is genuinely wanted as a handle for looking
        /// a map up, and it is being held back on purpose so that adding it is a
        /// real format bump to version 1 rather than a rehearsal with an invented
        /// field. <b>Do not "fix" this by adding the field here.</b>
        /// </remarks>
        public const int GhostVersion = 0;

        /// <summary>The wave layout, version 0.</summary>
        public const int WaveVersion = 0;

        /// <summary>The replay bundle layout, version 0.</summary>
        public const int ReplayVersion = 0;

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

                default:
                    throw NoSuchKind(kind);
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
                    return formatVersion == 0;

                case RecordKind.Wave:
                    return formatVersion == 0;

                case RecordKind.Replay:
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

            kind = RecordKind.Ghost;
            return false;
        }

        private static ArgumentOutOfRangeException NoSuchKind(RecordKind kind) =>
            new ArgumentOutOfRangeException(
                nameof(kind),
                "There are three record kinds and "
                + ((int)kind).ToString(CultureInfo.InvariantCulture)
                + " is not one of them.");
    }
}
