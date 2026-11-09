using System;

namespace DIYsect.ClassmateControls.Tests
{
    /// <summary>A voice service with no Vivox and no microphone. A test answers the permission dialog by hand.</summary>
    public sealed class FakeVoiceService : IVoiceService
    {
        private Action<bool> _pending;

        public bool IsMuted { get; private set; } = true;
        public bool HasMicPermission { get; set; }
        public int PermissionRequests { get; private set; }

        /// <summary>Whether the professor has allowed this person to speak. Allowed by default, so older tests are unaffected.</summary>
        public bool MayUnmute { get; set; } = true;

        /// <summary>How many times the person asked the professor for permission.</summary>
        public int SpeakRequests { get; private set; }

        public void RequestToSpeak()
        {
            SpeakRequests++;
        }

        public event Action Changed;

        public void RequestMicPermission(Action<bool> done)
        {
            PermissionRequests++;
            _pending = done;
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            Changed?.Invoke();
        }

        /// <summary>The person answers the dialog.</summary>
        public void Answer(bool allowed)
        {
            HasMicPermission = allowed;
            Action<bool> done = _pending;
            _pending = null;
            done?.Invoke(allowed);
        }
    }
}
