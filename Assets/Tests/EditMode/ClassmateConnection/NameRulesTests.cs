using NUnit.Framework;

namespace DIYsect.ClassmateConnection.Tests
{
    public class NameRulesTests
    {
        [TestCase("Maya", "Maya")]
        [TestCase("  Maya  Rao ", "Maya Rao")]
        [TestCase("Maya\tRao\n", "Maya Rao")]
        [TestCase("<b>Maya</b>", "bMaya/b")]
        public void ANameIsTrimmedAndTidied(string typed, string expected)
        {
            Assert.IsTrue(NameRules.TryClean(typed, out string name));
            Assert.AreEqual(expected, name);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("<>")]
        public void NothingLeftMeansNoName(string typed)
        {
            Assert.IsFalse(NameRules.TryClean(typed, out string name));
            Assert.AreEqual(string.Empty, name);
        }

        [Test]
        public void ALongNameIsCutToTheLimit()
        {
            Assert.IsTrue(NameRules.TryClean(new string('a', 100), out string name));
            Assert.AreEqual(NameRules.MaxLength, name.Length);
        }

        [Test]
        public void AngleBracketsNeverReachTheProfessorsPanels()
        {
            NameRules.TryClean("<color=red>Mia", out string name);
            StringAssert.DoesNotContain("<", name);
            StringAssert.DoesNotContain(">", name);
        }
    }
}
