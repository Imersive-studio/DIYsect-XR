// Run Tools > DIYsect > Add Practice Session after Add Role Auto Connect.
using System;
using System.IO;
using DIYsect.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Lets the headset student dissect alone: adds <see cref="PracticeSession"/> to the student scene's network manager,
    /// pointed at the room-joining script so it is switched off when practice starts. Safe to run more than once.
    /// </summary>
    public static class AddPracticeSession
    {
        public const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";

        [MenuItem("Tools/DIYsect/Add Practice Session")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var manager = UnityEngine.Object.FindFirstObjectByType<XRINetworkGameManager>(FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException($"{ScenePath} has no XRINetworkGameManager.");
            var joiner = manager.GetComponent<ClassmateAutoJoin>();
            if (joiner == null)
                throw new InvalidOperationException("Run Tools > DIYsect > Add Role Auto Connect first.");

            var practice = manager.GetComponent<PracticeSession>() ?? manager.gameObject.AddComponent<PracticeSession>();
            var serialized = new SerializedObject(practice);
            SerializedProperty list = serialized.FindProperty("_disableOnStart");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = joiner;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"[AddPracticeSession] {ScenePath} starts a practice session when no room is found.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddPracticeSession.ApplyBatch
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
