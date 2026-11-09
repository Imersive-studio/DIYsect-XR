using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class OrganInfoCardTextTests
    {
        [Test]
        public void TheTextHoldsTheNameTheFunctionAndTheFact()
        {
            var entry = new OrganInfoEntry("spleen", "Spleen", "Filters the blood.", "It is about the size of a fist.");

            string text = OrganInfoCardText.Build(entry);

            StringAssert.Contains("Spleen", text);
            StringAssert.Contains("Filters the blood.", text);
            StringAssert.Contains("It is about the size of a fist.", text);
            Assert.Less(text.IndexOf("Spleen"), text.IndexOf("Filters the blood."));
            Assert.Less(text.IndexOf("Filters the blood."), text.IndexOf("It is about the size of a fist."));
        }

        [Test]
        public void AMissingEntryGivesNoText()
        {
            Assert.AreEqual(string.Empty, OrganInfoCardText.Build(null));
        }
    }
}
