// Pure math, no scene. A quick tap shows an organ name, a hold places a pin, a drag orbits the camera and must never do either.
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class TapDragClassifierTests
    {
        private TapDragClassifier _classifier;

        [SetUp]
        public void Create()
        {
            _classifier = new TapDragClassifier(10f, 0.5f);
        }

        [Test]
        public void QuickPressWithoutMovingIsATap()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            Assert.IsTrue(_classifier.End(new Vector2(100f, 100f), 0.1f));
        }

        [Test]
        public void SmallWobbleStillCountsAsATap()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            Assert.AreEqual(Vector2.zero, _classifier.Move(new Vector2(105f, 104f), 0.05f));
            Assert.IsTrue(_classifier.End(new Vector2(105f, 104f), 0.1f));
        }

        [Test]
        public void MovingMoreThanTheThresholdIsADragAndReportsTheDistanceSoFar()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            Vector2 delta = _classifier.Move(new Vector2(130f, 100f), 0.05f);
            Assert.IsTrue(_classifier.IsDrag);
            Assert.AreEqual(new Vector2(30f, 0f), delta);
            Assert.AreEqual(new Vector2(5f, 0f), _classifier.Move(new Vector2(135f, 100f), 0.07f));
            Assert.IsFalse(_classifier.End(new Vector2(135f, 100f), 0.1f));
        }

        [Test]
        public void DraggingBackToTheStartIsStillNotATap()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            _classifier.Move(new Vector2(200f, 100f), 0.05f);
            Assert.IsFalse(_classifier.End(new Vector2(100f, 100f), 0.1f));
        }

        [Test]
        public void ASlowTapUnderTheHoldTimeIsStillATap()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            _classifier.Move(new Vector2(100f, 100f), 0.4f);
            Assert.IsFalse(_classifier.HoldReached);
            Assert.IsFalse(_classifier.TakeHold());
            Assert.IsTrue(_classifier.End(new Vector2(100f, 100f), 0.45f));
        }

        [Test]
        public void StayingPutForTheHoldTimeIsAHoldReportedOnceAndNotATap()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            Assert.AreEqual(Vector2.zero, _classifier.Move(new Vector2(102f, 100f), 0.5f));
            Assert.IsTrue(_classifier.HoldReached);
            Assert.IsTrue(_classifier.TakeHold());
            Assert.IsFalse(_classifier.TakeHold(), "A hold is reported once.");
            Assert.IsFalse(_classifier.IsDrag);
            Assert.IsFalse(_classifier.End(new Vector2(102f, 100f), 0.9f), "A hold is not also a tap.");
            Assert.IsFalse(_classifier.TakeHold());
        }

        [Test]
        public void AHoldNoticedOnlyAtReleaseIsStillReportedOnce()
        {
            _classifier.Begin(Vector2.zero, 0f);
            Assert.IsFalse(_classifier.End(Vector2.zero, 0.7f));
            Assert.IsTrue(_classifier.TakeHold());
            Assert.IsFalse(_classifier.TakeHold());
        }

        [Test]
        public void MovingBeforeTheHoldTimeIsADragAndNeverAHold()
        {
            _classifier.Begin(Vector2.zero, 0f);
            _classifier.Move(new Vector2(40f, 0f), 0.2f);
            _classifier.Move(new Vector2(40f, 0f), 0.8f);
            Assert.IsTrue(_classifier.IsDrag);
            Assert.IsFalse(_classifier.TakeHold());
            Assert.IsFalse(_classifier.End(new Vector2(40f, 0f), 0.9f));
        }

        [Test]
        public void AHoldThatThenMovesBecomesADragSoTheCameraOrbits()
        {
            _classifier.Begin(Vector2.zero, 0f);
            _classifier.Move(Vector2.zero, 0.6f);
            Assert.IsTrue(_classifier.TakeHold());
            Vector2 delta = _classifier.Move(new Vector2(30f, 0f), 0.7f);
            Assert.IsTrue(_classifier.IsDrag);
            Assert.AreEqual(new Vector2(30f, 0f), delta);
            Assert.IsFalse(_classifier.End(new Vector2(30f, 0f), 0.8f));
        }

        [Test]
        public void ASecondFingerCancelsAHoldBeforeItIsReported()
        {
            _classifier.Begin(Vector2.zero, 0f);
            _classifier.MarkNotATap();
            _classifier.Move(Vector2.zero, 0.9f);
            Assert.IsFalse(_classifier.TakeHold());
        }

        [Test]
        public void ANewPressForgetsTheLastHold()
        {
            _classifier.Begin(Vector2.zero, 0f);
            _classifier.Move(Vector2.zero, 0.6f);
            _classifier.End(Vector2.zero, 0.7f);
            _classifier.Begin(Vector2.zero, 5f);
            Assert.IsFalse(_classifier.HoldReached);
            Assert.IsTrue(_classifier.End(Vector2.zero, 5.1f));
        }

        [Test]
        public void SecondFingerMakesItNotATap()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            _classifier.MarkNotATap();
            Assert.IsFalse(_classifier.End(new Vector2(100f, 100f), 0.1f));
        }

        [Test]
        public void ThresholdIsInclusiveOfExactlyTheLimit()
        {
            _classifier.Begin(Vector2.zero, 0f);
            _classifier.Move(new Vector2(10f, 0f), 0.05f);
            Assert.IsTrue(_classifier.End(new Vector2(10f, 0f), 0.1f), "Exactly 10 px is not more than 10 px.");
        }

        [Test]
        public void CancelledPressReportsNoTapAndCanBeRestarted()
        {
            _classifier.Begin(Vector2.zero, 0f);
            _classifier.Cancel();
            Assert.IsFalse(_classifier.End(Vector2.zero, 0.1f));
            _classifier.Begin(Vector2.zero, 1f);
            Assert.IsTrue(_classifier.End(Vector2.zero, 1.1f));
        }
    }
}
