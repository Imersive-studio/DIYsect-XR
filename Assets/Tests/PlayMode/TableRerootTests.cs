// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: none new. Each peer re-roots its own copy of the body, as a professor does when lining the table up with a
// real one. The server still owns the organ poses, which sync in body-local space, so every peer must keep seeing the same body.
using System.Collections;
using DIYsect.Classroom;
using DIYsect.MixedReality;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class TableRerootTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 3;
        private const int PinOrganIndex = 4;
        private const float PositionTolerance = 0.005f;
        private const float RotationToleranceDegrees = 0.5f;

        // Where the middle of the table top sits inside the scene content root in the authored layout.
        private static readonly Vector3 _tableTopLocal = new Vector3(0f, 0.8f, 1.2f);

        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private SharedBody _bodyA;
        private SharedBody _bodyB;

        [UnityTest]
        public IEnumerator PeersPlacedOnDifferentTablesStillShareOneBodyInBodySpace()
        {
            yield return StartHostAndTwoClients();
            MoveOrgans(_serverBody, step: 1);
            yield return WaitForSameLocalPoses(_bodyA, "client A before re-rooting");

            Vector3 hostBefore = _serverBody.Organs[OrganIndex].transform.position;
            Reroot(_serverBody, new Vector3(2f, 0.74f, 3f), new Vector3(2f, 1.6f, 0f));
            Reroot(_bodyA, new Vector3(-1.5f, 0.9f, 2f), new Vector3(-1.5f, 1.6f, -1f));
            Reroot(_bodyB, new Vector3(0.3f, 1.1f, -4f), new Vector3(3f, 1.6f, -4f));

            // The server keeps editing the body after every peer has moved its copy.
            MoveOrgans(_serverBody, step: 2);
            yield return WaitForSameLocalPoses(_bodyA, "client A after re-rooting");
            yield return WaitForSameLocalPoses(_bodyB, "client B after re-rooting");

            Assert.Greater(Vector3.Distance(hostBefore, _serverBody.Organs[OrganIndex].transform.position), 1f, "The host root did not move.");
            Assert.Greater(Vector3.Distance(_serverBody.Organs[OrganIndex].transform.position, _bodyA.Organs[OrganIndex].transform.position), 1f,
                "The peers should sit at different places in the world, otherwise this test proves nothing.");
        }

        [UnityTest]
        public IEnumerator ReRootingAfterJoinKeepsLateJoinerInSync()
        {
            yield return StartHostAndSpawnBody();
            Reroot(_serverBody, new Vector3(1f, 0.74f, 2f), Vector3.zero);
            MoveOrgans(_serverBody, step: 1);

            NetworkManager late = StartClient("LateClient");
            SharedBody lateBody = null;
            yield return WaitForBody(late, body => lateBody = body);
            Reroot(lateBody, new Vector3(-3f, 0.8f, 1f), new Vector3(-3f, 1.6f, -2f));
            yield return WaitForSameLocalPoses(lateBody, "the late joiner");
        }

        [UnityTest]
        public IEnumerator GrabStillMovesTheOrganForEveryoneAfterRerooting()
        {
            yield return StartHostAndTwoClients();
            Reroot(_serverBody, new Vector3(2f, 0.74f, 3f), new Vector3(2f, 1.6f, 0f));
            Reroot(_bodyA, new Vector3(-1.5f, 0.9f, 2f), new Vector3(-1.5f, 1.6f, -1f));
            Reroot(_bodyB, new Vector3(0.3f, 1.1f, -4f), new Vector3(3f, 1.6f, -4f));

            OrganGrabAuthority authorityA = _bodyA.GetComponent<OrganGrabAuthority>();
            OrganGrabAuthority server = _serverBody.GetComponent<OrganGrabAuthority>();
            yield return WaitFor(() => authorityA.IsSpawned && _bodyB.GetComponent<OrganGrabAuthority>().IsSpawned, "the grab authority to spawn on both clients");

            bool answered = false;
            bool granted = false;
            authorityA.GrabResolved += (index, result) =>
            {
                if (index != OrganIndex)
                    return;
                answered = true;
                granted = result;
            };
            authorityA.RequestGrab(OrganIndex);
            yield return WaitFor(() => answered, "the server to answer the grab request");
            Assert.IsTrue(granted, "The grab should be granted although every peer moved its root.");

            // The holder carries the organ in body space, wherever its own table is.
            Vector3 position = _serverBody.Organs[OrganIndex].transform.localPosition + new Vector3(0.2f, 0.1f, -0.15f);
            Quaternion rotation = Quaternion.Euler(10f, 40f, 5f);
            authorityA.SendPose(OrganIndex, position, rotation);
            yield return WaitFor(() => Near(_serverBody, position, rotation), "the server organ to move");
            yield return WaitFor(() => Near(_bodyB, position, rotation), "client B to see the organ move");

            authorityA.Release(OrganIndex);
            yield return WaitFor(() => server.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "the server to free the organ");
        }

        [UnityTest]
        public IEnumerator PinPlacedAtAWorldPointLandsOnTheSameSpotOfTheOrganEverywhere()
        {
            yield return StartHostAndTwoClients();
            Reroot(_serverBody, new Vector3(2f, 0.74f, 3f), new Vector3(2f, 1.6f, 0f));
            Reroot(_bodyA, new Vector3(-1.5f, 0.9f, 2f), new Vector3(-1.5f, 1.6f, -1f));
            Reroot(_bodyB, new Vector3(0.3f, 1.1f, -4f), new Vector3(3f, 1.6f, -4f));

            QuestionBoard server = _serverBody.GetComponent<QuestionBoard>();
            QuestionBoard boardA = _bodyA.GetComponent<QuestionBoard>();
            QuestionBoard boardB = _bodyB.GetComponent<QuestionBoard>();

            // A pin is aimed at a world point on client A's organ, then sent as an organ-local point, as the pin input does.
            Transform organA = _bodyA.Organs[PinOrganIndex].transform;
            MeshFilter filter = organA.GetComponentInChildren<MeshFilter>();
            Vector3 worldPoint = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
            Vector3 localPoint = organA.InverseTransformPoint(worldPoint);
            boardA.RequestPin(PinOrganIndex, localPoint, "why?");

            foreach (QuestionBoard board in new[] { server, boardA, boardB })
            {
                QuestionBoard waiting = board;
                yield return WaitFor(() => waiting.Count == 1 && waiting.GetComponent<QuestionPinPresenter>().Markers.Count >= 1, $"the pin on {board.name}");
            }

            Assert.AreEqual(0f, Vector3.Distance(worldPoint, boardA.GetComponent<QuestionPinPresenter>().Markers[0].transform.position), 0.002f,
                "On the asking client the marker must sit where it was aimed.");
            foreach (QuestionBoard board in new[] { server, boardA, boardB })
            {
                Assert.AreEqual(0f, Vector3.Distance(localPoint, board[0].LocalPoint), 0.002f);
                Transform marker = board.GetComponent<QuestionPinPresenter>().Markers[0].transform;
                Assert.AreSame(board.Body.Organs[PinOrganIndex].transform, marker.parent);
                Assert.AreEqual(0f, Vector3.Distance(localPoint, marker.localPosition), 0.002f);
            }
        }

        private IEnumerator StartHostAndTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            yield return WaitForBody(_managerA, body => _bodyA = body);
            yield return WaitForBody(_managerB, body => _bodyB = body);
            yield return WaitForSameLocalPoses(_bodyA, "client A initial pose");
            yield return WaitForSameLocalPoses(_bodyB, "client B initial pose");
        }

        // Moves one peer's whole body the way the table alignment moves the scene content root.
        private static void Reroot(SharedBody body, Vector3 tableHit, Vector3 viewer)
        {
            Pose pose = TableAlignmentMath.RootPoseForHit(tableHit, _tableTopLocal, viewer, Quaternion.identity);
            body.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private static bool Near(SharedBody body, Vector3 position, Quaternion rotation)
        {
            Transform organ = body.Organs[OrganIndex].transform;
            return Vector3.Distance(organ.localPosition, position) < PositionTolerance && Quaternion.Angle(organ.localRotation, rotation) < RotationToleranceDegrees;
        }

        private static void MoveOrgans(SharedBody body, int step)
        {
            for (int i = 0; i < body.Organs.Count; i++)
            {
                Transform organ = body.Organs[i].transform;
                organ.localPosition += new Vector3(0.1f * step, 0.002f * i, -0.03f * step);
                organ.localRotation *= Quaternion.Euler(3f * step, 1f * i, 0f);
            }
        }

        private IEnumerator WaitForSameLocalPoses(SharedBody actual, string label)
        {
            string lastDifference = "";
            yield return WaitFor(() =>
            {
                lastDifference = DescribeDifferences(_serverBody, actual);
                return lastDifference.Length == 0;
            }, label, () => lastDifference);
        }

        private static string DescribeDifferences(SharedBody expected, SharedBody actual)
        {
            for (int i = 0; i < expected.Organs.Count; i++)
            {
                Transform want = expected.Organs[i].transform;
                Transform got = actual.Organs[i].transform;
                float distance = Vector3.Distance(want.localPosition, got.localPosition);
                float angle = Quaternion.Angle(want.localRotation, got.localRotation);
                if (distance > PositionTolerance || angle > RotationToleranceDegrees)
                    return $"{want.name}: server {want.localPosition.ToString("F4")} peer {got.localPosition.ToString("F4")}, position off by {distance:F4} m, rotation off by {angle:F2} deg";
            }
            return "";
        }
    }
}
