// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone adds and removes pins after validating each request.
// Clients only ask by Rpc; every peer then shows the same pins as children of the organs.
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class QuestionPinTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;
        private const float Tolerance = 0.002f;
        private const float MoveTolerance = 0.005f;

        private QuestionBoard _server;
        private QuestionBoard _boardA;
        private QuestionBoard _boardB;
        private readonly List<(ulong, PinRejection)> _rejections = new List<(ulong, PinRejection)>();

        [SetUp]
        public void ClearRejections()
        {
            _rejections.Clear();
        }

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuestionBoard>();
            Assert.IsNotNull(_server, "The body prefab has no QuestionBoard. Run Tools > DIYsect > Add Question Pins.");
            _server.RequestRejected += (client, reason) => _rejections.Add((client, reason));

            NetworkManager a = StartClient("ClientA");
            NetworkManager b = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(a, found => bodyA = found);
            yield return WaitForBody(b, found => bodyB = found);
            _boardA = bodyA.GetComponent<QuestionBoard>();
            _boardB = bodyB.GetComponent<QuestionBoard>();
        }

        // A point inside the organ: the centre of its first mesh bounds, in organ-local space.
        // fraction 0 is the centre, 0.8 is near the +X end of the mesh.
        private Vector3 PointOn(int organ, float fraction = 0f)
        {
            Transform t = _serverBody.Organs[organ].transform;
            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            Bounds mesh = filter.sharedMesh.bounds;
            Vector3 point = mesh.center + new Vector3(mesh.extents.x * fraction, 0f, 0f);
            return t.InverseTransformPoint(filter.transform.TransformPoint(point));
        }

        private static PinMarker[] MarkersOf(QuestionBoard board)
        {
            var markers = board.GetComponent<QuestionPinPresenter>().Markers;
            var copy = new PinMarker[markers.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = markers[i];
            return copy;
        }

        private IEnumerator WaitForPins(QuestionBoard board, int count)
        {
            yield return WaitFor(() => board.Count == count && board.GetComponent<QuestionPinPresenter>().Markers.Count >= (count == 0 ? 0 : 1),
                $"{count} pins on {board.name}", () => $"Has {board.Count}.");
        }

        [UnityTest]
        public IEnumerator PinIsSharedAndParentedUnderTheSameOrganEverywhere()
        {
            yield return StartTwoClients();
            Vector3 point = PointOn(OrganIndex);
            _boardA.RequestPin(OrganIndex, point, "why?");

            yield return WaitForPins(_server, 1);
            yield return WaitForPins(_boardA, 1);
            yield return WaitForPins(_boardB, 1);

            foreach (QuestionBoard board in new[] { _server, _boardA, _boardB })
            {
                QuestionPin pin = board[0];
                Assert.AreEqual(OrganIndex, pin.OrganIndex);
                Assert.AreEqual("why?", pin.Text.ToString());
                Assert.IsTrue((pin.LocalPoint - point).magnitude < Tolerance);
                PinMarker marker = MarkersOf(board)[0];
                Assert.AreSame(board.Body.Organs[OrganIndex].transform, marker.transform.parent, "The marker must be a child of the organ.");
                Assert.IsTrue((marker.transform.localPosition - point).magnitude < Tolerance);
            }
            Assert.AreEqual(_boardA.NetworkManager.LocalClientId, _boardB[0].AuthorClientId);
        }

        [UnityTest]
        public IEnumerator PinFollowsTheOrganWhenItMoves()
        {
            yield return StartTwoClients();
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "?");
            yield return WaitForPins(_boardA, 1);
            yield return WaitForPins(_boardB, 1);

            Transform serverOrgan = _serverBody.Organs[OrganIndex].transform;
            Vector3 target = serverOrgan.localPosition + new Vector3(0.15f, 0.05f, -0.1f);
            serverOrgan.localPosition = target;

            foreach (QuestionBoard board in new[] { _boardA, _boardB })
            {
                Transform organ = board.Body.Organs[OrganIndex].transform;
                Transform marker = MarkersOf(board)[0].transform;
                yield return WaitFor(() => (organ.localPosition - target).magnitude < MoveTolerance, $"{board.name} organ to move", () => $"At {organ.localPosition.ToString("F4")}, want {target.ToString("F4")}.");
                Vector3 expected = organ.TransformPoint(board[0].LocalPoint);
                Assert.IsTrue((marker.position - expected).magnitude < Tolerance, "The pin must stay on the organ.");
            }
        }

        [UnityTest]
        public IEnumerator InvalidRequestsAreRejectedAndChangeNothing()
        {
            yield return StartTwoClients();
            Vector3 good = PointOn(OrganIndex);

            yield return Reject(() => _boardA.RequestPin(99, good, "x"), PinRejection.BadOrgan);
            yield return Reject(() => _boardA.RequestPin(-1, good, "x"), PinRejection.BadOrgan);
            yield return Reject(() => _boardA.RequestPin(OrganIndex, new Vector3(float.NaN, 0f, 0f), "x"), PinRejection.BadPoint);
            yield return Reject(() => _boardA.RequestPin(OrganIndex, good + new Vector3(50f, 0f, 0f), "x"), PinRejection.BadPoint);
            yield return Reject(() => _boardA.RequestPin(OrganIndex, good, "   "), PinRejection.BadText);
            yield return Reject(() => _boardA.RequestPin(OrganIndex, good, new string('a', PinRules.MaxTextLength + 1)), PinRejection.BadText);
            Assert.AreEqual(0, _server.Count);
        }

        [UnityTest]
        public IEnumerator RateLimitStopsAFlood()
        {
            yield return StartTwoClients();
            Vector3 good = PointOn(OrganIndex);
            for (int i = 0; i < 5; i++)
                _boardA.RequestPin(OrganIndex, good, "q");
            yield return WaitFor(() => _server.Count + _rejections.Count >= 5, "five requests to be handled");
            int accepted = _server.Count;
            _rejections.Clear();

            _boardA.RequestPin(OrganIndex, good, "one too many");
            yield return WaitFor(() => _rejections.Count == 1, "the sixth request to be handled");
            Assert.AreEqual(PinRejection.RateLimited, _rejections[0].Item2);
            Assert.AreEqual(accepted, _server.Count);

            // Another client is not affected by A's limit.
            _boardB.RequestPin(OrganIndex, good, "mine");
            yield return WaitFor(() => _server.Count == accepted + 1, "client B to add its pin");
        }

        [UnityTest]
        public IEnumerator OnlyTheAuthorOrHostCanRemoveAPin()
        {
            yield return StartTwoClients();
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "mine");
            yield return WaitForPins(_boardB, 1);
            int id = _boardB[0].Id;

            _boardB.RequestRemove(id);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(1, _server.Count, "Another client must not remove the pin.");

            _boardA.RequestRemove(id);
            yield return WaitFor(() => _server.Count == 0 && _boardB.Count == 0, "the author to remove the pin");
        }

        [UnityTest]
        public IEnumerator NearbyPinsClusterAndFarPinsDoNot()
        {
            yield return StartTwoClients();
            Vector3 p = PointOn(OrganIndex);
            Vector3 far = PointOn(OrganIndex, 0.8f);
            Assert.Greater(Vector3.Distance(p, far), 0.04f, "The test organ is too small to hold a far pin.");
            _boardA.RequestPin(OrganIndex, p, "a");
            _boardA.RequestPin(OrganIndex, p + new Vector3(0.005f, 0f, 0f), "b");
            _boardB.RequestPin(OrganIndex, p + new Vector3(0f, 0.005f, 0f), "c");
            _boardB.RequestPin(OrganIndex, far, "far");

            foreach (QuestionBoard board in new[] { _server, _boardA, _boardB })
            {
                yield return WaitFor(() => board.Count == 4, $"4 pins on {board.name}");
                yield return WaitFor(() => board.GetComponent<QuestionPinPresenter>().Markers.Count == 2, $"2 clusters on {board.name}",
                    () => $"Has {board.GetComponent<QuestionPinPresenter>().Markers.Count}.");
                var counts = new List<int>();
                foreach (PinMarker marker in MarkersOf(board))
                    counts.Add(marker.Count);
                counts.Sort();
                CollectionAssert.AreEqual(new[] { 1, 3 }, counts);
                PinMarker big = MarkersOf(board)[0].Count == 3 ? MarkersOf(board)[0] : MarkersOf(board)[1];
                PinMarker small = MarkersOf(board)[0].Count == 3 ? MarkersOf(board)[1] : MarkersOf(board)[0];
                Assert.Greater(big.Glow, small.Glow, "The bigger cluster must glow more.");
                Assert.Greater(big.transform.localScale.x, small.transform.localScale.x);
            }
        }

        [UnityTest]
        public IEnumerator LateJoinerReceivesExistingPinsAndClusters()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuestionBoard>();
            NetworkManager a = StartClient("ClientA");
            SharedBody bodyA = null;
            yield return WaitForBody(a, found => bodyA = found);
            _boardA = bodyA.GetComponent<QuestionBoard>();
            Vector3 p = PointOn(OrganIndex);
            Vector3 far = PointOn(OrganIndex, 0.8f);
            Assert.Greater(Vector3.Distance(p, far), 0.04f, "The test organ is too small to hold a far pin.");
            _boardA.RequestPin(OrganIndex, p, "a");
            _boardA.RequestPin(OrganIndex, p + new Vector3(0.004f, 0f, 0f), "b");
            _boardA.RequestPin(OrganIndex, far, "c");
            yield return WaitFor(() => _server.Count == 3, "three pins on the server");

            NetworkManager late = StartClient("LateClient");
            SharedBody lateBody = null;
            yield return WaitForBody(late, found => lateBody = found);
            QuestionBoard lateBoard = lateBody.GetComponent<QuestionBoard>();
            QuestionPinPresenter presenter = lateBoard.GetComponent<QuestionPinPresenter>();
            yield return WaitFor(() => lateBoard.Count == 3 && presenter.Markers.Count == 2, "the late joiner to see the pins",
                () => $"Has {lateBoard.Count} pins and {presenter.Markers.Count} markers.");
            for (int i = 0; i < 3; i++)
                Assert.IsTrue(_server[i].Equals(lateBoard[i]));
        }

        [UnityTest]
        public IEnumerator PointerRayOnAnOrganRequestsAPinInOrganLocalSpace()
        {
            yield return StartTwoClients();
            Physics.SyncTransforms();
            Vector3 target = _serverBody.Organs[OrganIndex].transform.TransformPoint(PointOn(OrganIndex));
            var ray = new Ray(target + Vector3.up * 0.5f, Vector3.down);
            Assert.IsTrue(Physics.Raycast(ray, out RaycastHit expectedHit), "The test ray should hit an organ collider.");
            int expectedOrgan = -1;
            for (int i = 0; i < _serverBody.Organs.Count; i++)
                if (expectedHit.collider.transform.IsChildOf(_serverBody.Organs[i].transform))
                    expectedOrgan = i;
            Assert.GreaterOrEqual(expectedOrgan, 0);

            var holder = new GameObject("PinInput");
            holder.SetActive(false);
            var input = holder.AddComponent<PinPlacementInput>();
            typeof(PinPlacementInput).GetField("_board", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(input, _server);
            holder.SetActive(true);
            try
            {
                Assert.IsTrue(input.TryRequestPin(ray));
                yield return WaitFor(() => _server.Count == 1, "the pin request to reach the server");
                QuestionPin pin = _server[0];
                Assert.AreEqual(expectedOrgan, pin.OrganIndex);
                Vector3 expectedLocal = _serverBody.Organs[expectedOrgan].transform.InverseTransformPoint(expectedHit.point);
                Assert.IsTrue((pin.LocalPoint - expectedLocal).magnitude < Tolerance);
                Assert.AreEqual(PinPlacementInput.DefaultText, pin.Text.ToString());

                Assert.IsFalse(input.TryRequestPin(new Ray(target + Vector3.up * 50f, Vector3.up)), "A ray that hits nothing must not request a pin.");
            }
            finally
            {
                Object.Destroy(holder);
            }
        }

        [UnityTest]
        public IEnumerator OnlyTheAskingClientIsToldWhyItWasRefused()
        {
            yield return StartTwoClients();
            var toA = new List<PinRejection>();
            var toB = new List<PinRejection>();
            var toHost = new List<PinRejection>();
            _boardA.LocalRequestRejected += toA.Add;
            _boardB.LocalRequestRejected += toB.Add;
            _server.LocalRequestRejected += toHost.Add;

            _boardA.RequestPin(99, PointOn(OrganIndex), "x");
            yield return WaitFor(() => toA.Count == 1, "client A to hear the refusal");
            // Give a wrong delivery time to arrive before asserting nobody else heard it.
            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(PinRejection.BadOrgan, toA[0]);
            Assert.AreEqual(0, toB.Count, "Client B did not ask, so it must not be told.");
            Assert.AreEqual(0, toHost.Count, "The host did not ask, so it must not be told.");
        }

        [UnityTest]
        public IEnumerator RateLimitedClientIsToldToSlowDown()
        {
            yield return StartTwoClients();
            var toA = new List<PinRejection>();
            _boardA.LocalRequestRejected += toA.Add;
            Vector3 good = PointOn(OrganIndex);
            for (int i = 0; i < 6; i++)
                _boardA.RequestPin(OrganIndex, good, "q");

            yield return WaitFor(() => toA.Count >= 1, "client A to hear the rate limit");
            Assert.AreEqual(PinRejection.RateLimited, toA[0]);
            Assert.AreEqual(1, toA.Count, "Only the sixth request is over the limit.");
        }

        [UnityTest]
        public IEnumerator HostIsToldAboutItsOwnRefusedRequest()
        {
            yield return StartTwoClients();
            var toHost = new List<PinRejection>();
            _server.LocalRequestRejected += toHost.Add;

            _server.RequestPin(OrganIndex, PointOn(OrganIndex), "   ");
            yield return WaitFor(() => toHost.Count == 1, "the host to hear its own refusal");
            Assert.AreEqual(PinRejection.BadText, toHost[0]);
        }

        private IEnumerator Reject(System.Action send, PinRejection expected)
        {
            int before = _rejections.Count;
            send();
            yield return WaitFor(() => _rejections.Count > before, $"the {expected} rejection");
            Assert.AreEqual(expected, _rejections[_rejections.Count - 1].Item2);
        }
    }
}
