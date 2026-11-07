using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class PinClustererTests
    {
        private const float Radius = 0.03f;

        private static QuestionPin Pin(int id, int organ, float x)
        {
            return new QuestionPin { Id = id, OrganIndex = organ, LocalPoint = new Vector3(x, 0f, 0f) };
        }

        [Test]
        public void PinsWithinTheRadiusFormOneCluster()
        {
            var clusters = new PinClusterer(Radius).Build(new[] { Pin(1, 2, 0f), Pin(2, 2, 0.01f), Pin(3, 2, 0.02f) });
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(3, clusters[0].Count);
            Assert.AreEqual(0.01f, clusters[0].Centre.x, 1e-5f);
        }

        [Test]
        public void PinsBeyondTheRadiusStaySeparate()
        {
            var clusters = new PinClusterer(Radius).Build(new[] { Pin(1, 2, 0f), Pin(2, 2, 0.01f), Pin(3, 2, 0.2f) });
            Assert.AreEqual(2, clusters.Count);
            Assert.AreEqual(2, clusters[0].Count);
            Assert.AreEqual(1, clusters[1].Count);
        }

        [Test]
        public void PinsOnDifferentOrgansNeverMerge()
        {
            var clusters = new PinClusterer(Radius).Build(new[] { Pin(1, 2, 0f), Pin(2, 3, 0f) });
            Assert.AreEqual(2, clusters.Count);
            Assert.AreNotEqual(clusters[0].OrganIndex, clusters[1].OrganIndex);
        }

        [Test]
        public void NoPinsGiveNoClusters()
        {
            Assert.AreEqual(0, new PinClusterer(Radius).Build(new QuestionPin[0]).Count);
        }

        [Test]
        public void GlowGrowsWithTheCountAndStopsAtFull()
        {
            Assert.AreEqual(0f, PinClusterer.Glow(1));
            for (int count = 2; count <= PinClusterer.FullGlowCount; count++)
                Assert.Greater(PinClusterer.Glow(count), PinClusterer.Glow(count - 1));
            Assert.AreEqual(1f, PinClusterer.Glow(PinClusterer.FullGlowCount));
            Assert.AreEqual(1f, PinClusterer.Glow(PinClusterer.FullGlowCount + 10));
        }
    }
}
