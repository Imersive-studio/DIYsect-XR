using DIYsect.Classroom;
using DIYsect.Interaction;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class SoundCueRulesTests
    {
        [Test]
        public void ChimeOnlyForAnAcceptedCorrectAnswer()
        {
            Assert.IsTrue(SoundCueRules.ShouldChime(QuizResult.Accepted, true));
            Assert.IsFalse(SoundCueRules.ShouldChime(QuizResult.Accepted, false));
            Assert.IsFalse(SoundCueRules.ShouldChime(QuizResult.WrongPhase, true));
            Assert.IsFalse(SoundCueRules.ShouldChime(QuizResult.AlreadyAnswered, true));
            Assert.IsFalse(SoundCueRules.ShouldChime(QuizResult.StaleRound, true));
        }

        [Test]
        public void ContinuousCutTicksAtMostEveryInterval()
        {
            var limiter = new CutHapticLimiter(SoundCueRules.CutMinIntervalSeconds);
            limiter.Begin(0f);
            int ticks = 0;
            float progress = 0f;
            // Cut progress reported every 0.02 s for 3 seconds.
            for (int i = 1; i <= 150; i++)
            {
                progress += 0.001f;
                if (limiter.ShouldPulse(progress, i * 0.02f))
                    ticks++;
            }
            Assert.LessOrEqual(ticks, 3.0f / SoundCueRules.CutMinIntervalSeconds + 1);
            Assert.GreaterOrEqual(ticks, 15);
        }

        [Test]
        public void NoTickWhenTheCutDidNotAdvance()
        {
            var limiter = new CutHapticLimiter(SoundCueRules.CutMinIntervalSeconds);
            limiter.Begin(0.5f);
            Assert.IsFalse(limiter.ShouldPulse(0.5f, 10f));
            Assert.IsFalse(limiter.ShouldPulse(0.4f, 20f));
            Assert.IsTrue(limiter.ShouldPulse(0.6f, 30f));
        }
    }
}
