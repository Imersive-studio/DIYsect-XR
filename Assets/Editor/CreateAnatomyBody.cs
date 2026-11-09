// Run Tools > DIYsect > Create Anatomy Body after the anatomy GLBs have imported.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds Assets/Prefabs/AnatomyBody.prefab from Assets/Models/Anatomy/manifest.json.
    /// Hierarchy: AnatomyBody / Supine / (Skin, Muscle, Omentum, Chest, Organs) / one instance per GLB.
    /// Every GLB keeps its imported anatomical placement (meters, uniform scale 1).
    /// The Supine pivot lays the upright body face-up along Z with its lowest point at y = 0,
    /// so placing the prefab root on the table top rests the body on the table.
    /// </summary>
    public static class CreateAnatomyBody
    {
        private const string ManifestPath = "Assets/Models/Anatomy/manifest.json";
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private static readonly Vector3 SupineEuler = new Vector3(-90f, 0f, 0f);

        // Manifest layer name -> group name. The order here is the order of the groups in the prefab.
        private static readonly string[][] LayerGroups =
        {
            new[] { "skin", "Skin" },
            new[] { "muscle", "Muscle" },
            new[] { "omentum", "Omentum" },
            new[] { "chest", "Chest" },
            new[] { "organ", "Organs" },
        };

        [Serializable]
        private class ManifestEntry
        {
            public string file;
            public string layer;
            public int triangles;
        }

        [Serializable]
        private class Manifest
        {
            public List<ManifestEntry> items;
        }

        [MenuItem("Tools/DIYsect/Create Anatomy Body")]
        public static void Create()
        {
            Build(rebuild: false);
        }

        [MenuItem("Tools/DIYsect/Rebuild Anatomy Body (overwrite prefab)")]
        public static void Rebuild()
        {
            Build(rebuild: true);
        }

        private static void Build(bool rebuild)
        {
            if (!rebuild && File.Exists(PrefabPath))
            {
                Debug.Log($"{PrefabPath} already exists, leaving it unchanged. Use Rebuild Anatomy Body to overwrite it.");
                return;
            }

            List<ManifestEntry> entries = ReadManifest();
            Directory.CreateDirectory("Assets/Prefabs");

            var root = new GameObject("AnatomyBody");
            try
            {
                var supine = new GameObject("Supine");
                supine.transform.SetParent(root.transform, false);
                supine.transform.localEulerAngles = SupineEuler;

                int manifestTriangles = AddLayerGroups(supine.transform, entries);
                Vector3 offset = MeasureRestingOffset(root, supine.transform);
                supine.transform.localPosition = offset;

                Bounds bounds = CombinedBounds(root);
                int meshTriangles = CountTriangles(root);
                Debug.Log($"Anatomy body size (m): {bounds.size.x:F3} x {bounds.size.y:F3} x {bounds.size.z:F3} (X wide, Y height lying down, Z length). " +
                          $"Supine offset: {offset}. Scale: 1.");
                Debug.Log($"Anatomy triangles: {meshTriangles} from meshes, {manifestTriangles} from manifest, {entries.Count} GLBs.");
                if (meshTriangles != manifestTriangles)
                    Debug.LogWarning($"Mesh triangle total {meshTriangles} differs from the manifest total {manifestTriangles}.");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Saved {PrefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            // A rebuilt body must stay networked, keep its incision, its question board and organ sockets, and its look.
            // The order matters: the later steps need the components of the earlier ones.
            AnatomyBodyNetworking.ApplyToPrefab();
            AnatomyBodyDissection.ApplyToPrefab();
            AnatomyBodyQuestions.ApplyToPrefab();
            AnatomyBodySockets.ApplyToPrefab();
            AnatomyBodyPhysics.ApplyToBody();
            AnatomyHoverFeedback.Apply();
            AddGuideLine.ApplyToPrefab();
            AddOrganPointer.ApplyToPrefab();
            AddHeadPose.ApplyToPrefab();
            AnatomyBodyLook.ApplyToPrefab();
        }

        private static List<ManifestEntry> ReadManifest()
        {
            if (!File.Exists(ManifestPath))
                throw new FileNotFoundException($"Anatomy manifest not found at {ManifestPath}.");
            // JsonUtility cannot read a top level array, so wrap it in an object.
            var manifest = JsonUtility.FromJson<Manifest>("{\"items\":" + File.ReadAllText(ManifestPath) + "}");
            if (manifest == null || manifest.items == null || manifest.items.Count == 0)
                throw new InvalidDataException($"{ManifestPath} has no entries.");
            manifest.items.Sort((a, b) => string.CompareOrdinal(a.file, b.file));
            return manifest.items;
        }

        private static int AddLayerGroups(Transform parent, List<ManifestEntry> entries)
        {
            var groups = new Dictionary<string, Transform>();
            foreach (string[] pair in LayerGroups)
            {
                var group = new GameObject(pair[1]);
                group.transform.SetParent(parent, false);
                groups[pair[0]] = group.transform;
            }

            int triangles = 0;
            foreach (ManifestEntry entry in entries)
            {
                if (!groups.TryGetValue(entry.layer, out Transform group))
                    throw new InvalidDataException($"Unknown layer '{entry.layer}' for {entry.file}. Add it to LayerGroups.");
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.file);
                if (model == null)
                    throw new FileNotFoundException($"Anatomy model {entry.file} is missing or did not import.");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, group);
                instance.name = Path.GetFileNameWithoutExtension(entry.file);
                triangles += entry.triangles;
            }
            return triangles;
        }

        // Returns the Supine local position that puts the lowest point at y = 0 and the body centred on x = 0, z = 0.
        private static Vector3 MeasureRestingOffset(GameObject root, Transform supine)
        {
            Bounds bounds = CombinedBounds(root);
            return supine.localPosition + new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        private static Bounds CombinedBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The anatomy body has no renderers. The GLBs may not have imported.");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static int CountTriangles(GameObject root)
        {
            int total = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
                total += MeshTriangles(filter.sharedMesh);
            foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                total += MeshTriangles(skinned.sharedMesh);
            return total;
        }

        private static int MeshTriangles(Mesh mesh)
        {
            if (mesh == null) return 0;
            long indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                indices += mesh.GetIndexCount(i);
            return (int)(indices / 3);
        }
    }
}
