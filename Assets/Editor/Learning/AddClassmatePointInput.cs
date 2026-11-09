using System;
using DIYsect.Classroom;
using DIYsect.Learning;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives the classmate camera "look here": a tap on an organ asks the server to show it to everyone (ClassmatePointInput).
    /// Called by Build Shared Scene, so run Add Organ Pointer first (it puts the pop-out on the body prefab) and then
    /// Build Shared Scene (or its UpgradeRolesBatch). The camera object is only active for a classmate, so no role list needs it.
    /// A running quiz round claims the tap, as it does for the name card. Safe to run more than once.
    /// </summary>
    public static class AddClassmatePointInput
    {
        public static void AddTo(GameObject rig, SharedBody body, PointerBoard board)
        {
            if (board == null)
                throw new InvalidOperationException("The body has no PointerBoard. Run Tools > DIYsect > Add Organ Pointer.");
            var tracker = rig.GetComponent<PointerGestureTracker>()
                ?? throw new InvalidOperationException("The classmate camera has no PointerGestureTracker. Run Tools > DIYsect > Add Classmate Desktop Camera.");

            var input = rig.GetComponent<ClassmatePointInput>();
            if (input == null)
                input = rig.AddComponent<ClassmatePointInput>();
            var serialized = new SerializedObject(input);
            serialized.FindProperty("_body").objectReferenceValue = body;
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_gestures").objectReferenceValue = tracker;
            serialized.FindProperty("_camera").objectReferenceValue = rig.GetComponent<Camera>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetClaimers(rig);
            EditorUtility.SetDirty(input);
        }

        /// <summary>Lists the quiz answer input, when the camera has one, as the claimer of the tap. Also called by Add Quiz, which may run after this.</summary>
        public static void SetClaimers(GameObject rig)
        {
            var input = rig.GetComponent<ClassmatePointInput>();
            var answers = rig.GetComponent<QuizAnswerInput>();
            if (input == null || answers == null)
                return;
            var serialized = new SerializedObject(input);
            SerializedProperty claimers = serialized.FindProperty("_tapClaimers");
            claimers.arraySize = 1;
            claimers.GetArrayElementAtIndex(0).objectReferenceValue = answers;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
