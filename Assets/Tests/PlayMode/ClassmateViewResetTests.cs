// Starts a real in-process Netcode host (localhost, no cloud services) and a classmate camera with the same parts the editor step adds.
// Authority under test: none (local camera only). Input is simulated with the Input System's InputTestFixture, not a real laptop or phone.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DIYsect.Networking.Tests
{
    public class ClassmateViewResetTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 4;

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private Keyboard _keyboard;
        private GameObject _cameraObject;
        private GameObject _uiObject;
        private Camera _camera;
        private ClassmateOrbitCamera _orbit;
        private QuestionBoard _board;
        private Button _button;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        [SetUp]
        public void SetUpInput()
        {
            _input.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
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
            _board = _serverBody.GetComponent<QuestionBoard>();
            Assert.IsNotNull(_board, "The body prefab has no QuestionBoard. Run Tools > DIYsect > Add Question Pins.");

            // A full-screen button, so any pointer position in the test is over it.
            _uiObject = new GameObject("TestResetUi", typeof(Canvas), typeof(GraphicRaycaster));
            _uiObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _uiObject.AddComponent<EventSystem>();
            _uiObject.AddComponent<InputSystemUIInputModule>();
            var buttonObject = new GameObject("Button", typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_uiObject.transform, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            _button = buttonObject.GetComponent<Button>();
            _button.targetGraphic = buttonObject.GetComponent<Image>();
            buttonObject.SetActive(false);

            _cameraObject = new GameObject("TestClassmateCamera");
            _cameraObject.SetActive(false);
            _camera = _cameraObject.AddComponent<Camera>();
            _cameraObject.transform.position = _serverBody.transform.position + new Vector3(0f, 0.9f, -0.9f);
            _cameraObject.transform.rotation = Quaternion.LookRotation(_serverBody.transform.position - _cameraObject.transform.position, Vector3.up);
            var tracker = _cameraObject.AddComponent<PointerGestureTracker>();
            Set(tracker, "_holdSeconds", 5f);
            _orbit = _cameraObject.AddComponent<ClassmateOrbitCamera>();
            Set(_orbit, "_gestures", tracker);
            Set(_orbit, "_target", _serverBody.transform);
            var pins = _cameraObject.AddComponent<PinPlacementInput>();
            Set(pins, "_board", _board);
            Set(pins, "_camera", _camera);
            Set(pins, "_gestures", tracker);
            buttonObject.SetActive(true);
            var reset = _cameraObject.AddComponent<ClassmateViewReset>();
            Set(reset, "_orbit", _orbit);
            Set(reset, "_button", _button);
            _cameraObject.SetActive(true);
            yield return null;
            _orbit.SnapToGoal();
            _startPosition = _camera.transform.position;
            _startRotation = _camera.transform.rotation;
        }

        private void MoveAway()
        {
            _orbit.Orbit(new Vector2(300f, 120f));
            _orbit.Zoom(3f);
            _orbit.SnapToGoal();
            Assert.Greater(Vector3.Distance(_startPosition, _camera.transform.position), 0.1f, "The camera should have moved away from the start.");
        }

        private void AssertBackAtStart()
        {
            _orbit.SnapToGoal();
            Assert.Less(Vector3.Distance(_startPosition, _camera.transform.position), 0.005f, "The camera should be back at its start position.");
            Assert.Less(Quaternion.Angle(_startRotation, _camera.transform.rotation), 0.5f, "The camera should be back at its start rotation.");
        }

        private IEnumerator Press(UnityEngine.InputSystem.Controls.KeyControl key)
        {
            _input.Press(key);
            yield return null;
            _input.Release(key);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResetViewReturnsToTheStartingPose()
        {
            yield return StartScene();
            MoveAway();
            _orbit.ResetView();
            AssertBackAtStart();
        }

        [UnityTest]
        public IEnumerator HomeAndRKeysResetTheView()
        {
            yield return StartScene();
            MoveAway();
            yield return Press(_keyboard.homeKey);
            AssertBackAtStart();

            MoveAway();
            yield return Press(_keyboard.rKey);
            AssertBackAtStart();
        }

        [UnityTest]
        public IEnumerator ClickingTheButtonResetsTheViewAndPlacesNoPin()
        {
            yield return StartScene();
            MoveAway();
            // The button covers the screen, so this click lands on an organ's screen position and on the button.
            MeshFilter filter = _serverBody.Organs[OrganIndex].GetComponentInChildren<MeshFilter>();
            Vector2 onOrgan = _camera.WorldToScreenPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
            _input.Set(_mouse.position, onOrgan);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;
            yield return new WaitForSeconds(0.5f);

            AssertBackAtStart();
            Assert.AreEqual(0, _board.Count, "Pressing the reset button must not place a pin.");
        }

        [UnityTest]
        public IEnumerator KeysDoNotPlaceAPin()
        {
            yield return StartScene();
            yield return Press(_keyboard.homeKey);
            yield return Press(_keyboard.rKey);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0, _board.Count, "A reset key must not place a pin.");
        }
    }
}
