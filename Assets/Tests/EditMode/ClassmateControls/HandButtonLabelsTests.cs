using NUnit.Framework;

namespace DIYsect.ClassmateControls.Tests
{
    public class HandButtonLabelsTests
    {
        [Test]
        public void ADownHandOffersToRaiseIt()
        {
            Assert.AreEqual("Raise hand", HandButtonLabels.For(0));
        }

        [Test]
        public void AnUpHandOffersToLowerItAndShowsAPlaceInTheLineAfterTheFirst()
        {
            Assert.AreEqual("Lower hand", HandButtonLabels.For(1));
            Assert.AreEqual("Lower hand (3)", HandButtonLabels.For(3));
        }
    }
}
