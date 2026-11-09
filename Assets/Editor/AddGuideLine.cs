// Run Tools > DIYsect > Add Guide Line after Create Scenes and Arrange Instrument Stand (Create Anatomy Body runs the prefab part).
using System;
using System.IO;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the professor's guide line. In the body prefab: a GuideLine on the root (it rides the body's
    /// NetworkObject) and a GuideLine child with an identity pose, a LineRenderer and a GuideLinePresenter.
    /// In Professor_MR: a marker pen (primitives, kinematic grab that stays where it is released) on the
    /// instrument tray, wired to the scene body's GuideLine. Safe to run more than once.
    /// </summary>
    public static class AddGuideLine
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string PenPath = "Assets/Prefabs/GuidePen.prefab";
        private const string LineMaterialPath = "Assets/Prefabs/GuideLine.mat";
        private const string PenMaterialPath = "Assets/Prefabs/GuidePen.mat";
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string ChildName = "GuideLine";
        private const string PenName = "GuidePen";
        private const float LineWidth = 0.004f;
        private static readonly Color LineColor = new Color(0.1f, 0.95f, 0.45f);
        private static readonly Color PenColor = new Color(0.15f, 0.35f, 0.85f);

        [MenuItem("Tools/DIYsect/Add Guide Line")]
        public static void Apply()
        {
            ApplyToPrefab();
            PlacePen();
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var line = AnatomyBodyDissection.GetOrAdd<GuideLine>(root);
                Transform child = root.transform.Find(ChildName);
                if (child == null)
                {
                    child = new GameObject(ChildName).transform;
                    child.SetParent(root.transform, false);
                }
                child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                child.localScale = Vector3.one;
                var renderer = AnatomyBodyDissection.GetOrAdd<LineRenderer>(child.gameObject);
                renderer.useWorldSpace = false;
                renderer.widthMultiplier = LineWidth;
                renderer.numCapVertices = 3;
                renderer.numCornerVertices = 2;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = LoadOrCreate(LineMaterialPath, "Universal Render Pipeline/Unlit", LineColor);
                renderer.positionCount = 0;
                renderer.enabled = false;
                var presenter = AnatomyBodyDissection.GetOrAdd<GuideLinePresenter>(child.gameObject);
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_line").objectReferenceValue = line;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the guide line to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void PlacePen()
        {
            GameObject penPrefab = LoadOrCreatePen();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    root = candidate.transform;
            if (root == null)
                throw new InvalidOperationException($"{ScenePath} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
            // The body's own GuideLine (on its root), never the classmate sketch line on a child.
            SharedBody body = root.GetComponentInChildren<SharedBody>(true);
            GuideLine line = body != null ? body.GetComponent<GuideLine>() : null;
            if (line == null)
                throw new InvalidOperationException($"{ScenePath} has no body with a GuideLine. Run the prefab step first.");
            Transform tray = null;
            foreach (Transform child in root)
                if (child.name.StartsWith("InstrumentTray"))
                    tray = child;
            if (tray == null)
                throw new InvalidOperationException($"{ScenePath} has no InstrumentTray. Run Tools > DIYsect > Arrange Instrument Stand.");

            Transform pen = root.Find(PenName);
            if (pen == null)
            {
                pen = ((GameObject)PrefabUtility.InstantiatePrefab(penPrefab, root)).transform;
                pen.name = PenName;
            }
            // On the tray, beside the other instruments, lying along the tray's long side.
            Bounds surface = tray.GetComponentInChildren<Renderer>().bounds;
            pen.SetPositionAndRotation(surface.center + tray.right * -0.12f + Vector3.up * (surface.extents.y + 0.012f), tray.rotation);
            var serialized = new SerializedObject(pen.GetComponent<GuideLinePen>());
            serialized.FindProperty("_line").objectReferenceValue = line;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: guide pen on the instrument tray.");
        }

        // A 14 cm marker along +Z with its tip at the front.
        private static GameObject LoadOrCreatePen()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PenPath);
            if (existing != null)
                return existing;
            Material material = LoadOrCreate(PenMaterialPath, "Universal Render Pipeline/Lit", PenColor);
            var root = new GameObject(PenName);
            try
            {
                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                barrel.name = "Barrel";
                barrel.transform.SetParent(root.transform, false);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                barrel.transform.localScale = new Vector3(0.014f, 0.07f, 0.014f);
                barrel.GetComponent<Renderer>().sharedMaterial = material;
                var tip = new GameObject("Tip").transform;
                tip.SetParent(root.transform, false);
                tip.localPosition = new Vector3(0f, 0f, 0.072f);

                var body = root.AddComponent<Rigidbody>();
                var grab = root.AddComponent<XRGrabInteractable>();
                // Gravity, mass, damping and throw values live in one shared place.
                ToolPhysicsSettings.Apply(body, grab);
                // The Barrel capsule keeps its solid collider; root and barrel go on the Default layer.
                ToolPhysicsSettings.SetToolLayer(root);
                var pen = root.AddComponent<GuideLinePen>();
                var serialized = new SerializedObject(pen);
                serialized.FindProperty("_tip").objectReferenceValue = tip;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PenPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(PenPath);
        }

        internal static Material LoadOrCreate(string path, string shaderName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"{shaderName} shader not found.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
