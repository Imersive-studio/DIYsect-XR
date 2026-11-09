using DIYsect.ClassmateControls;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the row of thumb-sized buttons along the bottom of the classmate's screen: Mute / Unmute on the left of centre
    /// and Raise hand on the right. The row sits under the pin message and the note lines, clear of the Reset view and follow
    /// buttons on the right edge and the banner in the upper middle. Called by Build Shared Scene. Safe to run more than once.
    /// </summary>
    public static class AddClassmateBottomBar
    {
        public const string CanvasName = "BottomBarCanvas";
        public const string BarName = "Bar";
        public static readonly Vector2 ButtonSize = new Vector2(300f, 96f);
        private static readonly Color ButtonColour = new Color(0f, 0f, 0f, 0.65f);

        /// <summary>Makes the canvas, the bar and the Mute button if they are missing. Returns the bar so more buttons can join it.</summary>
        public static Transform EnsureBar(GameObject rig)
        {
            Transform canvasTransform = rig.transform.Find(CanvasName);
            if (canvasTransform == null)
                canvasTransform = ClassmateUiBuilder.NewCanvas(rig.transform, CanvasName, clickable: true).transform;
            // Everything lives under the SafeArea panel once the layout step has run.
            Transform bar = canvasTransform.Find(BarName) ?? canvasTransform.Find(DIYsect.Classroom.ClassmateCanvasLayout.PanelName + "/" + BarName);
            if (bar == null)
            {
                var barObject = new GameObject(BarName, typeof(RectTransform));
                barObject.transform.SetParent(canvasTransform, false);
                var rect = (RectTransform)barObject.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                bar = barObject.transform;
            }
            var barBehaviour = rig.GetComponent<ClassmateBottomBar>() ?? rig.AddComponent<ClassmateBottomBar>();
            var serialized = new SerializedObject(barBehaviour);
            serialized.FindProperty("_bar").objectReferenceValue = bar.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        public static void AddMic(GameObject rig)
        {
            Transform bar = EnsureBar(rig);
            var mic = rig.GetComponent<ClassmateMicButton>();
            if (mic == null)
            {
                mic = rig.AddComponent<ClassmateMicButton>();
                Button button = AddBarButton(bar, "MuteButton", MicButtonModel.UnmuteLabel, new Vector2(1f, 0f), -12f, out Text label);
                Text message = ClassmateUiBuilder.AddText(bar, "MicMessage", string.Empty, 30, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleCenter, new Vector2(0.15f, 0.175f), new Vector2(0.85f, 0.235f));
                message.gameObject.SetActive(false);
                var serialized = new SerializedObject(mic);
                serialized.FindProperty("_button").objectReferenceValue = button;
                serialized.FindProperty("_label").objectReferenceValue = label;
                serialized.FindProperty("_message").objectReferenceValue = message;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var voice = Object.FindFirstObjectByType<VoiceChatManager>(FindObjectsInactive.Include)
                ?? throw new System.InvalidOperationException("The scene has no VoiceChatManager. It comes with the XRI Network Game Manager prefab.");
            var micSerialized = new SerializedObject(mic);
            micSerialized.FindProperty("_voice").objectReferenceValue = voice;
            micSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AddHand(GameObject rig, DIYsect.Classroom.HandBoard hands)
        {
            Transform bar = EnsureBar(rig);
            var hand = rig.GetComponent<ClassmateHandButton>();
            if (hand == null)
            {
                hand = rig.AddComponent<ClassmateHandButton>();
                Button button = AddBarButton(bar, "RaiseHandButton", HandButtonLabels.Raise, new Vector2(0f, 0f), 12f, out Text label);
                var created = new SerializedObject(hand);
                created.FindProperty("_button").objectReferenceValue = button;
                created.FindProperty("_label").objectReferenceValue = label;
                created.ApplyModifiedPropertiesWithoutUndo();
            }
            var serialized = new SerializedObject(hand);
            serialized.FindProperty("_hands").objectReferenceValue = hands;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A thumb-sized button on the bottom edge, hanging off the centre line on the side the pivot's x says.</summary>
        public static Button AddBarButton(Transform bar, string name, string text, Vector2 pivot, float xFromCentre, out Text label)
        {
            Button button = ClassmateUiBuilder.AddButton(bar, name, text, 40, ButtonColour, out label);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = pivot;
            rect.sizeDelta = ButtonSize;
            rect.anchoredPosition = new Vector2(xFromCentre, 24f);
            return button;
        }
    }
}
