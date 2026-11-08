// Run Tools > DIYsect > Add Organ Physics after Add Organ Sockets (Create Anatomy Body runs it).
using System;
using System.IO;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Physics and grab settings for the organs and the scalpel:
    /// <list type="bullet">
    /// <item>XRGrabInteractable keeps the original parent while held (unparentTransformOnGrab off). The grab bridges
    /// stream the local pose to the server, which is only right while the object keeps its parent.</item>
    /// <item>Organs start on the SeatedOrgan layer, and the body root gets OrganPhysics, which loosens an organ
    /// dropped away from the body so it falls and rests, on the server only.</item>
    /// <item>OrganSocketKeeper gets a forgiving capture radius (own socket nearest within 6 cm), a 60 degree turn
    /// tolerance, a short ease into the socket, and the body zone measured from the organs plus room above the open
    /// body, where a drop puts the organ back home.</item>
    /// </list>
    /// Safe to run more than once.
    /// </summary>
    public static class AnatomyBodyPhysics
    {
        private const string BodyPrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string ScalpelPrefabPath = "Assets/Prefabs/ScalpelPlaceholder.prefab";
        private const float CaptureRadius = 0.06f;
        private const float AngleToleranceDegrees = 60f;
        private const float EaseSeconds = 0.15f;
        private const float ZoneMargin = 0.03f;      // around the organs, sideways and along the body
        private const float ZoneHeadroom = 0.25f;    // above the organs, where the hands bring an organ back

        [MenuItem("Tools/DIYsect/Add Organ Physics")]
        public static void Apply()
        {
            ApplyToBody();
            ApplyToScalpel();
            AssetDatabase.SaveAssets();
        }

        public static void ApplyToBody()
        {
            int seatedLayer = RequireLayer(OrganPhysics.SeatedLayerName);
            RequireLayer(OrganPhysics.LooseLayerName);
            GameObject root = PrefabUtility.LoadPrefabContents(BodyPrefabPath);
            try
            {
                var body = root.GetComponent<SharedBody>();
                var keeper = root.GetComponent<OrganSocketKeeper>();
                if (body == null || keeper == null)
                    throw new InvalidOperationException($"{BodyPrefabPath} needs SharedBody and OrganSocketKeeper. Run Add Body Networking and Add Organ Sockets first.");
                AnatomyBodyDissection.GetOrAdd<OrganPhysics>(root);

                Bounds zone = default;
                bool first = true;
                foreach (var networkTransform in body.Organs)
                {
                    Transform organ = networkTransform.transform;
                    KeepParentWhileHeld(organ.gameObject);
                    foreach (Transform part in organ.GetComponentsInChildren<Transform>(true))
                        part.gameObject.layer = seatedLayer;
                    foreach (Renderer renderer in organ.GetComponentsInChildren<Renderer>(true))
                    {
                        Bounds local = ToRootLocal(root.transform, renderer.bounds);
                        if (first)
                        {
                            zone = local;
                            first = false;
                        }
                        else
                        {
                            zone.Encapsulate(local);
                        }
                    }
                }
                if (first)
                    throw new InvalidOperationException($"{BodyPrefabPath} has no organ renderers to measure the body zone.");
                zone.Expand(new Vector3(ZoneMargin * 2f, 0f, ZoneMargin * 2f));
                zone.max += Vector3.up * ZoneHeadroom;

                var serialized = new SerializedObject(keeper);
                serialized.FindProperty("_captureRadius").floatValue = CaptureRadius;
                serialized.FindProperty("_angleToleranceDegrees").floatValue = AngleToleranceDegrees;
                serialized.FindProperty("_easeSeconds").floatValue = EaseSeconds;
                serialized.FindProperty("_bodyZone").boundsValue = zone;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BodyPrefabPath);
                Debug.Log($"Organ physics on {body.Organs.Count} organs; body zone {zone} (body-local).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScalpel()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ScalpelPrefabPath);
            try
            {
                KeepParentWhileHeld(root);
                PrefabUtility.SaveAsPrefabAsset(root, ScalpelPrefabPath);
                Debug.Log($"{ScalpelPrefabPath}: keeps its parent while held.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void KeepParentWhileHeld(GameObject target)
        {
            var grab = target.GetComponent<XRGrabInteractable>();
            if (grab == null)
                throw new InvalidOperationException($"{target.name} has no XRGrabInteractable.");
            grab.unparentTransformOnGrab = false;
        }

        // The eight corners of a world bounds box, turned into the root's local space.
        private static Bounds ToRootLocal(Transform root, Bounds world)
        {
            Vector3 min = world.min, max = world.max;
            var local = new Bounds(root.InverseTransformPoint(min), Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                local.Encapsulate(root.InverseTransformPoint(corner));
            }
            return local;
        }

        private static int RequireLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                throw new InvalidOperationException($"Layer {layerName} is missing from Project Settings > Tags and Layers.");
            return layer;
        }
    }
}
