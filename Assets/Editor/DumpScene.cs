// Run Tools > DIYsect > Dump Scenes. Writes the top of each scene's hierarchy, with components, to Logs/scene-dump.txt.
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>Lists a scene's hierarchy to a depth, with each object's active state, layer and components. For comparing the role scenes.</summary>
    public static class DumpScene
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Dump Scenes")]
        public static void Run()
        {
            var text = new StringBuilder();
            foreach (string path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                text.AppendLine($"=== {path}");
                foreach (GameObject root in scene.GetRootGameObjects())
                    Write(text, root.transform, 0, 3);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/scene-dump.txt", text.ToString());
        }

        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }

        private static void Write(StringBuilder text, Transform t, int depth, int maxDepth)
        {
            string components = string.Join(",", t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).Where(n => n != "Transform"));
            text.AppendLine($"{new string(' ', depth * 2)}{t.name}{(t.gameObject.activeSelf ? "" : " [OFF]")} L{t.gameObject.layer} {{{components}}}");
            if (depth >= maxDepth)
                return;
            for (int i = 0; i < t.childCount; i++)
                Write(text, t.GetChild(i), depth + 1, maxDepth);
        }
    }
}
