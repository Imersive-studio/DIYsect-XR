namespace XRMultiplayer
{
    /// <summary>
    /// Who starts the class with their microphone open. The template says "unmuted" the moment a player joins the voice channel, and
    /// nothing in the shared scene sets the microphone itself, so a classmate on a phone could be heard before the professor allowed
    /// it, and nothing opened the professor's. The professor and the student in a headset start open; a classmate on a phone starts
    /// muted until the professor allows them to speak. Pure, so it is tested without Vivox.
    /// </summary>
    public static class VoiceDefaultsRule
    {
        /// <summary>The voice service reports this status once the local player is in the room's voice channel.</summary>
        public const string JoinedStatus = "Joined Voice Channel";

        public static bool StartsMuted(ClassroomRoleKind role)
        {
            return role == ClassroomRoleKind.Classmate;
        }
    }
}
