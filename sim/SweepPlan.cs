using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Everything one sweep is played under: the content it is pointed at, the
    /// shape of the runs, the economy's dials, who plays them, and how far it is
    /// allowed to reach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The map, the ruleset and the schedule are parameters, and so is
    /// everything else.</b> Pointing this at another map to score it, at another
    /// matrix, at a wider field or at a different offering ratio is an argument
    /// here -- which is the one thing that stops a sweep being a retrofit across
    /// every call site later.
    /// </para>
    /// <para>
    /// <b>Who plays the runs is one of those arguments.</b>
    /// <see cref="Policy"/> is the scripted player every build phase of every
    /// run comes from, and it defaults to <see cref="EvenShareBot"/> -- so
    /// scoring the same roster under a different strategy is a plan the caller
    /// builds rather than a branch inside the harness.
    /// </para>
    /// <para>
    /// <b>The offering ratio and the scouting line arrive as numbers rather than
    /// as a second ruleset file.</b> They are folded into the rules through
    /// <see cref="Ruleset.With"/>, so the ruleset a sweep plays is a real
    /// ruleset with a real content hash and not a set of overrides carried
    /// alongside one.
    /// </para>
    /// <para>
    /// <b>Death ends a run here by default, exactly as it does everywhere
    /// else</b>, and a harness that wants otherwise asks. One knob with one
    /// default is the point: a sweep that quietly played a different game from
    /// the one the same content plays through <c>play-run</c> would be a report
    /// about a rule nobody chose. What no-death mode buys is a round of data for
    /// every wave rather than a short row wherever a build failed, which is why
    /// the flag exists -- and how many rounds a row actually got is on the row,
    /// so neither answer can be mistaken for the other.
    /// </para>
    /// </remarks>
    public sealed class SweepPlan
    {
        /// <summary>The argument that says "whatever the ruleset already says" for a retunable number.</summary>
        public const int AsAuthored = -1;

        /// <summary>
        /// How many seeds each creep is played on unless the caller says
        /// otherwise. A sample this size separates the roster and answers while
        /// somebody is still looking at the shell; it is the number the committed
        /// report was produced at.
        /// </summary>
        public const int DefaultRunsPerCreep = 8;

        /// <summary>The <see cref="MostCreeps"/> that scores the whole roster.</summary>
        public const int WholeRoster = 0;

        /// <summary>Names the derivation of one run's seed inside a sweep.</summary>
        private const string RunLabel = "sweep-run/1";

        private readonly UnitType[] _creeps;

        /// <summary>
        /// Builds a sweep's parameters, resolving the retunable numbers against
        /// the ruleset and refusing anything the harness cannot play.
        /// </summary>
        /// <param name="map">The board every match of every run is fought on.</param>
        /// <param name="rules">The matrix, the purse, the bands and the dials below.</param>
        /// <param name="types">The roster the rows are scored over and every cost is read out of.</param>
        /// <param name="schedule">The shape: the anchors, their tiers and the derived slot widths.</param>
        /// <param name="defense">What stands while each run's waves are sent.</param>
        /// <param name="field">
        /// The population a round's field of K is drawn from -- canned, until
        /// runs are stored and a real pool of them exists. See the remarks on
        /// <see cref="Sweep"/>.
        /// </param>
        /// <param name="firstSeed">What every run's seed in this sweep is derived from.</param>
        /// <param name="runsPerCreep">How many seeds each row of the roster is played on.</param>
        /// <param name="waves">N: how many waves a run lasts.</param>
        /// <param name="fieldSize">K: how many opponents a round is resolved against.</param>
        /// <param name="deathEndsTheRun">Whether health reaching zero stops a run.</param>
        /// <param name="ordinaryOptionsPerRound">The offering ratio's first half, or <see cref="AsAuthored"/>.</param>
        /// <param name="gameChangersPerAnchor">The offering ratio's second half, or <see cref="AsAuthored"/>.</param>
        /// <param name="freeSnapshotsPerRun">How many snapshots a run gets free, or <see cref="AsAuthored"/>.</param>
        /// <param name="snapshotPriceGold">What one costs after that, or <see cref="AsAuthored"/>.</param>
        /// <param name="mostCreeps">
        /// How many rows of the roster to score, or <see cref="WholeRoster"/>.
        /// Whatever it leaves out is reported rather than silently absent.
        /// </param>
        /// <param name="policy">
        /// The scripted player every build phase of every run comes from, or
        /// nothing for <see cref="EvenShareBot"/>.
        /// </param>
        public SweepPlan(
            HexMap map,
            Ruleset rules,
            UnitTypeTable types,
            AnchorSchedule schedule,
            TowerLayout defense,
            FieldPool field,
            ulong firstSeed,
            int runsPerCreep = DefaultRunsPerCreep,
            int waves = Run.DefaultWaves,
            int fieldSize = Run.DefaultFieldSize,
            bool deathEndsTheRun = true,
            int ordinaryOptionsPerRound = AsAuthored,
            int gameChangersPerAnchor = AsAuthored,
            int freeSnapshotsPerRun = AsAuthored,
            int snapshotPriceGold = AsAuthored,
            int mostCreeps = WholeRoster,
            BuildPolicy? policy = null)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Types = types ?? throw new ArgumentNullException(nameof(types));
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            Defense = defense ?? throw new ArgumentNullException(nameof(defense));
            Field = field ?? throw new ArgumentNullException(nameof(field));

            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            Rules = rules.With(
                Or(ordinaryOptionsPerRound, rules.OrdinaryOptionsPerRound),
                Or(gameChangersPerAnchor, rules.GameChangersPerAnchor),
                Or(freeSnapshotsPerRun, rules.FreeSnapshotsPerRun),
                Or(snapshotPriceGold, rules.SnapshotPriceGold));

            RequireAtLeast(runsPerCreep, 1, "runs per creep", "A cell of no runs is a row about nothing.");
            RequireAtLeast(
                waves,
                1,
                "waves",
                "A sweep row is a bounded run: lifting the wave cap makes it a loop rather than a row.");
            RequireAtLeast(
                mostCreeps,
                WholeRoster,
                "the roster bound",
                "The whole roster is written as "
                + WholeRoster.ToString(CultureInfo.InvariantCulture)
                + " rather than as a negative count.");

            FirstSeed = firstSeed;
            RunsPerCreep = runsPerCreep;
            Waves = waves;
            FieldSize = fieldSize;
            DeathEndsTheRun = deathEndsTheRun;
            MostCreeps = mostCreeps;
            Policy = policy ?? EvenShareBot.Decide;
            _creeps = Scored(types, mostCreeps);
            Roster = Walkers(types);
        }

        /// <summary>The board.</summary>
        public HexMap Map { get; }

        /// <summary>The rules as this sweep plays them, dials folded in and content hash moved with them.</summary>
        public Ruleset Rules { get; }

        /// <summary>The roster.</summary>
        public UnitTypeTable Types { get; }

        /// <summary>The shape.</summary>
        public AnchorSchedule Schedule { get; }

        /// <summary>What stands while a run's waves are sent.</summary>
        public TowerLayout Defense { get; }

        /// <summary>The population a round's field is drawn from.</summary>
        public FieldPool Field { get; }

        /// <summary>What every run's seed in this sweep is derived from.</summary>
        public ulong FirstSeed { get; }

        /// <summary>How many seeds each scored creep is played on.</summary>
        public int RunsPerCreep { get; }

        /// <summary>N.</summary>
        public int Waves { get; }

        /// <summary>K.</summary>
        public int FieldSize { get; }

        /// <summary>Whether health reaching zero stops a run.</summary>
        public bool DeathEndsTheRun { get; }

        /// <summary>How many rows of the roster this sweep scores, or <see cref="WholeRoster"/>.</summary>
        public int MostCreeps { get; }

        /// <summary>Every creep in the roster, scored or not.</summary>
        public int Roster { get; }

        /// <summary>
        /// Who plays the runs: the scripted player every build phase of this
        /// sweep is decided by.
        /// </summary>
        public BuildPolicy Policy { get; }

        /// <summary>The creeps this sweep actually scores, in table order.</summary>
        public IReadOnlyList<UnitType> Creeps => _creeps;

        /// <summary>
        /// The seed one run of this sweep is played on: derived from the sweep's
        /// own seed and the run's index rather than counted up from it, so two
        /// adjacent runs are not two adjacent streams.
        /// </summary>
        public ulong SeedOf(int run)
        {
            if (run < 0 || run >= RunsPerCreep)
            {
                throw new SimulationException(
                    "A sweep was asked for the seed of run "
                    + run.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + RunsPerCreep.ToString(CultureInfo.InvariantCulture)
                    + ". Every run of a cell is played on a seed derived from its index, so an index "
                    + "outside the cell is a run nothing in this sweep plays.");
            }

            return Hash64.Start(RunLabel).Add(unchecked((long)FirstSeed)).Add(run).Value;
        }

        /// <summary>An argument, or what the ruleset already says where the argument declined to say.</summary>
        private static int Or(int argument, int authored) => argument == AsAuthored ? authored : argument;

        /// <summary>How many rows of the roster walk. A tower is not a thing a wave sends.</summary>
        private static int Walkers(UnitTypeTable types)
        {
            int walkers = 0;

            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Role == UnitRole.Moving)
                {
                    walkers++;
                }
            }

            return walkers;
        }

        /// <summary>
        /// The creeps a sweep scores: the roster's walkers in table order, cut
        /// to the bound if there is one.
        /// </summary>
        private static UnitType[] Scored(UnitTypeTable types, int mostCreeps)
        {
            var creeps = new List<UnitType>();

            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Role != UnitRole.Moving)
                {
                    continue;
                }

                if (mostCreeps != WholeRoster && creeps.Count == mostCreeps)
                {
                    break;
                }

                creeps.Add(types.Types[index]);
            }

            if (creeps.Count == 0)
            {
                throw new SimulationException(
                    "A sweep was pointed at a roster with no creep in it to score. A row of this report is "
                    + "what one creep did over a population of runs, so a roster of towers alone is a sweep "
                    + "with nothing to be about rather than a sweep that comes back empty.");
            }

            return creeps.ToArray();
        }

        private static void RequireAtLeast(int value, int minimum, string what, string why)
        {
            if (value >= minimum)
            {
                return;
            }

            throw new SimulationException(
                "A sweep was planned with "
                + value.ToString(CultureInfo.InvariantCulture)
                + " for "
                + what
                + ", and the least it can be is "
                + minimum.ToString(CultureInfo.InvariantCulture)
                + ". "
                + why);
        }
    }
}
