// Run Tools > DIYsect > Add Shared Gaze after Build Shared Scene and Add Head Pose.
using System;
using System.IO;
using DIYsect.Classroom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the shared gaze dots to the shared classroom scene: a GazePresenter under the DIYsect root, next to the body, wired to
    /// the body's HeadPoseBoard, and one unlit transparent material for the dots. Safe to run more than once: existing parts are reused.
    /// </summary>
    public static class AddSharedGaze
    {
        public const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        public const string MaterialPath = "Assets/Materials/GazeDot.mat";
        public const string PresenterName = "SharedGaze";
        private const string RootName = "DIYsect";

        [MenuItem("Tools/DIYsect/Add Shared Gaze")]
        public static void Apply()
        {
            Material material = LoadOrCreateMaterial();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    root = candidate.transform;
            if (root == null)
                throw new InvalidOperationException($"{ScenePath} has no {RootName} root.");
            HeadPoseBoard board = root.GetComponentInChildren<HeadPoseBoard>(true);
            if (board == null)
                throw new InvalidOperationException($"{ScenePath} has no HeadPoseBoard under {RootName}. Run Tools > DIYsect > Add Head Pose and rebuild the scene.");

            Transform holder = root.Find(PresenterName);
            if (holder == null)
            {
                holder = new GameObject(PresenterName).transform;
                holder.SetParent(root, false);
            }
            holder.gameObject.layer = 0;
            var presenter = holder.GetComponent<GazePresenter>();
            if (presenter == null)
                presenter = holder.gameObject.AddComponent<GazePresenter>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_board").objectReferenceValue = board;
            serialized.FindProperty("_material").objectReferenceValue = material;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"[AddSharedGaze] {ScenePath}: shared gaze wired to {board.name}.");
        }

        private static Material LoadOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("The Universal Render Pipeline/Unlit shader was not found.");
            material = new Material(shader) { name = "GazeDot" };
            material.SetFloat("_Surface", 1f);   // transparent
            material.SetFloat("_Blend", 0f);     // alpha
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);      // both sides
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", Color.white);
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddSharedGaze.ApplyBatch
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
