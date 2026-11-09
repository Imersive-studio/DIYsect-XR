using Unity.Netcode;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class PinAnswerRuleTests
    {
        [Test]
        public void OnlyTheHostMayMark()
        {
            Assert.IsTrue(PinAnswerRule.CanMarkAnswered(NetworkManager.ServerClientId));
        }

        [Test]
        public void NoClientMayMark()
        {
            for (ulong client = 1; client <= 5; client++)
                Assert.IsFalse(PinAnswerRule.CanMarkAnswered(client), "client " + client);
        }

        [Test]
        public void RefusalsHaveMessages()
        {
            Assert.IsNotEmpty(PinRejectionMessages.For(PinRejection.NotProfessor));
            Assert.IsNotEmpty(PinRejectionMessages.For(PinRejection.NoSuchPin));
        }
    }
}
