// Starts a real in-process Netcode host (localhost, no cloud services) and a classmate camera with the same parts the editor step adds.
// Authority under test: none (local camera only). Input is simulated with the Input System's InputTestFixture, not a real laptop or phone.
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
    public class ClassmateFollowTests : NetworkedBodyTestBase
    {
        private static readonly Vector3 ProfessorPosition = new Vector3(0.3f, 0.7f, -0.6f);
        private static readonly Quaternion ProfessorRotation = Quaternion.Euler(35f, 200f, 18f);
        private const float EaseSeconds = 0.5f;

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private GameObject _cameraObject;
        private GameObject _uiObject;
        private Camera _camera;
        private ClassmateOrbitCamera _orbit;
        private ClassmateFollowCamera _follow;
        private ClassmateViewReset _reset;
        private HeadPoseBoard _heads;
        private QuestionBoard _board;
        private Button _professorButton;
        private Button _studentButton;
        private Button _backButton;
        private Text _studentLabel;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

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

        private static Button MakeButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out Text label)
        {
            var buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            var labelObject = new GameObject("Label", typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);
            label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return button;
        }

        // professorFillsScreen: the Follow professor button covers everything, so a click anywhere (even on an organ) lands on it.
        private IEnumerator StartScene(bool professorFillsScreen = false)
        {
            yield return StartHostAndSpawnBody();
            _board = _serverBody.GetComponent<QuestionBoard>();
            _heads = _serverBody.GetComponent<HeadPoseBoard>();
            Assert.IsNotNull(_heads, "The body prefab has no HeadPoseBoard. Run Tools > DIYsect > Add Head Pose.");

            _uiObject = new GameObject("TestFollowUi", typeof(Canvas), typeof(GraphicRaycaster));
            _uiObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            _uiObject.AddComponent<EventSystem>();
            _uiObject.AddComponent<InputSystemUIInputModule>();
            _uiObject.SetActive(false);
            _professorButton = MakeButton(_uiObject.transform, "Professor",
                professorFillsScreen ? Vector2.zero : new Vector2(0.85f, 0.7f), professorFillsScreen ? Vector2.one : new Vector2(1f, 0.8f), out Text professorLabel);
            _studentButton = MakeButton(_uiObject.transform, "Student", new Vector2(0.85f, 0.55f), new Vector2(1f, 0.65f), out _studentLabel);
            _backButton = MakeButton(_uiObject.transform, "Back", new Vector2(0.85f, 0.4f), new Vector2(1f, 0.5f), out _);

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
            _follow = _cameraObject.AddComponent<ClassmateFollowCamera>();
            Set(_follow, "_orbit", _orbit);
            Set(_follow, "_board", _heads);
            Set(_follow, "_easeSeconds", EaseSeconds);
            var buttons = _cameraObject.AddComponent<ClassmateFollowButtons>();
            Set(buttons, "_follow", _follow);
            Set(buttons, "_professorButton", _professorButton);
            Set(buttons, "_professorLabel", professorLabel);
            Set(buttons, "_studentButton", _studentButton);
            Set(buttons, "_studentLabel", _studentLabel);
            Set(buttons, "_backButton", _backButton);
            _reset = _cameraObject.AddComponent<ClassmateViewReset>();
            Set(_reset, "_orbit", _orbit);
            Set(_reset, "_button", _backButton); // any button: only the Reset() call is used here
            Set(_reset, "_follow", _follow);
            _uiObject.SetActive(true);
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

        private IEnumerator ProfessorArrives()
        {
            _heads.Publish(ProfessorPosition, ProfessorRotation);
            yield return WaitFor(() => _heads.IsPresent(PointerRole.Professor), "the professor's head");
        }

        private Vector3 ProfessorWorldPosition() => _serverBody.transform.TransformPoint(ProfessorPosition);

        private Quaternion ProfessorWorldRotation() => FollowTransition.Level(_serverBody.transform.rotation * ProfessorRotation);

        private void AssertAtProfessor()
        {
            Assert.Less(Vector3.Distance(ProfessorWorldPosition(), _camera.transform.position), 0.005f, "The camera should be at the professor's head.");
            Assert.Less(Quaternion.Angle(ProfessorWorldRotation(), _camera.transform.rotation), 0.5f, "The camera should look where the professor looks.");
        }

        private void AssertAtPose(Vector3 position, Quaternion rotation, string what)
        {
            Assert.Less(Vector3.Distance(position, _camera.transform.position), 0.005f, $"The camera should be at {what}.");
            Assert.Less(Quaternion.Angle(rotation, _camera.transform.rotation), 0.5f, $"The camera should look as at {what}.");
        }

        private Vector2 CentreOf(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        private IEnumerator Click(Vector2 screenPosition)
        {
            _input.Set(_mouse.position, screenPosition);
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FollowingTheProfessorEasesToTheirEyesAndBackToMyViewReturnsToTheSavedOrbit()
        {
            yield return StartScene();
            MoveAway();
            Vector3 minePosition = _camera.transform.position;
            Quaternion mineRotation = _camera.transform.rotation;
            yield return ProfessorArrives();

            Assert.IsTrue(_follow.Follow(FollowTarget.Professor));
            yield return null;
            float total = Vector3.Distance(minePosition, ProfessorWorldPosition());
            Assert.Less(Vector3.Distance(minePosition, _camera.transform.position), total * 0.1f, "The first frame must not jump.");
            yield return new WaitForSeconds(EaseSeconds * 0.5f);
            float midway = Vector3.Distance(minePosition, _camera.transform.position);
            Assert.Greater(midway, total * 0.1f, "Halfway through the camera should be on its way.");
            Assert.Less(midway, total * 0.95f, "Halfway through the camera should not have arrived.");
            yield return new WaitForSeconds(EaseSeconds);
            AssertAtProfessor();

            _follow.BackToMyView();
            yield return null;
            Assert.Less(Vector3.Distance(ProfessorWorldPosition(), _camera.transform.position), total * 0.1f, "Leaving follow must not jump either.");
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            AssertAtPose(minePosition, mineRotation, "my saved orbit view");
            Assert.IsFalse(_orbit.Driven, "The orbit camera owns the camera again.");
            Assert.AreEqual(FollowTarget.Mine, _follow.Target);
        }

        [UnityTest]
        public IEnumerator FollowingSomeoneWhoIsNotInTheSessionIsRefusedAndTheButtonIsDisabled()
        {
            yield return StartScene();
            MoveAway();
            Vector3 position = _camera.transform.position;
            Assert.IsFalse(_follow.Follow(FollowTarget.Student));
            Assert.IsFalse(_studentButton.interactable, "Nobody to follow, so the button is off.");
            Assert.IsFalse(_professorButton.interactable);
            Assert.IsFalse(_backButton.gameObject.activeSelf, "Back to my view shows only while following.");
            StringAssert.Contains("No student", _studentLabel.text);
            yield return new WaitForSeconds(0.3f);
            Assert.Less(Vector3.Distance(position, _camera.transform.position), 0.001f);
            Assert.AreEqual(FollowTarget.Mine, _follow.Target);
        }

        [UnityTest]
        public IEnumerator ClickingFollowProfessorFollowsAndPlacesNoPin()
        {
            yield return StartScene(professorFillsScreen: true);
            yield return ProfessorArrives();
            yield return null;
            Assert.IsTrue(_professorButton.interactable);

            // The button covers the screen, so this click lands on an organ's screen position and on the button.
            MeshFilter filter = _serverBody.Organs[4].GetComponentInChildren<MeshFilter>();
            Vector2 onOrgan = _camera.WorldToScreenPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
            yield return Click(onOrgan);
            yield return new WaitForSeconds(EaseSeconds * 1.5f);

            Assert.AreEqual(FollowTarget.Professor, _follow.Target);
            AssertAtProfessor();
            Assert.AreEqual(0, _board.Count, "Pressing the follow button must not place a pin.");
        }

        [UnityTest]
        public IEnumerator ResetViewWhileFollowingGoesBackToMyViewAndNotToTheStart()
        {
            yield return StartScene();
            MoveAway();
            Vector3 minePosition = _camera.transform.position;
            Quaternion mineRotation = _camera.transform.rotation;
            yield return ProfessorArrives();
            _follow.Follow(FollowTarget.Professor);
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            AssertAtProfessor();

            _reset.Reset();
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            AssertAtPose(minePosition, mineRotation, "my view, not the starting view");
            Assert.AreEqual(FollowTarget.Mine, _follow.Target);

            _reset.Reset();
            yield return new WaitForSeconds(1f);
            AssertAtPose(_startPosition, _startRotation, "the starting view after a second Reset view");
        }

        [UnityTest]
        public IEnumerator DraggingWhileFollowingDoesNotMoveTheCamera()
        {
            yield return StartScene();
            yield return ProfessorArrives();
            _follow.Follow(FollowTarget.Professor);
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            float yaw = _orbit.Yaw;

            _input.Set(_mouse.position, new Vector2(100f, 100f));
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            for (int i = 1; i <= 5; i++)
            {
                _input.Set(_mouse.position, new Vector2(100f + i * 40f, 100f));
                yield return null;
            }
            _input.Release(_mouse.leftButton);
            yield return null;

            AssertAtProfessor();
            Assert.AreEqual(yaw, _orbit.Yaw, 0.01f, "My orbit state must not change while following.");
            Assert.AreEqual(FollowTarget.Professor, _follow.Target, "A drag does not leave follow.");
        }

        [UnityTest]
        public IEnumerator WhenTheFollowedProfessorLeavesTheCameraEasesBackToMyView()
        {
            yield return StartScene();
            MoveAway();
            Vector3 minePosition = _camera.transform.position;
            Quaternion mineRotation = _camera.transform.rotation;
            string message = null;
            yield return ProfessorArrives();
            _follow.Message += text => message = text;
            _follow.Follow(FollowTarget.Professor);
            yield return new WaitForSeconds(EaseSeconds * 1.5f);

            _heads.Withdraw();
            yield return WaitFor(() => _follow.Target == FollowTarget.Mine, "the camera to head back");
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            AssertAtPose(minePosition, mineRotation, "my view");
            Assert.AreEqual("Professor left", message);
        }

        [UnityTest]
        public IEnumerator SwitchingFromTheProfessorToTheStudentEasesAndDoesNotJump()
        {
            yield return StartScene();
            yield return ProfessorArrives();
            // The host is the professor, so the student's head comes from a client.
            NetworkManager client = StartClient("Student");
            SharedBody clientBody = null;
            yield return WaitForBody(client, found => clientBody = found);
            var studentPosition = new Vector3(-0.5f, 0.5f, 0.6f);
            var studentRotation = Quaternion.Euler(20f, 40f, 0f);
            clientBody.GetComponent<HeadPoseBoard>().Publish(studentPosition, studentRotation);
            yield return WaitFor(() => _heads.IsPresent(PointerRole.Student), "the student's head");

            _follow.Follow(FollowTarget.Professor);
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            AssertAtProfessor();
            Assert.IsTrue(_follow.Follow(FollowTarget.Student));
            yield return null;
            Assert.Less(Vector3.Distance(ProfessorWorldPosition(), _camera.transform.position), 0.05f, "Switching person must not jump.");
            yield return new WaitForSeconds(EaseSeconds * 1.5f);
            AssertAtPose(_serverBody.transform.TransformPoint(studentPosition), FollowTransition.Level(_serverBody.transform.rotation * studentRotation), "the student's head");
        }
    }
}
