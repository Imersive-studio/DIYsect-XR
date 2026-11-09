// Run Tools > DIYsect > Add Quiz Heatmap after Add Quiz, Add Organ Pointer and Apply Label Design (it needs QuizRound, the pop-out and the Inter font).
using System;
using System.IO;
using DIYsect.Learning;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the quiz heatmap to Assets/Prefabs/AnatomyBody.prefab: a QuizHeatmapPresenter next to the body's QuizRound, wired to
    /// that QuizRound, to the ClassmatePopOutPresenter and to the Inter font. Every scene that holds the body prefab gets it
    /// through the prefab, so no scene needs editing. Safe to run more than once: the existing component is reused.
    /// </summary>
    public static class AddQuizHeatmap
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        [MenuItem("Tools/DIYsect/Add Quiz Heatmap")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddQuizHeatmap.ApplyBatch
        public static void ApplyBatch()
        {
            try
            {
                Apply();
                AssetDatabase.SaveAssets();
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
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ApplyLabelDesign.FontPath)
                ?? throw new FileNotFoundException($"{ApplyLabelDesign.FontPath} is missing or is not a font asset.");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (!root.TryGetComponent(out QuizRound quiz))
                    throw new InvalidOperationException("The body has no QuizRound. Run Tools > DIYsect > Add Quiz first.");
                // The pop-out is optional: without it the heatmap still works and the correct organ only turns mint.
                root.TryGetComponent(out ClassmatePopOutPresenter popOut);

                if (!root.TryGetComponent(out QuizHeatmapPresenter presenter))
                    presenter = root.AddComponent<QuizHeatmapPresenter>();

                var serialized = new SerializedObject(presenter);
                Set(serialized, "_quiz", quiz);
                Set(serialized, "_popOut", popOut);
                Set(serialized, "_fontAsset", font);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the quiz heatmap to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Set(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new InvalidOperationException($"QuizHeatmapPresenter has no field {field}.");
            property.objectReferenceValue = value;
        }
    }
}
