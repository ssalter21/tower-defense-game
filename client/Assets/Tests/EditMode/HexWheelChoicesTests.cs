using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sim;
using View;

namespace Tests.EditMode
{
    public class HexWheelChoicesTests
    {
        private const int ArcherId = 3;

        private const int RangerId = 14;

        private const int OverwatchId = 31;

        private const int RootCount = 9;

        private const int ShortPurse = 35;

        [Test]
        public void AnEmptyHexOffersEveryRootByItsRosterName()
        {
            ComposedRound round = Opening(ShortPurse);

            IReadOnlyList<HexWheelWedge> wedges = HexWheelChoices.EveryRoot(round);

            Assert.That(wedges.Select(wedge => wedge.Word), Is.EqualTo(round.Palette.Select(RosterNames.Of)));
            Assert.That(wedges, Has.Count.EqualTo(RootCount));
            Assert.That(wedges.Where(wedge => wedge.Refused).Select(wedge => wedge.Type),
                Is.EqualTo(round.Palette.Where(root => !round.CanAfford(root))));
        }

        [Test]
        public void AffordableRootsLeaveOutWhatThePurseCannotCover()
        {
            ComposedRound round = Opening(ShortPurse);

            IReadOnlyList<HexWheelWedge> wedges = HexWheelChoices.AffordableRoots(round);

            Assert.That(wedges.Count, Is.GreaterThan(0).And.LessThan(RootCount));
            Assert.That(wedges.Select(wedge => wedge.Type), Is.EqualTo(round.Palette.Where(round.CanAfford)));
            Assert.That(wedges.Any(wedge => wedge.Refused), Is.False);
        }

        [Test]
        public void EachRootCarriesTheLadderAboveItOutward()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            ComposedRound round = Opening(ShortPurse, types);

            HexWheelWedge archer = HexWheelChoices
                .RootsWithLadders(round, StreamingContent.ReadUpgrades(types), types)
                .Single(wedge => wedge.Type.Id == ArcherId);

            Assert.That(archer.Outward.Select(rung => rung.Type.Id), Is.EqualTo(new[] { RangerId, OverwatchId }));
            Assert.That(archer.Outward.Last().Price, Is.EqualTo(RosterNames.CapstoneToken()));
        }

        [Test]
        public void AnArcherOnItsFirstRungOffersTheRangerAndAnUndesignedCapstonePlace()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            UpgradeLadder ladder = StreamingContent.ReadUpgrades(types);
            ComposedRound round = Opening(100, types);
            PlaceAnArcher(round, out int column, out int row);

            IReadOnlyList<HexWheelWedge> wedges = HexWheelChoices.Upgrades(round, ladder, column, row);

            Assert.That(wedges, Has.Count.EqualTo(2));
            Assert.That(wedges[0].Type.Id, Is.EqualTo(RangerId));
            Assert.That(wedges[1].Undesigned, Is.True);
            Assert.That(wedges[1].Word, Is.Empty);
            Assert.That(wedges[1].Price, Is.Empty);
        }

        [Test]
        public void TheEntranceWheelOffersOnlyCreepsThePurseCanSend()
        {
            ComposedRound round = Opening(ShortPurse);

            IReadOnlyList<HexWheelWedge> wedges = HexWheelChoices.Creeps(round);

            Assert.That(wedges, Is.Not.Empty);
            Assert.That(wedges.All(wedge => wedge.Type.Role == UnitRole.Moving && round.PriceOf(wedge.Type) <= round.Gold), Is.True);
        }

        [Test]
        public void NineRootsWithTheirLaddersReachFurtherThanNineRootsAlone()
        {
            UnitTypeTable types = StreamingContent.ReadUnitTypes();
            ComposedRound round = Opening(ShortPurse, types);

            float alone = HexWheel.OuterRadiusOf(HexWheelChoices.EveryRoot(round));
            float laddered = HexWheel.OuterRadiusOf(
                HexWheelChoices.RootsWithLadders(round, StreamingContent.ReadUpgrades(types), types));

            Assert.That(2f * UnityEngine.Mathf.PI * HexWheel.RootLabelRadius(RootCount) / RootCount,
                Is.GreaterThanOrEqualTo(HexWheel.LabelWidth - 0.01f));
            Assert.That(laddered, Is.EqualTo(alone + (2f * HexWheel.LadderRingDepth)));
        }

        private static ComposedRound Opening(int gold, UnitTypeTable types = null)
        {
            types ??= StreamingContent.ReadUnitTypes();
            Ruleset rules = StreamingContent.ReadRuleset();

            return new ComposedRound(
                wave: 1,
                WaveScript.Nothing,
                StreamingContent.ReadUpgrades(types),
                Purse.Holding(gold),
                0,
                CostTable.From(rules, types),
                types,
                StreamingContent.ReadMap(),
                Board.Empty);
        }

        private static void PlaceAnArcher(ComposedRound round, out int column, out int row)
        {
            for (row = 0; row < round.Map.Height; row++)
            {
                for (column = 0; column < round.Map.Width; column++)
                {
                    BuildAction place = BuildAction.Of(ActionKind.Place, ArcherId, column, row);

                    if (round.Allows(place))
                    {
                        round.Do(place);

                        return;
                    }
                }
            }

            column = 0;
            Assert.Fail("No cell on the shipped map takes an archer.");
        }
    }
}
