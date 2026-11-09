// Run Tools > DIYsect > Add Raise Hand after Add Body Networking.
using System;
using System.IO;
using DIYsect.Classroom;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the HandBoard (the class's raised hands) to Assets/Prefabs/AnatomyBody.prefab, next to the question board, so every
    /// scene's body has it. Safe to run more than once.
    /// </summary>
    public static class AnatomyBodyHands
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        [MenuItem("Tools/DIYsect/Add Raise Hand")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        /// <summary>Adds the board to the prefab. Also called by Create Anatomy Body, so a rebuilt body keeps it.</summary>
        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<DIYsect.Networking.SharedBody>() == null)
                    throw new InvalidOperationException("The body has no SharedBody. Run Tools > DIYsect > Add Body Networking first.");
                if (!root.TryGetComponent(out HandBoard _))
                    root.AddComponent<HandBoard>();
                // Who may unmute goes with the raised hands: a hand is how a classmate asks, the professor's answer opens the mic.
                if (!root.TryGetComponent(out SpeakBoard _))
                    root.AddComponent<SpeakBoard>();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the hand board and the speak board to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AnatomyBodyHands.ApplyBatch
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
