// Run Tools > DIYsect > List Network Objects. Writes the NetworkObjects of each scene, with their hierarchy path, to Logs/network-objects.txt.
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Lists every NetworkObject of the three role scenes by hierarchy path. The scenes can only share a body if the
    /// objects the server spawns also exist, under the same path, in the scene of every client.
    /// </summary>
    public static class ListNetworkObjects
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/List Network Objects")]
        public static void Run()
        {
            var text = new StringBuilder();
            foreach (string path in Scenes)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                NetworkObject[] objects = Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                text.AppendLine($"== {path}: {objects.Length} NetworkObjects");
                foreach (NetworkObject networkObject in objects)
                    text.AppendLine($"{PathOf(networkObject.transform)}\t{networkObject.GetComponent<NetworkBehaviour>()?.GetType().Name}");
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/network-objects.txt", text.ToString());
            Debug.Log("[ListNetworkObjects] Wrote Logs/network-objects.txt");
        }

        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }

        private static string PathOf(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
