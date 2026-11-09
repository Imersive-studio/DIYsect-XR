using System.Collections.Generic;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class OrganInfoCardTrackerTests
    {
        private OrganInfoCardTracker _tracker;
        private List<(int index, OrganInfoCardState previous, OrganInfoCardState current)> _changes;

        [SetUp]
        public void CreateTracker()
        {
            _tracker = new OrganInfoCardTracker(4);
            _changes = new List<(int, OrganInfoCardState, OrganInfoCardState)>();
            _tracker.Changed += (index, previous, current) => _changes.Add((index, previous, current));
        }

        [Test]
        public void EveryOrganStartsHidden()
        {
            for (int i = 0; i < _tracker.Count; i++)
                Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(i));
        }

        [Test]
        public void PickUpThenPutBackShowsThenHidesTheCard()
        {
            _tracker.Handle(2, OrganInfoCardEvent.Grabbed);
            _tracker.Handle(2, OrganInfoCardEvent.Released);

            Assert.AreEqual(2, _changes.Count);
            Assert.AreEqual((2, OrganInfoCardState.Hidden, OrganInfoCardState.Held), _changes[0]);
            Assert.AreEqual((2, OrganInfoCardState.Held, OrganInfoCardState.Hidden), _changes[1]);
        }

        [Test]
        public void OneOrgansEventsDoNotTouchAnother()
        {
            _tracker.Handle(1, OrganInfoCardEvent.Grabbed);

            Assert.AreEqual(OrganInfoCardState.Held, _tracker.StateOf(1));
            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(0));
            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(2));
        }

        [Test]
        public void AnEventThatChangesNothingRaisesNoChange()
        {
            _tracker.Handle(0, OrganInfoCardEvent.Released);
            _tracker.Handle(0, OrganInfoCardEvent.PointerLeft);

            Assert.IsEmpty(_changes);
        }

        [Test]
        public void AnIndexOutOfRangeIsIgnored()
        {
            OrganInfoCardStep low = _tracker.Handle(-1, OrganInfoCardEvent.Grabbed);
            OrganInfoCardStep high = _tracker.Handle(4, OrganInfoCardEvent.Grabbed);

            Assert.AreEqual(OrganInfoCardEffect.None, low.Effect);
            Assert.AreEqual(OrganInfoCardEffect.None, high.Effect);
            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(-1));
            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(4));
            Assert.IsEmpty(_changes);
        }

        [Test]
        public void SyncingPointersShowsTheCardOfEachPointedOrgan()
        {
            _tracker.SyncPointers(new[] { 0, 3 });

            Assert.AreEqual(OrganInfoCardState.Pointed, _tracker.StateOf(0));
            Assert.AreEqual(OrganInfoCardState.Pointed, _tracker.StateOf(3));
            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(1));
        }

        [Test]
        public void SyncingPointersAgainMovesTheCard()
        {
            _tracker.SyncPointers(new[] { 0 });
            _tracker.SyncPointers(new[] { 1 });

            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(0));
            Assert.AreEqual(OrganInfoCardState.Pointed, _tracker.StateOf(1));
        }

        [Test]
        public void SyncingTheSamePointersTwiceRaisesNothingTheSecondTime()
        {
            _tracker.SyncPointers(new[] { 2 });
            int before = _changes.Count;
            _tracker.SyncPointers(new[] { 2 });

            Assert.AreEqual(before, _changes.Count);
        }

        [Test]
        public void SyncingNoPointersClearsAllPointedCards()
        {
            _tracker.SyncPointers(new[] { 0, 1 });
            _tracker.SyncPointers(new List<int>());

            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(0));
            Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(1));
        }

        [Test]
        public void SyncingPointersIgnoresIndexesOutOfRangeAndNull()
        {
            _tracker.SyncPointers(new[] { -1, 9 });
            _tracker.SyncPointers(null);

            for (int i = 0; i < _tracker.Count; i++)
                Assert.AreEqual(OrganInfoCardState.Hidden, _tracker.StateOf(i));
        }

        [Test]
        public void SyncingPointersNeverDropsAHold()
        {
            _tracker.Handle(1, OrganInfoCardEvent.Grabbed);
            _tracker.SyncPointers(new[] { 1 });
            Assert.AreEqual(OrganInfoCardState.HeldAndPointed, _tracker.StateOf(1));

            _tracker.SyncPointers(new int[0]);
            Assert.AreEqual(OrganInfoCardState.Held, _tracker.StateOf(1));
        }

        [Test]
        public void TheCardStaysWhenAHeldAndPointedOrganIsPutBack()
        {
            _tracker.Handle(0, OrganInfoCardEvent.Grabbed);
            _tracker.SyncPointers(new[] { 0 });
            _tracker.Handle(0, OrganInfoCardEvent.Released);

            Assert.AreEqual(OrganInfoCardState.Pointed, _tracker.StateOf(0));
        }

        [Test]
        public void ATrackerForNoOrgansIsHarmless()
        {
            var empty = new OrganInfoCardTracker(-3);

            Assert.AreEqual(0, empty.Count);
            empty.Handle(0, OrganInfoCardEvent.Grabbed);
            empty.SyncPointers(new[] { 0 });
            Assert.AreEqual(OrganInfoCardState.Hidden, empty.StateOf(0));
        }
    }
}
