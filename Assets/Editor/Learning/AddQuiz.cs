// Run Tools > DIYsect > Add Quiz after Add Question Pins, Add Classmate Desktop Camera and Add Student View Window.
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.Learning;
using DIYsect.Networking;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Sets up the "Tap the ..." quiz in three places. Safe to run more than once.
    /// 1. Adds QuizRound to Assets/Prefabs/AnatomyBody.prefab, so every scene's body has it.
    /// 2. Builds the professor's quiz panel in Professor_MR, beside the table on the professor's right (the student
    ///    view window is on the left): status text and ray-selectable &lt; &gt; Start End Clear buttons. The panel is
    ///    given the default question set (DefaultQuizQuestions.asset).
    /// 3. Gives the classmate camera in Classmate_Desktop a QuizAnswerInput and a quiz line at the top of the screen, and lists the answer input as a tap claimer on its PinPlacementInput.
    /// </summary>
    public static class AddQuiz
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        public const string QuestionsPath = "Assets/Scripts/Learning/Quiz/DefaultQuizQuestions.asset";
        private const string ProfessorScenePath = "Assets/Scenes/Professor_MR.unity";
        // Professor_MR is the shared scene: the classmate camera lives there next to the headset rig (see BuildSharedScene).
        private const string ClassmateScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string PanelName = "QuizPanel";
        private const string ClassmateRigName = "ClassmateCamera";
        private const string CanvasName = "QuizCanvas";
        private const string ScreenLayerName = "CloseUpScreen";
        private const string PanelMaterialPath = "Assets/Models/Environment/QuizPanel.mat";
        private const string ButtonMaterialPath = "Assets/Models/Environment/StudentViewFrame.mat";
        private static readonly Vector2 PanelSize = new Vector2(0.5f, 0.3f);
        private static readonly Vector3 SmallButton = new Vector3(0.06f, 0.06f, 0.02f);
        private static readonly Vector3 WideButton = new Vector3(0.12f, 0.06f, 0.02f);
        // From the table centre: further to the professor's right than the class roster (0.95 m), at eye height, so the two do not overlap.
        private static readonly Vector3 OffsetFromTable = new Vector3(1.75f, 0f, -0.1f);
        private const float Height = 1.45f;

        [MenuItem("Tools/DIYsect/Add Quiz")]
        public static void Apply()
        {
            ApplyToPrefab();
            AddProfessorPanel();
            AddClassmateControls();
        }

        /// <summary>Adds QuizRound to the body prefab. Also usable from Create Anatomy Body, so a rebuilt body keeps it.</summary>
        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<SharedBody>() == null)
                    throw new InvalidOperationException("The body has no SharedBody. Run Tools > DIYsect > Add Body Networking first.");
                if (!root.TryGetComponent(out QuizRound _))
                    root.AddComponent<QuizRound>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the quiz to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddProfessorPanel()
        {
            var questions = AssetDatabase.LoadAssetAtPath<QuizQuestionSet>(QuestionsPath);
            if (questions == null)
                throw new InvalidOperationException($"{QuestionsPath} is missing or is not a QuizQuestionSet.");

            Scene scene = EditorSceneManager.OpenScene(ProfessorScenePath, OpenSceneMode.Single);
            // Opening a scene in Single mode unloads unused assets, which leaves the object loaded above dangling. Assigning it
            // then saved an empty reference and the panel disabled itself on the headset. Load it again now.
            questions = AssetDatabase.LoadAssetAtPath<QuizQuestionSet>(QuestionsPath);
            if (questions == null)
                throw new InvalidOperationException($"{QuestionsPath} could not be loaded after opening the scene.");
            Transform root = FindRoot(scene, RootName);
            QuizRound quiz = root.GetComponentInChildren<QuizRound>(true);
            if (quiz == null)
                throw new InvalidOperationException($"{ProfessorScenePath} has no body with a QuizRound. Is the body in the scene an AnatomyBody prefab instance?");
            int layer = LayerMask.NameToLayer(ScreenLayerName);
            if (layer < 0)
                throw new InvalidOperationException($"Layer {ScreenLayerName} is missing. Run Tools > DIYsect > Create Scenes.");

            Transform old = root.Find(PanelName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var panelObject = new GameObject(PanelName);
            panelObject.transform.SetParent(root, false);
            Bounds table = FindTableTop(root, ProfessorScenePath);
            Vector3 position = new Vector3(table.center.x, Height, table.center.z) + OffsetFromTable;
            Vector3 professorHead = new Vector3(0f, Height, 0f);
            // A quad's visible side faces -Z, so +Z points away from the professor.
            panelObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - professorHead, Vector3.up));

            Material background = LoadOrCreateMaterial(PanelMaterialPath, "Universal Render Pipeline/Unlit", new Color(0.08f, 0.1f, 0.14f));
            Material buttons = LoadOrCreateMaterial(ButtonMaterialPath, "Universal Render Pipeline/Lit", new Color(0.12f, 0.12f, 0.14f));

            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            board.name = "Background";
            board.transform.SetParent(panelObject.transform, false);
            board.transform.localPosition = new Vector3(0f, 0f, 0.004f);
            board.transform.localScale = new Vector3(PanelSize.x, PanelSize.y, 1f);
            board.GetComponent<Renderer>().sharedMaterial = background;
            UnityEngine.Object.DestroyImmediate(board.GetComponent<Collider>());

            AddText(panelObject.transform, "Title", new Vector3(0f, PanelSize.y / 2f + 0.03f, -0.002f), "Quiz", 0.007f);
            TextMesh status = AddText(panelObject.transform, "Status", new Vector3(0f, 0.04f, -0.002f), "", 0.0045f);

            var panel = panelObject.AddComponent<QuizPanel>();
            float row = -PanelSize.y / 2f - 0.05f;
            AddButton(panelObject.transform, "PreviousQuestion", "<", new Vector3(-0.245f, row, 0f), SmallButton, buttons, panel.PreviousQuestion);
            AddButton(panelObject.transform, "Start", "Start", new Vector3(-0.135f, row, 0f), WideButton, buttons, panel.StartRound);
            AddButton(panelObject.transform, "End", "End", new Vector3(0f, row, 0f), WideButton, buttons, panel.EndRound);
            AddButton(panelObject.transform, "Clear", "Clear", new Vector3(0.135f, row, 0f), WideButton, buttons, panel.ClearRound);
            AddButton(panelObject.transform, "NextQuestion", ">", new Vector3(0.245f, row, 0f), SmallButton, buttons, panel.NextQuestion);

            // Same layer as the student view window, so neither the close-up camera nor the window camera films the panel.
            foreach (Transform part in panelObject.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = layer;

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_quiz").objectReferenceValue = quiz;
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.FindProperty("_questions").objectReferenceValue = questions;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Save(scene, ProfessorScenePath);
            Debug.Log($"{ProfessorScenePath}: quiz panel at {position} with {questions.Count} questions.");
        }

        private static void AddClassmateControls()
        {
            Scene scene = EditorSceneManager.OpenScene(ClassmateScenePath, OpenSceneMode.Single);
            GameObject rig = null;
            QuizRound quiz = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == ClassmateRigName)
                    rig = root;
                if (quiz == null)
                    quiz = root.GetComponentInChildren<QuizRound>(true);
            }
            if (rig == null)
                throw new InvalidOperationException($"{ClassmateScenePath} has no {ClassmateRigName}. Run Tools > DIYsect > Add Classmate Desktop Camera first.");
            if (quiz == null)
                throw new InvalidOperationException($"{ClassmateScenePath} has no body with a QuizRound. Is the body in the scene an AnatomyBody prefab instance?");
            var tracker = rig.GetComponent<PointerGestureTracker>();
            if (tracker == null)
                throw new InvalidOperationException($"{ClassmateRigName} needs a PointerGestureTracker. Run Tools > DIYsect > Add Classmate Desktop Camera.");

            var answers = rig.GetComponent<QuizAnswerInput>();
            if (answers == null)
                answers = rig.AddComponent<QuizAnswerInput>();
            var answerSerialized = new SerializedObject(answers);
            answerSerialized.FindProperty("_quiz").objectReferenceValue = quiz;
            answerSerialized.FindProperty("_gestures").objectReferenceValue = tracker;
            answerSerialized.FindProperty("_camera").objectReferenceValue = rig.GetComponent<Camera>();
            answerSerialized.ApplyModifiedPropertiesWithoutUndo();

            // A tap during a round answers the quiz, so the question-pin input must not also place a pin.
            var pins = rig.GetComponent<PinPlacementInput>()
                ?? throw new InvalidOperationException($"{ClassmateRigName} has no PinPlacementInput. Run Tools > DIYsect > Add Classmate Desktop Camera.");
            var pinSerialized = new SerializedObject(pins);
            SerializedProperty claimers = pinSerialized.FindProperty("_tapClaimers");
            claimers.arraySize = 1;
            claimers.GetArrayElementAtIndex(0).objectReferenceValue = answers;
            pinSerialized.ApplyModifiedPropertiesWithoutUndo();

            // The same tap shows an organ name when there is no round; during one it is an answer and shows none.
            AddOrganNameTap.SetClaimers(rig);

            Text label = GetOrCreateQuizLine(rig);
            var prompt = rig.GetComponent<QuizPrompt>();
            if (prompt == null)
                prompt = rig.AddComponent<QuizPrompt>();
            var promptSerialized = new SerializedObject(prompt);
            promptSerialized.FindProperty("_quiz").objectReferenceValue = quiz;
            promptSerialized.FindProperty("_label").objectReferenceValue = label;
            promptSerialized.ApplyModifiedPropertiesWithoutUndo();

            Save(scene, ClassmateScenePath);
            Debug.Log($"{ClassmateScenePath}: quiz answers and quiz line on {ClassmateRigName}.");
        }

        // A screen overlay with one line of text near the top (the pin message uses the bottom). Hidden until a round starts.
        private static Text GetOrCreateQuizLine(GameObject rig)
        {
            Transform existing = rig.transform.Find(CanvasName);
            if (existing != null)
            {
                Text found = existing.GetComponentInChildren<Text>(true);
                if (found != null)
                    return found;
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var canvasObject = new GameObject(CanvasName);
            canvasObject.transform.SetParent(rig.transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            var labelObject = new GameObject("QuizLine");
            labelObject.transform.SetParent(canvasObject.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.92f, 0.4f);
            label.raycastTarget = false;
            var shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.1f, 0.82f);
            rect.anchorMax = new Vector2(0.9f, 0.94f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            labelObject.SetActive(false);
            return label;
        }

        // A cube button with an XRSimpleInteractable that calls the action when selected, and a label on its front.
        // The label is scaled back to square, so a wide button does not stretch its text.
        private static void AddButton(Transform parent, string name, string label, Vector3 position, Vector3 scale, Material material, UnityAction action)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(parent, false);
            button.transform.localPosition = position;
            button.transform.localScale = scale;
            button.GetComponent<Renderer>().sharedMaterial = material;
            var interactable = button.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, action);
            TextMesh text = AddText(button.transform, "Label", new Vector3(0f, 0f, -0.6f), label, 0.08f);
            text.transform.localScale = new Vector3(scale.y / scale.x, 1f, 1f);
        }

        // White, centred world-space text.
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

        // Loads the material at the path, or creates it with the shader and colour and saves it there.
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

        private static Transform FindRoot(Scene scene, string rootName)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == rootName)
                    return candidate.transform;
            throw new InvalidOperationException($"{scene.path} has no {rootName} root. Run Tools > DIYsect > Create Scenes.");
        }

        private static Bounds FindTableTop(Transform root, string scenePath)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Top" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("DissectionTable"))
                    return renderer.bounds;
            throw new InvalidOperationException($"{scenePath} has no DissectionTable/Top under {root.name}.");
        }

        private static void Save(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddQuiz.ApplyBatch
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
    }
}
