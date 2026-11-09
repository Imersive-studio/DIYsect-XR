// Starts a real in-process Netcode host and a client over UnityTransport on localhost (no cloud services).
// Authority under test: the server holds each line. The client only asks by Rpc to draw on the classmate sketch line,
// and the professor's guide line on the same body is never touched.
// Input is simulated with the Input System's InputTestFixture: a fake mouse, not a real phone.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DIYsect.Networking.Tests
{
    public class ClassmateSketchTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private SharedBody _clientBody;
        private GuideLine _hostSketch;
        private GuideLine _hostProfessorLine;
        private GuideLine _clientSketch;
        private GameObject _cameraObject;
        private GameObject _uiObject;
        private Camera _camera;
        private ClassmateOrbitCamera _orbit;
        private ClassmateSketchInput _sketch;
        private PointerGestureTracker _tracker;

        [SetUp]
        public void SetUpInput()
        {
            _input.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [TearDown]
        public void TearDownInput()
        {
            if (_cameraObject != null)
                Object.Destroy(_cameraObject);
            if (_uiObject != null)
                Object.Destroy(_uiObject);
            _input.TearDown();
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private IEnumerator StartScene()
        {
            yield return StartHostAndSpawnBody();
            _hostProfessorLine = _serverBody.GetComponent<GuideLine>();
            Transform hostChild = _serverBody.transform.Find("ClassmateSketch");
            Assert.IsNotNull(hostChild, "The body prefab has no ClassmateSketch child. Run Tools > DIYsect > Add Classmate Sketch.");
            _hostSketch = hostChild.GetComponent<GuideLine>();
            Assert.AreNotSame(_hostProfessorLine, _hostSketch, "The sketch must be its own line, not the professor's.");

            NetworkManager client = StartClient("Classmate");
            yield return WaitForBody(client, found => _clientBody = found);
            _clientSketch = _clientBody.transform.Find("ClassmateSketch").GetComponent<GuideLine>();
            yield return WaitFor(() => _clientSketch.IsSpawned, "the client's sketch line to spawn");
            // Host and client copies sit in one scene here, so the host copy is made untouchable and the ray meets the client's body.
            foreach (Collider collider in _serverBody.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            _cameraObject = new GameObject("TestClassmateCamera");
            _cameraObject.SetActive(false);
            _camera = _cameraObject.AddComponent<Camera>();
            _cameraObject.transform.position = _clientBody.transform.position + new Vector3(0f, 0.9f, -0.9f);
            _tracker = _cameraObject.AddComponent<PointerGestureTracker>();
            Set(_tracker, "_holdToPinSeconds", 0.3f);
            _orbit = _cameraObject.AddComponent<ClassmateOrbitCamera>();
            Set(_orbit, "_gestures", _tracker);
            Set(_orbit, "_target", _clientBody.transform);
            _sketch = _cameraObject.AddComponent<ClassmateSketchInput>();
            Set(_sketch, "_body", _clientBody);
            Set(_sketch, "_line", _clientSketch);
            Set(_sketch, "_gestures", _tracker);
            Set(_sketch, "_camera", _camera);
            _cameraObject.SetActive(true);
            for (int i = 0; i < 5; i++)
                yield return null;
            _orbit.SnapToGoal();
        }

        private Vector2 ScreenPointOnOrgan()
        {
            MeshFilter filter = _clientBody.Organs[OrganIndex].GetComponentInChildren<MeshFilter>();
            Vector3 world = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
            return _camera.WorldToScreenPoint(world);
        }

        private IEnumerator Drag(Vector2 from, Vector2 to)
        {
            _input.Set(_mouse.position, from);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            for (int i = 1; i <= 8; i++)
            {
                _input.Set(_mouse.position, Vector2.Lerp(from, to, i / 8f));
                yield return null;
            }
            _input.Release(_mouse.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClientSketchShowsOnTheHostAndLeavesTheProfessorLineAlone()
        {
            yield return StartScene();
            _sketch.SetDrawing(true);
            Vector2 centre = ScreenPointOnOrgan();

            yield return Drag(centre - new Vector2(40f, 0f), centre + new Vector2(40f, 0f));
            yield return WaitFor(() => _hostSketch.Count >= 3 && _clientSketch.Count == _hostSketch.Count, "the sketch on the host and the client",
                () => $"Host {_hostSketch.Count}, client {_clientSketch.Count}.");

            Assert.AreEqual(0, _hostProfessorLine.Count, "The professor's guide line must not change.");
            Assert.AreEqual(GuideLine.NoDrawer, _hostProfessorLine.Drawer);
            yield return WaitFor(() => _hostSketch.Drawer == GuideLine.NoDrawer, "the stroke to end when the finger lifted");
            // Every point floats just off the surface, so it lies inside the body's bounds (a little wider than the colliders).
            Bounds bounds = default;
            bool first = true;
            foreach (Collider collider in _clientBody.GetComponentsInChildren<Collider>())
            {
                if (first) bounds = collider.bounds; else bounds.Encapsulate(collider.bounds);
                first = false;
            }
            bounds.Expand(0.02f);
            for (int i = 0; i < _clientSketch.Count; i++)
                Assert.IsTrue(bounds.Contains(_clientSketch.transform.TransformPoint(_clientSketch[i])), $"Point {i} is not on the body.");
        }

        [UnityTest]
        public IEnumerator ADragWhileDrawingDoesNotOrbitNameOrPin()
        {
            yield return StartScene();
            _sketch.SetDrawing(true);
            int taps = 0, holds = 0;
            _tracker.Tapped += _ => taps++;
            _tracker.Held += _ => holds++;
            float yawBefore = _orbit.Yaw;
            Vector2 centre = ScreenPointOnOrgan();

            yield return Drag(centre - new Vector2(30f, 0f), centre + new Vector2(30f, 0f));
            // A press that stays put long enough to be a hold must not pin either.
            _input.Set(_mouse.position, centre);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return new WaitForSeconds(0.5f);
            _input.Release(_mouse.leftButton);
            yield return null;
            _orbit.SnapToGoal();

            Assert.AreEqual(yawBefore, _orbit.Yaw, 0.01f, "The camera must not orbit while drawing.");
            Assert.AreEqual(0, taps, "No tap (organ name, look here) while drawing.");
            Assert.AreEqual(0, holds, "No hold (question pin) while drawing.");
        }

        [UnityTest]
        public IEnumerator WithDrawingOffADragOrbitsAndNothingIsDrawn()
        {
            yield return StartScene();
            float yawBefore = _orbit.Yaw;
            Vector2 centre = ScreenPointOnOrgan();

            yield return Drag(centre - new Vector2(60f, 0f), centre + new Vector2(60f, 0f));
            _orbit.SnapToGoal();
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(yawBefore, _orbit.Yaw)), 0.5f, "A drag should orbit when drawing is off.");
            Assert.AreEqual(0, _hostSketch.Count);
        }

        [UnityTest]
        public IEnumerator TurningDrawingOffGivesTheFingerBackToTheCameraAndKeepsTheSketch()
        {
            yield return StartScene();
            int changes = 0;
            bool last = false;
            _sketch.DrawingChanged += on => { changes++; last = on; };
            _sketch.SetDrawing(true);
            _sketch.SetDrawing(true); // no second event for the same value
            Vector2 centre = ScreenPointOnOrgan();
            yield return Drag(centre - new Vector2(40f, 0f), centre + new Vector2(40f, 0f));
            yield return WaitFor(() => _hostSketch.Count >= 3, "a sketch");
            int drawn = _hostSketch.Count;

            _sketch.SetDrawing(false);
            Assert.IsFalse(_tracker.OneFingerClaimed);
            Assert.AreEqual(2, changes);
            Assert.IsFalse(last);
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(drawn, _hostSketch.Count, "Turning drawing off keeps the sketch.");

            _sketch.Clear();
            yield return WaitFor(() => _hostSketch.Count == 0 && _clientSketch.Count == 0, "the sketch to clear everywhere");
        }

        [UnityTest]
        public IEnumerator APressOnAButtonNeverDraws()
        {
            yield return StartScene();
            // A full-screen button in front of the body: any press lands on UI.
            _uiObject = new GameObject("TestUi");
            var events = _uiObject.AddComponent<EventSystem>();
            var module = _uiObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
            var canvas = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvas.transform.SetParent(_uiObject.transform, false);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var button = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            EventSystem.current = events;
            yield return null;

            _sketch.SetDrawing(true);
            Vector2 centre = ScreenPointOnOrgan();
            yield return Drag(centre - new Vector2(40f, 0f), centre + new Vector2(40f, 0f));
            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(0, _hostSketch.Count, "A press that starts on UI must not draw.");
        }
    }
}
