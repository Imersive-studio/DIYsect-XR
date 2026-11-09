// Pure numbers, no scene. The opening view must sit inside the orbit camera's own limits so the first frame does not jump.
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class ClassmateOpeningViewTests
    {
        [Test]
        public void OffsetIsBehindTheBodyAboveAndAtTheOpeningDistance()
        {
            Vector3 offset = ClassmateOpeningView.Offset();
            Assert.AreEqual(ClassmateOpeningView.Distance, offset.magnitude, 0.001f);
            Assert.Greater(offset.y, 0f, "The camera starts above the body.");
            Assert.Less(offset.z, 0f, "The camera starts on the foot side, looking toward +Z.");
            Assert.AreEqual(0f, offset.x, 0.001f);
        }

        [Test]
        public void OffsetGivesTheOpeningPitchWhenReadBackByTheOrbitMath()
        {
            OrbitMath.AnglesFrom(Vector3.zero, ClassmateOpeningView.Offset(), out float yaw, out float pitch, out float distance);
            Assert.AreEqual(ClassmateOpeningView.Pitch, pitch, 0.01f);
            Assert.AreEqual(ClassmateOpeningView.Distance, distance, 0.001f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, yaw), 0.01f);
        }

        [Test]
        public void OpeningViewIsLowerAndWiderThanTheOldStraightDownStart()
        {
            // The old start was (0, 0.9, -0.9): a 45 degree pitch about 1.27 m away.
            Assert.Less(ClassmateOpeningView.Pitch, 45f);
            Assert.Greater(ClassmateOpeningView.Distance, 1.27f);
        }

        [Test]
        public void OpeningViewIsInsideTheOrbitCameraDefaultLimits()
        {
            Assert.GreaterOrEqual(ClassmateOpeningView.Pitch, 10f);
            Assert.LessOrEqual(ClassmateOpeningView.Pitch, 80f);
            Assert.GreaterOrEqual(ClassmateOpeningView.Distance, 0.4f);
            Assert.LessOrEqual(ClassmateOpeningView.Distance, 3f);
        }
    }
}
