using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One anchor of a shape: the wave it falls on, the tier its menu is drawn
    /// from, whether it opens the steep counter, and the answer to it.
    /// </summary>
    public sealed class Anchor
    {
        internal Anchor(int wave, int tier, bool opensTheSteepCounter, int counterTypeId, int counterFromWave)
        {
            Wave = wave;
            Tier = tier;
            OpensTheSteepCounter = opensTheSteepCounter;
            CounterTypeId = counterTypeId;
            CounterFromWave = counterFromWave;
        }

        /// <summary>Which wave of the run this anchor falls on.</summary>
        public int Wave { get; }

        /// <summary>Which pool this anchor's game changers are drawn from.</summary>
        public int Tier { get; }

        /// <summary>
        /// Whether this is the anchor whose game changers carry a
        /// <see cref="GameChanger.BonusVsTag"/>. Exactly one anchor per shape
        /// does, and it is the last one.
        /// </summary>
        public bool OpensTheSteepCounter { get; }

        /// <summary>The unit type that answers this anchor. A placed unit.</summary>
        public int CounterTypeId { get; }

        /// <summary>
        /// The wave that counter is purchasable from, which is strictly before
        /// <see cref="Wave"/>.
        /// </summary>
        public int CounterFromWave { get; }

        public override string ToString() =>
            "wave "
            + Wave.ToString(CultureInfo.InvariantCulture)
            + ", tier "
            + Tier.ToString(CultureInfo.InvariantCulture)
            + (OpensTheSteepCounter ? ", steep" : ", plain");

        /// <summary>
        /// Folds this anchor in field order.
        /// <see cref="OpensTheSteepCounter"/> is not folded because it is not a
        /// number the shape chose: the steep counter is the last anchor's, and a
        /// file that says otherwise is refused rather than loaded, so the column
        /// is a statement the loader checks rather than a value it reads.
        /// </summary>
        internal Hash64 Fold(Hash64 hash) =>
            hash
                .Add(Wave)
                .Add(Tier)
                .Add(CounterTypeId)
                .Add(CounterFromWave);
    }

    /// <summary>
    /// One game changer creep: which tier pool it sits in, the unit type it
    /// fields, and what its anchor's counter gets against it.
    /// </summary>
    public sealed class GameChanger
    {
        internal GameChanger(int id, string label, int tier, int typeId, int bonusVsTag)
        {
            Id = id;
            Label = label;
            Tier = tier;
            TypeId = typeId;
            BonusVsTag = bonusVsTag;
        }

        /// <summary>The identity a filling is drawn by. Never reused.</summary>
        public int Id { get; }

        /// <summary>For people and for messages. Nothing branches on it.</summary>
        public string Label { get; }

        /// <summary>Which pool this belongs to, and therefore which anchor's menu.</summary>
        public int Tier { get; }

        /// <summary>The unit type this fields. A moving unit: an anchor opens offense.</summary>
        public int TypeId { get; }

        /// <summary>
        /// What this game changer's anchor counter adds to its rolled damage
        /// before the type chart and armour, when it shoots this creep. Zero
        /// everywhere but the steep anchor's pool.
        /// </summary>
        public int BonusVsTag { get; }

        public override string ToString() =>
            Label + " (#" + Id.ToString(CultureInfo.InvariantCulture) + ")";

        internal Hash64 Fold(Hash64 hash) =>
            hash
                .Add(Id)
                .Add(Tier)
                .Add(TypeId)
                .Add(BonusVsTag);
    }

    /// <summary>
    /// The anchor schedule: the shape a rotation holds fixed, and the tier pools
    /// a run's filling is drawn from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the same arrangement as <see cref="UnitTypeTable"/> and
    /// <see cref="Ruleset"/>, for the same reason: the shape lives in a
    /// committed data file, the file is handed to <see cref="Parse(string,
    /// UnitTypeTable)"/> as text, and nothing in this assembly knows where it
    /// came from.
    /// </para>
    /// <para>
    /// <b>Two layers.</b> The <i>shape</i> is what this type holds: which waves
    /// are anchors, which tier each draws from, which one opens the steep
    /// counter, and what answers each. The <i>filling</i> -- which of a tier's
    /// game changers reach an anchor's menu -- is drawn per run by
    /// <see cref="Fill"/> and revealed at run start.
    /// </para>
    /// <para>
    /// <b><see cref="ContentHash"/> is folded over the parsed integers in field
    /// order, not over the file.</b> Reindent a column, rewrap a comment or
    /// rename a game changer and the hash does not move. Change one number and
    /// it does.
    /// </para>
    /// <para>
    /// <b>Wave slot width is derived here and authored nowhere.</b>
    /// <see cref="WaveSlotsAt"/> is the anchors counted against the ruleset's
    /// widening step, so moving an anchor moves the widths with it; a
    /// <c>slots</c> row in this file is refused by name rather than read as a
    /// second series free to drift.
    /// </para>
    /// </remarks>
    public sealed class AnchorSchedule
    {
        /// <summary>
        /// Names this schedule and its field layout inside the hash. The digit
        /// is the layout version: moving, adding or removing a field bumps it.
        /// </summary>
        private const string HashLabel = "anchor-schedule/1";

        private const string AnchorKeyword = "anchor";

        private const string ChangerKeyword = "changer";

        /// <summary>The row this file refuses on sight. See the remarks on the type.</summary>
        private const string SlotsKeyword = "slots";

        /// <summary>
        /// The words a row here may open with. <see cref="SlotsKeyword"/> is not
        /// among them and has a refusal of its own.
        /// </summary>
        private static readonly string[] RowWords = { AnchorKeyword, ChangerKeyword };

        /// <summary>Both rows carry the same number of columns, keyword included.</summary>
        private const int FieldsPerRow = 6;

        /// <summary>Where <c>steep</c> sits in <see cref="OpensWords"/>.</summary>
        private const int SteepWord = 1;

        /// <summary>
        /// Waves are counted from one. There is no upper bound here: how many
        /// waves a run lasts is the run's argument, and a shape whose late
        /// anchors fall past the end of a short one is a truncated run rather
        /// than a broken schedule.
        /// </summary>
        private const int FirstWave = 1;

        private const int LastWave = 65535;

        private const int HighestTier = 1000;

        /// <summary>
        /// Every id in this file -- a game changer's own, and the unit type ids
        /// it and its anchor's counter name -- is a <c>u16</c>, and zero means
        /// "nothing" rather than naming a row.
        /// </summary>
        private const int MinimumId = 1;

        private const int MaximumId = 65535;

        /// <summary>
        /// The largest counter a game changer may carry. It joins the base of a
        /// hit, so it is bounded where the matrix cells are and for the same
        /// reason: the product of the two has to stay inside a 64-bit integer.
        /// </summary>
        private const int LargestBonus = 1000000;

        /// <summary>What the two spellings of the <c>opens</c> column mean.</summary>
        private static readonly string[] OpensWords = { "plain", "steep" };

        private readonly Anchor[] _anchors;

        private readonly GameChanger[] _changers;

        private AnchorSchedule(Anchor[] anchors, GameChanger[] changers, Hash64 contentHash)
        {
            _anchors = anchors;
            _changers = changers;
            ContentHash = contentHash;
        }

        /// <summary>The anchors, in file order -- which is ascending wave order.</summary>
        public IReadOnlyList<Anchor> Anchors => _anchors;

        /// <summary>Every game changer in every tier pool, in file order.</summary>
        public IReadOnlyList<GameChanger> GameChangers => _changers;

        /// <summary>
        /// The content hash: a fold over every parsed integer, in field order.
        /// See the remarks on <see cref="AnchorSchedule"/>.
        /// </summary>
        public Hash64 ContentHash { get; }

        /// <summary>Parses the schedule from text. Not from a path -- see <see cref="DataText"/>.</summary>
        public static AnchorSchedule Parse(string text, UnitTypeTable types) =>
            Parse("anchor schedule", text, types);

        /// <summary>Parses the schedule from UTF-8 bytes, which is what a caller that read a file holds.</summary>
        public static AnchorSchedule ParseUtf8(byte[] utf8, UnitTypeTable types) =>
            ParseUtf8("anchor schedule", utf8, types);

        /// <summary>Parses the schedule, naming the content in any error message.</summary>
        public static AnchorSchedule ParseUtf8(string source, byte[] utf8, UnitTypeTable types) =>
            Parse(source, DataText.FromUtf8(source, utf8), types);

        /// <summary>
        /// Parses the schedule, naming the content in any error message.
        /// </summary>
        /// <param name="source">What to call this content in a refusal.</param>
        /// <param name="text">The authored shape and its tier pools.</param>
        /// <param name="types">
        /// The unit table every counter and every game changer body is resolved
        /// through. It is what makes "this anchor opens defense" a refusal at
        /// load rather than a discovery at wave nine.
        /// </param>
        public static AnchorSchedule Parse(string source, string text, UnitTypeTable types)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var draft = new Draft();

            foreach (DataText.Row row in DataText.Rows(source, text))
            {
                ReadRow(source, row.Line, row.Fields, draft, types);
            }

            draft.RequireEverything(source);

            Hash64 hash = Hash64.Start(HashLabel).Add(draft.Anchors.Count);

            foreach (Anchor anchor in draft.Anchors)
            {
                hash = anchor.Fold(hash);
            }

            hash = hash.Add(draft.Changers.Count);

            foreach (GameChanger changer in draft.Changers)
            {
                hash = changer.Fold(hash);
            }

            return new AnchorSchedule(draft.Anchors.ToArray(), draft.Changers.ToArray(), hash);
        }

        /// <summary>How many anchors fall at or before this wave.</summary>
        public int AnchorsBy(int wave)
        {
            int count = 0;

            for (int index = 0; index < _anchors.Length; index++)
            {
                if (_anchors[index].Wave <= wave)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// How many wave slots this round offers: the ruleset's starting width
        /// plus its widening step once for every anchor at or before the round.
        /// </summary>
        /// <remarks>
        /// The one place a slot width comes from. It is a derivation over the
        /// shape rather than a series authored beside it, so moving an anchor
        /// moves the widths with it and the two cannot fall out of step.
        /// </remarks>
        public int WaveSlotsAt(Ruleset rules, int wave)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return rules.WaveSlotsAt(AnchorsBy(wave));
        }

        /// <summary>Every game changer in this anchor's tier pool, in file order.</summary>
        public IReadOnlyList<GameChanger> PoolFor(Anchor anchor)
        {
            if (anchor is null)
            {
                throw new ArgumentNullException(nameof(anchor));
            }

            var pool = new List<GameChanger>();

            for (int index = 0; index < _changers.Length; index++)
            {
                if (_changers[index].Tier == anchor.Tier)
                {
                    pool.Add(_changers[index]);
                }
            }

            return pool;
        }

        /// <summary>
        /// What a shot from this unit type adds to its rolled damage against this
        /// game changer, before the type chart and armour.
        /// </summary>
        /// <remarks>
        /// A counter answers one anchor, so the bonus is the anchor's and it is
        /// paid only to the unit type that anchor named. Anything else shooting
        /// the same creep is unprepared and gets nothing, which is the whole of
        /// what "prepared" buys.
        /// </remarks>
        public int BonusVsTag(int shooterTypeId, GameChanger changer)
        {
            if (changer is null)
            {
                throw new ArgumentNullException(nameof(changer));
            }

            for (int index = 0; index < _anchors.Length; index++)
            {
                if (_anchors[index].Tier == changer.Tier && _anchors[index].CounterTypeId == shooterTypeId)
                {
                    return changer.BonusVsTag;
                }
            }

            return 0;
        }

        /// <summary>
        /// Draws one run's filling: which game changers sit on each anchor's
        /// menu.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Drawn once, from a position derived from the run's seed, and revealed
        /// at run start. The pool is drawn from without replacement, so a menu
        /// never offers the same game changer twice, and each anchor draws from
        /// its own tier alone -- which is what stops a late-grade creep reaching
        /// an early wave.
        /// </para>
        /// <para>
        /// The draw is a partial Fisher-Yates over the pool's positions. It walks
        /// the anchors in wave order on one stream, so the whole filling is a
        /// pure function of the position handed in.
        /// </para>
        /// </remarks>
        /// <param name="gameChangersPerAnchor">How many an anchor's menu carries.</param>
        /// <param name="seed">The derived position the draw starts from.</param>
        public AnchorFilling Fill(int gameChangersPerAnchor, ulong seed)
        {
            if (gameChangersPerAnchor < 1)
            {
                throw new SimulationException(
                    "An anchor's menu was asked for "
                    + gameChangersPerAnchor.ToString(CultureInfo.InvariantCulture)
                    + " game changers. An anchor whose menu carries none of them is an ordinary round "
                    + "wearing an anchor's name.");
            }

            var dice = new Pcg32(seed);
            var menus = new AnchorMenu[_anchors.Length];

            for (int index = 0; index < _anchors.Length; index++)
            {
                Anchor anchor = _anchors[index];
                IReadOnlyList<GameChanger> pool = PoolFor(anchor);

                if (pool.Count < gameChangersPerAnchor)
                {
                    throw new SimulationException(
                        "The anchor at wave "
                        + anchor.Wave.ToString(CultureInfo.InvariantCulture)
                        + " draws a menu of "
                        + gameChangersPerAnchor.ToString(CultureInfo.InvariantCulture)
                        + " from a tier pool of "
                        + pool.Count.ToString(CultureInfo.InvariantCulture)
                        + ". A game changer appears on a menu once, so a pool thinner than the menu is a "
                        + "filling that cannot be drawn rather than one that repeats itself.");
                }

                menus[index] = new AnchorMenu(anchor, Draw(dice, pool, gameChangersPerAnchor));
            }

            return new AnchorFilling(menus);
        }

        /// <summary>This many of the pool, without replacement, in the order they were drawn.</summary>
        private static GameChanger[] Draw(Pcg32 dice, IReadOnlyList<GameChanger> pool, int count)
        {
            int[] positions = Draws.Positions(dice, pool.Count, count);
            var drawn = new GameChanger[count];

            for (int index = 0; index < count; index++)
            {
                drawn[index] = pool[positions[index]];
            }

            return drawn;
        }

        private static void ReadRow(string source, int line, string[] fields, Draft draft, UnitTypeTable types)
        {
            switch (fields[0])
            {
                case AnchorKeyword:
                    DataText.RequireFieldCount(source, line, AnchorKeyword, FieldsPerRow, fields);
                    draft.AddAnchor(
                        source,
                        line,
                        types,
                        DataText.IntegerInRange(
                            source, line, "the anchor's wave", fields[1], FirstWave, LastWave),
                        DataText.IntegerInRange(source, line, "the anchor's tier", fields[2], 1, HighestTier),
                        DataText.Keyword(source, line, "what the anchor opens", fields[3], OpensWords) == SteepWord,
                        DataText.IntegerInRange(
                            source, line, "the counter's type id", fields[4], MinimumId, MaximumId),
                        DataText.IntegerInRange(
                            source,
                            line,
                            "the wave the counter is purchasable from",
                            fields[5],
                            FirstWave,
                            LastWave));
                    return;

                case ChangerKeyword:
                    DataText.RequireFieldCount(source, line, ChangerKeyword, FieldsPerRow, fields);
                    draft.AddChanger(
                        source,
                        line,
                        types,
                        DataText.IntegerInRange(
                            source, line, "the game changer's id", fields[1], MinimumId, MaximumId),
                        DataText.Label(source, line, "the label", fields[2]),
                        DataText.IntegerInRange(source, line, "the game changer's tier", fields[3], 1, HighestTier),
                        DataText.IntegerInRange(
                            source, line, "the game changer's type id", fields[4], MinimumId, MaximumId),
                        DataText.IntegerInRange(source, line, "the bonus against its tag", fields[5], 0, LargestBonus));
                    return;

                case SlotsKeyword:
                    throw new ContentException(
                        source,
                        line,
                        "is a '"
                        + SlotsKeyword
                        + "' row. Wave slot width is DERIVED from the anchors above it and the ruleset's "
                        + "widening step, and is authored nowhere: a second series here would be a copy of "
                        + "that derivation, and the first time somebody moved an anchor without moving the "
                        + "copy the two would disagree with nothing to say so.");

                default:
                    throw DataText.NoSuchRow(source, line, fields[0], RowWords);
            }
        }

        /// <summary>
        /// The schedule part-read: the rows so far, and every rule that can be
        /// checked against the row above.
        /// </summary>
        private sealed class Draft
        {
            internal List<Anchor> Anchors { get; } = new List<Anchor>();

            internal List<GameChanger> Changers { get; } = new List<GameChanger>();

            /// <summary>
            /// Adds an anchor, later than the one above it, on a higher tier, and
            /// answered by a placed unit that was purchasable before it.
            /// </summary>
            internal void AddAnchor(
                string source,
                int line,
                UnitTypeTable types,
                int wave,
                int tier,
                bool steep,
                int counterTypeId,
                int counterFromWave)
            {
                if (Anchors.Count > 0)
                {
                    Anchor above = Anchors[Anchors.Count - 1];

                    if (wave <= above.Wave)
                    {
                        throw new ContentException(
                            source,
                            line,
                            "puts an anchor at wave "
                            + wave.ToString(CultureInfo.InvariantCulture)
                            + ", at or before the anchor at wave "
                            + above.Wave.ToString(CultureInfo.InvariantCulture)
                            + " above it. Anchors ascend strictly down this file, which is what makes the "
                            + "shape canonical and a repeated wave impossible to read past.");
                    }

                    if (tier <= above.Tier)
                    {
                        throw new ContentException(
                            source,
                            line,
                            "draws the anchor at wave "
                            + wave.ToString(CultureInfo.InvariantCulture)
                            + " from tier "
                            + tier.ToString(CultureInfo.InvariantCulture)
                            + ", at or below the tier "
                            + above.Tier.ToString(CultureInfo.InvariantCulture)
                            + " of the anchor above it. Tiers escalate with the waves, so that nothing "
                            + "hands out a late-grade creep at an early wave where nothing yet answers it.");
                    }
                }

                if (counterFromWave >= wave)
                {
                    throw new ContentException(
                        source,
                        line,
                        "answers the anchor at wave "
                        + wave.ToString(CultureInfo.InvariantCulture)
                        + " with a counter purchasable from wave "
                        + counterFromWave.ToString(CultureInfo.InvariantCulture)
                        + ". A counter is purchasable STRICTLY BEFORE the anchor that needs it: an answer "
                        + "that first appears at the wave it answers is a forced simultaneous buy, and it "
                        + "deletes the preparation the schedule exists to restore.");
                }

                // An anchor is a threat that can be seen coming and the
                // preparation happens on the other side of the board, so what
                // answers one stands where it was put.
                DataText.RequireType(
                    source, line, types, counterTypeId, UnitRole.Placed, "an anchor's counter");

                Anchors.Add(new Anchor(wave, tier, steep, counterTypeId, counterFromWave));
            }

            /// <summary>Adds a game changer, on a new id, fielding a unit that attacks rather than defends.</summary>
            internal void AddChanger(
                string source,
                int line,
                UnitTypeTable types,
                int id,
                string label,
                int tier,
                int typeId,
                int bonusVsTag)
            {
                for (int index = 0; index < Changers.Count; index++)
                {
                    if (Changers[index].Id == id)
                    {
                        throw new ContentException(
                            source,
                            line,
                            "is a second game changer with id "
                            + id.ToString(CultureInfo.InvariantCulture)
                            + ". A game changer sits in one tier pool and therefore on exactly one anchor's "
                            + "menu, so that nobody doubles down on the same one twice; a repeated id is a "
                            + "game changer on two of them.");
                    }
                }

                if (Changers.Count > 0 && id < Changers[Changers.Count - 1].Id)
                {
                    throw new ContentException(
                        source,
                        line,
                        "has game changer id "
                        + id.ToString(CultureInfo.InvariantCulture)
                        + " after id "
                        + Changers[Changers.Count - 1].Id.ToString(CultureInfo.InvariantCulture)
                        + ". Ids ascend strictly down this file, so that the pools are canonical and a "
                        + "duplicate is impossible to miss.");
                }

                // AN ANCHOR OPENS OFFENSE AND NEVER DEFENSE: a better tower
                // would be a gift rather than a threat, and it would leave
                // preparation with nothing to be about.
                DataText.RequireType(
                    source, line, types, typeId, UnitRole.Moving, "a game changer's body");

                Changers.Add(new GameChanger(id, label, tier, typeId, bonusVsTag));
            }

            /// <summary>
            /// Every rule that cannot be checked one row at a time: that there is
            /// a shape at all, that exactly one anchor is steep and it is the
            /// last, that every pool has an anchor and every anchor a pool, and
            /// that the steep anchor's pool is the only one carrying a bonus.
            /// </summary>
            internal void RequireEverything(string source)
            {
                if (Anchors.Count == 0)
                {
                    throw new ContentException(
                        source,
                        0,
                        "has no 'anchor' rows, so the shape has no anchors in it and a run against it is "
                        + "ten ordinary rounds with a slot width that never widens.");
                }

                if (Changers.Count == 0)
                {
                    throw new ContentException(
                        source,
                        0,
                        "has no 'changer' rows, so every anchor's menu would be drawn from nothing.");
                }

                RequireOneSteepAnchorAtTheEnd(source);
                RequireEveryTierPaired(source);
                RequireTheBonusOnlyOnTheSteepTier(source);
            }

            private void RequireOneSteepAnchorAtTheEnd(string source)
            {
                int steep = 0;

                for (int index = 0; index < Anchors.Count; index++)
                {
                    if (Anchors[index].OpensTheSteepCounter)
                    {
                        steep++;
                    }
                }

                if (steep != 1)
                {
                    throw new ContentException(
                        source,
                        0,
                        "has "
                        + steep.ToString(CultureInfo.InvariantCulture)
                        + " anchors opening a steep counter. EXACTLY ONE PER SHAPE DOES: none makes "
                        + "preparation optional, and more than one turns a run on a single missed buy.");
                }

                if (!Anchors[Anchors.Count - 1].OpensTheSteepCounter)
                {
                    throw new ContentException(
                        source,
                        0,
                        "opens its steep counter at wave "
                        + SteepWave().ToString(CultureInfo.InvariantCulture)
                        + " rather than at the last anchor, wave "
                        + Anchors[Anchors.Count - 1].Wave.ToString(CultureInfo.InvariantCulture)
                        + ". The steep counter is the late anchor's, so that the rounds of income that pay "
                        + "for the answer have happened before the question is asked.");
                }
            }

            private void RequireEveryTierPaired(string source)
            {
                for (int index = 0; index < Anchors.Count; index++)
                {
                    if (CountInTier(Anchors[index].Tier) == 0)
                    {
                        throw new ContentException(
                            source,
                            0,
                            "draws the anchor at wave "
                            + Anchors[index].Wave.ToString(CultureInfo.InvariantCulture)
                            + " from tier "
                            + Anchors[index].Tier.ToString(CultureInfo.InvariantCulture)
                            + ", which no game changer is in.");
                    }
                }

                for (int index = 0; index < Changers.Count; index++)
                {
                    if (!IsAnchorTier(Changers[index].Tier))
                    {
                        throw new ContentException(
                            source,
                            0,
                            "puts "
                            + Changers[index].ToString()
                            + " in tier "
                            + Changers[index].Tier.ToString(CultureInfo.InvariantCulture)
                            + ", which no anchor draws from. A pool no menu reaches is content nobody can "
                            + "be offered, and its numbers would still move the content hash.");
                    }
                }
            }

            private void RequireTheBonusOnlyOnTheSteepTier(string source)
            {
                int steepTier = Anchors[Anchors.Count - 1].Tier;

                for (int index = 0; index < Changers.Count; index++)
                {
                    GameChanger changer = Changers[index];
                    bool onTheSteepTier = changer.Tier == steepTier;

                    if (onTheSteepTier && changer.BonusVsTag == 0)
                    {
                        throw new ContentException(
                            source,
                            0,
                            "puts "
                            + changer.ToString()
                            + " on the steep anchor's tier with no bonus against its tag, so the anchor "
                            + "that is supposed to open a steep counter can draw a menu that opens none.");
                    }

                    if (!onTheSteepTier && changer.BonusVsTag != 0)
                    {
                        throw new ContentException(
                            source,
                            0,
                            "gives "
                            + changer.ToString()
                            + " a bonus of "
                            + changer.BonusVsTag.ToString(CultureInfo.InvariantCulture)
                            + " against its tag outside the steep anchor's tier, so a second anchor opens "
                            + "a steep counter without saying so.");
                    }
                }
            }

            private int SteepWave()
            {
                for (int index = 0; index < Anchors.Count; index++)
                {
                    if (Anchors[index].OpensTheSteepCounter)
                    {
                        return Anchors[index].Wave;
                    }
                }

                return 0;
            }

            private int CountInTier(int tier)
            {
                int count = 0;

                for (int index = 0; index < Changers.Count; index++)
                {
                    if (Changers[index].Tier == tier)
                    {
                        count++;
                    }
                }

                return count;
            }

            private bool IsAnchorTier(int tier)
            {
                for (int index = 0; index < Anchors.Count; index++)
                {
                    if (Anchors[index].Tier == tier)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
