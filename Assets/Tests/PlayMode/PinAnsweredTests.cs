// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone marks a pin answered, and only for the host (the professor's headset always hosts).
// Clients only ask by Rpc; every peer, late joiners included, then shows the pin as answered.
using System.Collections;
using System.Collections.Generic;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class PinAnsweredTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;

        private QuestionBoard _server;
        private QuestionBoard _boardA;
        private QuestionBoard _boardB;
        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private GameObject _scalpel;

        [TearDown]
        public void DestroyScalpel()
        {
            if (_scalpel != null)
                Object.Destroy(_scalpel);
        }

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuestionBoard>();
            Assert.IsNotNull(_server, "The body prefab has no QuestionBoard. Run Tools > DIYsect > Add Question Pins.");
            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(_managerA, found => bodyA = found);
            yield return WaitForBody(_managerB, found => bodyB = found);
            _boardA = bodyA.GetComponent<QuestionBoard>();
            _boardB = bodyB.GetComponent<QuestionBoard>();
        }

        private Vector3 PointOn(int organ)
        {
            Transform t = _serverBody.Organs[organ].transform;
            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            return t.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
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
            yield return WaitFor(() => board.Count == count, $"{count} pins on {board.name}", () => $"Has {board.Count}.");
        }

        private IEnumerator WaitForAnswered(QuestionBoard board, int index, bool expected)
        {
            yield return WaitFor(() => board.Count > index && board[index].Answered == expected,
                $"pin {index} answered={expected} on {board.name}");
        }

        [UnityTest]
        public IEnumerator HostMarksAPinAnsweredAndEveryClientSeesItDimmed()
        {
            yield return StartTwoClients();
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "why?");
            yield return WaitForPins(_server, 1);
            yield return WaitForPins(_boardB, 1);
            Assert.IsFalse(MarkersOf(_boardB)[0].AllAnswered);

            _server.RequestMarkAnswered(_server[0].Id);

            foreach (QuestionBoard board in new[] { _server, _boardA, _boardB })
            {
                yield return WaitForAnswered(board, 0, true);
                PinMarker marker = MarkersOf(board)[0];
                Assert.IsTrue(marker.AllAnswered, $"{board.name}: the marker must be answered.");
                Assert.AreEqual(0, marker.OpenCount);
                Assert.AreEqual(0f, marker.Glow);
                Assert.AreEqual(PinLabels.AnsweredMark + "why?", marker.GetComponentInChildren<TextMesh>().text);
            }
        }

        [UnityTest]
        public IEnumerator NoClientMayMarkNotEvenTheSteeringOne()
        {
            yield return StartTwoClients();
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "why?");
            yield return WaitForPins(_boardA, 1);
            yield return WaitForPins(_boardB, 1);
            yield return SpawnScalpelWithSteerer(_managerA, scalpel => _scalpel = scalpel);

            var serverRejections = new List<(ulong, PinRejection)>();
            var toA = new List<PinRejection>();
            var toB = new List<PinRejection>();
            _server.RequestRejected += (client, reason) => serverRejections.Add((client, reason));
            _boardA.LocalRequestRejected += toA.Add;
            _boardB.LocalRequestRejected += toB.Add;

            _boardA.RequestMarkAnswered(_boardA[0].Id);
            _boardB.RequestMarkAnswered(_boardB[0].Id);
            yield return WaitFor(() => toA.Count == 1 && toB.Count == 1, "both clients to hear the refusal");

            Assert.AreEqual(PinRejection.NotProfessor, toA[0]);
            Assert.AreEqual(PinRejection.NotProfessor, toB[0]);
            Assert.AreEqual(2, serverRejections.Count);
            Assert.IsFalse(_server[0].Answered);
        }

        [UnityTest]
        public IEnumerator UnknownPinIsRefusedToTheProfessor()
        {
            yield return StartTwoClients();
            var toHost = new List<PinRejection>();
            _server.LocalRequestRejected += toHost.Add;

            _server.RequestMarkAnswered(999);
            yield return WaitFor(() => toHost.Count == 1, "the host to hear the refusal");
            Assert.AreEqual(PinRejection.NoSuchPin, toHost[0]);
        }

        [UnityTest]
        public IEnumerator LateJoinerSeesTheAnsweredPin()
        {
            yield return StartTwoClients();
            _boardA.RequestPin(OrganIndex, PointOn(OrganIndex), "why?");
            yield return WaitForPins(_server, 1);
            _server.RequestMarkAnswered(_server[0].Id);
            yield return WaitForAnswered(_server, 0, true);

            NetworkManager late = StartClient("LateClient");
            SharedBody lateBody = null;
            yield return WaitForBody(late, found => lateBody = found);
            QuestionBoard lateBoard = lateBody.GetComponent<QuestionBoard>();
            yield return WaitForAnswered(lateBoard, 0, true);
            yield return WaitFor(() => MarkersOf(lateBoard).Length == 1 && MarkersOf(lateBoard)[0].AllAnswered, "the late joiner's marker to be answered");
        }

        [UnityTest]
        public IEnumerator AnsweredPinDoesNotCountTowardsTheGlow()
        {
            yield return StartTwoClients();
            Vector3 point = PointOn(OrganIndex);
            _boardA.RequestPin(OrganIndex, point, "q1");
            _boardA.RequestPin(OrganIndex, point, "q2");
            _boardA.RequestPin(OrganIndex, point, "q3");
            yield return WaitForPins(_boardB, 3);
            yield return WaitFor(() => MarkersOf(_boardB).Length == 1, "one cluster");
            float before = MarkersOf(_boardB)[0].Glow;
            Assert.Greater(before, 0f);

            _server.RequestMarkAnswered(_server[0].Id);
            yield return WaitForAnswered(_boardB, 0, true);
            yield return WaitFor(() => MarkersOf(_boardB)[0].OpenCount == 2, "the cluster to count two open pins");
            PinMarker marker = MarkersOf(_boardB)[0];
            Assert.AreEqual(3, marker.Count);
            Assert.Less(marker.Glow, before, "An answered pin must not add to the glow.");
            Assert.IsFalse(marker.AllAnswered);
            Assert.AreEqual("2", marker.GetComponentInChildren<TextMesh>().text);
        }
    }
}
