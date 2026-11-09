// Starts a real in-process Netcode host and a client over UnityTransport on localhost (no cloud services).
// Authority under test: the server owns the pins and the organ poses. A client that leaves and comes back on the same
// NetworkManager is a late joiner again, so it must see the state as it is now, including changes made while it was away.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class ReconnectRestoreTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;
        private const float PositionTolerance = 0.005f;

        private Vector3 PointOn(int organ)
        {
            Transform t = _serverBody.Organs[organ].transform;
            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            return t.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
        }

        [UnityTest]
        public IEnumerator AClientThatDisconnectsAndReconnectsSeesTheCurrentPinsAndOrgans()
        {
            yield return StartHostAndSpawnBody();
            QuestionBoard server = _serverBody.GetComponent<QuestionBoard>();
            NetworkManager client = StartClient("Reconnecting");
            NetworkManager other = StartClient("StaysConnected");
            SharedBody body = null;
            SharedBody otherBody = null;
            yield return WaitForBody(client, found => body = found);
            yield return WaitForBody(other, found => otherBody = found);

            // State before the drop: one pin, answered, and a moved organ.
            body.GetComponent<QuestionBoard>().RequestPin(OrganIndex, PointOn(OrganIndex), "first");
            yield return WaitFor(() => server.Count == 1, "the first pin on the server");
            server.RequestMarkAnswered(server[0].Id);
            _serverBody.Organs[OrganIndex].transform.localPosition += new Vector3(0.05f, 0f, 0f);
            yield return WaitFor(() => body.GetComponent<QuestionBoard>().Count == 1 && body.GetComponent<QuestionBoard>()[0].Answered, "the client to see the first pin answered");

            // The drop: the same NetworkManager shuts down, as the app does when the network goes away.
            client.Shutdown();
            yield return WaitFor(() => !client.IsListening && !client.IsConnectedClient, "the client to stop");
            yield return WaitFor(() => _host.ConnectedClientsIds.Count == 2, "the host to notice the client left");

            // The class carries on while the client is away.
            otherBody.GetComponent<QuestionBoard>().RequestPin(OrganIndex, PointOn(OrganIndex) + new Vector3(0.003f, 0f, 0f), "second");
            yield return WaitFor(() => server.Count == 2, "the second pin on the server");
            _serverBody.Organs[OrganIndex].transform.localPosition += new Vector3(0f, 0.04f, 0f);

            // The return.
            Assert.IsTrue(client.StartClient(), "The client could not start again on the same NetworkManager.");
            SharedBody returned = null;
            yield return WaitForBody(client, found => returned = found);
            QuestionBoard returnedBoard = returned.GetComponent<QuestionBoard>();
            yield return WaitFor(() => returnedBoard.Count == 2, "the returning client to see both pins", () => $"Has {returnedBoard.Count}.");

            Assert.IsTrue(returnedBoard[0].Answered, "The pin answered before the drop must still be answered.");
            Assert.IsFalse(returnedBoard[1].Answered);
            Assert.AreEqual("first", returnedBoard[0].Text.ToString());
            Assert.AreEqual("second", returnedBoard[1].Text.ToString());
            yield return WaitFor(() => Vector3.Distance(_serverBody.Organs[OrganIndex].transform.localPosition,
                returned.Organs[OrganIndex].transform.localPosition) < PositionTolerance, "the moved organ to match the server");
        }
    }
}
