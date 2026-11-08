// Run Tools > DIYsect > Apply Tool Physics once to update the prefabs that already exist (needs the Environment layer).
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives the hand tools (forceps and guide pen) real physics and puts the table, tray, screen and stand on the
    /// Environment layer, in the prefabs that already exist. The tools are local objects (not networked), so plain
    /// Rigidbody physics is enough: released tools fall, rest on the tray or table and can be tossed gently.
    /// Fresh builds (CreateForceps, AddGuideLine, CreateClassroomProps, ArrangeInstrumentStand) use the same helper.
    /// Safe to run more than once.
    /// </summary>
    public static class ApplyToolPhysics
    {
        private const string ForcepsPath = "Assets/Prefabs/ForcepsPlaceholder.prefab";
        private const string PenPath = "Assets/Prefabs/GuidePen.prefab";
        private static readonly string[] EnvironmentPaths =
        {
            "Assets/Models/Environment/DissectionTable.prefab",
            "Assets/Models/Environment/InstrumentTray.prefab",
            "Assets/Models/Environment/CloseUpScreen.prefab",
            "Assets/Models/Environment/InstrumentStand.prefab",
        };

        [MenuItem("Tools/DIYsect/Apply Tool Physics")]
        public static void Apply()
        {
            // Look the layer up first so a missing layer stops everything before any prefab is touched.
            ToolPhysicsSettings.GetEnvironmentLayer();
            int tools = 0;
            foreach (string path in new[] { ForcepsPath, PenPath })
                tools += EditPrefab(path, ToolPhysicsSettings.ApplyToToolPrefab);
            int environment = 0;
            foreach (string path in EnvironmentPaths)
                environment += EditPrefab(path, ToolPhysicsSettings.SetEnvironmentLayer);
            AssetDatabase.SaveAssets();
            Debug.Log($"Tool physics applied to {tools} tool prefab(s); {environment} environment prefab(s) moved to the Environment layer.");
        }

        // Opens the prefab, runs the edit, saves, and always unloads. Returns 1 if edited, 0 if the file is missing.
        private static int EditPrefab(string path, Action<GameObject> edit)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"{path} not found, skipped.");
                return 0;
            }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return 1;
        }
    }

    /// <summary>
    /// The one place that holds the tool physics values and the layer rules, so no script repeats them.
    /// </summary>
    internal static class ToolPhysicsSettings
    {
        private const string EnvironmentLayerName = "Environment";
        private const int DefaultLayer = 0;

        /// <summary>Makes the tool fall under gravity, rest on surfaces and be tossed gently when released.</summary>
        public static void Apply(Rigidbody body, XRGrabInteractable grab)
        {
            // Light, slightly damped body so it settles instead of sliding forever.
            body.isKinematic = false;
            body.useGravity = true;
            body.mass = 0.05f;
            body.linearDamping = 0.2f;
            body.angularDamping = 0.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;           // smooth motion between physics steps
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // a small fast tool will not tunnel through the table

            // Kinematic while held: the tool follows the hand and passes through the table. Physics takes over on release.
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            grab.throwOnDetach = true;
            grab.throwVelocityScale = 0.7f;
            grab.throwAngularVelocityScale = 0.5f;
            grab.forceGravityOnDetach = true;
        }

        /// <summary>For a loaded tool prefab: physics values, Default layer on the root and its solid colliders.</summary>
        public static void ApplyToToolPrefab(GameObject root)
        {
            var body = root.GetComponent<Rigidbody>();
            var grab = root.GetComponent<XRGrabInteractable>();
            if (body == null || grab == null)
                throw new InvalidOperationException($"{root.name} needs a Rigidbody and an XRGrabInteractable on its root.");
            Apply(body, grab);
            SetToolLayer(root);
        }

        /// <summary>Tool root and every solid collider go on Default. Trigger colliders (the tip) are left alone.</summary>
        public static void SetToolLayer(GameObject root)
        {
            bool hasSolid = false;
            root.layer = DefaultLayer;
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider.isTrigger)
                    continue;
                collider.gameObject.layer = DefaultLayer;
                hasSolid = true;
            }
            // Without a solid collider the tool would fall through the tray.
            if (!hasSolid)
                throw new InvalidOperationException($"{root.name} has no solid (non-trigger) collider, so it could not rest on a surface.");
        }

        /// <summary>Puts every collider of a table, tray, screen or stand on the Environment layer.</summary>
        public static void SetEnvironmentLayer(GameObject root)
        {
            int layer = GetEnvironmentLayer();
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                collider.gameObject.layer = layer;
        }

        public static int GetEnvironmentLayer()
        {
            int layer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (layer == -1)
                throw new InvalidOperationException($"The \"{EnvironmentLayerName}\" layer does not exist. Add it in Project Settings > Tags and Layers (layer 9).");
            return layer;
        }
    }
}
