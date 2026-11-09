// Run Tools > DIYsect > Add Classmate Desktop Camera after Add Question Pins.
using System;
using System.IO;
using DIYsect.Classroom;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives Classmate_Desktop a plain camera that looks at the body and lets a click or tap pin a question, lets a drag or pinch orbit and zoom around the body, and shows a short message when the server refuses a pin.
    /// The XR Interaction Simulator and the template's XR rig are switched off in that scene, because a
    /// classmate has no headset. Safe to run more than once.
    /// </summary>
    public static class ClassmateDesktopRig
    {
        private const string ScenePath = "Assets/Scenes/Classmate_Desktop.unity";
        private const string RigName = "ClassmateCamera";
        private const string SimulatorName = "XR Interaction Simulator";
        // The opening view: lower and a little farther back, so a phone shows the hall, the class and the body together.
        private static readonly Vector3 _cameraOffset = ClassmateOpeningView.Offset();

        [MenuItem("Tools/DIYsect/Add Classmate Desktop Camera")]
        public static void Apply() => Apply(ScenePath, true);

        /// <summary>
        /// Builds the classmate camera in any scene that has the body. <paramref name="switchOffXr"/> turns the headset rig off,
        /// which is right for the classmate-only scene; a shared scene keeps it, and a role switch picks one at start.
        /// </summary>
        public static void Apply(string scenePath, bool switchOffXr)
        {
            if (!File.Exists(scenePath))
                throw new FileNotFoundException($"{scenePath} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            QuestionBoard board = null;
            GameObject existingRig = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == RigName)
                    existingRig = root;
                if (board == null)
                    board = root.GetComponentInChildren<QuestionBoard>(true);
            }
            if (board == null)
                throw new InvalidOperationException("No body with a QuestionBoard in the scene. Run Tools > DIYsect > Add Question Pins first.");

            if (existingRig == null)
            {
                existingRig = CreateRig(board);
                if (switchOffXr)
                    SwitchOffXr(scene);
            }
            else
            {
                // Keep the camera, but always start it from the opening view, so a change to that view reaches existing scenes.
                Vector3 target = board.transform.position;
                existingRig.transform.position = target + _cameraOffset;
                existingRig.transform.rotation = Quaternion.LookRotation(target - existingRig.transform.position, Vector3.up);
            }
            AddRejectionToast(existingRig, board);
            AddOrbitControls(existingRig, board);
            AddViewReset(existingRig);
            AddReconnectBannerIfJoinExists(existingRig);
            AddFollowControls(existingRig, board);
            // After every canvas exists, so the follow buttons and the banner are inside the safe area too.
            FitCanvasesToPhone(existingRig);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {scenePath}.");
            Debug.Log($"Set up the {RigName} in {scenePath}.");
        }

        // The banner needs the scene's auto join. BuildSharedScene adds the auto join after this step, so it adds the banner itself the first time.
        private static void AddReconnectBannerIfJoinExists(GameObject rig)
        {
            var join = UnityEngine.Object.FindFirstObjectByType<XRMultiplayer.ClassmateAutoJoin>(FindObjectsInactive.Include);
            if (join != null)
                AddReconnectBanner.AddTo(rig, join);
        }

        private static GameObject CreateRig(QuestionBoard board)
        {
            // The body sits on the table; look at it from the foot side, above and behind.
            Vector3 target = board.transform.position;
            var rig = new GameObject(RigName);
            var camera = rig.AddComponent<Camera>();
            rig.tag = "MainCamera";
            rig.AddComponent<AudioListener>();
            camera.nearClipPlane = 0.05f;
            rig.transform.position = target + _cameraOffset;
            rig.transform.rotation = Quaternion.LookRotation(target - rig.transform.position, Vector3.up);

            var input = rig.AddComponent<PinPlacementInput>();
            var serialized = new SerializedObject(input);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_camera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        // The tracker reads mouse and touch, the orbit camera moves the rig around the body, and the pin placer listens for taps only.
        // Safe on a rig that already has them: missing parts are added and the references are set again.
        private static void AddOrbitControls(GameObject rig, QuestionBoard board)
        {
            var tracker = rig.GetComponent<PointerGestureTracker>();
            if (tracker == null)
                tracker = rig.AddComponent<PointerGestureTracker>();
            var orbit = rig.GetComponent<ClassmateOrbitCamera>();
            if (orbit == null)
                orbit = rig.AddComponent<ClassmateOrbitCamera>();

            var orbitSerialized = new SerializedObject(orbit);
            orbitSerialized.FindProperty("_gestures").objectReferenceValue = tracker;
            orbitSerialized.FindProperty("_target").objectReferenceValue = board.transform;
            orbitSerialized.ApplyModifiedPropertiesWithoutUndo();

            var pins = rig.GetComponent<PinPlacementInput>();
            if (pins == null)
                throw new InvalidOperationException($"{RigName} has no PinPlacementInput.");
            var pinSerialized = new SerializedObject(pins);
            pinSerialized.FindProperty("_gestures").objectReferenceValue = tracker;
            pinSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Every uGUI canvas under the classmate camera (also ones other tools add) gets a SafeArea panel and an orientation-aware scaler.
        private static void FitCanvasesToPhone(GameObject rig)
        {
            foreach (Canvas canvas in rig.GetComponentsInChildren<Canvas>(true))
                ClassmateCanvasLayout.Apply(canvas);
        }

        // Batch entry point: -executeMethod DIYsect.Editor.ClassmateDesktopRig.ApplyBatch
        public static void ApplyBatch()
        {
            try
            {
                Apply();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        // A glass chip just above the dock. Hidden until the server refuses a pin. Safe on a rig whose canvas was removed: it builds a new one.
        private static void AddRejectionToast(GameObject rig, QuestionBoard board)
        {
            var toast = rig.GetComponent<PinRejectionToast>();
            if (!ClassmateUiBuilder.NeedsUi(toast, "_label"))
                return;
            toast = toast != null ? toast : rig.AddComponent<PinRejectionToast>();

            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, "PinRejectionCanvas", clickable: false);
            TMPro.TextMeshProUGUI label = ClassmateUiBuilder.AddChip(canvas.transform, "Message", new Vector2(0.5f, 0f), new Vector2(0f, AddClassmateBottomBar.AboveDock),
                ClassmateUiBuilder.SmallSize, DIYsect.Presentation.GuidePalette.TextPrimary, hidden: true);

            var serialized = new SerializedObject(toast);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // "Reset view" heads the column of view pills in the top-right corner (clear of the status chip on the left and the dock
        // at the bottom), plus the Home and R keys. A UI button needs an EventSystem, which also lets the gesture tracker ignore presses on it.
        private static void AddViewReset(GameObject rig)
        {
            var reset = rig.GetComponent<ClassmateViewReset>();
            if (!ClassmateUiBuilder.NeedsUi(reset, "_button"))
                return;
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                // Under the classmate camera, so the professor's headset never gets a second EventSystem.
                var events = new GameObject("EventSystem");
                events.transform.SetParent(rig.transform, false);
                events.AddComponent<UnityEngine.EventSystems.EventSystem>();
                events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, "ViewResetCanvas", clickable: true);
            Button button = MakeButton(canvas.transform, "ResetViewButton", "Reset view", 0, out _);

            reset = reset != null ? reset : rig.AddComponent<ClassmateViewReset>();
            var serialized = new SerializedObject(reset);
            serialized.FindProperty("_orbit").objectReferenceValue = rig.GetComponent<ClassmateOrbitCamera>();
            serialized.FindProperty("_button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public const float ViewButtonWidth = 228f;

        // A glass pill in the top-right column, <paramref name="slot"/> places down from the top (0 is the first).
        private static Button MakeButton(Transform parent, string name, string text, int slot, out TMPro.TextMeshProUGUI label)
        {
            Button button = ClassmateUiBuilder.AddButton(parent, name, text, ClassmateUiBuilder.SmallSize, ClassmateUiBuilder.Kind.Glass, out label, ClassmateUiBuilder.SmallButtonHeight);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(ViewButtonWidth, ClassmateUiBuilder.SmallButtonHeight);
            rect.anchoredPosition = new Vector2(-ClassmateUiBuilder.Margin, -ClassmateUiBuilder.Margin - slot * (ClassmateUiBuilder.SmallButtonHeight + 10f));
            return button;
        }

        // Three buttons in the right-hand column under Reset view: Follow professor, Follow student, and Back to my view
        // (shown only while following). The column is clear of the status line (top-left), the toast (bottom) and the body (centre).
        // Safe on a rig that already has them: the references are set again.
        private static void AddFollowControls(GameObject rig, QuestionBoard board)
        {
            var orbit = rig.GetComponent<ClassmateOrbitCamera>() ?? throw new InvalidOperationException($"{RigName} has no ClassmateOrbitCamera.");
            var heads = board.GetComponent<HeadPoseBoard>() ?? throw new InvalidOperationException("The body has no HeadPoseBoard. Run Tools > DIYsect > Add Head Pose first.");

            var follow = rig.GetComponent<ClassmateFollowCamera>();
            if (follow == null)
                follow = rig.AddComponent<ClassmateFollowCamera>();
            var followSerialized = new SerializedObject(follow);
            followSerialized.FindProperty("_orbit").objectReferenceValue = orbit;
            followSerialized.FindProperty("_board").objectReferenceValue = heads;
            followSerialized.ApplyModifiedPropertiesWithoutUndo();

            var reset = rig.GetComponent<ClassmateViewReset>() ?? throw new InvalidOperationException($"{RigName} has no ClassmateViewReset.");
            var resetSerialized = new SerializedObject(reset);
            resetSerialized.FindProperty("_follow").objectReferenceValue = follow;
            resetSerialized.ApplyModifiedPropertiesWithoutUndo();

            var buttons = rig.GetComponent<ClassmateFollowButtons>();
            if (!ClassmateUiBuilder.NeedsUi(buttons, "_professorButton"))
                return;
            Transform canvas = rig.transform.Find("ViewResetCanvas") ?? throw new InvalidOperationException($"{RigName} has no ViewResetCanvas.");
            Transform column = canvas.Find(ClassmateCanvasLayout.PanelName) ?? canvas;
            Button professor = MakeButton(column, "FollowProfessorButton", "Follow professor", 1, out TMPro.TextMeshProUGUI professorLabel);
            Button student = MakeButton(column, "FollowStudentButton", "Follow student", 2, out TMPro.TextMeshProUGUI studentLabel);
            Button back = MakeButton(column, "BackToMyViewButton", "Back to my view", 3, out _);

            buttons = buttons != null ? buttons : rig.AddComponent<ClassmateFollowButtons>();
            var serialized = new SerializedObject(buttons);
            serialized.FindProperty("_follow").objectReferenceValue = follow;
            serialized.FindProperty("_professorButton").objectReferenceValue = professor;
            serialized.FindProperty("_professorLabel").objectReferenceValue = professorLabel;
            serialized.FindProperty("_studentButton").objectReferenceValue = student;
            serialized.FindProperty("_studentLabel").objectReferenceValue = studentLabel;
            serialized.FindProperty("_backButton").objectReferenceValue = back;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SwitchOffXr(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == SimulatorName)
                    root.SetActive(false);
                foreach (XROrigin origin in root.GetComponentsInChildren<XROrigin>(true))
                    origin.gameObject.SetActive(false);
            }
            // The simulator is placed under the DIYsect root.
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == SimulatorName)
                        child.gameObject.SetActive(false);
        }
    }
}
