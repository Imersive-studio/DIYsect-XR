using System;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class ChecklistRuleTests
    {
        private static readonly ChecklistRule Rule = ChecklistRule.Default;

        [Test]
        public void TheWalkThroughAdvancesOneStepPerEvent()
        {
            var steps = new[]
            {
                (ChecklistEvent.IncisionTraced, ChecklistState.OpenSkin),
                (ChecklistEvent.SkinOpened, ChecklistState.OpenRectus),
                (ChecklistEvent.RectusOpened, ChecklistState.LiftOmentum),
                (ChecklistEvent.OmentumLifted, ChecklistState.LiftOrgan),
                (ChecklistEvent.OrganLifted, ChecklistState.InspectOrgan),
                (ChecklistEvent.OrganInspected, ChecklistState.PutOrganBack),
                (ChecklistEvent.OrganPutBack, ChecklistState.Complete),
            };
            ChecklistState state = ChecklistState.TraceIncision;
            foreach ((ChecklistEvent trigger, ChecklistState expected) in steps)
            {
                Assert.AreEqual(ChecklistResult.Advanced, Rule.Evaluate(state, trigger, out ChecklistState next), trigger.ToString());
                Assert.AreEqual(expected, next, trigger.ToString());
                state = next;
            }
        }

        [Test]
        public void AnEventOutOfOrderIsIgnored()
        {
            Assert.AreEqual(ChecklistResult.Ignored, Rule.Evaluate(ChecklistState.TraceIncision, ChecklistEvent.RectusOpened, out ChecklistState next));
            Assert.AreEqual(ChecklistState.TraceIncision, next);
            Assert.AreEqual(ChecklistResult.Ignored, Rule.Evaluate(ChecklistState.OpenRectus, ChecklistEvent.OrganLifted, out next));
            Assert.AreEqual(ChecklistState.OpenRectus, next);
        }

        [Test]
        public void AnEventForAStepAlreadyDoneIsIgnored()
        {
            Assert.AreEqual(ChecklistResult.Ignored, Rule.Evaluate(ChecklistState.LiftOrgan, ChecklistEvent.SkinOpened, out ChecklistState next));
            Assert.AreEqual(ChecklistState.LiftOrgan, next);
        }

        [Test]
        public void DroppingTheOrganBeforeItIsInspectedGoesBackToLiftingAnOrgan()
        {
            Assert.AreEqual(ChecklistResult.Reverted, Rule.Evaluate(ChecklistState.InspectOrgan, ChecklistEvent.OrganDropped, out ChecklistState next));
            Assert.AreEqual(ChecklistState.LiftOrgan, next);
        }

        [Test]
        public void DroppingTheOrganAfterItIsInspectedChangesNothing()
        {
            Assert.AreEqual(ChecklistResult.Ignored, Rule.Evaluate(ChecklistState.PutOrganBack, ChecklistEvent.OrganDropped, out ChecklistState next));
            Assert.AreEqual(ChecklistState.PutOrganBack, next);
        }

        [Test]
        public void ANewBodyRestartsTheChecklistFromAnyLaterStep()
        {
            for (ChecklistState state = ChecklistState.OpenSkin; state <= ChecklistState.Complete; state++)
            {
                Assert.AreEqual(ChecklistResult.Reset, Rule.Evaluate(state, ChecklistEvent.BodyReset, out ChecklistState next), state.ToString());
                Assert.AreEqual(ChecklistState.TraceIncision, next, state.ToString());
            }
        }

        [Test]
        public void ANewBodyAtTheStartChangesNothing()
        {
            Assert.AreEqual(ChecklistResult.Ignored, Rule.Evaluate(ChecklistState.TraceIncision, ChecklistEvent.BodyReset, out ChecklistState next));
            Assert.AreEqual(ChecklistState.TraceIncision, next);
        }

        [Test]
        public void ACompleteChecklistOnlyLeavesByReset()
        {
            foreach (ChecklistEvent trigger in Enum.GetValues(typeof(ChecklistEvent)))
            {
                ChecklistResult result = Rule.Evaluate(ChecklistState.Complete, trigger, out ChecklistState next);
                if (trigger == ChecklistEvent.BodyReset)
                {
                    Assert.AreEqual(ChecklistResult.Reset, result);
                    Assert.AreEqual(ChecklistState.TraceIncision, next);
                }
                else
                {
                    Assert.AreEqual(ChecklistResult.Ignored, result, trigger.ToString());
                    Assert.AreEqual(ChecklistState.Complete, next, trigger.ToString());
                }
            }
        }

        [Test]
        public void ATableWithTheSameRowTwiceIsRejected()
        {
            var row = new ChecklistRule.Row(ChecklistState.TraceIncision, ChecklistEvent.IncisionTraced, ChecklistState.OpenSkin, ChecklistResult.Advanced);
            Assert.Throws<ArgumentException>(() => new ChecklistRule(new[] { row, row }));
        }

        [Test]
        public void ATableCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ChecklistRule(null));
        }
    }
}
