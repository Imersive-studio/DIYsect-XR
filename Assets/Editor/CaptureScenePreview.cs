// Run Tools > DIYsect > Capture Scene Previews to refresh docs/previews/scene_*.png.
// Batch (needs graphics, not -nographics): Unity -batchmode -quit -projectPath . -executeMethod DIYsect.Editor.CaptureScenePreview.Capture
using System.IO;
using DIYsect.Dissection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Renders the student's lecture hall scene from where the student stands, once intact and once with every
    /// layer swung open (skin, rectus, omentum), and writes PNGs to docs/previews. The open pose is set on the
    /// loaded scene only and never saved, so the scene file is unchanged.
    /// </summary>
    public static class CaptureScenePreview
    {
        private const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";
        private const string OutputFolder = "docs/previews";
        private const int Width = 1600;
        private const int Height = 900;
        // The student stands at the rig origin; the table centre is 1.2 m ahead.
        private static readonly Vector3 EyePosition = new Vector3(0.75f, 1.55f, 0.45f);
        private static readonly Vector3 LookAt = new Vector3(0f, 1.0f, 1.2f);

        [MenuItem("Tools/DIYsect/Capture Scene Previews")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);
            Render($"{OutputFolder}/scene_intact.png");
            foreach (LayerHinge hinge in Object.FindObjectsByType<LayerHinge>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SwingOpen(hinge);
            Render($"{OutputFolder}/scene_opened.png");
            // Discard the opened pose.
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void SwingOpen(LayerHinge hinge)
        {
            var serialized = new SerializedObject(hinge);
            Vector3 axis = serialized.FindProperty("_hingeAxis").vector3Value.normalized;
            float angle = serialized.FindProperty("_openAngleDegrees").floatValue;
            hinge.transform.localRotation *= Quaternion.AngleAxis(angle, axis);
        }

        private static void Render(string path)
        {
            var holder = new GameObject("PreviewCamera");
            try
            {
                holder.transform.SetPositionAndRotation(EyePosition, Quaternion.LookRotation(LookAt - EyePosition));
                var camera = holder.AddComponent<Camera>();
                camera.fieldOfView = 50f;
                camera.nearClipPlane = 0.05f;
                camera.clearFlags = CameraClearFlags.Skybox;
                var target = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                RenderTexture.active = null;
                camera.targetTexture = null;
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(image);
                Debug.Log($"Wrote {path}.");
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }
    }
}
