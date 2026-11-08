// Run Tools > DIYsect > Add Floor Collider after Create Scenes (Apply Realistic Look runs it).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives every DIYsect scene an invisible floor at floor level (y = 0 under the DIYsect root), on the
    /// Environment layer, so dropped organs and tools land on the floor. The lecture hall model has no colliders,
    /// and in mixed reality this is where the real floor is. Safe to run more than once.
    /// </summary>
    public static class AddFloorCollider
    {
        private const string RootName = "DIYsect";
        private const string FloorName = "FloorCollider";
        private static readonly Vector3 FloorSize = new Vector3(30f, 0.2f, 30f);
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Add Floor Collider")]
        public static void Apply()
        {
            int layer = LayerMask.NameToLayer("Environment");
            if (layer < 0)
                throw new InvalidOperationException("Layer Environment is missing from Project Settings > Tags and Layers.");
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Transform root = null;
                foreach (GameObject candidate in scene.GetRootGameObjects())
                    if (candidate.name == RootName)
                        root = candidate.transform;
                if (root == null)
                    throw new InvalidOperationException($"{path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
                Transform floor = root.Find(FloorName);
                if (floor == null)
                {
                    floor = new GameObject(FloorName).transform;
                    floor.SetParent(root, false);
                }
                floor.gameObject.layer = layer;
                floor.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                var box = AnatomyBodyDissection.GetOrAdd<BoxCollider>(floor.gameObject);
                box.isTrigger = false;
                // The top face sits exactly at floor level.
                box.center = new Vector3(0f, -FloorSize.y / 2f, 0f);
                box.size = FloorSize;
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException($"Could not save {path}.");
                Debug.Log($"{path}: invisible floor collider at floor level.");
            }
        }
    }
}
