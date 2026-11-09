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
        private static readonly Vector3 _cameraOffset = new Vector3(0f, 0.9f, -0.9f);

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
                Debug.Log($"{scenePath} already has the {RigName}, keeping its camera.");
            }
            AddRejectionToast(existingRig, board);
            AddOrbitControls(existingRig, board);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {scenePath}.");
            Debug.Log($"Set up the {RigName} in {scenePath}.");
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

        // A screen overlay with one line of text near the bottom. Hidden until the server refuses a pin.
        private static void AddRejectionToast(GameObject rig, QuestionBoard board)
        {
            if (rig.GetComponentInChildren<PinRejectionToast>(true) != null)
                return;

            var canvasObject = new GameObject("PinRejectionCanvas");
            canvasObject.transform.SetParent(rig.transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            var labelObject = new GameObject("Message");
            labelObject.transform.SetParent(canvasObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            var shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.1f, 0.08f);
            rect.anchorMax = new Vector2(0.9f, 0.18f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var toast = rig.AddComponent<PinRejectionToast>();
            var serialized = new SerializedObject(toast);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_label").objectReferenceValue = label;
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
