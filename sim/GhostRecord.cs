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
    /// <b>Version 0 deliberately carries no map handle</b>, which is a decision
    /// and not an omission. See <see cref="RecordFormat.GhostVersion"/> before
    /// adding one.
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
        private readonly RecordTower[] _towers;

        private GhostRecord(RecordHeader header, Hash64 mapHash, RecordTower[] towers)
        {
            Header = header;
            MapHash = mapHash;
            _towers = towers;
        }

        /// <summary>Magic, format version, simulation version, content hash.</summary>
        public RecordHeader Header { get; }

        /// <summary>The hash of the parsed grid this defense was placed on.</summary>
        public Hash64 MapHash { get; }

        /// <summary>The towers, ascending by <c>(r, q)</c>. Asserted at load.</summary>
        public IReadOnlyList<RecordTower> Towers => _towers;

        /// <summary>How many towers there are.</summary>
        public int Count => _towers.Length;

        /// <summary>Records a live defense, at the current format version.</summary>
        public static GhostRecord Of(HexMap map, TowerLayout layout, UnitTypeTable types)
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
            var writer = new ByteWriter(RecordFormat.HeaderBytes + 10 + (_towers.Length * RecordFormat.TowerBytes));
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
            if (other is null || Header != other.Header || MapHash != other.MapHash)
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
            + MapHash.ToString();

        internal void WriteTo(ByteWriter writer)
        {
            Header.Write(writer);
            writer.U64(MapHash.Value);
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
        /// Version 0: <c>u64 map_hash · u16 tower_count · Tower[]</c>.
        /// </summary>
        /// <remarks>
        /// This branch never goes away. When version 1 arrives it gets its own
        /// branch beside this one and this one keeps reading version-0 records
        /// forever, which is the normal case rather than a legacy path.
        /// </remarks>
        private static GhostRecord ReadVersion0(ByteCursor cursor, RecordHeader header)
        {
            ulong mapHash = cursor.U64("the map hash");
            int count = cursor.U16("the tower count");

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

            return new GhostRecord(header, Hash64.FromValue(mapHash), towers);
        }
    }
}
