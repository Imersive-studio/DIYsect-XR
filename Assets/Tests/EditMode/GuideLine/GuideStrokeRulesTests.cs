using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Networking.Tests
{
    public class GuideStrokeRulesTests
    {
        private const float MaxRadius = 1.5f;
        private const float MinSpacing = 0.005f;
        private const int MaxPoints = 256;

        private GuideStrokeRules _rules;

        [SetUp]
        public void SetUp()
        {
            _rules = new GuideStrokeRules(MaxRadius, MinSpacing, MaxPoints);
        }

        [Test]
        public void PointInsideReachAndFarEnoughIsAccepted()
        {
            var verdict = _rules.Judge(new Vector3(0.2f, 0.1f, 0f), new Vector3(0.1f, 0.1f, 0f), 3);
            Assert.AreEqual(GuideStrokeRules.Verdict.Accepted, verdict);
        }

        [Test]
        public void FirstPointWithNoLastPointIsAccepted()
        {
            var verdict = _rules.Judge(new Vector3(0.1f, 0f, 0f), null, 0);
            Assert.AreEqual(GuideStrokeRules.Verdict.Accepted, verdict);
        }

        [Test]
        public void NanPointIsRejectedAsNotFinite()
        {
            var verdict = _rules.Judge(new Vector3(float.NaN, 0f, 0f), null, 0);
            Assert.AreEqual(GuideStrokeRules.Verdict.NotFinite, verdict);
        }

        [Test]
        public void InfinitePointIsRejectedAsNotFinite()
        {
            var verdict = _rules.Judge(new Vector3(0f, float.PositiveInfinity, 0f), null, 0);
            Assert.AreEqual(GuideStrokeRules.Verdict.NotFinite, verdict);
        }

        [Test]
        public void PointBeyondTheRadiusIsRejectedAsOutOfReach()
        {
            var verdict = _rules.Judge(new Vector3(MaxRadius + 0.5f, 0f, 0f), null, 0);
            Assert.AreEqual(GuideStrokeRules.Verdict.OutOfReach, verdict);
        }

        [Test]
        public void PointCloserThanTheSpacingToTheLastPointIsRejectedAsTooClose()
        {
            Vector3 last = new Vector3(0.1f, 0f, 0f);
            var verdict = _rules.Judge(last + new Vector3(MinSpacing / 2f, 0f, 0f), last, 2);
            Assert.AreEqual(GuideStrokeRules.Verdict.TooClose, verdict);
        }

        [Test]
        public void StrokeIsFullWhenTheCountReachesTheMaximum()
        {
            var verdict = _rules.Judge(new Vector3(0.5f, 0f, 0f), new Vector3(0.1f, 0f, 0f), MaxPoints);
            Assert.AreEqual(GuideStrokeRules.Verdict.Full, verdict);
        }

        [Test]
        public void StrokeStillHasRoomOnePointBelowTheMaximum()
        {
            var verdict = _rules.Judge(new Vector3(0.5f, 0f, 0f), new Vector3(0.1f, 0f, 0f), MaxPoints - 1);
            Assert.AreEqual(GuideStrokeRules.Verdict.Accepted, verdict);
        }
    }
}
