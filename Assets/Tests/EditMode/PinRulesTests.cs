using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class PinRulesTests
    {
        private static readonly Bounds Box = new Bounds(Vector3.zero, new Vector3(0.2f, 0.1f, 0.1f));

        [Test]
        public void TextIsTrimmedAndLengthChecked()
        {
            Assert.IsTrue(PinRules.TryCleanText("  why? ", out string cleaned));
            Assert.AreEqual("why?", cleaned);
            Assert.IsFalse(PinRules.TryCleanText("   ", out _));
            Assert.IsFalse(PinRules.TryCleanText(null, out _));
            Assert.IsFalse(PinRules.TryCleanText(new string('a', PinRules.MaxTextLength + 1), out _));
            Assert.IsTrue(PinRules.TryCleanText(new string('a', PinRules.MaxTextLength), out _));
        }

        [Test]
        public void PointMustBeFiniteAndOnTheOrgan()
        {
            Assert.IsTrue(PinRules.IsPointOnOrgan(new Vector3(0.05f, 0f, 0f), Box, 0.02f));
            Assert.IsTrue(PinRules.IsPointOnOrgan(new Vector3(0.11f, 0f, 0f), Box, 0.02f), "Inside the margin.");
            Assert.IsFalse(PinRules.IsPointOnOrgan(new Vector3(0.2f, 0f, 0f), Box, 0.02f));
            Assert.IsFalse(PinRules.IsPointOnOrgan(new Vector3(float.NaN, 0f, 0f), Box, 0.02f));
            Assert.IsFalse(PinRules.IsPointOnOrgan(new Vector3(float.PositiveInfinity, 0f, 0f), Box, 0.02f));
        }

        [Test]
        public void RateLimiterAllowsTheLimitPerWindowThenRecovers()
        {
            var limiter = new PinRateLimiter(2, 10f);
            Assert.IsTrue(limiter.TryAccept(1, 0f));
            Assert.IsTrue(limiter.TryAccept(1, 1f));
            Assert.IsFalse(limiter.TryAccept(1, 2f));
            Assert.IsTrue(limiter.TryAccept(2, 2f), "Another sender has its own allowance.");
            Assert.IsTrue(limiter.TryAccept(1, 10.5f), "The first request left the window.");
        }
    }
}
