using System.Collections.Generic;
using NUnit.Framework;

namespace DIYsect.Presentation.Tests
{
    public class RevealTests
    {
        [TestCase(RevealPhase.Hidden, RevealEvent.Show, RevealPhase.Entering)]
        [TestCase(RevealPhase.Hidden, RevealEvent.Hide, RevealPhase.Hidden)]
        [TestCase(RevealPhase.Hidden, RevealEvent.Finished, RevealPhase.Hidden)]
        [TestCase(RevealPhase.Entering, RevealEvent.Show, RevealPhase.Entering)]
        [TestCase(RevealPhase.Entering, RevealEvent.Hide, RevealPhase.Exiting)]
        [TestCase(RevealPhase.Entering, RevealEvent.Finished, RevealPhase.Shown)]
        [TestCase(RevealPhase.Shown, RevealEvent.Show, RevealPhase.Shown)]
        [TestCase(RevealPhase.Shown, RevealEvent.Hide, RevealPhase.Exiting)]
        [TestCase(RevealPhase.Shown, RevealEvent.Finished, RevealPhase.Shown)]
        [TestCase(RevealPhase.Exiting, RevealEvent.Show, RevealPhase.Entering)]
        [TestCase(RevealPhase.Exiting, RevealEvent.Hide, RevealPhase.Exiting)]
        [TestCase(RevealPhase.Exiting, RevealEvent.Finished, RevealPhase.Hidden)]
        public void RuleTableCoversEveryPhaseAndEvent(RevealPhase phase, RevealEvent change, RevealPhase expected)
        {
            Assert.AreEqual(expected, RevealRule.Next(phase, change));
        }

        [Test]
        public void ShowRunsForTheInDurationThenSettles()
        {
            var timeline = new RevealTimeline(0.2f, 0.1f);
            var phases = new List<RevealPhase>();
            timeline.PhaseChanged += phases.Add;

            timeline.Show();
            timeline.Tick(0.1f);
            Assert.AreEqual(0.5f, timeline.Amount, 1e-4f);
            Assert.IsTrue(timeline.IsAnimating);
            timeline.Tick(0.1f);

            Assert.AreEqual(RevealPhase.Shown, timeline.Phase);
            Assert.AreEqual(1f, timeline.Amount);
            CollectionAssert.AreEqual(new[] { RevealPhase.Entering, RevealPhase.Shown }, phases);
        }

        [Test]
        public void HidingHalfWayTurnsAroundFromTheSameAmount()
        {
            var timeline = new RevealTimeline(0.2f, 0.1f);
            timeline.Show();
            timeline.Tick(0.1f);

            timeline.Hide();
            Assert.AreEqual(RevealPhase.Exiting, timeline.Phase);
            Assert.AreEqual(0.5f, timeline.Amount, 1e-4f, "No jump when the direction flips.");
            timeline.Tick(0.05f);

            Assert.AreEqual(RevealPhase.Hidden, timeline.Phase);
            Assert.AreEqual(0f, timeline.Amount);
            Assert.IsFalse(timeline.IsVisible);
        }

        [Test]
        public void SnapSkipsTheAnimation()
        {
            var timeline = new RevealTimeline(0.2f, 0.1f);
            timeline.Snap(true);
            Assert.AreEqual(RevealPhase.Shown, timeline.Phase);
            Assert.AreEqual(1f, timeline.Amount);
            timeline.Snap(false);
            Assert.AreEqual(RevealPhase.Hidden, timeline.Phase);
        }

        [Test]
        public void TickDoesNothingWhileHiddenOrShown()
        {
            var timeline = new RevealTimeline(0.2f, 0.1f);
            timeline.Tick(1f);
            Assert.AreEqual(RevealPhase.Hidden, timeline.Phase);
            timeline.Snap(true);
            timeline.Tick(1f);
            Assert.AreEqual(1f, timeline.Amount);
        }

        [Test]
        public void EasesStartAtZeroAndEndAtOne()
        {
            foreach (System.Func<float, float> ease in new System.Func<float, float>[] { Ease.OutCubic, Ease.InCubic, t => Ease.OutBack(t) })
            {
                Assert.AreEqual(0f, ease(0f), 1e-5f);
                Assert.AreEqual(1f, ease(1f), 1e-5f);
            }
            Assert.Greater(Ease.OutCubic(0.5f), 0.5f, "Out curves are past half way at half time.");
            Assert.Greater(Ease.OutBack(0.8f), 1f, "The spring overshoots before it settles.");
        }

        [Test]
        public void TimingsSitInsideTheDesignTable()
        {
            Assert.That(MotionSpec.LabelIn, Is.InRange(0.18f, 0.22f));
            Assert.That(MotionSpec.LabelOut, Is.InRange(0.18f, 0.22f));
            Assert.That(MotionSpec.LeaderIn, Is.InRange(0.20f, 0.28f));
            Assert.That(MotionSpec.LeaderOut, Is.InRange(0.20f, 0.28f));
            Assert.That(MotionSpec.TitleIn, Is.InRange(0.25f, 0.35f));
            Assert.That(MotionSpec.TitleOut, Is.InRange(0.25f, 0.35f));
            Assert.That(MotionSpec.InstructionIn, Is.InRange(0.22f, 0.28f));
            Assert.That(MotionSpec.InstructionOut, Is.InRange(0.22f, 0.28f));
            Assert.AreEqual(0.25f, MotionSpec.CheckIn, 1e-5f);
        }
    }
}
