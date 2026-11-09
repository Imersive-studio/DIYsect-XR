// Authority: none. This only moves the classmate's own camera on their own screen; nothing is sent over the network.
using UnityEngine;
using UnityEngine.InputSystem;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a desktop or phone classmate walk around the shared body. Dragging (left mouse or one finger) turns the camera
    /// around the body, the scroll wheel or a two-finger pinch moves it closer or farther. It reads gestures from a
    /// <see cref="PointerGestureTracker"/> and moves this object's transform, so anything that reads the camera's pose
    /// (such as the viewpoint a pin stores) sees where the camera really is now.
    /// On a phone, tilting the device also turns the view a little (a "magic window", see <see cref="GyroLookRule"/>), on top of the orbit.
    /// It is measured from the attitude when the view starts, and it re-centres when the player drags, resets the view or follows someone.
    /// With no attitude sensor (a laptop) it does nothing.
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

        [Header("Phone tilt look-around")]
        [SerializeField, Tooltip("Tilting the phone turns the view a little. On by default; does nothing without an attitude sensor.")]
        private bool _gyroLook = true;
        [SerializeField, Range(0f, 45f)] private float _gyroMaxYaw = GyroLookRule.DefaultMaxYaw;
        [SerializeField, Range(0f, 30f)] private float _gyroMaxPitch = GyroLookRule.DefaultMaxPitch;
        [SerializeField, Range(0f, 5f), Tooltip("Tilt smaller than this many degrees is ignored, so a steady hand does not wobble the view.")]
        private float _gyroDeadZone = GyroLookRule.DefaultDeadZone;
        [SerializeField, Min(0.1f), Tooltip("Higher is snappier. Also how fast the view re-centres.")]
        private float _gyroSmoothing = 6f;

        private AttitudeSensor _sensor;
        private bool _enabledSensorMyself;
        private bool _gyroNeedsReference = true;
        private Quaternion _gyroReference = Quaternion.identity;
        private Vector2 _gyroOffset;

        // Where the player wants the camera, and where it is drawn this frame.
        private float _goalYaw, _goalPitch, _goalDistance;
        private float _yaw, _pitch, _distance;
        // The framing the camera started with, so the player can always get back to it.
        private float _startYaw, _startPitch, _startDistance;

        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public float Distance => _distance;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public float MinPitch => _minPitch;

        /// <summary>
        /// True while something else (the follow camera) places the camera. The orbit state is kept as it was, drag and zoom are
        /// ignored, and <see cref="MyPosition"/> and <see cref="MyRotation"/> still say where my view is, so it can be restored.
        /// </summary>
        public bool Driven { get; set; }

        /// <summary>Where my own orbit view puts the camera now, whether or not the transform follows it.</summary>
        public Vector3 MyPosition { get; private set; }

        /// <summary>Which way my own orbit view looks now.</summary>
        public Quaternion MyRotation { get; private set; } = Quaternion.identity;
        public float MaxPitch => _maxPitch;

        /// <summary>The (yaw, pitch) in degrees that the phone's tilt adds to the view right now. Zero without a sensor or while following.</summary>
        public Vector2 GyroOffset => _gyroOffset;

        /// <summary>Turns the tilt look-around on or off (it is on by default).</summary>
        public bool GyroLook
        {
            get => _gyroLook;
            set
            {
                _gyroLook = value;
                _gyroNeedsReference = true;
                if (!value)
                    _gyroOffset = Vector2.zero;
            }
        }

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
            _startYaw = _goalYaw;
            _startPitch = _goalPitch;
            _startDistance = _goalDistance;
        }

        private void OnEnable()
        {
            FindSensor();
            InputSystem.onDeviceChange += HandleDeviceChange;
            _gyroNeedsReference = true;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
            ReleaseSensor();
        }

        // A phone has one attitude sensor. It is looked for by device, not through AttitudeSensor.current, which stays empty until the first reading.
        private void FindSensor()
        {
            if (_sensor != null)
                return;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is AttitudeSensor found)
                {
                    _sensor = found;
                    break;
                }
            }
            if (_sensor != null && !_sensor.enabled)
            {
                InputSystem.EnableDevice(_sensor);
                _enabledSensorMyself = true;
            }
        }

        private void ReleaseSensor()
        {
            if (_sensor != null && _enabledSensorMyself && _sensor.added)
                InputSystem.DisableDevice(_sensor);
            _sensor = null;
            _enabledSensorMyself = false;
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (!(device is AttitudeSensor))
                return;
            if (change == InputDeviceChange.Added)
                FindSensor();
            else if (change == InputDeviceChange.Removed && device == _sensor)
            {
                _enabledSensorMyself = false; // the device is gone, so there is nothing to switch off
                ReleaseSensor();
            }
        }

        // After the tracker's Update, so this frame's gestures are already known.
        private void LateUpdate()
        {
            bool dragged = false;
            // While another script drives the camera, drag and zoom change nothing.
            if (!Driven)
            {
                dragged = _gestures.FrameDrag != Vector2.zero;
                Orbit(_gestures.FrameDrag);
                Zoom(_gestures.FrameZoom);
            }
            UpdateGyro(Time.unscaledDeltaTime, Driven || dragged);
            Step(Time.unscaledDeltaTime);
        }

        // Measures the tilt since the reference attitude and eases the view toward it. While the player drags or the camera follows
        // someone, the reference is taken again each frame, so the view glides back to centre and the new pose is the new zero.
        private void UpdateGyro(float deltaTime, bool recentre)
        {
            bool available = _gyroLook && _sensor != null && _sensor.enabled;
            if (!available)
            {
                _gyroOffset = GyroLookRule.Smooth(_gyroOffset, Vector2.zero, _gyroSmoothing, deltaTime);
                _gyroNeedsReference = true;
                return;
            }
            Quaternion attitude = _sensor.attitude.ReadValue();
            // Before its first reading a sensor reports all zeros. That is not an attitude, so wait for a real one.
            if (!GyroLookRule.IsUsable(attitude))
            {
                _gyroNeedsReference = true;
                return;
            }
            if (recentre || _gyroNeedsReference)
            {
                _gyroReference = attitude;
                _gyroNeedsReference = false;
            }
            Vector2 target = GyroLookRule.Limit(GyroLookRule.TiltSince(_gyroReference, attitude), _gyroMaxYaw, _gyroMaxPitch, _gyroDeadZone);
            _gyroOffset = GyroLookRule.Smooth(_gyroOffset, target, _gyroSmoothing, deltaTime);
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
            rotation = GyroLookRule.Apply(rotation, _gyroOffset);
            MyPosition = position;
            MyRotation = rotation;
            if (!Driven)
                transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>Sends the goal back to the starting framing; the camera eases there.</summary>
        public void ResetView()
        {
            _goalYaw = _startYaw;
            _goalPitch = _startPitch;
            _goalDistance = _startDistance;
            _gyroNeedsReference = true; // the view is centred again, so the phone's current tilt is the new zero
        }

        /// <summary>Jumps straight to the goal with no easing. Used by tests and when the body is moved.</summary>
        public void SnapToGoal()
        {
            Step(float.MaxValue);
        }
    }
}
