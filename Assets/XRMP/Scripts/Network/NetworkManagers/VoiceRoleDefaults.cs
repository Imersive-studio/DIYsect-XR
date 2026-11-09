// Authority: none. Local audio setting for this device's own microphone.
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Sets this device's microphone when it joins the room's voice channel, by role (see <see cref="VoiceDefaultsRule"/>): the
    /// professor is open, a classmate on a phone is muted until the professor allows them. Runs again after a reconnect. It sits on the
    /// network manager, next to the voice manager, which it needs.
    /// </summary>
    [RequireComponent(typeof(VoiceChatManager))]
    public class VoiceRoleDefaults : MonoBehaviour
    {
        private VoiceChatManager _voice;

        private void OnEnable()
        {
            _voice = GetComponent<VoiceChatManager>();
            _voice.connectionStatus.Subscribe(OnStatus);
        }

        private void OnDisable()
        {
            if (_voice != null)
                _voice.connectionStatus.Unsubscribe(OnStatus);
        }

        private void OnStatus(string status)
        {
            if (status != VoiceDefaultsRule.JoinedStatus)
                return;
            bool mute = VoiceDefaultsRule.StartsMuted(ClassroomRole.Current);
            // Set explicitly in both directions: the template leaves the input device as it found it when the channel is joined.
            _voice.ToggleSelfMute(true, mute);
            Debug.Log($"[VoiceRoleDefaults] Joined the voice channel as the {ClassroomRole.Current}: microphone {(mute ? "muted" : "open")}.");
        }
    }
}
