using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class ClassmateOrbitMathTests
    {
        private const float Tolerance = 1e-3f;

        [Test]
        public void FromPosition_ThenPoseAround_ReproducesTheStartingCamera()
        {
            var pivot = new Vector3(0.2f, 0.9f, -0.4f);
            var camera = new Vector3(0.6f, 1.9f, -1.4f);

            ClassmateOrbitMath.FromPosition(pivot, camera, out float yaw, out float pitch, out float distance);
            Pose pose = ClassmateOrbitMath.PoseAround(pivot, yaw, pitch, distance);

            Assert.That(Vector3.Distance(camera, pose.position), Is.LessThan(Tolerance));
            Vector3 looking = pose.rotation * Vector3.forward;
            Assert.That(Vector3.Angle(looking, (pivot - camera).normalized), Is.LessThan(0.1f), "The camera must still look at the pivot.");
        }

        [Test]
        public void DraggingRight_SwingsTheCameraLeftAroundTheBody()
        {
            Pose before = ClassmateOrbitMath.PoseAround(Vector3.zero, 0f, 30f, 2f);
            Vector2 next = ClassmateOrbitMath.ApplyDrag(0f, 30f, new Vector2(100f, 0f), 0.25f, 5f, 85f);
            Pose after = ClassmateOrbitMath.PoseAround(Vector3.zero, next.x, next.y, 2f);

            Assert.Less(after.position.x, before.position.x);
        }

        [Test]
        public void DraggingDown_RaisesTheCamera_AndPitchIsClamped()
        {
            Vector2 down = ClassmateOrbitMath.ApplyDrag(0f, 30f, new Vector2(0f, -40f), 0.25f, 8f, 85f);
            Assert.Greater(down.y, 30f);

            Vector2 farDown = ClassmateOrbitMath.ApplyDrag(0f, 30f, new Vector2(0f, -100000f), 0.25f, 8f, 85f);
            Assert.AreEqual(85f, farDown.y, Tolerance);
            Vector2 farUp = ClassmateOrbitMath.ApplyDrag(0f, 30f, new Vector2(0f, 100000f), 0.25f, 8f, 85f);
            Assert.AreEqual(8f, farUp.y, Tolerance);
        }

        [Test]
        public void SpreadingFingers_MovesCloser_PinchingMovesAway_AndBothAreClamped()
        {
            Assert.Less(ClassmateOrbitMath.ApplyPinch(2f, 100f, 200f, 0.4f, 4f), 2f);
            Assert.Greater(ClassmateOrbitMath.ApplyPinch(2f, 200f, 100f, 0.4f, 4f), 2f);
            Assert.AreEqual(0.4f, ClassmateOrbitMath.ApplyPinch(2f, 100f, 100000f, 0.4f, 4f), Tolerance);
            Assert.AreEqual(4f, ClassmateOrbitMath.ApplyPinch(2f, 100000f, 100f, 0.4f, 4f), Tolerance);
        }

        [Test]
        public void ACollapsedPinch_DoesNothing()
        {
            Assert.AreEqual(2f, ClassmateOrbitMath.ApplyPinch(2f, 0f, 120f, 0.4f, 4f), Tolerance);
            Assert.AreEqual(2f, ClassmateOrbitMath.ApplyPinch(2f, 120f, 0f, 0.4f, 4f), Tolerance);
        }

        [Test]
        public void ScrollingUp_MovesCloser()
        {
            Assert.Less(ClassmateOrbitMath.ApplyScroll(2f, 1f, 0.1f, 0.4f, 4f), 2f);
            Assert.Greater(ClassmateOrbitMath.ApplyScroll(2f, -1f, 0.1f, 0.4f, 4f), 2f);
        }

        [Test]
        public void IsTap_AcceptsSmallMovementAndRejectsADrag()
        {
            Assert.IsTrue(ClassmateOrbitMath.IsTap(new Vector2(100f, 100f), new Vector2(104f, 103f), 12f));
            Assert.IsTrue(ClassmateOrbitMath.IsTap(new Vector2(100f, 100f), new Vector2(100f, 100f), 12f));
            Assert.IsFalse(ClassmateOrbitMath.IsTap(new Vector2(100f, 100f), new Vector2(140f, 100f), 12f));
        }
    }
}
