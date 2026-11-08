// Authority: none. Pure local math for the classmate's own camera.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>Orbit-camera maths kept apart from input and scene objects so it can be tested on its own.</summary>
    public static class OrbitMath
    {
        public static float ClampPitch(float pitch, float min, float max)
        {
            return Mathf.Clamp(pitch, min, max);
        }

        public static float ClampDistance(float distance, float min, float max)
        {
            return Mathf.Clamp(distance, min, max);
        }

        /// <summary>Yaw wraps around; dragging right turns the view so the body appears to turn with the finger.</summary>
        public static float DragYaw(float yaw, float deltaPixelsX, float degreesPerPixel)
        {
            return Mathf.Repeat(yaw - deltaPixelsX * degreesPerPixel, 360f);
        }

        /// <summary>Dragging up raises the camera (pitch grows), clamped to the allowed range.</summary>
        public static float DragPitch(float pitch, float deltaPixelsY, float degreesPerPixel, float min, float max)
        {
            return ClampPitch(pitch - deltaPixelsY * degreesPerPixel, min, max);
        }

        /// <summary>Positive amount zooms in. Multiplicative, so each step feels the same near and far.</summary>
        public static float Zoom(float distance, float amount, float min, float max)
        {
            return ClampDistance(distance * Mathf.Exp(-amount), min, max);
        }

        /// <summary>Camera position and rotation that look at <paramref name="target"/> from the given angles and distance.</summary>
        public static void Pose(Vector3 target, float yaw, float pitch, float distance, out Vector3 position, out Quaternion rotation)
        {
            rotation = Quaternion.Euler(pitch, yaw, 0f);
            position = target - rotation * Vector3.forward * distance;
        }

        /// <summary>The yaw, pitch and distance that reproduce a camera standing at <paramref name="position"/> looking at <paramref name="target"/>.</summary>
        public static void AnglesFrom(Vector3 target, Vector3 position, out float yaw, out float pitch, out float distance)
        {
            Vector3 offset = position - target;
            distance = offset.magnitude;
            if (distance < 1e-5f)
            {
                yaw = 0f;
                pitch = 0f;
                return;
            }
            pitch = Mathf.Asin(Mathf.Clamp(offset.y / distance, -1f, 1f)) * Mathf.Rad2Deg;
            yaw = Mathf.Repeat(Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg, 360f);
        }

        /// <summary>Share of the remaining gap to close this frame; the same result at any frame rate.</summary>
        public static float DampFactor(float speed, float deltaTime)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
        }

        /// <summary>Moves an angle toward its goal along the shortest way round.</summary>
        public static float DampAngle(float current, float goal, float speed, float deltaTime)
        {
            return Mathf.LerpAngle(current, goal, DampFactor(speed, deltaTime));
        }
    }
}
