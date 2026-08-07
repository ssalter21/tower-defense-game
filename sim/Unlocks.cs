using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What a run may field: every option it has taken, in the order it took
    /// them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Free to unlock and paid to buy.</b> Taking an option costs nothing and
    /// unlocks its creep for the rest of the run; fielding one is priced by the
    /// <see cref="CostTable"/> out of the one purse. What a player may send is
    /// therefore bounded by what they chose rather than by which wallet they
    /// remembered to save into.
    /// </para>
    /// <para>
    /// <b>Permanent, and a value.</b> Nothing takes an unlock away, and
    /// <see cref="With"/> returns a new set rather than moving this one, so a
    /// run's unlocks are a fold over its build phases and a test can assert on
    /// any intermediate without replaying anything.
    /// </para>
    /// <para>
    /// The takes are kept rather than reduced to a set of type ids, because two
    /// game changers can field one body: which one was taken is what
    /// <see cref="AnchorSchedule.BonusVsTag"/> needs and a type id cannot say.
    /// </para>
    /// </remarks>
    public sealed class Unlocks
    {
        private static readonly Unlocks Nothing = new Unlocks(new Option[0]);

        private readonly Option[] _taken;

        private Unlocks(Option[] taken)
        {
            _taken = taken;
        }

        /// <summary>A run that has taken nothing yet.</summary>
        public static Unlocks None => Nothing;

        /// <summary>Every option taken, in the order the build phases took them.</summary>
        public IReadOnlyList<Option> Taken => _taken;

        /// <summary>How many takes there have been. One per build phase.</summary>
        public int Count => _taken.Length;

        /// <summary>Whether this run may field that creep.</summary>
        public bool Has(int typeId)
        {
            for (int index = 0; index < _taken.Length; index++)
            {
                if (_taken[index].TypeId == typeId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The unit row behind an unlocked creep. A take carries the row it was
        /// drawn against, so nothing downstream re-resolves an id it was
        /// already told is good.
        /// </summary>
        public UnitType TypeOf(int typeId)
        {
            for (int index = 0; index < _taken.Length; index++)
            {
                if (_taken[index].TypeId == typeId)
                {
                    return _taken[index].Type;
                }
            }

            throw new SimulationException(
                "Type id "
                + typeId.ToString(CultureInfo.InvariantCulture)
                + " has no unit row among "
                + ToString()
                + ". Every unlock is an option and every option carries the row it was drawn from, so this "
                + "is a creep being priced out of a set that never took it.");
        }

        /// <summary>
        /// The game changer this run took that fields that creep, if it took
        /// one. What a prepared counter gets against it is the schedule's
        /// <see cref="AnchorSchedule.BonusVsTag"/>, which is keyed on the
        /// changer rather than on the body.
        /// </summary>
        public bool TryChangerFor(int typeId, out GameChanger? changer)
        {
            for (int index = 0; index < _taken.Length; index++)
            {
                if (_taken[index].TypeId == typeId && _taken[index].Changer is object)
                {
                    changer = _taken[index].Changer;
                    return true;
                }
            }

            changer = null;
            return false;
        }

        /// <summary>These unlocks plus one more take.</summary>
        public Unlocks With(Option taken)
        {
            if (taken is null)
            {
                throw new ArgumentNullException(nameof(taken));
            }

            var grown = new Option[_taken.Length + 1];

            for (int index = 0; index < _taken.Length; index++)
            {
                grown[index] = _taken[index];
            }

            grown[_taken.Length] = taken;

            return new Unlocks(grown);
        }

        public override string ToString() =>
            _taken.Length == 0
                ? "nothing unlocked"
                : _taken.Length.ToString(CultureInfo.InvariantCulture)
                    + " taken: "
                    + string.Join(", ", Array.ConvertAll(_taken, option => option.ToString()));
    }
}
