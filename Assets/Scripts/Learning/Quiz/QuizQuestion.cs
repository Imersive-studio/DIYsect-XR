using System;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// One quiz question: the sentence classmates read and the organ that is the right answer. The organ is named by its
    /// object name in the body (for example "gallbladder" or "kidney_l"), not by a number, so reordering the body never
    /// breaks a question set. It is plain data and can be edited in the Inspector.
    /// </summary>
    [Serializable]
    public struct QuizQuestion
    {
        [SerializeField, Tooltip("What classmates read, for example: Tap the organ that stores bile")]
        private string _prompt;
        [SerializeField, Tooltip("Object name of the right organ in the body, for example: gallbladder")]
        private string _targetOrgan;

        public QuizQuestion(string prompt, string targetOrgan)
        {
            _prompt = prompt;
            _targetOrgan = targetOrgan;
        }

        public string Prompt => _prompt ?? string.Empty;

        /// <summary>The right organ's object name in the body.</summary>
        public string TargetOrgan => _targetOrgan ?? string.Empty;

        /// <summary>True when the question can be asked. Otherwise <paramref name="problem"/> says what to fix.</summary>
        public bool IsValid(out string problem)
        {
            if (string.IsNullOrWhiteSpace(Prompt))
                problem = "the prompt is empty";
            else if (QuizPromptText.Clamp(Prompt) != Prompt.Trim())
                problem = $"the prompt is longer than {QuizPromptText.MaxBytes} bytes";
            else if (string.IsNullOrWhiteSpace(TargetOrgan))
                problem = "the target organ is empty";
            else
                problem = string.Empty;
            return problem.Length == 0;
        }
    }
}
