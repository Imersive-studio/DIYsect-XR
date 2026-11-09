using DIYsect.ClassmateConnection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the classmate's start screen on the classmate camera: a full-screen page with a big "Join the room" button and a
    /// room code box, and tells the scene's ClassmateAutoJoin to wait for the press. Called by Build Shared Scene, which owns
    /// the scene; other scenes keep joining on their own. Safe to run more than once.
    /// </summary>
    public static class AddClassmateStartScreen
    {
        private const string CanvasName = "StartScreenCanvas";
        private static readonly Color Ink = new Color(0.05f, 0.07f, 0.1f, 1f);

        public static void AddTo(GameObject rig, ClassmateAutoJoin join, ClassmateStatusLabel statusLabel)
        {
            var joinSerialized = new SerializedObject(join);
            joinSerialized.FindProperty("_waitForJoinPress").boolValue = true;
            joinSerialized.ApplyModifiedPropertiesWithoutUndo();

            var adapter = rig.GetComponent<ClassmateJoinAdapter>() ?? rig.AddComponent<ClassmateJoinAdapter>();
            var adapterSerialized = new SerializedObject(adapter);
            adapterSerialized.FindProperty("_join").objectReferenceValue = join;
            adapterSerialized.ApplyModifiedPropertiesWithoutUndo();

            var screen = rig.GetComponent<ClassmateStartScreen>();
            if (screen == null)
            {
                screen = rig.AddComponent<ClassmateStartScreen>();
                BuildCanvas(rig, screen);
            }
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_joiner").objectReferenceValue = adapter;
            serialized.FindProperty("_statusLabel").objectReferenceValue = statusLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Everything sits on a page that is bigger than the safe area, so the dark background still covers a notch.
        private static void BuildCanvas(GameObject rig, ClassmateStartScreen screen)
        {
            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, CanvasName, clickable: true, sortingOrder: 100);

            var page = new GameObject("Page", typeof(RectTransform), typeof(Image));
            page.transform.SetParent(canvas.transform, false);
            var backdrop = page.GetComponent<Image>();
            backdrop.color = Ink;
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
            ClassmateUiBuilder.AddText(content.transform, "Subtitle", "Your professor is running a class.", 36, new Color(0.75f, 0.8f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.68f));

            Button join = ClassmateUiBuilder.AddButton(content.transform, "JoinTheRoomButton", ClassmateStartScreen.JoinLabel, 56, new Color(0.1f, 0.5f, 0.35f, 1f), out Text joinLabel);
            var joinRect = (RectTransform)join.transform;
            joinRect.anchorMin = new Vector2(0.25f, 0.38f);
            joinRect.anchorMax = new Vector2(0.75f, 0.56f);
            joinRect.offsetMin = Vector2.zero;
            joinRect.offsetMax = Vector2.zero;

            ClassmateUiBuilder.AddText(content.transform, "CodeHint", "Have a room code instead?", 28, new Color(0.75f, 0.8f, 0.9f), TextAnchor.MiddleCenter, new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.34f));
            InputField field = BuildCodeField(content.transform);
            Button codeButton = ClassmateUiBuilder.AddButton(content.transform, "JoinWithCodeButton", "Join", 40, new Color(0.2f, 0.3f, 0.5f, 1f), out _);
            var codeButtonRect = (RectTransform)codeButton.transform;
            codeButtonRect.anchorMin = new Vector2(0.58f, 0.14f);
            codeButtonRect.anchorMax = new Vector2(0.78f, 0.27f);
            codeButtonRect.offsetMin = Vector2.zero;
            codeButtonRect.offsetMax = Vector2.zero;

            Text message = ClassmateUiBuilder.AddText(content.transform, "Message", string.Empty, 30, new Color(1f, 0.75f, 0.45f), TextAnchor.MiddleCenter, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.12f));

            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_root").objectReferenceValue = page;
            serialized.FindProperty("_joinButton").objectReferenceValue = join;
            serialized.FindProperty("_joinLabel").objectReferenceValue = joinLabel;
            serialized.FindProperty("_codeField").objectReferenceValue = field;
            serialized.FindProperty("_codeButton").objectReferenceValue = codeButton;
            serialized.FindProperty("_message").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InputField BuildCodeField(Transform parent)
        {
            var fieldObject = new GameObject("CodeField", typeof(RectTransform), typeof(Image));
            fieldObject.transform.SetParent(parent, false);
            var background = fieldObject.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);
            var rect = (RectTransform)fieldObject.transform;
            rect.anchorMin = new Vector2(0.22f, 0.14f);
            rect.anchorMax = new Vector2(0.56f, 0.27f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = ClassmateUiBuilder.AddText(fieldObject.transform, "Text", string.Empty, 44, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            text.raycastTarget = false;
            Text placeholder = ClassmateUiBuilder.AddText(fieldObject.transform, "Placeholder", "ROOM CODE", 36, new Color(1f, 1f, 1f, 0.4f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            placeholder.resizeTextForBestFit = false;

            var field = fieldObject.AddComponent<InputField>();
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.characterLimit = RoomInvite.CodeLength + 2;
            field.contentType = InputField.ContentType.Standard;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }
    }
}
