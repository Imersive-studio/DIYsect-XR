// Authority: none. Local audio setting; the voice service carries the sound to the room.
using System;
using UnityEngine;
using UnityEngine.Android;
using XRMultiplayer;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// <see cref="IVoiceService"/> over the template's <see cref="VoiceChatManager"/>. The manager already joins the room's
    /// Vivox channel once the classmate is in the room and mutes the local player when it spawns; this only turns the
    /// microphone on and off after that, and asks Android for the microphone.
    /// Not covered by a test that makes sound: it needs the Vivox cloud service and a real microphone.
    /// </summary>
    public sealed class VoiceChatManagerService : IVoiceService
    {
        private readonly VoiceChatManager _voice;

        public VoiceChatManagerService(VoiceChatManager voice)
        {
            _voice = voice ?? throw new ArgumentNullException(nameof(voice));
            _voice.selfMuted.Subscribe(OnMutedChanged);
        }

        public event Action Changed;

        public bool IsMuted => _voice.selfMuted.Value;

        // The same check the manager makes in ToggleSelfMute, so the button and the manager never disagree.
        public bool HasMicPermission => Permission.HasUserAuthorizedPermission(Permission.Microphone);

        public void RequestMicPermission(Action<bool> done)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (HasMicPermission)
            {
                done(true);
                return;
            }
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => done(true);
            callbacks.PermissionDenied += _ => done(false);
            callbacks.PermissionDeniedAndDontAskAgain += _ => done(false);
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
            // Only Android asks in the app; other systems ask the first time a program opens the microphone.
            done(HasMicPermission);
#endif
        }

        public void SetMuted(bool muted)
        {
            _voice.ToggleSelfMute(true, muted);
        }

        /// <summary>Stops listening to the manager. Call when the button goes away.</summary>
        public void Release()
        {
            _voice.selfMuted.Unsubscribe(OnMutedChanged);
        }

        private void OnMutedChanged(bool muted)
        {
            Changed?.Invoke();
        }
    }
}
