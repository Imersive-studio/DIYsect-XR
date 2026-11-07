// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone decides who draws and which points are kept.
// Clients only ask by Rpc; every peer then holds the same body-local line.
using System.Collections;
using DIYsect.Interaction;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class GuideLineTests : NetworkedBodyTestBase
    {
        private const float Tolerance = 0.001f;

        private GuideLine _server;
        private GuideLine _lineA;
        private GuideLine _lineB;
        private SharedBody _bodyA;
        private SharedBody _bodyB;
        private NetworkManager _managerA;
        private NetworkManager _managerB;

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<GuideLine>();
            Assert.IsNotNull(_server, "The body prefab has no GuideLine. Run Tools > DIYsect > Add Guide Line.");

            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            yield return WaitForBody(_managerA, found => _bodyA = found);
            yield return WaitForBody(_managerB, found => _bodyB = found);
            _lineA = _bodyA.GetComponent<GuideLine>();
            _lineB = _bodyB.GetComponent<GuideLine>();
        }

        // A point in the air over the body, in world space of the given body copy.
        private static Vector3 WorldPoint(SharedBody body, float x)
        {
            return body.transform.TransformPoint(new Vector3(x, 0.2f, 0f));
        }

        // Asks for a stroke and waits for the server's answer.
        private IEnumerator Start(GuideLine line, bool expected)
        {
            bool? answer = null;
            void OnResolved(bool granted) => answer = granted;
            line.StrokeResolved += OnResolved;
            line.StartStroke();
            yield return WaitFor(() => answer.HasValue, $"{line.name} to hear the answer to its stroke request");
            line.StrokeResolved -= OnResolved;
            Assert.AreEqual(expected, answer.Value);
        }

        private IEnumerator Draw(GuideLine line, SharedBody body, int count)
        {
            int target = _server.Count + count;
            for (int i = 0; i < count; i++)
                line.AddPoint(WorldPoint(body, 0.05f * (_server.Count + i + 1)));
            yield return WaitFor(() => _server.Count == target, $"{count} points on the server", () => $"Has {_server.Count}.");
        }

        private static void AssertSamePoints(GuideLine expected, GuideLine actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
                Assert.IsTrue((expected[i] - actual[i]).magnitude < Tolerance, $"Point {i} differs.");
        }

        [UnityTest]
        public IEnumerator StrokeIsGrantedAndPointsAppearEverywhereInBodyLocalSpace()
        {
            yield return StartTwoClients();
            yield return Start(_lineA, true);

            var sent = new[] { WorldPoint(_bodyA, 0.1f), WorldPoint(_bodyA, 0.2f), WorldPoint(_bodyA, 0.3f) };
            foreach (Vector3 point in sent)
                _lineA.AddPoint(point);

            yield return WaitFor(() => _server.Count == 3 && _lineA.Count == 3 && _lineB.Count == 3, "three points on every peer",
                () => $"Server {_server.Count}, A {_lineA.Count}, B {_lineB.Count}.");

            foreach (GuideLine line in new[] { _server, _lineA, _lineB })
            {
                for (int i = 0; i < sent.Length; i++)
                {
                    Vector3 expected = _bodyA.transform.InverseTransformPoint(sent[i]);
                    Assert.IsTrue((line[i] - expected).magnitude < Tolerance, $"Point {i} on {line.name} is not where it was drawn.");
                }
            }
            Assert.AreEqual(_managerA.LocalClientId, _server.Drawer);
        }

        [UnityTest]
        public IEnumerator SecondClientIsRefusedWhileAnotherIsDrawing()
        {
            yield return StartTwoClients();
            yield return Start(_lineA, true);
            yield return Draw(_lineA, _bodyA, 2);

            yield return Start(_lineB, false);
            Assert.AreEqual(_managerA.LocalClientId, _server.Drawer, "The drawer must not change.");

            _lineB.AddPoint(WorldPoint(_bodyB, 0.9f));
            _lineB.AddPoint(WorldPoint(_bodyB, 0.95f));
            // Messages from one sender arrive in order, so once A's next point is in, B's earlier ones have been handled.
            _lineA.AddPoint(WorldPoint(_bodyA, 0.5f));
            yield return WaitFor(() => _server.Count == 3, "A's next point", () => $"Has {_server.Count}.");
            Assert.AreEqual(3, _server.Count, "B's points must be ignored.");
        }

        [UnityTest]
        public IEnumerator InvalidPointsFromTheDrawerAreIgnored()
        {
            yield return StartTwoClients();
            yield return Start(_lineA, true);
            yield return Draw(_lineA, _bodyA, 2);
            Vector3 lastLocal = _server[1];
            Vector3 lastWorld = _bodyA.transform.TransformPoint(lastLocal);

            var verdict = _server.ServerAddPoint(_managerA.LocalClientId, new Vector3(float.NaN, 0f, 0f));
            Assert.AreEqual(GuideStrokeRules.Verdict.NotFinite, verdict);

            _lineA.AddPoint(_bodyA.transform.TransformPoint(new Vector3(3f, 0f, 0f)));
            _lineA.AddPoint(lastWorld + _bodyA.transform.TransformDirection(new Vector3(0.001f, 0f, 0f)));
            // A valid point sent afterwards proves the invalid ones before it were handled and dropped.
            _lineA.AddPoint(WorldPoint(_bodyA, 0.7f));
            yield return WaitFor(() => _server.Count == 3, "the valid point after the invalid ones", () => $"Has {_server.Count}.");
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(3, _server.Count, "Only the valid point may be added.");
        }

        [UnityTest]
        public IEnumerator EndingTheStrokeFreesTheDrawerAndTheLineStaysUntilReplaced()
        {
            yield return StartTwoClients();
            yield return Start(_lineA, true);
            yield return Draw(_lineA, _bodyA, 3);

            _lineA.EndStroke();
            yield return WaitFor(() => _server.Drawer == GuideLine.NoDrawer, "the drawer to be freed", () => $"Drawer is {_server.Drawer}.");
            Assert.AreEqual(3, _server.Count, "The line must stay after the stroke ends.");
            yield return WaitFor(() => _lineB.Count == 3, "client B to still see the line");

            yield return Start(_lineB, true);
            yield return WaitFor(() => _server.Count == 0, "the new stroke to replace the line", () => $"Has {_server.Count}.");
            Assert.AreEqual(_managerB.LocalClientId, _server.Drawer);
        }

        [UnityTest]
        public IEnumerator DisconnectingTheDrawerFreesTheDrawerAndTheLineStays()
        {
            yield return StartTwoClients();
            yield return Start(_lineA, true);
            yield return Draw(_lineA, _bodyA, 3);

            _managerA.Shutdown();

            yield return WaitFor(() => _server.Drawer == GuideLine.NoDrawer, "the drawer to be freed after the disconnect", () => $"Drawer is {_server.Drawer}.");
            Assert.AreEqual(3, _server.Count, "The line must stay after the drawer leaves.");
            yield return WaitFor(() => _lineB.Count == 3, "client B to still see the line");
        }

        [UnityTest]
        public IEnumerator OnlyTheDrawerOrTheHostCanClearTheLine()
        {
            yield return StartTwoClients();
            yield return Start(_lineA, true);
            yield return Draw(_lineA, _bodyA, 3);

            // B is neither the drawer nor the host. B's Clear is sent first, then A's point proves it was handled.
            _lineB.Clear();
            _lineA.AddPoint(WorldPoint(_bodyA, 0.8f));
            yield return WaitFor(() => _server.Count == 4, "A's next point", () => $"Has {_server.Count}.");
            Assert.AreEqual(4, _server.Count, "Another client must not clear the line.");
            Assert.AreEqual(_managerA.LocalClientId, _server.Drawer);

            _server.Clear();
            yield return WaitFor(() => _server.Count == 0 && _lineA.Count == 0 && _lineB.Count == 0, "the host to clear the line on every peer",
                () => $"Server {_server.Count}, A {_lineA.Count}, B {_lineB.Count}.");
        }

        [UnityTest]
        public IEnumerator LateJoinerReceivesTheExistingLineAndDrawsIt()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<GuideLine>();
            Assert.IsNotNull(_server, "The body prefab has no GuideLine. Run Tools > DIYsect > Add Guide Line.");
            _managerA = StartClient("ClientA");
            yield return WaitForBody(_managerA, found => _bodyA = found);
            _lineA = _bodyA.GetComponent<GuideLine>();
            yield return Start(_lineA, true);
            yield return Draw(_lineA, _bodyA, 4);

            NetworkManager late = StartClient("LateClient");
            SharedBody lateBody = null;
            yield return WaitForBody(late, found => lateBody = found);
            GuideLine lateLine = lateBody.GetComponent<GuideLine>();
            GuideLinePresenter presenter = lateBody.GetComponentInChildren<GuideLinePresenter>(true);
            Assert.IsNotNull(presenter, "The body prefab has no GuideLinePresenter. Run Tools > DIYsect > Add Guide Line.");
            LineRenderer renderer = presenter.GetComponent<LineRenderer>();

            yield return WaitFor(() => lateLine.Count == 4 && renderer.positionCount == 4, "the late joiner to receive and draw the line",
                () => $"Has {lateLine.Count} points and {renderer.positionCount} drawn.");
            AssertSamePoints(_server, lateLine);
            Assert.IsTrue(renderer.enabled, "A line of two or more points must be visible.");
        }
    }
}
