// Run Tools > DIYsect > Show All Layers To Rig Camera.
using System;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// The template's rig prefab gives its camera a culling mask that draws only the template's own layers (0, 1, 2, 4, 5
    /// and 7). The tools moved the table, the instrument stand, the lamp and the organs onto their own layers
    /// (OperatingLight, SeatedOrgan, LooseOrgan, Environment, CloseUpScreen) for physics, and the headset camera then
    /// never drew them: the table and stand vanished and only the drapes and tools floated. This adds those layers to the
    /// rig camera's mask in the student and professor scenes. Safe to run more than once.
    /// </summary>
    public static class ShowAllLayersToRigCamera
    {
        public static readonly string[] ScenePaths = { "Assets/Scenes/LectureHall_VR.unity", "Assets/Scenes/Professor_MR.unity" };
        public static readonly string[] LayerNames = { "CloseUpScreen", "OperatingLight", "SeatedOrgan", "LooseOrgan", "Environment" };

        /// <summary>The mask bits of the project's own layers.</summary>
        public static int ProjectLayerMask()
        {
            int mask = 0;
            foreach (string name in LayerNames)
            {
                int layer = LayerMask.NameToLayer(name);
                if (layer < 0)
                    throw new InvalidOperationException($"The layer {name} does not exist. Check ProjectSettings/TagManager.asset.");
                mask |= 1 << layer;
            }
            return mask;
        }

        [MenuItem("Tools/DIYsect/Show All Layers To Rig Camera")]
        public static void Apply()
        {
            foreach (string path in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
                if (origin == null || origin.Camera == null)
                    throw new InvalidOperationException($"{path} has no XR rig camera.");
                Camera camera = origin.Camera;
                camera.cullingMask |= ProjectLayerMask();
                EditorUtility.SetDirty(camera);
                PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException($"Could not save {path}.");
                Debug.Log($"{path}: the rig camera now draws the project layers (mask {camera.cullingMask}).");
            }
        }

        // Batch entry point: -executeMethod DIYsect.Editor.ShowAllLayersToRigCamera.ApplyBatch
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
    }
}
