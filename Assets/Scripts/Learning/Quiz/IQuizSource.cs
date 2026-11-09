namespace DIYsect.Learning
{
    /// <summary>
    /// Where the quiz questions come from. The professor panel only talks to this interface, so the questions can come
    /// from a ScriptableObject today and from a file, a lesson plan or a server later, with no change to the panel.
    /// </summary>
    public interface IQuizSource
    {
        /// <summary>How many questions there are.</summary>
        int Count { get; }

        /// <summary>The question at the index (0 to Count - 1). Throws when the index is out of range.</summary>
        QuizQuestion Get(int index);
    }
}
