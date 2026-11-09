// Run Tools > DIYsect > Apply Label Design once the label scripts are in. Batch: -executeMethod DIYsect.Editor.ApplyLabelDesign.ApplyBatch
using System;
using System.IO;
using DIYsect.Interaction;
using DIYsect.Learning;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Wires the label design into Assets/Prefabs/AnatomyBody.prefab: the Inter font, the organ captions (the organ info
    /// library) and the unlit transparent material go into every OrganHoverLabel, the OrganGrabPresenter and the
    /// OrganInfoCardPresenter. The scenes hold the body only as a prefab instance and have none of these components of
    /// their own, so saving the prefab is enough; any scene that does hold one directly is reported, not edited.
    /// Safe to run more than once.
    /// </summary>
    public static class ApplyLabelDesign
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string FontPath = "Assets/MRTabletopAssets/Fonts/Inter-Regular SDF.asset";
        private const string MaterialPath = "Assets/Models/Environment/ChecklistGuidance.mat";
        private const string LibraryPath = "Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset";

        [MenuItem("Tools/DIYsect/Apply Label Design")]
        public static void Apply()
        {
            ApplyToPrefab();
            IncludeOnTopShader();
        }

        // Batch entry point. Exits 0 on success and 1 on any failure.
        public static void ApplyBatch()
        {
            try
            {
                ApplyToPrefab();
                IncludeOnTopShader();
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Apply Label Design failed: {exception}");
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Adds the overlay text shader to Graphics settings > Always Included Shaders. Labels look it up by name at run
        /// time, and a shader nothing references is left out of the Quest build otherwise.
        /// </summary>
        public static void IncludeOnTopShader()
        {
            Shader shader = Shader.Find(LabelText.OnTopShaderName)
                ?? throw new InvalidOperationException($"{LabelText.OnTopShaderName} is missing. Import the TextMesh Pro essentials.");
            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("ProjectSettings/GraphicsSettings.asset"));
            SerializedProperty list = settings.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    return;
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ApplyLabelDesign] {LabelText.OnTopShaderName} is now always included in builds.");
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            var font = Require<TMP_FontAsset>(FontPath);
            var material = Require<Material>(MaterialPath);
            var library = Require<OrganInfoLibrary>(LibraryPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                int hover = 0;
                foreach (OrganHoverLabel label in root.GetComponentsInChildren<OrganHoverLabel>(true))
                {
                    Assign(label, ("_font", font), ("_captions", library));
                    hover++;
                }
                if (hover == 0)
                    throw new InvalidOperationException("The body has no OrganHoverLabel. Run Tools > DIYsect > Add Hover Labels And Haptics first.");

                var grab = root.GetComponent<OrganGrabPresenter>();
                if (grab == null)
                    throw new InvalidOperationException("The body has no OrganGrabPresenter. Run Tools > DIYsect > Add Organ Sockets first.");
                Assign(grab, ("_font", font), ("_captions", library), ("_leaderMaterial", material));

                var cards = root.GetComponent<OrganInfoCardPresenter>();
                if (cards == null)
                    throw new InvalidOperationException("The body has no OrganInfoCardPresenter. Run Tools > DIYsect > Add Organ Info Cards first.");
                Assign(cards, ("_fontAsset", font), ("_panelMaterial", material));

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Applied the label design to {PrefabPath} ({hover} hover labels).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static T Require<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new FileNotFoundException($"{path} is missing or is not a {typeof(T).Name}.");
            return asset;
        }

        private static void Assign(Component target, params (string field, UnityEngine.Object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, UnityEngine.Object value) in values)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null)
                    throw new InvalidOperationException($"{target.GetType().Name} has no field {field}.");
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
