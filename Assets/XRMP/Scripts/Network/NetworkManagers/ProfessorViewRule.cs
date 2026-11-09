namespace XRMultiplayer
{
    /// <summary>How the professor sees the room.</summary>
    public enum ProfessorViewMode
    {
        /// <summary>The same lecture hall the students and the phones are in, drawn in the headset with no passthrough.</summary>
        Classroom,
        /// <summary>The real room through the headset, with the table placed on a real table.</summary>
        MixedReality,
    }

    /// <summary>
    /// What each view of the professor turns on and off. The professor picks one with a button and the choice is kept for next time.
    /// Pure, so it is tested without a headset.
    /// </summary>
    public static class ProfessorViewRule
    {
        public const string PrefsKey = "diysect.professorView";

        /// <summary>Everyone starts in the classroom, so the professor sees what the class sees.</summary>
        public const ProfessorViewMode Default = ProfessorViewMode.Classroom;

        /// <summary>The lecture hall, the seated class and the lights are on.</summary>
        public static bool HallVisible(ProfessorViewMode mode) => mode == ProfessorViewMode.Classroom;

        /// <summary>The camera shows the real room behind the virtual objects.</summary>
        public static bool PassthroughOn(ProfessorViewMode mode) => mode == ProfessorViewMode.MixedReality;

        /// <summary>The "put the table on your real table" step and its handle are on. In the classroom the table and the hall stay put.</summary>
        public static bool TableAlignmentOn(ProfessorViewMode mode) => mode == ProfessorViewMode.MixedReality;

        /// <summary>The headset stands at one fixed eye height on the hall floor, like the student, instead of following the real floor.</summary>
        public static bool FixedEyeHeight(ProfessorViewMode mode) => mode == ProfessorViewMode.Classroom;

        public static ProfessorViewMode Other(ProfessorViewMode mode)
        {
            return mode == ProfessorViewMode.Classroom ? ProfessorViewMode.MixedReality : ProfessorViewMode.Classroom;
        }

        /// <summary>The text on the button: where the professor is now, and what pressing it does.</summary>
        public static string ButtonText(ProfessorViewMode mode)
        {
            return mode == ProfessorViewMode.Classroom
                ? "View: Classroom\nPress for Mixed reality"
                : "View: Mixed reality\nPress for Classroom";
        }

        /// <summary>The kept choice, or the default when nothing is kept or the text is not a view.</summary>
        public static ProfessorViewMode Parse(string kept)
        {
            return System.Enum.TryParse(kept, out ProfessorViewMode mode) && System.Enum.IsDefined(typeof(ProfessorViewMode), mode) ? mode : Default;
        }
    }
}
