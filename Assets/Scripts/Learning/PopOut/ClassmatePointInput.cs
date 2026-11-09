// Authority: none here. It is a local input bridge; PointerBoard on the server decides what everyone sees.
using System.Collections.Generic;
using DIYsect.Classroom;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// "Look here" for a classmate on a phone: a quick tap on an organ asks the server to show that organ to everybody
    /// (<see cref="PointerBoard.PointAsClassmate"/>); tapping the same organ again, or empty space, stops it.
    /// It listens to the same quick taps as <see cref="OrganNameTapInput"/>, so one tap also shows the local name card; a drag
    /// (orbit) and a hold (question pin) are never taps. While a quiz round runs the tap is an answer (<see cref="ITapClaim"/>), so it
    /// points at nothing, because pointing would give the answer away.
    /// This lives on the classmate camera, which the role switch only turns on for a phone or laptop classmate.
    /// </summary>
    public class ClassmatePointInput : MonoBehaviour
    {
        [SerializeField] private SharedBody _body;
        [SerializeField] private PointerBoard _board;
        [SerializeField, Tooltip("Reports taps; a drag and a hold are never reported as taps.")]
        private PointerGestureTracker _gestures;
        [SerializeField, Tooltip("Camera that looks at the body. Leave empty to use the camera tagged MainCamera.")]
        private Camera _camera;
        [SerializeField, Min(0.1f)] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _hitLayers = ~0;
        [SerializeField, Tooltip("Behaviours that implement ITapClaim. While any of them claims the tap, nothing is pointed at (for example the quiz answer input).")]
        private MonoBehaviour[] _tapClaimers = new MonoBehaviour[0];

        private List<ITapClaim> _claimers = new List<ITapClaim>();

        private void Awake()
        {
            bool valid = true;
            if (_body == null) { Debug.LogError($"{name}: ClassmatePointInput has no SharedBody. Run Tools > DIYsect > Build Shared Scene.", this); valid = false; }
            if (_board == null) { Debug.LogError($"{name}: ClassmatePointInput has no PointerBoard. Run Tools > DIYsect > Build Shared Scene.", this); valid = false; }
            if (_gestures == null) { Debug.LogError($"{name}: ClassmatePointInput has no PointerGestureTracker.", this); valid = false; }
            if (!valid)
            {
                enabled = false;
                return;
            }
            _claimers = TapClaims.Resolve(_tapClaimers, this);
        }

        // Shows a wrong assignment in the Inspector as soon as it is made.
        private void OnValidate()
        {
            TapClaims.Resolve(_tapClaimers, this);
        }

        private void OnEnable()
        {
            if (_gestures != null)
                _gestures.Tapped += HandleTap;
        }

        private void OnDisable()
        {
            if (_gestures != null)
                _gestures.Tapped -= HandleTap;
        }

        private void HandleTap(Vector2 screenPosition)
        {
            Camera view = _camera != null ? _camera : Camera.main;
            if (view != null)
                TryPoint(view.ScreenPointToRay(screenPosition));
        }

        /// <summary>Casts the ray: an organ is pointed at (or released on a second tap), empty space stops the pointer. Public so tests can call it.</summary>
        public void TryPoint(Ray ray)
        {
            if (TapClaims.AnyClaims(_claimers))
                return;
            if (QuizOrganRaycast.TryHit(ray, _body, _maxDistance, _hitLayers, out int organIndex))
                _board.PointAsClassmate(organIndex);
            else if (_board.OrganOf(PointerRole.Classmate) != PointerBoard.None)
                _board.StopClassmate();
        }
    }
}
