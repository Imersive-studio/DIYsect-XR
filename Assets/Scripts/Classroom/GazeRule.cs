using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The pure rules of the shared gaze dots: how often to cast, when a dot is visible, how it fades and how it is smoothed.
    /// No scene, no network, so it is easy to test.
    /// </summary>
    public static class GazeRule
    {
        /// <summary>Casts per second, per head.</summary>
        public const float CastsPerSecond = 15f;
        /// <summary>Seconds a dot stays fully shown after its last hit.</summary>
        public const float HoldSeconds = 1.5f;
        /// <summary>Seconds a dot takes to fade out once the hold is over.</summary>
        public const float FadeSeconds = 0.3f;

        /// <summary>True when enough time has passed since the last cast. A negative infinity last time always casts.</summary>
        public static bool ShouldCast(float now, float lastCastAt, float castsPerSecond = CastsPerSecond)
        {
            if (castsPerSecond <= 0f)
                return false;
            return now - lastCastAt >= 1f / castsPerSecond;
        }

        /// <summary>0 to 1. Zero when the head is absent or is my own; one while the hit is fresh; then a straight fade to zero.</summary>
        public static float Alpha(bool present, bool isOwn, float secondsSinceHit)
        {
            if (!present || isOwn || float.IsNaN(secondsSinceHit))
                return 0f;
            if (secondsSinceHit <= HoldSeconds)
                return 1f;
            return Mathf.Clamp01(1f - (secondsSinceHit - HoldSeconds) / FadeSeconds);
        }

        public static bool IsVisible(bool present, bool isOwn, float secondsSinceHit)
        {
            return Alpha(present, isOwn, secondsSinceHit) > 0f;
        }

        /// <summary>
        /// The slot this device publishes its own head to, or null when it publishes none. A headset publishes; a phone does not.
        /// The host's head is the professor slot and any other client's head is the student slot (see HeadPoseRule), so a student
        /// who opened the room is the host and fills the professor slot.
        /// </summary>
        public static PointerRole? OwnSlot(bool isHeadset, bool isServer)
        {
            if (!isHeadset)
                return null;
            return isServer ? PointerRole.Professor : PointerRole.Student;
        }

        /// <summary>
        /// The share of the way to the target to move this frame, for a smooth glide that looks the same at any frame rate.
        /// A higher rate follows faster. After one second of any frame rate the same share of the gap is left.
        /// </summary>
        public static float SmoothFactor(float deltaSeconds, float rate)
        {
            if (deltaSeconds <= 0f || rate <= 0f)
                return 0f;
            return 1f - Mathf.Exp(-rate * deltaSeconds);
        }
    }
}
