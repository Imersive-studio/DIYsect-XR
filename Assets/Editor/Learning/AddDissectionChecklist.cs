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
    /// Adds the dissection checklist to the DIYsect scenes. Every scene gets a "DissectionChecklist" object under the
    /// DIYsect root with a ChecklistTracker wired to the body (and a ChecklistHud, the one-line version for flat
    /// screens). The two headset scenes (Professor_MR and LectureHall_VR) also get a world-space panel beside the
    /// table on the professor's right, turned toward the professor, mirroring the student view window on the left.
    /// Everything is a child of the DIYsect root, so table alignment carries it along. Safe to run more than once:
    /// the old checklist object is rebuilt.
    /// </summary>
    public static class AddDissectionChecklist
    {
        private const string RootName = "DIYsect";
        private const string ObjectName = "DissectionChecklist";
        private const string PanelMaterialPath = "Assets/Models/Environment/ChecklistPanel.mat";
        private static readonly string[] HeadsetScenes = { "Assets/Scenes/Professor_MR.unity", "Assets/Scenes/LectureHall_VR.unity" };
        private static readonly string[] FlatScreenScenes = { "Assets/Scenes/Classmate_Desktop.unity" };
        // From the table centre: to the professor's right (the student view window is on the left), a little nearer, at eye height.
        private static readonly Vector3 OffsetFromTable = new Vector3(0.95f, 0f, 0.55f);
        private const float Height = 1.4f;
        private static readonly Vector2 PanelSize = new Vector2(0.52f, 0.36f);

        [MenuItem("Tools/DIYsect/Add Dissection Checklist")]
        public static void Apply()
        {
            foreach (string path in HeadsetScenes)
                BuildScene(path, withPanel: true);
            foreach (string path in FlatScreenScenes)
                BuildScene(path, withPanel: false);
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddDissectionChecklist.ApplyBatch
        public static void ApplyBatch()
        {
            Apply();
        }

        private static void BuildScene(string path, bool withPanel)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Transform root = FindRoot(scene);

            DissectionProgress dissection = root.GetComponentInChildren<DissectionProgress>(true);
            if (dissection == null)
                throw new InvalidOperationException($"{path} has no body with a DissectionProgress. Run Tools > DIYsect > Add Body Dissection.");
            OrganGrabAuthority grabs = dissection.GetComponent<OrganGrabAuthority>();
            OrganSocketKeeper sockets = dissection.GetComponent<OrganSocketKeeper>();
            if (grabs == null || sockets == null)
                throw new InvalidOperationException($"{path}: the body has no OrganGrabAuthority or OrganSocketKeeper. Run Tools > DIYsect > Add Body Networking and Add Organ Sockets.");

            Transform old = root.Find(ObjectName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var checklistObject = new GameObject(ObjectName);
            checklistObject.transform.SetParent(root, false);

            var tracker = checklistObject.AddComponent<ChecklistTracker>();
            var serialized = new SerializedObject(tracker);
            serialized.FindProperty("_dissection").objectReferenceValue = dissection;
            serialized.FindProperty("_grabs").objectReferenceValue = grabs;
            serialized.FindProperty("_sockets").objectReferenceValue = sockets;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            checklistObject.AddComponent<ChecklistHud>();

            if (withPanel)
                BuildPanel(checklistObject.transform, root, tracker);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
            Debug.Log($"{path}: dissection checklist added{(withPanel ? " with a panel beside the table" : " (one line for flat screens)")}.");
        }

        private static void BuildPanel(Transform parent, Transform root, ChecklistTracker tracker)
        {
            Bounds table = FindTableTop(root);
            Vector3 position = new Vector3(table.center.x, Height, table.center.z) + OffsetFromTable;
            Vector3 professorHead = new Vector3(0f, Height, 0f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(parent, false);
            // Text and quads show their visible side towards -Z, so +Z points away from the professor.
            panel.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - professorHead, Vector3.up));

            GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Backing";
            backing.transform.SetParent(panel.transform, false);
            backing.transform.localScale = new Vector3(PanelSize.x, PanelSize.y, 0.01f);
            backing.GetComponent<Renderer>().sharedMaterial = LoadOrCreateMaterial();
            UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());

            // The text starts at the top left of the backing and sits just in front of it.
            var textObject = new GameObject("Text");
            textObject.transform.SetParent(panel.transform, false);
            textObject.transform.localPosition = new Vector3(-PanelSize.x / 2f + 0.03f, PanelSize.y / 2f - 0.025f, -0.008f);
            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = ChecklistLabels.Format(ChecklistState.TraceIncision);
            mesh.anchor = TextAnchor.UpperLeft;
            mesh.alignment = TextAlignment.Left;
            mesh.characterSize = 0.005f;
            mesh.fontSize = 64;
            mesh.color = Color.white;

            var presenter = panel.AddComponent<ChecklistPanel>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_tracker").objectReferenceValue = tracker;
            serialized.FindProperty("_text").objectReferenceValue = mesh;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LoadOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PanelMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("Universal Render Pipeline/Unlit shader not found.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(PanelMaterialPath) };
            material.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.11f));
            AssetDatabase.CreateAsset(material, PanelMaterialPath);
            return material;
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
