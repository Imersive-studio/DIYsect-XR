// Run Tools > DIYsect > Add Question Pins after Add Body Networking.
using System;
using System.IO;
using DIYsect.Classroom;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the QuestionBoard and QuestionPinPresenter to Assets/Prefabs/AnatomyBody.prefab and creates the
    /// unlit pin marker material. Safe to run more than once.
    /// </summary>
    public static class AnatomyBodyQuestions
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string MaterialPath = "Assets/Prefabs/PinMarker.mat";

        [MenuItem("Tools/DIYsect/Add Question Pins")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        /// <summary>Adds the components to the prefab. Also called by Create Anatomy Body, so a rebuilt body keeps them.</summary>
        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");

            Material material = GetOrCreateMaterial();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<DIYsect.Networking.SharedBody>() == null)
                    throw new InvalidOperationException("The body has no SharedBody. Run Tools > DIYsect > Add Body Networking first.");
                if (!root.TryGetComponent(out QuestionBoard _))
                    root.AddComponent<QuestionBoard>();
                if (!root.TryGetComponent(out QuestionPinPresenter presenter))
                    presenter = root.AddComponent<QuestionPinPresenter>();

                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_markerMaterial").objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the question board to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Material GetOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("The URP Unlit shader was not found.");
            material = new Material(shader) { name = "PinMarker" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
