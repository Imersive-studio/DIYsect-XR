// Authority: none. This only asks the server for a pin through QuestionBoard; the server decides.
using UnityEngine;
using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a desktop or phone classmate tap an organ to pin a question on it. A tap from the PointerGestureTracker
    /// (mouse click or one short finger touch, not a drag) casts a ray from the camera through the pointer, finds which organ
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
        [SerializeField, Tooltip("Reports taps; a drag (orbiting the camera) is never reported, so it cannot pin.")]
        private PointerGestureTracker _gestures;

        /// <summary>Text sent with the next pin. Empty means <see cref="DefaultText"/>.</summary>
        public string PendingText { get; set; } = string.Empty;

        private void Awake()
        {
            bool valid = true;
            if (_board == null) { Debug.LogError($"{name}: PinPlacementInput has no QuestionBoard assigned.", this); valid = false; }
            if (_gestures == null) { Debug.LogError($"{name}: PinPlacementInput has no PointerGestureTracker assigned.", this); valid = false; }
            if (!valid)
                enabled = false;
        }

        private void OnEnable()
        {
            if (_gestures == null)
                return;
            _gestures.Tapped += HandleTap;
        }

        private void OnDisable()
        {
            if (_gestures != null)
                _gestures.Tapped -= HandleTap;
        }

        // Only a tap reaches here; the tracker has already ignored drags and presses over UI.
        private void HandleTap(Vector2 screenPosition)
        {
            Camera view = ResolveCamera();
            if (view == null)
                return;
            TryRequestPin(view.ScreenPointToRay(screenPosition));
        }

        private Camera ResolveCamera()
        {
            return _camera != null ? _camera : Camera.main;
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
