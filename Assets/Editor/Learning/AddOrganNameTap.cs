using System;
using DIYsect.Classroom;
using DIYsect.Learning;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives the classmate camera the tap-to-name card: a short label with the organ's name and what it does, in the lower middle
    /// of the screen above the pin message, that fades after a few seconds. Called by Build Shared Scene. A running quiz round
    /// claims the tap, so the card is not shown then (the same quiz answer input that stops the pin is listed as a claimer).
    /// Safe to run more than once.
    /// </summary>
    public static class AddOrganNameTap
    {
        public const string CanvasName = "OrganNameCanvas";
        private const string LibraryPath = "Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset";

        public static void AddTo(GameObject rig, SharedBody body)
        {
            var library = AssetDatabase.LoadAssetAtPath<OrganInfoLibrary>(LibraryPath)
                ?? throw new InvalidOperationException($"{LibraryPath} is missing or did not import.");
            var tracker = rig.GetComponent<PointerGestureTracker>()
                ?? throw new InvalidOperationException("The classmate camera has no PointerGestureTracker. Run Tools > DIYsect > Add Classmate Desktop Camera.");

            var input = rig.GetComponent<OrganNameTapInput>();
            if (input == null)
            {
                input = rig.AddComponent<OrganNameTapInput>();
                BuildCard(rig, input);
            }
            var serialized = new SerializedObject(input);
            serialized.FindProperty("_body").objectReferenceValue = body;
            serialized.FindProperty("_gestures").objectReferenceValue = tracker;
            serialized.FindProperty("_camera").objectReferenceValue = rig.GetComponent<Camera>();
            serialized.FindProperty("_library").objectReferenceValue = library;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetClaimers(rig);
        }

        /// <summary>Lists the quiz answer input, when the camera has one, as the claimer of the tap. Also called by Add Quiz, which may run after this.</summary>
        public static void SetClaimers(GameObject rig)
        {
            var input = rig.GetComponent<OrganNameTapInput>();
            var answers = rig.GetComponent<QuizAnswerInput>();
            if (input == null || answers == null)
                return;
            var serialized = new SerializedObject(input);
            SerializedProperty claimers = serialized.FindProperty("_tapClaimers");
            claimers.arraySize = 1;
            claimers.GetArrayElementAtIndex(0).objectReferenceValue = answers;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCard(GameObject rig, OrganNameTapInput input)
        {
            Canvas canvas = ClassmateUiBuilder.NewCanvas(rig.transform, CanvasName, clickable: false);
            var cardObject = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            cardObject.transform.SetParent(canvas.transform, false);
            var group = cardObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var background = cardObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.7f);
            background.raycastTarget = false;
            var rect = (RectTransform)cardObject.transform;
            rect.anchorMin = new Vector2(0.25f, 0.36f);
            rect.anchorMax = new Vector2(0.75f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text label = ClassmateUiBuilder.AddText(cardObject.transform, "Name", string.Empty, 34, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);

            var serialized = new SerializedObject(input);
            serialized.FindProperty("_card").objectReferenceValue = group;
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
