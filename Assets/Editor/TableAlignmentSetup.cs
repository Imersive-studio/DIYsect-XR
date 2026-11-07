// Run Tools > DIYsect > Add Table Alignment after Create Scenes. It changes Professor_MR only.
using System;
using System.IO;
using DIYsect.MixedReality;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the table alignment to Professor_MR: plane detection and ray casting on the XR origin, a grabbable
    /// alignment handle, and the TableAlignment component that moves the DIYsect root. Safe to run more than once.
    /// </summary>
    public static class TableAlignmentSetup
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string RootName = "DIYsect";
        private const string AlignmentName = "TableAlignment";
        private const string HandleName = "TableAlignmentHandle";
        private const string TableName = "DissectionTable";
        private const string PlanePrefabPath = "Assets/MRTabletopAssets/Prefabs/AR/ARPlane.prefab";
        private const string MaterialPath = "Assets/Prefabs/AlignmentHandle.mat";

        [MenuItem("Tools/DIYsect/Add Table Alignment")]
        public static void Apply()
        {
            if (!File.Exists(ScenePath))
                throw new FileNotFoundException($"{ScenePath} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                if (candidate.name == AlignmentName || candidate.GetComponentInChildren<TableAlignment>(true) != null)
                {
                    Debug.Log($"{ScenePath} already has the table alignment, leaving it unchanged.");
                    return;
                }
                if (candidate.name == RootName)
                    root = candidate;
            }
            if (root == null)
                throw new InvalidOperationException($"No {RootName} root in {ScenePath}. Run Tools > DIYsect > Create Scenes first.");

            Transform table = FindChild(root.transform, TableName);
            Renderer top = table != null ? FindChild(table, "Top")?.GetComponent<Renderer>() : null;
            if (top == null)
                throw new InvalidOperationException($"The {TableName} with a Top renderer was not found under {RootName}.");

            XROrigin origin = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
            {
                origin = candidate.GetComponentInChildren<XROrigin>(true);
                if (origin != null) break;
            }
            if (origin == null || origin.Camera == null)
                throw new InvalidOperationException("No XR origin with a camera in the scene.");

            ARPlaneManager planes = EnsureComponent<ARPlaneManager>(origin.gameObject);
            planes.planePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlanePrefabPath);
            // Planes are only requested once the scene permission is granted. TableAlignment switches this on.
            planes.enabled = false;
            ARRaycastManager raycasts = EnsureComponent<ARRaycastManager>(origin.gameObject);

            XRGrabInteractable handle = CreateHandle(root.transform);

            var alignmentObject = new GameObject(AlignmentName);
            var alignment = alignmentObject.AddComponent<TableAlignment>();
            var serialized = new SerializedObject(alignment);
            serialized.FindProperty("_root").objectReferenceValue = root.transform;
            serialized.FindProperty("_tableTop").objectReferenceValue = top;
            serialized.FindProperty("_viewer").objectReferenceValue = origin.Camera.transform;
            serialized.FindProperty("_handle").objectReferenceValue = handle;
            serialized.FindProperty("_raycastManager").objectReferenceValue = raycasts;
            serialized.FindProperty("_planeManager").objectReferenceValue = planes;
            Renderer[] parts = table.GetComponentsInChildren<Renderer>(true);
            SerializedProperty list = serialized.FindProperty("_virtualTable");
            list.arraySize = parts.Length;
            for (int i = 0; i < parts.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"Added the table alignment to {ScenePath}.");
        }

        private static XRGrabInteractable CreateHandle(Transform nearRoot)
        {
            // A low disc a hand can pinch or grab. It starts beside the table; TableAlignment parks it exactly at run time.
            var handle = new GameObject(HandleName);
            handle.transform.SetPositionAndRotation(nearRoot.position + new Vector3(0f, 0.83f, 0.5f), Quaternion.identity);

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc";
            UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(handle.transform, false);
            disc.transform.localScale = new Vector3(0.14f, 0.012f, 0.14f);
            disc.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial();

            var collider = handle.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = 0.07f;
            collider.height = 0.05f;

            var body = handle.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var grab = handle.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            return grab;
        }

        private static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("The Universal Render Pipeline/Unlit shader was not found.");
            material = new Material(shader) { name = "AlignmentHandle" };
            material.SetColor("_BaseColor", new Color(0.1f, 0.8f, 1f, 1f));
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                if (child.name == childName) return child;
            return null;
        }
    }
}
