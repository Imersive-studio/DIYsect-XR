using DIYsect.Networking;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class PinAnswerRuleTests
    {
        [Test]
        public void ProfessorMayMarkWhenNobodySteers()
        {
            Assert.IsTrue(PinAnswerRule.CanMarkAnswered(1, ScalpelSteering.NoSteerer));
        }

        [Test]
        public void ProfessorMayMarkWhileAnotherClientSteers()
        {
            Assert.IsTrue(PinAnswerRule.CanMarkAnswered(0, 2));
        }

        [Test]
        public void TheSteeringStudentMayNotMark()
        {
            Assert.IsFalse(PinAnswerRule.CanMarkAnswered(2, 2));
        }

        [Test]
        public void RefusalsHaveMessages()
        {
            Assert.IsNotEmpty(PinRejectionMessages.For(PinRejection.NotProfessor));
            Assert.IsNotEmpty(PinRejectionMessages.For(PinRejection.NoSuchPin));
        }
    }
}
