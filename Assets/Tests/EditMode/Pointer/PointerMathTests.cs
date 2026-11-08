// Under test: the pure parts of the translated pointer: wording, screen placement, overlap, roles, pulse and the rate limit.
using DIYsect.Networking;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class PointerMathTests
    {
        private const float Margin = 0.07f;

        [Test]
        public void LabelNamesTheRoleAndTheOrgan()
        {
            Assert.AreEqual("Professor: Liver", PointerLabelText.Build(PointerRole.Professor, "Liver", null));
            Assert.AreEqual("Student: Liver", PointerLabelText.Build(PointerRole.Student, "Liver", ""));
        }

        [Test]
        public void LabelSaysWhenAClosedLayerHidesTheOrgan()
        {
            Assert.AreEqual("Professor: Liver (under the omentum)", PointerLabelText.Build(PointerRole.Professor, "Liver", "the omentum"));
        }

        [Test]
        public void OnScreenOrganKeepsItsScreenPosition()
        {
            PointerPlacement place = PointerScreenMath.Place(new Vector3(0.3f, 0.6f, 2f), Margin);
            Assert.IsTrue(place.OnScreen);
            Assert.AreEqual(0.3f, place.Position.x, 1e-5f);
            Assert.AreEqual(0.6f, place.Position.y, 1e-5f);
        }

        [Test]
        public void OffScreenToTheRightGivesAnArrowOnTheRightEdgePointingRight()
        {
            PointerPlacement place = PointerScreenMath.Place(new Vector3(1.8f, 0.5f, 3f), Margin);
            Assert.IsFalse(place.OnScreen);
            Assert.AreEqual(1f - Margin, place.Position.x, 1e-4f);
            Assert.AreEqual(0.5f, place.Position.y, 1e-4f);
            Assert.AreEqual(0f, place.AngleDegrees, 1e-3f);
        }

        [Test]
        public void OffScreenAboveGivesAnArrowOnTheTopEdgePointingUp()
        {
            PointerPlacement place = PointerScreenMath.Place(new Vector3(0.5f, 2f, 3f), Margin);
            Assert.IsFalse(place.OnScreen);
            Assert.AreEqual(1f - Margin, place.Position.y, 1e-4f);
            Assert.AreEqual(90f, place.AngleDegrees, 1e-3f);
        }

        [Test]
        public void ArrowIsAlwaysInsideTheMargin()
        {
            foreach (var viewport in new[] { new Vector3(-3f, 4f, 1f), new Vector3(0.9f, -5f, 2f), new Vector3(7f, 0.2f, 1f), new Vector3(0.4f, 0.1f, -2f) })
            {
                Vector2 p = PointerScreenMath.Place(viewport, Margin).Position;
                Assert.GreaterOrEqual(p.x, Margin - 1e-4f);
                Assert.LessOrEqual(p.x, 1f - Margin + 1e-4f);
                Assert.GreaterOrEqual(p.y, Margin - 1e-4f);
                Assert.LessOrEqual(p.y, 1f - Margin + 1e-4f);
            }
        }

        [Test]
        public void BehindTheCameraNeverCountsAsOnScreenAndPointsTheRealWay()
        {
            // An organ behind the camera and to its right projects mirrored, to the left of the centre. The arrow must point right.
            PointerPlacement place = PointerScreenMath.Place(new Vector3(0.3f, 0.5f, -2f), Margin);
            Assert.IsFalse(place.OnScreen);
            Assert.AreEqual(0f, place.AngleDegrees, 1e-3f);
            Assert.AreEqual(1f - Margin, place.Position.x, 1e-4f);
        }

        [Test]
        public void StraightBehindPointsDownInsteadOfFailing()
        {
            PointerPlacement place = PointerScreenMath.Place(new Vector3(0.5f, 0.5f, -1f), Margin);
            Assert.IsFalse(place.OnScreen);
            Assert.AreEqual(-90f, place.AngleDegrees, 1e-3f);
            Assert.IsFalse(float.IsNaN(place.Position.x) || float.IsNaN(place.Position.y));
        }

        [Test]
        public void TwoArrowsOnTheSameSpotArePushedApart()
        {
            Vector2 a = PointerScreenMath.Place(new Vector3(2f, 0.5f, 1f), Margin).Position;
            Vector2 b = PointerScreenMath.Separate(a, a, 0.12f, Margin);
            Assert.GreaterOrEqual(Vector2.Distance(a, b), 0.119f);
            Assert.GreaterOrEqual(b.x, Margin - 1e-4f);
            Assert.LessOrEqual(b.x, 1f - Margin + 1e-4f);
        }

        [Test]
        public void TwoArrowsInACornerStaySeparatedInsideTheMargin()
        {
            Vector2 a = new Vector2(1f - Margin, 1f - Margin);
            Vector2 b = PointerScreenMath.Separate(a, a + new Vector2(-0.01f, 0f), 0.12f, Margin);
            Assert.GreaterOrEqual(Vector2.Distance(a, b), 0.119f);
            Assert.LessOrEqual(b.x, 1f - Margin + 1e-4f);
            Assert.LessOrEqual(b.y, 1f - Margin + 1e-4f);
        }

        [Test]
        public void ArrowsAlreadyApartAreLeftAlone()
        {
            var b = new Vector2(0.2f, 0.9f);
            Assert.AreEqual(b, PointerScreenMath.Separate(new Vector2(0.9f, 0.1f), b, 0.12f, Margin));
        }

        [Test]
        public void TheScalpelSteererIsTheStudentAndEveryoneElseTheProfessor()
        {
            Assert.AreEqual(PointerRole.Student, PointerRoleRule.For(2, 2));
            Assert.AreEqual(PointerRole.Professor, PointerRoleRule.For(1, 2));
            Assert.AreEqual(PointerRole.Professor, PointerRoleRule.For(1, ScalpelSteering.NoSteerer));
            Assert.AreEqual(PointerRole.Professor, PointerRoleRule.For(ScalpelSteering.NoSteerer, ScalpelSteering.NoSteerer));
        }

        [Test]
        public void PulseStaysBetweenZeroAndOneAndRepeatsEveryPeriod()
        {
            for (float t = 0f; t < 5f; t += 0.13f)
            {
                float v = PointerPulse.Value(t, 1.6f);
                Assert.GreaterOrEqual(v, 0f);
                Assert.LessOrEqual(v, 1f);
                Assert.AreEqual(v, PointerPulse.Value(t + 1.6f, 1.6f), 1e-3f);
            }
        }

        [Test]
        public void RateLimitAllowsTheWindowThenRefusesAndRecovers()
        {
            var limiter = new PinRateLimiter(3, 2f);
            Assert.IsTrue(limiter.TryAccept(1, 0f));
            Assert.IsTrue(limiter.TryAccept(1, 0.1f));
            Assert.IsTrue(limiter.TryAccept(1, 0.2f));
            Assert.IsFalse(limiter.TryAccept(1, 0.3f));
            Assert.IsTrue(limiter.TryAccept(2, 0.3f), "Another sender has its own allowance.");
            Assert.IsTrue(limiter.TryAccept(1, 2.1f), "The window slid past the first request.");
        }
    }
}
