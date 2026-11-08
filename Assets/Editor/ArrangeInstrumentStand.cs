// Run Tools > DIYsect > Arrange Instrument Stand after the scalpel and forceps are placed (Apply Realistic Look runs it).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// The drapes cover the table, so the instruments move to their own stand beside it, as in an operating room.
    /// Builds Assets/Models/Environment/InstrumentStand.prefab from primitives (once), places one next to the table
    /// on the student's side near the foot end, puts the instrument tray on it, and moves the scalpel and forceps
    /// with the tray, keeping each tool's pose relative to the tray. Tools keep their parents (the scalpel stays
    /// under the body, so its shared pose stays body-relative). Safe to run more than once.
    /// </summary>
    public static class ArrangeInstrumentStand
    {
        private const string RootName = "DIYsect";
        private const string StandPath = "Assets/Models/Environment/InstrumentStand.prefab";
        private const string SteelPath = "Assets/Models/Environment/Steel.mat";
        private const float StandTopAboveTable = 0.1f;       // the stand top sits a little above the patient, like a Mayo stand
        // From the table centre, in the table's frame: toward the foot end and out past the student's edge.
        private static readonly Vector3 StandOffset = new Vector3(0f, 0f, 0.55f);
        private const float StandOutFromEdge = 0.3f;
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Arrange Instrument Stand")]
        public static void Apply()
        {
            GameObject standPrefab = LoadOrCreateStand();
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Transform root = FindRoot(scene);
                Transform table = FindChild(root, "DissectionTable");
                Transform tray = FindChild(root, "InstrumentTray");
                Bounds top = FindRenderer(table, "Top").bounds;

                // Remember each tool relative to the tray before the tray moves.
                Transform[] tools = { FindAnywhere(root, "ScalpelPlaceholder"), FindAnywhere(root, "ForcepsPlaceholder") };
                var relative = new Matrix4x4[tools.Length];
                for (int i = 0; i < tools.Length; i++)
                    relative[i] = tray.worldToLocalMatrix * tools[i].localToWorldMatrix;

                Transform stand = root.Find("InstrumentStand") ?? Place(standPrefab, root);
                // The student stands at the rig origin in front of the table, so the stand goes on that side.
                Vector3 towardStudent = Vector3.ProjectOnPlane(-table.position, Vector3.up).normalized;
                float halfDepth = Mathf.Min(top.extents.x, top.extents.z);
                Vector3 footward = table.TransformDirection(StandOffset);
                Vector3 position = new Vector3(top.center.x, root.position.y, top.center.z) + footward + towardStudent * (halfDepth + StandOutFromEdge);
                stand.SetPositionAndRotation(position, table.rotation);
                float standHeight = top.max.y + StandTopAboveTable - root.position.y;
                stand.localScale = new Vector3(1f, standHeight, 1f);

                Bounds surface = FindRenderer(tray, "Surface").bounds;
                tray.SetPositionAndRotation(new Vector3(position.x, top.max.y + StandTopAboveTable + surface.extents.y, position.z), tray.rotation);
                for (int i = 0; i < tools.Length; i++)
                {
                    Matrix4x4 world = tray.localToWorldMatrix * relative[i];
                    tools[i].SetPositionAndRotation(world.GetColumn(3), world.rotation);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException($"Could not save {path}.");
                Debug.Log($"{path}: instrument stand at {position}, tray top {top.max.y + StandTopAboveTable:F2} m.");
            }
        }

        // A one-metre-tall unit stand; the scene scales it on Y to the measured height.
        private static GameObject LoadOrCreateStand()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(StandPath);
            if (existing != null)
                return existing;
            var steel = AssetDatabase.LoadAssetAtPath<Material>(SteelPath);
            if (steel == null)
                throw new FileNotFoundException($"{SteelPath} is missing. Run Tools > DIYsect > Create Classroom Props.");
            var root = new GameObject("InstrumentStand");
            try
            {
                AddPart(root.transform, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.01f, 0f), new Vector3(0.45f, 0.01f, 0.45f), steel);
                AddPart(root.transform, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.5f, 0f), new Vector3(0.04f, 0.49f, 0.04f), steel);
                AddPart(root.transform, PrimitiveType.Cube, "Shelf", new Vector3(0f, 0.99f, 0f), new Vector3(0.36f, 0.02f, 0.5f), steel);
                ToolPhysicsSettings.SetEnvironmentLayer(root); // solid colliders on the Environment layer
                PrefabUtility.SaveAsPrefabAsset(root, StandPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(StandPath);
        }

        private static void AddPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Transform Place(GameObject prefab, Transform root)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            instance.name = "InstrumentStand";
            return instance.transform;
        }

        private static Transform FindRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    return candidate.transform;
            throw new InvalidOperationException($"{scene.path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
        }

        private static Transform FindChild(Transform root, string prefix)
        {
            foreach (Transform child in root)
                if (child.name.StartsWith(prefix))
                    return child;
            throw new InvalidOperationException($"{root.gameObject.scene.path} has no {prefix} under {RootName}.");
        }

        private static Transform FindAnywhere(Transform root, string prefix)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name.StartsWith(prefix))
                    return candidate;
            throw new InvalidOperationException($"{root.gameObject.scene.path} has no {prefix}. Run the placeholder instrument steps.");
        }

        private static Renderer FindRenderer(Transform root, string childName)
        {
            foreach (Renderer candidate in root.GetComponentsInChildren<Renderer>(true))
                if (candidate.name == childName)
                    return candidate;
            throw new InvalidOperationException($"{root.name} has no renderer named {childName}.");
        }
    }
}
