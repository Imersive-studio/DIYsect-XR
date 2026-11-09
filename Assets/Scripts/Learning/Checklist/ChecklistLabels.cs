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

        private static readonly string[] Headlines =
        {
            "Trace the midline",
            "Open the skin flaps",
            "Open the rectus",
            "Lift the omentum",
            "Lift out an organ",
            "Inspect it up close",
            "Put it back home",
            "Dissection complete",
        };

        // The skin flaps fold open by themselves once the incision is done, so that step asks for no tool action.
        private static readonly string[] Hints =
        {
            "Hold grip on the scalpel and draw the tip down the glowing line.",
            "Watch the skin flaps fold open.",
            "Touch the muscle with the forceps tip.",
            "Touch the omentum with the forceps tip and lift.",
            "Hold grip on any organ to lift it out.",
            "Keep holding it for a moment and read its card.",
            "Carry it back to its glowing outline and let go.",
            "All seven steps done. Beautiful work, Doctor.",
        };

        private static readonly string[] Tools =
        {
            "SCALPEL", "AUTO", "FORCEPS", "FORCEPS", "HANDS", "HANDS", "HANDS", string.Empty,
        };

        private static readonly string[] Cheers =
        {
            "Clean incision!", "Nicely opened.", "Muscle wall open.", "Omentum lifted.",
            "Got it!", "Good look.", "Right back home.", string.Empty,
        };

        /// <summary>The short title of the step to do next ("Open the rectus").</summary>
        public static string Headline(ChecklistState state) => Headlines[(int)state];

        /// <summary>One friendly sentence on how to do the step.</summary>
        public static string Hint(ChecklistState state) => Hints[(int)state];

        /// <summary>The tool chip text: SCALPEL, FORCEPS, HANDS, AUTO (nothing to do), or empty when finished.</summary>
        public static string ToolName(ChecklistState state) => Tools[(int)state];

        /// <summary>The short praise shown while a step that was just finished fades out. Empty for Complete.</summary>
        public static string Cheer(ChecklistState completedStep) => Cheers[(int)completedStep];

        /// <summary>"STEP 3 OF 7", or "ALL DONE" once finished.</summary>
        public static string ProgressChip(ChecklistState state)
        {
            return state >= ChecklistState.Complete ? "ALL DONE" : $"STEP {(int)state + 1} OF {StepCount}";
        }

        /// <summary>"2/7": steps done out of all steps.</summary>
        public static string Count(ChecklistState state) => $"{DoneCount(state)}/{StepCount}";

        /// <summary>The number in the badge: the step number, staying at 7 when finished.</summary>
        public static string Badge(ChecklistState state)
        {
            return state >= ChecklistState.Complete ? StepCount.ToString() : ((int)state + 1).ToString();
        }

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
