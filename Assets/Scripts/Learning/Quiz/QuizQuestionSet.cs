using System;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// A list of quiz questions kept in an asset, so teachers and designers change the quiz in the Inspector and no code
    /// changes. Create another one with Assets > Create > DIYsect > Quiz Question Set. The default set is
    /// DefaultQuizQuestions.asset in this folder.
    /// </summary>
    [CreateAssetMenu(fileName = "QuizQuestions", menuName = "DIYsect/Quiz Question Set")]
    public class QuizQuestionSet : ScriptableObject, IQuizSource
    {
        [SerializeField] private QuizQuestion[] _questions = new QuizQuestion[0];

        public int Count => _questions != null ? _questions.Length : 0;

        public QuizQuestion Get(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"{name} has {Count} questions; there is no question {index}.");
            return _questions[index];
        }

        // Used by tests to fill a set made in code.
        internal void SetQuestions(QuizQuestion[] questions)
        {
            _questions = questions ?? new QuizQuestion[0];
        }

        // Tell the author about a bad question as soon as they type it.
        private void OnValidate()
        {
            for (int i = 0; i < Count; i++)
                if (!_questions[i].IsValid(out string problem))
                    Debug.LogWarning($"{name}: question {i} cannot be asked because {problem}.", this);
        }
    }
}
