// Run Tools > DIYsect > Add Class Roster after Add Question Board.
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.ClassRoster;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the professor's class roster in Professor_MR: a dark panel on the professor's right, turned toward the
    /// professor, listing every student in the room with their device, a marker when their hand is up, and how many questions they have pinned.
    /// It is a child of the DIYsect root, so table alignment carries it along, and it sits opposite the student view
    /// window, which is on the left. Safe to run more than once.
    /// </summary>
    public static class AddClassRoster
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string RosterName = "ClassRoster";
        private const string FrameMaterialPath = "Assets/Models/Environment/ClassRosterFrame.mat";
        private const string ButtonMaterialPath = "Assets/Models/Environment/StudentViewFrame.mat";
        private static readonly Vector3 ButtonSize = new Vector3(0.22f, 0.06f, 0.02f);
        private static readonly Vector2 PanelSize = new Vector2(0.78f, 0.4f);
        // From the table centre: to the professor's right, a little nearer, at eye height.
        private static readonly Vector3 OffsetFromTable = new Vector3(0.95f, 0f, -0.25f);
        private const float Height = 1.45f;

        [MenuItem("Tools/DIYsect/Add Class Roster")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = FindRoot(scene);
            QuestionBoard board = root.GetComponentInChildren<QuestionBoard>(true);
            if (board == null)
                throw new InvalidOperationException($"{ScenePath} has no body with a QuestionBoard. Run Tools > DIYsect > Add Question Pins.");

            Transform old = root.Find(RosterName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var rosterObject = new GameObject(RosterName);
            rosterObject.transform.SetParent(root, false);
            Bounds table = FindTableTop(root);
            Vector3 position = new Vector3(table.center.x, Height, table.center.z) + OffsetFromTable;
            Vector3 professorHead = new Vector3(0f, Height, 0f);
            // A text mesh reads from -Z, so +Z points away from the professor.
            rosterObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - professorHead, Vector3.up));

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(rosterObject.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            frame.transform.localScale = new Vector3(PanelSize.x, PanelSize.y, 0.01f);
            UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().sharedMaterial = LoadOrCreateFrameMaterial();

            var textObject = new GameObject("Text");
            textObject.transform.SetParent(rosterObject.transform, false);
            textObject.transform.localPosition = new Vector3(-PanelSize.x / 2f + 0.03f, PanelSize.y / 2f - 0.03f, 0f);
            var text = textObject.AddComponent<TextMesh>();
            text.text = "Class: no students yet";
            text.anchor = TextAnchor.UpperLeft;
            text.alignment = TextAlignment.Left;
            text.characterSize = 0.0045f;
            text.fontSize = 64;
            text.color = Color.white;

            HandBoard hands = board.GetComponent<HandBoard>()
                ?? throw new InvalidOperationException("The body has no HandBoard. Run Tools > DIYsect > Add Raise Hand.");
            var panel = rosterObject.AddComponent<ClassRosterPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_hands").objectReferenceValue = hands;
            serialized.FindProperty("_text").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Two ray-select buttons under the panel, like the quiz panel's: take down the hand that went up first, or all of them.
            Material buttons = LoadOrCreateMaterial(ButtonMaterialPath, "Universal Render Pipeline/Lit", new Color(0.12f, 0.12f, 0.14f));
            float row = -PanelSize.y / 2f - 0.05f;
            AddButton(rosterObject.transform, "LowerNextHand", "Lower next hand", new Vector3(-0.12f, row, 0f), buttons, panel.LowerNext);
            AddButton(rosterObject.transform, "LowerAllHands", "Lower all hands", new Vector3(0.12f, row, 0f), buttons, panel.LowerAll);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: class roster at {position}.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddClassRoster.ApplyBatch
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

        private static Material LoadOrCreateFrameMaterial()
        {
            return LoadOrCreateMaterial(FrameMaterialPath, "Universal Render Pipeline/Lit", new Color(0.08f, 0.09f, 0.12f));
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"{shaderName} shader not found.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // A cube with an XRSimpleInteractable that calls the action when the ray selects it, and a label on its front.
        // The label is scaled back to square, so a wide button does not stretch its text.
        private static void AddButton(Transform parent, string name, string label, Vector3 position, Material material, UnityAction action)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(parent, false);
            button.transform.localPosition = position;
            button.transform.localScale = ButtonSize;
            button.GetComponent<Renderer>().sharedMaterial = material;
            var interactable = button.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, action);

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(button.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            textObject.transform.localScale = new Vector3(ButtonSize.y / ButtonSize.x, 1f, 1f);
            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = label;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.05f;
            mesh.fontSize = 64;
            mesh.color = Color.white;
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
            throw new InvalidOperationException($"{ScenePath} has no DissectionTable/Top under {RootName}.");
        }
    }
}
