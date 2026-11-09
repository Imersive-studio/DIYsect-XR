using DIYsect.ClassmateControls;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the dock along the bottom of the classmate's screen: one floating glass pill that holds the phone's actions
    /// (Unmute, Raise hand, and Draw and Clear when the sketch is added), centred above the bottom edge and clear of the view
    /// buttons on the right and the status chip on the left. Short notes (the microphone message) float just above it.
    /// Called by Build Shared Scene. Safe to run more than once.
    /// </summary>
    public static class AddClassmateBottomBar
    {
        public const string CanvasName = "BottomBarCanvas";
        public const string BarName = "Bar";
        public const string DockName = "Dock";
        public const float ButtonWidth = 176f;
        public const float DockPadding = 8f;
        public const float DockBottom = 20f;
        /// <summary>Where notes above the dock start, in reference units from the bottom edge.</summary>
        public const float AboveDock = DockBottom + ClassmateUiBuilder.ButtonHeight + 2f * DockPadding + 14f;
        /// <summary>Where cards (organ name, quiz line, connection banner) sit: above the notes, in the thumb's half of the screen and clear of the view pills.</summary>
        public const float CardLine = AboveDock + 104f;

        /// <summary>Makes the canvas, the bar and the dock if they are missing. Returns the dock so buttons can join it.</summary>
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
                ClassmateUiBuilder.Stretch((RectTransform)barObject.transform, Vector2.zero, Vector2.one);
                bar = barObject.transform;
            }
            Transform dock = bar.Find(DockName);
            if (dock == null)
            {
                Image glass = ClassmateUiBuilder.AddPanel(bar, DockName, ClassmateUiBuilder.Glass, ClassmateUiBuilder.ButtonHeight * 0.5f + DockPadding);
                glass.raycastTarget = true;
                var rect = glass.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, DockBottom);
                var layout = glass.gameObject.AddComponent<HorizontalLayoutGroup>();
                int pad = Mathf.RoundToInt(DockPadding);
                layout.padding = new RectOffset(pad, pad, pad, pad);
                layout.spacing = DockPadding;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
                var fitter = glass.gameObject.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                dock = glass.transform;
            }
            var barBehaviour = rig.GetComponent<ClassmateBottomBar>() ?? rig.AddComponent<ClassmateBottomBar>();
            var serialized = new SerializedObject(barBehaviour);
            serialized.FindProperty("_bar").objectReferenceValue = bar.gameObject;
            serialized.FindProperty("_network").objectReferenceValue = Object.FindFirstObjectByType<Unity.Netcode.NetworkManager>(FindObjectsInactive.Include);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return dock;
        }

        public static void AddMic(GameObject rig)
        {
            Transform dock = EnsureBar(rig);
            var mic = rig.GetComponent<ClassmateMicButton>();
            if (ClassmateUiBuilder.NeedsUi(mic, "_button"))
            {
                mic = mic != null ? mic : rig.AddComponent<ClassmateMicButton>();
                Button button = AddBarButton(dock, "MuteButton", MicButtonModel.UnmuteLabel, out TextMeshProUGUI label);
                // A note floating just above the dock, centred, whatever the screen's shape.
                TextMeshProUGUI message = ClassmateUiBuilder.AddChip(dock.parent, "MicMessage", new Vector2(0.5f, 0f), new Vector2(0f, AboveDock + 52f), ClassmateUiBuilder.SmallSize, ClassmateUiBuilder.Warning, hidden: true);
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
            Transform dock = EnsureBar(rig);
            var hand = rig.GetComponent<ClassmateHandButton>();
            if (ClassmateUiBuilder.NeedsUi(hand, "_button"))
            {
                hand = hand != null ? hand : rig.AddComponent<ClassmateHandButton>();
                Button button = AddBarButton(dock, "RaiseHandButton", HandButtonLabels.Raise, out TextMeshProUGUI label);
                var created = new SerializedObject(hand);
                created.FindProperty("_button").objectReferenceValue = button;
                created.FindProperty("_label").objectReferenceValue = label;
                created.ApplyModifiedPropertiesWithoutUndo();
            }
            var serialized = new SerializedObject(hand);
            serialized.FindProperty("_hands").objectReferenceValue = hands;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A pill in the dock, in the order it is added. <paramref name="kind"/> picks mint for the main action.</summary>
        public static Button AddBarButton(Transform dock, string name, string text, out TextMeshProUGUI label, ClassmateUiBuilder.Kind kind = ClassmateUiBuilder.Kind.Quiet)
        {
            Button button = ClassmateUiBuilder.AddButton(dock, name, text, 22f, kind, out label);
            var size = button.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = ButtonWidth;
            size.preferredHeight = ClassmateUiBuilder.ButtonHeight;
            return button;
        }
    }
}
