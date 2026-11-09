// Run Tools > DIYsect > Add Student View Window after Create Scenes and Add Question Board.
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.Presentation;
using TMPro;
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
    /// turned toward the professor: a card (ProfessorPanelKit) with a screen showing its own render texture, a caption with the question, and
    /// previous and next buttons, an Answered button (the professor marks the shown question answered) and a Live button (the view follows the student's head) (ray-selectable, wired to the window with persistent events). The window's camera
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
        private static readonly Vector2 ScreenSize = new Vector2(0.64f, 0.36f);
        private static readonly Vector2 CardSize = new Vector2(0.86f, 0.70f);
        // The screen picture inside the card's content area, from its top left corner.
        private const float ScreenLeft = 0.11f;
        private const float ScreenTop = 0.03f;
        // From the table centre: to the professor's left, a little nearer, at eye height.
        private static readonly Vector3 OffsetFromTable = new Vector3(-0.95f, 0f, -0.25f);
        private const float Height = 1.45f;
        private const int TextureWidth = 512;
        private const int TextureHeight = 288;

        [MenuItem("Tools/DIYsect/Add Student View Window")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = FindRoot(scene);
            QuestionBoard board = root.GetComponentInChildren<QuestionBoard>(true);
            if (board == null)
                throw new InvalidOperationException($"{ScenePath} has no body with a QuestionBoard. Run Tools > DIYsect > Add Question Board.");
            HeadPoseBoard headBoard = board.GetComponent<HeadPoseBoard>();
            if (headBoard == null)
                headBoard = root.GetComponentInChildren<HeadPoseBoard>(true);
            if (headBoard == null)
                throw new InvalidOperationException($"{ScenePath} has no body with a HeadPoseBoard. Run Tools > DIYsect > Add Head Pose.");
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
            // The whole card is the Panel the window hides when there is nothing to show. The stand is its child, so it
            // hides with it: a pole and foot with no screen on them would look broken.
            Transform panel = ProfessorPanelKit.BuildCard(windowObject.transform, "Panel", CardSize, "Student view", "VIEW");
            Transform content = panel.Find("Content");
            Vector3 screenCentre = new Vector3(ScreenLeft + ScreenSize.x / 2f, -ScreenTop - ScreenSize.y / 2f, 0f);
            // A 4 mm mint rim sits just behind the screen picture.
            AddPart(content, PrimitiveType.Quad, "ScreenRim", screenCentre + new Vector3(0f, 0f, 0.001f), new Vector3(ScreenSize.x + 0.008f, ScreenSize.y + 0.008f, 1f),
                ProfessorPanelKit.Material("Panel_Mint", GuidePalette.Mint, false, false), keepCollider: false);
            AddPart(content, PrimitiveType.Quad, "Screen", screenCentre, new Vector3(ScreenSize.x, ScreenSize.y, 1f),
                LoadOrCreateMaterial(ScreenMaterialPath, "Universal Render Pipeline/Unlit", Color.white, texture), keepCollider: false);
            TextMeshPro caption = ProfessorPanelKit.AddText(content, "Caption", "", 0.017f, GuidePalette.TextMuted, TextAlignmentOptions.Top,
                new Vector2(ScreenLeft, -ScreenTop - ScreenSize.y - 0.025f), new Vector2(ScreenSize.x, 0.08f));

            var window = windowObject.AddComponent<StudentViewWindow>();
            float rowY = -ScreenTop - ScreenSize.y - 0.14f;
            AddButton(content, "Previous", "<", new Vector3(ScreenLeft - 0.055f, -ScreenTop - ScreenSize.y / 2f, 0f), window, window.Previous);
            AddButton(content, "Next", ">", new Vector3(ScreenLeft + ScreenSize.x + 0.055f, -ScreenTop - ScreenSize.y / 2f, 0f), window, window.Next);
            // Live is left of Answered. It switches between the questions and the live view through the student's eyes.
            AddButton(content, "Answered", "Answered", new Vector3(0.52f, rowY, 0f), window, window.MarkSelectedAnswered, width: 0.2f);
            AddButton(content, "Live", "Live", new Vector3(0.34f, rowY, 0f), window, window.ToggleLive, width: 0.14f);
            // Anchor the card to the floor. The card is placed above, so its bottom is known.
            ProfessorPanelKit.AddStand(panel, position.y - CardSize.y / 2f, root.position.y);

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
            serialized.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("_headBoard").objectReferenceValue = headBoard;
            serialized.FindProperty("_caption").objectReferenceValue = caption;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: student view window at {position}.");
        }

        private static void AddButton(Transform parent, string name, string label, Vector3 position, StudentViewWindow window, UnityAction action, float width = 0.06f)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(parent, false);
            button.transform.localPosition = position + new Vector3(0f, 0f, -0.002f);
            button.transform.localScale = new Vector3(width, 0.06f, 0.012f);
            var interactable = button.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, action);
            ProfessorPanelKit.StyleButton(button, label);
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
            {
                // An older, bigger texture is shrunk once. Every live render fills it, so smaller is cheaper on Quest.
                if (texture.width != TextureWidth || texture.height != TextureHeight)
                {
                    texture.Release();
                    texture.width = TextureWidth;
                    texture.height = TextureHeight;
                    EditorUtility.SetDirty(texture);
                }
                return texture;
            }
            texture = new RenderTexture(TextureWidth, TextureHeight, 16) { name = "StudentView" };
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
