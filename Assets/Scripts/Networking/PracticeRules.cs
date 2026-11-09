namespace DIYsect.Networking
{
    /// <summary>When a headset with nobody to join should start its own practice session. Pure, so a test can check it.</summary>
    public static class PracticeRules
    {
        /// <summary>
        /// True once <paramref name="waitSeconds"/> have passed since the scene started and the network is not running
        /// yet, which means no room was found or joined.
        /// </summary>
        public static bool ShouldStart(float secondsSinceStart, float waitSeconds, bool networkRunning)
        {
            return !networkRunning && secondsSinceStart >= waitSeconds;
        }
    }
}
