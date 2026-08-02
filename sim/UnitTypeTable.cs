using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Every unit type the simulation knows, parsed from authored text, and the
    /// content hash over what was parsed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what "tuning never blocks on a compile" means in practice: the
    /// numbers live in a committed data file, the file is handed to
    /// <see cref="Parse(string)"/> as text, and nothing in this assembly knows
    /// where it came from.
    /// </para>
    /// <para>
    /// <b><see cref="ContentHash"/> is folded over the parsed integers in field
    /// order, not over the file.</b> Reindent a column, rewrap a comment,
    /// convert the line endings, rename a label: the hash does not move and no
    /// stored record is retired. Change one number and it does. That is the
    /// only version of this hash worth having -- a byte hash would fire on
    /// every one of those and would be overridden within a week.
    /// </para>
    /// <para>
    /// Ids must ascend strictly down the file. That makes the file canonical
    /// for free, makes a duplicate id a comparison against the previous row
    /// rather than a lookup, and keeps the parser clear of the hashed
    /// collections the IL scan bans.
    /// </para>
    /// </remarks>
    public sealed class UnitTypeTable
    {
        /// <summary>
        /// Names this table and its field layout inside the hash. The digit is
        /// the layout version: moving, adding or removing a column bumps it, so
        /// records pinned to the old layout are retired loudly instead of being
        /// silently reinterpreted against shifted fields.
        /// </summary>
        private const string HashLabel = "unit-types/1";

        private const string Keyword = "unit";

        /// <summary>Fields per row, keyword included. Checked before anything is read.</summary>
        private const int FieldCount = 15;

        /// <summary>Ids are <c>u16</c> in the record format, and zero means "no unit".</summary>
        private const int MinimumId = 1;

        private const int MaximumId = 65535;

        private static readonly string[] RoleWords = { "placed", "moving" };

        private static readonly string[] DeliveryWords = { "none", "hitscan", "projectile" };

        private readonly UnitType[] _types;

        private UnitTypeTable(UnitType[] types, Hash64 contentHash)
        {
            _types = types;
            ContentHash = contentHash;
        }

        /// <summary>The rows, in file order -- which is ascending id order.</summary>
        public IReadOnlyList<UnitType> Types => _types;

        /// <summary>How many types there are.</summary>
        public int Count => _types.Length;

        /// <summary>
        /// The content hash: a fold over every parsed integer of every row, in
        /// field order. See the remarks on <see cref="UnitTypeTable"/>.
        /// </summary>
        public Hash64 ContentHash { get; }

        /// <summary>Parses the table from text. Not from a path -- see <see cref="DataText"/>.</summary>
        public static UnitTypeTable Parse(string text) => Parse("unit types", text);

        /// <summary>Parses the table from UTF-8 bytes, which is what a caller that read a file holds.</summary>
        public static UnitTypeTable ParseUtf8(byte[] utf8) => ParseUtf8("unit types", utf8);

        /// <summary>Parses the table, naming the content in any error message.</summary>
        public static UnitTypeTable ParseUtf8(string source, byte[] utf8) =>
            Parse(source, DataText.FromUtf8(source, utf8));

        /// <summary>Parses the table, naming the content in any error message.</summary>
        public static UnitTypeTable Parse(string source, string text)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            string[] lines = DataText.SplitLines(text);
            var types = new List<UnitType>();
            int previousId = 0;

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                int number = index + 1;

                if (DataText.IsBlankOrComment(line))
                {
                    continue;
                }

                string[] fields = DataText.Fields(source, number, line);

                if (!string.Equals(fields[0], Keyword, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        source,
                        number,
                        "starts with '" + fields[0] + "', but the only row this table has is '" + Keyword + "'.");
                }

                if (fields.Length != FieldCount)
                {
                    throw DataText.WrongFieldCount(source, number, Keyword, FieldCount, fields.Length);
                }

                UnitType type = ReadRow(source, number, fields);

                if (type.Id == previousId)
                {
                    throw new ContentException(
                        source,
                        number,
                        "reuses type id "
                        + type.Id.ToString(CultureInfo.InvariantCulture)
                        + ". Ids are the one global identity in this file and a record pins them for years; "
                        + "two rows claiming one id means a stored record would resolve to whichever of them "
                        + "was read last.");
                }

                if (type.Id < previousId)
                {
                    throw new ContentException(
                        source,
                        number,
                        "has type id "
                        + type.Id.ToString(CultureInfo.InvariantCulture)
                        + " after id "
                        + previousId.ToString(CultureInfo.InvariantCulture)
                        + ". Ids ascend strictly down this file, so that the file is canonical and a "
                        + "duplicate is impossible to miss.");
                }

                previousId = type.Id;
                types.Add(type);
            }

            if (types.Count == 0)
            {
                throw new ContentException(source, 0, "has no unit types in it at all.");
            }

            Hash64 hash = Hash64.Start(HashLabel).Add(types.Count);

            foreach (UnitType type in types)
            {
                hash = type.Fold(hash);
            }

            return new UnitTypeTable(types.ToArray(), hash);
        }

        /// <summary>The type with this id.</summary>
        /// <exception cref="ContentException">There is no such type.</exception>
        public UnitType ById(int id)
        {
            if (TryById(id, out UnitType? type))
            {
                return type!;
            }

            throw new ContentException(
                "unit types",
                0,
                "has no type with id "
                + id.ToString(CultureInfo.InvariantCulture)
                + ". An unknown id refuses to load rather than being skipped: a replay that ignores a "
                + "unit it does not understand produces a confidently wrong result that still validates.");
        }

        /// <summary>
        /// The type with this id, if there is one. A linear scan on purpose:
        /// the table has a handful of rows, and the obvious dictionary is a
        /// banned type whose enumeration order is an implementation detail.
        /// </summary>
        public bool TryById(int id, out UnitType? type)
        {
            for (int index = 0; index < _types.Length; index++)
            {
                if (_types[index].Id == id)
                {
                    type = _types[index];
                    return true;
                }
            }

            type = null;
            return false;
        }

        private static UnitType ReadRow(string source, int line, string[] fields)
        {
            int id = DataText.IntegerInRange(source, line, "the type id", fields[1], MinimumId, MaximumId);
            string label = DataText.Label(source, line, "the label", fields[2]);
            var role = (UnitRole)DataText.Keyword(source, line, "the role", fields[3], RoleWords);
            int maxHp = DataText.IntegerInRange(source, line, "max hp", fields[4], 0, int.MaxValue);
            int speed = DataText.IntegerInRange(source, line, "speed", fields[5], 0, int.MaxValue);
            int range = DataText.IntegerInRange(source, line, "range", fields[6], 0, int.MaxValue);
            int cooldown = DataText.IntegerInRange(source, line, "cooldown ticks", fields[7], 0, int.MaxValue);
            int windup = DataText.IntegerInRange(source, line, "windup ticks", fields[8], 0, int.MaxValue);
            int backswing = DataText.IntegerInRange(source, line, "backswing ticks", fields[9], 0, int.MaxValue);
            int damageMin = DataText.IntegerInRange(source, line, "minimum damage", fields[10], 0, int.MaxValue);
            int damageMax = DataText.IntegerInRange(source, line, "maximum damage", fields[11], 0, int.MaxValue);
            var delivery = (Delivery)DataText.Keyword(source, line, "the delivery", fields[12], DeliveryWords);
            int flight = DataText.IntegerInRange(source, line, "projectile flight ticks", fields[13], 0, int.MaxValue);
            int dying = DataText.IntegerInRange(source, line, "dying ticks", fields[14], 0, int.MaxValue);

            if (damageMax < damageMin)
            {
                throw new ContentException(
                    source,
                    line,
                    "has a maximum damage below its minimum, so the roll has no values to draw from.");
            }

            if (delivery == Delivery.Projectile && flight < 1)
            {
                throw new ContentException(
                    source,
                    line,
                    "delivers by projectile but spends no ticks in flight, which is a hitscan attack "
                    + "wearing a projectile's clothes.");
            }

            if (delivery != Delivery.Projectile && flight != 0)
            {
                throw new ContentException(
                    source,
                    line,
                    "has projectile flight ticks but does not deliver by projectile, so the number would "
                    + "be read by nothing and would still move the content hash.");
            }

            return new UnitType(
                id,
                label,
                role,
                maxHp,
                speed,
                range,
                cooldown,
                windup,
                backswing,
                damageMin,
                damageMax,
                delivery,
                flight,
                dying);
        }
    }
}
