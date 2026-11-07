// Run Tools > DIYsect > Add Placeholder Forceps after Add Placeholder Scalpel (it needs the scenes and the tray).
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
    /// Builds a primitive placeholder forceps prefab (the real model is not available yet) and places one beside the
    /// scalpel on the instrument tray in each DIYsect scene. Two thin arms meet at a trigger tip. The forceps is not
    /// shared over the network yet: each client's copy only sends requests. Safe to run more than once.
    /// </summary>
    public static class CreateForceps
    {
        private const string PrefabPath = "Assets/Prefabs/ForcepsPlaceholder.prefab";
        private const string SteelPath = "Assets/Models/Environment/Steel.mat";
        private const string RootName = "DIYsect";
        private const float ArmThickness = 0.006f;
        private static readonly string[] _scenePaths =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Add Placeholder Forceps")]
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

            var root = new GameObject("ForcepsPlaceholder");
            try
            {
                // Arms along +Z, closing at the tip at the front.
                AddArm(root.transform, "ArmLeft", -0.004f, steel, keepCollider: true);
                AddArm(root.transform, "ArmRight", 0.004f, steel, keepCollider: false);

                var tip = new GameObject("Tip");
                tip.transform.SetParent(root.transform, false);
                tip.transform.localPosition = new Vector3(0f, 0f, 0.075f);
                var tipCollider = tip.AddComponent<SphereCollider>();
                tipCollider.isTrigger = true;
                tipCollider.radius = 0.004f;

                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;

                var grab = root.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                grab.throwOnDetach = false;

                var forceps = root.AddComponent<ForcepsTool>();
                var serialized = new SerializedObject(forceps);
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

        private static void AddArm(Transform parent, string name, float x, Material material, bool keepCollider)
        {
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = name;
            arm.transform.SetParent(parent, false);
            arm.transform.localPosition = new Vector3(x, 0f, 0f);
            arm.transform.localScale = new Vector3(ArmThickness * 0.5f, ArmThickness, 0.15f);
            arm.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
                UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>());
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
            if (root.GetComponentInChildren<ForcepsTool>(true) != null)
            {
                Debug.Log($"{path} already has forceps, leaving them unchanged.");
                return;
            }

            ScalpelTool scalpel = root.GetComponentInChildren<ScalpelTool>(true);
            if (scalpel == null)
                throw new InvalidOperationException($"{path} has no scalpel. Run Tools > DIYsect > Add Placeholder Scalpel.");

            // Beside the scalpel on the tray, same orientation, in the tray's own frame (the scalpel lives under the body).
            var forceps = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
            Transform tray = null;
            foreach (Transform child in root.transform)
                if (child.name.StartsWith("InstrumentTray")) tray = child;
            Vector3 side = tray != null ? tray.forward : Vector3.forward;
            forceps.transform.SetPositionAndRotation(scalpel.transform.position + side * 0.08f, scalpel.transform.rotation);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
            Debug.Log($"Placed the placeholder forceps on the tray in {path}.");
        }
    }
}
