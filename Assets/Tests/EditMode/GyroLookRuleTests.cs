// Pure math, no phone. Defaults: up to 20 degrees of yaw, 12 of pitch, a 1.5 degree dead zone.
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class GyroLookRuleTests
    {
        private static Vector2 Limit(Vector2 tilt) =>
            GyroLookRule.Limit(tilt, GyroLookRule.DefaultMaxYaw, GyroLookRule.DefaultMaxPitch, GyroLookRule.DefaultDeadZone);

        [Test]
        public void NoTurnSinceTheReferenceIsNoTilt()
        {
            Quaternion reference = Quaternion.Euler(10f, 80f, 5f);
            Assert.AreEqual(Vector2.zero, GyroLookRule.TiltSince(reference, reference));
        }

        [Test]
        public void TurningTheRightWayGivesPositiveYawAndPitch()
        {
            Quaternion reference = Quaternion.Euler(0f, 90f, 0f);
            Vector2 tilt = GyroLookRule.TiltSince(reference, reference * Quaternion.Euler(0f, 10f, 0f));
            Assert.AreEqual(10f, tilt.x, 0.01f);
            Assert.AreEqual(0f, tilt.y, 0.01f);
            tilt = GyroLookRule.TiltSince(reference, reference * Quaternion.Euler(7f, 0f, 0f));
            Assert.AreEqual(0f, tilt.x, 0.01f);
            Assert.AreEqual(7f, tilt.y, 0.01f);
        }

        [Test]
        public void TiltPastTheWrapAroundPointStillComesBackAsASmallAngle()
        {
            Quaternion reference = Quaternion.Euler(0f, 359f, 0f);
            Vector2 tilt = GyroLookRule.TiltSince(reference, Quaternion.Euler(0f, 3f, 0f));
            Assert.AreEqual(4f, tilt.x, 0.01f);
        }

        [Test]
        public void TiltInsideTheDeadZoneDoesNothing()
        {
            Assert.AreEqual(Vector2.zero, Limit(new Vector2(1.4f, -1.4f)));
        }

        [Test]
        public void TiltPastTheDeadZoneKeepsOnlyTheExcessSoThereIsNoJump()
        {
            Vector2 limited = Limit(new Vector2(4f, -3.5f));
            Assert.AreEqual(2.5f, limited.x, 0.001f);
            Assert.AreEqual(-2f, limited.y, 0.001f);
        }

        [Test]
        public void TheTurnIsClampedToTheLimitsOnBothSides()
        {
            Assert.AreEqual(new Vector2(20f, 12f), Limit(new Vector2(90f, 90f)));
            Assert.AreEqual(new Vector2(-20f, -12f), Limit(new Vector2(-90f, -90f)));
        }

        [Test]
        public void SmoothingMovesPartWayAndReachesTheTargetEventually()
        {
            Vector2 value = Vector2.zero;
            value = GyroLookRule.Smooth(value, new Vector2(10f, 0f), 6f, 0.05f);
            Assert.Greater(value.x, 0f);
            Assert.Less(value.x, 10f);
            for (int i = 0; i < 200; i++)
                value = GyroLookRule.Smooth(value, new Vector2(10f, 0f), 6f, 0.05f);
            Assert.AreEqual(10f, value.x, 0.01f);
        }

        [Test]
        public void RecentringEasesBackToZeroWhenTheTargetIsZero()
        {
            Vector2 value = new Vector2(15f, 8f);
            for (int i = 0; i < 200; i++)
                value = GyroLookRule.Smooth(value, Vector2.zero, 6f, 0.05f);
            Assert.AreEqual(0f, value.magnitude, 0.01f);
        }

        [Test]
        public void SmoothingWithNoTimePassedChangesNothing()
        {
            Assert.AreEqual(new Vector2(3f, 2f), GyroLookRule.Smooth(new Vector2(3f, 2f), new Vector2(9f, 9f), 6f, 0f));
        }

        [Test]
        public void ApplyWithNoOffsetKeepsTheOrbitRotation()
        {
            Quaternion orbit = Quaternion.Euler(30f, 70f, 0f);
            Assert.Less(Quaternion.Angle(orbit, GyroLookRule.Apply(orbit, Vector2.zero)), 0.001f);
        }

        [Test]
        public void ApplyTurnsTheViewByTheOffset()
        {
            Quaternion orbit = Quaternion.Euler(30f, 70f, 0f);
            Quaternion turned = GyroLookRule.Apply(orbit, new Vector2(10f, 0f));
            Assert.AreEqual(10f, Quaternion.Angle(orbit, turned), 0.01f);
            Assert.AreEqual(80f, turned.eulerAngles.y, 0.01f);
        }

        [Test]
        public void AnAllZeroOrBrokenReadingIsNotAnAttitude()
        {
            Assert.IsFalse(GyroLookRule.IsUsable(new Quaternion(0f, 0f, 0f, 0f)));
            Assert.IsFalse(GyroLookRule.IsUsable(new Quaternion(float.NaN, 0f, 0f, 1f)));
            Assert.IsTrue(GyroLookRule.IsUsable(Quaternion.identity));
            Assert.IsTrue(GyroLookRule.IsUsable(Quaternion.Euler(10f, 20f, 30f)));
        }

        [Test]
        public void BadNumbersNeverLeakOutAsNaN()
        {
            var nan = new Vector2(float.NaN, 1f);
            var badQuaternion = new Quaternion(float.NaN, 0f, 0f, 1f);
            Assert.AreEqual(Vector2.zero, GyroLookRule.TiltSince(badQuaternion, Quaternion.identity));
            Assert.AreEqual(Vector2.zero, GyroLookRule.TiltSince(Quaternion.identity, badQuaternion));
            Assert.AreEqual(Vector2.zero, GyroLookRule.TiltSince(new Quaternion(0f, 0f, 0f, 0f), Quaternion.identity));
            Assert.AreEqual(Vector2.zero, Limit(nan));
            Assert.AreEqual(Vector2.zero, GyroLookRule.Smooth(nan, nan, 6f, 0.016f));
            Quaternion orbit = Quaternion.Euler(30f, 70f, 0f);
            Assert.AreEqual(orbit, GyroLookRule.Apply(orbit, nan));
        }
    }
}
