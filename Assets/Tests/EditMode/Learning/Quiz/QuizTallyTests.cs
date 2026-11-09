using System.Collections.Generic;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizTallyTests
    {
        private const int Organs = 5;
        private const int Heart = 2;

        [Test]
        public void RightAndWrongAnswersAreCountedPerOrgan()
        {
            var tally = new QuizTally(Organs);
            Assert.AreEqual(QuizResult.Accepted, tally.Begin(Heart));

            Assert.AreEqual(QuizResult.Accepted, tally.Record(10, Heart, out bool first));
            Assert.AreEqual(QuizResult.Accepted, tally.Record(11, 4, out bool second));
            Assert.AreEqual(QuizResult.Accepted, tally.Record(12, 4, out _));

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(1, tally.Right);
            Assert.AreEqual(2, tally.Wrong);
            Assert.AreEqual(1, tally.CountFor(Heart));
            Assert.AreEqual(2, tally.CountFor(4));
            Assert.AreEqual(0, tally.CountFor(0));
        }

        [Test]
        public void EachClientCountsOncePerRound()
        {
            var tally = new QuizTally(Organs);
            tally.Begin(Heart);
            tally.Record(10, 4, out _);
            Assert.AreEqual(QuizResult.AlreadyAnswered, tally.Record(10, Heart, out bool correct));
            Assert.IsFalse(correct);
            Assert.AreEqual(0, tally.Right, "A second try must not turn a wrong answer into a right one.");
            Assert.AreEqual(1, tally.Wrong);
        }

        [Test]
        public void ANewRoundForgetsTheLastOne()
        {
            var tally = new QuizTally(Organs);
            tally.Begin(Heart);
            tally.Record(10, Heart, out _);
            tally.Begin(1);
            Assert.AreEqual(1, tally.Target);
            Assert.AreEqual(0, tally.Right);
            Assert.AreEqual(0, tally.CountFor(Heart));
            Assert.AreEqual(QuizResult.Accepted, tally.Record(10, 1, out bool correct), "The same client may answer the new round.");
            Assert.IsTrue(correct);
        }

        [Test]
        public void OrgansOutsideTheBodyAreRefused()
        {
            var tally = new QuizTally(Organs);
            Assert.AreEqual(QuizResult.BadOrgan, tally.Begin(-1));
            Assert.AreEqual(QuizResult.BadOrgan, tally.Begin(Organs));
            Assert.AreEqual(-1, tally.Target, "A refused start must not pick a target.");
            tally.Begin(Heart);
            Assert.AreEqual(QuizResult.BadOrgan, tally.Record(10, Organs, out _));
            Assert.AreEqual(QuizResult.Accepted, tally.Record(10, Heart, out _), "A refused answer must not use up the client's turn.");
        }

        [Test]
        public void TopWrongSortsByTapsAndLeavesOutTheTarget()
        {
            var taps = new[] { 1, 3, 9, 3, 0 };
            List<int> picks = QuizSummary.TopWrong(taps.Length, organ => taps[organ], Heart, 2);
            CollectionAssert.AreEqual(new[] { 1, 3 }, picks, "Ties keep organ order; the target (9 taps) is not a wrong pick.");
            CollectionAssert.AreEqual(new[] { 1, 3, 0 }, QuizSummary.TopWrong(taps.Length, organ => taps[organ], Heart, 10));
        }
    }
}
