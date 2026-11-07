// Run Tools > DIYsect > Add Placeholder Scalpel after Create Scenes.
using System;
using System.IO;
using DIYsect.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds a primitive placeholder scalpel prefab (the real model is not available yet) and places one
    /// on the instrument tray in each DIYsect scene. Safe to run more than once.
    /// </summary>
    public static class CreateScalpel
    {
        private const string PrefabPath = "Assets/Prefabs/ScalpelPlaceholder.prefab";
        private const string SteelPath = "Assets/Models/Environment/Steel.mat";
        private const string RootName = "DIYsect";
        private const float HandleHeight = 0.012f;
        private static readonly string[] _scenePaths =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Add Placeholder Scalpel")]
        public static void Apply()
        {
            GameObject prefab = BuildPrefab();
            foreach (string path in _scenePaths)
                PlaceOnTray(path, prefab);
        }

        private static GameObject BuildPrefab()
        {
            if (File.Exists(PrefabPath))
                return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            var steel = AssetDatabase.LoadAssetAtPath<Material>(SteelPath);
            if (steel == null)
                throw new FileNotFoundException($"{SteelPath} is missing. Run Tools > DIYsect > Create Classroom Props.");

            var root = new GameObject("ScalpelPlaceholder");
            try
            {
                // Handle along +Z with the blade at the front and the tip at its end.
                AddCube(root.transform, "Handle", new Vector3(0f, 0f, 0f), new Vector3(HandleHeight, HandleHeight, 0.1f), steel, keepCollider: true);
                AddCube(root.transform, "Blade", new Vector3(0f, 0f, 0.0675f), new Vector3(0.002f, 0.01f, 0.035f), steel, keepCollider: false);

                var tip = new GameObject("Tip");
                tip.transform.SetParent(root.transform, false);
                tip.transform.localPosition = new Vector3(0f, 0f, 0.085f);
                var tipCollider = tip.AddComponent<SphereCollider>();
                tipCollider.isTrigger = true;
                tipCollider.radius = 0.004f;

                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                var grab = root.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                grab.throwOnDetach = false;

                var scalpel = root.AddComponent<ScalpelTool>();
                var serialized = new SerializedObject(scalpel);
                serialized.FindProperty("_tip").objectReferenceValue = tip.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Saved {PrefabPath}");
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        private static void PlaceOnTray(string path, GameObject prefab)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            GameObject root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName) root = candidate;
            if (root == null)
                throw new InvalidOperationException($"{path} has no {RootName} root.");
            if (root.GetComponentInChildren<ScalpelTool>(true) != null)
            {
                Debug.Log($"{path} already has a scalpel, leaving it unchanged.");
                return;
            }

            Transform tray = root.transform.Find("InstrumentTray");
            if (tray == null)
            {
                foreach (Transform child in root.transform)
                    if (child.name.StartsWith("InstrumentTray")) tray = child;
            }
            if (tray == null)
                throw new InvalidOperationException($"{path} has no InstrumentTray under {RootName}.");
            Renderer surface = null;
            foreach (Renderer candidate in tray.GetComponentsInChildren<Renderer>())
                if (candidate.name == "Surface") surface = candidate;
            if (surface == null)
                throw new InvalidOperationException("The instrument tray has no Surface renderer.");

            var scalpel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            Bounds top = surface.bounds;
            // Lying on the tray top, handle along the tray's long side.
            scalpel.transform.SetPositionAndRotation(
                new Vector3(top.center.x, top.max.y + HandleHeight * 0.5f, top.center.z),
                tray.rotation * Quaternion.Euler(0f, 90f, 0f));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
            Debug.Log($"Placed the placeholder scalpel on the tray in {path}.");
        }
    }
}
