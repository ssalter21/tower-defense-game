using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Sim;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The wave bar: a row that grows as it is filled, a list that offers only
    /// what may go in the box it belongs to, and boxes dragged into the order
    /// their creeps arrive in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every legality assertion here is really an assertion about who
    /// decided.</b> These tests do not check that a wave nobody can afford is
    /// refused, or that a creep may fill only one slot — <c>sim</c> has tests
    /// for both. They check that the screen's answer is the same answer, because
    /// it came from the same call. Prevention that drifted from refusal is the
    /// failure ADR-0051 is built to make impossible and it would be invisible
    /// from either side alone.
    /// </para>
    /// <para>
    /// <b>Driven through the screen and not around it.</b> A wave is composed by
    /// opening a box's list and taking something out of it, and rearranged by
    /// grabbing a box at a point in the panel and letting go somewhere else, so
    /// the offering, the composed phase and the drag's own arithmetic are all in
    /// the path. The one thing simulated by hand is the device, which is the
    /// same split <see cref="BuildingTests"/> and the camera rig's tests use.
    /// </para>
    /// </remarks>
    public class WaveTests : ViewTest
    {
        /// <summary>Walking rows of <c>content/units.txt</c>, named so the tests read.</summary>
        private const int MinionId = 1;

        private const int SkeletonScoutId = 2;

        private const int NecromancerId = 7;

        private const int SkeletonId = 12;

        private const int SkeletonWarriorId = 13;

        /// <summary>The committed archer, which is what these tests spend on the board.</summary>
        private const int ArcherId = 3;

        /// <summary>What an Archer climbs into: the one edge the committed ladder carries.</summary>
        private const int RangerId = 14;

        /// <summary>A cell of ground with nothing on it, well clear of the corridor.</summary>
        private const int FreeColumn = 7;

        private const int FreeRow = 0;

        /// <summary>
        /// The row opens as one empty box and nothing else. There is no width to
        /// draw — #179 deleted the schedule that used to say two, then three,
        /// then four — so what the bar shows at the start is the one place a
        /// creep can go.
        /// </summary>
        [Test]
        public void TheRowOpensAsASingleEmptyBoxCarryingAPlusSign()
        {
            MatchRoot root = Building(Opening());

            Assert.That(root.Composing.Slots, Is.Empty);
            Assert.That(root.Wave.Boxes.Count, Is.EqualTo(1));
            Assert.That(root.Wave.Boxes[0].Q<Label>("Plus").text, Is.EqualTo("+"));
        }

        /// <summary>
        /// A round that carries creeps opens with them already in the row. A
        /// creep is bought once and attacks every round after (#207), so the
        /// build phase a later round opens on is not the empty one — it is the
        /// wave the last round sent, ready to be added to and rearranged.
        /// </summary>
        [Test]
        public void TheRowOpensHoldingWhatTheRunAlreadyCarries()
        {
            MatchRoot root = Building(Opening(carried: Carrying(MinionId, 2, SkeletonId, 1)));

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { MinionId, SkeletonId }),
                "The carried creeps opened the row, in the order they were last sent in.");

            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(2));
            Assert.That(root.Wave.Boxes.Count, Is.EqualTo(3), "Two carried and the trailing empty one.");

            // And none of it was charged again: the purse the round opens on is
            // the purse it still has.
            Assert.That(root.Composing.Gold, Is.EqualTo(100), "A carried creep is not paid for twice.");
        }

        /// <summary>
        /// A carried box lowers to what is carried and no further, and it cannot
        /// be emptied at all. There is no selling a creep back, so the screen
        /// does not offer the verb rather than letting the simulation refuse it
        /// — ADR-0051's prevention, applied to #207's floor.
        /// </summary>
        [Test]
        public void ACarriedCreepIsAFloorTheBoxDoesNotLowerPast()
        {
            MatchRoot root = Building(Opening(carried: Carrying(MinionId, 2)));

            // One more on top of the two carried, then straight back off again.
            root.Wave.Open(0);
            root.Wave.More();
            root.Wave.Open(0);
            root.Wave.Fewer();

            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(2), "Down to the floor.");
            Assert.That(root.Composing.CanSendFewer(0), Is.False, "And no further.");

            // So the bar does not offer the verb at all, which is the whole of
            // what prevention means here.
            root.Wave.Open(0);

            Assert.That(
                Wording(root.Wave.Choices),
                Does.Not.Contain("One fewer"),
                "A box at its floor is not offered a way down.");

            // And reaching for it anyway leaves the wave alone rather than
            // throwing out of the simulation.
            root.Wave.Open(0);
            root.Wave.Fewer();

            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(2));
            Assert.That(root.Composing.Slots.Count, Is.EqualTo(1), "The box is still in the row.");

            // And it cannot be taken out from underneath either.
            root.Composing.SendNone(0);

            Assert.That(root.Composing.Slots.Count, Is.EqualTo(1), "A carried box cannot be emptied.");
        }

        /// <summary>
        /// Filling the last box appends a new empty one behind it, and the row
        /// keeps growing for as long as the purse reaches. Nothing bounds it:
        /// <c>sim/BuildPhase.cs</c> says nothing bounds how many slots a wave
        /// carries, and a fixed row here would be that gate coming back in
        /// through the interface.
        /// </summary>
        [Test]
        public void FillingTheLastBoxAppendsAnotherAndTheRowKeepsGrowing()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, MinionId);

            Assert.That(root.Composing.Slots.Count, Is.EqualTo(1));
            Assert.That(root.Wave.Boxes.Count, Is.EqualTo(2), "One filled box and one empty one.");

            Send(root, 1, SkeletonScoutId);
            Send(root, 2, SkeletonId);
            Send(root, 3, NecromancerId);
            Send(root, 4, SkeletonWarriorId);

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { MinionId, SkeletonScoutId, SkeletonId, NecromancerId, SkeletonWarriorId }),
                "Five creep types in one round, which no width in the old rules would have allowed at "
                + "wave one.");

            Assert.That(root.Wave.Boxes.Count, Is.EqualTo(6), "And there is still an empty one behind.");
            Assert.That(root.Composing.Gold, Is.EqualTo(14), "A hundred, less 10, 9, 17, 19 and 31.");
        }

        /// <summary>
        /// A box says a name and a count. No type ids on screen, and none of the
        /// record's vocabulary — see ADR-0051, #196 and #197.
        /// </summary>
        [Test]
        public void ABoxIsANameAndACount()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, SkeletonWarriorId);

            Assert.That(Wording(root.Wave.Boxes[0]), Does.Contain("Skeleton Warrior"));
            Assert.That(Wording(root.Wave.Boxes[0]), Does.Contain("x1"));

            root.Wave.Open(0);
            root.Wave.More();

            Assert.That(Wording(root.Wave.Boxes[0]), Does.Contain("x2"));

            Assert.That(
                Wording(root.Wave.Boxes[0]),
                Does.Not.Contain("gold"),
                "A box is a creep and a count. What it costs is said where the buying is decided, which "
                + "is the box's own list.");

            foreach (VisualElement box in root.Wave.Boxes)
            {
                string wording = Wording(box).ToLowerInvariant();

                Assert.That(wording, Does.Not.Contain("ordinary"));
                Assert.That(wording, Does.Not.Contain("game changer"));
                Assert.That(wording, Does.Not.Contain("type"));
                Assert.That(wording, Does.Not.Contain("slot"));
            }
        }

        /// <summary>
        /// <b>Prevention, not refusal.</b> A creep another box already sends is
        /// not in the list, because <c>BuildPhase.Resolve</c> throws on a
        /// duplicate; the screen does not restate that rule, it simply never
        /// offers the second box.
        /// </summary>
        [Test]
        public void ACreepAlreadyInTheRowIsNotOfferedASecondBox()
        {
            MatchRoot root = Building(Opening());

            Assert.That(
                Ids(root.Composing.Sendable(0)),
                Is.EqualTo(new[]
                {
                    SkeletonScoutId, MinionId, SkeletonId, NecromancerId, SkeletonWarriorId,
                }),
                "Every walking row, cheapest first. There are no unlocks: what a wave may carry is the "
                + "roster, and the only question left is price.");

            Send(root, 0, MinionId);

            Assert.That(
                Ids(root.Composing.Sendable(1)),
                Has.No.Member(MinionId),
                "A repeat is spelled by raising that box's count, never by a second box.");

            Assert.That(root.Composing.Sendable(1).Count, Is.EqualTo(4));
        }

        /// <summary>
        /// The other half of prevention: what the purse cannot cover is not on
        /// the list. One wallet buys both halves of a phase, so this is the same
        /// gold the towers spend from.
        /// </summary>
        [Test]
        public void ACreepThePurseCannotCoverIsNotOffered()
        {
            MatchRoot root = Building(Opening(gold: 18));

            Assert.That(
                Ids(root.Composing.Sendable(0)),
                Is.EqualTo(new[] { SkeletonScoutId, MinionId, SkeletonId }),
                "A Necromancer is 19 and a Skeleton Warrior 31.");

            Send(root, 0, SkeletonId);

            Assert.That(root.Composing.Gold, Is.EqualTo(1));
            Assert.That(root.Composing.Sendable(1), Is.Empty, "Nothing on the roster costs one gold.");
            Assert.That(root.Composing.CanSendMore(0), Is.False);

            // The box with nothing legal left in it opens nothing at all.
            root.Wave.Open(1);

            Assert.That(root.Wave.IsListing, Is.False);
        }

        /// <summary>
        /// <b>The whole wave is bought from the same purse as the towers.</b> A
        /// phase whose towers ate its wave is not composable, so what a box
        /// offers is priced against what the board has already spent — and the
        /// other way round, a hex stops lighting once the wave has eaten the
        /// gold.
        /// </summary>
        [Test]
        public void TheWaveAndTheTowersSpendOnePurse()
        {
            MatchRoot root = Building(Opening(gold: 50));

            Assert.That(root.Composing.Sendable(0).Count, Is.EqualTo(5), "The whole roster, at 50 gold.");

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Composing.Gold, Is.EqualTo(10), "Fifty, less an Archer.");
            Assert.That(
                Ids(root.Composing.Sendable(0)),
                Is.EqualTo(new[] { SkeletonScoutId, MinionId }),
                "The tower was paid for first, so the wave is priced against what it left.");

            Send(root, 0, MinionId);

            Assert.That(root.Composing.Gold, Is.EqualTo(0));
            Assert.That(
                root.Composing.Allows(BuildAction.Of(ActionKind.Place, ArcherId, 5, 0)),
                Is.False,
                "And a wave that has eaten the purse is a hex that no longer lights.");
        }

        /// <summary>
        /// A repeat raises the box's count rather than opening a second box, and
        /// the raise is offered only where the purse reaches it.
        /// </summary>
        [Test]
        public void ARepeatIsSpelledByRaisingTheBoxsCount()
        {
            MatchRoot root = Building(Opening(gold: 25));

            Send(root, 0, MinionId);
            root.Wave.Open(0);
            root.Wave.More();

            Assert.That(root.Composing.Slots.Count, Is.EqualTo(1), "Still one box.");
            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(2));
            Assert.That(root.Composing.Gold, Is.EqualTo(5));
            Assert.That(root.Wave.IsListing, Is.False, "The list goes away once something is taken.");

            root.Wave.Open(0);

            Assert.That(root.Composing.CanSendMore(0), Is.False, "A third Minion is 10 out of 5.");
            Assert.That(Wording(root.Wave.Choices), Does.Not.Contain("One more"));
        }

        /// <summary>
        /// Emptying a box takes it out of the row and closes the gap behind it.
        /// The rules model an empty slot and skip it; the bar never produces one.
        /// </summary>
        [Test]
        public void EmptyingABoxTakesItOutAndClosesTheGap()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, MinionId);
            Send(root, 1, SkeletonScoutId);
            Send(root, 2, SkeletonId);

            root.Wave.Open(1);
            root.Wave.Fewer();

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { MinionId, SkeletonId }),
                "The Skeleton closed up behind the Minion rather than leaving a hole.");

            Assert.That(root.Wave.Boxes.Count, Is.EqualTo(3), "Two filled and the trailing empty one.");

            foreach (WaveSlot slot in root.Composing.Slots)
            {
                Assert.That(slot.IsEmpty, Is.False, "A composed wave never carries an empty slot.");
            }

            Assert.That(root.Composing.Gold, Is.EqualTo(73), "A hundred, less a Minion and a Skeleton.");

            // And the creep that was taken out is offerable again.
            Assert.That(Ids(root.Composing.Sendable(2)), Has.Member(SkeletonScoutId));
        }

        /// <summary>
        /// Lowering a count of more than one leaves the box where it is. Only
        /// the last one out removes it.
        /// </summary>
        [Test]
        public void LoweringACountOfMoreThanOneLeavesTheBoxWhereItIs()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, MinionId);
            root.Wave.Open(0);
            root.Wave.More();
            root.Wave.Open(0);
            root.Wave.Fewer();

            Assert.That(root.Composing.Slots.Count, Is.EqualTo(1));
            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(1));

            root.Wave.Open(0);
            root.Wave.Fewer();

            Assert.That(root.Composing.Slots, Is.Empty);
            Assert.That(root.Wave.Boxes.Count, Is.EqualTo(1), "Back to one empty box.");
        }

        /// <summary>
        /// <b>The ticket's "done when", end to end.</b> A wave is composed
        /// through the bar, a box is dragged to the front with a pointer, and
        /// the <see cref="BuildPhase"/> that comes out carries the slots in the
        /// dragged order — which <c>BuildPhase.Resolve</c> turns into the order
        /// the creeps walk out in.
        /// </summary>
        /// <remarks>
        /// A frame has to pass first. The drag's arithmetic compares a pointer
        /// against where the boxes actually are, and a runtime panel lays out
        /// when it is updated — so a drag driven before the first update would
        /// be comparing against a row of zero-width boxes at the origin and
        /// would pass whatever it was asked.
        /// </remarks>
        [UnityTest]
        public IEnumerator DraggingABoxToTheFrontSendsThatCreepFirst()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, SkeletonScoutId);
            Send(root, 1, MinionId);
            Send(root, 2, SkeletonId);

            yield return null;
            yield return null;

            Assert.That(
                MiddleOf(root, 0), Is.LessThan(MiddleOf(root, 2)), "The row is laid out left to right.");

            // Grab the third box and carry it past the first one's middle.
            root.Wave.Grab(2, MiddleOf(root, 2));
            root.Wave.Drag(MiddleOf(root, 0) - 1f);

            Assert.That(root.Wave.IsDragging, Is.True);
            Assert.That(root.Wave.DraggingTo, Is.EqualTo(0), "A drop here lands at the front.");

            root.Wave.Release();

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { SkeletonId, SkeletonScoutId, MinionId }),
                "Position is arrival order, so the box dragged to the front sends that creep first.");

            Assert.That(root.Wave.IsDragging, Is.False);

            // The composed phase is what a commit would hand to Run.Advance, and
            // resolving it is what turns those positions into release ticks.
            WaveScript wave = Resolved(root.Composing.Phase).Wave;


            Assert.That(wave.Count, Is.EqualTo(3));
            Assert.That(wave.Orders[0].TypeId, Is.EqualTo(SkeletonId));
            Assert.That(wave.Orders[1].TypeId, Is.EqualTo(SkeletonScoutId));
            Assert.That(wave.Orders[2].TypeId, Is.EqualTo(MinionId));
            Assert.That(wave.Orders[0].TickOffset, Is.LessThan(wave.Orders[1].TickOffset));
            Assert.That(wave.Orders[1].TickOffset, Is.LessThan(wave.Orders[2].TickOffset));
        }

        /// <summary>
        /// A carried box drags like any other. #207's "done when" says the
        /// accumulated wave can be dragged into any order in <b>any</b> build
        /// phase — so the creeps an earlier round paid for are a floor under the
        /// count and not a pin on the position.
        /// </summary>
        /// <remarks>
        /// The sim side of this is covered by
        /// <c>The_whole_carried_wave_is_reordered_by_a_later_round...</c>. This
        /// is the clause the ticket actually wrote down, which names the drag:
        /// the carried box is grabbed with a pointer and dropped at the front.
        /// </remarks>
        [UnityTest]
        public IEnumerator ACarriedBoxIsDraggedLikeAnyOther()
        {
            MatchRoot root = Building(Opening(carried: Carrying(MinionId, 2, SkeletonId, 1)));

            yield return null;
            yield return null;

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { MinionId, SkeletonId }),
                "Both boxes are carried, in the order they were last sent.");

            // The second carried box, dragged in front of the first.
            root.Wave.Grab(1, MiddleOf(root, 1));
            root.Wave.Drag(MiddleOf(root, 0) - 1f);

            Assert.That(root.Wave.DraggingTo, Is.EqualTo(0), "A drop here lands at the front.");

            root.Wave.Release();

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { SkeletonId, MinionId }),
                "A creep bought in an earlier round still chooses where in the column it walks.");

            // Rearranging buys nothing, so it costs nothing — and the wave that
            // comes out still carries every creep, at the counts it carried.
            Assert.That(root.Composing.Gold, Is.EqualTo(100), "Reordering is free.");
            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(1));
            Assert.That(root.Composing.Slots[1].Count, Is.EqualTo(2));
        }

        /// <summary>
        /// A box dropped between two others lands between them, dragged either
        /// way.
        /// </summary>
        /// <remarks>
        /// <b>The two directions are not one case.</b>
        /// <see cref="ComposedRound.Rearrange"/> takes the box out of the row
        /// before putting it back, so what it is handed is a position in a row
        /// one shorter — and taking a box out shifts everything after it down by
        /// one and nothing before it. A landing that named the index of the last
        /// box it passed rather than counting the boxes it passed was therefore
        /// right dragging rightwards and one place too far left dragging
        /// leftwards, and a test that only ever dropped at the front could not
        /// see it: the front is the one position both readings agree on.
        /// </remarks>
        [UnityTest]
        public IEnumerator ABoxDroppedBetweenTwoOthersLandsBetweenThem()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, MinionId);
            Send(root, 1, SkeletonScoutId);
            Send(root, 2, SkeletonId);

            yield return null;
            yield return null;

            // Leftwards: the third box, dropped between the first two.
            root.Wave.Grab(2, MiddleOf(root, 2));
            root.Wave.Drag(Between(root, 0, 1));

            Assert.That(root.Wave.DraggingTo, Is.EqualTo(1));

            root.Wave.Release();

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { MinionId, SkeletonId, SkeletonScoutId }),
                "Between the first two, not in front of them.");

            yield return null;
            yield return null;

            // Rightwards: the first box, dropped between the other two.
            root.Wave.Grab(0, MiddleOf(root, 0));
            root.Wave.Drag(Between(root, 1, 2));

            Assert.That(root.Wave.DraggingTo, Is.EqualTo(1));

            root.Wave.Release();

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { SkeletonId, MinionId, SkeletonScoutId }));

            yield return null;
            yield return null;

            // And a press that goes nowhere leaves the row exactly as it was.
            root.Wave.Grab(1, MiddleOf(root, 1));
            root.Wave.Drag(MiddleOf(root, 1));
            root.Wave.Release();

            Assert.That(Sent(root), Is.EqualTo(new[] { SkeletonId, MinionId, SkeletonScoutId }));
        }

        /// <summary>
        /// <b>An open list does not outlive the purse it was priced against.</b>
        /// One wallet buys both halves of a phase, so a tower bought while a box
        /// is open can make what that box is offering unaffordable — and
        /// ADR-0051 says illegality is prevented and never refused, so an
        /// affordance that survived the answer it was built from is the one way
        /// this screen could hand <c>BuildPhase.Resolve</c> something it throws
        /// on.
        /// </summary>
        [Test]
        public void ABoardClickPutsAnOpenListAway()
        {
            MatchRoot root = Building(Opening(gold: 60));

            root.Wave.Open(0);

            Assert.That(root.Wave.IsListing, Is.True);
            Assert.That(
                Ids(root.Composing.Sendable(0)),
                Has.Member(SkeletonWarriorId),
                "A Skeleton Warrior is 31 out of 60.");

            Select(root, ArcherId);

            Assert.That(root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow)), Is.True);
            Assert.That(root.Composing.Gold, Is.EqualTo(20), "Sixty, less an Archer.");
            Assert.That(root.Wave.IsListing, Is.False, "And the list it priced went with it.");

            // Which is also the whole of the dismissal: there is no other way to
            // put a box's list away without taking something out of it.
            root.Wave.Choose(Types().ById(SkeletonWarriorId));

            Assert.That(root.Composing.Slots, Is.Empty, "A closed list offers nothing.");
        }

        /// <summary>
        /// The same, for the route that never touches the board: taking a rung
        /// of a tower's ladder spends from the same purse.
        /// </summary>
        [Test]
        public void TakingAnUpgradePutsAnOpenListAway()
        {
            MatchRoot root = Building(Opening(gold: 120));

            Select(root, ArcherId);
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));
            root.Pointer.Click(ScreenPointOf(root, FreeColumn, FreeRow));

            Assert.That(root.Palette.IsOffering, Is.True);

            root.Wave.Open(0);

            Assert.That(root.Wave.IsListing, Is.True);

            root.Palette.Take(Types().ById(RangerId));

            Assert.That(root.Composing.Gold, Is.EqualTo(40), "A hundred and twenty, less 40 and 40.");
            Assert.That(root.Wave.IsListing, Is.False);
        }

        /// <summary>
        /// A box that is not in the row is refused rather than clamped. Every
        /// affordance on screen counts the row first, so an index naming nothing
        /// means the screen counted it wrong — which wants a stack trace, not a
        /// wave quietly composed at the wrong position.
        /// </summary>
        [Test]
        public void ABoxThatIsNotInTheRowIsRefused()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, MinionId);

            UnitType scout = Types().ById(SkeletonScoutId);

            Assert.That(
                () => root.Composing.Send(1, scout),
                Throws.Nothing,
                "One past the end is the trailing empty box, and it appends.");

            Assert.That(() => root.Composing.Send(9, scout), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => root.Composing.Send(-1, scout), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => root.Composing.SendMore(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => root.Composing.SendFewer(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => root.Composing.SendNone(2), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// A press that never travelled is a click, and a click opens the box's
        /// list. Same element, two verbs, told apart by distance — without the
        /// slack every click would land as a rearrangement nobody asked for.
        /// </summary>
        [UnityTest]
        public IEnumerator APressThatDidNotTravelIsAClickAndOpensTheList()
        {
            MatchRoot root = Building(Opening());

            Send(root, 0, MinionId);
            Send(root, 1, SkeletonScoutId);

            yield return null;
            yield return null;

            root.Wave.Grab(1, MiddleOf(root, 1));
            root.Wave.Drag(MiddleOf(root, 1) + (WaveBar.DragSlack * 0.5f));
            root.Wave.Release();

            Assert.That(Sent(root), Is.EqualTo(new[] { MinionId, SkeletonScoutId }), "Nothing moved.");
            Assert.That(root.Wave.IsListing, Is.True);
            Assert.That(root.Wave.ListingAt, Is.EqualTo(1));

            // And the list a filled box opens leads with what to do about the
            // count, then offers what else could go there.
            Assert.That(Wording(root.Wave.Choices), Does.Contain("One more"));
            Assert.That(Wording(root.Wave.Choices), Does.Contain("One fewer"));
            Assert.That(Wording(root.Wave.Choices), Does.Not.Contain("Skeleton Scout"), "It is in the box.");
            Assert.That(
                Wording(root.Wave.Choices),
                Does.Not.Contain("Minion"),
                "And the Minion is in the box in front of it, which is the duplicate rule preventing "
                + "rather than refusing.");

            Assert.That(Wording(root.Wave.Choices), Does.Contain("Necromancer   19 gold"));
        }

        /// <summary>
        /// <b>No input reaches the simulation.</b> Composing a wave appends to a
        /// phase held in a local; the run is untouched until somebody commits,
        /// which is a thing nothing on this screen can do yet.
        /// </summary>
        [Test]
        public void NothingComposedReachesTheRun()
        {
            MatchRoot root = Playfield();
            UnitTypeTable types = Types();

            var run = new Run(
                root.Map,
                StreamingContent.ReadRuleset(),
                types,
                StreamingContent.ReadUpgrades(types),
                FieldPool.Canned(StreamingContent.ReadDefense(types), StreamingContent.ReadWave(types)),
                TheMatchOnScreen.Seed);

            root.BeginBuilding(ComposedRound.For(run), TheMatchOnScreen.Art());

            Send(root, 0, MinionId);
            root.Wave.Open(0);
            root.Wave.More();

            Assert.That(root.Composing.Slots.Count, Is.EqualTo(1));
            Assert.That(run.Purse.Gold, Is.EqualTo(100), "The run's purse has not moved.");
            Assert.That(run.Round, Is.EqualTo(0), "Nor has its round.");
            Assert.That(run.Sent, Is.Empty, "And nothing has been sent.");
        }

        /// <summary>
        /// The round after a real one opens holding what that round sent. This
        /// is #207's "the wave bar opens each build phase already holding what
        /// you carry", driven through <see cref="ComposedRound.For"/> off a run
        /// that has actually played a round — rather than a hand-built wave
        /// handed to the constructor.
        /// </summary>
        [Test]
        public void TheRoundAfterOneThatSentSomethingOpensHoldingIt()
        {
            MatchRoot root = Playfield();
            UnitTypeTable types = Types();

            var run = new Run(
                root.Map,
                StreamingContent.ReadRuleset(),
                types,
                StreamingContent.ReadUpgrades(types),
                FieldPool.Canned(StreamingContent.ReadDefense(types), StreamingContent.ReadField(types)),
                TheMatchOnScreen.Seed);

            // Round one, composed and committed through the screen's own phase.
            root.BeginBuilding(ComposedRound.For(run), TheMatchOnScreen.Art());

            Send(root, 0, MinionId);
            root.Wave.Open(0);
            root.Wave.More();

            int gold = root.Composing.Gold;

            run.Advance(root.Composing.Phase);

            Assert.That(run.Round, Is.EqualTo(1));
            Assert.That(run.Sent[0].Wave.CountOf(MinionId), Is.EqualTo(2), "Round one sent two Minions.");

            // Round two, opened off the same run. The first round's chrome comes
            // down first, exactly as the run loop takes it down to watch.
            root.EndBuilding();
            root.BeginBuilding(ComposedRound.For(run), TheMatchOnScreen.Art());

            Assert.That(
                Sent(root),
                Is.EqualTo(new[] { MinionId }),
                "The row opened holding what round one sent.");

            Assert.That(root.Composing.Slots[0].Count, Is.EqualTo(2));
            Assert.That(root.Composing.Wave, Is.EqualTo(2));

            // And they are not charged again: the round opens on the run's own
            // purse, with the whole of it still there.
            Assert.That(root.Composing.Gold, Is.EqualTo(run.Purse.Gold), "Nothing was re-bought.");
            Assert.That(
                root.Composing.Gold,
                Is.GreaterThan(gold),
                "Round two is richer than round one was after paying for those Minions.");

            // The floor came with them: neither Minion can be taken back off.
            Assert.That(root.Composing.CanSendFewer(0), Is.False);
        }

        /// <summary>
        /// A click on the wave row stops at the wave row. Which way up a screen
        /// point is read is the one thing here a reasonable person could get
        /// backwards, and getting it backwards is invisible: the box would still
        /// open its list and the tower would <i>also</i> land on whatever hex was
        /// behind the bar.
        /// </summary>
        /// <remarks>
        /// Both ends are asserted, because an assertion only that the top of the
        /// screen is clear passes just as well when the panel was never laid out
        /// at all. This row sits above the palette, so it pins a different band
        /// of the screen than <see cref="BuildingTests"/> does.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheWaveRowSwallowsTheClicksThatLandOnIt()
        {
            MatchRoot root = Building(Opening());

            yield return null;
            yield return null;

            // The wave row sits directly on the palette, which sits on the
            // bottom edge, on a panel laid out 1080 high and scaled to the
            // window's height.
            float scale = Screen.height / 1080f;
            float middle = TowerPalette.BarHeight + (WaveBar.BarHeight * 0.5f);
            var onTheRow = new Vector2(Screen.width * 0.5f, middle * scale);
            var overTheBoard = new Vector2(Screen.width * 0.5f, Screen.height * 0.8f);

            Assert.That(root.Wave.Covers(onTheRow), Is.True, "A point on the wave row.");
            Assert.That(root.Wave.Covers(overTheBoard), Is.False, "A point well above it.");

            Select(root, ArcherId);

            Assert.That(root.Pointer.Click(onTheRow), Is.False);
            Assert.That(
                root.Composing.Phase.Actions.Count,
                Is.EqualTo(0),
                "A click on the row must not also land on the board behind it.");

            root.Pointer.Point(onTheRow);

            Assert.That(root.Building.IsLit, Is.False);
        }

        // ---------------------------------------------------------------
        // Scaffolding
        // ---------------------------------------------------------------

        private MatchRoot Playfield() =>
            Spawn(SceneFraming.RootObjectName).AddComponent<MatchRoot>();

        private MatchRoot Building(ComposedRound round)
        {
            MatchRoot root = Playfield();

            root.BeginBuilding(round, TheMatchOnScreen.Art());

            return root;
        }

        /// <summary>
        /// A round opening on an empty board with as much gold as the caller
        /// says, priced and laddered out of the shipped content.
        /// </summary>
        private static ComposedRound Opening(int gold = 100, WaveScript carried = null)
        {
            UnitTypeTable types = Types();
            Ruleset rules = StreamingContent.ReadRuleset();

            return new ComposedRound(
                wave: 1,
                carried ?? WaveScript.Nothing,
                StreamingContent.ReadUpgrades(types),
                Purse.Holding(gold),
                CostTable.From(rules, types),
                types,
                StreamingContent.ReadMap(),
                Board.Empty);
        }

        private static UnitTypeTable Types() => StreamingContent.ReadUnitTypes();

        /// <summary>
        /// A wave a round carries into its build phase, spelled as the pairs it
        /// holds: type id, then count, and so on.
        /// </summary>
        /// <remarks>
        /// Parsed from the wave text rather than composed through a build phase,
        /// so that what a round carries into these tests is written out here and
        /// not derived by the code under test.
        /// </remarks>
        private static WaveScript Carrying(params int[] pairs)
        {
            var text = new System.Text.StringBuilder();
            int tick = 0;

            for (int index = 0; index < pairs.Length; index += 2)
            {
                text.AppendLine("order  " + tick + "  " + pairs[index] + "  " + pairs[index + 1] + "  0");
                tick += pairs[index + 1];
            }

            return WaveScript.Parse("carried", text.ToString(), Types());
        }

        /// <summary>
        /// What the composed phase would come to, resolved against the same
        /// content the round was opened on.
        /// </summary>
        private static Build Resolved(BuildPhase phase)
        {
            UnitTypeTable types = Types();

            return phase.Resolve(
                1,
                WaveScript.Nothing,
                StreamingContent.ReadUpgrades(types),
                Purse.Holding(100),
                CostTable.From(StreamingContent.ReadRuleset(), types),
                types,
                StreamingContent.ReadMap(),
                Board.Empty);
        }

        /// <summary>
        /// Puts a creep in a box the way a hand does: open the box's list, and
        /// take the creep out of it. Fails where the list did not offer it,
        /// which is what makes every call to this an assertion about prevention.
        /// </summary>
        private static void Send(MatchRoot root, int index, int typeId)
        {
            root.Wave.Open(index);

            Assert.That(root.Wave.IsListing, Is.True, "No list opened on box " + index);

            foreach (UnitType creep in root.Composing.Sendable(index))
            {
                if (creep.Id == typeId)
                {
                    root.Wave.Choose(creep);

                    return;
                }
            }

            Assert.Fail("Box " + index + " does not offer type id " + typeId);
        }

        private static void Select(MatchRoot root, int typeId)
        {
            for (int index = 0; index < root.Composing.Palette.Count; index++)
            {
                if (root.Composing.Palette[index].Id == typeId)
                {
                    root.Pointer.Shortcut(index);

                    return;
                }
            }

            Assert.Fail("No palette entry for type id " + typeId);
        }

        private static Vector2 ScreenPointOf(MatchRoot root, int column, int row) =>
            root.CameraRig.Camera.WorldToScreenPoint(HexGeometry.ToWorld(column, row));

        /// <summary>Where a box's middle is, in the panel's own coordinates.</summary>
        private static float MiddleOf(MatchRoot root, int index) =>
            root.Wave.Boxes[index].worldBound.center.x;

        /// <summary>
        /// A point halfway between two boxes' middles: past the first and not
        /// yet past the second, which is what "dropped between them" means.
        /// </summary>
        private static float Between(MatchRoot root, int left, int right) =>
            (MiddleOf(root, left) + MiddleOf(root, right)) * 0.5f;

        /// <summary>The creep in each filled box, in the order the row shows them.</summary>
        private static int[] Sent(MatchRoot root)
        {
            IReadOnlyList<WaveSlot> slots = root.Composing.Slots;
            var ids = new int[slots.Count];

            for (int index = 0; index < ids.Length; index++)
            {
                ids[index] = slots[index].TypeId;
            }

            return ids;
        }

        private static int[] Ids(IReadOnlyList<UnitType> types)
        {
            var ids = new int[types.Count];

            for (int index = 0; index < ids.Length; index++)
            {
                ids[index] = types[index].Id;
            }

            return ids;
        }

        private static string Wording(VisualElement element)
        {
            var words = new StringBuilder();

            foreach (Label label in element.Query<Label>().ToList())
            {
                words.Append(label.text).Append(' ');
            }

            return words.ToString();
        }

        private static string Wording(IReadOnlyList<Button> buttons)
        {
            var words = new StringBuilder();

            foreach (Button button in buttons)
            {
                words.Append(button.text).Append(" | ");
            }

            return words.ToString();
        }
    }
}
