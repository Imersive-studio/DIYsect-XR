// Run Tools > DIYsect > Add Professor View Toggle. Build Shared Scene also runs it when the button is missing.
using System;
using System.IO;
using DIYsect.ClassRoster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the professor's view button in Professor_MR: a small card on the panel arc with one ray-select button that flips between
    /// the classroom (the hall the students are in) and mixed reality (the real room). It is a child of the DIYsect root, and Build
    /// Shared Scene switches it on for the professor only. Safe to run more than once.
    /// </summary>
    public static class AddProfessorViewToggle
    {
        public const string PanelName = "ProfessorViewToggle";

        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string FrameMaterialPath = "Assets/Models/Environment/SpeakPanelFrame.mat";
        private const string ButtonMaterialPath = "Assets/Models/Environment/SpeakPanelButton.mat";
        private static readonly Vector2 CardSize = new Vector2(0.4f, 0.17f);
        private static readonly Vector3 ButtonSize = new Vector3(0.36f, 0.12f, 0.02f);
        // Low on the arc, below the other panels, so it never overlaps one. Arrange Professor Panels sets its bearing and distance.
        private const float Height = 1.0f;

        [MenuItem("Tools/DIYsect/Add Professor View Toggle")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        /// <summary>Builds the button in a scene that is already open and returns it. Does not save.</summary>
        public static GameObject Build(Scene scene)
        {
            Transform root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    root = candidate.transform;
            if (root == null)
                throw new InvalidOperationException($"{scene.path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");

            Transform old = root.Find(PanelName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var panelObject = new GameObject(PanelName);
            panelObject.transform.SetParent(root, false);
            // A rough first place in front of the professor; Arrange Professor Panels puts it on the arc.
            panelObject.transform.SetPositionAndRotation(new Vector3(-1.2f, Height, 0.4f), Quaternion.LookRotation(new Vector3(-1.2f, 0f, 0.4f), Vector3.up));

            Material frameMaterial = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath)
                ?? throw new InvalidOperationException($"{FrameMaterialPath} is missing. Run Tools > DIYsect > Add Speak Panel first.");
            Material buttonMaterial = AssetDatabase.LoadAssetAtPath<Material>(ButtonMaterialPath)
                ?? throw new InvalidOperationException($"{ButtonMaterialPath} is missing. Run Tools > DIYsect > Add Speak Panel first.");

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(panelObject.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            frame.transform.localScale = new Vector3(CardSize.x, CardSize.y, 0.01f);
            UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().sharedMaterial = frameMaterial;

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Button";
            button.transform.SetParent(panelObject.transform, false);
            button.transform.localPosition = Vector3.zero;
            button.transform.localScale = ButtonSize;
            button.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
            var interactable = button.AddComponent<XRSimpleInteractable>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(button.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            labelObject.transform.localScale = new Vector3(ButtonSize.y / ButtonSize.x, 1f, 1f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = XRMultiplayer.ProfessorViewRule.ButtonText(XRMultiplayer.ProfessorViewRule.Default);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.04f;
            label.fontSize = 48;
            label.color = Color.white;

            var view = panelObject.AddComponent<ProfessorViewButton>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_button").objectReferenceValue = interactable;
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"{scene.path}: professor view button built.");
            return panelObject;
        }
    }
}
