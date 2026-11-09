using System.Collections.Generic;
using DIYsect.Dissection;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class ChecklistProgressTests
    {
        private const int Liver = 3;
        private const int Stomach = 4;

        private ChecklistProgress _progress;
        private List<ChecklistState> _changes;

        [SetUp]
        public void SetUp()
        {
            _progress = new ChecklistProgress();
            _changes = new List<ChecklistState>();
            _progress.Changed += state => _changes.Add(state);
        }

        private void OpenEverything()
        {
            _progress.ApplyStage(DissectionStage.OmentumLifted);
        }

        [Test]
        public void ItStartsAtTheFirstStepWithNoOrgan()
        {
            Assert.AreEqual(ChecklistState.TraceIncision, _progress.State);
            Assert.AreEqual(ChecklistProgress.NoOrgan, _progress.ActiveOrgan);
        }

        [Test]
        public void TheFullWalkThroughTicksEveryStepInOrder()
        {
            _progress.ApplyStage(DissectionStage.Incised);
            Assert.AreEqual(ChecklistState.OpenSkin, _progress.State);
            _progress.ApplyStage(DissectionStage.SkinOpen);
            Assert.AreEqual(ChecklistState.OpenRectus, _progress.State);
            _progress.ApplyStage(DissectionStage.RectusOpen);
            Assert.AreEqual(ChecklistState.LiftOmentum, _progress.State);
            _progress.ApplyStage(DissectionStage.OmentumLifted);
            Assert.AreEqual(ChecklistState.LiftOrgan, _progress.State);

            _progress.OnHolderChanged(Liver, true);
            Assert.AreEqual(ChecklistState.InspectOrgan, _progress.State);
            Assert.AreEqual(Liver, _progress.ActiveOrgan);

            _progress.OnHeldLongEnough(Liver);
            Assert.AreEqual(ChecklistState.PutOrganBack, _progress.State);

            _progress.OnHolderChanged(Liver, false);
            Assert.AreEqual(ChecklistState.PutOrganBack, _progress.State);

            _progress.OnSnapResolved(Liver, true);
            Assert.AreEqual(ChecklistState.Complete, _progress.State);
            Assert.AreEqual(ChecklistProgress.NoOrgan, _progress.ActiveOrgan);
        }

        [Test]
        public void ALateJoinerCatchesUpFromTheSharedStage()
        {
            _progress.ApplyStage(DissectionStage.RectusOpen);
            Assert.AreEqual(ChecklistState.LiftOmentum, _progress.State);
        }

        [Test]
        public void ALateJoinerAtTheLastStageIsWaitingForAnOrgan()
        {
            OpenEverything();
            Assert.AreEqual(ChecklistState.LiftOrgan, _progress.State);
        }

        [Test]
        public void ALateJoinerAtTheIncisionIsWaitingToOpenTheSkin()
        {
            _progress.ApplyStage(DissectionStage.Incised);
            Assert.AreEqual(ChecklistState.OpenSkin, _progress.State);
        }

        [Test]
        public void AStageJumpRaisesOneChangeWithTheFinalState()
        {
            OpenEverything();
            CollectionAssert.AreEqual(new[] { ChecklistState.LiftOrgan }, _changes);
        }

        [Test]
        public void ReportingTheSameStageAgainDoesNotMoveTheChecklistBack()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);
            _changes.Clear();

            _progress.ApplyStage(DissectionStage.OmentumLifted);
            _progress.ApplyStage(DissectionStage.SkinOpen);

            Assert.AreEqual(ChecklistState.InspectOrgan, _progress.State);
            Assert.IsEmpty(_changes);
        }

        [Test]
        public void ANewIntactBodyRestartsTheChecklist()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);

            _progress.ApplyStage(DissectionStage.Intact);

            Assert.AreEqual(ChecklistState.TraceIncision, _progress.State);
            Assert.AreEqual(ChecklistProgress.NoOrgan, _progress.ActiveOrgan);
        }

        [Test]
        public void AnIntactBodyAtTheStartChangesNothing()
        {
            _progress.ApplyStage(DissectionStage.Intact);
            Assert.IsEmpty(_changes);
        }

        [Test]
        public void GrabbingAnOrganBeforeTheOmentumIsLiftedIsNotAStep()
        {
            _progress.ApplyStage(DissectionStage.RectusOpen);
            _progress.OnHolderChanged(Liver, true);

            Assert.AreEqual(ChecklistState.LiftOmentum, _progress.State);
            Assert.AreEqual(ChecklistProgress.NoOrgan, _progress.ActiveOrgan);
        }

        [Test]
        public void LettingGoBeforeTheInspectionGoesBackAndFreesTheOrgan()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);

            _progress.OnHolderChanged(Liver, false);

            Assert.AreEqual(ChecklistState.LiftOrgan, _progress.State);
            Assert.AreEqual(ChecklistProgress.NoOrgan, _progress.ActiveOrgan);
        }

        [Test]
        public void AfterLettingGoAnotherOrganCanBeLifted()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);
            _progress.OnHolderChanged(Liver, false);

            _progress.OnHolderChanged(Stomach, true);

            Assert.AreEqual(ChecklistState.InspectOrgan, _progress.State);
            Assert.AreEqual(Stomach, _progress.ActiveOrgan);
        }

        [Test]
        public void OnlyTheLiftedOrganCanBeInspected()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);

            _progress.OnHeldLongEnough(Stomach);

            Assert.AreEqual(ChecklistState.InspectOrgan, _progress.State);
        }

        [Test]
        public void AnotherOrganBeingLetGoDoesNotUndoTheLiftedOne()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);

            _progress.OnHolderChanged(Stomach, false);

            Assert.AreEqual(ChecklistState.InspectOrgan, _progress.State);
        }

        [Test]
        public void OnlyAnAcceptedSnapOfTheLiftedOrganPutsItBack()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);
            _progress.OnHeldLongEnough(Liver);

            _progress.OnSnapResolved(Liver, false);
            Assert.AreEqual(ChecklistState.PutOrganBack, _progress.State);
            _progress.OnSnapResolved(Stomach, true);
            Assert.AreEqual(ChecklistState.PutOrganBack, _progress.State);
            _progress.OnSnapResolved(Liver, true);
            Assert.AreEqual(ChecklistState.Complete, _progress.State);
        }

        [Test]
        public void AnAcceptedSnapBeforeTheInspectionDoesNotTick()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);

            _progress.OnSnapResolved(Liver, true);

            Assert.AreEqual(ChecklistState.InspectOrgan, _progress.State);
        }

        [Test]
        public void TheSnapMayArriveBeforeTheReleaseAndStillCompletes()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);
            _progress.OnHeldLongEnough(Liver);

            _progress.OnSnapResolved(Liver, true);
            _progress.OnHolderChanged(Liver, false);

            Assert.AreEqual(ChecklistState.Complete, _progress.State);
        }

        [Test]
        public void TheReleaseMayArriveBeforeTheSnapAndStillCompletes()
        {
            OpenEverything();
            _progress.OnHolderChanged(Liver, true);
            _progress.OnHeldLongEnough(Liver);

            _progress.OnHolderChanged(Liver, false);
            _progress.OnSnapResolved(Liver, true);

            Assert.AreEqual(ChecklistState.Complete, _progress.State);
        }

        [Test]
        public void AnIgnoredEventRaisesNoChange()
        {
            _progress.OnHolderChanged(Liver, true);
            _progress.OnHeldLongEnough(Liver);
            _progress.OnSnapResolved(Liver, true);
            Assert.IsEmpty(_changes);
        }

        [Test]
        public void EachRealStepRaisesOneChange()
        {
            _progress.ApplyStage(DissectionStage.Incised);
            _progress.ApplyStage(DissectionStage.SkinOpen);
            CollectionAssert.AreEqual(new[] { ChecklistState.OpenSkin, ChecklistState.OpenRectus }, _changes);
        }

        [Test]
        public void ACustomRuleTableIsUsed()
        {
            var rule = new ChecklistRule(new[]
            {
                new ChecklistRule.Row(ChecklistState.TraceIncision, ChecklistEvent.IncisionTraced, ChecklistState.Complete, ChecklistResult.Advanced),
            });
            var progress = new ChecklistProgress(rule);

            progress.ApplyStage(DissectionStage.Incised);

            Assert.AreEqual(ChecklistState.Complete, progress.State);
        }
    }
}
