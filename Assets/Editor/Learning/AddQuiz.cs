// Run Tools > DIYsect > Add Quiz after Add Question Pins, Add Classmate Desktop Camera and Add Student View Window.
using System;
using System.IO;
using System.Linq;
using DIYsect.Classroom;
using DIYsect.Learning;
using DIYsect.Networking;
using DIYsect.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace DIYsect.Editor
{
    /// <summary>
    /// Sets up the "Tap the ..." quiz in three places. Safe to run more than once.
    /// 1. Adds QuizRound to Assets/Prefabs/AnatomyBody.prefab, so every scene's body has it.
    /// 2. Builds the professor's quiz panel in Professor_MR, beside the table on the professor's right (the student
    ///    view window is on the left): a designed card (ProfessorPanelKit) with the question, a status line and ray-selectable &lt; &gt; Start Reveal Clear buttons. The panel is
    ///    given the default question set (DefaultQuizQuestions.asset).
    /// 3. Gives the classmate camera in Classmate_Desktop a QuizAnswerInput and a quiz line at the top of the screen, and lists the answer input as a tap claimer on its PinPlacementInput.
    /// 4. Lets the student in the headset answer: a QuizHeadsetAnswer on the XR origin (off until the role switch turns it on
    ///    for the student) and a small question card standing beyond the table, facing the student.
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
        private static readonly Vector2 PanelSize = new Vector2(0.7f, 0.48f);
        // The kit's content area: a little narrower than the card. Y runs down from the top left corner.
        private const float ContentWidth = 0.64f;
        private const float RowY = -0.31f;
        private static readonly Vector3 SmallButton = new Vector3(0.07f, 0.06f, 0.02f);
        private static readonly Vector3 WideButton = new Vector3(0.22f, 0.06f, 0.02f);
        private static readonly Vector3 ClearButton = new Vector3(0.12f, 0.06f, 0.02f);
        // From the table centre: further to the professor's right than the class roster (0.95 m), at eye height, so the two do not overlap.
        private static readonly Vector3 OffsetFromTable = new Vector3(1.75f, 0f, -0.1f);
        private const float Height = 1.45f;
        // The panel is on the CloseUpScreen layer so no camera films it, but the headset rays only hit Default and UI and
        // the hands only Default and the organ layers. Each button therefore gets a collider-only child on Default.
        public const string PressAreaName = "PressArea";
        private const int PressAreaLayer = 0;
        public const string StudentCardName = "StudentQuizCard";
        private static readonly Vector2 StudentCardSize = new Vector2(0.62f, 0.26f);
        private const float StudentCardHeight = 1.5f;
        // How far past the table's far edge the student's card stands, so it never covers the body.
        private const float StudentCardBeyondTable = 0.45f;

        [MenuItem("Tools/DIYsect/Add Quiz")]
        public static void Apply()
        {
            ApplyToPrefab();
            AddProfessorPanel();
            AddClassmateControls();
            AddStudentControls();
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
            // The card is the QuizPanel object: same name and parent as before, so the arc step still finds it.
            Transform card = ProfessorPanelKit.BuildCard(root, PanelName, PanelSize, "QUIZ");
            GameObject panelObject = card.gameObject;
            Bounds table = FindTableTop(root, ProfessorScenePath);
            Vector3 position = new Vector3(table.center.x, Height, table.center.z) + OffsetFromTable;
            Vector3 professorHead = new Vector3(0f, Height, 0f);
            // The card face is -Z, so +Z points away from the professor.
            panelObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - professorHead, Vector3.up));
            // Pole from the card bottom to the floor (the DIYsect root's height).
            ProfessorPanelKit.AddStand(card, position.y - PanelSize.y / 2f, root.position.y);

            Transform content = card.Find("Content");
            TextMeshPro question = ProfessorPanelKit.AddText(content, "Question", "", 0.04f, GuidePalette.TextPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 0f), new Vector2(ContentWidth, 0.12f), FontStyles.Bold);
            TextMeshPro status = ProfessorPanelKit.AddText(content, "Status", "", 0.026f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0f, -0.125f), new Vector2(ContentWidth, 0.035f));
            TextMeshPro detail = ProfessorPanelKit.AddText(content, "Detail", "", 0.02f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0f, -0.17f), new Vector2(ContentWidth, 0.08f));

            var panel = panelObject.AddComponent<QuizPanel>();
            // Control row, left to right: previous and next question (small), then the main action (Start or Reveal,
            // wide, in the same place) and Clear. The panel script shows only the buttons that fit the phase.
            GameObject previous = AddButton(content, "PreviousQuestion", "<", new Vector2(0.075f, RowY), SmallButton, panel.PreviousQuestion);
            GameObject next = AddButton(content, "NextQuestion", ">", new Vector2(0.16f, RowY), SmallButton, panel.NextQuestion);
            GameObject start = AddButton(content, "Start", "Start", new Vector2(0.39f, RowY), WideButton, panel.StartRound);
            GameObject end = AddButton(content, "End", "Reveal", new Vector2(0.39f, RowY), WideButton, panel.EndRound);
            GameObject clear = AddButton(content, "Clear", "Clear", new Vector2(0.58f, RowY), ClearButton, panel.ClearRound);

            // Same layer as the student view window, so neither the close-up camera nor the window camera films the panel.
            // The press areas stay on Default so the professor's rays and hands can reach the buttons.
            foreach (Transform part in panelObject.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = part.name == PressAreaName ? PressAreaLayer : layer;

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_quiz").objectReferenceValue = quiz;
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.FindProperty("_question").objectReferenceValue = question;
            serialized.FindProperty("_detail").objectReferenceValue = detail;
            serialized.FindProperty("_questions").objectReferenceValue = questions;
            serialized.FindProperty("_startButton").objectReferenceValue = start;
            serialized.FindProperty("_endButton").objectReferenceValue = end;
            serialized.FindProperty("_clearButton").objectReferenceValue = clear;
            SerializedProperty steps = serialized.FindProperty("_stepButtons");
            steps.arraySize = 2;
            steps.GetArrayElementAtIndex(0).objectReferenceValue = previous;
            steps.GetArrayElementAtIndex(1).objectReferenceValue = next;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Save(scene, ProfessorScenePath);
            Debug.Log($"{ProfessorScenePath}: quiz panel at {position} with {questions.Count} questions.");
        }

        public static void AddClassmateControls()
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
            AddClassmatePointInput.SetClaimers(rig);

            TMPro.TMP_Text label = GetOrCreateQuizLine(rig);
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

        // A glass chip with the quiz line above the dock and its notes, where the thumb already is. Hidden until a round starts.
        private static TMPro.TMP_Text GetOrCreateQuizLine(GameObject rig)
        {
            Transform existing = rig.transform.Find(CanvasName);
            if (existing != null)
            {
                TMPro.TMP_Text found = existing.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (found != null)
                    return found;
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, CanvasName, clickable: false, sortingOrder: 10);
            TMPro.TextMeshProUGUI label = ClassmateUiBuilder.AddChip(canvas.transform, "QuizLine", new Vector2(0.5f, 0f), new Vector2(0f, AddClassmateBottomBar.CardLine),
                ClassmateUiBuilder.BodySize, new Color(1f, 0.92f, 0.55f), hidden: true);
            return label;
        }

        // A cube button with an XRSimpleInteractable that calls the action when selected. The kit styles it and adds the label.
        // The old TextMesh label is added first because the kit hides it; Content's origin is its top left corner.
        private static GameObject AddButton(Transform content, string name, string label, Vector2 centre, Vector3 scale, UnityAction action)
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(content, false);
            button.transform.localPosition = new Vector3(centre.x, centre.y, -scale.z / 2f);
            button.transform.localScale = scale;
            var interactable = button.AddComponent<XRSimpleInteractable>();
            UnityEventTools.AddVoidPersistentListener(interactable.selectEntered, action);
            var textObject = new GameObject("Label");
            textObject.transform.SetParent(button.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = label;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.08f;
            mesh.fontSize = 64;
            ProfessorPanelKit.StyleButton(button, label);
            // The visible cube keeps the panel's layer; the collider moves to a child that the rays can hit. A unit box
            // under the cube's scale is exactly the button's size. XRSimpleInteractable finds colliders in its children.
            var cubeCollider = button.GetComponent<Collider>();
            if (cubeCollider != null)
                UnityEngine.Object.DestroyImmediate(cubeCollider);
            var press = new GameObject(PressAreaName);
            press.transform.SetParent(button.transform, false);
            press.AddComponent<BoxCollider>();
            return button;
        }

        /// <summary>
        /// Lets the student in the headset take a round: a QuizHeadsetAnswer on the XR origin that reads either trigger and
        /// a question card that stands beyond the table, facing the student. The component is saved switched off and is
        /// listed in the role's student scripts, so only the student's headset runs it (Build Shared Scene keeps it there
        /// through <see cref="WithStudentInput"/>).
        /// </summary>
        public static void AddStudentControls()
        {
            Scene scene = EditorSceneManager.OpenScene(ProfessorScenePath, OpenSceneMode.Single);
            Transform root = FindRoot(scene, RootName);
            QuizRound quiz = root.GetComponentInChildren<QuizRound>(true)
                ?? throw new InvalidOperationException($"{ProfessorScenePath} has no body with a QuizRound. Is the body in the scene an AnatomyBody prefab instance?");
            var origin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ProfessorScenePath} has no XR Origin.");
            Transform leftAim = FindAim(origin.transform, "Left Controller");
            Transform rightAim = FindAim(origin.transform, "Right Controller");

            Transform old = root.Find(StudentCardName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            // The student stands on the scene origin's side of the table (the room code board and quiz panel face it too).
            Bounds table = FindTableTop(root, ProfessorScenePath);
            Vector3 centre = new Vector3(table.center.x, root.position.y + StudentCardHeight, table.center.z);
            Vector3 away = centre - new Vector3(root.position.x, centre.y, root.position.z);
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.forward;
            away.Normalize();
            float halfDepth = table.extents.x * Mathf.Abs(away.x) + table.extents.z * Mathf.Abs(away.z);
            Vector3 position = centre + away * (halfDepth + StudentCardBeyondTable);
            Transform card = ProfessorPanelKit.BuildCard(root, StudentCardName, StudentCardSize, "Quiz");
            card.SetPositionAndRotation(position, Quaternion.LookRotation(away, Vector3.up));
            ProfessorPanelKit.AddStand(card, position.y - StudentCardSize.y / 2f, root.position.y);
            Transform content = card.Find("Content");
            Vector2 area = ProfessorPanelKit.ContentSize(StudentCardSize);
            TextMeshPro question = ProfessorPanelKit.AddText(content, "Question", "", 0.024f, GuidePalette.TextPrimary,
                TextAlignmentOptions.TopLeft, new Vector2(0.03f, -0.02f), new Vector2(area.x - 0.06f, 0.09f), FontStyles.Bold);
            TextMeshPro status = ProfessorPanelKit.AddText(content, "Status", "", 0.017f, GuidePalette.TextMuted,
                TextAlignmentOptions.TopLeft, new Vector2(0.03f, -0.115f), new Vector2(area.x - 0.06f, 0.055f));
            // Hidden until a round is asking; the component shows it.
            card.gameObject.SetActive(false);

            var answer = origin.GetComponent<QuizHeadsetAnswer>();
            if (answer == null)
                answer = origin.gameObject.AddComponent<QuizHeadsetAnswer>();
            var serialized = new SerializedObject(answer);
            serialized.FindProperty("_quiz").objectReferenceValue = quiz;
            serialized.FindProperty("_leftAim").objectReferenceValue = leftAim;
            serialized.FindProperty("_rightAim").objectReferenceValue = rightAim;
            serialized.FindProperty("_card").objectReferenceValue = card.gameObject;
            serialized.FindProperty("_question").objectReferenceValue = question;
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            answer.enabled = false;
            EditorUtility.SetDirty(answer);

            // List it in the role's student scripts now, so the quiz works even before Build Shared Scene runs again.
            var role = UnityEngine.Object.FindFirstObjectByType<XRMultiplayer.ClassroomRole>(FindObjectsInactive.Include);
            if (role != null)
            {
                var roleSerialized = new SerializedObject(role);
                SerializedProperty students = roleSerialized.FindProperty("_studentBehaviours");
                bool listed = false;
                for (int i = 0; i < students.arraySize; i++)
                    if (students.GetArrayElementAtIndex(i).objectReferenceValue == answer)
                        listed = true;
                if (!listed)
                {
                    students.arraySize++;
                    students.GetArrayElementAtIndex(students.arraySize - 1).objectReferenceValue = answer;
                    roleSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            Save(scene, ProfessorScenePath);
            Debug.Log($"{ProfessorScenePath}: the student answers the quiz with either trigger; question card at {position}.");
        }

        /// <summary>
        /// Adds the rig's QuizHeadsetAnswer, switched off, to the student-only scripts, so Build Shared Scene keeps the
        /// student able to answer. Returns the list unchanged when the rig has none (Add Quiz not run yet).
        /// </summary>
        public static Behaviour[] WithStudentInput(Behaviour[] behaviours, GameObject rig)
        {
            if (rig == null || !rig.TryGetComponent(out QuizHeadsetAnswer input))
                return behaviours;
            input.enabled = false;
            EditorUtility.SetDirty(input);
            return behaviours.Concat(new Behaviour[] { input }).Distinct().ToArray();
        }

        // Where the controller's visible ray starts: its far caster's origin, or the controller itself.
        private static Transform FindAim(Transform rig, string controllerName)
        {
            foreach (Transform candidate in rig.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != controllerName)
                    continue;
                CurveInteractionCaster caster = candidate.GetComponentInChildren<CurveInteractionCaster>(true);
                return caster != null && caster.castOrigin != null ? caster.castOrigin : candidate;
            }
            throw new InvalidOperationException($"The XR Origin has no {controllerName}.");
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
