// Run Tools > DIYsect > Add Head Pose after Add Organ Pointer (Create Anatomy Body runs it).
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the shared head positions to Assets/Prefabs/AnatomyBody.prefab: a HeadPoseBoard (server-held professor and student
    /// head slots, on the body's own NetworkObject) and a HeadPosePublisher that sends a headset's head to it, so a classmate can follow it.
    /// Safe to run more than once: existing components are reused and values you changed are kept.
    /// </summary>
    public static class AddHeadPose
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        [MenuItem("Tools/DIYsect/Add Head Pose")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddHeadPose.ApplyBatch
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

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<SharedBody>() == null)
                    throw new InvalidOperationException("The body has no SharedBody. Run Add Body Networking first.");
                AnatomyBodyDissection.GetOrAdd<HeadPoseBoard>(root);
                AnatomyBodyDissection.GetOrAdd<HeadPosePublisher>(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the head positions to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
