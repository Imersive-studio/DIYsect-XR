// Authority: none. Moves only this client's own camera; nothing is sent over the network.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a classmate look around the body: drag one finger (or the mouse) to orbit it, pinch (or scroll) to zoom.
    /// The orbit keeps the view the camera starts with and pivots on the point the camera is looking at, so the first
    /// frame looks exactly as it did before. A short press without movement is left to <see cref="PinPlacementInput"/>,
    /// which pins a question on a tap.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ClassmateOrbitCamera : MonoBehaviour
    {
        [SerializeField, Tooltip("What to orbit. Empty means the middle of the body, found through the QuestionBoard.")]
        private Transform _target;
        [SerializeField, Min(0.01f)] private float _degreesPerPixel = 0.25f;
        [SerializeField, Range(0f, 89f)] private float _minPitch = 8f;
        [SerializeField, Range(0f, 89f)] private float _maxPitch = 85f;
        [SerializeField, Min(0.05f)] private float _minDistance = 0.4f;
        [SerializeField, Min(0.1f)] private float _maxDistance = 4f;

        private Vector3 _pivot;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private float _previousFingerGap;

        /// <summary>Pixels a press may move and still count as a tap. Shared with <see cref="PinPlacementInput"/>.</summary>
        public static float TapThresholdPixels => Mathf.Max(10f, Screen.dpi * 0.04f);

        private void Awake()
        {
            Vector3 center = FindBodyCenter();
            // Pivot on the body's centre as seen along the camera's own axis, so the camera keeps its starting view.
            Vector3 forward = transform.forward;
            float along = Mathf.Max(_minDistance, Vector3.Dot(center - transform.position, forward));
            _pivot = transform.position + forward * along;
            ClassmateOrbitMath.FromPosition(_pivot, transform.position, out _yaw, out _pitch, out _distance);
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
            _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void LateUpdate()
        {
            bool moved = false;
            var touches = Touch.activeTouches;
            if (touches.Count >= 2)
            {
                float gap = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
                if (_previousFingerGap > 0f)
                {
                    _distance = ClassmateOrbitMath.ApplyPinch(_distance, _previousFingerGap, gap, _minDistance, _maxDistance);
                    moved = true;
                }
                _previousFingerGap = gap;
            }
            else
            {
                _previousFingerGap = 0f;
                Pointer pointer = Pointer.current;
                if (pointer != null && pointer.press.isPressed)
                {
                    Vector2 drag = pointer.delta.ReadValue();
                    if (drag.sqrMagnitude > 0f)
                    {
                        Vector2 next = ClassmateOrbitMath.ApplyDrag(_yaw, _pitch, drag, _degreesPerPixel, _minPitch, _maxPitch);
                        _yaw = next.x;
                        _pitch = next.y;
                        moved = true;
                    }
                }
                Mouse mouse = Mouse.current;
                if (mouse != null && Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f)
                {
                    _distance = ClassmateOrbitMath.ApplyScroll(_distance, mouse.scroll.ReadValue().y, 0.1f, _minDistance, _maxDistance);
                    moved = true;
                }
            }

            if (moved)
            {
                Pose pose = ClassmateOrbitMath.PoseAround(_pivot, _yaw, _pitch, _distance);
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        private Vector3 FindBodyCenter()
        {
            Transform root = _target;
            if (root == null)
            {
                QuestionBoard board = FindAnyObjectByType<QuestionBoard>();
                if (board != null)
                    root = board.transform;
            }
            if (root == null)
                return transform.position + transform.forward * 1f;

            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);
            return bounds.center;
        }
    }
}
