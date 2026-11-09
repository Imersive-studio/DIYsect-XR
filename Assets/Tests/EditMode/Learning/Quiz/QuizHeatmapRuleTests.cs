using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizHeatmapRuleTests
    {
        [Test]
        public void AnOrganNobodyTappedIsCold()
        {
            Assert.AreEqual(0f, QuizHeatmapRule.Heat(0, 10, 6));
        }

        [Test]
        public void NoAnswersAtAllMakesEveryOrganCold()
        {
            Assert.AreEqual(0f, QuizHeatmapRule.Heat(0, 0, 0));
            Assert.AreEqual(0f, QuizHeatmapRule.Heat(3, 0, 0), "Stale counts with a zero total must not glow.");
        }

        [Test]
        public void TheBusiestOrganIsHottest()
        {
            Assert.AreEqual(1f, QuizHeatmapRule.Heat(6, 10, 6));
        }

        [Test]
        public void TiedOrgansAreBothHottest()
        {
            Assert.AreEqual(1f, QuizHeatmapRule.Heat(4, 8, 4));
            Assert.AreEqual(QuizHeatmapRule.Heat(4, 8, 4), QuizHeatmapRule.Heat(4, 8, 4));
        }

        [Test]
        public void ALonelyTapStillShowsButStaysBelowTheBusiestOrgan()
        {
            float lonely = QuizHeatmapRule.Heat(1, 11, 10);
            Assert.GreaterOrEqual(lonely, QuizHeatmapRule.MinTappedHeat);
            Assert.Less(lonely, 1f);
        }

        [Test]
        public void MoreTapsAreNeverCooler()
        {
            float previous = 0f;
            for (int taps = 1; taps <= 9; taps++)
            {
                float heat = QuizHeatmapRule.Heat(taps, 20, 9);
                Assert.GreaterOrEqual(heat, previous);
                previous = heat;
            }
        }

        [Test]
        public void ASingleAnswerIsTheHottestAndNeverAboveOne()
        {
            Assert.AreEqual(1f, QuizHeatmapRule.Heat(1, 1, 1));
            Assert.AreEqual(1f, QuizHeatmapRule.Heat(5, 2, 9), "A maximum above the total is capped, and taps never exceed 1.");
        }

        [Test]
        public void StrengthRunsFromNothingToSoftToHotWithoutAJump()
        {
            const float soft = 0.16f;
            const float hot = 0.8f;
            Assert.AreEqual(0f, QuizHeatmapRule.Strength(0f, soft, hot));
            Assert.AreEqual(soft, QuizHeatmapRule.Strength(QuizHeatmapRule.MinTappedHeat, soft, hot), 1e-5f);
            Assert.AreEqual(hot, QuizHeatmapRule.Strength(1f, soft, hot), 1e-5f);
            Assert.Less(QuizHeatmapRule.Strength(0.01f, soft, hot), soft);
        }

        [Test]
        public void ChipTextCountsAnswers()
        {
            Assert.AreEqual(string.Empty, QuizHeatmapRule.ChipText(0));
            Assert.AreEqual("1 answer", QuizHeatmapRule.ChipText(1));
            Assert.AreEqual("12 answers", QuizHeatmapRule.ChipText(12));
        }
    }
}
