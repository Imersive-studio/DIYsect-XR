namespace DIYsect.Classroom
{
    /// <summary>The words on a pin marker and on the student view caption. Kept apart from the presenter so they are easy to test.</summary>
    public static class PinLabels
    {
        /// <summary>Put in front of the text of an answered question. Plain ASCII, because the default font has no check glyph.</summary>
        public const string AnsweredMark = "[x] ";

        /// <summary>The text with the answered mark in front when the question is answered.</summary>
        public static string ForPin(string text, bool answered)
        {
            return answered ? AnsweredMark + text : text;
        }

        /// <summary>A lone pin shows its text. A cluster shows how many questions are still open; when all are answered it shows the marked total.</summary>
        public static string ForCluster(int count, int openCount, string loneText)
        {
            if (count == 1)
                return ForPin(loneText, openCount == 0);
            return openCount == 0 ? AnsweredMark + count : openCount.ToString();
        }
    }
}
