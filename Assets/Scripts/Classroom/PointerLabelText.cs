using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>The words on a classmate's pointer label. The role word differs per role, so colour is never the only cue.</summary>
    public static class PointerLabelText
    {
        /// <param name="hidingLayer">A phrase such as "the omentum", or null when the organ is in view.</param>
        public static string Build(PointerRole role, string organName, string hidingLayer)
        {
            string who = role == PointerRole.Professor ? "Professor" : "Student";
            string text = $"{who}: {organName}";
            return string.IsNullOrEmpty(hidingLayer) ? text : $"{text} (under {hidingLayer})";
        }
    }
}
