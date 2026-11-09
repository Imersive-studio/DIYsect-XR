// Run Tools > DIYsect > Add Speak Panel after Add Class Roster and Add Raise Hand.
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.ClassRoster;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the professor's participant list in Professor_MR, under the class roster: a row per student with their name, a
    /// raised-hand marker, a microphone marker and an Allow / Mute button, like a video call. It is a child of the DIYsect root, so
    /// table alignment carries it along, and Build Shared Scene switches it on for the professor only. Safe to run more than once.
    /// </summary>
    public static class AddSpeakPanel
    {
        public const string PanelName = "SpeakPanel";
        public const int RowCount = 8;

        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string FrameMaterialPath = "Assets/Models/Environment/SpeakPanelFrame.mat";
        private const string ButtonMaterialPath = "Assets/Models/Environment/SpeakPanelButton.mat";
        private const string IconMaterialPath = "Assets/Models/Environment/SpeakPanelIcon.mat";
        private static readonly Vector2 PanelSize = new Vector2(0.78f, 0.52f);
        private const float RowHeight = 0.054f;
        private static readonly Vector3 OffsetFromTable = new Vector3(0.95f, 0f, -0.25f);
        private const float Height = 0.84f;

        [MenuItem("Tools/DIYsect/Add Speak Panel")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddSpeakPanel.ApplyBatch
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

        /// <summary>Builds the panel in a scene that is already open and returns it. Does not save. Safe to run more than once.</summary>
        public static GameObject Build(Scene scene)
        {
            Transform root = FindRoot(scene);
            HandBoard hands = root.GetComponentInChildren<HandBoard>(true)
                ?? throw new InvalidOperationException("The body has no HandBoard. Run Tools > DIYsect > Add Raise Hand.");
            SpeakBoard speak = root.GetComponentInChildren<SpeakBoard>(true)
                ?? throw new InvalidOperationException("The body has no SpeakBoard. Run Tools > DIYsect > Add Raise Hand.");

            Transform old = root.Find(PanelName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var panelObject = new GameObject(PanelName);
            panelObject.transform.SetParent(root, false);
            Bounds table = FindTableTop(root, scene.path);
            Vector3 position = new Vector3(table.center.x, Height, table.center.z) + OffsetFromTable;
            Vector3 professorHead = new Vector3(0f, 1.45f, 0f);
            // A text mesh reads from -Z, so +Z points away from the professor.
            panelObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - professorHead, Vector3.up));

            Material frameMaterial = LoadOrCreateMaterial(FrameMaterialPath, new Color(0.08f, 0.09f, 0.12f));
            Material buttonMaterial = LoadOrCreateMaterial(ButtonMaterialPath, new Color(0.14f, 0.3f, 0.2f));
            Material iconMaterial = LoadOrCreateMaterial(IconMaterialPath, Color.white);

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(panelObject.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.012f);
            frame.transform.localScale = new Vector3(PanelSize.x, PanelSize.y, 0.01f);
            UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());
            frame.GetComponent<Renderer>().sharedMaterial = frameMaterial;

            AddText(panelObject.transform, "Title", "Participants: raise hand, mic", new Vector3(-PanelSize.x / 2f + 0.03f, PanelSize.y / 2f - 0.015f, 0f), 0.0042f, TextAnchor.UpperLeft, Color.white);
            TextMesh summary = AddText(panelObject.transform, "Summary", string.Empty, new Vector3(0f, -PanelSize.y / 2f + 0.012f, 0f), 0.0038f, TextAnchor.LowerCenter, new Color(0.75f, 0.8f, 0.9f));

            var rows = new SpeakPanel.Row[RowCount];
            float top = PanelSize.y / 2f - 0.07f;
            for (int i = 0; i < RowCount; i++)
                rows[i] = BuildRow(panelObject.transform, i, top - i * RowHeight, iconMaterial, buttonMaterial);

            var panel = panelObject.AddComponent<SpeakPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_speak").objectReferenceValue = speak;
            serialized.FindProperty("_hands").objectReferenceValue = hands;
            serialized.FindProperty("_summary").objectReferenceValue = summary;
            SerializedProperty rowList = serialized.FindProperty("_rows");
            rowList.arraySize = RowCount;
            for (int i = 0; i < RowCount; i++)
            {
                SerializedProperty row = rowList.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Root").objectReferenceValue = rows[i].Root;
                row.FindPropertyRelative("Name").objectReferenceValue = rows[i].Name;
                row.FindPropertyRelative("Status").objectReferenceValue = rows[i].Status;
                row.FindPropertyRelative("Hand").objectReferenceValue = rows[i].Hand;
                row.FindPropertyRelative("HandIcon").objectReferenceValue = rows[i].HandIcon;
                row.FindPropertyRelative("MicIcon").objectReferenceValue = rows[i].MicIcon;
                row.FindPropertyRelative("MicSlash").objectReferenceValue = rows[i].MicSlash;
                row.FindPropertyRelative("ButtonLabel").objectReferenceValue = rows[i].ButtonLabel;
                row.FindPropertyRelative("Button").objectReferenceValue = rows[i].Button;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Every row starts hidden; the panel shows a row for each student.
            foreach (SpeakPanel.Row row in rows)
                row.Root.SetActive(false);
            Debug.Log($"{scene.path}: speak panel at {position} with {RowCount} rows.");
            return panelObject;
        }

        private static SpeakPanel.Row BuildRow(Transform parent, int index, float y, Material iconMaterial, Material buttonMaterial)
        {
            var rowObject = new GameObject($"Row{index}");
            rowObject.transform.SetParent(parent, false);
            rowObject.transform.localPosition = new Vector3(0f, y, 0f);
            float left = -PanelSize.x / 2f + 0.03f;

            // Raised hand: a yellow disc with the order number beside it.
            Renderer handIcon = AddIcon(rowObject.transform, "HandIcon", PrimitiveType.Sphere, new Vector3(left + 0.012f, 0f, 0f), new Vector3(0.022f, 0.022f, 0.01f), iconMaterial);
            TextMesh hand = AddText(rowObject.transform, "HandNumber", string.Empty, new Vector3(left + 0.03f, 0f, 0f), 0.0036f, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.2f));

            // Microphone: a capsule, green when speaking, grey when not, with a red slash across it while muted.
            Renderer mic = AddIcon(rowObject.transform, "MicIcon", PrimitiveType.Capsule, new Vector3(left + 0.085f, 0f, 0f), new Vector3(0.014f, 0.016f, 0.01f), iconMaterial);
            GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slash.name = "MicSlash";
            slash.transform.SetParent(rowObject.transform, false);
            slash.transform.localPosition = new Vector3(left + 0.085f, 0f, -0.007f);
            slash.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            slash.transform.localScale = new Vector3(0.004f, 0.036f, 0.002f);
            UnityEngine.Object.DestroyImmediate(slash.GetComponent<Collider>());
            slash.GetComponent<Renderer>().sharedMaterial = iconMaterial;
            var slashBlock = new MaterialPropertyBlock();
            slashBlock.SetColor("_BaseColor", new Color(0.95f, 0.25f, 0.25f));
            slashBlock.SetColor("_Color", new Color(0.95f, 0.25f, 0.25f));
            slash.GetComponent<Renderer>().SetPropertyBlock(slashBlock);

            TextMesh name = AddText(rowObject.transform, "Name", string.Empty, new Vector3(left + 0.13f, 0.012f, 0f), 0.0042f, TextAnchor.MiddleLeft, Color.white);
            TextMesh status = AddText(rowObject.transform, "Status", string.Empty, new Vector3(left + 0.13f, -0.012f, 0f), 0.0030f, TextAnchor.MiddleLeft, new Color(0.75f, 0.8f, 0.9f));

            // The Allow / Mute button on the right, selected by the ray like the roster's buttons.
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Button";
            button.transform.SetParent(rowObject.transform, false);
            button.transform.localPosition = new Vector3(PanelSize.x / 2f - 0.085f, 0f, 0f);
            button.transform.localScale = new Vector3(0.13f, 0.042f, 0.02f);
            button.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
            var interactable = button.AddComponent<XRSimpleInteractable>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(button.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            labelObject.transform.localScale = new Vector3(0.042f / 0.13f, 1f, 1f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = SpeakRowText.AllowLabel;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.05f;
            label.fontSize = 48;
            label.color = Color.white;

            return new SpeakPanel.Row
            {
                Root = rowObject,
                Name = name,
                Status = status,
                Hand = hand,
                HandIcon = handIcon,
                MicIcon = mic,
                MicSlash = slash,
                ButtonLabel = label,
                Button = interactable,
            };
        }

        private static Renderer AddIcon(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            GameObject icon = GameObject.CreatePrimitive(shape);
            icon.name = name;
            icon.transform.SetParent(parent, false);
            icon.transform.localPosition = position;
            icon.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(icon.GetComponent<Collider>());
            var renderer = icon.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static TextMesh AddText(Transform parent, string name, string text, Vector3 position, float size, TextAnchor anchor, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position;
            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = anchor;
            mesh.alignment = anchor == TextAnchor.LowerCenter ? TextAlignment.Center : TextAlignment.Left;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.color = color;
            return mesh;
        }

        private static Material LoadOrCreateMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? throw new InvalidOperationException("The URP Lit shader was not found.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
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

        private static Bounds FindTableTop(Transform root, string scenePath)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Top" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("DissectionTable"))
                    return renderer.bounds;
            throw new InvalidOperationException($"{scenePath} has no DissectionTable/Top under {RootName}.");
        }
    }
}
