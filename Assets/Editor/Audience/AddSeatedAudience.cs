using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DIYsect.Audience;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    public static class AddSeatedAudience
    {
        public const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";
        public const string CrowdName = "SeatedAudience";
        public const int FigureCount = 25;
        public const int TriangleBudget = 40000;
        private const int Seed = 159;
        private const string MaterialPath = "Assets/Materials/Audience.mat";

        private static readonly Color32[] Shirts =
        {
            new Color32(54, 99, 133, 255), new Color32(146, 69, 68, 255),
            new Color32(69, 110, 93, 255), new Color32(176, 128, 66, 255),
            new Color32(111, 86, 135, 255), new Color32(196, 188, 163, 255),
        };

        private static readonly Color32[] Skin =
        {
            new Color32(220, 167, 130, 255), new Color32(176, 121, 86, 255),
            new Color32(120, 76, 53, 255), new Color32(232, 190, 159, 255),
        };

        [MenuItem("Tools/DIYsect/Add Seated Audience")]
        public static void Apply()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                ApplyAndSave();
        }

        public static void ApplyBatch()
        {
            try
            {
                ApplyAndSave();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ApplyAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Populate(scene, LoadMaterial()) && !EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        public static bool Populate(Scene scene, Material material)
        {
            if (scene.path != ScenePath)
                throw new ArgumentException($"Open {ScenePath} before placing the audience.", nameof(scene));
            if (material == null)
                throw new ArgumentNullException(nameof(material));

            GameObject[] roots = scene.GetRootGameObjects();
            Transform root = roots.Single(item => item.name == "DIYsect").transform;
            Transform hall = root.Find("LectureHall") ?? throw new InvalidOperationException("Run Tools > DIYsect > Add Lecture Hall first.");
            Transform body = root.Find("AnatomyBody") ?? throw new InvalidOperationException("The lecture hall has no AnatomyBody.");
            XROrigin rig = roots.SelectMany(item => item.GetComponentsInChildren<XROrigin>(true)).Single();
            Camera student = rig.Camera ?? throw new InvalidOperationException("The student rig has no camera.");
            List<Vector3> floor = HallSeatGeometry.ReadFloor(hall);
            var available = new List<(Vector3 seat, Vector3 forward, float floorHeight)>();

            foreach (Vector3 seat in HallSeatGeometry.FindSeats(hall))
            {
                Vector3 forward = Vector3.ProjectOnPlane(body.position - seat, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.5f || (body.position - seat).sqrMagnitude < 2f * 2f)
                    continue;

                Vector3 foot = seat + forward * 0.26f;
                if (HallSeatGeometry.TryFloorHeight(floor, foot, out float floorHeight) &&
                    seat.y - floorHeight >= 0.28f && seat.y - floorHeight <= 0.78f)
                    available.Add((seat, forward, floorHeight));
            }

            if (available.Count < FigureCount)
                throw new InvalidOperationException($"Found {available.Count} usable seats in the front rows. Expected at least {FigureCount}.");

            int[] chosen = AudienceRules.ChooseSeats(available.Count, FigureCount, Seed);
            var layout = new StringBuilder("1|");
            layout.Append(AssetDatabase.GetAssetPath(material));
            foreach (int index in chosen)
            {
                var slot = available[index];
                layout.AppendFormat(CultureInfo.InvariantCulture, "|{0:R},{1:R},{2:R},{3:R},{4:R},{5:R}",
                    slot.seat.x, slot.seat.y, slot.seat.z, slot.forward.x, slot.forward.z, slot.floorHeight);
            }

            string key = Hash128.Compute(layout.ToString()).ToString();
            Transform existing = root.Find(CrowdName);
            if (existing != null && existing.TryGetComponent(out SeatedAudience current) &&
                current.Matches(key, body, student.transform, FigureCount))
                return false;

            var crowd = new GameObject(CrowdName);
            SceneManager.MoveGameObjectToScene(crowd, scene);
            crowd.transform.SetParent(root, false);
            try
            {
                var random = new System.Random(Seed);
                var figures = new SeatedAudience.Figure[FigureCount];
                for (int i = 0; i < chosen.Length; i++)
                {
                    var slot = available[chosen[i]];
                    figures[i] = MakeFigure(crowd.transform, i, slot.seat, slot.forward,
                        slot.seat.y - slot.floorHeight, random, material);
                }

                long triangles = CountTriangles(crowd);
                if (triangles > TriangleBudget)
                    throw new InvalidOperationException($"Audience has {triangles} triangles. Budget is {TriangleBudget}.");

                crowd.AddComponent<SeatedAudience>().Configure(body, student.transform, figures, key);
                if (existing != null)
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                long sceneTriangles = scene.GetRootGameObjects().Sum(CountTriangles);
                Debug.Log($"Placed {FigureCount} seated classmates with {triangles:N0} triangles. Scene mesh instances total {sceneTriangles:N0} triangles.");
                return true;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(crowd);
                throw;
            }
        }

        public static long CountTriangles(GameObject root)
        {
            long total = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;
                for (int i = 0; i < mesh.subMeshCount; i++)
                    if (mesh.GetTopology(i) == MeshTopology.Triangles)
                        total += mesh.GetIndexCount(i) / 3;
            }

            return total;
        }

        private static SeatedAudience.Figure MakeFigure(Transform parent, int index, Vector3 seat, Vector3 forward,
            float legLength, System.Random random, Material material)
        {
            Transform figure = Child(parent, $"Student {index + 1:00}", Vector3.zero);
            figure.SetPositionAndRotation(seat, Quaternion.LookRotation(forward, Vector3.up));
            float height = 0.91f + random.Next(5) * 0.045f;
            Transform upper = Child(figure, "Upper Body", Vector3.zero);
            Transform head = Child(upper, "Head Pivot", new Vector3(0f, 0.47f * height, 0f));

            Renderer torso = Part(upper, "Torso", new Vector3(0f, 0.21f * height, -0.02f),
                new Vector3(0.32f, 0.41f * height, 0.22f), material);
            var shirt = new List<Renderer> { torso };
            var trousers = new List<Renderer>();
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.095f;
                shirt.Add(Part(upper, side < 0 ? "Left Sleeve" : "Right Sleeve",
                    new Vector3(side * 0.18f, 0.24f * height, 0.04f),
                    new Vector3(0.105f, 0.23f * height, 0.12f), material));
                trousers.Add(Part(figure, side < 0 ? "Left Thigh" : "Right Thigh",
                    new Vector3(x, 0.06f, 0.105f), new Vector3(0.15f, 0.12f, 0.32f), material));
                trousers.Add(Part(figure, side < 0 ? "Left Leg" : "Right Leg",
                    new Vector3(x, -legLength * 0.5f + 0.025f, 0.25f),
                    new Vector3(0.12f, legLength + 0.04f, 0.12f), material));
                trousers.Add(Part(figure, side < 0 ? "Left Shoe" : "Right Shoe",
                    new Vector3(x, -legLength + 0.04f, 0.28f), new Vector3(0.13f, 0.08f, 0.20f), material));
            }

            Renderer face = Part(head, "Face", new Vector3(0f, 0.07f, 0.01f), new Vector3(0.19f, 0.22f, 0.18f), material, PrimitiveType.Sphere);
            Renderer nose = Part(head, "Nose", new Vector3(0f, 0.06f, 0.104f), new Vector3(0.035f, 0.043f, 0.035f), material);
            Renderer hair = Part(head, "Hair", new Vector3(0f, 0.143f, -0.01f), new Vector3(0.16f, 0.065f, 0.145f), material);
            Renderer leftEye = Part(head, "Left Eye", new Vector3(-0.038f, 0.089f, 0.09f), new Vector3(0.017f, 0.017f, 0.014f), material);
            Renderer rightEye = Part(head, "Right Eye", new Vector3(0.038f, 0.089f, 0.09f), new Vector3(0.017f, 0.017f, 0.014f), material);

            return new SeatedAudience.Figure
            {
                upperBody = upper,
                head = head,
                shirt = shirt.ToArray(),
                trousers = trousers.ToArray(),
                face = new[] { face, nose },
                hair = new[] { hair, leftEye, rightEye },
                shirtColor = Shirts[random.Next(Shirts.Length)],
                skinColor = Skin[random.Next(Skin.Length)],
                hairColor = new Color32((byte)random.Next(30, 79), 42, 35, 255),
            };
        }

        private static Transform Child(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, parent.gameObject.scene);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            return child.transform;
        }

        private static Renderer Part(Transform parent, string name, Vector3 position, Vector3 scale,
            Material material, PrimitiveType shape = PrimitiveType.Cube)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            SceneManager.MoveGameObjectToScene(part, parent.gameObject.scene);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());

            var renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Material LoadMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The hall needs the Universal Render Pipeline Lit shader.");
            material = new Material(shader) { name = "Audience" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
