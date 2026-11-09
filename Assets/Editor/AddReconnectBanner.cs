// Run Tools > DIYsect > Add Reconnect Banner after Add Classmate Auto Join and Add Classmate Desktop Camera.
using System;
using DIYsect.ClassmateConnection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Puts a "Reconnecting…" banner on the classmate camera and the rejoin helper that brings the classmate back to the same
    /// room after a drop. The banner sits in the upper middle of the screen: clear of the Reset view button (top right), the
    /// pin message (bottom) and the status line (top left). Safe to run more than once.
    /// </summary>
    public static class AddReconnectBanner
    {
        private const string ClassmateScenePath = "Assets/Scenes/Classmate_Desktop.unity";
        private const string RigName = "ClassmateCamera";
        private const string CanvasName = "ReconnectBannerCanvas";

        [MenuItem("Tools/DIYsect/Add Reconnect Banner")]
        public static void Apply() => Apply(ClassmateScenePath);

        public static void Apply(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject rig = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == RigName)
                        rig = t.gameObject;
            }
            if (rig == null)
                throw new InvalidOperationException($"{scenePath} has no {RigName}. Run Tools > DIYsect > Add Classmate Desktop Camera first.");
            var join = UnityEngine.Object.FindFirstObjectByType<ClassmateAutoJoin>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{scenePath} has no ClassmateAutoJoin. Run Tools > DIYsect > Add Classmate Auto Join first.");
            AddTo(rig, join);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.IO.IOException($"Could not save {scenePath}.");
            Debug.Log($"[AddReconnectBanner] Banner and rejoin are on the {RigName} in {scenePath}.");
        }

        /// <summary>Adds the banner canvas, the banner and the rejoin helper to the classmate camera. Does nothing it has already done.</summary>
        public static void AddTo(GameObject rig, ClassmateAutoJoin join)
        {
            var rejoin = rig.GetComponent<ClassmateRejoin>() ?? rig.AddComponent<ClassmateRejoin>();
            var rejoinSerialized = new SerializedObject(rejoin);
            rejoinSerialized.FindProperty("_join").objectReferenceValue = join;
            rejoinSerialized.ApplyModifiedPropertiesWithoutUndo();

            var banner = rig.GetComponent<ReconnectBanner>();
            if (banner == null)
            {
                banner = rig.AddComponent<ReconnectBanner>();
                BuildCanvas(rig, banner);
            }
            var bannerSerialized = new SerializedObject(banner);
            bannerSerialized.FindProperty("_rejoin").objectReferenceValue = rejoin;
            bannerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildCanvas(GameObject rig, ReconnectBanner banner)
        {
            var canvasObject = new GameObject(CanvasName);
            canvasObject.transform.SetParent(rig.transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            // Upper middle: left of the Reset view button, below the status line and its code box, above the pin message.
            var panel = new GameObject("Banner");
            panel.transform.SetParent(canvasObject.transform, false);
            var background = panel.AddComponent<Image>();
            background.color = new Color(0.75f, 0.35f, 0.05f, 0.9f);
            background.raycastTarget = false;
            var rect = background.rectTransform;
            rect.anchorMin = new Vector2(0.15f, 0.62f);
            rect.anchorMax = new Vector2(0.75f, 0.76f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var labelObject = new GameObject("Message");
            labelObject.transform.SetParent(panel.transform, false);
            var label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 32;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 8f);
            labelRect.offsetMax = new Vector2(-16f, -8f);

            var serialized = new SerializedObject(banner);
            serialized.FindProperty("_panel").objectReferenceValue = panel;
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddReconnectBanner.ApplyBatch
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
