// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: only the host (the professor) decides who may unmute. A classmate cannot give themselves permission,
// the professor can take it away, and a classmate that leaves loses it.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class SpeakBoardTests : NetworkedBodyTestBase
    {
        private SpeakBoard _professor;
        private NetworkManager _clientA;
        private NetworkManager _clientB;
        private SpeakBoard _boardA;
        private SpeakBoard _boardB;

        private IEnumerator StartWithTwoClassmates()
        {
            yield return StartHostAndSpawnBody();
            _professor = _serverBody.GetComponent<SpeakBoard>();
            Assert.IsNotNull(_professor, "The body prefab has no SpeakBoard. Run Tools > DIYsect > Add Raise Hand.");
            _clientA = StartClient("ClassmateA");
            _clientB = StartClient("ClassmateB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(_clientA, found => bodyA = found);
            yield return WaitForBody(_clientB, found => bodyB = found);
            _boardA = bodyA.GetComponent<SpeakBoard>();
            _boardB = bodyB.GetComponent<SpeakBoard>();
            yield return WaitFor(() => _boardA.IsSpawned && _boardB.IsSpawned, "both classmates to receive the speak board");
        }

        [UnityTest]
        public IEnumerator EveryoneButTheProfessorStartsWithoutPermissionToSpeak()
        {
            yield return StartWithTwoClassmates();

            Assert.IsFalse(_boardA.IsLocalAllowed);
            Assert.IsFalse(_boardB.IsLocalAllowed);
            Assert.IsTrue(_professor.IsLocalAllowed, "The professor may always speak.");
        }

        [UnityTest]
        public IEnumerator TheProfessorAllowsOneClassmate_AndOnlyThatClassmateMaySpeak()
        {
            yield return StartWithTwoClassmates();

            _professor.RequestAllow(_clientA.LocalClientId);

            yield return WaitFor(() => _boardA.IsLocalAllowed, "A to be allowed");
            yield return WaitFor(() => _professor.IsAllowed(_clientA.LocalClientId), "the professor to see the permission");
            Assert.IsFalse(_boardB.IsLocalAllowed, "B was not allowed.");
        }

        [UnityTest]
        public IEnumerator AClassmateCannotGiveThemselvesOrAnyoneElsePermission()
        {
            yield return StartWithTwoClassmates();

            _boardA.RequestAllow(_clientA.LocalClientId);
            _boardA.RequestAllow(_clientB.LocalClientId);
            for (int i = 0; i < 30; i++)
                yield return null;

            Assert.IsFalse(_professor.IsAllowed(_clientA.LocalClientId), "Only the professor may allow someone to speak.");
            Assert.IsFalse(_professor.IsAllowed(_clientB.LocalClientId));
            Assert.IsFalse(_boardA.IsLocalAllowed);
        }

        [UnityTest]
        public IEnumerator TheProfessorTakesPermissionAway_AndTheClassmateSeesIt()
        {
            yield return StartWithTwoClassmates();
            _professor.RequestAllow(_clientA.LocalClientId);
            yield return WaitFor(() => _boardA.IsLocalAllowed, "A to be allowed");

            _professor.RequestRevoke(_clientA.LocalClientId);

            yield return WaitFor(() => !_boardA.IsLocalAllowed, "A to lose permission");
            Assert.AreEqual(0, _professor.Count);
        }

        [UnityTest]
        public IEnumerator AClassmateCannotTakeAwayAnotherClassmatesPermission()
        {
            yield return StartWithTwoClassmates();
            _professor.RequestAllow(_clientA.LocalClientId);
            yield return WaitFor(() => _boardA.IsLocalAllowed, "A to be allowed");

            _boardB.RequestRevoke(_clientA.LocalClientId);
            _boardB.RequestRevokeAll();
            for (int i = 0; i < 30; i++)
                yield return null;

            Assert.IsTrue(_boardA.IsLocalAllowed, "Only the professor may take permission away.");
        }

        [UnityTest]
        public IEnumerator TheProfessorCanTakeEveryonesPermissionAwayAtOnce()
        {
            yield return StartWithTwoClassmates();
            _professor.RequestAllow(_clientA.LocalClientId);
            _professor.RequestAllow(_clientB.LocalClientId);
            yield return WaitFor(() => _boardA.IsLocalAllowed && _boardB.IsLocalAllowed, "both to be allowed");

            _professor.RequestRevokeAll();

            yield return WaitFor(() => !_boardA.IsLocalAllowed && !_boardB.IsLocalAllowed && _professor.Count == 0, "everyone to lose permission");
        }

        [UnityTest]
        public IEnumerator ADisconnectedClassmateLosesPermission()
        {
            yield return StartWithTwoClassmates();
            _professor.RequestAllow(_clientA.LocalClientId);
            _professor.RequestAllow(_clientB.LocalClientId);
            yield return WaitFor(() => _professor.Count == 2, "both to be allowed");

            ulong leaver = _clientA.LocalClientId;
            _clientA.Shutdown();

            yield return WaitFor(() => _professor.Count == 1 && !_professor.IsAllowed(leaver), "the leaver's permission to be cleared");
            Assert.IsTrue(_professor.IsAllowed(_clientB.LocalClientId));
        }
    }
}
