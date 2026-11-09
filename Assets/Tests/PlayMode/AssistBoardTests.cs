// Starts a real in-process Netcode host and two clients (student and professor) over UnityTransport on localhost.
// Authority under test: the server alone decides the help request, and every peer reads the same phase.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class AssistBoardTests : NetworkedBodyTestBase
    {
        private AssistBoard _serverBoard;
        private AssistBoard _studentBoard;
        private AssistBoard _professorBoard;
        private NetworkManager _studentManager;
        private NetworkManager _professorManager;
        private GameObject _scalpel;
        private GuidingHand _professorHand;

        [TearDown]
        public void DestroyScalpel()
        {
            if (_scalpel != null)
                Object.Destroy(_scalpel);
        }

        [UnityTest]
        public IEnumerator StudentRequestIsSeenByEveryPeerAndAnsweredByTheProfessor()
        {
            yield return StartSession(studentSteers: true);

            _studentBoard.Toggle();
            yield return WaitFor(() => AllAre(AssistPhase.Requested), "every peer to see the request");
            Assert.AreEqual(_studentManager.LocalClientId, _serverBoard.Requester);
            Assert.IsTrue(_studentBoard.IsLocalRequest);
            Assert.IsFalse(_professorBoard.IsLocalRequest);

            yield return Grab(_professorHand);
            yield return WaitFor(() => AllAre(AssistPhase.Helping), "every peer to see that the professor is guiding");

            _professorHand.ReleaseControl();
            yield return WaitFor(() => AllAre(AssistPhase.None), "every peer to see the request end");
            Assert.AreEqual(ScalpelSteering.NoSteerer, _serverBoard.Requester);
        }

        [UnityTest]
        public IEnumerator PressingAgainCancelsTheRequest()
        {
            yield return StartSession(studentSteers: true);
            _studentBoard.Toggle();
            yield return WaitFor(() => AllAre(AssistPhase.Requested), "every peer to see the request");
            _studentBoard.Toggle();
            yield return WaitFor(() => AllAre(AssistPhase.None), "every peer to see the request taken back");
        }

        [UnityTest]
        public IEnumerator ClientThatDoesNotSteerCannotAsk()
        {
            yield return StartSession(studentSteers: true);
            _professorBoard.RequestSet(true);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(AssistPhase.None, _serverBoard.Phase, "Only the person steering the scalpel may ask.");
        }

        [UnityTest]
        public IEnumerator NobodyCanAskWhileNobodySteers()
        {
            yield return StartSession(studentSteers: false);
            _studentBoard.RequestSet(true);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(AssistPhase.None, _serverBoard.Phase);
        }

        [UnityTest]
        public IEnumerator PuttingTheScalpelDownClosesTheRequest()
        {
            yield return StartSession(studentSteers: true);
            _studentBoard.Toggle();
            yield return WaitFor(() => AllAre(AssistPhase.Requested), "every peer to see the request");
            // The student lets go on their own copy; a release sent from the server's copy is not the steerer's.
            ScalpelSteering studentCopy = null;
            foreach (NetworkObject spawned in _studentManager.SpawnManager.SpawnedObjects.Values)
                if (spawned.TryGetComponent(out studentCopy) && studentCopy.IsSpawned)
                    break;
            studentCopy.ReleaseSteer();
            yield return WaitFor(() => AllAre(AssistPhase.None), "the request to close when the student lets go");
        }

        [UnityTest]
        public IEnumerator StudentDisconnectingClosesTheRequest()
        {
            yield return StartSession(studentSteers: true);
            _studentBoard.Toggle();
            yield return WaitFor(() => _serverBoard.Phase == AssistPhase.Requested && _professorBoard.Phase == AssistPhase.Requested, "the request to arrive");
            _studentManager.Shutdown();
            yield return WaitFor(() => _serverBoard.Phase == AssistPhase.None && _professorBoard.Phase == AssistPhase.None, "the request to close when the student leaves");
        }

        [UnityTest]
        public IEnumerator LateJoinerReadsTheOpenRequest()
        {
            yield return StartSession(studentSteers: true);
            _studentBoard.Toggle();
            yield return WaitFor(() => AllAre(AssistPhase.Requested), "every peer to see the request");

            NetworkManager late = StartClient("Late");
            AssistBoard lateBoard = null;
            yield return WaitForBody(late, body => lateBoard = body.GetComponent<AssistBoard>());
            yield return WaitFor(() => lateBoard.Phase == AssistPhase.Requested, "the late joiner to read the open request");
        }

        // ---- setup ----

        private bool AllAre(AssistPhase phase)
        {
            return _serverBoard.Phase == phase && _studentBoard.Phase == phase && _professorBoard.Phase == phase;
        }

        private IEnumerator StartSession(bool studentSteers)
        {
            yield return StartHostAndSpawnBody();
            _studentManager = StartClient("Student");
            _professorManager = StartClient("Professor");
            SharedBody studentBody = null;
            SharedBody professorBody = null;
            yield return WaitForBody(_studentManager, body => studentBody = body);
            yield return WaitForBody(_professorManager, body => professorBody = body);
            _serverBoard = _serverBody.GetComponent<AssistBoard>();
            _studentBoard = studentBody.GetComponent<AssistBoard>();
            _professorBoard = professorBody.GetComponent<AssistBoard>();
            Assert.IsNotNull(_serverBoard, "The body prefab has no AssistBoard. Run Tools > DIYsect > Add Help Request.");

            if (studentSteers)
            {
                yield return SpawnScalpelWithSteerer(_studentManager, scalpel => _scalpel = scalpel);
            }
            else
            {
                _scalpel = Object.Instantiate(LoadScalpelPrefabAsset());
                var spawned = _scalpel.GetComponent<NetworkObject>();
                spawned.Spawn();
                spawned.TrySetParent(_serverBody.GetComponent<NetworkObject>(), worldPositionStays: false);
            }
            yield return WaitFor(() =>
            {
                foreach (NetworkObject spawned in _professorManager.SpawnManager.SpawnedObjects.Values)
                    if (spawned.TryGetComponent(out _professorHand) && _professorHand.IsSpawned)
                        return true;
                return false;
            }, "the professor to receive the scalpel");
        }

        private static IEnumerator Grab(GuidingHand hand)
        {
            bool? answer = null;
            System.Action<bool> handler = granted => answer = granted;
            hand.ControlResolved += handler;
            hand.RequestControl();
            yield return WaitFor(() => answer.HasValue, "the answer to the depth request");
            hand.ControlResolved -= handler;
            Assert.IsTrue(answer.Value, "The professor should be granted the depth.");
        }
    }
}
