// Run Tools > DIYsect > Add Role Auto Connect after Create Scenes.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Makes the headset roles connect on their own. The professor's headset creates the class's room as soon as it signs
    /// in (<see cref="ProfessorAutoHost"/>) and the student's headset joins it (<see cref="ClassmateAutoJoin"/>, which only
    /// joins and never creates), so neither needs the VR menu. The template's welcome panel is switched off in both scenes.
    /// Safe to run more than once.
    /// </summary>
    public static class AddRoleAutoConnect
    {
        public const string ProfessorScenePath = "Assets/Scenes/Professor_MR.unity";
        public const string StudentScenePath = "Assets/Scenes/LectureHall_VR.unity";

        [MenuItem("Tools/DIYsect/Add Role Auto Connect")]
        public static void Apply()
        {
            AddTo<ProfessorAutoHost>(ProfessorScenePath);
            AddTo<ClassmateAutoJoin>(StudentScenePath);
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddRoleAutoConnect.ApplyBatch
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

        private static void AddTo<T>(string scenePath) where T : Component
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            XRINetworkGameManager manager = UnityEngine.Object.FindFirstObjectByType<XRINetworkGameManager>(FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException($"{scenePath} has no XRINetworkGameManager.");
            if (manager.GetComponent<T>() == null)
                manager.gameObject.AddComponent<T>();
            // The template's welcome panel asks for a press on Continue before anything else; the headset roles start without it.
            int hidden = AddClassmateAutoJoin.HideWelcomePanel(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AddRoleAutoConnect] {typeof(T).Name} is on {manager.name} in {scenePath}; {hidden} welcome panel(s) switched off.");
        }
    }
}
