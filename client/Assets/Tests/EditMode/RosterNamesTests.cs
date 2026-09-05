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
    /// <b>Every name is written out here on purpose.</b> They are
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
        /// <b>A row on <see cref="UnboundUnits"/>'s list is here too, and that
        /// is the point.</b> The twenty-three rungs the nine tower lines added
        /// arrive in the simulation ahead of their art, so they draw the
        /// stand-in — but their names were signed with the rest of the roster,
        /// and a name does not wait on a model. What the exemption in the walk
        /// below covers is a row whose name the index has not signed at all.
        /// </remarks>
        /// <remarks>
        /// <b>Id 7 is the one row here that is not what the index says, and it
        /// is deliberate.</b> <c>docs/roster.md</c> renamed it Skeleton Mage to
        /// free the name for the creep that should carry it, and
        /// <c>content/units.txt</c> still labels it <c>necromancer</c>: the
        /// relabel and the new Necromancer row have to land together, because
        /// two rows cannot share a label. This table follows the file until
        /// they do, so that it goes on being what a player would read on
        /// screen.
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
            (15, "Sergeant"),
            (16, "Shield Wall"),
            (17, "Barbarian"),
            (18, "Berserker"),
            (19, "Slam"),
            (20, "Paladin"),
            (21, "Templar"),
            (22, "Blessing"),
            (23, "Cleric"),
            (24, "Bishop"),
            (25, "Consecration"),
            (26, "Sorcerer"),
            (27, "Unravel"),
            (28, "Druid"),
            (29, "Elder"),
            (30, "Overgrowth"),
            (31, "Overwatch"),
            (32, "Rogue"),
            (33, "Cutthroat"),
            (34, "Fan of Knives"),
            (35, "Engineer"),
            (36, "Artificer"),
            (37, "Mortar"),
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
