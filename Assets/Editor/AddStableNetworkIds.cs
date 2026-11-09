// Run Tools > DIYsect > Add Stable Network Ids after Create Scenes.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Puts <see cref="StableNetworkObjectIds"/> on the network manager object of the professor, student and classmate scenes,
    /// so the body they all place is recognised as the same object by every device. Safe to run more than once.
    /// </summary>
    public static class AddStableNetworkIds
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Add Stable Network Ids")]
        public static void Apply()
        {
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                XRINetworkGameManager manager = UnityEngine.Object.FindFirstObjectByType<XRINetworkGameManager>(FindObjectsInactive.Include);
                if (manager == null)
                    throw new InvalidOperationException($"{path} has no XRINetworkGameManager.");
                if (manager.GetComponent<StableNetworkObjectIds>() == null)
                    manager.gameObject.AddComponent<StableNetworkObjectIds>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AddStableNetworkIds] {path}: stable ids are on {manager.name}.");
            }
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddStableNetworkIds.ApplyBatch
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
