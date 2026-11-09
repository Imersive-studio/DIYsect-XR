// Authority: none. This only asks the server for a pin through QuestionBoard; the server decides.
using System.Collections.Generic;
using UnityEngine;
using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a desktop or phone classmate press and hold on an organ to pin a question on it. A hold from the PointerGestureTracker
    /// (mouse button or one finger held still for about half a second, not a drag, not a quick tap) casts a ray from the camera through the pointer, finds which organ
    /// was hit and sends the hit point in that organ's local space
    /// together with the camera pose, so the professor can later see what the asker saw.
    /// There is no text entry yet: every pin carries <see cref="DefaultText"/> ("?"). Set <see cref="PendingText"/>
    /// from a UI field later to ask a real question.
    /// A press that another feature claims (see <see cref="ITapClaim"/>, for example a running quiz round) places no pin.
    /// A quick tap does not pin: it shows the organ's name instead (OrganNameTapInput).
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
        [SerializeField, Tooltip("Reports holds; a drag (orbiting the camera) and a quick tap are never reported, so they cannot pin.")]
        private PointerGestureTracker _gestures;
        [SerializeField, Tooltip("Behaviours that implement ITapClaim. While any of them claims the tap, no pin is placed (for example the quiz answer input).")]
        private MonoBehaviour[] _tapClaimers = new MonoBehaviour[0];

        private List<ITapClaim> _claimers = new List<ITapClaim>();

        /// <summary>Text sent with the next pin. Empty means <see cref="DefaultText"/>.</summary>
        public string PendingText { get; set; } = string.Empty;

        private void Awake()
        {
            bool valid = true;
            if (_board == null) { Debug.LogError($"{name}: PinPlacementInput has no QuestionBoard assigned.", this); valid = false; }
            if (_gestures == null) { Debug.LogError($"{name}: PinPlacementInput has no PointerGestureTracker assigned.", this); valid = false; }
            if (!valid)
                enabled = false;
            _claimers = TapClaims.Resolve(_tapClaimers, this);
        }

        // Shows a wrong assignment in the Inspector as soon as it is made.
        private void OnValidate()
        {
            TapClaims.Resolve(_tapClaimers, this);
        }

        private void OnEnable()
        {
            if (_gestures == null)
                return;
            _gestures.Held += HandleHold;
        }

        private void OnDisable()
        {
            if (_gestures != null)
                _gestures.Held -= HandleHold;
        }

        // Only a hold reaches here; the tracker has already ignored drags, quick taps and presses over UI.
        private void HandleHold(Vector2 screenPosition)
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

        /// <summary>Casts the ray and, if it hits an organ, asks the server for a pin there. Returns true if it asked. Returns false without asking while a tap claimer claims the tap.</summary>
        public bool TryRequestPin(Ray ray)
        {
            if (!_board.IsSpawned || TapClaims.AnyClaims(_claimers))
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
