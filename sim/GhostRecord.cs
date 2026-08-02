using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One tower as the record carries it: a type id and an axial cell, six
    /// bytes, and nothing else.
    /// </summary>
    /// <remarks>
    /// A tower entry names its type and never its stats. Range, damage, cooldown
    /// and windup live in the type tables, which the header's content hash pins,
    /// so a record cannot disagree with the ruleset it was made under about what
    /// a tower does. The cell is axial with no cube coordinate, for the same
    /// reason <see cref="Hex"/> does not store one: a third number that must
    /// always equal <c>-q-r</c> is only an opportunity to be inconsistent.
    /// </remarks>
    public readonly struct RecordTower : IEquatable<RecordTower>
    {
        public RecordTower(int typeId, Hex cell)
        {
            TypeId = typeId;
            Cell = cell;
        }

        /// <summary>The unit type, by its stable id. Never an array index.</summary>
        public int TypeId { get; }

        /// <summary>Where it stands, axial.</summary>
        public Hex Cell { get; }

        public static bool operator ==(RecordTower a, RecordTower b) => a.Equals(b);

        public static bool operator !=(RecordTower a, RecordTower b) => !a.Equals(b);

        public bool Equals(RecordTower other) => TypeId == other.TypeId && Cell == other.Cell;

        public override bool Equals(object? obj) => obj is RecordTower other && Equals(other);

        public override int GetHashCode() => (TypeId * 31) ^ Cell.GetHashCode();

        public override string ToString() =>
            "type " + TypeId.ToString(CultureInfo.InvariantCulture) + " at " + Cell.ToString();
    }

    /// <summary>
    /// A stored defense: the map it was built on by hash, and the towers, in
    /// canonical order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The seed is not here.</b> It lives in the replay bundle, because a
    /// defense's id is the hash of its bytes: putting the dice in the defense
    /// would make rolling different dice a different defense, and would orphan
    /// every replay pointing at the old one. The same argument evicts everything
    /// mutable or descriptive -- rating, author, timestamps, progression. Those
    /// are data <i>about</i> a record rather than input <i>to</i> a simulation,
    /// they belong to a cheap layer keyed by this record's id, and a rating that
    /// moved every time the defense won would otherwise change its identity.
    /// The expensive-to-change artefact stays minimal and the cheap layer absorbs
    /// the churn.
    /// </para>
    /// <para>
    /// <b>The map is pinned by hash and nothing else.</b> That answers the only
    /// question a replay asks about geometry -- what did this run happen on, and
    /// can I prove it is unchanged -- and it answers it whether maps turn out to
    /// be authored text, generated from a seed, or downloaded. A bare map id
    /// would not: somebody nudging one hex under a stored defense would replay it
    /// on different geometry and nothing would notice.
    /// </para>
    /// <para>
    /// <b>The map handle arrived at format version 1 and it is a handle, not an
    /// input.</b> It says which map this defense claims to be on, so a defense
    /// can be looked up, listed and drawn beside its map without a search
    /// through every map anybody has; it does not say what the geometry is, and
    /// nothing in the tick loop reads it. That is why the version-0 branch may
    /// default it -- see <see cref="RecordFormat.GhostVersion"/> for the same
    /// argument at length, and for why a simulation-affecting field could not be
    /// treated this way.
    /// </para>
    /// <para>
    /// <b>Canonical order is asserted here, not restored here.</b> Towers ascend
    /// by <c>r</c> and then by <c>q</c>, strictly, which is the same order
    /// <see cref="TowerLayout"/> asserts over the authored file. Sorting on load
    /// was considered and rejected: it would stabilise iteration and still leave
    /// two identical defenses with two different sets of bytes, at which point
    /// content-addressing one stops meaning anything at all.
    /// </para>
    /// </remarks>
    public sealed class GhostRecord : IEquatable<GhostRecord>
    {
        /// <summary>
        /// The handle a record carries when it does not say which map it was
        /// on: version-0 records, which had no field for one, and version-1
        /// records written by a caller that had no handle to give.
        /// </summary>
        /// <remarks>
        /// Zero is "unstated" rather than "map zero" for the same reason a type
        /// id of zero means no unit: a sentinel that is also a legal value is a
        /// sentinel nobody can test for. Whatever ends up assigning handles
        /// starts at one.
        /// </remarks>
        public const int NoMapHandle = 0;

        private readonly RecordTower[] _towers;

        private GhostRecord(RecordHeader header, Hash64 mapHash, int mapHandle, RecordTower[] towers)
        {
            Header = header;
            MapHash = mapHash;
            MapHandle = mapHandle;
            _towers = towers;
        }

        /// <summary>Magic, format version, simulation version, content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>The hash of the parsed grid this defense was placed on.</summary>
        public Hash64 MapHash { get; }

        /// <summary>
        /// Which map this defense claims to be on, or <see cref="NoMapHandle"/>
        /// when it does not say. A handle for looking one up, and never the
        /// authority on what the geometry is -- that is <see cref="MapHash"/>,
        /// and no amount of agreement here substitutes for it.
        /// </summary>
        public int MapHandle { get; }

        /// <summary>The towers, ascending by <c>(r, q)</c>. Asserted at load.</summary>
        public IReadOnlyList<RecordTower> Towers => _towers;

        /// <summary>How many towers there are.</summary>
        public int Count => _towers.Length;

        /// <summary>
        /// Records a live defense, at the current format version, on a map the
        /// caller names by handle.
        /// </summary>
        /// <remarks>
        /// The handle is asked for rather than found on the map, because a
        /// <see cref="HexMap"/> is a parsed grid and a grid has no name.
        /// Whatever stores maps knows what it filed this one under; the
        /// simulation does not, and inventing one here would put a number in
        /// records that means whatever the last person to guess thought it did.
        /// Pass <see cref="NoMapHandle"/> when there is nothing to say.
        /// </remarks>
        public static GhostRecord Of(HexMap map, TowerLayout layout, UnitTypeTable types, int mapHandle)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (layout is null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var towers = new RecordTower[layout.Count];

            for (int index = 0; index < layout.Count; index++)
            {
                PlacedTower tower = layout.Towers[index];
                towers[index] = new RecordTower(tower.Type.Id, tower.Hex);
            }

            return new GhostRecord(
                RecordHeader.Current(RecordKind.Ghost, types.ContentHash),
                map.MapHash,
                mapHandle,
                towers);
        }

        /// <summary>Reads a defense from bytes. The read gate, and nothing else.</summary>
        public static GhostRecord FromBytes(byte[] bytes) => FromBytes("defense record", bytes);

        /// <summary>Reads a defense from bytes, naming them in any error message.</summary>
        public static GhostRecord FromBytes(string record, byte[] bytes)
        {
            var cursor = new ByteCursor(record, bytes);
            GhostRecord read = ReadFrom(cursor);
            cursor.ExpectEnd("defense");
            return read;
        }

        /// <summary>The bytes. Always the current format version -- there is one writer.</summary>
        public byte[] ToBytes()
        {
            var writer = new ByteWriter(RecordFormat.HeaderBytes + 12 + (_towers.Length * RecordFormat.TowerBytes));
            WriteTo(writer);
            return writer.ToArray();
        }

        /// <summary>
        /// The defense as the simulation wants it, resolved against a type table.
        /// </summary>
        /// <remarks>
        /// This is where a type id becomes a type, and where a record naming an
        /// id the table has never heard of refuses. It does not refuse quietly
        /// and it does not skip the row: a replay that drops a tower it cannot
        /// resolve produces a confidently wrong result that still validates.
        /// </remarks>
        public TowerLayout ToLayout(UnitTypeTable types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var placed = new PlacedTower[_towers.Length];

            for (int index = 0; index < _towers.Length; index++)
            {
                RecordTower tower = _towers[index];

                if (!types.TryById(tower.TypeId, out UnitType? type))
                {
                    throw new RecordException(
                        "defense record",
                        "places type id "
                        + tower.TypeId.ToString(CultureInfo.InvariantCulture)
                        + ", which this unit type table does not define. An unknown id refuses rather "
                        + "than being skipped.");
                }

                if (type!.Role != UnitRole.Placed)
                {
                    throw new RecordException(
                        "defense record",
                        "places "
                        + type.ToString()
                        + ", which is a moving unit. A defense is composed of units that stand where "
                        + "they were put.");
                }

                Hex.ToOddRowOffset(tower.Cell, out int column, out int row);
                placed[index] = new PlacedTower(type, column, row, index + 1);
            }

            return TowerLayout.FromRecord(placed);
        }

        public bool Equals(GhostRecord? other)
        {
            if (other is null
                || Header != other.Header
                || MapHash != other.MapHash
                || MapHandle != other.MapHandle)
            {
                return false;
            }

            if (_towers.Length != other._towers.Length)
            {
                return false;
            }

            for (int index = 0; index < _towers.Length; index++)
            {
                if (_towers[index] != other._towers[index])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as GhostRecord);

        public override int GetHashCode() => (Header.GetHashCode() * 31) ^ _towers.Length;

        public override string ToString() =>
            Header.ToString()
            + ", "
            + _towers.Length.ToString(CultureInfo.InvariantCulture)
            + " towers on map "
            + MapHash.ToString()
            + (MapHandle == NoMapHandle
                ? " (no handle)"
                : " (handle " + MapHandle.ToString(CultureInfo.InvariantCulture) + ")");

        internal void WriteTo(ByteWriter writer)
        {
            // One writer, one version. There is no branch here and there never
            // will be: history lives in the reader, where it is a list that
            // grows by one, and a writer with history would multiply the pairs
            // anybody has to think about.
            Header.Write(writer);
            writer.U64(MapHash.Value);
            writer.U16("map handle", MapHandle);
            writer.U16("tower count", _towers.Length);

            for (int index = 0; index < _towers.Length; index++)
            {
                RecordTower tower = _towers[index];
                writer.U16("tower type id", tower.TypeId);
                writer.I16("tower q", tower.Cell.Q);
                writer.I16("tower r", tower.Cell.R);
            }
        }

        internal static GhostRecord ReadFrom(ByteCursor cursor)
        {
            RecordHeader header = RecordHeader.Read(cursor, RecordKind.Ghost);

            switch (header.FormatVersion)
            {
                case 0:
                    return ReadVersion0(cursor, header);

                case 1:
                    return ReadVersion1(cursor, header);

                default:
                    throw cursor.Fault(
                        "is defense format version "
                        + header.FormatVersion.ToString(CultureInfo.InvariantCulture)
                        + ", which the read gate accepted and this reader has no branch for. The two "
                        + "lists have drifted apart, which is a fault in this build rather than in the "
                        + "record.");
            }
        }

        /// <summary>
        /// Version 0: <c>u64 map_hash + u16 tower_count + Tower[]</c>. No map
        /// handle, so one is defaulted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This branch never goes away.</b> Version 1 sits beside it and this
        /// one keeps reading version-0 records forever, which is the normal case
        /// rather than a legacy path. <c>content/golden/defense-0.replay</c> is
        /// the evidence: a real recorded bundle, kept so that deleting this
        /// branch is a red gate rather than a quiet loss.
        /// </para>
        /// <para>
        /// <b>The default here is <see cref="NoMapHandle"/>, and it is honest
        /// rather than convenient.</b> A version-0 record genuinely does not say
        /// which map it was on, so "unstated" is what it says; the alternative,
        /// guessing a handle from the map hash, would put a number into a record
        /// that the record never carried. Nothing downstream is misled, because
        /// nothing downstream simulates from a handle. A field the tick loop
        /// read could not be defaulted at all -- see
        /// <see cref="RecordFormat.GhostVersion"/>.
        /// </para>
        /// </remarks>
        private static GhostRecord ReadVersion0(ByteCursor cursor, RecordHeader header)
        {
            ulong mapHash = cursor.U64("the map hash");
            int count = cursor.U16("the tower count");

            return ReadTowers(cursor, header, mapHash, NoMapHandle, count);
        }

        /// <summary>
        /// Version 1: <c>u64 map_hash + u16 map_id + u16 tower_count +
        /// Tower[]</c>.
        /// </summary>
        /// <remarks>
        /// The handle goes after the hash rather than in front of it, so the two
        /// things about the map sit together and every version-0 offset before
        /// them is unmoved. That is a courtesy to a hexdump and to a person
        /// reading both branches side by side; it is not what makes the old
        /// records readable, which is the branch above.
        /// </remarks>
        private static GhostRecord ReadVersion1(ByteCursor cursor, RecordHeader header)
        {
            ulong mapHash = cursor.U64("the map hash");
            int mapHandle = cursor.U16("the map handle");
            int count = cursor.U16("the tower count");

            return ReadTowers(cursor, header, mapHash, mapHandle, count);
        }

        /// <summary>
        /// The tower array, which both versions share unchanged. Shared because
        /// it is the same bytes and not because it is convenient: a copy per
        /// branch would let the canonical-order assertion drift between them,
        /// and the version that lost it would go on loading records the other
        /// refuses.
        /// </summary>
        private static GhostRecord ReadTowers(
            ByteCursor cursor,
            RecordHeader header,
            ulong mapHash,
            int mapHandle,
            int count)
        {
            if (count == 0)
            {
                throw cursor.Fault("has no towers in it at all.");
            }

            var towers = new RecordTower[count];
            int previousQ = 0;
            int previousR = 0;

            for (int index = 0; index < count; index++)
            {
                string what =
                    "tower "
                    + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + count.ToString(CultureInfo.InvariantCulture);

                int typeId = cursor.U16("the type id of " + what);
                int q = cursor.I16("the q of " + what);
                int r = cursor.I16("the r of " + what);

                if (typeId == 0)
                {
                    throw cursor.Fault(what + " has type id 0, and zero means no unit.");
                }

                if (index > 0 && (r < previousR || (r == previousR && q <= previousQ)))
                {
                    throw cursor.Fault(
                        what
                        + " is out of canonical order: towers ascend strictly by r and then by q, and ("
                        + q.ToString(CultureInfo.InvariantCulture)
                        + ", "
                        + r.ToString(CultureInfo.InvariantCulture)
                        + ") does not follow ("
                        + previousQ.ToString(CultureInfo.InvariantCulture)
                        + ", "
                        + previousR.ToString(CultureInfo.InvariantCulture)
                        + "). The order is asserted rather than sorted on load, because sorting would "
                        + "leave two identical defenses with two different sets of bytes and every id "
                        + "would become a hash of somebody's typing order. Equal coordinates are two "
                        + "towers on one cell, which is the same fault.");
                }

                previousQ = q;
                previousR = r;
                towers[index] = new RecordTower(typeId, new Hex(q, r));
            }

            return new GhostRecord(header, Hash64.FromValue(mapHash), mapHandle, towers);
        }
    }
}
