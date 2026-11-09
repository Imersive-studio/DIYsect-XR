// Run Tools > DIYsect > Add Help Request, or: Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.AddAssistRequest.ApplyBatch
using System;
using System.IO;
using System.Linq;
using DIYsect.Classroom;
using DIYsect.ClassRoster;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the student's "Help me" request. The body prefab gets the AssistBoard (the server-held request), the AssistBeacon
    /// (the ring and label everyone sees) and AssistHaptics. Professor_MR gets the Y button input on the XR rig, listed as a
    /// student-only script in the ClassroomRole, and the class roster learns about the request. Safe to run more than once.
    /// Run Tools > DIYsect > Add Class Roster again and this command again if the roster is ever rebuilt.
    /// </summary>
    public static class AddAssistRequest
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string FontPath = "Assets/MRTabletopAssets/Fonts/Inter-Regular SDF.asset";
        private const string RingMaterialPath = "Assets/Prefabs/AssistBeacon.mat";

        [MenuItem("Tools/DIYsect/Add Help Request")]
        public static void Apply()
        {
            ApplyToPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ApplyToScene();
        }

        /// <summary>
        /// Called by Build Shared Scene when it rewrites the student script list, so the help input is not lost.
        /// Returns <paramref name="behaviours"/> plus the input on <paramref name="rig"/> when it exists, switched off until the role enables it.
        /// </summary>
        public static Behaviour[] WithStudentInput(Behaviour[] behaviours, GameObject rig)
        {
            if (rig == null || !rig.TryGetComponent(out AssistRequestInput input))
                return behaviours;
            input.enabled = false;
            EditorUtility.SetDirty(input);
            return behaviours.Concat(new Behaviour[] { input }).Distinct().ToArray();
        }

        private static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? throw new FileNotFoundException($"{FontPath} is missing. Run Tools > DIYsect > Apply Label Design first.");
            Material ring = LoadOrCreateRingMaterial();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<DIYsect.Networking.SharedBody>() == null)
                    throw new InvalidOperationException("The body has no SharedBody. Run Tools > DIYsect > Add Body Networking first.");
                var board = GetOrAdd<AssistBoard>(root);
                var beacon = GetOrAdd<AssistBeacon>(root);
                var haptics = GetOrAdd<AssistHaptics>(root);

                var beaconSerialized = new SerializedObject(beacon);
                beaconSerialized.FindProperty("_board").objectReferenceValue = board;
                beaconSerialized.FindProperty("_font").objectReferenceValue = font;
                beaconSerialized.FindProperty("_ringMaterial").objectReferenceValue = ring;
                beaconSerialized.ApplyModifiedPropertiesWithoutUndo();

                var hapticsSerialized = new SerializedObject(haptics);
                hapticsSerialized.FindProperty("_board").objectReferenceValue = board;
                hapticsSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the help request to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyToScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var hands = UnityEngine.Object.FindFirstObjectByType<HandBoard>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no body with a HandBoard. Run Tools > DIYsect > Add Raise Hand.");
            var board = hands.GetComponent<AssistBoard>()
                ?? throw new InvalidOperationException($"The body in {ScenePath} has no AssistBoard. Its prefab instance did not pick up the new components.");

            var origin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no XR Origin.");
            var input = GetOrAdd<AssistRequestInput>(origin.gameObject);
            var inputSerialized = new SerializedObject(input);
            inputSerialized.FindProperty("_board").objectReferenceValue = board;
            inputSerialized.ApplyModifiedPropertiesWithoutUndo();
            // Only the student has the button. The role switches it on.
            input.enabled = false;
            EditorUtility.SetDirty(input);

            var role = UnityEngine.Object.FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no ClassroomRole. Run Tools > DIYsect > Build Shared Scene first.");
            var roleSerialized = new SerializedObject(role);
            SerializedProperty list = roleSerialized.FindProperty("_studentBehaviours");
            bool listed = false;
            for (int i = 0; i < list.arraySize; i++)
                listed |= list.GetArrayElementAtIndex(i).objectReferenceValue == input;
            if (!listed)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = input;
            }
            roleSerialized.ApplyModifiedPropertiesWithoutUndo();

            var roster = UnityEngine.Object.FindFirstObjectByType<ClassRosterPanel>(FindObjectsInactive.Include);
            if (roster != null)
            {
                var rosterSerialized = new SerializedObject(roster);
                rosterSerialized.FindProperty("_assist").objectReferenceValue = board;
                rosterSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"{ScenePath} has no ClassRosterPanel, so the roster will not show the help request. Run Tools > DIYsect > Add Class Roster, then this command again.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: help button wired for the student (Y), roster {(roster != null ? "updated" : "skipped")}.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddAssistRequest.ApplyBatch
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

        private static Material LoadOrCreateRingMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RingMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? throw new InvalidOperationException("The URP Unlit shader was not found.");
            material = new Material(shader) { name = "AssistBeacon" };
            // The settings the URP Unlit inspector applies for Surface Type: Transparent, Blend: Alpha.
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", GuidePaletteCoral());
            AssetDatabase.CreateAsset(material, RingMaterialPath);
            return material;
        }

        private static Color GuidePaletteCoral()
        {
            return DIYsect.Presentation.GuidePalette.Coral;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }
    }
}
