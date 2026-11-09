using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class PinLabelsTests
    {
        [Test]
        public void AnsweredPinTextIsMarked()
        {
            Assert.AreEqual("why?", PinLabels.ForPin("why?", false));
            Assert.AreEqual(PinLabels.AnsweredMark + "why?", PinLabels.ForPin("why?", true));
        }

        [Test]
        public void LoneClusterShowsItsTextWithTheMarkWhenAnswered()
        {
            Assert.AreEqual("why?", PinLabels.ForCluster(1, 1, "why?"));
            Assert.AreEqual(PinLabels.AnsweredMark + "why?", PinLabels.ForCluster(1, 0, "why?"));
        }

        [Test]
        public void BiggerClusterShowsOpenQuestionsUntilAllAreAnswered()
        {
            Assert.AreEqual("3", PinLabels.ForCluster(3, 3, null));
            Assert.AreEqual("1", PinLabels.ForCluster(3, 1, null));
            Assert.AreEqual(PinLabels.AnsweredMark + "3", PinLabels.ForCluster(3, 0, null));
        }
    }
}
