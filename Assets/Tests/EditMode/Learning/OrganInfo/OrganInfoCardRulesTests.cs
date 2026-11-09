using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class OrganInfoCardRulesTests
    {
        private const OrganInfoCardState Hidden = OrganInfoCardState.Hidden;
        private const OrganInfoCardState Pointed = OrganInfoCardState.Pointed;
        private const OrganInfoCardState Held = OrganInfoCardState.Held;
        private const OrganInfoCardState Both = OrganInfoCardState.HeldAndPointed;

        // Every (state, event) pair, so a change to the table shows up here.
        [TestCase(OrganInfoCardState.Hidden, OrganInfoCardEvent.PointerEntered, OrganInfoCardState.Pointed)]
        [TestCase(OrganInfoCardState.Hidden, OrganInfoCardEvent.PointerLeft, OrganInfoCardState.Hidden)]
        [TestCase(OrganInfoCardState.Hidden, OrganInfoCardEvent.Grabbed, OrganInfoCardState.Held)]
        [TestCase(OrganInfoCardState.Hidden, OrganInfoCardEvent.Released, OrganInfoCardState.Hidden)]
        [TestCase(OrganInfoCardState.Pointed, OrganInfoCardEvent.PointerEntered, OrganInfoCardState.Pointed)]
        [TestCase(OrganInfoCardState.Pointed, OrganInfoCardEvent.PointerLeft, OrganInfoCardState.Hidden)]
        [TestCase(OrganInfoCardState.Pointed, OrganInfoCardEvent.Grabbed, OrganInfoCardState.HeldAndPointed)]
        [TestCase(OrganInfoCardState.Pointed, OrganInfoCardEvent.Released, OrganInfoCardState.Pointed)]
        [TestCase(OrganInfoCardState.Held, OrganInfoCardEvent.PointerEntered, OrganInfoCardState.HeldAndPointed)]
        [TestCase(OrganInfoCardState.Held, OrganInfoCardEvent.PointerLeft, OrganInfoCardState.Held)]
        [TestCase(OrganInfoCardState.Held, OrganInfoCardEvent.Grabbed, OrganInfoCardState.Held)]
        [TestCase(OrganInfoCardState.Held, OrganInfoCardEvent.Released, OrganInfoCardState.Hidden)]
        [TestCase(OrganInfoCardState.HeldAndPointed, OrganInfoCardEvent.PointerEntered, OrganInfoCardState.HeldAndPointed)]
        [TestCase(OrganInfoCardState.HeldAndPointed, OrganInfoCardEvent.PointerLeft, OrganInfoCardState.Held)]
        [TestCase(OrganInfoCardState.HeldAndPointed, OrganInfoCardEvent.Grabbed, OrganInfoCardState.HeldAndPointed)]
        [TestCase(OrganInfoCardState.HeldAndPointed, OrganInfoCardEvent.Released, OrganInfoCardState.Pointed)]
        public void TheTableGivesTheNextState(OrganInfoCardState state, OrganInfoCardEvent cardEvent, OrganInfoCardState expected)
        {
            Assert.AreEqual(expected, OrganInfoCardRules.Apply(state, cardEvent).Next);
        }

        [Test]
        public void PickingUpAnOrganShowsTheCard()
        {
            OrganInfoCardStep step = OrganInfoCardRules.Apply(Hidden, OrganInfoCardEvent.Grabbed);

            Assert.AreEqual(OrganInfoCardEffect.Show, step.Effect);
        }

        [Test]
        public void PuttingAnOrganBackHidesTheCard()
        {
            OrganInfoCardStep step = OrganInfoCardRules.Apply(Held, OrganInfoCardEvent.Released);

            Assert.AreEqual(Hidden, step.Next);
            Assert.AreEqual(OrganInfoCardEffect.Hide, step.Effect);
        }

        [Test]
        public void APointerOnAHeldOrganKeepsTheCardAfterTheOrganIsPutBack()
        {
            OrganInfoCardStep step = OrganInfoCardRules.Apply(Both, OrganInfoCardEvent.Released);

            Assert.AreEqual(Pointed, step.Next);
            Assert.AreEqual(OrganInfoCardEffect.None, step.Effect);
        }

        [Test]
        public void ThePointerLeavingAHeldOrganKeepsTheCard()
        {
            OrganInfoCardStep step = OrganInfoCardRules.Apply(Both, OrganInfoCardEvent.PointerLeft);

            Assert.AreEqual(Held, step.Next);
            Assert.AreEqual(OrganInfoCardEffect.None, step.Effect);
        }

        [Test]
        public void ThePointerLeavingAnOrganThatIsNotHeldHidesTheCard()
        {
            Assert.AreEqual(OrganInfoCardEffect.Hide, OrganInfoCardRules.Apply(Pointed, OrganInfoCardEvent.PointerLeft).Effect);
        }

        [Test]
        public void TheEffectIsOnlyShowOrHideWhenVisibilityChanges()
        {
            foreach (OrganInfoCardState state in System.Enum.GetValues(typeof(OrganInfoCardState)))
            foreach (OrganInfoCardEvent cardEvent in System.Enum.GetValues(typeof(OrganInfoCardEvent)))
            {
                OrganInfoCardStep step = OrganInfoCardRules.Apply(state, cardEvent);
                bool was = OrganInfoCardRules.IsShown(state);
                bool now = OrganInfoCardRules.IsShown(step.Next);
                OrganInfoCardEffect expected = was == now ? OrganInfoCardEffect.None : (now ? OrganInfoCardEffect.Show : OrganInfoCardEffect.Hide);
                Assert.AreEqual(expected, step.Effect, $"{state} + {cardEvent}");
            }
        }

        [Test]
        public void AnUnknownStateOrEventChangesNothing()
        {
            OrganInfoCardStep badEvent = OrganInfoCardRules.Apply(Held, (OrganInfoCardEvent)99);
            OrganInfoCardStep badState = OrganInfoCardRules.Apply((OrganInfoCardState)99, OrganInfoCardEvent.Grabbed);

            Assert.AreEqual(Held, badEvent.Next);
            Assert.AreEqual(OrganInfoCardEffect.None, badEvent.Effect);
            Assert.AreEqual((OrganInfoCardState)99, badState.Next);
            Assert.AreEqual(OrganInfoCardEffect.None, badState.Effect);
        }

        [Test]
        public void OnlyHiddenIsNotShown()
        {
            Assert.IsFalse(OrganInfoCardRules.IsShown(Hidden));
            Assert.IsTrue(OrganInfoCardRules.IsShown(Pointed));
            Assert.IsTrue(OrganInfoCardRules.IsShown(Held));
            Assert.IsTrue(OrganInfoCardRules.IsShown(Both));
        }
    }
}
