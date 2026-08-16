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
    /// <para>
    /// <b>A file says which column layout it is written in, and there is a
    /// reader branch per layout.</b> A <c>layout</c> row states the number
    /// before any <c>unit</c> row is read, so the reader knows how many columns
    /// to expect and where each one is before it reads a single field. A file
    /// with no such row is layout 1, which is what every table written before
    /// the row existed is. Each layout folds under its own hash label, so a
    /// table read through one branch can never collide with a table read
    /// through another.
    /// </para>
    /// </remarks>
    public sealed class UnitTypeTable
    {
        /// <summary>The layout a file that does not say is written in.</summary>
        public const int DefaultLayout = 1;

        /// <summary>The layout the columns documented on <see cref="UnitType"/> describe.</summary>
        public const int CurrentLayout = 3;

        private const string Keyword = "unit";

        private const string LayoutKeyword = "layout";

        /// <summary>The words a row here may open with.</summary>
        private static readonly string[] RowWords = { Keyword, LayoutKeyword };

        /// <summary>Ids are <c>u16</c> in the record format, and zero means "no unit".</summary>
        private const int MinimumId = 1;

        private const int MaximumId = 65535;

        private static readonly string[] RoleWords = { "placed", "moving" };

        private static readonly string[] DeliveryWords = { "none", "hitscan", "projectile" };

        /// <summary>The word a unit outside the damage matrix carries in either type column.</summary>
        private const string NoTypeWord = "none";

        /// <summary>
        /// The word the bubble radius column carries where there is no bubble,
        /// which is the one column of the six that says so. A radius of zero is
        /// a bubble on the centre alone and means something else entirely.
        /// </summary>
        private const string AbsentWord = "none";

        /// <summary>
        /// The payload a bubble may not carry, spelled out so the refusal can
        /// say why rather than listing five words that do not include it.
        /// </summary>
        private const string RefusedPayloadWord = "range";

        /// <summary>
        /// Where a bubble centres. The index of each word is its
        /// <see cref="BubbleOrigin"/>, and absence comes first because a bubble
        /// nobody authored is <c>default</c>.
        /// </summary>
        private static readonly string[] OriginWords = { AbsentWord, "self", "target" };

        /// <summary>Which side it reaches into. Index is its <see cref="BubbleAffects"/>.</summary>
        private static readonly string[] AffectsWords = { AbsentWord, "friend", "enemy" };

        /// <summary>What it carries. Index is its <see cref="BubblePayload"/>.</summary>
        private static readonly string[] PayloadWords =
            { AbsentWord, "damage", "speed", "cooldown", "armour", "shield" };

        /// <summary>
        /// The three attack types, then the word a unit that never attacks
        /// carries. The index of each is its <see cref="AttackType"/>.
        /// </summary>
        private static readonly string[] AttackWords = WithNoType(DamageMatrix.AttackWords);

        /// <summary>
        /// The three armour types, then the word a unit with no health pool
        /// carries. The index of each is its <see cref="ArmourType"/>.
        /// </summary>
        private static readonly string[] ArmourWords = WithNoType(DamageMatrix.ArmourWords);

        private readonly UnitType[] _types;

        private UnitTypeTable(UnitType[] types, int layout, Hash64 contentHash)
        {
            _types = types;
            Layout = layout;
            ContentHash = contentHash;
        }

        /// <summary>The rows, in file order -- which is ascending id order.</summary>
        public IReadOnlyList<UnitType> Types => _types;

        /// <summary>Which column layout this table was written in and read through.</summary>
        public int Layout { get; }

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

            var types = new List<UnitType>();
            int previousId = 0;
            int layout = DefaultLayout;
            bool declared = false;

            foreach (DataText.Row row in DataText.Rows(source, text))
            {
                string[] fields = row.Fields;

                if (string.Equals(row.Keyword, LayoutKeyword, StringComparison.Ordinal))
                {
                    layout = ReadLayout(source, row.Line, fields, declared, types.Count);
                    declared = true;
                    continue;
                }

                DataText.RequireRow(source, row, RowWords);

                if (fields.Length != FieldCountOf(layout))
                {
                    throw WrongColumnCount(source, row.Line, layout, fields.Length);
                }

                UnitType type = ReadRow(source, row.Line, fields, layout);

                if (type.Id == previousId)
                {
                    throw new ContentException(
                        source,
                        row.Line,
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
                        row.Line,
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

            Hash64 hash = Hash64.Start(HashLabelOf(layout)).Add(types.Count);

            foreach (UnitType type in types)
            {
                hash = type.Fold(hash, layout);
            }

            return new UnitTypeTable(types.ToArray(), layout, hash);
        }

        /// <summary>
        /// The same rows, with an upgrade ladder folded into the content hash --
        /// and <b>the receiver itself when the ladder has no edges in it</b>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>That identity is the load-bearing part of this method.</b>
        /// <c>content/golden/defense-0.replay</c> is a bundle this repository can
        /// never produce again, and its header carries the hash of the table
        /// pinned beside it. No ladder is pinned there, so nothing folds, so its
        /// frozen hash stands forever. Every record made before this file existed
        /// stays replayable for the same reason.
        /// </para>
        /// <para>
        /// <b>The ladder joins this hash rather than being carried separately.</b>
        /// <see cref="ContentHash"/> is the one value the ghost, the wave, the
        /// bundle and the command stream all already stamp and all already gate
        /// on, so an edge set that changes what a roster means is covered by four
        /// writers without one of them gaining a field. What the edges fold under
        /// is the ladder's own label, so a ladder cannot hash equal to a row.
        /// </para>
        /// <para>
        /// The rows come back untouched either way: an edge is an annotation, and
        /// no <see cref="UnitType"/> gains or loses anything by being on one.
        /// </para>
        /// </remarks>
        public UnitTypeTable WithLadder(UpgradeLadder ladder)
        {
            if (ladder is null)
            {
                throw new ArgumentNullException(nameof(ladder));
            }

            if (ladder.Count == 0)
            {
                return this;
            }

            return new UnitTypeTable(
                _types,
                Layout,
                ContentHash.Add(unchecked((long)ladder.ContentHash.Value)));
        }

        /// <summary>
        /// Whether this reader has a branch for that column layout.
        /// </summary>
        /// <remarks>
        /// Spelled out one layout at a time rather than as
        /// <c>layout &lt;= current</c>, for the same reason
        /// <see cref="RecordFormat.IsKnown"/> is: these are the branches that
        /// exist rather than the branches that ought to, and a layout somebody
        /// skipped or deleted has to arrive here as a refusal instead of passing
        /// an inequality and falling through a switch.
        /// </remarks>
        public static bool IsKnownLayout(int layout)
        {
            return layout == 1 || layout == 2 || layout == 3;
        }

        /// <summary>Fields per row, keyword included, in that layout.</summary>
        public static int FieldCountOf(int layout)
        {
            switch (layout)
            {
                case 1:
                    return 15;

                case 2:
                    return 19;

                case 3:
                    return 28;

                default:
                    throw NoSuchLayout(layout);
            }
        }

        /// <summary>
        /// The label that layout folds under. It names both the table and its
        /// field layout, so a table read through one branch cannot hash equal to
        /// a table read through another.
        /// </summary>
        private static string HashLabelOf(int layout)
        {
            switch (layout)
            {
                case 1:
                    return "unit-types/1";

                case 2:
                    return "unit-types/2";

                case 3:
                    return "unit-types/3";

                default:
                    throw NoSuchLayout(layout);
            }
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
        /// The row an id names, required to be one this table has and -- where
        /// <paramref name="role"/> says so -- to play that half of the loop.
        /// <paramref name="what"/> names whatever asked for it, and the refusal
        /// quotes it back.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the whole of the rule, and every id that arrives from
        /// authored text or from stored bytes is resolved here.</b> The wave,
        /// the defense, the anchor schedule and the upgrade ladder each wrote it
        /// out for themselves once, and the copies drifted into three different
        /// sentences with nothing to make them agree. What does not come through
        /// here is a lookup of an id that was already resolved at load --
        /// <see cref="ById"/>, which names no context and requires no role.
        /// </para>
        /// <para>
        /// <b>It refuses with a <see cref="SimulationException"/> because it
        /// belongs to neither side of the load seam.</b> A caller reading text
        /// rewraps it as a <see cref="ContentException"/> carrying its line
        /// number, a caller reading bytes rewraps it as a
        /// <see cref="RecordException"/> carrying the record's name, and a
        /// caller that owes neither -- a ladder walked against a table it was
        /// not parsed against -- lets it through as the program fault it is.
        /// </para>
        /// <para>
        /// A null <paramref name="role"/> is "either half of the loop will do",
        /// which is what an upgrade edge wants: an edge joins two ids, and a
        /// ladder of creeps stays structurally possible.
        /// </para>
        /// </remarks>
        public UnitType Require(int id, UnitRole? role, string what)
        {
            if (!TryById(id, out UnitType? type))
            {
                throw new SimulationException(
                    Asking(what, role)
                    + " names type id "
                    + id.ToString(CultureInfo.InvariantCulture)
                    + ", which this unit type table does not define. An unknown id refuses rather than "
                    + "being skipped: an order, a tower or an edge that resolves to nothing produces a "
                    + "confidently wrong result that still validates.");
            }

            if (role.HasValue && type!.Role != role.Value)
            {
                throw new SimulationException(
                    Asking(what, role)
                    + " names "
                    + type.ToString()
                    + ", which is a "
                    + RoleWords[(int)type.Role]
                    + " unit.");
            }

            return type!;
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

        /// <summary>
        /// Reads the layout a file declares. It comes before every row it
        /// governs, because it is the field that says how to read them.
        /// </summary>
        private static int ReadLayout(string source, int line, string[] fields, bool declared, int rowsSoFar)
        {
            if (fields.Length != 2)
            {
                throw DataText.WrongFieldCount(source, line, LayoutKeyword, 2, fields.Length);
            }

            if (declared)
            {
                throw new ContentException(
                    source,
                    line,
                    "is a second '"
                    + LayoutKeyword
                    + "' row. A table is written in one column layout, and two rows claiming two of them "
                    + "means the rows above and below this line would be read against different field "
                    + "orders.");
            }

            if (rowsSoFar > 0)
            {
                throw new ContentException(
                    source,
                    line,
                    "declares the column layout after "
                    + rowsSoFar.ToString(CultureInfo.InvariantCulture)
                    + " rows have already been read against another one. The layout says how to read a "
                    + "row, so it is stated before the first of them or not at all.");
            }

            int layout = DataText.Integer(source, line, "the column layout", fields[1]);

            if (!IsKnownLayout(layout))
            {
                throw new ContentException(
                    source,
                    line,
                    "declares column layout "
                    + layout.ToString(CultureInfo.InvariantCulture)
                    + ", and this reader has branches for 1 through "
                    + CurrentLayout.ToString(CultureInfo.InvariantCulture)
                    + ". A layout that was skipped, or a branch somebody deleted, is refused here rather "
                    + "than read against whichever field order happened to be nearest.");
            }

            return layout;
        }

        private static ContentException WrongColumnCount(string source, int line, int layout, int actual)
        {
            return new ContentException(
                source,
                line,
                "a '"
                + Keyword
                + "' row has "
                + actual.ToString(CultureInfo.InvariantCulture)
                + " fields where column layout "
                + layout.ToString(CultureInfo.InvariantCulture)
                + " has "
                + FieldCountOf(layout).ToString(CultureInfo.InvariantCulture)
                + ". Field order is what the content hash folds, so a row with the wrong number of them "
                + "cannot be read at all. A table with columns this reader does not expect says so with a '"
                + LayoutKeyword
                + "' row above its first unit.");
        }

        /// <summary>
        /// The three type spellings with the out-of-matrix word appended, so
        /// that a keyword's index in the result is the enum value it parses to.
        /// </summary>
        private static string[] WithNoType(string[] words)
        {
            var all = new string[words.Length + 1];
            words.CopyTo(all, 0);
            all[words.Length] = NoTypeWord;

            return all;
        }

        /// <summary>
        /// What asked for a row, with the half of the loop it asked for appended
        /// where it named one. It is the subject of both sentences
        /// <see cref="Require"/> refuses with.
        /// </summary>
        private static string Asking(string what, UnitRole? role) =>
            role.HasValue
                ? what + " requiring a " + RoleWords[(int)role.Value] + " unit"
                : what;

        private static ArgumentOutOfRangeException NoSuchLayout(int layout) =>
            new ArgumentOutOfRangeException(
                nameof(layout),
                "Column layout "
                + layout.ToString(CultureInfo.InvariantCulture)
                + " has no reader branch in this table.");

        private static UnitType ReadRow(string source, int line, string[] fields, int layout)
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

            // What a layout-1 row carries: no cost, and no place in the matrix.
            int cost = 0;
            var attack = AttackType.None;
            var armour = ArmourType.None;
            int armourPoints = 0;

            if (layout >= 2)
            {
                cost = DataText.IntegerInRange(source, line, "the cost", fields[15], 0, int.MaxValue);
                attack = (AttackType)DataText.Keyword(source, line, "the attack type", fields[16], AttackWords);
                armour = (ArmourType)DataText.Keyword(source, line, "the armour type", fields[17], ArmourWords);
                armourPoints = DataText.IntegerInRange(source, line, "the armour", fields[18], 0, int.MaxValue);

                RequireTyping(source, line, delivery, attack, maxHp, armour, armourPoints);
            }

            // And what a row before layout 3 carries: no second pool, one shot
            // an attack, and nothing radial. None of those is a default standing
            // in for a value the table stated -- they are what such a row is.
            int shield = 0;
            int targets = 1;
            Bubble bubble = Bubble.Absent;

            if (layout >= 3)
            {
                shield = DataText.IntegerInRange(source, line, "the shield", fields[19], 0, int.MaxValue);
                targets = DataText.IntegerInRange(source, line, "the target count", fields[20], 1, int.MaxValue);
                bubble = ReadBubble(source, line, fields);

                RequireShotShapes(source, line, delivery, maxHp, shield, targets, bubble);
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
                dying,
                cost,
                attack,
                armour,
                armourPoints,
                shield,
                targets,
                bubble);
        }

        /// <summary>
        /// The six bubble columns, read as one thing because they are one thing.
        /// </summary>
        /// <remarks>
        /// <b>The radius column is the one that says whether there is a bubble
        /// at all</b>, because it is the column whose absence has a meaning
        /// distinct from every value it can hold: zero is the centre alone.
        /// <c>none</c> there is a row with nothing radial about it, and the five
        /// columns after it are then required to say the same -- a magnitude
        /// nobody reads would still move the content hash, which is the rule
        /// every other unread column in this file is refused by.
        /// </remarks>
        private static Bubble ReadBubble(string source, int line, string[] fields)
        {
            bool absent = string.Equals(fields[21], AbsentWord, StringComparison.Ordinal);

            int radius = absent
                ? 0
                : DataText.IntegerInRange(source, line, "the bubble radius", fields[21], 0, int.MaxValue);

            var origin = (BubbleOrigin)DataText.Keyword(source, line, "the bubble origin", fields[22], OriginWords);
            var affects = (BubbleAffects)DataText.Keyword(source, line, "what the bubble affects", fields[23], AffectsWords);
            int period = DataText.IntegerInRange(source, line, "the bubble period", fields[24], 0, int.MaxValue);
            var payload = ReadPayload(source, line, fields[25]);

            // Signed on purpose, and the Cryomancer is why: a slow is a speed
            // payload with a negative percentage, and no dedicated slow columns
            // exist for it to be authored in instead.
            int magnitude = DataText.Integer(source, line, "the bubble magnitude", fields[26]);
            int duration = DataText.IntegerInRange(source, line, "the bubble duration", fields[27], 0, int.MaxValue);

            RequireBubble(source, line, absent, origin, affects, period, payload, magnitude, duration);

            return absent
                ? Bubble.Absent
                : Bubble.Of(radius, origin, affects, period, payload, magnitude, duration);
        }

        /// <summary>
        /// The payload column, with the one word that is refused for a reason
        /// rather than for not being on a list.
        /// </summary>
        private static BubblePayload ReadPayload(string source, int line, string field)
        {
            if (string.Equals(field, RefusedPayloadWord, StringComparison.Ordinal))
            {
                throw new ContentException(
                    source,
                    line,
                    "carries a bubble payload of '"
                    + RefusedPayloadWord
                    + "'. Range is the one stat a bubble may not modify: a tower's coverage is intersected "
                    + "with the route once, at load, and handed to the tick loop as intervals of distance, "
                    + "so a payload that moved a range would have to rebuild those intervals inside the "
                    + "tick -- which is exactly what keeping the two dimensions out of the tick loop was "
                    + "for. Author a bigger range on the row instead.");
            }

            return (BubblePayload)DataText.Keyword(source, line, "the bubble payload", field, PayloadWords);
        }

        /// <summary>
        /// Every unit that attacks carries an attack type, every unit that can
        /// be damaged carries an armour type, and neither carries one it has no
        /// use for. Between them these are what stop a row falling outside the
        /// three-by-three matrix.
        /// </summary>
        private static void RequireTyping(
            string source,
            int line,
            Delivery delivery,
            AttackType attack,
            int maxHp,
            ArmourType armour,
            int armourPoints)
        {
            if (delivery != Delivery.None && attack == AttackType.None)
            {
                throw new ContentException(
                    source,
                    line,
                    "delivers damage but carries no attack type, so its shots fall outside the damage "
                    + "matrix and there is no cell to resolve one through.");
            }

            if (delivery == Delivery.None && attack != AttackType.None)
            {
                throw new ContentException(
                    source,
                    line,
                    "carries an attack type but delivers no damage, so the type would be read by nothing "
                    + "and would still move the content hash.");
            }

            if (maxHp > 0 && armour == ArmourType.None)
            {
                throw new ContentException(
                    source,
                    line,
                    "has a health pool but carries no armour type, so a shot at it falls outside the "
                    + "damage matrix and there is no cell to resolve one through.");
            }

            if (maxHp == 0 && armour != ArmourType.None)
            {
                throw new ContentException(
                    source,
                    line,
                    "carries an armour type but has no health pool, so nothing can ever be resolved "
                    + "against it and the type would still move the content hash.");
            }

            if (armour == ArmourType.None && armourPoints != 0)
            {
                throw new ContentException(
                    source,
                    line,
                    "carries "
                    + armourPoints.ToString(CultureInfo.InvariantCulture)
                    + " points of armour with no armour type to apply them through, so the number would "
                    + "be read by nothing and would still move the content hash.");
            }
        }

        /// <summary>
        /// The six bubble columns agree with each other: either all six describe
        /// a bubble or all six say there is none, and a bubble says what it
        /// carries in the units that payload is measured in.
        /// </summary>
        private static void RequireBubble(
            string source,
            int line,
            bool absent,
            BubbleOrigin origin,
            BubbleAffects affects,
            int period,
            BubblePayload payload,
            int magnitude,
            int duration)
        {
            if (absent)
            {
                if (origin != BubbleOrigin.None
                    || affects != BubbleAffects.None
                    || payload != BubblePayload.None
                    || period != 0
                    || magnitude != 0
                    || duration != 0)
                {
                    throw new ContentException(
                        source,
                        line,
                        "authors no bubble radius and then says something about the bubble anyway. The "
                        + "radius column is what says whether there is one -- zero is a bubble on the "
                        + "centre alone, and '"
                        + AbsentWord
                        + "' is no bubble -- so the five columns after it carry '"
                        + AbsentWord
                        + "' and zero, or the numbers would be read by nothing and would still move the "
                        + "content hash.");
                }

                return;
            }

            if (origin == BubbleOrigin.None || affects == BubbleAffects.None || payload == BubblePayload.None)
            {
                throw new ContentException(
                    source,
                    line,
                    "authors a bubble radius and then leaves the bubble half-described. A radius on its "
                    + "own says how far something reaches without saying where it is centred, whom it "
                    + "reaches or what it carries, and there is no value for any of those a reader could "
                    + "supply that the row ever stated.");
            }

            if (payload == BubblePayload.Damage)
            {
                if (magnitude != 0)
                {
                    throw new ContentException(
                        source,
                        line,
                        "carries a bubble magnitude of "
                        + magnitude.ToString(CultureInfo.InvariantCulture)
                        + " beside a damage payload. A damage bubble is one shot and one roll -- it "
                        + "carries the attack's own damage to everything it encloses, at full damage and "
                        + "with no falloff -- so a second damage number would be read by nothing and "
                        + "would still move the content hash.");
                }

                if (duration != 0)
                {
                    throw new ContentException(
                        source,
                        line,
                        "carries a bubble duration of "
                        + duration.ToString(CultureInfo.InvariantCulture)
                        + " ticks beside a damage payload. Damage lands and is done; a duration on it "
                        + "would be damage over time, which is a mechanic nobody has authored and which "
                        + "no reader here applies.");
                }

                return;
            }

            if (magnitude == 0)
            {
                throw new ContentException(
                    source,
                    line,
                    "carries a bubble that modifies "
                    + PayloadWords[(int)payload]
                    + " by nothing at all. A magnitude of zero is a bubble that is emitted, enclosed and "
                    + "applied every time it fires and changes no number when it lands.");
            }
        }

        /// <summary>
        /// The three columns layout 3 added agree with the row they were added
        /// to: a thing that cannot be damaged carries no shield, a thing that
        /// never attacks fires one shot, and the two shot shapes stay apart.
        /// </summary>
        /// <remarks>
        /// <b>The last of those is the determinism contract, spelled as a
        /// refusal.</b> <c>targets</c> fires n shots and draws n rolls; a damage
        /// bubble is one shot and draws one roll applied to everything it
        /// encloses. A row claiming both would draw n rolls and blanket the
        /// board n times, which is not a shape anybody designed -- and the
        /// number of draws per attack is folded into the state hash every tick,
        /// so it is settled here rather than found later as a replay that will
        /// not reproduce.
        /// </remarks>
        private static void RequireShotShapes(
            string source,
            int line,
            Delivery delivery,
            int maxHp,
            int shield,
            int targets,
            Bubble bubble)
        {
            if (maxHp == 0 && shield > 0)
            {
                throw new ContentException(
                    source,
                    line,
                    "carries a shield of "
                    + shield.ToString(CultureInfo.InvariantCulture)
                    + " with no health pool underneath it. A shield absorbs first and overkill carries "
                    + "through to health, so a shield on a unit nothing can damage is a pool nothing can "
                    + "ever spend.");
            }

            if (delivery == Delivery.None && targets != 1)
            {
                throw new ContentException(
                    source,
                    line,
                    "fires at "
                    + targets.ToString(CultureInfo.InvariantCulture)
                    + " targets and delivers no damage. A target count is shots per attack, so a row that "
                    + "never attacks has one of them the way it has one of everything else it never does.");
            }

            if (delivery == Delivery.None && bubble.Present && bubble.FiresWithTheAttack)
            {
                throw new ContentException(
                    source,
                    line,
                    "carries a bubble that fires with an attack it never makes. A period of zero is what "
                    + "says the bubble goes off as part of a shot; a bubble on a row that never shoots is "
                    + "an aura, and an aura has a period.");
            }

            if (targets != 1 && bubble.Payload == BubblePayload.Damage && bubble.FiresWithTheAttack)
            {
                throw new ContentException(
                    source,
                    line,
                    "fires at "
                    + targets.ToString(CultureInfo.InvariantCulture)
                    + " targets and carries a damage bubble on the same attack. Those are the two shot "
                    + "shapes and a row is one of them: n targets is n shots drawing n rolls, and a "
                    + "damage bubble is one shot drawing one roll that lands on everything it encloses. "
                    + "A row claiming both would draw one of them per body of the other, and the number "
                    + "of draws an attack makes is part of what every stored record replays through.");
            }
        }
    }
}
