using System.Text;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizPromptTextTests
    {
        [Test]
        public void ShortTextIsOnlyTrimmed()
        {
            Assert.AreEqual("Tap the liver", QuizPromptText.Clamp("  Tap the liver \n"));
        }

        [Test]
        public void EmptyTextBecomesEmpty()
        {
            Assert.AreEqual(string.Empty, QuizPromptText.Clamp(null));
            Assert.AreEqual(string.Empty, QuizPromptText.Clamp("   "));
        }

        [Test]
        public void LongTextIsCutToFitTheNetworkString()
        {
            string clamped = QuizPromptText.Clamp(new string('a', 500));
            Assert.AreEqual(QuizPromptText.MaxBytes, Encoding.UTF8.GetByteCount(clamped));
        }

        [Test]
        public void ACharacterIsNeverCutInHalf()
        {
            // Each emoji is two UTF-16 units and four UTF-8 bytes.
            string clamped = QuizPromptText.Clamp(new string('a', QuizPromptText.MaxBytes - 2) + "\U0001F600\U0001F600");
            Assert.AreEqual(QuizPromptText.MaxBytes - 2, clamped.Length);
            Assert.LessOrEqual(Encoding.UTF8.GetByteCount(clamped), QuizPromptText.MaxBytes);
            foreach (char c in clamped)
                Assert.IsFalse(char.IsSurrogate(c));
        }
    }
}
