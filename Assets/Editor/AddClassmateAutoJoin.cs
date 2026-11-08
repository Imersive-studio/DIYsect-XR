// Run Tools > DIYsect > Add Classmate Auto Join after Create Scenes.
using System;
using DIYsect.Classroom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Makes Classmate_Desktop usable by touch: the classmate joins the professor's lobby on its own, shows a status line
    /// saying whether they are in the room, can orbit and zoom the camera with a finger or the mouse, and the headset
    /// template's welcome panel, which a finger cannot press, is switched off. Safe to run more than once.
    /// </summary>
    public static class AddClassmateAutoJoin
    {
        public const string ScenePath = "Assets/Scenes/Classmate_Desktop.unity";
        public const string WelcomePanelPrefab = "CoachingUI.prefab";
        public const string ClassmateCameraName = "ClassmateCamera";

        [MenuItem("Tools/DIYsect/Add Classmate Auto Join")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            XRINetworkGameManager manager = UnityEngine.Object.FindFirstObjectByType<XRINetworkGameManager>(FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException($"{ScenePath} has no XRINetworkGameManager.");
            if (manager.GetComponent<ClassmateAutoJoin>() == null)
                manager.gameObject.AddComponent<ClassmateAutoJoin>();
            if (manager.GetComponent<ClassmateStatusLabel>() == null)
                manager.gameObject.AddComponent<ClassmateStatusLabel>();

            Camera classmateCamera = FindClassmateCamera();
            if (classmateCamera.GetComponent<ClassmateOrbitCamera>() == null)
                classmateCamera.gameObject.AddComponent<ClassmateOrbitCamera>();

            int hidden = HideWelcomePanel(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[AddClassmateAutoJoin] Auto join and status line are on {manager.name}; orbit camera is on {classmateCamera.name}; {hidden} welcome panel(s) switched off.");
        }

        /// <summary>Switches off every instance of the template's welcome panel in the scene. Returns how many it switched off.</summary>
        public static int HideWelcomePanel(Scene scene)
        {
            int hidden = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    // The panel is an instance of the template's CoachingUI prefab, whose root is not named after it.
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    bool isWelcomePanel = PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)
                        && source != null
                        && AssetDatabase.GetAssetPath(source).EndsWith(WelcomePanelPrefab, StringComparison.Ordinal);
                    if (isWelcomePanel && t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        hidden++;
                    }
                }
            }
            return hidden;
        }

        private static Camera FindClassmateCamera()
        {
            GameObject cameraObject = GameObject.Find(ClassmateCameraName);
            Camera camera = cameraObject != null ? cameraObject.GetComponentInChildren<Camera>(true) : null;
            if (camera == null)
                throw new InvalidOperationException($"{ScenePath} has no {ClassmateCameraName} with a Camera.");
            return camera;
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddClassmateAutoJoin.ApplyBatch
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
