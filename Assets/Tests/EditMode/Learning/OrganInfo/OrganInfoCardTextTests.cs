using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class OrganInfoCardTextTests
    {
        [Test]
        public void TheTitleIsTheNameInCapitals()
        {
            var entry = new OrganInfoEntry("kidney_l", "Left kidney", "Filters blood.", "It is bean shaped.");

            Assert.AreEqual("LEFT KIDNEY", OrganInfoCardText.Title(entry));
            Assert.AreEqual("Filters blood.", OrganInfoCardText.Function(entry));
            Assert.AreEqual("It is bean shaped.", OrganInfoCardText.Fact(entry));
            Assert.AreEqual("ANATOMICAL STRUCTURE", OrganInfoCardText.Eyebrow);
        }

        [Test]
        public void AMissingEntryGivesNoText()
        {
            Assert.AreEqual(string.Empty, OrganInfoCardText.Title(null));
            Assert.AreEqual(string.Empty, OrganInfoCardText.Function(null));
            Assert.AreEqual(string.Empty, OrganInfoCardText.Fact(null));
        }
    }
}
