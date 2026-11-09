// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone decides which slot a head goes to, refuses bad poses, and clears a head that is withdrawn or gone.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class HeadPoseBoardTests : NetworkedBodyTestBase
    {
        private static readonly Vector3 Position = new Vector3(0.2f, 0.6f, -0.8f);
        private static readonly Quaternion Rotation = Quaternion.Euler(25f, 160f, 0f);

        private HeadPoseBoard _server;
        private HeadPoseBoard _clientBoard;
        private NetworkManager _client;

        private IEnumerator StartWithClient()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<HeadPoseBoard>();
            Assert.IsNotNull(_server, "The body prefab has no HeadPoseBoard. Run Tools > DIYsect > Add Head Pose.");
            _client = StartClient("Client");
            SharedBody clientBody = null;
            yield return WaitForBody(_client, found => clientBody = found);
            _clientBoard = clientBody.GetComponent<HeadPoseBoard>();
        }

        [UnityTest]
        public IEnumerator TheHostsHeadIsTheProfessorAndAClientsHeadIsTheStudentOnEveryPeer()
        {
            yield return StartWithClient();
            _server.Publish(Position, Rotation);
            yield return WaitFor(() => _clientBoard.IsPresent(PointerRole.Professor), "the professor's head to reach the client");
            Assert.IsTrue(_clientBoard.TryGetLocalPose(PointerRole.Professor, out Vector3 position, out Quaternion rotation));
            Assert.Less(Vector3.Distance(Position, position), 1e-4f);
            Assert.Less(Quaternion.Angle(Rotation, rotation), 0.01f);
            Assert.IsFalse(_clientBoard.IsPresent(PointerRole.Student));

            _clientBoard.Publish(Position * 2f, Rotation);
            yield return WaitFor(() => _server.IsPresent(PointerRole.Student), "the student's head to reach the server");
            Assert.IsTrue(_server.TryGetLocalPose(PointerRole.Student, out position, out _));
            Assert.Less(Vector3.Distance(Position * 2f, position), 1e-4f);
        }

        [UnityTest]
        public IEnumerator ABadPoseIsRefusedByTheServer()
        {
            yield return StartWithClient();
            _clientBoard.Publish(new Vector3(float.NaN, 0f, 0f), Rotation);
            _clientBoard.Publish(new Vector3(0f, 0f, 500f), Rotation);
            _clientBoard.Publish(Vector3.zero, new Quaternion(0f, 0f, 0f, 0f));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(_server.IsPresent(PointerRole.Student));
            Assert.IsFalse(_clientBoard.IsPresent(PointerRole.Student));
        }

        [UnityTest]
        public IEnumerator WithdrawingClearsTheHeadOnEveryPeer()
        {
            yield return StartWithClient();
            _clientBoard.Publish(Position, Rotation);
            yield return WaitFor(() => _server.IsPresent(PointerRole.Student), "the student's head");
            _clientBoard.Withdraw();
            yield return WaitFor(() => !_server.IsPresent(PointerRole.Student) && !_clientBoard.IsPresent(PointerRole.Student), "the head to be cleared");
        }

        [UnityTest]
        public IEnumerator AHeadThatGoesQuietIsDropped()
        {
            yield return StartWithClient();
            _server.Publish(Position, Rotation);
            yield return WaitFor(() => _clientBoard.IsPresent(PointerRole.Professor), "the professor's head");
            // The default quiet time is 3 seconds with no update.
            yield return WaitFor(() => !_clientBoard.IsPresent(PointerRole.Professor), "the quiet head to be dropped");
        }

        [UnityTest]
        public IEnumerator ADisconnectedStudentIsCleared()
        {
            yield return StartWithClient();
            _clientBoard.Publish(Position, Rotation);
            yield return WaitFor(() => _server.IsPresent(PointerRole.Student), "the student's head");
            _client.Shutdown();
            yield return WaitFor(() => !_server.IsPresent(PointerRole.Student), "the student's head to clear after disconnect");
        }
    }
}
