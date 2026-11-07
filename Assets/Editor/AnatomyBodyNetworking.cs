// Run Tools > DIYsect > Add Body Networking after Create Anatomy Body (it also runs from Create Anatomy Body).
using System;
using System.IO;
using System.Collections.Generic;
using DIYsect.Networking;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the network components to Assets/Prefabs/AnatomyBody.prefab and re-saves the DIYsect scenes
    /// so their placed body instances get a stable network id. Safe to run more than once.
    /// The body root gets a NetworkObject and SharedBody. Every child of Supine/Organs gets a
    /// server-authoritative, interpolated NetworkTransform in local space.
    /// </summary>
    public static class AnatomyBodyNetworking
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string OrgansPath = "Supine/Organs";
        private const int ExpectedOrganCount = 22;
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Add Body Networking")]
        public static void Apply()
        {
            ApplyToPrefab();
            ResaveScenes();
        }

        /// <summary>Adds the network components to the prefab only.</summary>
        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform organsGroup = root.transform.Find(OrgansPath);
                if (organsGroup == null)
                    throw new InvalidOperationException($"{PrefabPath} has no {OrgansPath} group.");
                if (organsGroup.childCount != ExpectedOrganCount)
                    throw new InvalidOperationException($"Expected {ExpectedOrganCount} organs under {OrgansPath}, found {organsGroup.childCount}.");

                GetOrAdd<NetworkObject>(root);
                var organs = new List<NetworkTransform>();
                foreach (Transform organ in organsGroup)
                {
                    var sync = GetOrAdd<NetworkTransform>(organ.gameObject);
                    sync.AuthorityMode = NetworkTransform.AuthorityModes.Server;
                    sync.InLocalSpace = true;
                    sync.Interpolate = true;
                    // The extra smoothing pass stops about 5 mm short of the target. Organs should land exactly.
                    sync.PositionLerpSmoothing = false;
                    sync.RotationLerpSmoothing = false;
                    sync.ScaleLerpSmoothing = false;
                    organs.Add(sync);
                }

                var body = GetOrAdd<SharedBody>(root);
                var serialized = new SerializedObject(body);
                SerializedProperty list = serialized.FindProperty("_organs");
                list.arraySize = organs.Count;
                for (int i = 0; i < organs.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = organs[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added network components to {PrefabPath} ({organs.Count} organs).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Opening and saving a scene makes Netcode bake the network id of the placed body instance.
        private static void ResaveScenes()
        {
            foreach (string path in ScenePaths)
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"{path} is missing. Run Tools > DIYsect > Create Scenes first.");
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException($"Could not save {path}.");
                Debug.Log($"Re-saved {path}.");
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }
    }
}
