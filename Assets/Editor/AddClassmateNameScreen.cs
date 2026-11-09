using DIYsect.ClassmateConnection;
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
        private static readonly Color Ink = new Color(0.05f, 0.07f, 0.1f, 1f);

        public static void AddTo(GameObject rig, ClassmateStartScreen startScreen)
        {
            var adapter = rig.GetComponent<ClassmateJoinAdapter>() ?? throw new System.InvalidOperationException("The classmate camera has no ClassmateJoinAdapter. Add the start screen first.");
            var screen = rig.GetComponent<ClassmateNameScreen>();
            if (screen == null)
            {
                screen = rig.AddComponent<ClassmateNameScreen>();
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

            var page = new GameObject("Page", typeof(RectTransform), typeof(Image));
            page.transform.SetParent(canvas.transform, false);
            page.GetComponent<Image>().color = Ink;
            var pageRect = (RectTransform)page.transform;
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.offsetMin = new Vector2(-600f, -600f);
            pageRect.offsetMax = new Vector2(600f, 600f);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(page.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(600f, 600f);
            contentRect.offsetMax = new Vector2(-600f, -600f);

            ClassmateUiBuilder.AddText(content.transform, "Title", "DIYsect", 72, Color.white, TextAnchor.MiddleCenter, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.86f));
            ClassmateUiBuilder.AddText(content.transform, "Prompt", "What is your name?", 40, new Color(0.75f, 0.8f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.68f));

            var fieldObject = new GameObject("NameField", typeof(RectTransform), typeof(Image));
            fieldObject.transform.SetParent(content.transform, false);
            var background = fieldObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);
            var fieldRect = (RectTransform)fieldObject.transform;
            fieldRect.anchorMin = new Vector2(0.25f, 0.4f);
            fieldRect.anchorMax = new Vector2(0.75f, 0.54f);
            fieldRect.offsetMin = Vector2.zero;
            fieldRect.offsetMax = Vector2.zero;
            Text text = ClassmateUiBuilder.AddText(fieldObject.transform, "Text", string.Empty, 48, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            Text placeholder = ClassmateUiBuilder.AddText(fieldObject.transform, "Placeholder", "Your name", 40, new Color(1f, 1f, 1f, 0.4f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            placeholder.resizeTextForBestFit = false;
            var field = fieldObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = NameRules.MaxLength;
            field.contentType = InputField.ContentType.Name;
            field.lineType = InputField.LineType.SingleLine;

            Button next = ClassmateUiBuilder.AddButton(content.transform, "ContinueButton", "Continue", 52, new Color(0.1f, 0.5f, 0.35f, 1f), out _);
            var nextRect = (RectTransform)next.transform;
            nextRect.anchorMin = new Vector2(0.3f, 0.2f);
            nextRect.anchorMax = new Vector2(0.7f, 0.34f);
            nextRect.offsetMin = Vector2.zero;
            nextRect.offsetMax = Vector2.zero;

            Text message = ClassmateUiBuilder.AddText(content.transform, "NameMessage", string.Empty, 30, new Color(1f, 0.75f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.18f));

            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_root").objectReferenceValue = page;
            serialized.FindProperty("_field").objectReferenceValue = field;
            serialized.FindProperty("_continue").objectReferenceValue = next;
            serialized.FindProperty("_message").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
