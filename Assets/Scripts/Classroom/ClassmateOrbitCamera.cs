// Authority: none. This only moves the classmate's own camera on their own screen; nothing is sent over the network.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a desktop or phone classmate walk around the shared body. Dragging (left mouse or one finger) turns the camera
    /// around the body, the scroll wheel or a two-finger pinch moves it closer or farther. It reads gestures from a
    /// <see cref="PointerGestureTracker"/> and moves this object's transform, so anything that reads the camera's pose
    /// (such as the viewpoint a pin stores) sees where the camera really is now.
    /// </summary>
    public class ClassmateOrbitCamera : MonoBehaviour
    {
        [SerializeField] private PointerGestureTracker _gestures;
        [SerializeField, Tooltip("The shared body (its root) that the camera circles and looks at.")]
        private Transform _target;
        [SerializeField, Range(0f, 89f)] private float _minPitch = 10f;
        [SerializeField, Range(0f, 89f)] private float _maxPitch = 80f;
        [SerializeField, Min(0.05f)] private float _minDistance = 0.4f;
        [SerializeField, Min(0.1f)] private float _maxDistance = 3f;
        [SerializeField, Min(0.01f)] private float _degreesPerPixel = 0.25f;
        [SerializeField, Min(0.01f), Tooltip("Share of the distance changed per zoom unit; 0.15 is about 15 percent per wheel notch.")]
        private float _zoomPerUnit = 0.15f;
        [SerializeField, Min(0.1f), Tooltip("Higher is snappier. The camera closes this share of the gap per second, whatever the frame rate.")]
        private float _damping = 14f;

        // Where the player wants the camera, and where it is drawn this frame.
        private float _goalYaw, _goalPitch, _goalDistance;
        private float _yaw, _pitch, _distance;

        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public float Distance => _distance;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public float MinPitch => _minPitch;
        public float MaxPitch => _maxPitch;

        private void Awake()
        {
            bool valid = true;
            if (_gestures == null) { Debug.LogError($"{name}: ClassmateOrbitCamera has no PointerGestureTracker.", this); valid = false; }
            if (_target == null) { Debug.LogError($"{name}: ClassmateOrbitCamera has no target body. Run Tools > DIYsect > Add Classmate Desktop Camera.", this); valid = false; }
            if (_minPitch > _maxPitch) { Debug.LogError($"{name}: ClassmateOrbitCamera min pitch is above max pitch.", this); valid = false; }
            if (_minDistance > _maxDistance) { Debug.LogError($"{name}: ClassmateOrbitCamera min distance is above max distance.", this); valid = false; }
            if (!valid)
            {
                enabled = false;
                return;
            }
            // Start from wherever the camera was placed, so the first frame does not jump.
            OrbitMath.AnglesFrom(_target.position, transform.position, out float yaw, out float pitch, out float distance);
            _goalYaw = _yaw = yaw;
            _goalPitch = _pitch = OrbitMath.ClampPitch(pitch, _minPitch, _maxPitch);
            _goalDistance = _distance = OrbitMath.ClampDistance(distance, _minDistance, _maxDistance);
        }

        // After the tracker's Update, so this frame's gestures are already known.
        private void LateUpdate()
        {
            Orbit(_gestures.FrameDrag);
            Zoom(_gestures.FrameZoom);
            Step(Time.unscaledDeltaTime);
        }

        /// <summary>Turns the goal by a drag in pixels.</summary>
        public void Orbit(Vector2 dragPixels)
        {
            _goalYaw = OrbitMath.DragYaw(_goalYaw, dragPixels.x, _degreesPerPixel);
            _goalPitch = OrbitMath.DragPitch(_goalPitch, dragPixels.y, _degreesPerPixel, _minPitch, _maxPitch);
        }

        /// <summary>Moves the goal closer (positive) or farther (negative) by zoom units.</summary>
        public void Zoom(float units)
        {
            if (units == 0f)
                return;
            _goalDistance = OrbitMath.Zoom(_goalDistance, units * _zoomPerUnit, _minDistance, _maxDistance);
        }

        /// <summary>Eases the drawn camera toward the goal and places the transform.</summary>
        public void Step(float deltaTime)
        {
            _yaw = OrbitMath.DampAngle(_yaw, _goalYaw, _damping, deltaTime);
            _pitch = Mathf.Lerp(_pitch, _goalPitch, OrbitMath.DampFactor(_damping, deltaTime));
            _distance = Mathf.Lerp(_distance, _goalDistance, OrbitMath.DampFactor(_damping, deltaTime));
            OrbitMath.Pose(_target.position, _yaw, _pitch, _distance, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Jumps straight to the goal with no easing. Used by tests and when the body is moved.</summary>
        public void SnapToGoal()
        {
            Step(float.MaxValue);
        }
    }
}
