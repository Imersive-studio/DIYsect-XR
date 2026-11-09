using UnityEngine;

namespace DIYsect.Presentation
{
    /// <summary>
    /// The colours of every label and instruction: ink-teal glass, mint for done, coral for now, slate for later.
    /// One place so the organ labels, the guide card, the list and the hand-side screens agree.
    /// </summary>
    public static class GuidePalette
    {
        public static readonly Color Ink = Rgb(0x0B, 0x1F, 0x2A, 0.94f);
        public static readonly Color InkDeep = Rgb(0x07, 0x14, 0x1B, 0.96f);
        public static readonly Color Mint = Rgb(0x5E, 0xF2, 0xC2, 1f);
        public static readonly Color MintGlow = Rgb(0x5E, 0xF2, 0xC2, 0.10f);
        public static readonly Color MintLine = Rgb(0x5E, 0xF2, 0xC2, 0.35f);
        public static readonly Color Coral = Rgb(0xFF, 0x7A, 0x59, 1f);
        public static readonly Color CoralSoft = Rgb(0xFF, 0x7A, 0x59, 0.16f);
        /// <summary>A classmate's own mark (their pointer and their sketch). Violet, so it is never mistaken for the orange professor or the cyan student.</summary>
        public static readonly Color Violet = Rgb(0xC7, 0x6B, 0xFF, 1f);
        public static readonly Color Slate = Rgb(0x3A, 0x51, 0x60, 1f);
        public static readonly Color TextPrimary = Rgb(0xF4, 0xFB, 0xFF, 1f);
        public static readonly Color TextMuted = Rgb(0x9F, 0xB8, 0xC4, 1f);
        public static readonly Color TextDone = Rgb(0x9F, 0xB8, 0xC4, 0.75f);

        /// <summary>
        /// A smooth blend from <paramref name="a"/> to <paramref name="b"/> and back, one round trip every
        /// <paramref name="periodSeconds"/>. At time 0 it is exactly <paramref name="a"/>.
        /// </summary>
        public static Color Pulse(Color a, Color b, float time, float periodSeconds)
        {
            if (periodSeconds <= 0f) return a;
            float blend = 0.5f - 0.5f * Mathf.Cos(time / periodSeconds * (2f * Mathf.PI));
            return Color.Lerp(a, b, blend);
        }

        private static Color Rgb(int r, int g, int b, float alpha)
        {
            return new Color(r / 255f, g / 255f, b / 255f, alpha);
        }
    }
}
