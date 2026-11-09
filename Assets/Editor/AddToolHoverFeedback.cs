// Run Tools > DIYsect > Add Tool Hover Feedback after Create Scalpel and Create Forceps.
using System;
using System.IO;
using DIYsect.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds ToolHoverFeedback (hover tint and grab pulse) to the scalpel and forceps prefabs.
    /// Safe to run more than once: an existing component is reused and values you changed are kept.
    /// </summary>
    public static class AddToolHoverFeedback
    {
        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/ScalpelPlaceholder.prefab",
            "Assets/Prefabs/ForcepsPlaceholder.prefab",
        };

        [MenuItem("Tools/DIYsect/Add Tool Hover Feedback")]
        public static void Apply()
        {
            foreach (string path in PrefabPaths)
                ApplyTo(path);
        }

        private static void ApplyTo(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. Create the scalpel and forceps first.");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                XRGrabInteractable grab = root.GetComponentInChildren<XRGrabInteractable>(true);
                if (grab == null)
                    throw new InvalidOperationException($"{path} has no XRGrabInteractable.");
                GetOrAdd<ToolHoverFeedback>(grab.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[AddToolHoverFeedback] Added hover feedback to {path}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddToolHoverFeedback.ApplyBatch
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
