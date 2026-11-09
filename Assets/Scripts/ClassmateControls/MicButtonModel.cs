using System;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// The rules of the Mute / Unmute button. A classmate starts muted. Pressing it while muted unmutes, after asking for the
    /// microphone the first time; if the person refuses, it stays muted and says so. Pressing it while live mutes again.
    /// No Unity objects, so it is tested with a fake <see cref="IVoiceService"/>.
    /// </summary>
    public sealed class MicButtonModel
    {
        public const string UnmuteLabel = "Unmute";
        public const string MuteLabel = "Mute";
        public const string AskingLabel = "Allow mic…";
        public const string DeniedText = "Microphone is blocked. Allow it in Settings to talk.";

        private readonly IVoiceService _voice;

        public MicButtonModel(IVoiceService voice)
        {
            _voice = voice ?? throw new ArgumentNullException(nameof(voice));
        }

        /// <summary>Raised when the label or message changes.</summary>
        public event Action Changed;

        /// <summary>True when others can hear this classmate.</summary>
        public bool IsLive => !_voice.IsMuted && _voice.HasMicPermission;

        /// <summary>True while the permission dialog is open.</summary>
        public bool Asking { get; private set; }

        /// <summary>A short note for the classmate, or empty.</summary>
        public string Message { get; private set; } = string.Empty;

        public string Label => Asking ? AskingLabel : IsLive ? MuteLabel : UnmuteLabel;

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
            Changed?.Invoke();
        }

        private void OnPermission(bool granted)
        {
            Asking = false;
            if (granted)
                _voice.SetMuted(false);
            else
                Message = DeniedText;
            Changed?.Invoke();
        }
    }
}
