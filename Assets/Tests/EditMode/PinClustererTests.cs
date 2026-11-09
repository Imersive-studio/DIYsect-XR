using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class PinClustererTests
    {
        private const float Radius = 0.03f;

        private static QuestionPin Pin(int id, int organ, float x)
        {
            return new QuestionPin(id, organ, new Vector3(x, 0f, 0f), 0, default);
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

        private static QuestionPin Answered(int id, int organ, float x)
        {
            return Pin(id, organ, x).AsAnswered();
        }

        [Test]
        public void AnsweredPinsStayInTheClusterButDoNotCountAsOpen()
        {
            var clusters = new PinClusterer(Radius).Build(new[] { Pin(1, 2, 0f), Answered(2, 2, 0.01f), Pin(3, 2, 0.02f) });
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(3, clusters[0].Count);
            Assert.AreEqual(2, clusters[0].OpenCount);
            Assert.IsFalse(clusters[0].AllAnswered);
        }

        [Test]
        public void ClusterWithOnlyAnsweredPinsIsAllAnswered()
        {
            var clusters = new PinClusterer(Radius).Build(new[] { Answered(1, 2, 0f), Answered(2, 2, 0.01f) });
            Assert.AreEqual(0, clusters[0].OpenCount);
            Assert.IsTrue(clusters[0].AllAnswered);
        }

        [Test]
        public void GlowFromTheOpenCountIgnoresAnsweredPins()
        {
            var pins = new[] { Pin(1, 2, 0f), Answered(2, 2, 0.01f), Answered(3, 2, 0.02f), Answered(4, 2, 0.02f), Answered(5, 2, 0.02f), Answered(6, 2, 0.02f) };
            PinCluster cluster = new PinClusterer(Radius).Build(pins)[0];
            Assert.AreEqual(6, cluster.Count);
            Assert.AreEqual(0f, PinClusterer.Glow(cluster.OpenCount));
            Assert.AreEqual(1f, PinClusterer.Glow(cluster.Count));
        }

        [Test]
        public void MarkingAPinAnsweredKeepsEverythingElse()
        {
            QuestionPin open = new QuestionPin(7, 3, new Vector3(1f, 2f, 3f), 4, default);
            QuestionPin done = open.AsAnswered();
            Assert.IsFalse(open.Answered);
            Assert.IsTrue(done.Answered);
            Assert.AreEqual(open.Id, done.Id);
            Assert.AreEqual(open.LocalPoint, done.LocalPoint);
            Assert.AreNotEqual(open, done);
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
