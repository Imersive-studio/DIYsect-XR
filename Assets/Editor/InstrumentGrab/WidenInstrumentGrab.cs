// Run Tools > DIYsect > Widen Instrument Grab Zones after Add Placeholder Scalpel and Add Placeholder Forceps.
using System;
using System.Collections.Generic;
using System.IO;
using DIYsect.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace DIYsect.Editor
{
    /// <summary>
    /// Makes the scalpel and the forceps easier to grab in a headset. Each prefab gets a child named GrabZone: a
    /// trigger sphere around the tool's visual centre that is added to the XRGrabInteractable's colliders, so a hand
    /// that hovers near the tool can grab it. The professor's depth handle gets the same zone. The toolkit's near-field
    /// check ignores trigger colliders unless told otherwise, so the near casters in the student and professor scenes
    /// are set to hit them. Safe to run more than once: an existing GrabZone is reused and never added twice.
    /// </summary>
    public static class WidenInstrumentGrab
    {
        public const float GrabZoneRadius = 0.06f;
        public const string GrabZoneName = "GrabZone";

        private const string ScalpelPath = "Assets/Prefabs/ScalpelPlaceholder.prefab";
        private static readonly string[] ScenePaths = { "Assets/Scenes/LectureHall_VR.unity", "Assets/Scenes/Professor_MR.unity" };
        private const string ForcepsPath = "Assets/Prefabs/ForcepsPlaceholder.prefab";

        [MenuItem("Tools/DIYsect/Widen Instrument Grab Zones")]
        public static void Apply()
        {
            WidenPrefab(ScalpelPath, "Run Tools > DIYsect > Add Placeholder Scalpel first.");
            WidenPrefab(ForcepsPath, "Run Tools > DIYsect > Add Placeholder Forceps first.");
            foreach (string scenePath in ScenePaths)
                WidenScene(scenePath);
        }

        // Batch entry point: -executeMethod DIYsect.Editor.WidenInstrumentGrab.ApplyBatch
        public static void ApplyBatch()
        {
            try
            {
                Apply();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        private static int OrganLayerMask()
        {
            int mask = LayerMask.GetMask("SeatedOrgan", "LooseOrgan");
            if (mask == 0)
                throw new InvalidOperationException("The SeatedOrgan and LooseOrgan layers are missing. Check ProjectSettings/TagManager.asset.");
            return mask;
        }

        private static void WidenScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int handles = 0;
            foreach (DepthControlInput input in UnityEngine.Object.FindObjectsByType<DepthControlInput>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!input.TryGetComponent(out XRGrabInteractable grab))
                    throw new InvalidOperationException($"{input.name} has a DepthControlInput but no XRGrabInteractable.");
                WidenHandle(grab);
                EditorUtility.SetDirty(grab);
                PrefabUtility.RecordPrefabInstancePropertyModifications(grab);
                handles++;
            }
            int casters = 0;
            foreach (SphereInteractionCaster caster in UnityEngine.Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                caster.physicsTriggerInteraction = QueryTriggerInteraction.Collide;
                // Organs live on their own physics layers, which the template's casters do not look at.
                caster.physicsLayerMask |= OrganLayerMask();
                EditorUtility.SetDirty(caster);
                PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
                casters++;
            }
            if (casters == 0)
                throw new InvalidOperationException($"{scenePath} has no near caster, so the grab zones could not be found. Check the rig.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {scenePath}.");
            Debug.Log($"{scenePath}: {handles} depth handle(s) widened, {casters} near caster(s) hit triggers.");
        }

        /// <summary>Gives any interactable (for example the depth handle) the same grab zone. Call it from a scene step.</summary>
        public static void WidenHandle(XRGrabInteractable grab)
        {
            if (grab == null)
                throw new ArgumentNullException(nameof(grab));
            EnsureGrabZone(grab.gameObject, grab, GrabZoneRadius);
        }

        /// <summary>
        /// Adds the GrabZone child (a trigger sphere at the visual centre) under root and lists it in the interactable's
        /// colliders. If it already exists it is reused, so calling this twice changes nothing.
        /// </summary>
        public static Collider EnsureGrabZone(GameObject root, XRGrabInteractable grab, float radius)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (grab == null)
                throw new ArgumentNullException(nameof(grab));

            Transform zone = root.transform.Find(GrabZoneName);
            if (zone == null)
            {
                zone = new GameObject(GrabZoneName).transform;
                zone.SetParent(root.transform, false);
                zone.localPosition = VisualCentre(root);
            }

            var sphere = zone.GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = zone.gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            // The radius is in metres in the world. A scaled parent (the depth handle is 0.05) would shrink it.
            Vector3 scale = zone.lossyScale;
            sphere.radius = radius / Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z), 0.0001f);

            // An empty list means the interactable collects its own colliders when it starts. Collect them now so
            // adding the zone does not replace them.
            if (grab.colliders.Count == 0)
            {
                var own = new List<Collider>();
                root.GetComponentsInChildren(own);
                foreach (Collider collider in own)
                    if (collider != sphere && !collider.isTrigger)
                        grab.colliders.Add(collider);
            }
            if (!grab.colliders.Contains(sphere))
                grab.colliders.Add(sphere);
            return sphere;
        }

        // Centre of all renderers, in the root's local space. With no renderers it falls back to the root's origin.
        private static Vector3 VisualCentre(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return Vector3.zero;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return root.transform.InverseTransformPoint(bounds.center);
        }

        private static void WidenPrefab(string path, string hint)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. {hint}");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent(out XRGrabInteractable grab))
                    throw new InvalidOperationException($"{path} has no XRGrabInteractable on its root.");
                EnsureGrabZone(root, grab, GrabZoneRadius);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Widened the grab zone of {path}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
