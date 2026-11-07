// Starts a real in-process Netcode host and two clients (student and professor) over UnityTransport on localhost.
// Authority under test: the server decides who steers the scalpel and who controls its depth, and validates both.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DIYsect.Dissection;
using DIYsect.Interaction;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class GuidingHandTests : NetworkedBodyTestBase
    {
        private const float DepthTolerance = 0.002f;
        private const float PositionTolerance = 0.005f;
        private const float QuietSeconds = 0.5f;
        private const float HeightAboveSkin = 0.1f;

        private sealed class Peer
        {
            public Peer(NetworkManager manager, SharedBody body = null)
            {
                Manager = manager;
                Body = body;
            }

            public NetworkManager Manager { get; }
            public SharedBody Body { get; set; }
            public ScalpelSteering Steering { get; private set; }
            public GuidingHand Hand { get; private set; }
            public DepthOverride Depth { get; private set; }
            public GuidingHandIndicator Indicator { get; private set; }
            public ScalpelTool Tool { get; private set; }
            public Transform Surface { get; set; }
            public ulong Id => Manager.LocalClientId;

            public void BindScalpel(GameObject scalpel)
            {
                Steering = scalpel.GetComponent<ScalpelSteering>();
                Hand = scalpel.GetComponent<GuidingHand>();
                Depth = scalpel.GetComponent<DepthOverride>();
                Indicator = scalpel.GetComponent<GuidingHandIndicator>();
                Tool = scalpel.GetComponent<ScalpelTool>();
            }
        }

        private Peer _server;
        private Peer _student;
        private Peer _professor;
        private DissectionProgress _progress;
        private GameObject _serverScalpel;

        [TearDown]
        public void DestroyScalpel()
        {
            if (_serverScalpel != null)
                UnityEngine.Object.Destroy(_serverScalpel);
        }

        [UnityTest]
        public IEnumerator StudentSteersTheScalpelAndServerAndProfessorSeeThePose()
        {
            yield return StartSession();

            yield return Steer(_student, expectGranted: true);
            yield return Steer(_professor, expectGranted: false);
            Assert.AreEqual(_student.Id, _server.Steering.Steerer, "A rejected request must not change the steerer.");

            Vector3 position = new Vector3(0.1f, 0.4f, -0.2f);
            Quaternion rotation = Quaternion.Euler(10f, 40f, 5f);
            _student.Steering.SendPose(position, rotation);
            yield return WaitFor(() => Near(_server, position, rotation), "the server scalpel to move");
            yield return WaitFor(() => Near(_professor, position, rotation), "the professor to see the scalpel move");

            // The professor does not steer, so its pose is ignored.
            _professor.Steering.SendPose(Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(QuietSeconds);
            Assert.IsTrue(Near(_server, position, rotation), "A pose from a client that does not steer must be ignored.");
        }

        [UnityTest]
        public IEnumerator ProfessorIsGrantedDepthControlAndASecondRequesterIsRejected()
        {
            yield return StartSession();

            // Nobody steers yet, so there is nothing to guide.
            yield return Control(_professor, expectGranted: false);

            yield return Steer(_student, expectGranted: true);
            // The student cannot guide their own scalpel.
            yield return Control(_student, expectGranted: false);
            Assert.AreEqual(GuidingHand.NoController, _server.Hand.Controller);

            yield return Control(_professor, expectGranted: true);
            yield return WaitFor(() => _student.Hand.Controller == _professor.Id && _server.Hand.Controller == _professor.Id, "everyone to see the professor control the depth");

            // The host is also a client; it is rejected while the professor holds the depth.
            yield return Control(_server, expectGranted: false);
            Assert.AreEqual(_professor.Id, _server.Hand.Controller, "The rejected request must not change the controller.");

            // The controller asking again is answered yes and nothing changes.
            yield return Control(_professor, expectGranted: true);
            Assert.AreEqual(_professor.Id, _server.Hand.Controller);
        }

        [UnityTest]
        public IEnumerator ControlledDepthOverridesTheStudentsDepthOnEveryPeerWhileSteeringStaysWithTheStudent()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);
            yield return Control(_professor, expectGranted: true);

            MoveStudentTip(SkinPoint(0.5f, lateral: 0.01f, above: HeightAboveSkin));
            _professor.Hand.SendDepth(0.5f);
            float expected = 0.5f * _student.Depth.MaxDepthMeters;
            yield return WaitForDepth(expected);

            // The student tries to change the depth with their own hand: higher, then lower. Nothing changes.
            MoveStudentTip(SkinPoint(0.5f, lateral: 0.01f, above: 0.25f));
            yield return WaitForDepth(expected);
            MoveStudentTip(SkinPoint(0.5f, lateral: 0.01f, above: 0.02f));
            yield return WaitForDepth(expected);

            // Steering still follows the student: the sideways and along-the-body position move on every peer.
            Vector3 steered = SkinPoint(0.6f, lateral: 0.03f, above: 0.05f);
            MoveStudentTip(steered);
            yield return WaitFor(() => AllPeers(p => PlaneDistance(p, p.Depth.EffectiveTip, steered) < PositionTolerance), "the guided tip to follow the student's sideways steering on every peer");
            yield return WaitForDepth(expected);

            // The professor changes the depth again; everyone follows, the student's steering is unchanged.
            _professor.Hand.SendDepth(1f);
            yield return WaitForDepth(_student.Depth.MaxDepthMeters);
            Assert.Less(PlaneDistance(_student, _student.Depth.EffectiveTip, steered), PositionTolerance);
        }

        [UnityTest]
        public IEnumerator ReleaseRestoresTheStudentsDepth()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);
            yield return Control(_professor, expectGranted: true);
            Vector3 hand = SkinPoint(0.5f, lateral: 0f, above: HeightAboveSkin);
            MoveStudentTip(hand);
            _professor.Hand.SendDepth(0.5f);
            yield return WaitForDepth(0.5f * _student.Depth.MaxDepthMeters);

            _professor.Hand.ReleaseControl();
            yield return WaitFor(() => AllPeers(p => p.Hand.Controller == GuidingHand.NoController), "everyone to see the depth released");
            Assert.IsFalse(_student.Depth.IsOverriding);
            Assert.AreEqual(-HeightAboveSkin, _student.Depth.DepthBelowSurface(_student.Tool.Tip.position), DepthTolerance, "The student's own height applies again after the release.");
            yield return WaitFor(() => AllPeers(p => Mathf.Abs(p.Depth.DepthBelowSurface(p.Tool.Tip.position) + HeightAboveSkin) < DepthTolerance), "every peer to show the student's own depth");
        }

        [UnityTest]
        public IEnumerator DisconnectingProfessorRestoresTheStudentsDepth()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);
            yield return Control(_professor, expectGranted: true);
            MoveStudentTip(SkinPoint(0.5f, lateral: 0f, above: HeightAboveSkin));
            _professor.Hand.SendDepth(1f);
            yield return WaitForDepth(_student.Depth.MaxDepthMeters, _server, _student);

            _professor.Manager.Shutdown();
            yield return WaitFor(() => _server.Hand.Controller == GuidingHand.NoController, "the server to free the depth of the disconnected professor");
            yield return WaitFor(() => _student.Hand.Controller == GuidingHand.NoController, "the student to see the depth freed");
            yield return WaitFor(() => Mathf.Abs(_student.Depth.DepthBelowSurface(_student.Tool.Tip.position) + HeightAboveSkin) < DepthTolerance, "the student's own depth to apply again");
        }

        [UnityTest]
        public IEnumerator PuttingTheScalpelDownEndsTheGuidance()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);
            yield return Control(_professor, expectGranted: true);

            _student.Steering.ReleaseSteer();
            yield return WaitFor(() => _server.Steering.Steerer == ScalpelSteering.NoSteerer, "the server to see the scalpel put down");
            yield return WaitFor(() => _professor.Hand.Controller == GuidingHand.NoController, "the professor to lose the depth control with it");
            Assert.IsFalse(_professor.Depth.IsOverriding);
        }

        [UnityTest]
        public IEnumerator IndicatorShowsWhoOwnsTheDepthOnEveryClient()
        {
            yield return StartSession();
            var events = new Dictionary<Peer, List<GuidingState>>();
            foreach (Peer peer in Peers())
            {
                var list = new List<GuidingState>();
                events[peer] = list;
                peer.Indicator.StateChanged += list.Add;
                Assert.AreEqual(GuidingState.Idle, peer.Indicator.State);
                Assert.IsFalse(peer.Indicator.GetComponentInChildren<TextMesh>(true).gameObject.activeSelf, "Nothing is shown while nobody holds the scalpel.");
            }

            yield return Steer(_student, expectGranted: true);
            yield return WaitFor(() => AllPeers(p => p.Indicator.State == GuidingState.StudentDepth), "every indicator to show the student's depth");
            Assert.AreEqual("Depth: You", _student.Indicator.LabelText);
            Assert.AreEqual("Depth: Student", _professor.Indicator.LabelText);

            yield return Control(_professor, expectGranted: true);
            yield return WaitFor(() => _professor.Indicator.State == GuidingState.GuidedByLocal && _student.Indicator.State == GuidingState.GuidedByOther && _server.Indicator.State == GuidingState.GuidedByOther, "the indicators to show the professor's ownership");
            Assert.AreEqual("Depth: Professor", _student.Indicator.LabelText);
            Assert.AreEqual("Depth: Professor", _server.Indicator.LabelText);
            Assert.AreEqual("Depth: You", _professor.Indicator.LabelText);
            Assert.AreNotEqual(_student.Indicator.RingColor, _professor.Indicator.RingColor, "The ring colour must differ between your guiding and being guided.");
            Assert.IsTrue(_student.Indicator.GetComponentInChildren<TextMesh>(true).gameObject.activeSelf);
            Assert.AreEqual("Depth: Professor", _student.Indicator.GetComponentInChildren<TextMesh>(true).text);

            _professor.Hand.ReleaseControl();
            yield return WaitFor(() => AllPeers(p => p.Indicator.State == GuidingState.StudentDepth), "the indicators to show the student's depth again");
            Assert.AreEqual("Depth: You", _student.Indicator.LabelText, "Release restores control, and the indicator says so.");

            Assert.AreEqual(new[] { GuidingState.StudentDepth, GuidingState.GuidedByOther, GuidingState.StudentDepth }, events[_student].ToArray());
            Assert.AreEqual(new[] { GuidingState.StudentDepth, GuidingState.GuidedByLocal, GuidingState.StudentDepth }, events[_professor].ToArray());
        }

        [UnityTest]
        public IEnumerator InvalidDepthValuesAreRejectedOrClamped()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);
            yield return Control(_professor, expectGranted: true);

            _professor.Hand.SendDepth(0.4f);
            yield return WaitFor(() => Mathf.Approximately(_server.Hand.Depth, 0.4f), "the server to take a valid depth");

            _professor.Hand.SendDepth(float.NaN);
            _professor.Hand.SendDepth(float.PositiveInfinity);
            _professor.Hand.SendDepth(float.NegativeInfinity);
            // The student does not control the depth, so its value is ignored too.
            _student.Hand.SendDepth(0.9f);
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(0.4f, _server.Hand.Depth, 0.0001f, "Values that are not finite, or from a client without control, must change nothing.");

            _professor.Hand.SendDepth(5f);
            yield return WaitFor(() => Mathf.Approximately(_server.Hand.Depth, 1f), "an out of range depth to be clamped to 1");
            _professor.Hand.SendDepth(-3f);
            yield return WaitFor(() => Mathf.Approximately(_server.Hand.Depth, 0f), "a negative depth to be clamped to 0");
            yield return WaitFor(() => Mathf.Approximately(_student.Hand.Depth, 0f), "the student to see the clamped depth");
        }

        [UnityTest]
        public IEnumerator InvalidScalpelPosesFromTheSteererAreIgnored()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);
            Vector3 position = new Vector3(0.1f, 0.4f, -0.2f);
            _student.Steering.SendPose(position, Quaternion.identity);
            yield return WaitFor(() => Near(_server, position, Quaternion.identity), "the server scalpel to move");

            LogAssert.Expect(LogType.Warning, new Regex("ignored an invalid scalpel pose"));
            _student.Steering.SendPose(new Vector3(float.NaN, 0f, 0f), Quaternion.identity);
            _student.Steering.SendPose(new Vector3(500f, 0f, 0f), Quaternion.identity);
            _student.Steering.SendPose(position, new Quaternion(0f, 0f, 0f, 0f));
            yield return new WaitForSeconds(QuietSeconds);
            Assert.IsTrue(Near(_server, position, Quaternion.identity), "Invalid poses must not move the scalpel.");
        }

        [UnityTest]
        public IEnumerator IncisionTracedWithTheGuidedDepthStillAdvancesThroughTheServerValidation()
        {
            yield return StartSession();
            yield return Steer(_student, expectGranted: true);

            // With the student's own hand 10 cm above the skin the tip is outside the cutting zone, so nothing is cut.
            MoveStudentTip(SkinPoint(0f, lateral: 0f, above: HeightAboveSkin));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(QuietSeconds);
            Assert.AreEqual(0f, _progress.IncisionProgress, 0.0001f);

            // The professor takes the depth: the blade goes 1.5 cm into the skin although the student's hand stays high.
            yield return Control(_professor, expectGranted: true);
            _professor.Hand.SendDepth(0.5f);
            yield return WaitForDepth(0.5f * _student.Depth.MaxDepthMeters);
            yield return new WaitForFixedUpdate();

            for (float t = 0.1f; t <= 1.0001f; t += 0.1f)
            {
                float step = Mathf.Min(t, 1f);
                MoveStudentTip(SkinPoint(step, lateral: 0f, above: HeightAboveSkin));
                yield return new WaitForFixedUpdate();
                yield return WaitFor(() => _progress.IncisionProgress >= Mathf.Min(step, 0.98f) - 0.01f, $"the guided scalpel to advance the incision to {step:F1}", () => $"progress {_progress.IncisionProgress}");
            }
            yield return WaitFor(() => _progress.Stage == DissectionStage.SkinOpen, "the guided scalpel to complete the cut and open the skin", () => $"stage {_progress.Stage}, progress {_progress.IncisionProgress}");
        }

        [Test]
        public void HandleHeightMapsToDepthWithoutJumpingAtTheGrab()
        {
            Assert.AreEqual(0.3f, DepthControlInput.DepthFromHandle(0.3f, 1.2f, 1.2f, 0.2f), 0.0001f, "No movement keeps the depth the handle was grabbed at.");
            Assert.AreEqual(0.8f, DepthControlInput.DepthFromHandle(0.3f, 1.2f, 1.1f, 0.2f), 0.0001f, "Lowering the handle by half the travel adds half.");
            Assert.AreEqual(0f, DepthControlInput.DepthFromHandle(0.3f, 1.2f, 1.5f, 0.2f), 0.0001f, "Raising it far enough bottoms out at zero.");
            Assert.AreEqual(1f, DepthControlInput.DepthFromHandle(0.3f, 1.2f, 0.5f, 0.2f), 0.0001f, "Lowering it far enough tops out at one.");
        }

        // ---- setup ----

        // Host with the body, a student client and a professor client, and the scalpel spawned under the body.
        private IEnumerator StartSession()
        {
            yield return StartHostAndSpawnBody();
            NetworkManager studentManager = StartClient("Student");
            NetworkManager professorManager = StartClient("Professor");
            _student = new Peer(studentManager);
            _professor = new Peer(professorManager);
            _server = new Peer(_host, _serverBody);
            yield return WaitForBody(studentManager, body => _student.Body = body);
            yield return WaitForBody(professorManager, body => _professor.Body = body);
            _progress = _serverBody.GetComponent<DissectionProgress>();

            _serverScalpel = UnityEngine.Object.Instantiate(LoadScalpelPrefabAsset());
            var spawned = _serverScalpel.GetComponent<NetworkObject>();
            Assert.IsNotNull(spawned, "The scalpel prefab needs a NetworkObject. Run Tools > DIYsect > Add Placeholder Scalpel.");
            spawned.Spawn();
            Assert.IsTrue(spawned.TrySetParent(_serverBody.GetComponent<NetworkObject>(), worldPositionStays: false), "The scalpel could not be parented to the body.");
            _serverScalpel.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.5f, 0f), Quaternion.identity);

            Bind(_server, _serverScalpel);
            yield return BindClient(_student);
            yield return BindClient(_professor);
            foreach (Peer peer in Peers())
            {
                yield return WaitFor(() => peer.Steering.IsSpawned && peer.Hand.IsSpawned, $"{peer.Manager.name} to spawn the scalpel");
                yield return WaitFor(() => peer.Tool.transform.parent == peer.Body.transform, $"{peer.Manager.name} to parent the scalpel to the body");
            }
        }

        private IEnumerator BindClient(Peer peer)
        {
            GameObject found = null;
            yield return WaitFor(() =>
            {
                foreach (NetworkObject spawned in peer.Manager.SpawnManager.SpawnedObjects.Values)
                {
                    if (spawned.GetComponent<GuidingHand>() != null)
                    {
                        found = spawned.gameObject;
                        return true;
                    }
                }
                return false;
            }, $"{peer.Manager.name} to receive the scalpel");
            Bind(peer, found);
        }

        private static void Bind(Peer peer, GameObject scalpel)
        {
            peer.BindScalpel(scalpel);
        }

        private IEnumerable<Peer> Peers()
        {
            yield return _server;
            yield return _student;
            yield return _professor;
        }

        private bool AllPeers(Func<Peer, bool> condition)
        {
            foreach (Peer peer in Peers())
                if (!condition(peer))
                    return false;
            return true;
        }

        // ---- requests ----

        private static IEnumerator Steer(Peer peer, bool expectGranted)
        {
            bool? answer = null;
            Action<bool> handler = granted => answer = granted;
            peer.Steering.SteerResolved += handler;
            peer.Steering.RequestSteer();
            yield return WaitFor(() => answer.HasValue, $"the answer to {peer.Manager.name}'s steer request");
            peer.Steering.SteerResolved -= handler;
            Assert.AreEqual(expectGranted, answer.Value, $"{peer.Manager.name}'s steer request");
            if (expectGranted)
                yield return WaitFor(() => peer.Steering.Steerer == peer.Id, $"{peer.Manager.name} to see itself steering");
        }

        private static IEnumerator Control(Peer peer, bool expectGranted)
        {
            bool? answer = null;
            Action<bool> handler = granted => answer = granted;
            peer.Hand.ControlResolved += handler;
            peer.Hand.RequestControl();
            yield return WaitFor(() => answer.HasValue, $"the answer to {peer.Manager.name}'s depth request");
            peer.Hand.ControlResolved -= handler;
            Assert.AreEqual(expectGranted, answer.Value, $"{peer.Manager.name}'s depth request");
            if (expectGranted)
                yield return WaitFor(() => peer.Hand.Controller == peer.Id, $"{peer.Manager.name} to see itself in control");
        }

        // ---- geometry ----

        // A point on the skin plane at the given fraction of the midline, moved sideways (zone x) and above the skin (zone up).
        private Vector3 SkinPoint(float fraction, float lateral, float above)
        {
            Transform zone = Surface(_server);
            Vector3 onLine = _serverBody.transform.TransformPoint(Vector3.Lerp(_progress.LineStart, _progress.LineEnd, fraction));
            return onLine + zone.right * lateral + zone.up * above;
        }

        private Transform Surface(Peer peer)
        {
            return peer.Surface != null ? peer.Surface : peer.Surface = peer.Body.GetComponentInChildren<IncisionTrigger>(true).transform;
        }

        // Plays the part of the student's hand: puts the tip at the world point and streams the pose like NetworkedScalpelGrab does.
        private void MoveStudentTip(Vector3 worldTip)
        {
            Transform root = _student.Tool.transform;
            root.position += worldTip - _student.Depth.StudentTip;
            _student.Steering.SendPose(root.localPosition, root.localRotation);
        }

        // Distance between two world points ignoring the height above the skin (zone up).
        private float PlaneDistance(Peer peer, Vector3 a, Vector3 b)
        {
            Transform zone = Surface(peer);
            Vector3 delta = zone.InverseTransformVector(a - b);
            delta.y = 0f;
            return delta.magnitude;
        }

        private IEnumerator WaitForDepth(float expectedMeters)
        {
            return WaitForDepth(expectedMeters, _server, _student, _professor);
        }

        private IEnumerator WaitForDepth(float expectedMeters, params Peer[] peers)
        {
            yield return WaitFor(() => Array.TrueForAll(peers, p => Mathf.Abs(p.Depth.DepthBelowSurface(p.Tool.Tip.position) - expectedMeters) < DepthTolerance && Mathf.Abs(p.Depth.DepthBelowSurface(p.Depth.EffectiveTip) - expectedMeters) < DepthTolerance),
                $"every peer to show the tip {expectedMeters * 1000f:F1} mm deep", () => DescribeDepths());
        }

        private string DescribeDepths()
        {
            string text = string.Empty;
            foreach (Peer peer in Peers())
                text += $"{peer.Manager.name}: {peer.Depth.DepthBelowSurface(peer.Tool.Tip.position) * 1000f:F1} mm; ";
            return text;
        }

        private static bool Near(Peer peer, Vector3 localPosition, Quaternion localRotation)
        {
            Transform scalpel = peer.Tool.transform;
            return Vector3.Distance(scalpel.localPosition, localPosition) < PositionTolerance && Quaternion.Angle(scalpel.localRotation, localRotation) < 1f;
        }
    }
}
