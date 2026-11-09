using XRMultiplayer;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// When a headset opens its microphone. The professor leads the class and the student in VR talks with them, so both speak
    /// freely; classmates on phones ask first (<see cref="MicButtonModel"/>). The template mutes every player when they join, and
    /// the headsets have no mute button in this app, so without this rule nobody in a headset is ever heard.
    /// No Unity objects, so it is easy to test.
    /// </summary>
    public static class HeadsetVoiceRule
    {
        /// <summary>The voice manager's status once this device is in the room's voice channel.</summary>
        public const string JoinedStatus = "Joined Voice Channel";

        /// <summary>True for the roles that speak without asking.</summary>
        public static bool SpeaksFreely(ClassroomRoleKind role) => role == ClassroomRoleKind.Professor || role == ClassroomRoleKind.Student;

        /// <summary>True when the microphone should be opened now.</summary>
        public static bool ShouldOpen(ClassroomRoleKind role, bool inRoom, bool hasMicrophone, string voiceStatus, bool muted)
        {
            return SpeaksFreely(role) && inRoom && hasMicrophone && voiceStatus == JoinedStatus && muted;
        }
    }
}
