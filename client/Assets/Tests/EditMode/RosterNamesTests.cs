using System.Linq;
using NUnit.Framework;
using Sim;
using View;
using View.Editor;

namespace Tests.EditMode
{
    /// <summary>
    /// What a unit is called on screen, held against the naming authority.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The nine names are written out here on purpose.</b> They are
    /// <c>docs/roster.md</c>'s index table, copied by hand, so that a change to
    /// the derivation in <see cref="RosterNames"/> is caught against what the
    /// design document says rather than against a second run of the same
    /// algorithm. That is the same shape as <c>Tests.Fixtures.ChosenArt</c>
    /// against the scene builder: two lists that must agree, and a disagreement
    /// that is a failure rather than a coincidence.
    /// </para>
    /// <para>
    /// It also pins what the derivation is <i>for</i>. Naming units is not an
    /// agent's to do — the roster is signed — so this test is what stops a
    /// display name being invented in the client.
    /// </para>
    /// </remarks>
    public class RosterNamesTests
    {
        /// <summary>
        /// Every live row of <c>content/units.txt</c> whose name
        /// <c>docs/roster.md</c>'s index has signed, by id.
        /// </summary>
        /// <remarks>
        /// A row on <see cref="UnboundUnits"/>'s list is not here. Those rows
        /// arrive in the simulation ahead of the art and the sign-off, and
        /// their names come off their labels by the same derivation as
        /// everything else — there is simply nothing yet to hold that
        /// derivation against.
        /// </remarks>
        private static readonly (int Id, string Name)[] TheRoster =
        {
            (1, "Minion"),
            (2, "Skeleton Scout"),
            (3, "Archer"),
            (4, "Mage"),
            (7, "Necromancer"),
            (11, "Soldier"),
            (12, "Skeleton"),
            (13, "Skeleton Warrior"),
            (14, "Ranger"),
        };

        [Test]
        public void EveryUnitIsCalledWhatTheRosterCallsIt()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            foreach (UnitType type in types.Types)
            {
                Assert.That(
                    TheRoster.Any(r => r.Id == type.Id) || UnboundUnits.Lists(type.Id),
                    Is.True,
                    "Unit " + type.Id + " (" + type.Label + ") was added and this table was not told. "
                    + "A row with no art yet is exempt for as long as it is on UnboundUnits' list.");
            }

            // ById throws on an id this table names and the shipped rows no
            // longer do, which is the retired half of the same check.
            foreach ((int id, string name) in TheRoster)
            {
                Assert.That(RosterNames.Of(types.ById(id)), Is.EqualTo(name));
            }
        }

        [Test]
        public void APriceReadsAsGold()
        {
            Assert.That(RosterNames.Gold(40), Is.EqualTo("40 gold"));
            Assert.That(RosterNames.Gold(0), Is.EqualTo("0 gold"));
        }

        [Test]
        public void ALabelWithNothingInItNamesNothing()
        {
            Assert.That(RosterNames.Of(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(RosterNames.Of((UnitType)null), Is.EqualTo(string.Empty));
        }
    }
}
