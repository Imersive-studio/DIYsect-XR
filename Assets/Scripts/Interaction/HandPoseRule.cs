using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The pure rule that turns the controller's trigger and grip into finger bends for the hand shown on the controller.
    /// No scene, so it is easy to test. Each value is 0 (straight) to 1 (fully bent).
    ///
    /// Trigger is the index finger, so pulling it pinches the index toward the thumb. Grip closes the other three fingers
    /// around the handle. The thumb follows whichever is stronger, a little less than fully, so a pinch reads as a pinch.
    /// </summary>
    public static class HandPoseRule
    {
        public const int Thumb = 0;
        public const int Index = 1;
        public const int Middle = 2;
        public const int Ring = 3;
        public const int Little = 4;
        public const int FingerCount = 5;

        /// <summary>How far the thumb bends at most, as a share of a full bend.</summary>
        public const float ThumbShare = 0.7f;

        /// <summary>Degrees each finger joint turns at a full bend. The thumb uses <see cref="ThumbDegrees"/>.</summary>
        public const float FingerDegrees = 75f;
        public const float ThumbDegrees = 40f;

        /// <summary>Fills <paramref name="bends"/> (length <see cref="FingerCount"/>) from the trigger and grip values.</summary>
        public static void Bends(float trigger, float grip, float[] bends)
        {
            trigger = Clean(trigger);
            grip = Clean(grip);
            bends[Thumb] = Mathf.Max(trigger, grip) * ThumbShare;
            bends[Index] = trigger;
            bends[Middle] = grip;
            bends[Ring] = grip;
            bends[Little] = grip;
        }

        /// <summary>Degrees for one joint of the given finger at the given bend.</summary>
        public static float Degrees(int finger, float bend)
        {
            return Clean(bend) * (finger == Thumb ? ThumbDegrees : FingerDegrees);
        }

        // Input can report NaN for a frame while a controller wakes up. Treat it as released.
        private static float Clean(float value)
        {
            return float.IsFinite(value) ? Mathf.Clamp01(value) : 0f;
        }
    }
}
