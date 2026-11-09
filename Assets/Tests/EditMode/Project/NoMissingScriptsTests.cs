using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Project.Tests
{
    /// <summary>
    /// A script missing from a prefab or a scene does nothing in a build and logs a warning every time the object is created.
    /// The room-scan plane prefab lost its feathered visualizer and its material that way: the headset logged 49 warnings and
    /// the planes could draw as solid pink. These tests keep that from coming back.
    /// </summary>
    public class NoMissingScriptsTests
    {
        [Test]
        public void NoPrefabInTheProjectHasAMissingScript()
        {
            var found = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Other samples are not built; the AR Starter Assets sample is, through the room-scan plane prefab.
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
            Assert.IsEmpty(found, "Objects with a missing script:\n" + string.Join("\n", found));
        }

        [Test]
        public void NoScenesObjectHasAMissingScript()
        {
            var found = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                    Collect(root.transform, path, found);
            }
            Assert.IsEmpty(found, "Objects with a missing script:\n" + string.Join("\n", found));
        }

        [Test]
        public void TheRoomScanPlanePrefabHasItsVisualizerAndItsMaterial()
        {
            GameObject root = PrefabUtility.LoadPrefabContents("Assets/MRTabletopAssets/Prefabs/AR/ARPlane.prefab");
            try
            {
                var renderer = root.GetComponentInChildren<MeshRenderer>(true);
                Assert.IsNotNull(renderer, "The plane prefab has no mesh renderer.");
                foreach (Material material in renderer.sharedMaterials)
                    Assert.IsNotNull(material, "A plane material is missing, which draws as solid pink in a build.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Collect(Transform root, string asset, List<string> found)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                if (missing > 0)
                    found.Add($"{asset}: {t.name} ({missing})");
            }
        }
    }
}
