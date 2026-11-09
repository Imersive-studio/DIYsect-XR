namespace DIYsect.Learning
{
    // Presentation decisions only. ChecklistTracker remains the writer of learning progress.
    public static class ChecklistTransitionRule
    {
        private static readonly ChecklistTransition[,] Table = BuildTable();

        public static ChecklistTransition Evaluate(ChecklistState? previous, ChecklistState next)
        {
            return previous.HasValue ? Table[(int)previous.Value, (int)next] : ChecklistTransition.Snap;
        }

        private static ChecklistTransition[,] BuildTable()
        {
            var table = new ChecklistTransition[8, 8];
            for (int from = 0; from < 8; from++)
                for (int to = 0; to < 8; to++)
                    table[from, to] = from == to ? ChecklistTransition.Snap
                        : to == 0 ? ChecklistTransition.Reset
                        : to == 7 ? ChecklistTransition.Complete
                        : to < from ? ChecklistTransition.Revert : ChecklistTransition.Advance;
            return table;
        }

        public static float Duration(ChecklistTransition transition)
        {
            switch (transition)
            {
                case ChecklistTransition.Advance: return 0.7f;
                case ChecklistTransition.Complete: return 1.2f;
                case ChecklistTransition.Revert: return 0.35f;
                case ChecklistTransition.Reset: return 0.25f;
                default: return 0f;
            }
        }
    }
}
