// Run Tools > DIYsect > Hold Organs Where Touched.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Editor
{
    /// <summary>
    /// Makes an organ stay where the hand took hold of it. Without this the toolkit moves the organ's pivot to the hand,
    /// and the organ pivots sit away from the organs themselves, so a lifted organ floated off to one side of the hand.
    /// Dynamic attach keeps the grab point under the hand, the usual setting for irregular objects. Only the body
    /// prefab changes. Safe to run more than once.
    /// </summary>
    public static class HoldOrgansWhereTouched
    {
        public const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        [MenuItem("Tools/DIYsect/Hold Organs Where Touched")]
        public static void Apply()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                int organs = 0;
                foreach (var grab in root.GetComponentsInChildren<XRGrabInteractable>(true))
                {
                    Configure(grab);
                    organs++;
                }
                if (organs == 0)
                    throw new InvalidOperationException($"{PrefabPath} has no grabbable organs. Run Tools > DIYsect > Add Body Networking first.");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[HoldOrgansWhereTouched] {organs} organs are now held where the hand touches them.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>The grab settings every organ uses. Shared with Add Body Networking so a rebuild keeps them.</summary>
        public static void Configure(XRGrabInteractable grab)
        {
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.snapToColliderVolume = true;
        }

        // Batch entry point: -executeMethod DIYsect.Editor.HoldOrgansWhereTouched.ApplyBatch
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
    }
}
