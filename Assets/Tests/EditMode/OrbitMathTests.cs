// Pure math, no scene. Limits: pitch 10 to 80 degrees, distance 0.4 to 3 m are the defaults of the camera.
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class OrbitMathTests
    {
        private const float MinPitch = 10f;
        private const float MaxPitch = 80f;
        private const float MinDistance = 0.4f;
        private const float MaxDistance = 3f;

        [Test]
        public void DraggingUpPastTheLimitStopsAtMaxPitch()
        {
            float pitch = 70f;
            for (int i = 0; i < 100; i++)
                pitch = OrbitMath.DragPitch(pitch, -50f, 0.25f, MinPitch, MaxPitch);
            Assert.AreEqual(MaxPitch, pitch, 1e-4f);
        }

        [Test]
        public void DraggingDownPastTheLimitStopsAtMinPitch()
        {
            float pitch = 30f;
            for (int i = 0; i < 100; i++)
                pitch = OrbitMath.DragPitch(pitch, 50f, 0.25f, MinPitch, MaxPitch);
            Assert.AreEqual(MinPitch, pitch, 1e-4f);
        }

        [Test]
        public void YawWrapsAroundWithoutLimit()
        {
            float yaw = 5f;
            yaw = OrbitMath.DragYaw(yaw, 40f, 0.25f); // -10 degrees
            Assert.AreEqual(355f, yaw, 1e-3f);
        }

        [Test]
        public void ZoomInNeverGoesBelowMinimumAndOutNeverAboveMaximum()
        {
            float distance = 1f;
            for (int i = 0; i < 100; i++)
                distance = OrbitMath.Zoom(distance, 0.5f, MinDistance, MaxDistance);
            Assert.AreEqual(MinDistance, distance, 1e-4f);
            for (int i = 0; i < 100; i++)
                distance = OrbitMath.Zoom(distance, -0.5f, MinDistance, MaxDistance);
            Assert.AreEqual(MaxDistance, distance, 1e-4f);
        }

        [Test]
        public void PositiveZoomMovesCloser()
        {
            Assert.Less(OrbitMath.Zoom(1.5f, 0.15f, MinDistance, MaxDistance), 1.5f);
        }

        [Test]
        public void PoseAndAnglesRoundTripAndTheCameraLooksAtTheTarget()
        {
            Vector3 target = new Vector3(1f, 0.8f, 1.2f);
            OrbitMath.Pose(target, 123f, 40f, 1.7f, out Vector3 position, out Quaternion rotation);
            Assert.AreEqual(1.7f, Vector3.Distance(position, target), 1e-4f);
            Assert.Greater(position.y, target.y, "A positive pitch looks down from above.");
            Vector3 toTarget = (target - position).normalized;
            Assert.Greater(Vector3.Dot(rotation * Vector3.forward, toTarget), 0.9999f);

            OrbitMath.AnglesFrom(target, position, out float yaw, out float pitch, out float distance);
            Assert.AreEqual(123f, yaw, 1e-2f);
            Assert.AreEqual(40f, pitch, 1e-2f);
            Assert.AreEqual(1.7f, distance, 1e-4f);
        }

        [Test]
        public void DampingCoversTheSameGroundWhetherFramesAreFastOrSlow()
        {
            float slow = OrbitMath.DampFactor(14f, 1f / 30f);
            float fast = 1f - Mathf.Pow(1f - OrbitMath.DampFactor(14f, 1f / 120f), 4f);
            Assert.AreEqual(slow, fast, 1e-4f);
        }

        [Test]
        public void DampFactorIsZeroWithNoTimeAndOneWithHugeTime()
        {
            Assert.AreEqual(0f, OrbitMath.DampFactor(14f, 0f), 1e-6f);
            Assert.AreEqual(1f, OrbitMath.DampFactor(14f, float.MaxValue), 1e-6f);
        }

        [Test]
        public void DampAngleTakesTheShortWayRoundZero()
        {
            float angle = OrbitMath.DampAngle(350f, 10f, 14f, 1f / 60f);
            Assert.IsTrue(angle > 350f || angle < 10f, $"Went the long way: {angle}");
        }
    }
}
