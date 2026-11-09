// Run Tools > DIYsect > Rebuild Phone UI, or: Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.RebuildPhoneUi.ApplyBatch
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Rebuilds every screen on the classmate's phone in the current style. It removes the phone's canvases from the classmate
    /// camera in Professor_MR and Classmate_Desktop (the scripts that drive them stay, with all their other settings), then runs the quiz line step and
    /// Build Shared Scene, whose steps build each missing screen again and wire it back to its script. Run it after changing
    /// <see cref="ClassmateUiBuilder"/> or any phone screen step. Safe to run more than once.
    /// </summary>
    public static class RebuildPhoneUi
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string DesktopScenePath = "Assets/Scenes/Classmate_Desktop.unity";
        private const string RigName = "ClassmateCamera";

        [MenuItem("Tools/DIYsect/Rebuild Phone UI")]
        public static void Apply()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                Rebuild();
        }

        public static void ApplyBatch()
        {
            try
            {
                Rebuild();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Rebuild()
        {
            RemoveCanvases(ScenePath);
            AddQuiz.AddClassmateControls();
            BuildSharedScene.Apply();

            // The laptop fallback scene has the same camera with the pin message, the view pills and the banner.
            RemoveCanvases(DesktopScenePath);
            ClassmateDesktopRig.Apply(DesktopScenePath, switchOffXr: true);
        }

        private static void RemoveCanvases(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject rig = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == RigName)
                        rig = t.gameObject;
            if (rig == null)
                throw new InvalidOperationException($"{scenePath} has no {RigName}. Run Tools > DIYsect > Build Shared Scene first.");

            int removed = 0;
            foreach (Canvas canvas in rig.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas == null || !canvas.isRootCanvas)
                    continue;
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                removed++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {scenePath}.");
            Debug.Log($"[RebuildPhoneUi] {scenePath}: removed {removed} phone canvases. Building them again.");
        }
    }
}
