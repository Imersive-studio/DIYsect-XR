// Run Tools > DIYsect > Add Class Roster after Add Question Board.
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.ClassRoster;
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
    /// Builds the professor's class roster in Professor_MR: a designed card on the professor's right, upright and turned toward the
    /// professor, listing every student in the room with their device, a marker when their hand is up, and how many questions they have pinned.
    /// The speaking panel hangs directly under it and shares one stand, so the two read as one console. It is a child of the
    /// DIYsect root, so table alignment carries it along. Safe to run more than once.
    /// </summary>
    public static class AddClassRoster
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string RosterName = "ClassRoster";
        private const string ButtonMaterialPath = "Assets/Models/Environment/StudentViewFrame.mat";

        /// <summary>The card size in metres. The speaking panel is as wide.</summary>
        public static readonly Vector2 PanelSize = new Vector2(0.62f, 0.5f);
        /// <summary>World height of the roster's centre. The speaking panel is stacked under it.</summary>
        public const float CentreHeight = 1.5f;
        /// <summary>Space between the roster card and the speaking card.</summary>
        public const float StackGap = 0.04f;
        /// <summary>Header room the content area gives up at the top of a card. A layout estimate, not read from the kit.</summary>
        public const float HeaderAllowance = 0.09f;

        // From the table centre: to the professor's right, a little nearer.
        private static readonly Vector3 OffsetFromTable = new Vector3(0.95f, 0f, -0.25f);
        private static readonly Vector3 ButtonSize = new Vector3(0.26f, 0.04f, 0.02f);
        private const float ContentWidth = 0.56f;
        private const float RowPitch = 0.031f;
        private const float RowsTop = -0.085f;

        [MenuItem("Tools/DIYsect/Add Class Roster")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = FindRoot(scene);
            QuestionBoard board = root.GetComponentInChildren<QuestionBoard>(true);
            if (board == null)
                throw new InvalidOperationException($"{ScenePath} has no body with a QuestionBoard. Run Tools > DIYsect > Add Question Pins.");
            HandBoard hands = board.GetComponent<HandBoard>()
                ?? throw new InvalidOperationException("The body has no HandBoard. Run Tools > DIYsect > Add Raise Hand.");

            // The help request link is made by Add Assist Request, so carry it over a rebuild.
            AssistBoard assist = null;
            Transform old = root.Find(RosterName);
            if (old != null)
            {
                var oldPanel = old.GetComponent<ClassRosterPanel>();
                if (oldPanel != null)
                    assist = new SerializedObject(oldPanel).FindProperty("_assist").objectReferenceValue as AssistBoard;
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }

            Vector3 position = ColumnPosition(root, CentreHeight);
            Transform card = ProfessorPanelKit.BuildCard(root, RosterName, PanelSize, "CLASS");
            card.SetPositionAndRotation(position, ColumnRotation(position));
            Transform content = card.Find("Content");

            // Help request and join code, then one line per student, then the count.
            TextMeshPro top = ProfessorPanelKit.AddText(content, "Info", string.Empty, 0.02f, GuidePalette.TextPrimary,
                TextAlignmentOptions.TopLeft, Vector2.zero, new Vector2(ContentWidth, 0.08f));

            Material dotMaterial = ProfessorPanelKit.Material("RosterDot", GuidePalette.Mint);
            var rows = new ClassRosterPanel.Row[ClassRosterText.MaxLines];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = BuildRow(content, i, RowsTop - i * RowPitch, dotMaterial);

            float bottom = -(PanelSize.y - HeaderAllowance);
            TextMeshPro summary = ProfessorPanelKit.AddText(content, "Summary", string.Empty, 0.018f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0f, bottom + 0.075f), new Vector2(ContentWidth, 0.025f));

            var panel = card.gameObject.AddComponent<ClassRosterPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_hands").objectReferenceValue = hands;
            serialized.FindProperty("_assist").objectReferenceValue = assist;
            serialized.FindProperty("_text").objectReferenceValue = top;
            serialized.FindProperty("_summary").objectReferenceValue = summary;
            SerializedProperty rowList = serialized.FindProperty("_rows");
            rowList.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                SerializedProperty row = rowList.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Root").objectReferenceValue = rows[i].Root;
                row.FindPropertyRelative("Name").objectReferenceValue = rows[i].Name;
                row.FindPropertyRelative("Detail").objectReferenceValue = rows[i].Detail;
                row.FindPropertyRelative("Hand").objectReferenceValue = rows[i].Hand;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (ClassRosterPanel.Row row in rows)
                row.Root.SetActive(false);

            // Two ray-select buttons along the bottom of the card: take down the hand that went up first, or all of them.
            Material buttons = LoadOrCreateMaterial(ButtonMaterialPath, "Universal Render Pipeline/Lit", new Color(0.12f, 0.12f, 0.14f));
            float row0 = -PanelSize.y / 2f + 0.032f;
            AddButton(card, "LowerNextHand", "Lower next hand", new Vector3(-0.14f, row0, -0.012f), buttons, panel.LowerNext);
            AddButton(card, "LowerAllHands", "Lower all hands", new Vector3(0.14f, row0, -0.012f), buttons, panel.LowerAll);

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

        /// <summary>Where the roster column stands at a given height: to the professor's right, near the table. The speaking panel shares it.</summary>
        public static Vector3 ColumnPosition(Transform root, float y)
        {
            Bounds table = FindTableTop(root);
            return new Vector3(table.center.x, y, table.center.z) + OffsetFromTable;
        }

        /// <summary>Upright and turned toward the professor. A panel reads from -Z, so +Z points away from them.</summary>
        public static Quaternion ColumnRotation(Vector3 position)
        {
            var away = new Vector3(position.x, 0f, position.z);
            return away.sqrMagnitude < 1e-6f ? Quaternion.identity : Quaternion.LookRotation(away, Vector3.up);
        }

        // A mint dot, the student's name, the device and questions in muted text, and a coral marker while the hand is up.
        private static ClassRosterPanel.Row BuildRow(Transform content, int index, float y, Material dotMaterial)
        {
            var rowObject = new GameObject($"Row{index}");
            rowObject.transform.SetParent(content, false);
            rowObject.transform.localPosition = new Vector3(0f, y, 0f);

            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "Dot";
            dot.transform.SetParent(rowObject.transform, false);
            dot.transform.localPosition = new Vector3(0.012f, -0.013f, -0.002f);
            dot.transform.localScale = new Vector3(0.012f, 0.012f, 0.003f);
            UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());
            dot.GetComponent<Renderer>().sharedMaterial = dotMaterial;

            TextMeshPro name = ProfessorPanelKit.AddText(rowObject.transform, "Name", string.Empty, 0.02f, GuidePalette.TextPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(0.034f, 0f), new Vector2(0.2f, 0.027f));
            TextMeshPro detail = ProfessorPanelKit.AddText(rowObject.transform, "Detail", string.Empty, 0.015f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0.24f, -0.005f), new Vector2(0.2f, 0.022f));
            TextMeshPro hand = ProfessorPanelKit.AddText(rowObject.transform, "Hand", string.Empty, 0.015f, GuidePalette.Coral,
                TextAlignmentOptions.TopRight, new Vector2(0.44f, -0.005f), new Vector2(0.12f, 0.022f), FontStyles.Bold);
            return new ClassRosterPanel.Row { Root = rowObject, Name = name, Detail = detail, Hand = hand };
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

        // A cube with an XRSimpleInteractable that calls the action when the ray selects it. The old label is kept for the kit
        // to hide; StyleButton adds the label that is shown.
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

            ProfessorPanelKit.StyleButton(button, label);
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
