using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class OrganInfoLookupTests
    {
        private static OrganInfoEntry Entry(string name)
        {
            return new OrganInfoEntry(name, "Name of " + name, "It does something.", "It is true.");
        }

        [Test]
        public void AKnownOrganIsFound()
        {
            var lookup = new OrganInfoLookup(new[] { Entry("liver"), Entry("spleen") });

            Assert.IsTrue(lookup.TryGet("spleen", out OrganInfoEntry entry));
            Assert.AreEqual("spleen", entry.OrganName);
            Assert.AreEqual(2, lookup.Count);
        }

        [Test]
        public void TheMatchIgnoresCapitalsAndSurroundingSpaces()
        {
            var lookup = new OrganInfoLookup(new[] { Entry("kidney_l") });

            Assert.IsTrue(lookup.TryGet("  Kidney_L ", out OrganInfoEntry entry));
            Assert.AreEqual("kidney_l", entry.OrganName);
        }

        [TestCase("heart")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void AnUnknownOrEmptyNameGivesNoEntry(string name)
        {
            var lookup = new OrganInfoLookup(new[] { Entry("liver") });

            Assert.IsFalse(lookup.TryGet(name, out OrganInfoEntry entry));
            Assert.IsNull(entry);
        }

        [Test]
        public void NullAndNamelessEntriesAreSkipped()
        {
            var lookup = new OrganInfoLookup(new[] { null, Entry(" "), Entry(null), Entry("liver") });

            Assert.AreEqual(1, lookup.Count);
        }

        [Test]
        public void ANullListGivesAnEmptyLookup()
        {
            var lookup = new OrganInfoLookup(null);

            Assert.AreEqual(0, lookup.Count);
            Assert.IsFalse(lookup.TryGet("liver", out OrganInfoEntry _));
        }

        [Test]
        public void WhenTwoEntriesShareANameTheLaterOneWins()
        {
            var first = new OrganInfoEntry("liver", "Liver", "First.", "First fact.");
            var second = new OrganInfoEntry("liver", "Liver", "Second.", "Second fact.");
            var lookup = new OrganInfoLookup(new[] { first, second });

            Assert.IsTrue(lookup.TryGet("liver", out OrganInfoEntry entry));
            Assert.AreEqual("Second.", entry.Function);
        }
    }
}
