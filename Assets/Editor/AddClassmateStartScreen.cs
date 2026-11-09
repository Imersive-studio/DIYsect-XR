using DIYsect.ClassmateConnection;
using DIYsect.Presentation;
using TMPro;
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
        public const string StatusCanvasName = "StatusCanvas";

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
            if (ClassmateUiBuilder.NeedsUi(screen, "_joinButton"))
            {
                screen = screen != null ? screen : rig.AddComponent<ClassmateStartScreen>();
                BuildCanvas(rig, screen);
            }
            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_joiner").objectReferenceValue = adapter;
            serialized.FindProperty("_statusLabel").objectReferenceValue = statusLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddStatusChip(rig, statusLabel);
        }

        // The status line as a small glass chip in the top-left corner: a mint "Live" and the room once in class.
        private static void AddStatusChip(GameObject rig, ClassmateStatusLabel statusLabel)
        {
            if (!ClassmateUiBuilder.NeedsUi(statusLabel, "_chipLabel"))
                return;
            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, StatusCanvasName, clickable: false, sortingOrder: 5);
            TextMeshProUGUI chip = ClassmateUiBuilder.AddChip(canvas.transform, "Status", new Vector2(0f, 1f),
                new Vector2(ClassmateUiBuilder.Margin, -ClassmateUiBuilder.Margin), ClassmateUiBuilder.SmallSize, GuidePalette.TextPrimary);
            chip.richText = true;
            var serialized = new SerializedObject(statusLabel);
            serialized.FindProperty("_chipLabel").objectReferenceValue = chip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // A calm, centred column: who is teaching, one big Join button, and a quieter way in with a room code.
        private static void BuildCanvas(GameObject rig, ClassmateStartScreen screen)
        {
            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, CanvasName, clickable: true, sortingOrder: 100);
            RectTransform column = ClassmateUiBuilder.AddPage(canvas.transform, out GameObject page);

            ClassmateUiBuilder.AddEyebrow(column, "LIVE ANATOMY CLASS", 40f);
            TextMeshProUGUI title = ClassmateUiBuilder.AddText(column, "Title", "DIYsect", 68f, GuidePalette.TextPrimary, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
            ClassmateUiBuilder.InColumn(title.rectTransform, 74f, 84f);
            TextMeshProUGUI subtitle = ClassmateUiBuilder.AddText(column, "Subtitle", "Your professor is running a class. Join to see the dissection live.", 22f, GuidePalette.TextMuted, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            ClassmateUiBuilder.InColumn(subtitle.rectTransform, 166f, 64f, 20f, 20f);

            Button join = ClassmateUiBuilder.AddButton(column, "JoinTheRoomButton", ClassmateStartScreen.JoinLabel, 26f, ClassmateUiBuilder.Kind.Primary, out TextMeshProUGUI joinLabel, 72f);
            ClassmateUiBuilder.InColumn((RectTransform)join.transform, 262f, 72f, 40f, 40f);

            TextMeshProUGUI hint = ClassmateUiBuilder.AddText(column, "CodeHint", "or join with a room code", 19f, GuidePalette.TextMuted, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            ClassmateUiBuilder.InColumn(hint.rectTransform, 368f, 28f);
            TMP_InputField field = ClassmateUiBuilder.AddInputField(column, "CodeField", "ROOM CODE", 26f, RoomInvite.CodeLength + 2, TMP_InputField.ContentType.Standard);
            ClassmateUiBuilder.InColumn((RectTransform)field.transform, 408f, ClassmateUiBuilder.ButtonHeight, 40f, 196f);
            Button codeButton = ClassmateUiBuilder.AddButton(column, "JoinWithCodeButton", "Join", 24f, ClassmateUiBuilder.Kind.Glass, out _);
            ClassmateUiBuilder.InColumn((RectTransform)codeButton.transform, 408f, ClassmateUiBuilder.ButtonHeight, 376f, 40f);

            TextMeshProUGUI message = ClassmateUiBuilder.AddText(column, "StartMessage", string.Empty, 19f, ClassmateUiBuilder.Warning, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            ClassmateUiBuilder.InColumn(message.rectTransform, 488f, 56f, 20f, 20f);

            var serialized = new SerializedObject(screen);
            serialized.FindProperty("_root").objectReferenceValue = page;
            serialized.FindProperty("_joinButton").objectReferenceValue = join;
            serialized.FindProperty("_joinLabel").objectReferenceValue = joinLabel;
            serialized.FindProperty("_codeField").objectReferenceValue = field;
            serialized.FindProperty("_codeButton").objectReferenceValue = codeButton;
            serialized.FindProperty("_message").objectReferenceValue = message;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
