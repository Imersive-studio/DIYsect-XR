// Starts a real in-process Netcode host and client over UnityTransport on localhost (no cloud services).
// Under test: the cut line is presentation only and follows the server-owned incision progress on every peer.
using System.Collections;
using DIYsect.Dissection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class IncisionLineTests : NetworkedBodyTestBase
    {
        private const float SkinTolerance = 0.01f;

        [UnityTest]
        public IEnumerator CutLineGrowsWithTheIncisionOnEveryPeerAndHidesWhenTheSkinOpens()
        {
            yield return StartHostAndSpawnBody();
            var server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());
            LineRenderer serverLine = Line(server);
            LineRenderer clientLine = Line(clientProgress);

            Assert.IsFalse(serverLine.enabled, "Nothing is cut yet, so no line shows.");
            Assert.IsFalse(clientLine.enabled);

            server.EvaluateSample(OnLine(server, 0.1f));
            server.EvaluateSample(OnLine(server, 0.25f));
            yield return WaitFor(() => clientLine.enabled && Mathf.Approximately(clientProgress.IncisionProgress, 0.25f),
                "the client to draw the first quarter of the cut");
            Vector3 tip = clientLine.GetPosition(clientLine.positionCount - 1);
            Vector3 expected = OnLine(server, 0.25f);
            Assert.AreEqual(expected.z, tip.z, SkinTolerance, "The line ends where the blade has reached along the midline.");
            Assert.AreEqual(0f, tip.x, 0.001f, "The line stays on the midline.");
            Assert.That(tip.y, Is.GreaterThan(Mathf.Min(server.LineStart.y, server.LineEnd.y) - SkinTolerance),
                "The line sits on the skin, not inside the body.");

            for (float t = 0.4f; t <= 1.0001f; t += 0.15f)
                server.EvaluateSample(OnLine(server, Mathf.Min(t, 1f)));
            server.EvaluateSample(server.LineEnd);
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.Incised, "the client to see the incision complete");
            Assert.IsTrue(clientLine.enabled, "A finished cut stays visible until the skin opens.");
            Assert.AreEqual(server.LineEnd.z, clientLine.GetPosition(clientLine.positionCount - 1).z, SkinTolerance);

            Assert.IsTrue(server.TryOpen());
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.SkinOpen, "the client to see the skin open");
            Assert.IsFalse(serverLine.enabled, "Once the flaps open, their edges show the cut.");
            Assert.IsFalse(clientLine.enabled);
        }

        [UnityTest]
        public IEnumerator LateJoiningClientSeesTheCutSoFar()
        {
            yield return StartHostAndSpawnBody();
            var server = _serverBody.GetComponent<DissectionProgress>();
            server.EvaluateSample(OnLine(server, 0.15f));
            server.EvaluateSample(OnLine(server, 0.3f));

            DissectionProgress late = null;
            NetworkManager client = StartClient("LateClient");
            yield return WaitForBody(client, body => late = body.GetComponent<DissectionProgress>());
            LineRenderer line = Line(late);
            yield return WaitFor(() => line.enabled, "the late joiner to draw the existing cut");
            Assert.AreEqual(OnLine(server, 0.3f).z, line.GetPosition(line.positionCount - 1).z, SkinTolerance);
        }

        private static LineRenderer Line(DissectionProgress progress)
        {
            var presenter = progress.GetComponentInChildren<IncisionLinePresenter>(true);
            Assert.IsNotNull(presenter, "The body needs an IncisionLinePresenter. Run Tools > DIYsect > Add Body Dissection.");
            return presenter.GetComponent<LineRenderer>();
        }

        private static Vector3 OnLine(DissectionProgress progress, float fraction)
        {
            return Vector3.Lerp(progress.LineStart, progress.LineEnd, fraction);
        }
    }
}
