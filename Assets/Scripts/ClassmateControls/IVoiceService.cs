using System;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// What the mute button needs from voice chat. The scene uses <see cref="VoiceChatManagerService"/> over the template's
    /// VoiceChatManager (Vivox); a test uses a fake, because Vivox needs the cloud services and a microphone.
    /// </summary>
    public interface IVoiceService
    {
        /// <summary>True when the microphone is switched off.</summary>
        bool IsMuted { get; }

        /// <summary>True when the app may use the microphone.</summary>
        bool HasMicPermission { get; }

        /// <summary>True when the professor has allowed this person to speak. Without it the microphone cannot be unmuted.</summary>
        bool MayUnmute { get; }

        /// <summary>Asks the professor for permission to speak (raises the person's hand).</summary>
        void RequestToSpeak();

        /// <summary>Raised when <see cref="IsMuted"/> changes.</summary>
        event Action Changed;

        /// <summary>Asks the person for the microphone. Calls back once with true (allowed) or false (refused).</summary>
        void RequestMicPermission(Action<bool> done);

        /// <summary>Switches the microphone off or on.</summary>
        void SetMuted(bool muted);
    }
}
