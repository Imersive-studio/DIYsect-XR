// State bugs of the guide pen. The pens are built in code (a grab interactable, the pen script and a tip) and
// driven through the public BeginStroke, so no controller is needed.
// The pen's state is read through its public IsDrawing and Phase (GrabRequest).
using System.Collections;
using System.Reflection;
using DIYsect.Networking;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction.Tests
{
    public class GuidePenStateTests : InteractionTestBase
    {
        private GuideLine _server;
        private GuideLine _lineA;
        private GuideLine _lineB;
        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private GuideLinePen _penA;
        private GuideLinePen _penB;

        [UnityTest]
        public IEnumerator PenStopsDrawingWhenTheServerDeniesTheStroke()
        {
            // Bug 4: GuideLinePen keeps sampling after StrokeResolved(false); it ignores the denial.
            yield return StartPens();
            yield return BeginAndWaitForDrawer(_penA, _managerA.LocalClientId);

            bool? answer = null;
            void OnResolved(bool granted) => answer = granted;
            _lineB.StrokeResolved += OnResolved;
            _penB.BeginStroke();
            yield return WaitFor(() => answer.HasValue, "client B to hear the answer to its stroke request");
            _lineB.StrokeResolved -= OnResolved;
            Assert.IsFalse(answer.Value, "Setup: B must be denied while A is drawing.");
            Assert.AreEqual(_managerA.LocalClientId, _server.Drawer, "The drawer must not change.");

            Assert.IsFalse(IsPenDrawing(_penB), "A denied pen must stop trying. It must not keep sampling points.");
            Assert.IsTrue(IsPenDrawing(_penA), "A's own stroke must go on.");
        }

        [UnityTest]
        public IEnumerator ServerDrawerIsClearedWhenTheDrawingPenIsDisabled()
        {
            // Bug 5: disabling the pen while drawing leaves the server drawer set, so nobody else can draw.
            yield return StartPens();
            yield return BeginAndWaitForDrawer(_penA, _managerA.LocalClientId);

            _penA.enabled = false;

            yield return WaitFor(() => _server.Drawer == GuideLine.NoDrawer, "the server to free the drawer after A's pen was disabled",
                () => $"Drawer is {_server.Drawer}.");

            bool? answer = null;
            void OnResolved(bool granted) => answer = granted;
            _lineB.StrokeResolved += OnResolved;
            _penB.BeginStroke();
            yield return WaitFor(() => answer.HasValue, "client B to hear the answer to its stroke request");
            _lineB.StrokeResolved -= OnResolved;
            Assert.IsTrue(answer.Value, "B must be able to draw once A's pen is gone.");
        }

        private IEnumerator StartPens()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<GuideLine>();
            Assert.IsNotNull(_server, "The body prefab has no GuideLine. Run Tools > DIYsect > Add Guide Line.");
            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(_managerA, found => bodyA = found);
            yield return WaitForBody(_managerB, found => bodyB = found);
            _lineA = bodyA.GetComponent<GuideLine>();
            _lineB = bodyB.GetComponent<GuideLine>();
            yield return WaitFor(() => _lineA.IsSpawned && _lineB.IsSpawned, "both clients to spawn the guide line");
            _penA = CreatePen("PenA", _lineA, bodyA);
            _penB = CreatePen("PenB", _lineB, bodyB);
            yield return null;
        }

        // Built inactive so the serialized fields are set before Awake runs.
        private GuideLinePen CreatePen(string penName, GuideLine line, SharedBody body)
        {
            GameObject penObject = Track(new GameObject(penName));
            penObject.SetActive(false);
            Transform tip = new GameObject("Tip").transform;
            tip.SetParent(penObject.transform, false);
            penObject.transform.position = body.transform.TransformPoint(new Vector3(0f, 0.2f, 0f));
            penObject.AddComponent<Rigidbody>().isKinematic = true;
            penObject.AddComponent<XRGrabInteractable>();
            var pen = penObject.AddComponent<GuideLinePen>();
            SetSerialized(pen, "_line", line);
            SetSerialized(pen, "_tip", tip);
            penObject.SetActive(true);
            return pen;
        }

        private IEnumerator BeginAndWaitForDrawer(GuideLinePen pen, ulong expectedDrawer)
        {
            pen.BeginStroke();
            yield return WaitFor(() => _server.Drawer == expectedDrawer, "the server to make the pen's client the drawer", () => $"Drawer is {_server.Drawer}.");
        }

        private static bool IsPenDrawing(GuideLinePen pen) => pen.IsDrawing;
    }
}
