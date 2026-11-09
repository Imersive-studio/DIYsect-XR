// Run Tools > DIYsect > Add Quit Menu, or: Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.AddQuitMenu.ApplyBatch
using System;
using System.IO;
using System.Linq;
using DIYsect.Menu;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the in-app quit menu to LectureHall_VR: a world space canvas with Resume and Quit buttons and a
    /// "Leave the dissection?" confirm group. The toggle action (left menu button, right secondary button, Escape) is
    /// made once as Assets/Menu/QuitMenuActions.inputactions. The canvas is found by name, so running this twice
    /// rebuilds the same object. Only LectureHall_VR changes.
    /// </summary>
    public static class AddQuitMenu
    {
        private const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";
        private const string MenuName = "QuitMenu";
        private const string ActionsFolder = "Assets/Menu";
        private const string ActionsPath = "Assets/Menu/QuitMenuActions.inputactions";
        private const float CanvasScale = 0.0015f;
        private static readonly Vector2 PanelSize = new Vector2(600f, 400f);

        [MenuItem("Tools/DIYsect/Add Quit Menu")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
                throw new InvalidOperationException($"{ScenePath} has no XROrigin.");

            InputActionReference toggle = EnsureToggleAction();

            // Reuse the canvas if it exists, and rebuild what is inside it.
            Transform existing = origin.transform.Find(MenuName);
            GameObject root = existing != null ? existing.gameObject : new GameObject(MenuName, typeof(RectTransform));
            root.transform.SetParent(origin.transform, false);
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * CanvasScale;
            ((RectTransform)root.transform).sizeDelta = PanelSize;

            var canvas = GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = origin.Camera;
            canvas.sortingOrder = 0;
            GetOrAdd<CanvasScaler>(root).dynamicPixelsPerUnit = 2f;
            GetOrAdd<TrackedDeviceGraphicRaycaster>(root);
            var menu = GetOrAdd<QuitMenu>(root);

            RectTransform panel = MakeImage("Panel", root.transform, PanelSize, new Color(0.05f, 0.05f, 0.08f, 0.85f));
            MakeText("Title", panel, "Menu", 48, new Vector2(0f, 150f), new Vector2(500f, 70f));
            Button resume = MakeButton("Resume", panel, "Resume", new Vector2(0f, 60f), new Vector2(300f, 70f));
            Button quit = MakeButton("Quit", panel, "Quit", new Vector2(0f, -30f), new Vector2(300f, 70f));

            RectTransform confirm = MakeGroup("ConfirmGroup", panel);
            MakeText("ConfirmText", confirm, "Leave the dissection?", 32, new Vector2(0f, -100f), new Vector2(500f, 45f));
            Button yes = MakeButton("Yes", confirm, "Yes, quit", new Vector2(-130f, -160f), new Vector2(220f, 60f));
            Button stay = MakeButton("Stay", confirm, "Stay", new Vector2(130f, -160f), new Vector2(220f, 60f));

            Wire(resume, menu.Cancel);
            Wire(quit, menu.RequestQuit);
            Wire(yes, menu.RequestQuit);
            Wire(stay, menu.Cancel);

            panel.gameObject.SetActive(false);
            confirm.gameObject.SetActive(false);

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            serialized.FindProperty("_confirmGroup").objectReferenceValue = confirm.gameObject;
            serialized.FindProperty("_toggleAction").objectReferenceValue = toggle;
            serialized.ApplyModifiedProperties();

            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
                Debug.LogWarning($"{ScenePath} has no EventSystem. The quit menu buttons will not respond until one with an XRUIInputModule is added. None was added.");
            else if (eventSystem.GetComponent<XRUIInputModule>() == null)
                Debug.LogWarning("The scene's EventSystem has no XRUIInputModule. The quit menu buttons will not respond to controllers. None was added.");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: quit menu added.");
        }

        /// <summary>Same as <see cref="Apply"/> for the command line. Exits 0 on success and 1 on failure.</summary>
        public static void ApplyBatch()
        {
            try
            {
                Apply();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogError($"Add Quit Menu failed: {error}");
                EditorApplication.Exit(1);
            }
        }

        // Makes the actions file once. It is written as JSON, the .inputactions format, and Unity imports it.
        private static InputActionReference EnsureToggleAction()
        {
            if (!File.Exists(ActionsPath))
            {
                if (!AssetDatabase.IsValidFolder(ActionsFolder))
                    AssetDatabase.CreateFolder("Assets", "Menu");
                var asset = InputActionAsset.CreateInstance<InputActionAsset>();
                asset.name = "QuitMenuActions";
                InputActionMap map = asset.AddActionMap("Menu");
                InputAction toggle = map.AddAction("Toggle", InputActionType.Button);
                toggle.AddBinding("<XRController>{LeftHand}/menuButton");
                toggle.AddBinding("<XRController>{RightHand}/secondaryButton");
                toggle.AddBinding("<Keyboard>/escape");
                File.WriteAllText(ActionsPath, asset.ToJson());
                UnityEngine.Object.DestroyImmediate(asset);
                AssetDatabase.ImportAsset(ActionsPath, ImportAssetOptions.ForceSynchronousImport);
            }

            InputActionReference reference = AssetDatabase.LoadAllAssetsAtPath(ActionsPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.action != null && r.action.name == "Toggle");
            if (reference == null)
                throw new InvalidOperationException($"{ActionsPath} has no Toggle action reference.");
            return reference;
        }

        private static void Wire(Button button, UnityAction call)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, call);
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var found = target.GetComponent<T>();
            return found != null ? found : target.AddComponent<T>();
        }

        private static RectTransform MakeGroup(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = PanelSize;
            return rect;
        }

        private static RectTransform MakeImage(string name, Transform parent, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        private static TextMeshProUGUI MakeText(string name, Transform parent, string text, float size, Vector2 position, Vector2 area)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = area;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button MakeButton(string name, Transform parent, string text, Vector2 position, Vector2 size)
        {
            RectTransform rect = MakeImage(name, parent, size, new Color(0.2f, 0.35f, 0.6f, 1f));
            rect.anchoredPosition = position;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            TextMeshProUGUI label = MakeText("Label", rect, text, 32, Vector2.zero, size);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
