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
    /// layer swung open (skin, rectus, transversus, omentum), plus a wide view of the hall from behind the student
    /// and a close view of the instruments on their stand,
    /// and writes PNGs to docs/previews. The open pose is set on the
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
        private static readonly Vector3 WideEye = new Vector3(-1.2f, 2.8f, -4f);
        private static readonly Vector3 WideLookAt = new Vector3(0f, 1.2f, 2.5f);
        // The instrument stand is beside the table at the student's front right.
        private static readonly Vector3 StandEye = new Vector3(0.25f, 1.4f, 0.15f);
        private static readonly Vector3 StandLookAt = new Vector3(0.55f, 1.04f, 0.5f);

        [MenuItem("Tools/DIYsect/Capture Scene Previews")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(OutputFolder);
            Render($"{OutputFolder}/scene_hall.png", WideEye, WideLookAt, 70f);
            Render($"{OutputFolder}/scene_intact.png", EyePosition, LookAt, 50f);
            Render($"{OutputFolder}/scene_instruments.png", StandEye, StandLookAt, 40f);
            foreach (LayerHinge hinge in Object.FindObjectsByType<LayerHinge>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                SwingOpen(hinge);
            Render($"{OutputFolder}/scene_opened.png", EyePosition, LookAt, 50f);
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

        private static void Render(string path, Vector3 eye, Vector3 lookAt, float fieldOfView)
        {
            var holder = new GameObject("PreviewCamera");
            try
            {
                holder.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(lookAt - eye));
                var camera = holder.AddComponent<Camera>();
                camera.fieldOfView = fieldOfView;
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
