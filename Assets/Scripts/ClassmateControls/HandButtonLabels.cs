namespace DIYsect.ClassmateControls
{
    /// <summary>The words on the Raise hand button. Pure, so it is tested without a scene.</summary>
    public static class HandButtonLabels
    {
        public const string Raise = "Raise hand";
        public const string Lower = "Lower hand";

        /// <summary>"Lower hand" while the hand is up (with its place in the line), otherwise "Raise hand".</summary>
        public static string For(int handOrder)
        {
            return handOrder <= 0 ? Raise : handOrder == 1 ? Lower : $"{Lower} ({handOrder})";
        }
    }
}
