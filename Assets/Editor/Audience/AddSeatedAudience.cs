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
        public const int FigureCount = 40;
        public const int TriangleBudget = 200;
        // Choose from the seats nearest the table, so the crowd reads as one class from where the student stands.
        public const int NearestSeatPool = 55;
        // The cutouts run from just below the hips to above the head.
        public const float BelowSeat = 0.04f;
        public const float WomanHeight = 0.94f;
        public const float ManHeight = 1.03f;
        public const float CutoutAspect = 0.625f;
        public const string CutoutFolder = "Assets/Models/Audience";
        private const string MaterialFolder = "Assets/Materials/Audience";
        private const int Seed = 159;

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
            if (Populate(scene, LoadMaterials()) && !EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        public static bool Populate(Scene scene, Material[] materials)
        {
            if (scene.path != ScenePath)
                throw new ArgumentException($"Open {ScenePath} before placing the audience.", nameof(scene));
            if (materials == null || materials.Length == 0)
                throw new ArgumentException($"No cutouts found in {CutoutFolder}.", nameof(materials));

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

            available = available.OrderBy(slot => Vector3.ProjectOnPlane(body.position - slot.seat, Vector3.up).sqrMagnitude)
                .ThenBy(slot => slot.seat.x).ThenBy(slot => slot.seat.z).Take(NearestSeatPool).ToList();
            int[] chosen = AudienceRules.ChooseSeats(available.Count, FigureCount, Seed);
            var layout = new StringBuilder("2|");
            layout.Append(string.Join(",", materials.Select(item => item.name)));
            foreach (int index in chosen)
            {
                var slot = available[index];
                layout.AppendFormat(CultureInfo.InvariantCulture, "|{0:R},{1:R},{2:R},{3:R},{4:R},{5:R}",
                    slot.seat.x, slot.seat.y, slot.seat.z, slot.forward.x, slot.forward.z, slot.floorHeight);
            }

            string key = Hash128.Compute(layout.ToString()).ToString();
            Transform existing = root.Find(CrowdName);
            if (existing != null && existing.TryGetComponent(out SeatedAudience current) &&
                current.Matches(key, student.transform, FigureCount))
                return false;

            var crowd = new GameObject(CrowdName);
            SceneManager.MoveGameObjectToScene(crowd, scene);
            crowd.transform.SetParent(root, false);
            try
            {
                var figures = new Transform[FigureCount];
                for (int i = 0; i < chosen.Length; i++)
                {
                    var slot = available[chosen[i]];
                    figures[i] = MakeFigure(crowd.transform, i, slot.seat, slot.forward, materials);
                }

                long triangles = CountTriangles(crowd);
                if (triangles > TriangleBudget)
                    throw new InvalidOperationException($"Audience has {triangles} triangles. Budget is {TriangleBudget}.");

                crowd.AddComponent<SeatedAudience>().Configure(student.transform, figures, key);
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

        private static Transform MakeFigure(Transform parent, int index, Vector3 seat, Vector3 forward, Material[] materials)
        {
            var (image, mirrored) = AudienceRules.ChooseCutout(index, materials.Length);
            Material material = materials[image];
            var figure = new GameObject($"Student {index + 1:00}").transform;
            SceneManager.MoveGameObjectToScene(figure.gameObject, parent.gameObject.scene);
            figure.SetParent(parent, false);
            // A quad shows its face toward -Z, so +Z points away from the room's centre of attention.
            figure.SetPositionAndRotation(seat, Quaternion.LookRotation(-forward, Vector3.up));

            float height = material.name.StartsWith("Male", StringComparison.Ordinal) ? ManHeight : WomanHeight;
            GameObject cutout = GameObject.CreatePrimitive(PrimitiveType.Quad);
            SceneManager.MoveGameObjectToScene(cutout, parent.gameObject.scene);
            cutout.name = "Cutout";
            cutout.transform.SetParent(figure, false);
            cutout.transform.localPosition = new Vector3(0f, height * 0.5f - BelowSeat, 0f);
            cutout.transform.localScale = new Vector3((mirrored ? -1f : 1f) * height * CutoutAspect, height, 1f);
            UnityEngine.Object.DestroyImmediate(cutout.GetComponent<Collider>());

            var renderer = cutout.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return figure;
        }

        /// <summary>One alpha clipped unlit material per cutout image. The lighting is already in the image.</summary>
        public static Material[] LoadMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? throw new InvalidOperationException("The hall needs the Universal Render Pipeline Unlit shader.");
            Directory.CreateDirectory(MaterialFolder);
            var materials = new List<Material>();
            foreach (string path in Directory.GetFiles(CutoutFolder, "*.png").OrderBy(item => item, StringComparer.Ordinal))
            {
                string assetPath = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer.maxTextureSize != 1024 || !importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.alphaIsTransparency = true;
                    importer.maxTextureSize = 1024;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.mipmapEnabled = true;
                    importer.SaveAndReimport();
                }

                string name = Path.GetFileNameWithoutExtension(assetPath);
                string materialPath = $"{MaterialFolder}/{name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = name };
                    material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath));
                    material.SetFloat("_AlphaClip", 1f);
                    material.SetFloat("_Cutoff", 0.5f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    // Half the seats mirror the image with a negative scale, which flips the quad's winding.
                    material.SetFloat("_Cull", 0f);
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                materials.Add(material);
            }

            return materials.ToArray();
        }
    }
}
