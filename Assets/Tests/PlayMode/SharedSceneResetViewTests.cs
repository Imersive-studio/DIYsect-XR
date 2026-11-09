// Loads the real shared scene (Professor_MR) as a classmate and clicks or taps the Reset view and the follow buttons through whatever EventSystem is active.
// Authority under test: none (local camera only). Input is simulated with the Input System's InputTestFixture, not a real laptop or phone.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMultiplayer;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DIYsect.Networking.Tests
{
    public class SharedSceneResetViewTests
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private Touchscreen _touch;

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
            ClassroomRole.OverrideForTests = null;
            _input.TearDown();
        }

        // The scene's module uses the project-wide input actions. Once an earlier test has reset the Input System those
        // actions stay off, so the module is given its own fresh set, as a module added at runtime would get.
        private static void UseFreshUiActions()
        {
            var module = InScene<EventSystem>().GetComponent<InputSystemUIInputModule>();
            module.enabled = false;
            module.AssignDefaultActions();
            module.enabled = true;
        }

        // An earlier test can leave its own EventSystem behind, and the scene's would then not be the current one.
        private static void ClearStrayEventSystems()
        {
            foreach (EventSystem stray in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(stray.gameObject);
        }

        private IEnumerator LoadAsClassmate()
        {
            ClearStrayEventSystems();
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = ClassroomRoleKind.Classmate;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            UseFreshUiActions();
            // Let the canvas scaler settle, or the button moves after its position is read.
            for (int i = 0; i < 10; i++)
                yield return null;
        }

        // Runs even when an assert failed, so a failure here cannot leave the scene and its host running for the next test.
        [UnityTearDown]
        public IEnumerator Unload()
        {
            LogAssert.ignoreFailingMessages = true; // the host's voice chat logs while it is torn down
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterSharedResetView" + System.Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
        }

        // The canvas rescales a frame after input starts, so point the mouse at the button until the spot stops moving.
        private IEnumerator Aim(Button button)
        {
            Vector2 last = new Vector2(-1f, -1f);
            for (int i = 0; i < 20; i++)
            {
                Vector2 centre = ButtonCentre(button);
                _input.Set(_mouse.position, centre);
                yield return null;
                if (centre == last && ButtonCentre(button) == centre)
                    yield break;
                last = centre;
            }
            Assert.Fail("The Reset view button never stopped moving.");
        }

        // Only the shared scene's own objects: a test that ran earlier may have left a camera or EventSystem of its own behind.
        private static T InScene<T>() where T : Component
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(ScenePath).GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            Assert.Fail($"The shared scene has no {typeof(T).Name}.");
            return null;
        }

        private static Vector2 ButtonCentre(Button button)
        {
            Canvas.ForceUpdateCanvases(); // the scaler resizes the canvas lazily
            var corners = new Vector3[4];
            ((RectTransform)button.transform).GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f; // an overlay canvas is in screen pixels
        }

        private static ClassmateOrbitCamera MoveAway(out Vector3 startPosition, out Quaternion startRotation)
        {
            var orbit = InScene<ClassmateOrbitCamera>();
            Assert.IsNotNull(orbit, "The shared scene has no ClassmateOrbitCamera.");
            orbit.SnapToGoal();
            startPosition = orbit.transform.position;
            startRotation = orbit.transform.rotation;
            orbit.Orbit(new Vector2(300f, 120f));
            orbit.Zoom(3f);
            orbit.SnapToGoal();
            Assert.Greater(Vector3.Distance(startPosition, orbit.transform.position), 0.1f, "The camera should have moved away from the start.");
            return orbit;
        }

        private static void AssertBackAtStart(ClassmateOrbitCamera orbit, Vector3 startPosition, Quaternion startRotation)
        {
            orbit.SnapToGoal();
            Assert.Less(Vector3.Distance(startPosition, orbit.transform.position), 0.005f, "The camera should be back at its start position.");
            Assert.Less(Quaternion.Angle(startRotation, orbit.transform.rotation), 0.5f, "The camera should be back at its start rotation.");
        }

        private static string EnabledUiModules()
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (BaseInputModule module in InScene<EventSystem>().GetComponents<BaseInputModule>())
                if (module.enabled)
                    names.Add(module.GetType().Name);
            return string.Join(",", names);
        }

        [UnityTest]
        public IEnumerator EveryRoleGetsTheRightUiModule_TheClassmateOneReadsTouch_TheHeadsetKeepsTheXrOne()
        {
            LogAssert.ignoreFailingMessages = true;
            foreach (ClassroomRoleKind kind in System.Enum.GetValues(typeof(ClassroomRoleKind)))
            {
                ClearStrayEventSystems();
                ClassroomRole.OverrideForTests = kind;
#if UNITY_EDITOR
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
                yield return null;
                yield return null;
                Assert.AreEqual(1, Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, $"{kind}: one EventSystem only.");
                // A new role must be given a module here on purpose: only a classmate (a phone) has no headset ray and needs finger taps.
                string expected = kind == ClassroomRoleKind.Classmate ? "InputSystemUIInputModule" : "XRUIInputModule";
                Assert.AreEqual(expected, EnabledUiModules(), $"{kind}: enabled UI module");
                yield return Unload();
            }
        }

        [UnityTest]
        public IEnumerator EveryClassmateCanvasInTheSharedSceneSitsUnderASafeAreaPanel()
        {
            yield return LoadAsClassmate();
            var rig = InScene<ClassmateViewReset>().transform;
            var canvases = rig.GetComponentsInChildren<Canvas>(true);
            Assert.IsNotEmpty(canvases);
            foreach (Canvas canvas in canvases)
            {
                Assert.IsNotNull(canvas.GetComponent<CanvasOrientationScaler>(), $"{canvas.name} needs the orientation scaler.");
                var panel = canvas.transform.Find(ClassmateCanvasLayout.PanelName);
                Assert.IsNotNull(panel?.GetComponent<SafeAreaFitter>(), $"{canvas.name} needs a SafeArea panel.");
                Assert.AreEqual(1, canvas.transform.childCount, $"{canvas.name}: everything belongs under the SafeArea panel.");
            }
        }

        [UnityTest]
        public IEnumerator ClickingResetViewInTheSharedSceneReturnsTheClassmateCameraAndPlacesNoPin()
        {
            yield return LoadAsClassmate();
            var button = InScene<ClassmateViewReset>().GetComponentInChildren<Button>();
            Assert.IsTrue(button.gameObject.activeInHierarchy);
            ClassmateOrbitCamera orbit = MoveAway(out Vector3 position, out Quaternion rotation);

            yield return Aim(button);
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;
            yield return new WaitForSeconds(0.5f);

            AssertBackAtStart(orbit, position, rotation);
            Assert.AreEqual(0, InScene<QuestionBoard>().Count, "Clicking Reset view must not place a pin.");
        }

        [UnityTest]
        public IEnumerator TappingResetViewInTheSharedSceneReturnsTheClassmateCameraAndPlacesNoPin()
        {
            yield return LoadAsClassmate();
            var button = InScene<ClassmateViewReset>().GetComponentInChildren<Button>();
            ClassmateOrbitCamera orbit = MoveAway(out Vector3 position, out Quaternion rotation);

            yield return Aim(button);
            Vector2 centre = ButtonCentre(button);
            _input.BeginTouch(1, centre);
            for (int i = 0; i < 4; i++)
                yield return null;
            _input.EndTouch(1, centre);
            yield return null;
            yield return new WaitForSeconds(0.5f);

            AssertBackAtStart(orbit, position, rotation);
            Assert.AreEqual(0, InScene<QuestionBoard>().Count, "Tapping Reset view must not place a pin.");
        }

        private static Button FollowButton(string buttonName)
        {
            foreach (Button button in InScene<ClassmateFollowButtons>().GetComponentsInChildren<Button>(true))
                if (button.name == buttonName)
                    return button;
            Assert.Fail($"The shared scene has no {buttonName}.");
            return null;
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator FollowButtonsAreThumbSizedAndKeepClearOfTheResetButtonTheCentreAndTheToast()
        {
            yield return LoadAsClassmate();
            var buttons = InScene<ClassmateFollowButtons>();
            Rect reset = ScreenRect((RectTransform)InScene<ClassmateViewReset>().GetComponentInChildren<Button>().transform);
            Rect toast = ScreenRect((RectTransform)InScene<PinRejectionToast>().GetComponentInChildren<Text>(true).transform);
            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            var centre = new Rect(Screen.width * 0.3f, Screen.height * 0.2f, Screen.width * 0.4f, Screen.height * 0.6f);
            var seen = new System.Collections.Generic.List<Rect>();
            foreach (string buttonName in new[] { "FollowProfessorButton", "FollowStudentButton", "BackToMyViewButton" })
            {
                Button button = FollowButton(buttonName);
                Rect rect = ScreenRect((RectTransform)button.transform);
                Assert.GreaterOrEqual(rect.height, Screen.height * 0.08f, $"{button.name} must be thumb-sized.");
                Assert.IsTrue(screen.Contains(rect.min) && screen.Contains(rect.max), $"{button.name} must be on screen.");
                Assert.IsFalse(rect.Overlaps(reset), $"{button.name} must not cover Reset view.");
                Assert.IsFalse(rect.Overlaps(toast), $"{button.name} must not cover the toast.");
                Assert.IsFalse(rect.Overlaps(centre), $"{button.name} must not cover the body.");
                Assert.Greater(rect.center.x, Screen.width * 0.5f, $"{button.name} must stay clear of the status line on the left.");
                foreach (Rect other in seen)
                    Assert.IsFalse(rect.Overlaps(other), $"{button.name} must not cover another follow button.");
                seen.Add(rect);
            }
        }

        [UnityTest]
        public IEnumerator FollowButtonsAreOffWhenNobodyIsInTheSessionAndAClickMovesNothingAndPlacesNoPin()
        {
            yield return LoadAsClassmate();
            var follow = InScene<ClassmateFollowCamera>();
            ClassmateOrbitCamera orbit = MoveAway(out Vector3 position, out Quaternion rotation);
            Vector3 moved = orbit.transform.position;
            Button professor = FollowButton("FollowProfessorButton");
            Assert.IsFalse(professor.interactable, "Nobody has published a head, so Follow professor is off.");

            yield return Aim(professor);
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(FollowTarget.Mine, follow.Target);
            Assert.Less(Vector3.Distance(moved, orbit.transform.position), 0.001f, "A disabled follow button must not move the camera.");
            Assert.AreEqual(0, InScene<QuestionBoard>().Count, "Clicking a follow button must not place a pin.");
        }

        [UnityTest]
        public IEnumerator ClickingFollowProfessorInTheSharedSceneFollowsTheirHeadAndBackReturnsAndPlacesNoPin()
        {
            yield return LoadAsClassmate();
            var follow = InScene<ClassmateFollowCamera>();
            var heads = InScene<HeadPoseBoard>();
            // This device hosts a local test room (no cloud services) so the scene's own body and head board are spawned.
            NetworkManager manager = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
            Assert.IsTrue(manager.StartHost(), "The test room did not start.");
            float end = Time.realtimeSinceStartup + 8f;
            while (!heads.IsSpawned && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(heads.IsSpawned, "The scene's body never spawned.");

            ClassmateOrbitCamera orbit = MoveAway(out _, out _);
            Vector3 minePosition = orbit.transform.position;
            Quaternion mineRotation = orbit.transform.rotation;
            var localPosition = new Vector3(0.3f, 0.7f, -0.6f);
            var localRotation = Quaternion.Euler(35f, 200f, 0f);
            heads.Publish(localPosition, localRotation);
            end = Time.realtimeSinceStartup + 8f;
            while (!heads.IsPresent(DIYsect.Networking.PointerRole.Professor) && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(heads.IsPresent(DIYsect.Networking.PointerRole.Professor));

            Button professor = FollowButton("FollowProfessorButton");
            Button back = FollowButton("BackToMyViewButton");
            Assert.IsTrue(professor.interactable);
            yield return Aim(professor);
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return new WaitForSeconds(follow.EaseSeconds * 1.5f);

            Assert.AreEqual(FollowTarget.Professor, follow.Target);
            Assert.Less(Vector3.Distance(heads.transform.TransformPoint(localPosition), orbit.transform.position), 0.01f, "The camera should be at the professor's head.");
            Assert.AreEqual(0, InScene<QuestionBoard>().Count, "Clicking Follow professor must not place a pin.");

            Assert.IsTrue(back.gameObject.activeInHierarchy, "Back to my view shows while following.");
            yield return Aim(back);
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return new WaitForSeconds(follow.EaseSeconds * 1.5f);

            Assert.AreEqual(FollowTarget.Mine, follow.Target);
            AssertBackAtStart(orbit, minePosition, mineRotation);
            Assert.AreEqual(0, InScene<QuestionBoard>().Count, "Clicking Back to my view must not place a pin.");
        }
    }
}
