// Run Tools > DIYsect > Add Organ Pointer after Add Body Networking and Add Body Dissection (Create Anatomy Body runs it).
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.Dissection;
using DIYsect.Interaction;
using DIYsect.Learning;
using DIYsect.Networking;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the translated pointer to Assets/Prefabs/AnatomyBody.prefab: a PointerBoard (server-held pointer slots, on the body's
    /// own NetworkObject), a PointerPresenter and PointerHud for classmates, an OrganPointerInput for headsets, and a ClassmatePopOutPresenter (the organ a classmate taps rises out of the body
    /// for everyone; it gets the Inter font from Apply Label Design's font path).
    /// Safe to run more than once: existing components are reused and values you changed are kept.
    /// </summary>
    public static class AddOrganPointer
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        [MenuItem("Tools/DIYsect/Add Organ Pointer")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddOrganPointer.ApplyBatch
        public static void ApplyBatch()
        {
            ApplyToPrefab();
            AssetDatabase.SaveAssets();
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<SharedBody>() == null || root.GetComponent<DissectionProgress>() == null)
                    throw new InvalidOperationException("The body has no SharedBody or DissectionProgress. Run Add Body Networking and Add Body Dissection first.");
                AnatomyBodyDissection.GetOrAdd<PointerBoard>(root);
                AnatomyBodyDissection.GetOrAdd<PointerPresenter>(root);
                AnatomyBodyDissection.GetOrAdd<PointerHud>(root);
                AnatomyBodyDissection.GetOrAdd<OrganPointerInput>(root);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ApplyLabelDesign.FontPath)
                    ?? throw new FileNotFoundException($"{ApplyLabelDesign.FontPath} is missing or is not a font asset.");
                var popOut = AnatomyBodyDissection.GetOrAdd<ClassmatePopOutPresenter>(root);
                var serialized = new SerializedObject(popOut);
                serialized.FindProperty("_fontAsset").objectReferenceValue = font;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the organ pointer to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
