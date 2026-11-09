// Run Tools > DIYsect > Add Classmate Sketch, or: Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.AddClassmateSketch.ApplyBatch
using System;
using System.IO;
using System.Linq;
using DIYsect.Classroom;
using DIYsect.Interaction;
using DIYsect.Networking;
using DIYsect.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Lets a phone classmate sketch on the body. In the body prefab: a ClassmateSketch child (its own GuideLine, a violet
    /// LineRenderer a little thicker than the professor's, and a GuideLinePresenter). The professor's GuideLine stays on the body root,
    /// so anything that asks the body for its GuideLine still gets the professor's. In Professor_MR: a ClassmateSketchInput on the
    /// classmate camera, listed as a classmate-only script in the ClassroomRole and saved switched off. It has no buttons of its own:
    /// a Draw button calls SetDrawing and a Clear button calls Clear. Safe to run more than once.
    /// </summary>
    public static class AddClassmateSketch
    {
        public const string ChildName = "ClassmateSketch";
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string LineMaterialPath = "Assets/Prefabs/ClassmateSketch.mat";
        // Thicker than the professor's 4 mm line, so it reads on a phone screen.
        private const float LineWidth = 0.007f;

        [MenuItem("Tools/DIYsect/Add Classmate Sketch")]
        public static void Apply()
        {
            ApplyToPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ApplyToScene();
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<GuideLine>() == null)
                    throw new InvalidOperationException("The body has no guide line yet. Run Tools > DIYsect > Add Guide Line first.");
                Transform child = root.transform.Find(ChildName);
                if (child == null)
                {
                    child = new GameObject(ChildName).transform;
                    child.SetParent(root.transform, false);
                }
                child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                child.localScale = Vector3.one;
                // A NetworkBehaviour on a child rides the body's NetworkObject; there is no second NetworkObject.
                var line = AnatomyBodyDissection.GetOrAdd<GuideLine>(child.gameObject);
                var renderer = AnatomyBodyDissection.GetOrAdd<LineRenderer>(child.gameObject);
                renderer.useWorldSpace = false;
                renderer.widthMultiplier = LineWidth;
                renderer.numCapVertices = 4;
                renderer.numCornerVertices = 3;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = AddGuideLine.LoadOrCreate(LineMaterialPath, "Universal Render Pipeline/Unlit", GuidePalette.Violet);
                renderer.positionCount = 0;
                renderer.enabled = false;
                var presenter = AnatomyBodyDissection.GetOrAdd<GuideLinePresenter>(child.gameObject);
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_line").objectReferenceValue = line;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the classmate sketch line to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Puts the sketch input on the classmate camera and wires it to the scene body's sketch line. Does not save the scene.</summary>
        public static ClassmateSketchInput AddToRig(GameObject rig, SharedBody body)
        {
            Transform child = body.transform.Find(ChildName)
                ?? throw new InvalidOperationException($"The body has no {ChildName} child. Run Tools > DIYsect > Add Classmate Sketch.");
            var line = child.GetComponent<GuideLine>()
                ?? throw new InvalidOperationException($"{ChildName} has no GuideLine. Run Tools > DIYsect > Add Classmate Sketch.");
            var tracker = rig.GetComponent<PointerGestureTracker>()
                ?? throw new InvalidOperationException("The classmate camera has no PointerGestureTracker. Run Tools > DIYsect > Add Classmate Desktop Camera.");

            var input = rig.GetComponent<ClassmateSketchInput>() ?? rig.AddComponent<ClassmateSketchInput>();
            var serialized = new SerializedObject(input);
            serialized.FindProperty("_body").objectReferenceValue = body;
            serialized.FindProperty("_line").objectReferenceValue = line;
            serialized.FindProperty("_gestures").objectReferenceValue = tracker;
            serialized.FindProperty("_camera").objectReferenceValue = rig.GetComponent<Camera>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Only a classmate sketches. The role switch turns it on; drawing itself stays off until a button asks.
            input.enabled = false;
            EditorUtility.SetDirty(input);
            return input;
        }

        /// <summary>
        /// Called by Build Shared Scene when it rewrites the classmate script list, so the sketch input is not lost.
        /// Returns <paramref name="behaviours"/> plus the input on <paramref name="rig"/> when it exists, switched off until the role enables it.
        /// </summary>
        public static Behaviour[] WithClassmateInput(Behaviour[] behaviours, GameObject rig)
        {
            if (rig == null || !rig.TryGetComponent(out ClassmateSketchInput input))
                return behaviours;
            input.enabled = false;
            EditorUtility.SetDirty(input);
            return behaviours.Concat(new Behaviour[] { input }).Distinct().ToArray();
        }

        private static void ApplyToScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var orbit = UnityEngine.Object.FindFirstObjectByType<ClassmateOrbitCamera>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no classmate camera. Run Tools > DIYsect > Build Shared Scene first.");
            var board = UnityEngine.Object.FindFirstObjectByType<QuestionBoard>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no body with a QuestionBoard.");
            var body = board.GetComponent<SharedBody>()
                ?? throw new InvalidOperationException("The scene body has no SharedBody.");
            ClassmateSketchInput input = AddToRig(orbit.gameObject, body);

            var role = UnityEngine.Object.FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no ClassroomRole. Run Tools > DIYsect > Build Shared Scene first.");
            var roleSerialized = new SerializedObject(role);
            SerializedProperty list = roleSerialized.FindProperty("_classmateBehaviours");
            bool listed = false;
            for (int i = 0; i < list.arraySize; i++)
                listed |= list.GetArrayElementAtIndex(i).objectReferenceValue == input;
            if (!listed)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = input;
            }
            roleSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: classmate sketch input on {orbit.name}, a classmate-only script.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddClassmateSketch.ApplyBatch
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
