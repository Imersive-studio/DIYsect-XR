using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizRuleTests
    {
        private static readonly QuizRule _rule = QuizRule.Default;

        [TestCase(QuizPhase.Idle, QuizEvent.Start, QuizPhase.Asking)]
        [TestCase(QuizPhase.Revealed, QuizEvent.Start, QuizPhase.Asking)]
        [TestCase(QuizPhase.Asking, QuizEvent.Answer, QuizPhase.Asking)]
        [TestCase(QuizPhase.Asking, QuizEvent.End, QuizPhase.Revealed)]
        [TestCase(QuizPhase.Revealed, QuizEvent.Close, QuizPhase.Idle)]
        public void AllowedMovesAreAccepted(QuizPhase from, QuizEvent quizEvent, QuizPhase expected)
        {
            Assert.AreEqual(QuizResult.Accepted, _rule.Evaluate(from, quizEvent, out QuizPhase next));
            Assert.AreEqual(expected, next);
        }

        [TestCase(QuizPhase.Idle, QuizEvent.Answer)]
        [TestCase(QuizPhase.Revealed, QuizEvent.Answer)]
        [TestCase(QuizPhase.Asking, QuizEvent.Start)]
        [TestCase(QuizPhase.Idle, QuizEvent.End)]
        [TestCase(QuizPhase.Revealed, QuizEvent.End)]
        [TestCase(QuizPhase.Idle, QuizEvent.Close)]
        [TestCase(QuizPhase.Asking, QuizEvent.Close)]
        public void OtherMovesAreRefusedAndKeepThePhase(QuizPhase from, QuizEvent quizEvent)
        {
            Assert.AreEqual(QuizResult.WrongPhase, _rule.Evaluate(from, quizEvent, out QuizPhase next));
            Assert.AreEqual(from, next);
        }

        [Test]
        public void AnEventWithNoRowIsUnknown()
        {
            var onlyStart = new QuizRule(new[] { new QuizRule.Transition(QuizPhase.Idle, QuizEvent.Start, QuizPhase.Asking) });
            Assert.AreEqual(QuizResult.Unknown, onlyStart.Evaluate(QuizPhase.Asking, QuizEvent.End, out QuizPhase next));
            Assert.AreEqual(QuizPhase.Asking, next);
        }

        [Test]
        public void AFullRoundWalksThroughEveryPhase()
        {
            QuizPhase phase = QuizPhase.Idle;
            foreach (QuizEvent step in new[] { QuizEvent.Start, QuizEvent.Answer, QuizEvent.End, QuizEvent.Start, QuizEvent.End, QuizEvent.Close })
            {
                QuizPhase before = phase;
                Assert.AreEqual(QuizResult.Accepted, _rule.Evaluate(before, step, out phase), $"{step} from {before}");
            }
            Assert.AreEqual(QuizPhase.Idle, phase);
        }
    }
}
