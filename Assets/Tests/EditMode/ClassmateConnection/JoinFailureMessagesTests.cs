using NUnit.Framework;

namespace DIYsect.ClassmateConnection.Tests
{
    public class JoinFailureMessagesTests
    {
        [TestCase("Lobby not found. Please try a new Lobby.", JoinFailure.RoomNotFound)]
        [TestCase("lobby not found", JoinFailure.RoomNotFound)]
        [TestCase("Lobby is full", JoinFailure.RoomFull)]
        [TestCase("lobby full", JoinFailure.RoomFull)]
        [TestCase("Rate limit exceeded. Please try again later.", JoinFailure.TooManyTries)]
        [TestCase("Failed to Authenticate.", JoinFailure.NoInternet)]
        [TestCase("Lobby has been deleted", JoinFailure.RoomClosed)]
        [TestCase("Failed to connect to lobby Anatomy 101.", JoinFailure.HostUnreachable)]
        [TestCase("Connection attempt still in progress.", JoinFailure.Busy)]
        [TestCase("Failed to Join Lobby.", JoinFailure.CouldNotJoin)]
        [TestCase("something nobody planned for", JoinFailure.CouldNotJoin)]
        [TestCase("", JoinFailure.None)]
        [TestCase(null, JoinFailure.None)]
        public void Classify_ReadsTheLobbyText(string raw, JoinFailure expected)
        {
            Assert.AreEqual(expected, JoinFailureMessages.Classify(raw));
        }

        [Test]
        public void Classify_DoesNotTakeAWordThatContainsFullForAFullRoom()
        {
            Assert.AreEqual(JoinFailure.CouldNotJoin, JoinFailureMessages.Classify("Successfully failed"));
        }

        [Test]
        public void EveryFailureWorthShowingHasAShortPlainSentence()
        {
            foreach (JoinFailure failure in System.Enum.GetValues(typeof(JoinFailure)))
            {
                string text = JoinFailureMessages.For(failure);
                if (failure == JoinFailure.None || failure == JoinFailure.Busy)
                {
                    Assert.AreEqual(string.Empty, text, failure.ToString());
                    continue;
                }
                Assert.IsNotEmpty(text, failure.ToString());
                Assert.LessOrEqual(text.Length, 90, failure + " should stay short.");
                Assert.IsTrue(text.EndsWith("."), failure + " should be a sentence.");
            }
        }
    }
}
