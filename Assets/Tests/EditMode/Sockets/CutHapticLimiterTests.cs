using DIYsect.Interaction;
using NUnit.Framework;

namespace DIYsect.Interaction.Tests
{
    public class CutHapticLimiterTests
    {
        private CutHapticLimiter _limiter;

        [SetUp]
        public void SetUp()
        {
            _limiter = new CutHapticLimiter(0.1f);
            _limiter.Begin(0f);
        }

        [Test]
        public void FirstAdvancePulses()
        {
            Assert.IsTrue(_limiter.ShouldPulse(0.1f, 5f));
        }

        [Test]
        public void NoAdvanceDoesNotPulse()
        {
            Assert.IsFalse(_limiter.ShouldPulse(0f, 5f));
        }

        [Test]
        public void ProgressGoingBackwardsDoesNotPulse()
        {
            _limiter.Begin(0.5f);
            Assert.IsFalse(_limiter.ShouldPulse(0.4f, 5f));
        }

        [Test]
        public void AdvanceInsideTheIntervalIsSkipped()
        {
            Assert.IsTrue(_limiter.ShouldPulse(0.1f, 5f));
            Assert.IsFalse(_limiter.ShouldPulse(0.2f, 5.05f));
        }

        [Test]
        public void AdvanceAfterTheIntervalPulsesAgain()
        {
            Assert.IsTrue(_limiter.ShouldPulse(0.1f, 5f));
            Assert.IsFalse(_limiter.ShouldPulse(0.2f, 5.05f));
            Assert.IsTrue(_limiter.ShouldPulse(0.3f, 5.1f));
        }

        [Test]
        public void SkippedAdvanceIsNotCountedTwice()
        {
            Assert.IsTrue(_limiter.ShouldPulse(0.1f, 5f));
            Assert.IsFalse(_limiter.ShouldPulse(0.2f, 5.05f));
            // Same progress again after the interval: nothing new happened, so no tick.
            Assert.IsFalse(_limiter.ShouldPulse(0.2f, 5.2f));
        }

        [Test]
        public void BeginStartsFreshSoTheNextAdvancePulsesImmediately()
        {
            Assert.IsTrue(_limiter.ShouldPulse(0.1f, 5f));
            _limiter.Begin(0.1f);
            Assert.IsTrue(_limiter.ShouldPulse(0.2f, 5.01f));
        }
    }
}
