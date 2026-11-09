using DIYsect.ClassmateConnection;
using DIYsect.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the classmate's name page on the classmate camera: a full-screen page that asks "What is your name?", drawn above the
    /// Join page so it comes first. Called by Build Shared Scene after the start screen. Safe to run more than once.
    /// </summary>
    public static class AddClassmateNameScreen
    {
        private const string CanvasName = "NameScreenCanvas";

        public static void AddTo(GameObject rig, ClassmateStartScreen startScreen)
        {
            var adapter = rig.GetComponent<ClassmateJoinAdapter>() ?? throw new System.InvalidOperationException("The classmate camera has no ClassmateJoinAdapter. Add the start screen first.");
            var screen = rig.GetComponent<ClassmateNameScreen>();
            if (ClassmateUiBuilder.NeedsUi(screen, "_field"))
            {
                screen = screen != null ? screen : rig.AddComponent<ClassmateNameScreen>();
                BuildCanvas(rig, screen);
            }
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_joiner").objectReferenceValue = adapter;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The Join page lets the status and checklist lines show again only when neither page covers the scene.
            var startSerialized = new SerializedObject(startScreen);
            startSerialized.FindProperty("_nameScreen").objectReferenceValue = screen;
            startSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCanvas(GameObject rig, ClassmateNameScreen screen)
        {
            // Above the Join page (sorting order 100), so it is the first thing seen.
            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, CanvasName, clickable: true, sortingOrder: 110);
            RectTransform column = ClassmateUiBuilder.AddPage(canvas.transform, out GameObject page);

            ClassmateUiBuilder.AddEyebrow(column, "WELCOME TO CLASS", 60f);
            TextMeshProUGUI title = ClassmateUiBuilder.AddText(column, "Title", "DIYsect", 68f, GuidePalette.TextPrimary, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
            ClassmateUiBuilder.InColumn(title.rectTransform, 94f, 84f);
            TextMeshProUGUI prompt = ClassmateUiBuilder.AddText(column, "Prompt", "What is your name?", 24f, GuidePalette.TextMuted, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            ClassmateUiBuilder.InColumn(prompt.rectTransform, 190f, 36f);

            TMP_InputField field = ClassmateUiBuilder.AddInputField(column, "NameField", "Your name", 26f, NameRules.MaxLength, TMP_InputField.ContentType.Name);
            ClassmateUiBuilder.InColumn((RectTransform)field.transform, 254f, 72f, 40f, 40f);
            Button next = ClassmateUiBuilder.AddButton(column, "ContinueButton", "Continue", 26f, ClassmateUiBuilder.Kind.Primary, out _, 72f);
            ClassmateUiBuilder.InColumn((RectTransform)next.transform, 346f, 72f, 40f, 40f);

            TextMeshProUGUI message = ClassmateUiBuilder.AddText(column, "NameMessage", string.Empty, 19f, ClassmateUiBuilder.Warning, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            ClassmateUiBuilder.InColumn(message.rectTransform, 436f, 56f, 20f, 20f);

            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_root").objectReferenceValue = page;
            serialized.FindProperty("_field").objectReferenceValue = field;
            serialized.FindProperty("_continue").objectReferenceValue = next;
            serialized.FindProperty("_message").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
