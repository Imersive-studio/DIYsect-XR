// Authority: none. This only asks the server for a pin through QuestionBoard; the server decides.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a desktop or phone classmate tap an organ to pin a question on it. A press from the Input System's
    /// Pointer (mouse click or first touch) casts a ray from the camera through the pointer, finds which organ
    /// was hit and sends the hit point in that organ's local space
    /// together with the camera pose, so the professor can later see what the asker saw.
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
        }

        private void OnDisable()
        {
            _press.performed -= HandlePress;
            _press.Disable();
            _point.Disable();
        }

        private void OnDestroy()
        {
            _press?.Dispose();
            _point?.Dispose();
        }

        private void HandlePress(InputAction.CallbackContext context)
        {
            if (IsPointerOverUi(context))
                return;
            Camera view = ResolveCamera();
            if (view == null)
                return;
            TryRequestPin(view.ScreenPointToRay(_point.ReadValue<Vector2>()));
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
