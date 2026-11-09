// Authority: none. It only sends the student's answer to the server through QuizRound; the server judges it.
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using DIYsect.Interaction;

namespace DIYsect.Learning
{
    /// <summary>
    /// Lets the student in the headset take the professor's quiz. While a round is asking, a small card beyond the table
    /// shows the question; the student points either controller at an organ and pulls that controller's trigger to answer.
    /// One answer per round (the server also refuses a second). The card then says whether it was right, and shows the
    /// answer once the professor reveals it. The card is hidden while no round is running.
    /// The component is saved switched off and the shared scene's role switch turns it on only on the student's headset
    /// (Tools > DIYsect > Add Quiz, then Build Shared Scene). Grip still grabs, so answering never lifts an organ.
    /// </summary>
    public class QuizHeadsetAnswer : MonoBehaviour
    {
        [SerializeField] private QuizRound _quiz;
        [SerializeField, Tooltip("Where the left controller's ray starts. It points along its forward.")]
        private Transform _leftAim;
        [SerializeField, Tooltip("Where the right controller's ray starts. It points along its forward.")]
        private Transform _rightAim;
        [SerializeField, Tooltip("The card that shows the question. Hidden while no round is running.")]
        private GameObject _card;
        [SerializeField] private TMP_Text _question;
        [SerializeField] private TMP_Text _status;
        [SerializeField, Min(0.1f)] private float _maxDistance = 6f;
        [SerializeField] private LayerMask _hitLayers = ~0;

        private InputAction _leftTrigger;
        private InputAction _rightTrigger;
        private int _answeredRound = -1;
        private int _shownRound = -1;
        private string _feedback = string.Empty;
        private bool _missed;

        /// <summary>True while the professor's round is asking.</summary>
        public bool IsAsking => _quiz != null && _quiz.IsSpawned && _quiz.Phase == QuizPhase.Asking;

        /// <summary>True when this headset has already sent an answer for the running round.</summary>
        public bool HasAnsweredThisRound => _quiz != null && _quiz.Round > 0 && _answeredRound == _quiz.Round;

        private void Awake()
        {
            if (_quiz == null || _card == null || _question == null || _status == null || (_leftAim == null && _rightAim == null))
            {
                Debug.LogError($"{name}: QuizHeadsetAnswer needs a QuizRound, its card and two texts, and at least one controller. Run Tools > DIYsect > Add Quiz.", this);
                enabled = false;
                return;
            }
            // XR controllers show up as XRController devices tagged with their hand, as in AssistRequestInput.
            _leftTrigger = new InputAction("QuizAnswerLeft", InputActionType.Button);
            _leftTrigger.AddBinding("<XRController>{LeftHand}/{TriggerButton}");
            _rightTrigger = new InputAction("QuizAnswerRight", InputActionType.Button);
            _rightTrigger.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        }

        private void OnEnable()
        {
            // Awake found something missing; the role switch may still turn the component on, so stay quiet here.
            if (_leftTrigger == null)
                return;
            _quiz.StateChanged += Refresh;
            _quiz.LocalAnswerResolved += OnAnswerResolved;
            _leftTrigger.performed += OnLeftTrigger;
            _rightTrigger.performed += OnRightTrigger;
            _leftTrigger.Enable();
            _rightTrigger.Enable();
            Refresh();
        }

        private void OnDisable()
        {
            if (_leftTrigger == null)
                return;
            _leftTrigger.Disable();
            _rightTrigger.Disable();
            _leftTrigger.performed -= OnLeftTrigger;
            _rightTrigger.performed -= OnRightTrigger;
            _quiz.StateChanged -= Refresh;
            _quiz.LocalAnswerResolved -= OnAnswerResolved;
            if (_card != null)
                _card.SetActive(false);
        }

        private void OnDestroy()
        {
            _leftTrigger?.Dispose();
            _rightTrigger?.Dispose();
        }

        private void OnLeftTrigger(InputAction.CallbackContext context) => TryAnswerFrom(_leftAim);
        private void OnRightTrigger(InputAction.CallbackContext context) => TryAnswerFrom(_rightAim);

        /// <summary>Answers along a controller's ray. Returns true if an answer was sent.</summary>
        public bool TryAnswerFrom(Transform aim)
        {
            return aim != null && TryAnswer(new Ray(aim.position, aim.forward));
        }

        /// <summary>
        /// If a round is asking and this headset has not answered yet, sends the organ the ray hits. A ray that hits no
        /// organ only updates the card. Returns true if an answer was sent.
        /// </summary>
        public bool TryAnswer(Ray ray)
        {
            if (!IsAsking || HasAnsweredThisRound)
                return false;
            if (!QuizOrganRaycast.TryHit(ray, _quiz.Body, _maxDistance, _hitLayers, out int organIndex))
            {
                _missed = true;
                Refresh();
                return false;
            }
            _answeredRound = _quiz.Round;
            _missed = false;
            _feedback = string.Empty;
            _quiz.SubmitAnswer(organIndex);
            Refresh();
            return true;
        }

        private void OnAnswerResolved(int round, QuizResult result, bool correct)
        {
            if (round != _quiz.Round)
                return;
            _feedback = QuizMessages.ForAnswer(result, correct, NameOf(_quiz.TargetOrgan));
            Refresh();
        }

        private void Refresh()
        {
            QuizPhase phase = _quiz.IsSpawned ? _quiz.Phase : QuizPhase.Idle;
            // A new round forgets what the card said about the last one.
            if (_quiz.Round != _shownRound)
            {
                _shownRound = _quiz.Round;
                _feedback = string.Empty;
                _missed = false;
            }
            bool show = phase != QuizPhase.Idle && _quiz.TargetOrgan >= 0;
            if (_card.activeSelf != show)
                _card.SetActive(show);
            if (!show)
                return;
            string target = NameOf(_quiz.TargetOrgan);
            _question.text = phase == QuizPhase.Asking
                ? QuizMessages.Prompt(target, _quiz.Prompt)
                : QuizMessages.Ended(target, _quiz.RightCount, _quiz.WrongCount);
            _status.text = QuizMessages.HeadsetStatus(phase, HasAnsweredThisRound, _missed, _feedback);
        }

        private string NameOf(int organ)
        {
            var organs = _quiz.Body.Organs;
            return organ >= 0 && organ < organs.Count && organs[organ] != null ? OrganNames.Display(organs[organ].name) : string.Empty;
        }
    }
}
