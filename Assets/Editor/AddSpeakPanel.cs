// Run Tools > DIYsect > Add Speak Panel after Add Class Roster and Add Raise Hand.
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.ClassRoster;
using DIYsect.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the professor's participant list in Professor_MR, under the class roster: a row per student with their name, a
    /// raised-hand marker, a microphone marker and an Allow / Mute button, like a video call. It is upright and stacked directly under the
    /// roster card, and one stand runs from its bottom to the floor. It is a child of the DIYsect root, so
    /// table alignment carries it along, and Build Shared Scene switches it on for the professor only. Safe to run more than once.
    /// </summary>
    public static class AddSpeakPanel
    {
        public const string PanelName = "SpeakPanel";
        public const int RowCount = 8;

        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private static readonly Vector2 PanelSize = new Vector2(0.62f, 0.42f);
        private const float ContentWidth = 0.56f;
        private const float RowHeight = 0.036f;
        private const float RowsTop = -0.01f;

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

            // Directly under the roster card, same bearing, same turn, both upright.
            float centreY = AddClassRoster.CentreHeight - AddClassRoster.PanelSize.y / 2f - AddClassRoster.StackGap - PanelSize.y / 2f;
            Vector3 position = AddClassRoster.ColumnPosition(root, centreY);
            Transform card = ProfessorPanelKit.BuildCard(root, PanelName, PanelSize, "SPEAKING");
            card.SetPositionAndRotation(position, AddClassRoster.ColumnRotation(position));
            Transform content = card.Find("Content");
            GameObject panelObject = card.gameObject;

            Material buttonMaterial = ProfessorPanelKit.Material("SpeakButton", GuidePalette.Slate);
            Material iconMaterial = ProfessorPanelKit.Material("SpeakIcon", Color.white);
            Material slashMaterial = ProfessorPanelKit.Material("SpeakSlash", GuidePalette.Coral);

            float summaryY = RowsTop - RowCount * RowHeight - 0.005f;
            TextMeshPro summary = ProfessorPanelKit.AddText(content, "Summary", string.Empty, 0.015f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0f, summaryY), new Vector2(ContentWidth, 0.022f));

            var rows = new SpeakPanel.Row[RowCount];
            for (int i = 0; i < RowCount; i++)
                rows[i] = BuildRow(content, i, RowsTop - i * RowHeight - RowHeight / 2f, iconMaterial, slashMaterial, buttonMaterial);

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

            // The one stand for the whole console: from the bottom of this card to the floor (the DIYsect root's height).
            ProfessorPanelKit.AddStand(card, position.y - PanelSize.y / 2f, root.position.y);

            // Every row starts hidden; the panel shows a row for each student.
            foreach (SpeakPanel.Row row in rows)
                row.Root.SetActive(false);
            Debug.Log($"{scene.path}: speak panel at {position} with {RowCount} rows.");
            return panelObject;
        }

        // y is the row's vertical centre in the content area. Text boxes hang from their top left corner.
        private static SpeakPanel.Row BuildRow(Transform parent, int index, float y, Material iconMaterial, Material slashMaterial, Material buttonMaterial)
        {
            var rowObject = new GameObject($"Row{index}");
            rowObject.transform.SetParent(parent, false);
            rowObject.transform.localPosition = new Vector3(0f, y, 0f);

            // Raised hand: a coral disc with the order number beside it.
            Renderer handIcon = AddIcon(rowObject.transform, "HandIcon", PrimitiveType.Sphere, new Vector3(0.012f, 0f, 0f), new Vector3(0.016f, 0.016f, 0.006f), iconMaterial);
            TextMeshPro hand = ProfessorPanelKit.AddText(rowObject.transform, "HandNumber", string.Empty, 0.014f, GuidePalette.Coral,
                TextAlignmentOptions.TopLeft, new Vector2(0.026f, 0.008f), new Vector2(0.04f, 0.018f), FontStyles.Bold);

            // Microphone: a capsule, mint when speaking, grey when not, with a coral slash across it while muted.
            Renderer mic = AddIcon(rowObject.transform, "MicIcon", PrimitiveType.Capsule, new Vector3(0.085f, 0f, 0f), new Vector3(0.012f, 0.014f, 0.006f), iconMaterial);
            GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slash.name = "MicSlash";
            slash.transform.SetParent(rowObject.transform, false);
            slash.transform.localPosition = new Vector3(0.085f, 0f, -0.005f);
            slash.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            slash.transform.localScale = new Vector3(0.003f, 0.028f, 0.002f);
            UnityEngine.Object.DestroyImmediate(slash.GetComponent<Collider>());
            slash.GetComponent<Renderer>().sharedMaterial = slashMaterial;

            TextMeshPro name = ProfessorPanelKit.AddText(rowObject.transform, "Name", string.Empty, 0.016f, GuidePalette.TextPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(0.115f, 0.0165f), new Vector2(0.31f, 0.019f));
            TextMeshPro status = ProfessorPanelKit.AddText(rowObject.transform, "Status", string.Empty, 0.012f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0.115f, -0.0015f), new Vector2(0.31f, 0.015f));

            // The Allow / Mute button on the right, selected by the ray. The old label is kept for the kit to hide.
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "Button";
            button.transform.SetParent(rowObject.transform, false);
            button.transform.localPosition = new Vector3(ContentWidth - 0.065f, 0f, -0.01f);
            button.transform.localScale = new Vector3(0.12f, 0.03f, 0.02f);
            button.GetComponent<Renderer>().sharedMaterial = buttonMaterial;
            var interactable = button.AddComponent<XRSimpleInteractable>();

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(button.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            labelObject.transform.localScale = new Vector3(0.03f / 0.12f, 1f, 1f);
            var oldLabel = labelObject.AddComponent<TextMesh>();
            oldLabel.text = SpeakRowText.AllowLabel;
            oldLabel.anchor = TextAnchor.MiddleCenter;
            oldLabel.alignment = TextAlignment.Center;
            oldLabel.characterSize = 0.05f;
            oldLabel.fontSize = 48;

            ProfessorPanelKit.StyleButton(button, SpeakRowText.AllowLabel);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true)
                ?? throw new InvalidOperationException("ProfessorPanelKit.StyleButton did not add a TMP label to the row button.");

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

        private static Transform FindRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    return candidate.transform;
            throw new InvalidOperationException($"{scene.path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
        }
    }
}
