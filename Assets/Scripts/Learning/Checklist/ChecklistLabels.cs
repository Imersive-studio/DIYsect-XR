using System.Text;

namespace DIYsect.Learning
{
    /// <summary>
    /// The words on the checklist screens. Pure text from a <see cref="ChecklistState"/>, so the headset panel and
    /// the classmate line always say the same thing and the wording is tested without a scene.
    /// </summary>
    public static class ChecklistLabels
    {
        /// <summary>How many steps the checklist has (every state before Complete).</summary>
        public const int StepCount = 7;

        public const string Title = "Dissection checklist";
        public const string AllDone = "All steps done.";

        private static readonly string[] Steps =
        {
            "Trace the midline incision",
            "Open the skin flaps",
            "Open the rectus",
            "Lift the omentum",
            "Lift out an organ",
            "Hold the organ to inspect it",
            "Put it back in its place",
        };

        private static readonly string[] Instructions =
        {
            "Hold grip on the scalpel. Draw the tip down the line",
            "Skin opening",
            "Touch the muscle with the forceps tip",
            "Touch the omentum with the forceps tip",
            "Hold grip on an organ to lift it out",
            "Keep holding to inspect it",
            "Bring it back to its place",
            AllDone,
        };

        public static string Instruction(ChecklistState state) => Instructions[(int)state];

        /// <summary>The wording of step <paramref name="index"/> (0 to <see cref="StepCount"/> - 1).</summary>
        public static string StepText(int index)
        {
            return Steps[index];
        }

        /// <summary>How many steps are ticked in this state.</summary>
        public static int DoneCount(ChecklistState state)
        {
            return (int)state;
        }

        /// <summary>The whole list: a title, then one line per step marked [x] done, [>] next or [ ] later.</summary>
        public static string Format(ChecklistState state)
        {
            int done = DoneCount(state);
            var text = new StringBuilder(Title);
            for (int i = 0; i < StepCount; i++)
            {
                string mark = i < done ? "[x]" : i == done ? "[>]" : "[ ]";
                text.Append('\n').Append(mark).Append(' ').Append(Steps[i]);
            }
            if (done >= StepCount)
                text.Append('\n').Append(AllDone);
            return text.ToString();
        }

        /// <summary>One line for a flat screen: how many steps are done and what is next.</summary>
        public static string OneLine(ChecklistState state)
        {
            int done = DoneCount(state);
            return done >= StepCount
                ? $"Checklist {StepCount}/{StepCount}: {AllDone}"
                : $"Checklist {done}/{StepCount}: {Steps[done]}";
        }
    }
}
