// Run Tools > DIYsect > Add Organ Info Cards after Create Anatomy Body (it needs the grab, socket and pointer parts on the body).
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.Learning;
using DIYsect.Networking;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the organ info card presenter to Assets/Prefabs/AnatomyBody.prefab and gives it the organ text asset
    /// (Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset) and the built-in font. Every scene that holds the body prefab
    /// gets the cards through the prefab, so no scene needs editing. Safe to run more than once: the existing component is reused.
    /// </summary>
    public static class AddOrganInfoCards
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string LibraryPath = "Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset";

        [MenuItem("Tools/DIYsect/Add Organ Info Cards")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddOrganInfoCards.ApplyBatch
        public static void ApplyBatch()
        {
            ApplyToPrefab();
            AssetDatabase.SaveAssets();
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            var library = AssetDatabase.LoadAssetAtPath<OrganInfoLibrary>(LibraryPath);
            if (library == null)
                throw new FileNotFoundException($"{LibraryPath} is missing or did not import.");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<SharedBody>() == null || root.GetComponent<OrganGrabAuthority>() == null)
                    throw new InvalidOperationException("The body has no SharedBody or OrganGrabAuthority. Run Tools > DIYsect > Add Body Networking first.");
                if (root.GetComponent<PointerBoard>() == null)
                    throw new InvalidOperationException("The body has no PointerBoard. Run Tools > DIYsect > Add Organ Pointer first.");

                if (!root.TryGetComponent(out OrganInfoCardPresenter presenter))
                    presenter = root.AddComponent<OrganInfoCardPresenter>();

                var serialized = new SerializedObject(presenter);
                SetReference(serialized, "_library", library);
                SetReference(serialized, "_font", font);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the organ info cards to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetReference(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException($"OrganInfoCardPresenter has no field {field}.");
            property.objectReferenceValue = value;
        }
    }
}
