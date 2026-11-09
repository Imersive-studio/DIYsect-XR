// Run Tools > DIYsect > Configure Student Rig.
using System;
using System.IO;
using DIYsect.MixedReality;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace DIYsect.Editor
{
    /// <summary>
    /// Sets up the student's VR rig in LectureHall_VR for a headset session.
    /// Height: one fixed standing eye height for everybody. The rig tracks from the headset's own start position
    /// (Device mode) and lifts the camera by <see cref="EyeHeight"/>, so the table and body always sit at the same
    /// comfortable spot below the eyes whether the student is tall, short, standing or seated.
    /// Walking: the template is a tabletop game, so it ships its Move and Turn providers switched off. They are the
    /// standard XR Interaction Toolkit ones; this turns them on: left thumbstick moves, right thumbstick snap-turns.
    /// Only LectureHall_VR changes. Professor_MR keeps its own setup. Safe to run more than once.
    /// </summary>
    public static class ConfigureStudentRig
    {
        private const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";
        private const float EyeHeight = 1.6f;
        public const float RayLength = 0.4f;
        public const float RayWidth = 4f;
        private const string BallMaterialPath = "Assets/Models/Environment/RayBall.mat";

        // Unlit, so the ball is bright against passthrough and against the dark hall.
        private static Material LoadOrCreateBallMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(BallMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? throw new InvalidOperationException("The URP Unlit shader was not found.");
            material = new Material(shader) { name = "RayBall" };
            material.SetColor("_BaseColor", new Color(0.45f, 0.95f, 1f, 1f));
            AssetDatabase.CreateAsset(material, BallMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        [MenuItem("Tools/DIYsect/Configure Student Rig")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
            if (origin == null)
                throw new InvalidOperationException($"{ScenePath} has no XROrigin.");

            var serialized = new SerializedObject(origin);
            serialized.FindProperty("m_RequestedTrackingOriginMode").intValue = (int)XROrigin.TrackingOriginMode.Device;
            serialized.FindProperty("m_CameraYOffset").floatValue = EyeHeight;
            serialized.ApplyModifiedProperties();

            // The template's rig prefab lifts the XR Origin 0.45 m for a table top room. The student stands on the
            // lecture hall floor, so the origin goes back to floor level and the eyes land at exactly EyeHeight.
            Vector3 position = origin.transform.position;
            origin.transform.position = new Vector3(position.x, 0f, position.z);

            Transform locomotion = origin.transform.Find("Locomotion");
            if (locomotion == null)
                throw new InvalidOperationException("The rig has no Locomotion object.");
            // Move: every provider on it on. Turn: snap turning only (continuous turning makes many people sick).
            Switch(locomotion.Find("Move"), name => true);
            Switch(locomotion.Find("Turn"), name => name == "SnapTurnProvider");
            int rays = MakeRaysVisible(origin);

            // Fixes the eye height at EyeHeight whatever the tracking origin is (see EyeHeightCalibration).
            if (!origin.TryGetComponent(out EyeHeightCalibration calibration))
                calibration = origin.gameObject.AddComponent<EyeHeightCalibration>();
            EditorUtility.SetDirty(calibration);
            PrefabUtility.RecordPrefabInstancePropertyModifications(calibration);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: Device tracking, eye height {EyeHeight} m, move and snap turn on, {rays} rays always visible.");
        }

        // The template draws a very short thin line that disappears when nothing is hit. Controllers and hands keep a
        // clear pointer: the line always shows, at least RayLength long, and RayWidth times as wide.
        public static int MakeRaysVisible(XROrigin origin)
        {
            int count = 0;
            foreach (CurveVisualController visual in origin.GetComponentsInChildren<CurveVisualController>(true))
            {
                var serialized = new SerializedObject(visual);
                serialized.FindProperty("m_ExtendLineToEmptyHit").boolValue = true;
                serialized.FindProperty("m_RestingVisualLineLength").floatValue = RayLength;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (visual.TryGetComponent(out LineRenderer line))
                {
                    line.widthMultiplier = RayWidth;
                    EditorUtility.SetDirty(line);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(line);
                }
                // A small ball where the ray ends.
                if (!visual.TryGetComponent(out DIYsect.Interaction.RayEndBall endBall))
                    endBall = visual.gameObject.AddComponent<DIYsect.Interaction.RayEndBall>();
                var serializedBall = new SerializedObject(endBall);
                serializedBall.FindProperty("_material").objectReferenceValue = LoadOrCreateBallMaterial();
                serializedBall.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(endBall);
                PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
                count++;
            }
            if (count == 0)
                throw new InvalidOperationException("The rig has no ray visuals (CurveVisualController).");

            // The template's rig also switches far casting off on every Near-Far interactor, since it is a game played by touching
            // the table. With it off the toolkit never enables the ray line, however long or wide the line is made, and nothing
            // can be pointed at from a distance. Turn it on so the rays draw and the tools can be grabbed by pointing.
            foreach (UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor interactor in origin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true))
            {
                var serializedInteractor = new SerializedObject(interactor);
                serializedInteractor.FindProperty("m_EnableFarCasting").boolValue = true;
                serializedInteractor.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(interactor);
            }
            return count;
        }

        private static void Switch(Transform target, Func<string, bool> enabledFor)
        {
            if (target == null)
                throw new InvalidOperationException("A locomotion object is missing from the rig.");
            target.gameObject.SetActive(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
            foreach (Behaviour provider in target.GetComponents<Behaviour>())
            {
                provider.enabled = enabledFor(provider.GetType().Name);
                EditorUtility.SetDirty(provider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(provider);
            }
        }
    }
}
