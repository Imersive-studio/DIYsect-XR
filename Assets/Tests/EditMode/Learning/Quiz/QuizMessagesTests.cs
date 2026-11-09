using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizMessagesTests
    {
        [Test]
        public void TheHeadsetLineSaysWhatToDoUntilTheVerdictArrives()
        {
            Assert.AreEqual(QuizMessages.HeadsetHint, QuizMessages.HeadsetStatus(QuizPhase.Asking, false, false, ""));
            Assert.AreEqual(QuizMessages.HeadsetMissed, QuizMessages.HeadsetStatus(QuizPhase.Asking, false, true, null));
            Assert.AreEqual(QuizMessages.AnswerSent, QuizMessages.HeadsetStatus(QuizPhase.Asking, true, false, ""));
            Assert.AreEqual("Right! That is the liver.", QuizMessages.HeadsetStatus(QuizPhase.Asking, true, false, "Right! That is the liver."));
            Assert.AreEqual("Not quite.", QuizMessages.HeadsetStatus(QuizPhase.Revealed, true, false, "Not quite."), "The verdict stays after the reveal.");
            Assert.AreEqual("", QuizMessages.HeadsetStatus(QuizPhase.Revealed, false, false, ""), "Nothing to do once the round is over.");
            Assert.AreEqual("", QuizMessages.HeadsetStatus(QuizPhase.Idle, false, false, ""));
        }

        [Test]
        public void PromptNamesTheOrganInASentence()
        {
            Assert.AreEqual("Quiz: tap the left kidney", QuizMessages.Prompt("Left kidney"));
            Assert.AreEqual("Quiz: tap the organ", QuizMessages.Prompt(""));
        }

        [Test]
        public void AnswerFeedbackSaysWhetherItWasRight()
        {
            Assert.AreEqual("Right! That is the liver.", QuizMessages.ForAnswer(QuizResult.Accepted, true, "Liver"));
            Assert.AreEqual("Not quite. That is not the liver.", QuizMessages.ForAnswer(QuizResult.Accepted, false, "Liver"));
        }

        [Test]
        public void EveryResultHasReadableText()
        {
            foreach (QuizResult result in System.Enum.GetValues(typeof(QuizResult)))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(QuizMessages.ForAnswer(result, false, "Liver")), $"ForAnswer({result})");
                if (result != QuizResult.Accepted)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(QuizMessages.ForRequest(result)), $"ForRequest({result})");
            }
            Assert.AreEqual(string.Empty, QuizMessages.ForRequest(QuizResult.Accepted));
        }

        [Test]
        public void ProfessorPanelListsCountsAndWrongPicks()
        {
            string text = QuizMessages.ForProfessor(QuizPhase.Asking, "Liver", 3, 2, new[] { ("Spleen", 2) });
            StringAssert.Contains("tap the liver", text);
            StringAssert.Contains("3 right, 2 wrong", text);
            StringAssert.Contains("Spleen: 2", text);
            StringAssert.Contains("Press Start", QuizMessages.ForProfessor(QuizPhase.Idle, "Liver", 0, 0, null));
        }

        [Test]
        public void QuestionTextReplacesTheOrganSentenceForClassmates()
        {
            Assert.AreEqual("Quiz: Tap the organ that stores bile", QuizMessages.Prompt("Gallbladder", "Tap the organ that stores bile"));
            Assert.AreEqual("Quiz: tap the gallbladder", QuizMessages.Prompt("Gallbladder", ""));
            Assert.AreEqual("Quiz: tap the gallbladder", QuizMessages.Prompt("Gallbladder", null));
        }

        [Test]
        public void ProfessorSeesTheQuestionAndTheAnswer()
        {
            string asking = QuizMessages.ForProfessor(QuizPhase.Asking, "Gallbladder", 1, 0, null, "Tap the organ that stores bile");
            StringAssert.Contains("Asking: Tap the organ that stores bile", asking);
            StringAssert.Contains("Answer: Gallbladder", asking);
            StringAssert.Contains("1 right, 0 wrong", asking);

            StringAssert.Contains("Answer: Gallbladder", QuizMessages.ForProfessor(QuizPhase.Revealed, "Gallbladder", 1, 0, null, "Tap the organ that stores bile"));
            string idle = QuizMessages.ForProfessor(QuizPhase.Idle, "Gallbladder", 0, 0, null, "Tap the organ that stores bile");
            StringAssert.Contains("Answer: Gallbladder", idle);
            StringAssert.Contains("Press Start", idle);
        }

        [Test]
        public void WrapBreaksAtSpacesAndKeepsLongWords()
        {
            Assert.AreEqual("aa bb\ncc", QuizMessages.Wrap("aa bb cc", 5));
            Assert.AreEqual("short", QuizMessages.Wrap("short", 32));
            Assert.AreEqual("abcdefgh\nx", QuizMessages.Wrap("abcdefgh x", 4));
            Assert.AreEqual(string.Empty, QuizMessages.Wrap(null, 10));
            foreach (string line in QuizMessages.Wrap("Tap the heart chamber that pumps blood out to the body", QuizMessages.ProfessorLineWidth).Split('\n'))
                Assert.LessOrEqual(line.Length, QuizMessages.ProfessorLineWidth);
        }
    }
}
