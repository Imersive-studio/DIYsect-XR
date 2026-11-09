using System;

namespace DIYsect.Learning
{
    /// <summary>
    /// Turns quiz tap counts into a 0..1 heat value per organ, and into the words on the answer chip.
    /// Pure logic with no Unity state, so it is tested without a scene.
    /// </summary>
    public static class QuizHeatmapRule
    {
        /// <summary>
        /// Heat of an organ that got at least one tap but fewer than the busiest organ. The lowest heat any tapped organ
        /// can have, so a single tap is still visible next to a crowded organ.
        /// </summary>
        public const float MinTappedHeat = 0.25f;

        /// <summary>
        /// 0 for an organ nobody tapped (or when there are no answers at all), 1 for the busiest organ (ties share 1),
        /// and a value from <see cref="MinTappedHeat"/> up to 1 in proportion to its taps in between.
        /// The busiest count is capped at the total, so a stale maximum can never make every organ look cold.
        /// </summary>
        public static float Heat(int tapsForOrgan, int totalTaps, int maxTaps)
        {
            if (tapsForOrgan <= 0 || totalTaps <= 0 || maxTaps <= 0)
                return 0f;
            int busiest = Math.Min(maxTaps, totalTaps);
            float share = Math.Min(1f, (float)tapsForOrgan / busiest);
            return MinTappedHeat + (1f - MinTappedHeat) * share;
        }

        /// <summary>
        /// Blend strength toward the warm colour for a heat value: nothing at 0, <paramref name="soft"/> at the lowest tapped heat
        /// (rising smoothly from 0, so an easing heat never jumps), <paramref name="hot"/> at 1.
        /// </summary>
        public static float Strength(float heat, float soft, float hot)
        {
            if (heat <= 0f)
                return 0f;
            if (heat < MinTappedHeat)
                return soft * heat / MinTappedHeat;
            float t = Math.Max(0f, Math.Min(1f, (heat - MinTappedHeat) / (1f - MinTappedHeat)));
            return soft + (hot - soft) * t;
        }

        /// <summary>The chip text: "1 answer", "12 answers". Empty when nobody answered yet.</summary>
        public static string ChipText(int totalAnswers)
        {
            if (totalAnswers <= 0)
                return string.Empty;
            return totalAnswers == 1 ? "1 answer" : totalAnswers + " answers";
        }
    }
}
