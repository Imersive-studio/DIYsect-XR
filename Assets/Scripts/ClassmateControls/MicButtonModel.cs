using System;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// The rules of the Mute / Unmute button. A classmate starts muted. Pressing it while muted unmutes, after asking for the
    /// microphone the first time; if the person refuses, it stays muted and says so. Pressing it while live mutes again.
    /// Without the professor's permission the button asks for it; once a classmate who asked is allowed, they go live at once,
    /// so "raise hand, professor says yes, speak" needs no second tap.
    /// No Unity objects, so it is tested with a fake <see cref="IVoiceService"/>.
    /// </summary>
    public sealed class MicButtonModel
    {
        public const string UnmuteLabel = "Unmute";
        public const string MuteLabel = "Mute";
        public const string AskingLabel = "Allow mic…";
        public const string DeniedText = "Microphone is blocked. Allow it in Settings to talk.";
        public const string AskLabel = "Ask to speak";
        public const string AskedText = "Asked. Waiting for the professor to let you speak.";
        public const string LiveText = "The professor let you speak. You are live.";

        private readonly IVoiceService _voice;
        private bool _asked;

        public MicButtonModel(IVoiceService voice)
        {
            _voice = voice ?? throw new ArgumentNullException(nameof(voice));
        }

        /// <summary>Raised when the label or message changes.</summary>
        public event Action Changed;

        /// <summary>True when others can hear this classmate.</summary>
        public bool IsLive => !_voice.IsMuted && _voice.HasMicPermission && _voice.MayUnmute;

        /// <summary>True while the permission dialog is open.</summary>
        public bool Asking { get; private set; }

        /// <summary>A short note for the classmate, or empty.</summary>
        public string Message { get; private set; } = string.Empty;

        public string Label => Asking ? AskingLabel : IsLive ? MuteLabel : _voice.MayUnmute ? UnmuteLabel : AskLabel;

        /// <summary>The button was pressed.</summary>
        public void Press()
        {
            if (Asking)
                return;
            Message = string.Empty;
            if (IsLive)
            {
                _voice.SetMuted(true);
            }
            else if (!_voice.MayUnmute)
            {
                // Nobody speaks without the professor's permission: the button asks for it instead.
                _voice.SetMuted(true);
                _voice.RequestToSpeak();
                _asked = true;
                Message = AskedText;
            }
            else if (_voice.HasMicPermission)
            {
                _voice.SetMuted(false);
            }
            else
            {
                Asking = true;
                _voice.RequestMicPermission(OnPermission);
            }
            Changed?.Invoke();
        }

        /// <summary>Tells the model the voice service changed on its own (for example a mute from elsewhere).</summary>
        public void Notify()
        {
            // The professor took the permission away while the microphone was open: close it.
            if (!_voice.MayUnmute && !_voice.IsMuted)
                _voice.SetMuted(true);
            // The classmate asked and the professor just said yes: open the microphone (asking Android for it the first time).
            if (_asked && _voice.MayUnmute && !Asking)
            {
                _asked = false;
                if (_voice.HasMicPermission)
                {
                    _voice.SetMuted(false);
                    Message = LiveText;
                }
                else
                {
                    Asking = true;
                    _voice.RequestMicPermission(OnPermission);
                }
            }
            Changed?.Invoke();
        }

        private void OnPermission(bool granted)
        {
            Asking = false;
            if (granted && _voice.MayUnmute)
                _voice.SetMuted(false);
            else
                Message = DeniedText;
            Changed?.Invoke();
        }
    }
}
