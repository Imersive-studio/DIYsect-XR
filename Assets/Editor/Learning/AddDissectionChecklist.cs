// Run Tools > DIYsect > Add Dissection Checklist after Create Scenes, Add Body Networking, Add Body Dissection and Add Organ Sockets.
using System;
using System.IO;
using DIYsect.Dissection;
using DIYsect.Learning;
using DIYsect.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Rebuilds local checklist presentation in both headset scenes. Flat-screen scenes stay unchanged.
    /// Everything follows the table root. The previous presentation survives if target validation fails.
    /// </summary>
    public static class AddDissectionChecklist
    {
        private const string RootName = "DIYsect";
        private const string ObjectName = "DissectionChecklist";
        private static readonly string[] HeadsetScenes = { "Assets/Scenes/Professor_MR.unity", "Assets/Scenes/LectureHall_VR.unity" };
        [MenuItem("Tools/DIYsect/Add Dissection Checklist")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in HeadsetScenes)
                BuildScene(path);
            // Flat-screen presentation is already installed and remains unchanged by this upgrade.
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddDissectionChecklist.ApplyBatch
        public static void ApplyBatch()
        {
            Apply();
        }

        private static void BuildScene(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Rebuild(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException($"Could not save {path}.");
            Debug.Log($"{path}: immersive checklist added beside the table.");
        }

        // Does not save; preview-scene tests can exercise the exact menu operation safely.
        public static void Rebuild(Scene scene)
        {
            string path = scene.path;
            Transform root = FindRoot(scene);

            DissectionProgress dissection = root.GetComponentInChildren<DissectionProgress>(true);
            if (dissection == null)
                throw new InvalidOperationException($"{path} has no body with a DissectionProgress. Run Tools > DIYsect > Add Body Dissection.");
            OrganGrabAuthority grabs = dissection.GetComponent<OrganGrabAuthority>();
            OrganSocketKeeper sockets = dissection.GetComponent<OrganSocketKeeper>();
            if (grabs == null || sockets == null)
                throw new InvalidOperationException($"{path}: the body has no OrganGrabAuthority or OrganSocketKeeper. Run Tools > DIYsect > Add Body Networking and Add Organ Sockets.");

            Transform old = root.Find(ObjectName);
            var checklistObject = new GameObject(ObjectName + " pending");
            checklistObject.transform.SetParent(root, false);

            var tracker = checklistObject.AddComponent<ChecklistTracker>();
            var serialized = new SerializedObject(tracker);
            serialized.FindProperty("_dissection").objectReferenceValue = dissection;
            serialized.FindProperty("_grabs").objectReferenceValue = grabs;
            serialized.FindProperty("_sockets").objectReferenceValue = sockets;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            checklistObject.AddComponent<ChecklistHud>();

            try
            {
                BuildPanel(checklistObject.transform, root, tracker);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(checklistObject);
                throw;
            }
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            checklistObject.name = ObjectName;
        }

        private static void BuildPanel(Transform parent, Transform root, ChecklistTracker tracker)
        {
            Bounds table = FindTableTop(root);
            ChecklistPresentationBuilder.Build(parent, root, tracker, table);
        }

        private static Transform FindRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    return candidate.transform;
            throw new InvalidOperationException($"{scene.path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
        }

        private static Bounds FindTableTop(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Top" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("DissectionTable"))
                    return renderer.bounds;
            throw new InvalidOperationException($"{root.gameObject.scene.path} has no DissectionTable/Top under {RootName}.");
        }
    }
}
