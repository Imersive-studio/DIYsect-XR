// Run Tools > DIYsect > Add Student View Window after Create Scenes and Add Question Board.
using System;
using System.IO;
using DIYsect.Classroom;
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
    /// Builds the professor's student-view window in Professor_MR, beside the table on the professor's left and
    /// turned toward the professor: a screen showing its own render texture, a caption with the question, and
    /// previous and next buttons (ray-selectable, wired to the window with persistent events). The window's camera
    /// leaves out the CloseUpScreen layer, which the window also uses, so it never films itself.
    /// It is a child of the DIYsect root, so table alignment carries it along. Safe to run more than once.
    /// </summary>
    public static class AddStudentViewWindow
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string WindowName = "StudentViewWindow";
        private const string ScreenLayerName = "CloseUpScreen";
        private const string TexturePath = "Assets/Models/Environment/StudentView.renderTexture";
        private const string ScreenMaterialPath = "Assets/Models/Environment/StudentView.mat";
        private const string FrameMaterialPath = "Assets/Models/Environment/StudentViewFrame.mat";
        private static readonly Vector2 ScreenSize = new Vector2(0.64f, 0.36f);
        // From the table centre: to the professor's left, a little nearer, at eye height.
        private static readonly Vector3 OffsetFromTable = new Vector3(-0.95f, 0f, -0.25f);
        private const float Height = 1.45f;

        [MenuItem("Tools/DIYsect/Add Student View Window")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = FindRoot(scene);
            QuestionBoard board = root.GetComponentInChildren<QuestionBoard>(true);
            if (board == null)
                throw new InvalidOperationException($"{ScenePath} has no body with a QuestionBoard. Run Tools > DIYsect > Add Question Board.");
            int layer = LayerMask.NameToLayer(ScreenLayerName);
            if (layer < 0)
                throw new InvalidOperationException($"Layer {ScreenLayerName} is missing. Run Tools > DIYsect > Create Scenes.");

            Transform old = root.Find(WindowName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var windowObject = new GameObject(WindowName);
            windowObject.transform.SetParent(root, false);
            Bounds table = FindTableTop(root);
            Vector3 position = new Vector3(table.center.x, Height, table.center.z) + OffsetFromTable;
            Vector3 professorHead = new Vector3(0f, Height, 0f);
            // A quad's visible side faces -Z, so +Z points away from the professor.
            windowObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - professorHead, Vector3.up));

            var texture = LoadOrCreateTexture();
            var panel = new GameObject("Panel");
            panel.transform.SetParent(windowObject.transform, false);
            AddPart(panel.transform, PrimitiveType.Quad, "Screen", Vector3.zero, new Vector3(ScreenSize.x, ScreenSize.y, 1f),
                LoadOrCreateMaterial(ScreenMaterialPath, "Universal Render Pipeline/Unlit", Color.white, texture), keepCollider: false);
            AddPart(panel.transform, PrimitiveType.Cube, "Frame", new Vector3(0f, 0f, 0.008f), new Vector3(ScreenSize.x + 0.04f, ScreenSize.y + 0.04f, 0.01f),
                LoadOrCreateMaterial(FrameMaterialPath, "Universal Render Pipeline/Lit", new Color(0.12f, 0.12f, 0.14f), null), keepCollider: false);
            TextMesh caption = AddText(panel.transform, "Caption", new Vector3(0f, -ScreenSize.y / 2f - 0.035f, -0.002f), "", 0.006f);

            var window = windowObject.AddComponent<StudentViewWindow>();
            AddButton(panel.transform, "Previous", "<", new Vector3(-ScreenSize.x / 2f - 0.06f, 0f, 0f), window, window.Previous);
            AddButton(panel.transform, "Next", ">", new Vector3(ScreenSize.x / 2f + 0.06f, 0f, 0f), window, window.Next);
            AddText(panel.transform, "Title", new Vector3(0f, ScreenSize.y / 2f + 0.035f, -0.002f), "Student view", 0.007f);

            var cameraObject = new GameObject("ViewCamera");
            cameraObject.transform.SetParent(windowObject.transform, false);
            var viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.enabled = false;
            viewCamera.targetTexture = texture;
            viewCamera.fieldOfView = 60f;
            viewCamera.nearClipPlane = 0.03f;
            viewCamera.cullingMask = ~(1 << layer);
            viewCamera.clearFlags = CameraClearFlags.Skybox;

            foreach (Transform part in panel.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = layer;

            var serialized = new SerializedObject(window);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_viewCamera").objectReferenceValue = viewCamera;
            serialized.FindProperty("_panel").objectReferenceValue = panel;
            serialized.FindProperty("_caption").objectReferenceValue = caption;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: student view window at {position}.");
        }

        private static void AddButton(Transform parent, string name, string label, Vector3 position, StudentViewWindow window, UnityAction action)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(parent, false);
            button.transform.localPosition = position;
            button.transform.localScale = new Vector3(0.06f, 0.06f, 0.02f);
            button.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath);
            var interactable = button.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, action);
            AddText(button.transform, "Label", new Vector3(0f, 0f, -0.6f), label, 0.08f);
        }

        private static TextMesh AddText(Transform parent, string name, Vector3 position, string text, float size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.color = Color.white;
            return mesh;
        }

        private static void AddPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static RenderTexture LoadOrCreateTexture()
        {
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(TexturePath);
            if (texture != null)
                return texture;
            texture = new RenderTexture(768, 432, 16) { name = "StudentView" };
            AssetDatabase.CreateAsset(texture, TexturePath);
            return texture;
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName, Color color, Texture texture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"{shaderName} shader not found.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            if (texture != null)
                material.SetTexture("_BaseMap", texture);
            AssetDatabase.CreateAsset(material, path);
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
            throw new InvalidOperationException($"{ScenePath} has no DissectionTable/Top under {RootName}.");
        }
    }
}
