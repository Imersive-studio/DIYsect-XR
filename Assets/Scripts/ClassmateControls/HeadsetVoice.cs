// Authority: none. Local audio setting; the voice service carries the sound to the room.
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// Opens the professor's and the student's microphone once they are in the room's voice channel, so the class hears them.
    /// The template mutes every player as they join and its unmute button lives in a menu this app hides. Event driven: it
    /// reacts to the room, the microphone permission, the voice status and the mute state. Does nothing on a phone.
    /// </summary>
    [RequireComponent(typeof(VoiceChatManager))]
    public class HeadsetVoice : MonoBehaviour
    {
        private VoiceChatManager _voice;

        private void Awake()
        {
            _voice = GetComponent<VoiceChatManager>();
        }

        private void OnEnable()
        {
            XRINetworkGameManager.Connected.Subscribe(OnChanged);
            VoiceChatManager.s_HasMicrophonePermission.Subscribe(OnChanged);
            _voice.connectionStatus.Subscribe(OnStatus);
            _voice.selfMuted.Subscribe(OnChanged);
        }

        private void OnDisable()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnChanged);
            VoiceChatManager.s_HasMicrophonePermission.Unsubscribe(OnChanged);
            if (_voice != null)
            {
                _voice.connectionStatus.Unsubscribe(OnStatus);
                _voice.selfMuted.Unsubscribe(OnChanged);
            }
        }

        private void OnStatus(string status) => Apply();

        private void OnChanged(bool value) => Apply();

        private void Apply()
        {
            bool hasMicrophone = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone);
            if (!HeadsetVoiceRule.ShouldOpen(ClassroomRole.Current, XRINetworkGameManager.Connected.Value, hasMicrophone,
                    _voice.connectionStatus.Value, _voice.selfMuted.Value))
                return;
            Debug.Log($"[HeadsetVoice] {ClassroomRole.Current}: microphone on.");
            _voice.ToggleSelfMute(true, false);
        }
    }
}
