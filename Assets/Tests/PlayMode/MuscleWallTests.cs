// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone moves the stage through skin, rectus and omentum, in that order.
// Clients and the forceps only ask by Rpc; out-of-order requests are rejected and change nothing.
using System.Collections;
using System.Collections.Generic;
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
    public class MuscleWallTests : NetworkedBodyTestBase
    {
        private const string ForcepsPrefabPath = "Assets/Prefabs/ForcepsPlaceholder.prefab";
        private const float AngleTolerance = 1f;
        private const float QuietSeconds = 0.5f;

        private DissectionProgress _server;
        private GameObject _forceps;

        [TearDown]
        public void DestroyForceps()
        {
            if (_forceps != null)
                Object.Destroy(_forceps);
        }

        [Test]
        public void LayerOrderRuleAllowsEachStepOnlyInItsPlace()
        {
            LayerOrderRule rule = LayerOrderRule.Default;
            Assert.AreEqual(StepResult.WrongOrder, rule.Evaluate(DissectionStage.Intact, DissectionAction.OpenRectus, out _));
            Assert.AreEqual(StepResult.WrongOrder, rule.Evaluate(DissectionStage.Incised, DissectionAction.OpenRectus, out _));
            Assert.AreEqual(StepResult.WrongOrder, rule.Evaluate(DissectionStage.SkinOpen, DissectionAction.LiftOmentum, out _));
            Assert.AreEqual(StepResult.Accepted, rule.Evaluate(DissectionStage.SkinOpen, DissectionAction.OpenRectus, out DissectionStage afterRectus));
            Assert.AreEqual(DissectionStage.RectusOpen, afterRectus);
            Assert.AreEqual(StepResult.Accepted, rule.Evaluate(DissectionStage.RectusOpen, DissectionAction.LiftOmentum, out DissectionStage afterOmentum));
            Assert.AreEqual(DissectionStage.OmentumLifted, afterOmentum);
            Assert.AreEqual(StepResult.AlreadyDone, rule.Evaluate(DissectionStage.OmentumLifted, DissectionAction.OpenRectus, out DissectionStage unchanged));
            Assert.AreEqual(DissectionStage.OmentumLifted, unchanged);
            Assert.AreEqual(StepResult.Unknown, rule.Evaluate(DissectionStage.SkinOpen, (DissectionAction)200, out _));
        }

        [UnityTest]
        public IEnumerator TouchTriggerWithoutTargetDisablesItselfAndForcepsIgnoreIt()
        {
            _forceps = Object.Instantiate(LoadForcepsPrefab());
            var tool = _forceps.GetComponent<ForcepsTool>();
            var zone = new GameObject("BrokenLayerTrigger");
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 0.2f;
            zone.transform.position = new Vector3(5f, 5f, 5f);
            // Awake runs on AddComponent and reports the missing target.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("has no DissectionProgress"));
            var trigger = zone.AddComponent<LayerTouchTrigger>();
            Assert.IsFalse(trigger.enabled, "A touch trigger without a target must disable itself.");

            PlaceTip(tool, zone.transform.position + Vector3.up * 0.6f);
            yield return new WaitForFixedUpdate();
            PlaceTip(tool, zone.transform.position);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(tool.enabled, "The forceps must keep working after touching a broken trigger.");
            Object.Destroy(zone);
        }

        [UnityTest]
        public IEnumerator RectusRequestIsRejectedBeforeSkinOpenAcceptedAfterAndHingesOpenOnPeers()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());
            LayerHinge[] serverRectus = HingesOf(_server, "rectus");
            LayerHinge[] clientRectus = HingesOf(clientProgress, "rectus");
            Assert.AreEqual(2, serverRectus.Length, "The body needs one hinge per rectus half.");
            var answers = new List<(DissectionAction, StepResult)>();
            clientProgress.StepResolved += (action, result) => answers.Add((action, result));

            // Before the skin is open, in every earlier stage, the request is refused and the client is told.
            clientProgress.RequestStep(DissectionAction.OpenRectus);
            yield return WaitFor(() => answers.Count == 1, "the answer to the early rectus request");
            Assert.AreEqual((DissectionAction.OpenRectus, StepResult.WrongOrder), answers[0]);
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);
            Complete(_server);
            clientProgress.RequestStep(DissectionAction.OpenRectus);
            yield return WaitFor(() => answers.Count == 2, "the answer to the rectus request after the incision");
            Assert.AreEqual(StepResult.WrongOrder, answers[1].Item2);
            Assert.AreEqual(DissectionStage.Incised, _server.Stage);
            yield return new WaitForSeconds(QuietSeconds);
            AssertHinges(serverRectus, open: false);
            AssertHinges(clientRectus, open: false);

            Assert.IsTrue(_server.TryOpen());
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.SkinOpen, "the client to see the skin open");
            clientProgress.RequestStep(DissectionAction.OpenRectus);
            yield return WaitFor(() => answers.Count == 3, "the answer to the rectus request after the skin opened");
            Assert.AreEqual(StepResult.Accepted, answers[2].Item2);
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.RectusOpen, "the client to see the rectus open");
            yield return WaitFor(() => HingesAre(serverRectus, true) && HingesAre(clientRectus, true), "the rectus halves to swing open on server and client");
            // The transversus stays where it is, and the omentum is still down.
            AssertHinges(HingesOf(clientProgress, "omentum"), open: false);
            Assert.AreEqual(0, HingesOf(clientProgress, "transversus").Length);
        }

        [UnityTest]
        public IEnumerator ForcepsLiftTheOmentumOnlyAfterTheRectusIsOpen()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());
            LayerHinge omentum = HingesOf(_server, "omentum")[0];
            LayerHinge clientOmentum = HingesOf(clientProgress, "omentum")[0];
            Complete(_server);
            Assert.IsTrue(_server.TryOpen());
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.SkinOpen, "the client to see the skin open");

            _forceps = Object.Instantiate(LoadForcepsPrefab());
            var tool = _forceps.GetComponent<ForcepsTool>();
            Assert.IsNotNull(tool, "The forceps prefab needs a ForcepsTool.");
            Assert.IsInstanceOf<ITool>(tool);
            var answers = new List<(DissectionAction, StepResult)>();
            tool.StepResolved += (action, result) => answers.Add((action, result));

            // Touching the omentum with the rectus still closed is refused; nothing changes.
            Vector3 away = _serverBody.transform.TransformPoint(OmentumCentre()) + Vector3.up * 0.6f;
            PlaceTip(tool, away);
            yield return new WaitForFixedUpdate();
            Vector3 centreBefore = RendererCentre(omentum);
            PlaceTip(tool, _serverBody.transform.TransformPoint(OmentumCentre()));
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => answers.Count >= 1, "the answer to the early lift");
            Assert.AreEqual((DissectionAction.LiftOmentum, StepResult.WrongOrder), answers[0]);
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(DissectionStage.SkinOpen, _server.Stage);
            Assert.IsFalse(HingeIsAt(omentum, true));

            // With the rectus open, the same touch lifts it on the server and on the client.
            PlaceTip(tool, away);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(StepResult.Accepted, _server.TryStep(DissectionAction.OpenRectus));
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.RectusOpen, "the client to see the rectus open");
            PlaceTip(tool, _serverBody.transform.TransformPoint(OmentumCentre()));
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => _server.Stage == DissectionStage.OmentumLifted, "the forceps to lift the omentum", () => $"stage {_server.Stage}");
            yield return WaitFor(() => clientProgress.Stage == DissectionStage.OmentumLifted, "the client to see the omentum lifted");
            yield return WaitFor(() => HingeIsAt(omentum, true) && HingeIsAt(clientOmentum, true), "the omentum to lift on server and client");
            Assert.Greater(RendererCentre(omentum).y, centreBefore.y + 0.02f, "The omentum should lift away from the organs.");
            Assert.Contains(StepResult.Accepted, answers.ConvertAll(a => a.Item2));
        }

        [UnityTest]
        public IEnumerator ForcepsOnTheRectusOpenItOnlyAfterTheSkinIsOpen()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            _forceps = Object.Instantiate(LoadForcepsPrefab());
            var tool = _forceps.GetComponent<ForcepsTool>();
            Transform rectusZone = _serverBody.transform.Find("RectusTrigger");
            Assert.IsNotNull(rectusZone, "The body needs a RectusTrigger. Run Tools > DIYsect > Add Body Dissection.");
            Vector3 onRectus = rectusZone.TransformPoint(rectusZone.GetComponent<BoxCollider>().center);
            var answers = new List<StepResult>();
            tool.StepResolved += (action, result) => answers.Add(result);

            PlaceTip(tool, onRectus + Vector3.up * 0.6f);
            yield return new WaitForFixedUpdate();
            PlaceTip(tool, onRectus);
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => answers.Count >= 1, "the answer to the early touch");
            Assert.AreEqual(StepResult.WrongOrder, answers[0]);
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);

            PlaceTip(tool, onRectus + Vector3.up * 0.6f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Complete(_server);
            Assert.IsTrue(_server.TryOpen());
            PlaceTip(tool, onRectus);
            yield return new WaitForFixedUpdate();
            yield return WaitFor(() => _server.Stage == DissectionStage.RectusOpen, "the forceps to open the rectus", () => $"stage {_server.Stage}");
        }

        [UnityTest]
        public IEnumerator LateJoiningClientSeesTheRectusOpenAndTheOmentumLifted()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            Complete(_server);
            Assert.IsTrue(_server.TryOpen());
            Assert.AreEqual(StepResult.Accepted, _server.TryStep(DissectionAction.OpenRectus));

            NetworkManager midway = StartClient("MidwayClient");
            DissectionProgress midwayProgress = null;
            yield return WaitForBody(midway, body => midwayProgress = body.GetComponent<DissectionProgress>());
            yield return WaitFor(() => midwayProgress.Stage == DissectionStage.RectusOpen, "the midway client to see RectusOpen");
            LayerHinge[] midwayRectus = HingesOf(midwayProgress, "rectus");
            yield return WaitFor(() => HingesAre(midwayRectus, true), "the rectus to be open on the midway client");
            AssertHinges(HingesOf(midwayProgress, "omentum"), open: false);

            Assert.AreEqual(StepResult.Accepted, _server.TryStep(DissectionAction.LiftOmentum));
            NetworkManager late = StartClient("LateClient");
            DissectionProgress lateProgress = null;
            yield return WaitForBody(late, body => lateProgress = body.GetComponent<DissectionProgress>());
            yield return WaitFor(() => lateProgress.Stage == DissectionStage.OmentumLifted, "the late client to see OmentumLifted");
            // A joiner shows the open pose. Netcode may deliver the stage as a change right after spawn, which swings
            // for a moment, so wait for the pose rather than asserting the very first frame.
            LayerHinge[] all = lateProgress.GetComponentsInChildren<LayerHinge>(true);
            yield return WaitFor(() => HingesAre(all, true), "every hinge to be open on the late client");
            Assert.AreEqual(5, all.Length);
        }

        [UnityTest]
        public IEnumerator ClientsCannotWriteTheStageAndBadRequestsChangeNothing()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<DissectionProgress>();
            DissectionProgress clientProgress = null;
            NetworkManager client = StartClient("Client");
            yield return WaitForBody(client, body => clientProgress = body.GetComponent<DissectionProgress>());
            var answers = new List<StepResult>();
            clientProgress.StepResolved += (action, result) => answers.Add(result);

            // The variable is server-write only: Netcode refuses a client write with an error and changes nothing.
            FieldInfo stageField = typeof(DissectionProgress).GetField("_stage", BindingFlags.NonPublic | BindingFlags.Instance);
            var stage = (NetworkVariable<DissectionStage>)stageField.GetValue(clientProgress);
            LogAssert.ignoreFailingMessages = true;
            try
            {
                stage.Value = DissectionStage.OmentumLifted;
            }
            catch (System.InvalidOperationException)
            {
                // Some Netcode versions throw instead of logging. Either way the server value must not change.
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);

            // The only client route is the Rpc, which the server judges. An unknown action and an out-of-order one both fail.
            clientProgress.RequestStep((DissectionAction)200);
            clientProgress.RequestStep(DissectionAction.LiftOmentum);
            yield return WaitFor(() => answers.Count == 2, "the answers to both bad requests");
            Assert.AreEqual(StepResult.Unknown, answers[0]);
            Assert.AreEqual(StepResult.WrongOrder, answers[1]);
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);

            // Calling the server-only method on a client does nothing either.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("only the server may apply dissection steps"));
            Assert.AreEqual(StepResult.Unknown, clientProgress.TryStep(DissectionAction.OpenRectus));
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(DissectionStage.Intact, _server.Stage);
        }

        private Vector3 OmentumCentre()
        {
            Transform zone = _serverBody.transform.Find("OmentumTrigger");
            Assert.IsNotNull(zone, "The body needs an OmentumTrigger. Run Tools > DIYsect > Add Body Dissection.");
            return _serverBody.transform.InverseTransformPoint(zone.TransformPoint(zone.GetComponent<BoxCollider>().center));
        }

        // Valid samples in steps the server accepts, up to the end of the line.
        private static void Complete(DissectionProgress server)
        {
            for (float t = 0.1f; t <= 1.0001f; t += 0.1f)
                server.EvaluateSample(Vector3.Lerp(server.LineStart, server.LineEnd, Mathf.Min(t, 1f)));
        }

        private static void PlaceTip(ForcepsTool tool, Vector3 worldPoint)
        {
            tool.transform.position += worldPoint - tool.Tip.position;
        }

        private static LayerHinge[] HingesOf(DissectionProgress body, string nameContains)
        {
            var found = new List<LayerHinge>();
            foreach (LayerHinge hinge in body.GetComponentsInChildren<LayerHinge>(true))
                if (hinge.name.Contains(nameContains))
                    found.Add(hinge);
            return found.ToArray();
        }

        private static void AssertHinges(LayerHinge[] hinges, bool open)
        {
            Assert.Greater(hinges.Length, 0);
            foreach (LayerHinge hinge in hinges)
                Assert.IsTrue(HingeIsAt(hinge, open), $"{hinge.name} should be {(open ? "open" : "closed")}.");
        }

        private static bool HingesAre(LayerHinge[] hinges, bool open)
        {
            foreach (LayerHinge hinge in hinges)
                if (!HingeIsAt(hinge, open))
                    return false;
            return true;
        }

        private static bool HingeIsAt(LayerHinge hinge, bool open)
        {
            Quaternion target = open ? hinge.OpenRotation : Quaternion.identity;
            return Quaternion.Angle(hinge.transform.localRotation, target) < AngleTolerance;
        }

        private static Vector3 RendererCentre(LayerHinge hinge)
        {
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in hinge.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds.center;
        }

        private static GameObject LoadForcepsPrefab()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForcepsPrefabPath);
            Assert.IsNotNull(prefab, $"{ForcepsPrefabPath} is missing. Run Tools > DIYsect > Add Placeholder Forceps.");
            return prefab;
#else
            throw new System.NotSupportedException("These tests run in the editor only.");
#endif
        }
    }
}
