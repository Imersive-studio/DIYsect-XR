// Starts a real in-process Netcode host (localhost, no cloud services) and a classmate camera with the same parts the editor step adds.
// Authority under test: none for the camera (local only). The pin is still decided by the server.
// Input is simulated with the Input System's InputTestFixture: a fake mouse and a fake touchscreen, not a real phone.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class ClassmateOrbitCameraTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private Touchscreen _touch;
        private GameObject _cameraObject;
        private Camera _camera;
        private ClassmateOrbitCamera _orbit;
        private QuestionBoard _board;

        [SetUp]
        public void SetUpInput()
        {
            _input.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _touch = InputSystem.AddDevice<Touchscreen>();
        }

        [TearDown]
        public void TearDownInput()
        {
            if (_cameraObject != null)
                Object.Destroy(_cameraObject);
            _input.TearDown();
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private IEnumerator StartScene(float holdToPinSeconds = 5f)
        {
            yield return StartHostAndSpawnBody();
            _board = _serverBody.GetComponent<QuestionBoard>();
            Assert.IsNotNull(_board, "The body prefab has no QuestionBoard. Run Tools > DIYsect > Add Question Pins.");

            _cameraObject = new GameObject("TestClassmateCamera");
            _cameraObject.SetActive(false);
            _camera = _cameraObject.AddComponent<Camera>();
            _cameraObject.transform.position = _serverBody.transform.position + new Vector3(0f, 0.9f, -0.9f);
            var tracker = _cameraObject.AddComponent<PointerGestureTracker>();
            Set(tracker, "_holdToPinSeconds", holdToPinSeconds); // a slow test frame must not turn a drag or a tap into a hold
            _orbit = _cameraObject.AddComponent<ClassmateOrbitCamera>();
            Set(_orbit, "_gestures", tracker);
            Set(_orbit, "_target", _serverBody.transform);
            var pins = _cameraObject.AddComponent<PinPlacementInput>();
            Set(pins, "_board", _board);
            Set(pins, "_camera", _camera);
            Set(pins, "_gestures", tracker);
            _cameraObject.SetActive(true);
            yield return null;
        }

        // The centre of an organ's mesh in screen pixels, so a tap there hits that organ.
        private Vector2 ScreenPointOnOrgan()
        {
            MeshFilter filter = _serverBody.Organs[OrganIndex].GetComponentInChildren<MeshFilter>();
            Vector3 world = filter.transform.TransformPoint(filter.sharedMesh.bounds.center);
            return _camera.WorldToScreenPoint(world);
        }

        private IEnumerator Tap(Vector2 screen)
        {
            _input.Set(_mouse.position, screen);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;
        }

        // A press that stays put long enough to be a hold (the tracker is set to 0.3 s in the tests that use this).
        private IEnumerator Hold(Vector2 screen)
        {
            _input.Set(_mouse.position, screen);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return new WaitForSeconds(0.5f);
            _input.Release(_mouse.leftButton);
            yield return null;
        }

        private IEnumerator Drag(Vector2 from, Vector2 to)
        {
            _input.Set(_mouse.position, from);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            for (int i = 1; i <= 6; i++)
            {
                _input.Set(_mouse.position, Vector2.Lerp(from, to, i / 6f));
                yield return null;
            }
            _input.Release(_mouse.leftButton);
            yield return null;
        }

        private IEnumerator Settle()
        {
            for (int i = 0; i < 30; i++)
                yield return null;
            _orbit.SnapToGoal();
        }

        [UnityTest]
        public IEnumerator DragRotatesTheCameraAndPlacesNoPin()
        {
            yield return StartScene();
            Quaternion before = _camera.transform.rotation;
            Vector2 start = ScreenPointOnOrgan(); // a drag that starts on an organ must still not pin it

            yield return Drag(start, start + new Vector2(120f, 40f));
            yield return Settle();
            yield return new WaitForSeconds(0.5f);

            Assert.Greater(Quaternion.Angle(before, _camera.transform.rotation), 5f, "The drag should have turned the camera.");
            Assert.AreEqual(0, _board.Count, "A drag must not place a pin.");
        }

        [UnityTest]
        public IEnumerator HoldPlacesAPinAndDoesNotMoveTheCamera()
        {
            yield return StartScene(0.3f);
            Quaternion before = _camera.transform.rotation;

            yield return Hold(ScreenPointOnOrgan());
            yield return WaitFor(() => _board.Count == 1, "the held pin");

            Assert.Less(Quaternion.Angle(before, _camera.transform.rotation), 0.1f, "A hold must not orbit.");
        }

        [UnityTest]
        public IEnumerator QuickTapPlacesNoPinAndDoesNotMoveTheCamera()
        {
            yield return StartScene(0.3f);
            Quaternion before = _camera.transform.rotation;

            yield return Tap(ScreenPointOnOrgan());
            for (int i = 0; i < 30; i++)
                yield return null;

            Assert.AreEqual(0, _board.Count, "A quick tap names the organ; it does not pin it.");
            Assert.Less(Quaternion.Angle(before, _camera.transform.rotation), 0.1f, "A tap must not orbit.");
        }

        [UnityTest]
        public IEnumerator ScrollWheelZoomsAndStopsAtBothLimits()
        {
            yield return StartScene();
            for (int i = 0; i < 60; i++)
            {
                _input.Set(_mouse.scroll, new Vector2(0f, 120f));
                yield return null;
            }
            _input.Set(_mouse.scroll, Vector2.zero);
            yield return Settle();
            Assert.AreEqual(_orbit.MinDistance, _orbit.Distance, 0.01f, "Zooming in should stop at the minimum distance.");

            for (int i = 0; i < 120; i++)
            {
                _input.Set(_mouse.scroll, new Vector2(0f, -120f));
                yield return null;
            }
            _input.Set(_mouse.scroll, Vector2.zero);
            yield return Settle();
            Assert.AreEqual(_orbit.MaxDistance, _orbit.Distance, 0.01f, "Zooming out should stop at the maximum distance.");
            float actual = Vector3.Distance(_camera.transform.position, _serverBody.transform.position);
            Assert.AreEqual(_orbit.MaxDistance, actual, 0.01f);
        }

        [UnityTest]
        public IEnumerator PitchStaysInsideItsLimits()
        {
            yield return StartScene();
            _orbit.Orbit(new Vector2(0f, 100000f));
            _orbit.SnapToGoal();
            Assert.AreEqual(_orbit.MinPitch, _orbit.Pitch, 0.01f);
            _orbit.Orbit(new Vector2(0f, -1000000f));
            _orbit.SnapToGoal();
            Assert.AreEqual(_orbit.MaxPitch, _orbit.Pitch, 0.01f);
        }

        [UnityTest]
        public IEnumerator TwoFingerPinchZoomsInAndDoesNotPin()
        {
            yield return StartScene();
            float before = _orbit.Distance;
            Vector2 centre = new Vector2(Screen.width / 2f, Screen.height / 2f);

            _input.BeginTouch(1, centre + new Vector2(-40f, 0f));
            yield return null;
            _input.BeginTouch(2, centre + new Vector2(40f, 0f));
            yield return null;
            for (int i = 1; i <= 8; i++)
            {
                _input.MoveTouch(1, centre + new Vector2(-40f - 25f * i, 0f));
                _input.MoveTouch(2, centre + new Vector2(40f + 25f * i, 0f));
                yield return null;
            }
            _input.EndTouch(1, centre);
            _input.EndTouch(2, centre);
            yield return null;
            yield return Settle();

            Assert.Less(_orbit.Distance, before, "Spreading two fingers should zoom in.");
            Assert.AreEqual(0, _board.Count, "A pinch must not place a pin.");
        }

        [UnityTest]
        public IEnumerator OneFingerDragOrbitsAndOneFingerHoldPins()
        {
            yield return StartScene(0.3f);
            Quaternion before = _camera.transform.rotation;
            Vector2 start = new Vector2(200f, 200f);

            _input.BeginTouch(1, start);
            yield return null;
            for (int i = 1; i <= 6; i++)
            {
                _input.MoveTouch(1, start + new Vector2(25f * i, 0f));
                yield return null;
            }
            _input.EndTouch(1, start + new Vector2(150f, 0f));
            yield return null;
            yield return Settle();
            Assert.Greater(Quaternion.Angle(before, _camera.transform.rotation), 5f, "A one-finger drag should orbit.");
            Assert.AreEqual(0, _board.Count);

            Vector2 organ = ScreenPointOnOrgan();
            _input.BeginTouch(2, organ);
            yield return new WaitForSeconds(0.5f);
            _input.EndTouch(2, organ);
            yield return null;
            yield return WaitFor(() => _board.Count == 1, "the touch-held pin");
        }

        [UnityTest]
        public IEnumerator PinStoresTheCameraPoseAfterOrbiting()
        {
            yield return StartScene(0.3f);
            yield return Drag(new Vector2(300f, 200f), new Vector2(180f, 260f));
            _input.Set(_mouse.scroll, new Vector2(0f, 120f));
            yield return null;
            _input.Set(_mouse.scroll, Vector2.zero);
            yield return Settle();

            Transform camera = _camera.transform;
            Vector3 expectedPosition = _board.transform.InverseTransformPoint(camera.position);
            Quaternion expectedRotation = Quaternion.Inverse(_board.transform.rotation) * camera.rotation;
            Assert.Greater(Quaternion.Angle(Quaternion.LookRotation(_serverBody.transform.position - (_serverBody.transform.position + new Vector3(0f, 0.9f, -0.9f))), camera.rotation), 1f, "The camera should have moved from its start.");

            yield return Hold(ScreenPointOnOrgan());
            yield return WaitFor(() => _board.Count == 1, "the pin after orbiting");

            QuestionPin pin = _board[0];
            Assert.IsTrue(pin.HasView);
            Assert.Less((pin.ViewPosition - expectedPosition).magnitude, 0.002f, "The stored view position should be the orbited camera's.");
            Assert.Less(Quaternion.Angle(pin.ViewRotation, expectedRotation), 0.5f, "The stored view rotation should be the orbited camera's.");
        }
    }
}
