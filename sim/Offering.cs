using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Which half of a round's menu an option came off.
    /// </summary>
    public enum OptionKind
    {
        /// <summary>One of the options drawn onto every round out of the roster.</summary>
        Ordinary = 0,

        /// <summary>One of the game changers an anchor's menu merges in.</summary>
        GameChanger = 1,
    }

    /// <summary>
    /// One thing on an offering: the creep taking it unlocks, and where on the
    /// menu it sat.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Id"/> is the identity a decision names, and
    /// <see cref="Kind"/> is what scopes it.</b> An ordinary option is
    /// identified by the unit type it unlocks; a game changer by its own id,
    /// which is what keeps two game changers over one body two different takes.
    /// </para>
    /// <para>
    /// The unit row arrives resolved, exactly as <see cref="UnitOrder"/>'s does:
    /// an option had to be drawn out of a unit table to exist at all, so
    /// carrying the row it was drawn from costs nothing and means nothing
    /// downstream re-resolves an id it was already told is good.
    /// </para>
    /// </remarks>
    public sealed class Option
    {
        internal Option(OptionKind kind, int id, UnitType type, string label, GameChanger? changer)
        {
            Kind = kind;
            Id = id;
            Type = type;
            Label = label;
            Changer = changer;
        }

        /// <summary>Which half of the menu this came off.</summary>
        public OptionKind Kind { get; }

        /// <summary>What a decision names to take this, inside its kind.</summary>
        public int Id { get; }

        /// <summary>The creep taking this unlocks.</summary>
        public UnitType Type { get; }

        /// <summary>That creep's id in the unit table.</summary>
        public int TypeId => Type.Id;

        /// <summary>For people and for messages. Nothing branches on it.</summary>
        public string Label { get; }

        /// <summary>
        /// The game changer this option is, or nothing where it is ordinary.
        /// What a prepared counter gets against it is
        /// <see cref="AnchorSchedule.BonusVsTag"/>, which needs the changer and
        /// not the body.
        /// </summary>
        public GameChanger? Changer { get; }

        public override string ToString() =>
            Label
            + " ("
            + NameOf(Kind)
            + " "
            + Id.ToString(CultureInfo.InvariantCulture)
            + ")";

        /// <summary>What a kind is called, in a message.</summary>
        public static string NameOf(OptionKind kind)
        {
            switch (kind)
            {
                case OptionKind.Ordinary:
                    return "ordinary option";

                case OptionKind.GameChanger:
                    return "game changer";

                default:
                    throw new SimulationException(
                        "Option kind "
                        + ((int)kind).ToString(CultureInfo.InvariantCulture)
                        + " is not one this build declares. A kind is declared, named here and drawn onto "
                        + "an offering, and all three or none.");
            }
        }
    }

    /// <summary>
    /// One round's public offering: the ordinary options drawn onto it, the
    /// game changers an anchor merges in, and how wide that round's wave is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The same selection for everybody in the match.</b> An offering is a
    /// pure function of the run's seed and the wave, and of nothing private --
    /// not the purse, not what has been unlocked, not what was sent. That is
    /// what makes it publishable, and it is what turns a send into a read
    /// rather than a guess: the shop becomes a second-order decision because
    /// everybody is shopping from one list.
    /// </para>
    /// <para>
    /// <b>Drawn fresh every round</b>, which is where most of a week's variety
    /// comes from -- ten draws a run against the anchors' three.
    /// </para>
    /// <para>
    /// <b>An anchor's menu merges rather than adds.</b> On an anchor round the
    /// game changers join that round's ordinary options and one thing is taken
    /// from the whole list, so a game changer competes head to head with an
    /// ordinary unlock. A free extra pick would end every run with everybody
    /// holding all three, which leaves only <i>when</i> they field it unknown.
    /// </para>
    /// <para>
    /// <b><see cref="WaveSlots"/> is the schedule's derivation and never a
    /// second series.</b> It comes from <see cref="AnchorSchedule.WaveSlotsAt"/>
    /// and is carried here so that a build phase is checked against one number
    /// rather than against a width whoever validated it recomputed.
    /// </para>
    /// </remarks>
    public sealed class Offering
    {
        private readonly Option[] _options;

        private Offering(int wave, int waveSlots, int ordinaryCount, Option[] options)
        {
            Wave = wave;
            WaveSlots = waveSlots;
            OrdinaryCount = ordinaryCount;
            _options = options;
        }

        /// <summary>Which wave of the run this offering stands in front of. Counted from one.</summary>
        public int Wave { get; }

        /// <summary>How many slots that wave has, derived by the schedule.</summary>
        public int WaveSlots { get; }

        /// <summary>How many of the options are the ordinary ones. The rest are the anchor's.</summary>
        public int OrdinaryCount { get; }

        /// <summary>Whether an anchor's menu was merged into this round.</summary>
        public bool IsAnchor => _options.Length > OrdinaryCount;

        /// <summary>The merged menu: the ordinary options, then the anchor's, in the order they were drawn.</summary>
        public IReadOnlyList<Option> Options => _options;

        /// <summary>How many things are on the menu altogether.</summary>
        public int Count => _options.Length;

        /// <summary>
        /// Draws one round's offering.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The ordinary options are drawn out of the roster's creeps without
        /// replacement, by the partial Fisher-Yates
        /// <see cref="AnchorSchedule.Fill"/> uses and for the same reason: a
        /// menu that offers one creep twice is one option wearing two
        /// positions, and no hashed collection enters the draw.
        /// </para>
        /// <para>
        /// <b>What is already unlocked is not taken out of the pool.</b> An
        /// offering that thinned itself against what somebody held would be a
        /// different offering for every player, and the whole of what this type
        /// is for is that it is not.
        /// </para>
        /// </remarks>
        /// <param name="rules">Where the ordinary-to-game-changer ratio and the slot widths are authored.</param>
        /// <param name="types">The roster the ordinary options are drawn from.</param>
        /// <param name="schedule">The shape, which is the only thing that derives a slot width.</param>
        /// <param name="filling">What this run drew onto each anchor's menu.</param>
        /// <param name="wave">Which wave of the run, counted from one.</param>
        /// <param name="seed">The derived position the draw starts from.</param>
        public static Offering Draw(
            Ruleset rules,
            UnitTypeTable types,
            AnchorSchedule schedule,
            AnchorFilling filling,
            int wave,
            ulong seed)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (schedule is null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            if (filling is null)
            {
                throw new ArgumentNullException(nameof(filling));
            }

            if (wave < 1)
            {
                throw new SimulationException(
                    "An offering was drawn for wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
                    + ". Waves are counted from one, so a wave below that is a round index that was never "
                    + "turned into a wave number -- and it would draw a menu nobody plays against.");
            }

            UnitType[] roster = Creeps(types);

            if (roster.Length < rules.OrdinaryOptionsPerRound)
            {
                throw new SimulationException(
                    "Wave "
                    + wave.ToString(CultureInfo.InvariantCulture)
                    + "'s offering draws "
                    + rules.OrdinaryOptionsPerRound.ToString(CultureInfo.InvariantCulture)
                    + " ordinary options out of a roster of "
                    + roster.Length.ToString(CultureInfo.InvariantCulture)
                    + " creeps. An option unlocks a creep and appears on a menu once, so a roster thinner "
                    + "than the ratio is an offering that cannot be drawn rather than one that offers the "
                    + "same creep twice.");
            }

            var dice = new Pcg32(seed);
            var drawn = new List<Option>();
            int[] positions = Positions(roster.Length);

            for (int index = 0; index < rules.OrdinaryOptionsPerRound; index++)
            {
                int remaining = positions.Length - index;
                int picked = index + (int)dice.NextBelow((uint)remaining);

                int swap = positions[index];
                positions[index] = positions[picked];
                positions[picked] = swap;

                UnitType creep = roster[positions[index]];
                drawn.Add(new Option(OptionKind.Ordinary, creep.Id, creep, creep.Label, null));
            }

            int ordinary = drawn.Count;

            if (filling.TryAt(wave, out AnchorMenu? menu))
            {
                IReadOnlyList<GameChanger> changers = menu!.GameChangers;

                for (int index = 0; index < changers.Count; index++)
                {
                    GameChanger changer = changers[index];
                    drawn.Add(new Option(
                        OptionKind.GameChanger,
                        changer.Id,
                        Body(types, changer),
                        changer.Label,
                        changer));
                }
            }

            return new Offering(wave, schedule.WaveSlotsAt(rules, wave), ordinary, drawn.ToArray());
        }

        /// <summary>
        /// The option a decision names, or a refusal naming what was asked for
        /// and what was on the menu.
        /// </summary>
        /// <remarks>
        /// The refusal is unconditional and it is a refusal rather than a skip,
        /// for the reason the wave loader refuses an unknown type id: a run that
        /// partially validates produces a confidently wrong result that still
        /// looks like a result.
        /// </remarks>
        public Option Take(OptionKind kind, int id)
        {
            if (TryFind(kind, id, out Option? option))
            {
                return option!;
            }

            throw new SimulationException(
                "A build phase at wave "
                + Wave.ToString(CultureInfo.InvariantCulture)
                + " takes "
                + Option.NameOf(kind)
                + " "
                + id.ToString(CultureInfo.InvariantCulture)
                + ", which that round's offering does not carry. It offered "
                + Describe()
                + ". An option the offering did not contain is refused rather than skipped, because the "
                + "offering is what everybody in the match was reading and a take against a different one "
                + "is a decision made in a different game.");
        }

        /// <summary>The option a decision names, if the menu carries it.</summary>
        public bool TryFind(OptionKind kind, int id, out Option? option)
        {
            for (int index = 0; index < _options.Length; index++)
            {
                if (_options[index].Kind == kind && _options[index].Id == id)
                {
                    option = _options[index];
                    return true;
                }
            }

            option = null;
            return false;
        }

        public override string ToString() =>
            "wave "
            + Wave.ToString(CultureInfo.InvariantCulture)
            + ", "
            + WaveSlots.ToString(CultureInfo.InvariantCulture)
            + " slots: "
            + Describe();

        /// <summary>Every unit in the table that walks. A tower is not an unlock.</summary>
        private static UnitType[] Creeps(UnitTypeTable types)
        {
            var creeps = new List<UnitType>();

            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Role == UnitRole.Moving)
                {
                    creeps.Add(types.Types[index]);
                }
            }

            return creeps.ToArray();
        }

        /// <summary>The row a game changer fields, which its schedule already checked exists.</summary>
        private static UnitType Body(UnitTypeTable types, GameChanger changer)
        {
            if (types.TryById(changer.TypeId, out UnitType? body))
            {
                return body!;
            }

            throw new SimulationException(
                "The game changer "
                + changer.ToString()
                + " fields type id "
                + changer.TypeId.ToString(CultureInfo.InvariantCulture)
                + ", which is in no row of the unit table this offering was drawn against. A schedule and "
                + "a roster that were loaded against each other cannot disagree about a body, so this is "
                + "an offering drawn from two tables that were never checked together.");
        }

        private static int[] Positions(int count)
        {
            var positions = new int[count];

            for (int index = 0; index < positions.Length; index++)
            {
                positions[index] = index;
            }

            return positions;
        }

        private string Describe() =>
            string.Join(", ", Array.ConvertAll(_options, option => option.ToString()));
    }
}
