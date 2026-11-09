// Authority: none. This only sends the classmate's answer to the server through QuizRound; the server decides.
using UnityEngine;
using DIYsect.Classroom;

namespace DIYsect.Learning
{
    /// <summary>
    /// Lets a desktop or phone classmate answer a quiz round by tapping an organ. A tap from the PointerGestureTracker
    /// (not a drag, so orbiting still works) casts a ray from the camera through the pointer and sends the organ it hits.
    /// It sends one answer per round; the server also refuses a second one. While a round is running
    /// <see cref="ClaimsTap"/> is true, which a question-pin input can read to stand aside. PinPlacementInput does not
    /// read it yet, so for now a tap during a round also drops a "?" pin.
    /// The ray comes from whatever camera is looking, so it works in every view mode.
    /// </summary>
    public class QuizAnswerInput : MonoBehaviour
    {
        [SerializeField] private QuizRound _quiz;
        [SerializeField, Tooltip("Reports taps; a drag is never reported, so orbiting cannot answer.")]
        private PointerGestureTracker _gestures;
        [SerializeField, Tooltip("Camera that looks at the body. Leave empty to use the camera tagged MainCamera.")]
        private Camera _camera;
        [SerializeField, Min(0.1f)] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _hitLayers = ~0;

        private int _answeredRound = -1;

        /// <summary>True while a round is running: taps are answers, not question pins.</summary>
        public bool ClaimsTap => _quiz != null && _quiz.IsSpawned && _quiz.Phase == QuizPhase.Asking;

        /// <summary>True when this client has already sent an answer for the running round.</summary>
        public bool HasAnsweredThisRound => _quiz != null && _quiz.Round > 0 && _answeredRound == _quiz.Round;

        private void Awake()
        {
            bool valid = true;
            if (_quiz == null) { Debug.LogError($"{name}: QuizAnswerInput has no QuizRound. Run Tools > DIYsect > Add Quiz.", this); valid = false; }
            if (_gestures == null) { Debug.LogError($"{name}: QuizAnswerInput has no PointerGestureTracker. Run Tools > DIYsect > Add Quiz.", this); valid = false; }
            if (!valid)
                enabled = false;
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
                TryAnswer(view.ScreenPointToRay(screenPosition));
        }

        /// <summary>Casts the ray and, if a round is running and it hits an organ, sends that organ as the answer. Returns true if it sent one.</summary>
        public bool TryAnswer(Ray ray)
        {
            if (!ClaimsTap || HasAnsweredThisRound)
                return false;
            if (!QuizOrganRaycast.TryHit(ray, _quiz.Body, _maxDistance, _hitLayers, out int organIndex))
                return false;
            _answeredRound = _quiz.Round;
            _quiz.SubmitAnswer(organIndex);
            return true;
        }
    }
}
