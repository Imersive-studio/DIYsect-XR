// Authority: none. Presentation only; it shows what the server told this client and changes no shared state.
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Shows a short message on screen when the server refuses this client's pin request, then hides it.
    /// It listens to <see cref="QuestionBoard.LocalRequestRejected"/>, which only the asking client receives.
    /// </summary>
    public class PinRejectionToast : MonoBehaviour
    {
        [SerializeField] private QuestionBoard _board;
        [SerializeField, Tooltip("Text that shows the message. It is hidden when there is nothing to say.")]
        private Text _label;
        [SerializeField, Min(0.1f), Tooltip("Seconds the message stays visible.")]
        private float _seconds = 2.5f;

        private float _hideAt;

        /// <summary>True while a message is on screen.</summary>
        public bool IsShowing => _label != null && _label.gameObject.activeSelf;

        /// <summary>The message on screen, or empty.</summary>
        public string CurrentMessage => IsShowing ? _label.text : string.Empty;

        private void Awake()
        {
            if (_board == null || _label == null)
            {
                Debug.LogError($"{name}: PinRejectionToast needs a QuestionBoard and a Text. Run Tools > DIYsect > Add Classmate Desktop Camera.", this);
                enabled = false;
                return;
            }
            _label.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _board.LocalRequestRejected += Show;
        }

        private void OnDisable()
        {
            _board.LocalRequestRejected -= Show;
        }

        /// <summary>Shows the message for a reason now and restarts the timer.</summary>
        public void Show(PinRejection reason)
        {
            _label.text = PinRejectionMessages.For(reason);
            _label.gameObject.SetActive(true);
            _hideAt = Time.unscaledTime + _seconds;
        }

        private void Update()
        {
            if (IsShowing && Time.unscaledTime >= _hideAt)
                _label.gameObject.SetActive(false);
        }
    }
}
