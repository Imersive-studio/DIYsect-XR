// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone decides whose hand is up. A client changes only its own hand, the host (the professor)
// can lower any hand, and a client that leaves loses its hand.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class HandBoardTests : NetworkedBodyTestBase
    {
        private HandBoard _professor;
        private NetworkManager _clientA;
        private NetworkManager _clientB;
        private HandBoard _boardA;
        private HandBoard _boardB;

        private IEnumerator StartWithTwoClassmates()
        {
            yield return StartHostAndSpawnBody();
            _professor = _serverBody.GetComponent<HandBoard>();
            Assert.IsNotNull(_professor, "The body prefab has no HandBoard. Run Tools > DIYsect > Add Raise Hand.");
            _clientA = StartClient("ClassmateA");
            _clientB = StartClient("ClassmateB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(_clientA, found => bodyA = found);
            yield return WaitForBody(_clientB, found => bodyB = found);
            _boardA = bodyA.GetComponent<HandBoard>();
            _boardB = bodyB.GetComponent<HandBoard>();
            yield return WaitFor(() => _boardA.IsSpawned && _boardB.IsSpawned, "both classmates to receive the hand board");
        }

        [UnityTest]
        public IEnumerator ARaisedHandReachesTheProfessorAndEveryoneInTheOrderTheyWentUp()
        {
            yield return StartWithTwoClassmates();

            _boardB.RequestSet(true);
            yield return WaitFor(() => _professor.Count == 1, "the first hand to reach the professor");
            _boardA.RequestSet(true);
            yield return WaitFor(() => _professor.Count == 2, "the second hand to reach the professor");

            Assert.AreEqual(1, _professor.OrderOf(_clientB.LocalClientId), "B raised first.");
            Assert.AreEqual(2, _professor.OrderOf(_clientA.LocalClientId));
            yield return WaitFor(() => _boardA.Count == 2 && _boardB.Count == 2, "both hands to reach the classmates");
            Assert.IsTrue(_boardA.IsLocalRaised);
            Assert.AreEqual(1, _boardA.OrderOf(_clientB.LocalClientId));
        }

        [UnityTest]
        public IEnumerator RaisingTwiceKeepsOneHand_AndALoweringMovesOthersUp()
        {
            yield return StartWithTwoClassmates();
            _boardA.RequestSet(true);
            _boardA.RequestSet(true);
            _boardB.RequestSet(true);
            yield return WaitFor(() => _professor.Count == 2, "two hands");
            yield return null;
            Assert.AreEqual(2, _professor.Count, "A second raise by the same classmate adds nothing.");

            _boardA.RequestSet(false);
            yield return WaitFor(() => _professor.Count == 1, "A's hand to come down");
            Assert.AreEqual(1, _professor.OrderOf(_clientB.LocalClientId));
        }

        [UnityTest]
        public IEnumerator TheProfessorLowersAHandAndTheClassmateSeesItLowered()
        {
            yield return StartWithTwoClassmates();
            _boardA.RequestSet(true);
            yield return WaitFor(() => _professor.IsRaised(_clientA.LocalClientId), "A's hand at the professor");
            yield return WaitFor(() => _boardA.IsLocalRaised, "A to see its own hand up");

            _professor.RequestLower(_clientA.LocalClientId);

            yield return WaitFor(() => !_boardA.IsLocalRaised && _boardA.Count == 0, "A to see the hand lowered");
            Assert.AreEqual(0, _professor.Count);
        }

        [UnityTest]
        public IEnumerator AClassmateCannotLowerSomeoneElsesHandOrAllHands()
        {
            yield return StartWithTwoClassmates();
            _boardA.RequestSet(true);
            yield return WaitFor(() => _professor.IsRaised(_clientA.LocalClientId), "A's hand");

            _boardB.RequestLower(_clientA.LocalClientId);
            _boardB.RequestLowerAll();
            for (int i = 0; i < 30; i++)
                yield return null;

            Assert.IsTrue(_professor.IsRaised(_clientA.LocalClientId), "Only the owner or the professor may lower a hand.");
            Assert.IsTrue(_boardA.IsLocalRaised);
        }

        [UnityTest]
        public IEnumerator TheProfessorCanLowerEveryHandAtOnce()
        {
            yield return StartWithTwoClassmates();
            _boardA.RequestSet(true);
            _boardB.RequestSet(true);
            yield return WaitFor(() => _professor.Count == 2, "both hands");

            _professor.RequestLowerAll();

            yield return WaitFor(() => _boardA.Count == 0 && _boardB.Count == 0 && _professor.Count == 0, "every hand to come down");
        }

        [UnityTest]
        public IEnumerator ADisconnectedClassmateLosesTheirHand()
        {
            yield return StartWithTwoClassmates();
            _boardA.RequestSet(true);
            _boardB.RequestSet(true);
            yield return WaitFor(() => _professor.Count == 2, "both hands");

            ulong leaver = _clientA.LocalClientId;
            _clientA.Shutdown();

            yield return WaitFor(() => _professor.Count == 1 && !_professor.IsRaised(leaver), "the leaver's hand to be cleared");
            yield return WaitFor(() => _boardB.Count == 1, "the other classmate to see the change");
            Assert.AreEqual(1, _professor.OrderOf(_clientB.LocalClientId));
        }

        [UnityTest]
        public IEnumerator ALateJoinerSeesTheHandsThatAreAlreadyUp()
        {
            yield return StartWithTwoClassmates();
            _boardA.RequestSet(true);
            yield return WaitFor(() => _professor.Count == 1, "A's hand");

            NetworkManager late = StartClient("LateClassmate");
            SharedBody lateBody = null;
            yield return WaitForBody(late, found => lateBody = found);
            var lateBoard = lateBody.GetComponent<HandBoard>();

            yield return WaitFor(() => lateBoard.IsSpawned && lateBoard.Count == 1, "the late joiner to receive the raised hand");
            Assert.IsTrue(lateBoard.IsRaised(_clientA.LocalClientId));
        }

        [UnityTest]
        public IEnumerator ARapidlyTappedButtonIsRateLimited()
        {
            yield return StartWithTwoClassmates();
            // Six changes are allowed in the window; they end with the hand down. The seventh is dropped.
            for (int i = 0; i < 3; i++)
            {
                _boardA.RequestSet(true);
                _boardA.RequestSet(false);
            }
            _boardA.RequestSet(true);
            for (int i = 0; i < 30; i++)
                yield return null;

            Assert.IsFalse(_professor.IsRaised(_clientA.LocalClientId), "The seventh change inside the window is refused.");
            Assert.AreEqual(0, _professor.Count);
        }
    }
}
