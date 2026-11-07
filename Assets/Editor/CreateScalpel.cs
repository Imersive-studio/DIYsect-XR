// Run Tools > DIYsect > Add Placeholder Scalpel after Create Scenes and Add Body Dissection.
using System;
using System.IO;
using DIYsect.Interaction;
using DIYsect.Networking;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds a primitive placeholder scalpel prefab (the real model is not available yet) and places one
    /// on the instrument tray in each DIYsect scene. Then it makes the scalpel shared: the prefab gets a NetworkObject,
    /// a server-authoritative pose, the steering and guiding-hand components and the depth indicator (a ring and a
    /// label), the scalpel in each scene becomes a child of the body so its pose is shared relative to the body,
    /// and the professor scene gets a depth handle. Safe to run more than once.
    /// </summary>
    public static class CreateScalpel
    {
        private const string PrefabPath = "Assets/Prefabs/ScalpelPlaceholder.prefab";
        private const string SteelPath = "Assets/Models/Environment/Steel.mat";
        private const string RingMaterialPath = "Assets/Prefabs/GuidingHandRing.mat";
        private const string ProfessorScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string HandleName = "DepthHandle";
        private const string ModelName = "Model";
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
            AddSharedScalpel();
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

        // Makes the saved prefab shared. Every step checks before it adds, so running it again changes nothing.
        private static void AddSharedScalpel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform model = GroupIntoModel(root);
                Transform tip = model.Find("Tip");

                GetOrAdd<NetworkObject>(root);
                var pose = GetOrAdd<HoldableNetworkTransform>(root);
                pose.AuthorityMode = NetworkTransform.AuthorityModes.Server;
                pose.InLocalSpace = true;
                pose.Interpolate = true;
                // The extra smoothing pass stops short of the target. The scalpel should land exactly.
                pose.PositionLerpSmoothing = false;
                pose.RotationLerpSmoothing = false;
                pose.ScaleLerpSmoothing = false;

                GetOrAdd<ScalpelSteering>(root);
                GetOrAdd<GuidingHand>(root);
                GetOrAdd<NetworkedScalpelGrab>(root);

                var depth = GetOrAdd<DepthOverride>(root);
                var depthSerialized = new SerializedObject(depth);
                depthSerialized.FindProperty("_model").objectReferenceValue = model;
                depthSerialized.FindProperty("_tip").objectReferenceValue = tip;
                depthSerialized.ApplyModifiedPropertiesWithoutUndo();

                AddIndicator(root, model);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Made {PrefabPath} a shared scalpel.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The model holds everything the depth override slides: handle, blade, tip and indicator.
        private static Transform GroupIntoModel(GameObject root)
        {
            Transform model = root.transform.Find(ModelName);
            if (model == null)
            {
                model = new GameObject(ModelName).transform;
                model.SetParent(root.transform, false);
            }
            foreach (string childName in new[] { "Handle", "Blade", "Tip" })
            {
                Transform child = root.transform.Find(childName);
                if (child != null)
                    child.SetParent(model, true);
            }
            if (model.Find("Tip") == null)
                throw new InvalidOperationException($"{PrefabPath} has no Tip. Delete the prefab and run the step again.");
            return model;
        }

        private static void AddIndicator(GameObject root, Transform model)
        {
            Material ringMaterial = LoadOrCreateRingMaterial();

            Transform ring = model.Find("DepthRing");
            if (ring == null)
            {
                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "DepthRing";
                UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
                ring = disc.transform;
                ring.SetParent(model, false);
            }
            // A flat disc around the blade base, facing along the blade (the cylinder's axis is Y, the blade runs along Z).
            ring.localPosition = new Vector3(0f, 0f, 0.052f);
            ring.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.localScale = new Vector3(0.03f, 0.0015f, 0.03f);
            ring.GetComponent<Renderer>().sharedMaterial = ringMaterial;

            Transform label = model.Find("DepthLabel");
            if (label == null)
            {
                label = new GameObject("DepthLabel").transform;
                label.SetParent(model, false);
            }
            label.localPosition = new Vector3(0f, 0.05f, 0.03f);
            var text = GetOrAdd<TextMesh>(label.gameObject);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            text.fontSize = 100;
            text.characterSize = 0.0015f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.text = "Depth";
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            var billboard = GetOrAdd<Billboard>(label.gameObject);

            var indicator = GetOrAdd<GuidingHandIndicator>(root);
            var serialized = new SerializedObject(indicator);
            serialized.FindProperty("_ring").objectReferenceValue = ring.GetComponent<Renderer>();
            serialized.FindProperty("_label").objectReferenceValue = text;
            serialized.FindProperty("_labelBillboard").objectReferenceValue = billboard;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LoadOrCreateRingMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RingMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("The URP Unlit shader was not found.");
            material = new Material(shader) { name = "GuidingHandRing" };
            AssetDatabase.CreateAsset(material, RingMaterialPath);
            return material;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
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
            ScalpelTool existing = root.GetComponentInChildren<ScalpelTool>(true);
            if (existing != null)
            {
                Debug.Log($"{path} already has a scalpel, leaving its place unchanged.");
                ShareScalpel(root, existing.gameObject, path);
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
            ShareScalpel(root, scalpel, path);
            Debug.Log($"Placed the placeholder scalpel on the tray in {path}.");
        }

        // The scalpel becomes a child of the body so its network pose is relative to the body. The depth handle goes into the professor scene.
        private static void ShareScalpel(GameObject root, GameObject scalpel, string path)
        {
            var body = root.GetComponentInChildren<SharedBody>(true);
            if (body == null)
                throw new InvalidOperationException($"{path} has no body with a SharedBody. Run Tools > DIYsect > Add Body Networking.");
            if (scalpel.transform.parent != body.transform)
                scalpel.transform.SetParent(body.transform, true);

            if (path == ProfessorScenePath)
                AddDepthHandle(root, scalpel);

            Scene scene = scalpel.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
        }

        // A small sphere beside the scalpel. Lowering it while holding it deepens the student's cut.
        private static void AddDepthHandle(GameObject root, GameObject scalpel)
        {
            Transform handle = root.transform.Find(HandleName);
            if (handle == null)
            {
                var steel = AssetDatabase.LoadAssetAtPath<Material>(SteelPath);
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = HandleName;
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localScale = Vector3.one * 0.05f;
                sphere.GetComponent<Renderer>().sharedMaterial = steel;
                // Beside the scalpel on the tray, raised so the hand can reach it.
                sphere.transform.position = scalpel.transform.position + scalpel.transform.right * 0.12f + Vector3.up * 0.06f;

                var body = sphere.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                var grab = sphere.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Kinematic;
                grab.throwOnDetach = false;
                handle = sphere.transform;
            }

            var input = GetOrAdd<DepthControlInput>(handle.gameObject);
            var serialized = new SerializedObject(input);
            serialized.FindProperty("_hand").objectReferenceValue = scalpel.GetComponent<GuidingHand>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
