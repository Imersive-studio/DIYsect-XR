// Pure server rules for a published head; no network.
using DIYsect.Networking;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class HeadPoseRuleTests
    {
        [Test]
        public void TheHostIsTheProfessorAndAnyOtherClientIsTheStudent()
        {
            Assert.AreEqual(PointerRole.Professor, HeadPoseRule.RoleFor(0, 0));
            Assert.AreEqual(PointerRole.Student, HeadPoseRule.RoleFor(3, 0));
        }

        [Test]
        public void ASensiblePoseNearTheBodyIsAccepted()
        {
            Assert.IsTrue(HeadPoseRule.IsAcceptable(new Vector3(0.3f, 0.6f, -0.9f), Quaternion.Euler(20f, 180f, 0f), 10f));
        }

        [Test]
        public void NotANumberInfinityAndZeroRotationAreRefused()
        {
            Assert.IsFalse(HeadPoseRule.IsAcceptable(new Vector3(float.NaN, 0f, 0f), Quaternion.identity, 10f));
            Assert.IsFalse(HeadPoseRule.IsAcceptable(new Vector3(0f, float.PositiveInfinity, 0f), Quaternion.identity, 10f));
            Assert.IsFalse(HeadPoseRule.IsAcceptable(Vector3.zero, new Quaternion(0f, 0f, 0f, 0f), 10f));
            Assert.IsFalse(HeadPoseRule.IsAcceptable(Vector3.zero, new Quaternion(float.NaN, 0f, 0f, 1f), 10f));
        }

        [Test]
        public void AHeadFarFromTheBodyIsRefused()
        {
            Assert.IsFalse(HeadPoseRule.IsAcceptable(new Vector3(0f, 0f, 50f), Quaternion.identity, 10f));
        }
    }
}
