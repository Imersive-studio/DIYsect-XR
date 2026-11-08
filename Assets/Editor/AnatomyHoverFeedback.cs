// Run Tools > DIYsect > Add Hover Labels And Haptics after Add Body Networking and Add Organ Sockets.
using System;
using System.IO;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the hover name label and the controller haptics. Every organ in Assets/Prefabs/AnatomyBody.prefab gets an
    /// OrganHoverLabel and an OrganHaptics (wired to the body's OrganSocketKeeper). The scalpel prefab gets a
    /// ScalpelCutHaptics. Safe to run more than once: existing components are reused and values you changed are kept.
    /// </summary>
    public static class AnatomyHoverFeedback
    {
        private const string BodyPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string ScalpelPath = "Assets/Prefabs/ScalpelPlaceholder.prefab";

        [MenuItem("Tools/DIYsect/Add Hover Labels And Haptics")]
        public static void Apply()
        {
            ApplyToBody();
            ApplyToScalpel();
        }

        private static void ApplyToBody()
        {
            RequireFile(BodyPath, "Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(BodyPath);
            try
            {
                var body = root.GetComponent<SharedBody>();
                var keeper = root.GetComponent<OrganSocketKeeper>();
                if (body == null || keeper == null)
                    throw new InvalidOperationException("The body has no SharedBody or OrganSocketKeeper. Run Add Body Networking and Add Organ Sockets first.");

                foreach (var organ in body.Organs)
                {
                    if (!organ.TryGetComponent(out XRGrabInteractable _))
                        throw new InvalidOperationException($"{organ.name} has no XRGrabInteractable. Run Tools > DIYsect > Add Body Networking first.");
                    GetOrAdd<OrganHoverLabel>(organ.gameObject);
                    var haptics = GetOrAdd<OrganHaptics>(organ.gameObject);
                    var serialized = new SerializedObject(haptics);
                    serialized.FindProperty("_keeper").objectReferenceValue = keeper;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, BodyPath);
                Debug.Log($"Added hover labels and haptics to {BodyPath} ({body.Organs.Count} organs).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScalpel()
        {
            RequireFile(ScalpelPath, "Run Tools > DIYsect > Create Scalpel first.");
            GameObject root = PrefabUtility.LoadPrefabContents(ScalpelPath);
            try
            {
                ScalpelTool tool = root.GetComponentInChildren<ScalpelTool>(true);
                if (tool == null)
                    throw new InvalidOperationException("The scalpel prefab has no ScalpelTool. Run Tools > DIYsect > Create Scalpel first.");
                GetOrAdd<ScalpelCutHaptics>(tool.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, ScalpelPath);
                Debug.Log($"Added cut haptics to {ScalpelPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RequireFile(string path, string hint)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. {hint}");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }
    }
}
