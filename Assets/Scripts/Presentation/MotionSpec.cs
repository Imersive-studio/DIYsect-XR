namespace DIYsect.Presentation
{
    /// <summary>
    /// Every duration and distance used to animate labels and instructions, in one table so they all move the same way.
    /// Times are seconds. Each value sits inside the range of the design's animation table.
    /// </summary>
    public static class MotionSpec
    {
        /// <summary>Organ label: opacity plus a small upward reveal (design: 180 to 220 ms).</summary>
        public const float LabelIn = 0.20f;
        /// <summary>Organ label: quick dissolve.</summary>
        public const float LabelOut = 0.18f;
        /// <summary>How far an organ label rises while it fades in, in metres (the design's 6 px at reading distance).</summary>
        public const float LabelRise = 0.004f;

        /// <summary>Leader line: draws from the organ to the text (design: 200 to 280 ms).</summary>
        public const float LeaderIn = 0.24f;
        /// <summary>Leader line: retracts toward the organ.</summary>
        public const float LeaderOut = 0.20f;

        /// <summary>Inspection title: soft masked reveal (design: 250 to 350 ms).</summary>
        public const float TitleIn = 0.30f;
        /// <summary>Inspection title: fade with a slight scale down.</summary>
        public const float TitleOut = 0.25f;
        /// <summary>Delay between the title's lines as they reveal one after another.</summary>
        public const float TitleStagger = 0.06f;
        /// <summary>Scale the title shrinks to while it fades out.</summary>
        public const float TitleOutScale = 0.96f;

        /// <summary>Instruction change: the new text rolls up into a mask (design: 220 to 280 ms).</summary>
        public const float InstructionIn = 0.25f;
        /// <summary>Instruction change: the previous text rolls out.</summary>
        public const float InstructionOut = 0.22f;

        /// <summary>Success check: the stroke draws, then a tiny spring (design: 250 ms in all).</summary>
        public const float CheckIn = 0.25f;
        /// <summary>Part of <see cref="CheckIn"/> spent drawing the stroke; the rest is the spring.</summary>
        public const float CheckStrokeShare = 0.64f;

        /// <summary>A classmate's "look here": the organ rises out of the body with a soft spring.</summary>
        public const float PopOutIn = 0.35f;
        /// <summary>A classmate's "look here" ends: the organ settles back.</summary>
        public const float PopOutOut = 0.28f;
        /// <summary>How far the pointed organ rises, in metres (5 cm reads from across the table without leaving the body).</summary>
        public const float PopOutLift = 0.05f;

        /// <summary>A contextual hint stays this long, then dissolves, so it never lingers over the anatomy.</summary>
        public const float HintHold = 2.5f;
    }
}
