// Authority: none. Local UI only; it asks the voice service to switch this classmate's own microphone.
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// The classmate's Mute / Unmute button. A classmate is muted until they press it; once unmuted, everyone in the room
    /// hears them through the room's voice channel. It is a UI element, so the gesture tracker ignores a press on it.
    /// </summary>
    public class ClassmateMicButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField, Tooltip("Short note, for example when the microphone is refused. Hidden when empty.")]
        private Text _message;
        [SerializeField, Tooltip("The template's voice manager. Leave empty in a test that supplies a service.")]
        private VoiceChatManager _voice;
        [SerializeField, Min(0.5f), Tooltip("Seconds a note stays on screen.")]
        private float _messageSeconds = 3.5f;

        private MicButtonModel _model;
        private IVoiceService _service;
        private float _hideMessageAt;
        private bool _lastMayUnmute;

        /// <summary>The text on the button.</summary>
        public string Label => _label.text;

        /// <summary>The note on screen, or empty.</summary>
        public string Message => _message.gameObject.activeSelf ? _message.text : string.Empty;

        /// <summary>True when the microphone is on and the classmate can be heard.</summary>
        public bool IsLive => _model != null && _model.IsLive;

        private void Awake()
        {
            bool valid = true;
            if (_button == null || _label == null) { Debug.LogError($"{name}: ClassmateMicButton has no button or label. Run Tools > DIYsect > Build Shared Scene.", this); valid = false; }
            if (_message == null) { Debug.LogError($"{name}: ClassmateMicButton has no message text.", this); valid = false; }
            if (!valid)
                enabled = false;
        }

        /// <summary>Replaces the voice service. Used by tests; the scene's button builds one from the voice manager.</summary>
        public void SetService(IVoiceService service)
        {
            UnhookModel();
            _service = service ?? throw new System.ArgumentNullException(nameof(service));
            _model = new MicButtonModel(_service);
            if (isActiveAndEnabled)
                HookModel();
        }

        private void OnEnable()
        {
            if (_service == null && _voice != null)
                _service = new VoiceChatManagerService(_voice);
            if (_service == null)
            {
                Debug.LogError($"{name}: ClassmateMicButton has no voice manager in the scene.", this);
                enabled = false;
                return;
            }
            _model ??= new MicButtonModel(_service);
            HookModel();
        }

        private void OnDisable()
        {
            UnhookModel();
        }

        private void OnDestroy()
        {
            (_service as VoiceChatManagerService)?.Release();
        }

        private void HookModel()
        {
            _button.onClick.AddListener(_model.Press);
            _model.Changed += Refresh;
            _service.Changed += _model.Notify;
            Refresh();
        }

        private void UnhookModel()
        {
            if (_model == null)
                return;
            // The button can be destroyed first when the scene unloads.
            if (_button != null)
                _button.onClick.RemoveListener(_model.Press);
            _model.Changed -= Refresh;
            if (_service != null)
                _service.Changed -= _model.Notify;
        }

        private void Update()
        {
            if (_message.gameObject.activeSelf && Time.unscaledTime >= _hideMessageAt)
                _message.gameObject.SetActive(false);
            // The professor's permission arrives over the network at any time, so watch it: the label changes from "Ask to speak"
            // to "Unmute" when it is given, and the microphone closes when it is taken away.
            bool mayUnmute = _service.MayUnmute;
            if (mayUnmute != _lastMayUnmute)
            {
                _lastMayUnmute = mayUnmute;
                _model.Notify();
            }
        }

        private void Refresh()
        {
            _label.text = _model.Label;
            _button.interactable = !_model.Asking;
            if (_model.Message.Length > 0)
            {
                _message.text = _model.Message;
                _message.gameObject.SetActive(true);
                _hideMessageAt = Time.unscaledTime + _messageSeconds;
            }
            else
            {
                _message.gameObject.SetActive(false);
            }
        }
    }
}
