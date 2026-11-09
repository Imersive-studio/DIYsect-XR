using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizSummaryTests
    {
        [Test]
        public void WrongPicksAreSortedByTapsThenOrganIndexAndSkipTheTarget()
        {
            int[] taps = { 0, 2, 5, 2, 1 };
            var picks = QuizSummary.TopWrong(taps.Length, i => taps[i], 2, 10);
            CollectionAssert.AreEqual(new[] { 1, 3, 4 }, picks);
        }

        [Test]
        public void TheListIsCutToTheMaximum()
        {
            int[] taps = { 3, 2, 1 };
            CollectionAssert.AreEqual(new[] { 0, 1 }, QuizSummary.TopWrong(taps.Length, i => taps[i], -1, 2));
            CollectionAssert.IsEmpty(QuizSummary.TopWrong(taps.Length, i => taps[i], -1, 0));
        }
    }
}
