// Run Tools > DIYsect > Find Missing Scripts. Writes every scene and prefab object with a missing script to Logs/missing-scripts.txt.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// A script that is missing from a scene or prefab does nothing in a build, and logs a warning every time the object is
    /// created. Finds them in the project's scenes and prefabs, so they are fixed before a headset session.
    /// </summary>
    public static class FindMissingScripts
    {
        [MenuItem("Tools/DIYsect/Find Missing Scripts")]
        public static void Run()
        {
            List<string> found = Scan();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/missing-scripts.txt", string.Join("\n", found));
            Debug.Log($"[FindMissingScripts] {found.Count} object(s) with a missing script. See Logs/missing-scripts.txt.");
        }

        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }

        /// <summary>Every object in the project's scenes and prefabs that has a missing script, as "asset: path (count)".</summary>
        public static List<string> Scan()
        {
            var found = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Samples/") && !path.Contains("AR Starter Assets"))
                    continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Collect(root.transform, path, found);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                    Collect(root.transform, path, found);
            }
            return found;
        }

        private static void Collect(Transform root, string asset, List<string> found)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing > 0)
                    found.Add($"{asset}: {PathOf(t)} ({missing})");
            }
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
