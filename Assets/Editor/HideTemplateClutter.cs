// Run Tools > DIYsect > Hide Template Clutter. Build Shared Scene also runs it.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// The mixed reality template ships a tabletop game: a lobby menu with seats and a Games button that sits at the
    /// professor's feet, and a grid slab under its virtual table. DIYsect joins rooms on its own and lays the body on the
    /// real table, so both only clutter the professor's view. They stay in the scene, switched off. Safe to run more than once.
    /// </summary>
    public static class HideTemplateClutter
    {
        public const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        public const string LobbyMenuPath = "UI/World Space Canvas";
        public const string GridPlanePath = "Virtual Table/TableSystem/TableTop/Plane";

        [MenuItem("Tools/DIYsect/Hide Template Clutter")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            HideIn(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        /// <summary>Switches the clutter off in an open scene without saving it.</summary>
        public static void HideIn(Scene scene)
        {
            Transform menu = Find(scene, LobbyMenuPath);
            if (menu != null)
                menu.gameObject.SetActive(false);
            Transform grid = Find(scene, GridPlanePath);
            if (grid != null && grid.TryGetComponent(out Renderer renderer))
            {
                renderer.enabled = false;
                EditorUtility.SetDirty(renderer);
            }
            Debug.Log($"[HideTemplateClutter] {scene.path}: lobby menu {(menu != null ? "off" : "not found")}, grid slab {(grid != null ? "off" : "not found")}.");
        }

        // A path from a root object, for example "UI/World Space Canvas".
        private static Transform Find(Scene scene, string path)
        {
            int slash = path.IndexOf('/');
            string rootName = slash < 0 ? path : path.Substring(0, slash);
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == rootName)
                    return slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
            return null;
        }

        // Batch entry point: -executeMethod DIYsect.Editor.HideTemplateClutter.ApplyBatch
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
