using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sim
{
    /// <summary>
    /// The handful of ticks in a match that anybody wants to be told about: the
    /// shot that lost its target, the first pass, the first leak, and the last
    /// creep to die. Those four and no more, because those four are the ones the
    /// sit-down checklist is written against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These are the tick numbers nobody knows until the match runs.</b> The
    /// sit-down checklist that ends this slice is written against them, which is
    /// why it can say "drag to 412 and back to 400" instead of "hunt for the
    /// moment" -- and why the table is committed rather than printed and thrown
    /// away. Regenerating it after a content change is a diff of four rows, and
    /// a checklist pointed at a tick that has moved goes stale loudly rather
    /// than sending somebody to look at the wrong second of the match.
    /// </para>
    /// <para>
    /// <b>Everything here arrives through <see cref="IMatchEvents"/> and
    /// nothing else.</b> No snapshot is pulled, because the run this is made
    /// from is the run with nobody watching -- that is the whole of what
    /// instant-resolve is. So this is a listener and not an inspector: it
    /// cannot reach into the match, and a landmark the match does not report is
    /// a landmark that does not exist rather than one to be inferred from the
    /// outside.
    /// </para>
    /// <para>
    /// <b>A table with a hole in it refuses to render.</b> A content change that
    /// stops anything ever being orphaned, or stops any creep ever passing
    /// another, has changed the match into one the checklist cannot be written
    /// against -- and the useful moment to find that out is while regenerating
    /// the table, by name, rather than at the sit-down with a row that points
    /// nowhere.
    /// </para>
    /// </remarks>
    public sealed class Landmarks : IMatchEvents
    {
        /// <summary>The tick a fast group first drew ahead of a slow one.</summary>
        public const string FirstOvertake = "first-overtake";

        /// <summary>The tick a shot in the air first lost the creep it was aimed at.</summary>
        public const string Orphaned = "projectile-orphaned";

        /// <summary>The tick the first creep reached the exit.</summary>
        public const string FirstLeak = "first-leak";

        /// <summary>The tick the last creep to die began dying on.</summary>
        public const string LastCreepDies = "last-creep-dies";

        /// <summary>Keyword every row starts with.</summary>
        private const string Keyword = "landmark";

        /// <summary>Column widths, so a regenerated table diffs against the old one row for row.</summary>
        private const int NameWidth = 22;

        private const int NumberWidth = 7;

        /// <summary>
        /// The rows, in the order they are written. Deliberately not sorted by
        /// tick: fixed rows mean a content change that moves a moment shows up
        /// as one line changing rather than as the whole table reshuffling.
        /// </summary>
        private static readonly string[] Order =
        {
            FirstOvertake,
            Orphaned,
            FirstLeak,
            LastCreepDies,
        };

        private readonly List<Landmark> _found = new List<Landmark>();

        private int _tick;

        private bool _told;

        /// <summary>
        /// The tick whose events are about to be listened to, called once
        /// before each tick is run. Nothing in an event carries a tick number --
        /// a tick number is state, and events are decorative -- so the caller,
        /// who is advancing a tick at a time to write the hash trace, is the one
        /// that knows.
        /// </summary>
        /// <remarks>
        /// Ticks arrive one at a time and in order, starting at tick one, and a
        /// gap throws. Advancing several ticks inside one call would file every
        /// event in all of them under one tick number, and a landmark table
        /// that is quietly a few ticks out is worse than no table at all:
        /// nothing about it looks wrong.
        /// </remarks>
        public void EnteringTick(int tick)
        {
            if (tick != _tick + 1)
            {
                throw new SimulationException(
                    "Landmarks were told about tick "
                    + tick.ToString(CultureInfo.InvariantCulture)
                    + " after tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + ". Ticks arrive one at a time and in order, because the events of a tick carry no "
                    + "tick number and the only thing that knows which tick they belong to is the caller "
                    + "advancing the match one tick per call.");
            }

            _tick = tick;
            _told = true;
        }

        /// <summary>The tick this was last told about. Zero before it was told anything.</summary>
        public int Tick => _tick;

        /// <summary>
        /// The name of the first landmark that never happened, or null when the
        /// table is complete.
        /// </summary>
        public string? Missing
        {
            get
            {
                for (int index = 0; index < Order.Length; index++)
                {
                    if (IndexOf(Order[index]) < 0)
                    {
                        return Order[index];
                    }
                }

                return null;
            }
        }

        /// <summary>What was found, in the order rows are written.</summary>
        /// <remarks>
        /// Call it once the match is over. <see cref="LastCreepDies"/> is the
        /// last death heard so far, which is only the last death of the match
        /// when there is no match left to run.
        /// </remarks>
        public IReadOnlyList<Landmark> Rows
        {
            get
            {
                var rows = new List<Landmark>();

                for (int index = 0; index < Order.Length; index++)
                {
                    int found = IndexOf(Order[index]);

                    if (found >= 0)
                    {
                        rows.Add(_found[found]);
                    }
                }

                return rows;
            }
        }

        /// <summary>
        /// The table as the text that is committed: one row per landmark, no
        /// header and no trailing newline, since whatever writes the file owns
        /// both.
        /// </summary>
        public string ToText()
        {
            string? missing = Missing;

            if (missing != null)
            {
                throw new SimulationException(
                    "Nothing in this match was a '"
                    + missing
                    + "', so the landmark table has a hole in it. Every row of it is a moment the "
                    + "sit-down checklist sends somebody to look at, and a table rendered without one "
                    + "would send them to look at a moment that never happens.");
            }

            var text = new StringBuilder();
            IReadOnlyList<Landmark> rows = Rows;

            for (int index = 0; index < rows.Count; index++)
            {
                if (index > 0)
                {
                    text.Append('\n');
                }

                text.Append(Line(rows[index]));
            }

            return text.ToString();
        }

        /// <summary>
        /// The table for a person to read: every row in the same layout, with
        /// the ones that never happened named rather than dropped.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b><see cref="ToText"/> is what gets committed; this is what gets
        /// printed.</b> The committed table refuses to render with a hole in it
        /// because the sit-down checklist is written against it, and a row that
        /// is simply absent sends somebody to look at a moment that never
        /// happens. That reasoning is about the checklist and not about the
        /// match: a report is nobody's checklist, and there the useful thing to
        /// do with a moment that did not occur is to say so.
        /// </para>
        /// <para>
        /// It matters for <c>content/golden/</c>, where a tiny historical bundle
        /// restaged under today's rules can legitimately produce a match in
        /// which nothing ever leaks. Refusing to print that would mean a rule
        /// change could not be measured against the very records kept to measure
        /// rule changes against.
        /// </para>
        /// </remarks>
        public string ToReportText()
        {
            var text = new StringBuilder();

            for (int index = 0; index < Order.Length; index++)
            {
                if (index > 0)
                {
                    text.Append('\n');
                }

                int found = IndexOf(Order[index]);

                text.Append(found >= 0 ? Line(_found[found]) : Absent(Order[index]));
            }

            return text.ToString();
        }

        /// <summary>One row, in the layout the committed table is written in.</summary>
        public static string Line(Landmark landmark) =>
            Keyword
            + "  "
            + landmark.Name.PadRight(NameWidth)
            + Number(landmark.Tick)
            + Number(landmark.Who)
            + Number(landmark.Other);

        /// <inheritdoc/>
        public void TowerFired(int towerId, int targetId)
        {
        }

        /// <inheritdoc/>
        public void CreepDamaged(int creepId, int amount)
        {
        }

        /// <inheritdoc/>
        public void CreepDied(int creepId) => Replace(LastCreepDies, creepId, 0);

        /// <inheritdoc/>
        public void CreepLeaked(int creepId) => Note(FirstLeak, creepId, 0);

        /// <inheritdoc/>
        public void ProjectileOrphaned(int projectileId) => Note(Orphaned, projectileId, 0);

        /// <inheritdoc/>
        public void CreepOvertook(int creepId, int overtakenCreepId) =>
            Note(FirstOvertake, creepId, overtakenCreepId);

        /// <summary>
        /// The row for a moment that never happened. Words rather than a tick of
        /// zero, because zero is a tick a real landmark could sit on.
        /// </summary>
        private static string Absent(string name) =>
            Keyword + "  " + name.PadRight(NameWidth) + "never happened";

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture).PadLeft(NumberWidth);

        /// <summary>Records a landmark the first time it happens, and never again.</summary>
        private void Note(string name, int who, int other)
        {
            if (IndexOf(name) >= 0)
            {
                return;
            }

            _found.Add(new Landmark(name, RequireTick(name), who, other));
        }

        /// <summary>Records a landmark that is the most recent one rather than the first.</summary>
        private void Replace(string name, int who, int other)
        {
            var landmark = new Landmark(name, RequireTick(name), who, other);
            int found = IndexOf(name);

            if (found < 0)
            {
                _found.Add(landmark);
                return;
            }

            _found[found] = landmark;
        }

        private int IndexOf(string name)
        {
            for (int index = 0; index < _found.Count; index++)
            {
                if (string.Equals(_found[index].Name, name, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private int RequireTick(string name)
        {
            if (!_told)
            {
                throw new SimulationException(
                    "A '"
                    + name
                    + "' arrived before anything said which tick it was on. Whatever is advancing the "
                    + "match has to name each tick before running it, or every landmark in the table is "
                    + "a number with nothing behind it.");
            }

            return _tick;
        }
    }

    /// <summary>One row of the landmark table: a moment, when it happened, and who it happened to.</summary>
    public readonly struct Landmark
    {
        internal Landmark(string name, int tick, int who, int other)
        {
            Name = name;
            Tick = tick;
            Who = who;
            Other = other;
        }

        /// <summary>Which landmark this is, as the committed table names it.</summary>
        public string Name { get; }

        /// <summary>The tick it happened on.</summary>
        public int Tick { get; }

        /// <summary>The entity it happened to: a creep, or a projectile.</summary>
        public int Who { get; }

        /// <summary>The other entity involved, or zero when there is only one.</summary>
        public int Other { get; }

        public override string ToString() =>
            Name
            + " at tick "
            + Tick.ToString(CultureInfo.InvariantCulture)
            + ", "
            + Who.ToString(CultureInfo.InvariantCulture)
            + (Other == 0 ? string.Empty : " and " + Other.ToString(CultureInfo.InvariantCulture));
    }
}
