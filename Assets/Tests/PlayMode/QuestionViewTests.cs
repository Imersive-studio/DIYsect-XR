// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone stores the asker's view on a pin, after validating it.
// The view is kept in the body root's local space, so every peer reads the same values and the view follows the body.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class QuestionViewTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;
        private const float PositionTolerance = 0.001f;
        private const float AngleTolerance = 0.5f;
        private const float RayTolerance = 0.002f;

        private static readonly Vector3 ViewLocalPosition = new Vector3(0.3f, 0.6f, -1.2f);
        private static readonly Quaternion ViewLocalRotation = Quaternion.Euler(15f, 40f, 5f);

        private QuestionBoard _server;
        private QuestionBoard _boardA;
        private QuestionBoard _boardB;

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuestionBoard>();
            Assert.IsNotNull(_server, "The body prefab has no QuestionBoard. Run Tools > DIYsect > Add Question Pins.");
            NetworkManager a = StartClient("ClientA");
            NetworkManager b = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(a, found => bodyA = found);
            yield return WaitForBody(b, found => bodyB = found);
            _boardA = bodyA.GetComponent<QuestionBoard>();
            _boardB = bodyB.GetComponent<QuestionBoard>();
        }

        private Vector3 PointOn(int organ)
        {
            Transform t = _serverBody.Organs[organ].transform;
            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            return t.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
        }

        // Sends a pin from a board with a view given as body-local values, converted to world with that board's own transform.
        private void RequestWithLocalView(QuestionBoard board, string text, Vector3 localPosition, Quaternion localRotation)
        {
            Vector3 worldPosition = board.transform.TransformPoint(localPosition);
            Quaternion worldRotation = board.transform.rotation * localRotation;
            board.RequestPin(OrganIndex, PointOn(OrganIndex), text, worldPosition, worldRotation);
        }

        private static void AssertView(QuestionPin pin, Vector3 position, Quaternion rotation, string who)
        {
            Assert.IsTrue(pin.HasView, $"{who}: the pin should have a view.");
            Assert.Less((pin.ViewPosition - position).magnitude, PositionTolerance, $"{who}: view position {pin.ViewPosition.ToString("F4")}, want {position.ToString("F4")}.");
            Assert.Less(Quaternion.Angle(pin.ViewRotation, rotation), AngleTolerance, $"{who}: view rotation differs.");
        }

        [UnityTest]
        public IEnumerator ViewIsStoredBodyLocalAndSharedWithEveryPeer()
        {
            yield return StartTwoClients();
            RequestWithLocalView(_boardA, "look", ViewLocalPosition, ViewLocalRotation);

            foreach (QuestionBoard board in new[] { _server, _boardA, _boardB })
            {
                yield return WaitFor(() => board.Count == 1, $"the pin on {board.name}");
                AssertView(board[0], ViewLocalPosition, ViewLocalRotation, board.name);
            }
        }

        [UnityTest]
        public IEnumerator RequestWithoutAViewStillWorksAndHasNoView()
        {
            yield return StartTwoClients();
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "plain");

            foreach (QuestionBoard board in new[] { _server, _boardA, _boardB })
            {
                yield return WaitFor(() => board.Count == 1, $"the pin on {board.name}");
                Assert.IsFalse(board[0].HasView);
                Assert.AreEqual("plain", board[0].Text.ToString());
            }
        }

        [UnityTest]
        public IEnumerator InvalidViewsKeepTheQuestionButDropTheView()
        {
            yield return StartTwoClients();
            RequestWithLocalView(_boardA, "nan", new Vector3(float.NaN, 0f, 0f), ViewLocalRotation);
            RequestWithLocalView(_boardB, "far", new Vector3(20f, 0f, 0f), ViewLocalRotation);
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "zero", Vector3.zero, new Quaternion(0f, 0f, 0f, 0f));

            yield return WaitFor(() => _server.Count == 3, "all three questions to be stored", () => $"Has {_server.Count}.");
            for (int i = 0; i < 3; i++)
                Assert.IsFalse(_server[i].HasView, $"Pin '{_server[i].Text}' must be stored without a view.");
        }

        [UnityTest]
        public IEnumerator ViewFollowsTheBodyWhenItMoves()
        {
            yield return StartTwoClients();
            RequestWithLocalView(_boardA, "look", ViewLocalPosition, ViewLocalRotation);
            yield return WaitFor(() => _server.Count == 1 && _boardB.Count == 1, "the pin everywhere");

            _serverBody.transform.position += new Vector3(2f, 0f, -1f);
            _serverBody.transform.rotation *= Quaternion.Euler(0f, 35f, 0f);
            yield return null;
            yield return null;

            AssertView(_server[0], ViewLocalPosition, ViewLocalRotation, "server after the body moved");
            AssertView(_boardB[0], ViewLocalPosition, ViewLocalRotation, "ClientB after the body moved");
        }

        [UnityTest]
        public IEnumerator LateJoinerReceivesTheView()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuestionBoard>();
            NetworkManager a = StartClient("ClientA");
            SharedBody bodyA = null;
            yield return WaitForBody(a, found => bodyA = found);
            _boardA = bodyA.GetComponent<QuestionBoard>();
            RequestWithLocalView(_boardA, "look", ViewLocalPosition, ViewLocalRotation);
            yield return WaitFor(() => _server.Count == 1, "the pin on the server");

            NetworkManager late = StartClient("LateClient");
            SharedBody lateBody = null;
            yield return WaitForBody(late, found => lateBody = found);
            QuestionBoard lateBoard = lateBody.GetComponent<QuestionBoard>();
            yield return WaitFor(() => lateBoard.Count == 1, "the late joiner to see the pin");
            AssertView(lateBoard[0], ViewLocalPosition, ViewLocalRotation, "late joiner");
            Assert.IsTrue(_server[0].Equals(lateBoard[0]));
        }

        [UnityTest]
        public IEnumerator PinPlacementInputSendsTheCameraPoseAsTheView()
        {
            yield return StartTwoClients();
            // All three peers share one physics scene in this fixture. A real device only raycasts its own copy;
            // remove the other two copies from this raycast so hit ordering cannot change the result.
            foreach (QuestionBoard otherPeer in new[] { _boardA, _boardB })
                foreach (Collider collider in otherPeer.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            Physics.SyncTransforms();
            Vector3 target = _serverBody.Organs[OrganIndex].transform.TransformPoint(PointOn(OrganIndex));
            var ray = new Ray(target + Vector3.up * 0.5f, Vector3.down);
            Assert.IsTrue(Physics.Raycast(ray, out RaycastHit testHit), "The test ray should hit an organ collider.");
            Assert.IsTrue(testHit.collider.transform.IsChildOf(_serverBody.transform),
                $"The ray hit a different peer's collider: {testHit.collider.transform.root.name}.");

            var cameraObject = new GameObject("AskerCamera");
            cameraObject.transform.SetPositionAndRotation(_serverBody.transform.TransformPoint(ViewLocalPosition),
                _serverBody.transform.rotation * ViewLocalRotation);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;

            var holder = new GameObject("PinInput");
            holder.SetActive(false);
            var input = holder.AddComponent<PinPlacementInput>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(PinPlacementInput).GetField("_board", flags).SetValue(input, _server);
            typeof(PinPlacementInput).GetField("_camera", flags).SetValue(input, camera);
            typeof(PinPlacementInput).GetField("_gestures", flags).SetValue(input, holder.AddComponent<PointerGestureTracker>());
            holder.SetActive(true);
            try
            {
                Assert.IsTrue(input.TryRequestPin(ray));
                yield return WaitFor(() => _server.Count == 1, "the pin request to reach the server");
                Vector3 expectedPosition = _server.transform.InverseTransformPoint(camera.transform.position);
                Quaternion expectedRotation = Quaternion.Inverse(_server.transform.rotation) * camera.transform.rotation;
                AssertView(_server[0], expectedPosition, expectedRotation, "server");
                Assert.Less((expectedPosition - ViewLocalPosition).magnitude, RayTolerance);
            }
            finally
            {
                Object.Destroy(holder);
                Object.Destroy(cameraObject);
            }
        }
    }
}
