using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class QuizOrganLookupTests
    {
        private static readonly string[] _organs = { "liver", "kidney_l", "kidney_r", "stomach" };

        [TestCase("liver", 0)]
        [TestCase("kidney_r", 2)]
        [TestCase("Stomach", 3)]
        [TestCase("  kidney_l ", 1)]
        public void FindsTheOrganByObjectName(string target, int expected)
        {
            Assert.AreEqual(expected, QuizOrganLookup.IndexOf(_organs.Length, i => _organs[i], target));
        }

        [TestCase("heart")]
        [TestCase("")]
        [TestCase(null)]
        public void UnknownOrEmptyNamesGiveMinusOne(string target)
        {
            Assert.AreEqual(-1, QuizOrganLookup.IndexOf(_organs.Length, i => _organs[i], target));
        }

        [Test]
        public void OnlyLooksAtTheGivenCount()
        {
            Assert.AreEqual(-1, QuizOrganLookup.IndexOf(1, i => _organs[i], "stomach"));
        }
    }
}
