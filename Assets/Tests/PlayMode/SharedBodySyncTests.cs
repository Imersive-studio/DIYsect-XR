// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server moves the organs, the clients only receive them.
using System;
using System.Collections;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class SharedBodySyncTests : NetworkedBodyTestBase
    {
        private const float PositionTolerance = 0.005f;
        private const float RotationToleranceDegrees = 0.5f;

        [UnityTest]
        public IEnumerator TwoClientsSeeTheSameOrgansAfterMovement()
        {
            yield return StartHostAndSpawnBody();
            NetworkManager clientA = StartClient("ClientA");
            NetworkManager clientB = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(clientA, body => bodyA = body);
            yield return WaitForBody(clientB, body => bodyB = body);

            yield return WaitForSamePoses(_serverBody, bodyA, "client A initial pose");
            yield return WaitForSamePoses(_serverBody, bodyB, "client B initial pose");

            MoveOrgans(_serverBody, step: 1);
            yield return WaitForSamePoses(_serverBody, bodyA, "client A after movement");
            yield return WaitForSamePoses(_serverBody, bodyB, "client B after movement");
            AssertOrgansMoved(bodyA);
        }

        [UnityTest]
        public IEnumerator ReconnectedClientReceivesTheCurrentOrganPoses()
        {
            yield return StartHostAndSpawnBody();
            NetworkManager client = StartClient("ClientBeforeDisconnect");
            SharedBody clientBody = null;
            yield return WaitForBody(client, body => clientBody = body);
            MoveOrgans(_serverBody, step: 1);
            yield return WaitForSamePoses(_serverBody, clientBody, "client before disconnect");

            _clients.Remove(client);
            DestroyManager(client);
            yield return WaitFor(() => _host.ConnectedClientsIds.Count == 1, "the client to leave the host");

            // The body keeps moving while the client is away.
            MoveOrgans(_serverBody, step: 2);

            NetworkManager returned = StartClient("ClientAfterReconnect");
            SharedBody returnedBody = null;
            yield return WaitForBody(returned, body => returnedBody = body);
            yield return WaitForSamePoses(_serverBody, returnedBody, "reconnected client");
            AssertOrgansMoved(returnedBody);
        }

        // Moves every organ in local space. Different organs get different offsets so a mix-up is caught.
        private static void MoveOrgans(SharedBody body, int step)
        {
            for (int i = 0; i < body.Organs.Count; i++)
            {
                Transform organ = body.Organs[i].transform;
                organ.localPosition += new Vector3(0.1f * step, 0.002f * i, -0.03f * step);
                organ.localRotation *= Quaternion.Euler(3f * step, 1f * i, 0f);
            }
        }

        private IEnumerator WaitForSamePoses(SharedBody expected, SharedBody actual, string label)
        {
            string lastDifference = "";
            yield return WaitFor(() =>
            {
                lastDifference = DescribeDifferences(expected, actual);
                return lastDifference.Length == 0;
            }, label, () => lastDifference);
        }

        private static string DescribeDifferences(SharedBody expected, SharedBody actual)
        {
            var report = new StringBuilder();
            if (expected.Organs.Count != actual.Organs.Count)
                return $"organ count {expected.Organs.Count} vs {actual.Organs.Count}";
            for (int i = 0; i < expected.Organs.Count; i++)
            {
                Transform want = expected.Organs[i].transform;
                Transform got = actual.Organs[i].transform;
                float distance = Vector3.Distance(want.localPosition, got.localPosition);
                float angle = Quaternion.Angle(want.localRotation, got.localRotation);
                if (distance > PositionTolerance || angle > RotationToleranceDegrees)
                    report.AppendLine($"{want.name}: server {want.localPosition.ToString("F4")} client {got.localPosition.ToString("F4")}, position off by {distance:F4} m, rotation off by {angle:F2} deg");
            }
            return report.ToString();
        }

        // Guards against a test that passes only because nothing ever moved: the client must differ from the prefab layout.
        private void AssertOrgansMoved(SharedBody actual)
        {
            SharedBody original = _bodyPrefab.GetComponent<SharedBody>();
            for (int i = 0; i < original.Organs.Count; i++)
            {
                float moved = Vector3.Distance(original.Organs[i].transform.localPosition, actual.Organs[i].transform.localPosition);
                Assert.Greater(moved, PositionTolerance, $"{actual.Organs[i].name} still sits at its prefab position.");
            }
        }

    }
}
