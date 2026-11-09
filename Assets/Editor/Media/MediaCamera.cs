using System.IO;
using DIYsect.Classroom;
using UnityEngine;

namespace DIYsect.Editor.Media
{
    /// <summary>
    /// The one camera every submission image is rendered with, so they share size, field of view, lighting and label style.
    /// Aim it with <see cref="Aim"/>, then <see cref="Save"/> writes a PNG. Editor only.
    /// </summary>
    public sealed class MediaCamera
    {
        public const int Width = 1280;
        public const int Height = 720;
        private const float FieldOfView = 45f;

        private readonly Camera _camera;
        private readonly RenderTexture _target;
        private readonly Texture2D _readback;

        public MediaCamera()
        {
            // Tagged so the billboards (pin labels, scalpel label) turn toward it.
            var holder = new GameObject("MediaCamera") { tag = "MainCamera" };
            _camera = holder.AddComponent<Camera>();
            _camera.fieldOfView = FieldOfView;
            _camera.nearClipPlane = 0.02f;
            _camera.clearFlags = CameraClearFlags.Skybox;
            _target = new RenderTexture(Width, Height, 24) { antiAliasing = 4 };
            _camera.targetTexture = _target;
            _readback = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        }

        public Camera Camera => _camera;

        /// <summary>Puts the camera on a sphere around the target, looking at it. Yaw turns around the vertical axis.</summary>
        public void Aim(Vector3 target, float yawDegrees, float pitchDegrees, float distance)
        {
            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            _camera.transform.SetPositionAndRotation(target + rotation * (Vector3.back * distance), rotation);
        }

        public void Aim(Vector3 position, Vector3 lookAt)
        {
            _camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }

        /// <summary>Renders the view with the optional pointer markers and the caption tag, and writes a PNG.</summary>
        public void Save(string path, string tag, PointerPresenter pointers = null)
        {
            if (pointers != null)
            {
                pointers.SetViewCamera(_camera);
                pointers.UpdatePlacements();
            }
            var overlay = new MediaHudOverlay(_camera, Width, Height);
            if (pointers != null)
                overlay.DrawPointers(pointers);
            overlay.DrawTag(tag);
            _camera.Render();
            RenderTexture.active = _target;
            _readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            _readback.Apply();
            RenderTexture.active = null;
            overlay.Destroy();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, _readback.EncodeToPNG());
        }

        public void Destroy()
        {
            _camera.targetTexture = null;
            Object.Destroy(_camera.gameObject);
            Object.Destroy(_target);
            Object.Destroy(_readback);
        }
    }
}
