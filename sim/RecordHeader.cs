using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// The eighteen bytes every record of every kind begins with: magic, format
    /// version, simulation version, content hash.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The reader knows how to parse the header before it parses the
    /// header.</b> Magic first, so "these are not the bytes you thought" is
    /// answered before anything is interpreted at all; then the format version,
    /// which is the field that says where every other field is. Everything after
    /// those six bytes is read on the authority of what they said.
    /// </para>
    /// <para>
    /// <b>Reading is the first gate and it is hard.</b> Unknown magic and an
    /// unknown format version both refuse outright. A format version <i>newer</i>
    /// than this reader knows refuses for a reason worth spelling out: the reader
    /// cannot know what it is missing, so there is no such thing as reading it
    /// partially and being right about the part. An <i>older, known</i> version
    /// reads perfectly well through its own branch, and that is the normal case
    /// forever.
    /// </para>
    /// </remarks>
    public readonly struct RecordHeader : IEquatable<RecordHeader>
    {
        public RecordHeader(RecordKind kind, int formatVersion, uint simVersion, Hash64 contentHash)
        {
            Kind = kind;
            FormatVersion = formatVersion;
            SimVersion = simVersion;
            ContentHash = contentHash;
        }

        /// <summary>Which record kind these bytes claim to be.</summary>
        public RecordKind Kind { get; }

        /// <summary>The layout version, counted per kind. See <see cref="RecordFormat"/>.</summary>
        public int FormatVersion { get; }

        /// <summary>The behaviour version. See <see cref="SimulationVersion"/>.</summary>
        public uint SimVersion { get; }

        /// <summary>The hash of the parsed type tables this record was made against.</summary>
        public Hash64 ContentHash { get; }

        /// <summary>
        /// The header the writer emits: this kind's current format version, this
        /// build's simulation version, and the content hash of the tables in
        /// front of it. There is no way to ask for an older format, on purpose.
        /// </summary>
        public static RecordHeader Current(RecordKind kind, Hash64 contentHash) =>
            new RecordHeader(kind, RecordFormat.CurrentVersionOf(kind), SimulationVersion.Current, contentHash);

        public static bool operator ==(RecordHeader a, RecordHeader b) => a.Equals(b);

        public static bool operator !=(RecordHeader a, RecordHeader b) => !a.Equals(b);

        public bool Equals(RecordHeader other) =>
            Kind == other.Kind
            && FormatVersion == other.FormatVersion
            && SimVersion == other.SimVersion
            && ContentHash == other.ContentHash;

        public override bool Equals(object? obj) => obj is RecordHeader other && Equals(other);

        public override int GetHashCode() =>
            ((int)Kind * 31 ^ FormatVersion) * 31 ^ ContentHash.GetHashCode();

        public override string ToString() =>
            RecordFormat.NameOf(Kind)
            + " format "
            + FormatVersion.ToString(CultureInfo.InvariantCulture)
            + ", simulation version "
            + SimVersion.ToString(CultureInfo.InvariantCulture)
            + ", content "
            + ContentHash.ToString();

        internal void Write(ByteWriter writer)
        {
            writer.Ascii("magic tag", RecordFormat.MagicOf(Kind));
            writer.U16("format version", FormatVersion);
            writer.U32("simulation version", SimVersion);
            writer.U64(ContentHash.Value);
        }

        /// <summary>
        /// Reads a header and runs the read gate on it. Everything after this
        /// call is parsed on the authority of the version it returned.
        /// </summary>
        internal static RecordHeader Read(ByteCursor cursor, RecordKind expected)
        {
            string magic = cursor.Ascii("the magic tag", 4);
            string wanted = RecordFormat.MagicOf(expected);

            if (!string.Equals(magic, wanted, StringComparison.Ordinal))
            {
                throw cursor.Fault(
                    "begins with "
                    + Render(magic)
                    + " where a "
                    + RecordFormat.NameOf(expected)
                    + " begins with '"
                    + wanted
                    + "'"
                    + WhatItLooksLike(magic)
                    + ".");
            }

            int formatVersion = cursor.U16("the format version");

            if (!RecordFormat.IsKnown(expected, formatVersion))
            {
                throw cursor.Fault(Unknown(expected, formatVersion));
            }

            uint simVersion = cursor.U32("the simulation version");
            ulong contentHash = cursor.U64("the content hash");

            return new RecordHeader(expected, formatVersion, simVersion, Hash64.FromValue(contentHash));
        }

        private static string Unknown(RecordKind kind, int formatVersion)
        {
            int current = RecordFormat.CurrentVersionOf(kind);

            if (formatVersion > current)
            {
                return "is "
                    + RecordFormat.NameOf(kind)
                    + " format version "
                    + formatVersion.ToString(CultureInfo.InvariantCulture)
                    + ", which is newer than the "
                    + current.ToString(CultureInfo.InvariantCulture)
                    + " this reader knows. A newer record cannot be read best-effort, because a reader "
                    + "cannot know what it is missing -- it would parse the fields it recognises at "
                    + "offsets that have moved and return a record made of noise.";
            }

            return "is "
                + RecordFormat.NameOf(kind)
                + " format version "
                + formatVersion.ToString(CultureInfo.InvariantCulture)
                + ", which this reader has no branch for. An older version is meant to read fine forever "
                + "through its own branch, so a missing one is a branch that was skipped or deleted "
                + "rather than a record that is wrong.";
        }

        private static string WhatItLooksLike(string magic)
        {
            if (!RecordFormat.TryKindOfMagic(magic, out RecordKind actual))
            {
                return string.Empty;
            }

            return ". Those are the bytes of a " + RecordFormat.NameOf(actual);
        }

        /// <summary>Four bytes as a person would want to see them in a message.</summary>
        private static string Render(string magic)
        {
            var shown = new char[magic.Length];

            for (int index = 0; index < magic.Length; index++)
            {
                char character = magic[index];
                shown[index] = character >= ' ' && character <= '~' ? character : '?';
            }

            return "'" + new string(shown) + "'";
        }
    }
}
