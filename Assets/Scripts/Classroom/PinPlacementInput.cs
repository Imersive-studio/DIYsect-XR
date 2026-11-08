// Authority: none. This only asks the server for a pin through QuestionBoard; the server decides.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a desktop or phone classmate tap an organ to pin a question on it. A tap from the Input System's
    /// Pointer (mouse click or first touch) casts a ray from the camera through the pointer, finds which organ
    /// was hit and sends the hit point in that organ's local space
    /// together with the camera pose, so the professor can later see what the asker saw. The pin is placed when the press
    /// ends, and only if it did not move: a drag orbits the camera (<see cref="ClassmateOrbitCamera"/>) instead.
    /// There is no text entry yet: every pin carries <see cref="DefaultText"/> ("?"). Set <see cref="PendingText"/>
    /// from a UI field later to ask a real question.
    /// </summary>
    public class PinPlacementInput : MonoBehaviour
    {
        /// <summary>Text of a pin when nothing else was typed.</summary>
        public const string DefaultText = "?";

        [SerializeField] private QuestionBoard _board;
        [SerializeField, Tooltip("Camera that looks at the body. Leave empty to use the camera tagged MainCamera.")]
        private Camera _camera;
        [SerializeField, Min(0.1f)] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _hitLayers = ~0;

        private InputAction _point;
        private InputAction _press;
        private Vector2 _pressStart;
        private Vector2 _lastPressedPosition;
        private bool _pressCountsAsTap;

        /// <summary>Text sent with the next pin. Empty means <see cref="DefaultText"/>.</summary>
        public string PendingText { get; set; } = string.Empty;

        private void Awake()
        {
            if (_board == null)
            {
                Debug.LogError($"{name}: PinPlacementInput has no QuestionBoard assigned.", this);
                enabled = false;
                return;
            }
            // <Pointer> matches the mouse and the touchscreen, so one binding serves desktop and Android.
            _point = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
            _press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
        }

        private void OnEnable()
        {
            _point.Enable();
            _press.Enable();
            _press.performed += HandlePress;
            _press.canceled += HandleRelease;
        }

        private void OnDisable()
        {
            _press.performed -= HandlePress;
            _press.canceled -= HandleRelease;
            _press.Disable();
            _point.Disable();
        }

        private void OnDestroy()
        {
            _press?.Dispose();
            _point?.Dispose();
        }

        // A press only pins on release, and only if it stayed put. Dragging orbits the camera instead, and a second finger
        // (a pinch) cancels the tap.
        private void HandlePress(InputAction.CallbackContext context)
        {
            _pressStart = _point.ReadValue<Vector2>();
            _lastPressedPosition = _pressStart;
            _pressCountsAsTap = !IsPointerOverUi(context);
        }

        private void Update()
        {
            // The pointer position can reset once a touch ends, so the last position while pressed is kept for the release.
            if (_press.IsPressed())
                _lastPressedPosition = _point.ReadValue<Vector2>();
            if (_pressCountsAsTap && Touchscreen.current != null)
            {
                int fingers = 0;
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.isPressed)
                        fingers++;
                if (fingers > 1)
                    _pressCountsAsTap = false;
            }
        }

        private void HandleRelease(InputAction.CallbackContext context)
        {
            if (!_pressCountsAsTap)
                return;
            _pressCountsAsTap = false;
            Vector2 release = _lastPressedPosition;
            if (!ClassmateOrbitMath.IsTap(_pressStart, release, ClassmateOrbitCamera.TapThresholdPixels))
                return;
            Camera view = ResolveCamera();
            if (view == null)
                return;
            TryRequestPin(view.ScreenPointToRay(release));
        }

        private Camera ResolveCamera()
        {
            return _camera != null ? _camera : Camera.main;
        }

        // A press on a screen-space panel must not also pin the organ behind it.
        // A touch is asked by its touch id; the mouse is asked with the module's own pointer.
        private static bool IsPointerOverUi(InputAction.CallbackContext context)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;
            if (context.control.device is Touchscreen touchscreen)
                return eventSystem.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
            return eventSystem.IsPointerOverGameObject();
        }

        /// <summary>Casts the ray and, if it hits an organ, asks the server for a pin there. Returns true if it asked.</summary>
        public bool TryRequestPin(Ray ray)
        {
            if (!_board.IsSpawned)
                return false;
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hitLayers, QueryTriggerInteraction.Ignore))
                return false;
            int organIndex = FindOrganIndex(hit.collider.transform);
            if (organIndex < 0)
                return false;

            Transform organ = _board.Body.Organs[organIndex].transform;
            string text = string.IsNullOrWhiteSpace(PendingText) ? DefaultText : PendingText;
            Vector3 organLocalPoint = organ.InverseTransformPoint(hit.point);
            Camera view = ResolveCamera();
            if (view != null)
                _board.RequestPin(organIndex, organLocalPoint, text, view.transform.position, view.transform.rotation);
            else
                _board.RequestPin(organIndex, organLocalPoint, text);
            return true;
        }

        private int FindOrganIndex(Transform hit)
        {
            var organs = _board.Body.Organs;
            for (int i = 0; i < organs.Count; i++)
                if (hit.IsChildOf(organs[i].transform))
                    return i;
            return -1;
        }
    }
}
