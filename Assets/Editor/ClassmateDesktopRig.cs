// Run Tools > DIYsect > Add Classmate Desktop Camera after Add Question Pins.
using System;
using System.IO;
using DIYsect.Classroom;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives Classmate_Desktop a plain camera that looks at the body and lets a click or tap pin a question.
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
        public static void Apply()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException($"{ScenePath} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            QuestionBoard board = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == RigName)
                {
                    Debug.Log($"{ScenePath} already has the {RigName}, leaving it unchanged.");
                    return;
                }
                if (board == null)
                    board = root.GetComponentInChildren<QuestionBoard>(true);
            }
            if (board == null)
                throw new InvalidOperationException("No body with a QuestionBoard in the scene. Run Tools > DIYsect > Add Question Pins first.");

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

            SwitchOffXr(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"Added the {RigName} to {ScenePath}.");
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
