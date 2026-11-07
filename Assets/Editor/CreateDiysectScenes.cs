// Run Tools > DIYsect > Create Scenes after Create Classroom Props and Create Anatomy Body.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

namespace DIYsect.Editor
{
    /// <summary>
    /// Copies the template SampleScene three times and adds a "DIYsect" root with the dissection table,
    /// instrument tray, close-up screen, anatomy body and an overhead close-up camera.
    /// The template rig, network manager and table logic are left untouched.
    /// </summary>
    public static class CreateDiysectScenes
    {
        private enum SceneMode { LectureHallVR, ProfessorMR, ClassmateDesktop }

        private const string TemplateScene = "Assets/Scenes/SampleScene.unity";
        private const string RootName = "DIYsect";
        private const string ScreenLayerName = "CloseUpScreen";
        private const string EnvironmentFolder = "Assets/Models/Environment";
        private const string BodyPrefab = "Assets/Prefabs/AnatomyBody.prefab";
        private const string SimulatorPrefab = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        private const string RoomPlaceholderName = "RoomPlaceholder (replace with lecture hall)";

        // The table sits in front of the player rig (which is at the origin) with its long side facing the player.
        private static readonly Vector3 TableCentre = new Vector3(0f, 0f, 1.2f);
        private static readonly Quaternion TableRotation = Quaternion.Euler(0f, 90f, 0f);

        [MenuItem("Tools/DIYsect/Create Scenes")]
        public static void Create()
        {
            if (!File.Exists(TemplateScene))
                throw new FileNotFoundException($"Template scene {TemplateScene} is missing.");

            int screenLayer = EnsureLayer(ScreenLayerName);
            CreateScene("Assets/Scenes/LectureHall_VR.unity", SceneMode.LectureHallVR, screenLayer);
            CreateScene("Assets/Scenes/Professor_MR.unity", SceneMode.ProfessorMR, screenLayer);
            CreateScene("Assets/Scenes/Classmate_Desktop.unity", SceneMode.ClassmateDesktop, screenLayer);
            AssetDatabase.SaveAssets();
        }

        private static void CreateScene(string path, SceneMode mode, int screenLayer)
        {
            if (!File.Exists(path) && !AssetDatabase.CopyAsset(TemplateScene, path))
                throw new IOException($"Could not copy {TemplateScene} to {path}.");

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (FindRoot(scene) != null)
            {
                Debug.Log($"{path} already has a {RootName} root, leaving it unchanged.");
                AddToBuildSettings(path);
                return;
            }

            var root = new GameObject(RootName);
            BuildClassroom(root.transform, screenLayer);
            ApplyMode(root.transform, mode);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
            AddToBuildSettings(path);
            Debug.Log($"Created {path} ({mode}).");
        }

        private static GameObject FindRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName) return candidate;
            return null;
        }

        private static void BuildClassroom(Transform root, int screenLayer)
        {
            GameObject table = Place(Load($"{EnvironmentFolder}/DissectionTable.prefab"), root, TableCentre, TableRotation);
            Bounds top = FindRenderer(table, "Top").bounds;

            // Rest the body prefab root on the table top, centred over it, with the same heading as the table.
            Vector3 bodyPosition = new Vector3(top.center.x, top.max.y, top.center.z);
            Place(Load(BodyPrefab), root, bodyPosition, TableRotation);

            // The tray sits on the table top near the foot end, clear of the body (which is about 0.64 m long).
            GameObject tray = Place(Load($"{EnvironmentFolder}/InstrumentTray.prefab"), root, Vector3.zero, TableRotation);
            Bounds trayBounds = FindRenderer(tray, "Surface").bounds;
            Vector3 footEnd = TableRotation * new Vector3(0f, 0f, 0.7f);
            tray.transform.position = new Vector3(top.center.x + footEnd.x, top.max.y + trayBounds.extents.y, top.center.z + footEnd.z);

            // The screen faces the player on the far side of the table and lives on its own layer.
            GameObject screen = Place(Load($"{EnvironmentFolder}/CloseUpScreen.prefab"), root, new Vector3(top.center.x, 1.7f, 3.2f), Quaternion.identity);
            SetLayerRecursive(screen, screenLayer);

            AddCloseUpCamera(root, top, screenLayer);
        }

        private static void AddCloseUpCamera(Transform root, Bounds tableTop, int screenLayer)
        {
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>($"{EnvironmentFolder}/CloseUp.renderTexture");
            if (texture == null)
                throw new FileNotFoundException($"{EnvironmentFolder}/CloseUp.renderTexture is missing. Run Tools > DIYsect > Create Classroom Props.");

            var cameraObject = new GameObject("CloseUpCamera");
            cameraObject.transform.SetParent(root, false);
            // One metre above the table top, looking straight down, with the head of the body at the top of the image.
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(tableTop.center.x, tableTop.max.y + 1f, tableTop.center.z),
                Quaternion.Euler(90f, -90f, 0f));

            var closeUp = cameraObject.AddComponent<Camera>();
            closeUp.targetTexture = texture;
            closeUp.fieldOfView = 50f;
            closeUp.nearClipPlane = 0.05f;
            closeUp.clearFlags = CameraClearFlags.SolidColor;
            closeUp.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            // Everything except the screen, so the screen cannot show its own picture (no recursion).
            closeUp.cullingMask = ~(1 << screenLayer);
        }

        private static void ApplyMode(Transform root, SceneMode mode)
        {
            switch (mode)
            {
                case SceneMode.ProfessorMR:
                    // The template rig starts with passthrough on (AR Camera Background + transparent clear), so nothing changes.
                    break;
                case SceneMode.LectureHallVR:
                    DisablePassthrough();
                    AddRoomPlaceholder(root);
                    break;
                case SceneMode.ClassmateDesktop:
                    Place(Load(SimulatorPrefab), root, Vector3.zero, Quaternion.identity);
                    break;
            }
        }

        // The template switches passthrough with the AR Camera Background on the rig camera.
        // Turning it off and clearing to the skybox gives an opaque VR view.
        private static void DisablePassthrough()
        {
            var background = Object.FindFirstObjectByType<ARCameraBackground>();
            if (background == null)
                throw new System.InvalidOperationException("No ARCameraBackground found in the template scene, cannot switch passthrough off.");
            var rigCamera = background.GetComponent<Camera>();
            if (rigCamera == null)
                throw new System.InvalidOperationException("The ARCameraBackground has no Camera next to it.");

            background.enabled = false;
            rigCamera.clearFlags = CameraClearFlags.Skybox;
            EditorUtility.SetDirty(background);
            EditorUtility.SetDirty(rigCamera);
            PrefabUtility.RecordPrefabInstancePropertyModifications(background);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rigCamera);
        }

        private static void AddRoomPlaceholder(Transform root)
        {
            var room = new GameObject(RoomPlaceholderName);
            room.transform.SetParent(root, false);
            AddBox(room.transform, "Floor", new Vector3(0f, -0.05f, 1.5f), new Vector3(10f, 0.1f, 10f));
            AddBox(room.transform, "BackWall", new Vector3(0f, 1.5f, 6.55f), new Vector3(10f, 3f, 0.1f));
        }

        private static void AddBox(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
        }

        private static GameObject Load(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
                throw new FileNotFoundException($"Required prefab {path} is missing.");
            return asset;
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static Renderer FindRenderer(GameObject root, string childName)
        {
            foreach (Renderer candidate in root.GetComponentsInChildren<Renderer>())
                if (candidate.name == childName) return candidate;
            throw new System.InvalidOperationException($"{root.name} has no renderer named {childName}.");
        }

        private static void SetLayerRecursive(GameObject target, int layer)
        {
            foreach (Transform child in target.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        // Adds the layer to ProjectSettings/TagManager.asset when it is missing and returns its index.
        private static int EnsureLayer(string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            int freeSlot = -1;
            for (int i = 0; i < layers.arraySize; i++)
            {
                string existing = layers.GetArrayElementAtIndex(i).stringValue;
                if (existing == layerName) return i;
                if (i >= 8 && freeSlot < 0 && string.IsNullOrEmpty(existing)) freeSlot = i;
            }
            if (freeSlot < 0)
                throw new System.InvalidOperationException($"No free user layer to add {layerName}.");
            layers.GetArrayElementAtIndex(freeSlot).stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"Added layer {layerName} at index {freeSlot}.");
            return freeSlot;
        }

        private static void AddToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene existing in scenes)
                if (existing.path == path) return;
            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[scenes.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
