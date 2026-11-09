// Authority: none. Local UI only; it asks the joiner to join and changes no shared state.
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// The first thing a classmate sees on a phone or laptop: a full-screen page with a big "Join the room" button and a box
    /// for a room code. Nothing is joined until they press one of them; an invite link skips the page. It hides itself
    /// once the classmate is in the room. The screen is hidden whenever the joiner is not waiting for a press, which is also
    /// the case in the Unity editor and in scenes that join on their own.
    /// </summary>
    public class ClassmateStartScreen : MonoBehaviour
    {
        public const string JoinLabel = "Join the room";
        public const string JoiningLabel = "Joining…";
        public const string BadCodeText = "A room code has 6 letters and digits.";
        public const string RejectedCodeText = "No room found with that code.";

        [SerializeField, Tooltip("The page that is shown and hidden.")]
        private GameObject _root;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Text _joinLabel;
        [SerializeField] private InputField _codeField;
        [SerializeField] private Button _codeButton;
        [SerializeField, Tooltip("Says what went wrong with a typed code.")]
        private Text _message;
        [SerializeField, Tooltip("Provides the joiner. Leave a ClassmateJoinAdapter here.")]
        private MonoBehaviour _joiner;
        [SerializeField, Tooltip("The corner status line. Its own code box is switched off while this page is up, because the page has one.")]
        private ClassmateStatusLabel _statusLabel;

        private readonly StartScreenModel _model = new StartScreenModel();
        private IClassmateJoiner _service;
        private string _typedMessage = string.Empty;

        /// <summary>True while the page is on screen.</summary>
        public bool IsShowing => _root != null && _root.activeSelf;

        public StartScreenPhase Phase => _model.Phase;

        private void Awake()
        {
            bool valid = true;
            if (_root == null) { Debug.LogError($"{name}: ClassmateStartScreen has no page. Run Tools > DIYsect > Build Shared Scene.", this); valid = false; }
            if (_joinButton == null || _joinLabel == null) { Debug.LogError($"{name}: ClassmateStartScreen has no Join button or label.", this); valid = false; }
            if (_codeField == null || _codeButton == null) { Debug.LogError($"{name}: ClassmateStartScreen has no code field or button.", this); valid = false; }
            if (_message == null) { Debug.LogError($"{name}: ClassmateStartScreen has no message text.", this); valid = false; }
            if (_joiner == null && _service == null) { Debug.LogError($"{name}: ClassmateStartScreen has no joiner.", this); valid = false; }
            if (!valid)
            {
                enabled = false;
                if (_root != null)
                    _root.SetActive(false);
                return;
            }
            if (_service == null)
                _service = _joiner as IClassmateJoiner;
            if (_service == null)
            {
                Debug.LogError($"{name}: {_joiner.GetType().Name} does not implement IClassmateJoiner.", this);
                enabled = false;
                _root.SetActive(false);
                return;
            }
            _root.SetActive(false);
        }

        /// <summary>Replaces the joiner. Used by tests, so no lobby service is needed.</summary>
        public void SetJoiner(IClassmateJoiner service)
        {
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
        }

        private void OnEnable()
        {
            if (_joinButton == null)
                return;
            _joinButton.onClick.AddListener(PressJoin);
            _codeButton.onClick.AddListener(PressCodeJoin);
            Refresh();
        }

        private void OnDisable()
        {
            // The buttons can be destroyed first when the scene unloads.
            if (_joinButton != null) _joinButton.onClick.RemoveListener(PressJoin);
            if (_codeButton != null) _codeButton.onClick.RemoveListener(PressCodeJoin);
            if (_statusLabel != null)
            {
                _statusLabel.ShowCodeEntry = true;
                _statusLabel.ShowStatusLine = true;
            }
            DIYsect.Learning.ChecklistHud.Suppressed = false;
        }

        // Three comparisons per frame; the layout is only touched when the phase or the message changes.
        private void Update()
        {
            Refresh();
        }

        /// <summary>The big button: join the first public room.</summary>
        public void PressJoin()
        {
            _typedMessage = string.Empty;
            _model.Press();
            _service.PressJoin();
            Refresh();
        }

        /// <summary>The code button: join the room with the typed code.</summary>
        public void PressCodeJoin()
        {
            if (!_service.JoinWithCode(_codeField.text))
            {
                _typedMessage = BadCodeText;
                Refresh();
                return;
            }
            _typedMessage = string.Empty;
            _model.Press();
            Refresh();
        }

        private void Refresh()
        {
            StartScreenPhase phase = _model.Update(_service.WaitingForPress, _service.Connected, _service.CodeRejected);
            bool show = phase != StartScreenPhase.Hidden;
            if (_root.activeSelf != show)
                _root.SetActive(show);
            if (_statusLabel != null)
            {
                _statusLabel.ShowCodeEntry = !show;
                _statusLabel.ShowStatusLine = !show;
            }
            // The page is a plain full-screen background. Nothing else may draw over it.
            // The checklist is about the dissection in the room, so it only shows once the classmate is in the room.
            DIYsect.Learning.ChecklistHud.Suppressed = show || !_service.Connected;
            if (!show)
                return;

            bool joining = phase == StartScreenPhase.Joining && !_model.CodeRejected;
            _joinButton.interactable = !joining;
            _codeButton.interactable = !joining;
            _joinLabel.text = joining ? JoiningLabel : JoinLabel;
            string message = _typedMessage.Length > 0 ? _typedMessage : _model.CodeRejected ? RejectedCodeText : string.Empty;
            if (_message.text != message)
                _message.text = message;
        }
    }
}
