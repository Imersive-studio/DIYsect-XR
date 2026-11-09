// Pure math, no scene. A tap places a pin; a drag orbits the camera and must never place one.
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
            _classifier = new TapDragClassifier(10f, 0.25f);
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
        public void HoldingLongerThanTheLimitIsADragEvenWithoutMoving()
        {
            _classifier.Begin(new Vector2(100f, 100f), 0f);
            Assert.IsFalse(_classifier.End(new Vector2(100f, 100f), 0.3f));
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
