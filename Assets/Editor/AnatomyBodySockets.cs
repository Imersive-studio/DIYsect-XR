// Run Tools > DIYsect > Add Organ Sockets after Add Body Networking (and after Add Question Pins, if you use it).
using System;
using System.IO;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the organ sockets to Assets/Prefabs/AnatomyBody.prefab. Every organ gets a SnappableOrgan that records
    /// its current pose as home and the middle of its mesh. The root gets an OrganSocketKeeper (the server's judge)
    /// and an OrganGrabPresenter (labels and ghosts), plus the transparent ghost material. Safe to run more than once.
    /// </summary>
    public static class AnatomyBodySockets
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string MaterialPath = "Assets/Prefabs/SocketGhost.mat";

        [MenuItem("Tools/DIYsect/Add Organ Sockets")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        /// <summary>Adds the components to the prefab. Also called by Create Anatomy Body, so a rebuilt body keeps them.</summary>
        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");

            Material material = GetOrCreateMaterial();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var body = root.GetComponent<SharedBody>();
                if (body == null || root.GetComponent<OrganGrabAuthority>() == null)
                    throw new InvalidOperationException("The body has no SharedBody or OrganGrabAuthority. Run Tools > DIYsect > Add Body Networking first.");

                foreach (var organ in body.Organs)
                    Record(organ.transform);

                // The keeper must exist before the presenter, which requires it.
                GetOrAdd<OrganSocketKeeper>(root);
                var presenter = GetOrAdd<OrganGrabPresenter>(root);
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_ghostMaterial").objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added organ sockets to {PrefabPath} ({body.Organs.Count} organs).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Record(Transform organ)
        {
            var snappable = GetOrAdd<SnappableOrgan>(organ.gameObject);
            var serialized = new SerializedObject(snappable);
            serialized.FindProperty("_homeLocalPosition").vector3Value = organ.localPosition;
            serialized.FindProperty("_homeLocalRotation").quaternionValue = organ.localRotation;
            serialized.FindProperty("_centreOffset").vector3Value = MeshCentre(organ);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Middle of all the organ's meshes, in the organ's own space.
        private static Vector3 MeshCentre(Transform organ)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in organ.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                Matrix4x4 toOrgan = organ.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = toOrgan.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                    if (!any)
                        bounds = new Bounds(point, Vector3.zero);
                    else
                        bounds.Encapsulate(point);
                    any = true;
                }
            }
            if (!any)
                throw new InvalidOperationException($"{organ.name} has no mesh, so it has no socket centre.");
            return bounds.center;
        }

        private static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("The URP Unlit shader was not found.");
            material = new Material(shader) { name = "SocketGhost" };
            // The same settings the URP Unlit inspector applies for Surface Type: Transparent, Blend: Alpha.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", new Color(0.4f, 0.8f, 1f, 0.35f));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }
    }
}
