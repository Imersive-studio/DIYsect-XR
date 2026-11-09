// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone advances the incision and opens the skin. Clients only submit samples and requests.
using System.Collections;
using System.Reflection;
using DIYsect.Dissection;
using DIYsect.Interaction;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DIYsect.Networking.Tests
{
    public class DissectionTests : NetworkedBodyTestBase
    {
        private const string ScalpelPrefabPath = "Assets/Prefabs/ScalpelPlaceholder.prefab";
        private const float AngleTolerance = 1f;
        private const float QuietSeconds = 0.5f;

        private DissectionProgress _server;
        private GameObject _scalpel;

        [TearDown]
        public void DestroyScalpel()
        {
            if (_scalpel != null)
                Object.Destroy(_scalpel);
        }

        [UnityTest]
        public IEnumerator SamplesAlongTheMidlineAdvanceProgressAndBadOnesAreRejected()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();

            Assert.AreEqual(SampleResult.Accepted, _server.EvaluateSample(OnLine(0.1f)));
            Assert.AreEqual(0.1f, _server.IncisionProgress, 0.001f);

            Assert.AreEqual(SampleResult.Accepted, _server.EvaluateSample(OnLine(0.25f)));
            Assert.AreEqual(SampleResult.Ignored, _server.EvaluateSample(OnLine(0.15f)), "Going backward must not move the progress.");
            Assert.AreEqual(SampleResult.OffLine, _server.EvaluateSample(OnLine(0.3f) + Vector3.right * 0.1f), "A sample off the midline must be rejected.");
            Assert.AreEqual(SampleResult.TooFar, _server.EvaluateSample(OnLine(0.8f)), "A jump beyond the maximum step must be rejected.");
            Assert.AreEqual(SampleResult.Invalid, _server.EvaluateSample(new Vector3(float.NaN, 0f, 0f)));
            Assert.AreEqual(0.25f, _server.IncisionProgress, 0.001f, "Rejected samples must leave the progress untouched.");
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);

            Complete(_server);
            Assert.AreEqual(1f, _server.IncisionProgress, 0.0001f);
            Assert.AreEqual(DissectionStage.Incised, _server.Stage);
            Assert.AreEqual(SampleResult.WrongStage, _server.EvaluateSample(OnLine(0.5f)));
        }

        [UnityTest]
        public IEnumerator FlapsStayClosedUntilTheIncisionIsCompleteThenOpenOnEveryPeer()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());
            SkinFlapHinge[] serverHinges = _serverBody.GetComponentsInChildren<SkinFlapHinge>();
            SkinFlapHinge[] clientHinges = clientProgress.GetComponentsInChildren<SkinFlapHinge>();
            Assert.AreEqual(2, serverHinges.Length, "The body needs one hinge per skin flap.");
            Vector3 closedCentre = FlapCentre(_serverBody, serverHinges[0]);

            // Before completion: a half-finished cut and an open request change nothing.
            _server.EvaluateSample(OnLine(0.15f));
            clientProgress.RequestOpen();
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);
            AssertFlaps(serverHinges, open: false);
            AssertFlaps(clientHinges, open: false);

            Complete(_server);
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.Incised, "the client to see the incision complete");
            yield return new WaitForSeconds(QuietSeconds);
            AssertFlaps(clientHinges, open: false);

            clientProgress.RequestOpen();
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.SkinOpen, "the client to see the skin open");
            yield return WaitFor(() => FlapsAre(serverHinges, open: true) && FlapsAre(clientHinges, open: true), "the flaps to swing open on server and client");

            // The flap swings outward: its middle moves away from the midline.
            Vector3 openCentre = FlapCentre(_serverBody, serverHinges[0]);
            Assert.Greater(Mathf.Abs(openCentre.x), Mathf.Abs(closedCentre.x) + 0.02f, "The flap should open away from the midline.");
        }

        [UnityTest]
        public IEnumerator LateJoiningClientSeesTheOpenSkin()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            Complete(_server);
            Assert.IsTrue(_server.TryOpen());

            NetworkManager late = StartClient("LateClient");
            DissectionProgress lateProgress = null;
            yield return WaitForBody(late, body => lateProgress = body.GetComponent<DissectionProgress>());
            yield return WaitFor(() => lateProgress.Stage == DissectionStage.SkinOpen, "the late client to see SkinOpen");
            Assert.AreEqual(1f, lateProgress.IncisionProgress, 0.0001f);
            // Joined state is applied at once, without waiting for a swing animation. The hinges can lag the stage by a
            // frame, so wait for them instead of reading them the moment the stage arrives.
            SkinFlapHinge[] hinges = lateProgress.GetComponentsInChildren<SkinFlapHinge>();
            yield return WaitFor(() => FlapsAre(hinges, open: true), "the late client's flaps to be open");
            AssertFlaps(hinges, open: true);
        }

        [UnityTest]
        public IEnumerator LateJoiningClientSeesTheIncisedStateWithClosedFlaps()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            Complete(_server);

            NetworkManager late = StartClient("LateClient");
            DissectionProgress lateProgress = null;
            yield return WaitForBody(late, body => lateProgress = body.GetComponent<DissectionProgress>());
            yield return WaitFor(() => lateProgress.Stage == DissectionStage.Incised, "the late client to see Incised");
            AssertFlaps(lateProgress.GetComponentsInChildren<SkinFlapHinge>(), open: false);
        }

        [UnityTest]
        public IEnumerator ClientsCannotSetProgressDirectlyOnlyThroughValidatedSamples()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());

            // The variables are server-write only: Netcode refuses a client write with an error and changes nothing.
            FieldInfo progressField = typeof(DissectionProgress).GetField("_progress", BindingFlags.NonPublic | BindingFlags.Instance);
            var progress = (NetworkVariable<float>)progressField.GetValue(clientProgress);
            // Netcode logs its own error for the refused write; ignore log text for this one attempt only.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                progress.Value = 1f;
            }
            catch (System.InvalidOperationException)
            {
                // Some Netcode versions throw instead of logging. Either way the server value must not change.
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
            Assert.AreEqual(0f, _server.IncisionProgress, 0.0001f);

            // A sample far off the midline goes through the real Rpc path and is rejected.
            clientProgress.SubmitContact(_serverBody.transform.TransformPoint(OnLine(0.1f) + Vector3.up * 0.5f));
            // A jump ahead is rejected too.
            clientProgress.SubmitContact(_serverBody.transform.TransformPoint(OnLine(0.9f)));
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(0f, _server.IncisionProgress, 0.0001f);

            // A valid sample from the client does advance it.
            clientProgress.SubmitContact(_serverBody.transform.TransformPoint(OnLine(0.1f)));
            yield return WaitFor(() => _server.IncisionProgress > 0.09f, "the valid client sample to be accepted");
            yield return WaitFor(() => clientProgress.IncisionProgress > 0.09f, "the client to see the progress");
        }

        [UnityTest]
        public IEnumerator ScalpelTracesTheMidlineAndTheFlapsOpenOnPeers()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());
            SkinFlapHinge[] clientHinges = clientProgress.GetComponentsInChildren<SkinFlapHinge>();

            _scalpel = Object.Instantiate(LoadScalpelPrefab());
            var tool = _scalpel.GetComponent<ScalpelTool>();
            Assert.IsNotNull(tool, "The scalpel prefab needs a ScalpelTool.");

            // Start well away from the body, then touch the start of the midline.
            PlaceTip(tool, _serverBody.transform.TransformPoint(OnLine(0f)) + Vector3.up * 0.5f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0f, _server.IncisionProgress, 0.0001f);

            for (float t = 0.1f; t <= 1.0001f; t += 0.1f)
            {
                float step = t;
                PlaceTip(tool, _serverBody.transform.TransformPoint(OnLine(step)));
                yield return new WaitForFixedUpdate();
                yield return WaitFor(() => _server.IncisionProgress >= Mathf.Min(step, 0.98f) - 0.01f, $"the scalpel to advance the incision to {step:F1}", () => $"progress {_server.IncisionProgress}");
            }

            yield return WaitFor(() => _server.Stage == DissectionStage.SkinOpen, "the scalpel to complete the cut and open the skin", () => $"stage {_server.Stage}, progress {_server.IncisionProgress}");
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.SkinOpen, "the client to see the skin open");
            yield return WaitFor(() => FlapsAre(clientHinges, open: true), "the flaps to open on the client");
        }

        [UnityTest]
        public IEnumerator CutCarriesOnWhenTheGrabZoneLeavesTheIncisionZoneButTheTipStaysIn()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            _scalpel = Object.Instantiate(LoadScalpelPrefab());
            var tool = _scalpel.GetComponent<ScalpelTool>();
            Transform grabZone = tool.transform.Find("GrabZone");
            Assert.IsNotNull(grabZone, "The scalpel prefab needs its GrabZone.");

            // Tip and grab zone both enter the incision zone.
            grabZone.position = tool.Tip.position;
            PlaceTip(tool, _serverBody.transform.TransformPoint(OnLine(0.1f)));
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => _server.IncisionProgress > 0.09f, "the scalpel to start the cut");

            // Only the grab zone leaves, as when the handle tilts away while the tip keeps cutting.
            grabZone.position += Vector3.up * 0.5f;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            PlaceTip(tool, _serverBody.transform.TransformPoint(OnLine(0.2f)));
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => _server.IncisionProgress > 0.19f, "the cut to carry on with the tip still in the zone", () => $"progress {_server.IncisionProgress}");
        }

        [UnityTest]
        public IEnumerator ScalpelThatLeftTheZoneDoesNotOpenTheSkinForSomeoneElsesCut()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            _scalpel = Object.Instantiate(LoadScalpelPrefab());
            var tool = _scalpel.GetComponent<ScalpelTool>();

            PlaceTip(tool, _serverBody.transform.TransformPoint(OnLine(0.1f)));
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => _server.IncisionProgress > 0.09f, "the scalpel to start the cut");

            // Lift the scalpel out of the zone, then let someone else finish the cut.
            PlaceTip(tool, _serverBody.transform.TransformPoint(OnLine(0.1f)) + Vector3.up * 0.5f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Complete(_server);
            Assert.AreEqual(DissectionStage.Incised, _server.Stage);
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(DissectionStage.Incised, _server.Stage, "A scalpel that left the zone must not open the skin.");
        }

        // A point on the midline at the given fraction, in body-local space.
        private Vector3 OnLine(float fraction)
        {
            return Vector3.Lerp(_server.LineStart, _server.LineEnd, fraction);
        }

        // Valid samples in steps the server accepts, up to the end of the line.
        private void Complete(DissectionProgress server)
        {
            for (float t = 0.1f; t <= 1.0001f; t += 0.1f)
                server.EvaluateSample(Vector3.Lerp(server.LineStart, server.LineEnd, Mathf.Min(t, 1f)));
        }

        private static void PlaceTip(ScalpelTool tool, Vector3 worldPoint)
        {
            tool.transform.position += worldPoint - tool.Tip.position;
        }

        private static void AssertFlaps(SkinFlapHinge[] hinges, bool open)
        {
            Assert.AreEqual(2, hinges.Length);
            foreach (SkinFlapHinge hinge in hinges)
                Assert.IsTrue(FlapIsAt(hinge, open), $"{hinge.name} should be {(open ? "open" : "closed")}.");
        }

        private static bool FlapsAre(SkinFlapHinge[] hinges, bool open)
        {
            foreach (SkinFlapHinge hinge in hinges)
                if (!FlapIsAt(hinge, open))
                    return false;
            return true;
        }

        private static bool FlapIsAt(SkinFlapHinge hinge, bool open)
        {
            Quaternion target = open ? hinge.OpenRotation : Quaternion.identity;
            return Quaternion.Angle(hinge.transform.localRotation, target) < AngleTolerance;
        }

        // Centre of the flap's renderers in body-local space.
        private static Vector3 FlapCentre(SharedBody body, SkinFlapHinge hinge)
        {
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in hinge.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return body.transform.InverseTransformPoint(bounds.center);
        }

        private static GameObject LoadScalpelPrefab()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScalpelPrefabPath);
            Assert.IsNotNull(prefab, $"{ScalpelPrefabPath} is missing. Run Tools > DIYsect > Add Placeholder Scalpel.");
            return prefab;
#else
            throw new System.NotSupportedException("These tests run in the editor only.");
#endif
        }
    }
}
