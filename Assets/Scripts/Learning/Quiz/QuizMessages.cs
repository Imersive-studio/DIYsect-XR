using System;
using System.Collections.Generic;
using System.Text;

namespace DIYsect.Learning
{
    /// <summary>
    /// Every line of quiz text people read, in one place so the wording stays consistent and is easy to test.
    /// Organ names come in already readable (see <c>OrganNames.Display</c>), for example "Left kidney".
    /// </summary>
    public static class QuizMessages
    {
        /// <summary>Longest line, in characters, of the professor's headset panel before the question text wraps.</summary>
        public const int ProfessorLineWidth = 32;

        /// <summary>Shown to the professor when a question names an organ the body does not have.</summary>
        public const string MissingOrgan = "This question's organ is not in the body.";

        /// <summary>
        /// What a classmate reads while a round is running and they have not answered yet. With question text it is
        /// "Quiz: Tap the organ that stores bile"; without, "Quiz: tap the gallbladder".
        /// </summary>
        public static string Prompt(string organName, string promptText = null)
        {
            if (!string.IsNullOrWhiteSpace(promptText))
                return $"Quiz: {promptText.Trim()}";
            return $"Quiz: tap the {Lower(organName)}";
        }

        /// <summary>What a classmate reads after the server answered their tap.</summary>
        public static string ForAnswer(QuizResult result, bool correct, string targetName)
        {
            switch (result)
            {
                case QuizResult.Accepted:
                    return correct
                        ? $"Right! That is the {Lower(targetName)}."
                        : $"Not quite. That is not the {Lower(targetName)}.";
                case QuizResult.AlreadyAnswered:
                    return "You already answered this round.";
                case QuizResult.WrongPhase:
                case QuizResult.StaleRound:
                    return "That quiz round has already ended.";
                case QuizResult.NotAllowed:
                    return "The professor does not answer the quiz.";
                case QuizResult.BadOrgan:
                    return "Tap an organ to answer.";
                default:
                    return "The answer was not accepted.";
            }
        }

        /// <summary>The student's line under the question in the headset, before they answer.</summary>
        public const string HeadsetHint = "Point at the organ and pull the trigger.";
        /// <summary>The student's line when the trigger was pulled while pointing at something that is not an organ.</summary>
        public const string HeadsetMissed = "That was not an organ. Point at one and pull the trigger.";
        /// <summary>The student's line while the server is judging the answer.</summary>
        public const string AnswerSent = "Answer sent.";

        /// <summary>
        /// The small line under the question on the student's headset card: the server's verdict once it arrived,
        /// otherwise what to do next in this phase. Empty when there is nothing to say.
        /// </summary>
        public static string HeadsetStatus(QuizPhase phase, bool answered, bool missed, string feedback)
        {
            if (!string.IsNullOrEmpty(feedback))
                return feedback;
            if (phase != QuizPhase.Asking)
                return string.Empty;
            if (answered)
                return AnswerSent;
            return missed ? HeadsetMissed : HeadsetHint;
        }

        /// <summary>What the professor reads when the server refused a start, end or close.</summary>
        public static string ForRequest(QuizResult result)
        {
            switch (result)
            {
                case QuizResult.Accepted:
                    return string.Empty;
                case QuizResult.NotAllowed:
                    return "Only the host can run the quiz.";
                case QuizResult.WrongPhase:
                    return "Not now: end or clear the current round first.";
                case QuizResult.BadOrgan:
                    return "Pick an organ first.";
                default:
                    return "The quiz request was not accepted.";
            }
        }

        /// <summary>What a classmate reads once the round has ended.</summary>
        public static string Ended(string targetName, int right, int wrong) =>
            $"Quiz over: the {Lower(targetName)}. {Counts(right, wrong)}";

        /// <summary>
        /// The professor's panel. <paramref name="wrongPicks"/> are the organs tapped by mistake, most taps first,
        /// as (name, taps).
        /// </summary>
        public static string ForProfessor(QuizPhase phase, string targetName, int right, int wrong,
            IReadOnlyList<(string name, int taps)> wrongPicks, string promptText = null)
        {
            switch (phase)
            {
                case QuizPhase.Asking:
                    return $"{Question("Asking", "tap the", targetName, promptText)}\n{Counts(right, wrong)}{WrongLines(wrongPicks)}";
                case QuizPhase.Revealed:
                    return $"{Question("Round over", "the", targetName, promptText)}\n{Counts(right, wrong)}{WrongLines(wrongPicks)}";
                default:
                    return $"{Question("Next question", "tap the", targetName, promptText)}\nPress Start";
            }
        }

        /// <summary>Breaks text into lines of at most <paramref name="width"/> characters at spaces. A longer word keeps its own line.</summary>
        public static string Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0)
                return text ?? string.Empty;
            var lines = new StringBuilder();
            var line = new StringBuilder();
            foreach (string word in text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width)
                {
                    lines.Append(line.ToString()).Append('\n');
                    line.Length = 0;
                }
                if (line.Length > 0)
                    line.Append(' ');
                line.Append(word);
            }
            lines.Append(line.ToString());
            return lines.ToString();
        }

        // With question text: "Asking: Tap the organ that stores bile" then "Answer: Gallbladder".
        // Without: "Asking: tap the gallbladder" (the older wording).
        private static string Question(string label, string withoutPrompt, string targetName, string promptText)
        {
            if (string.IsNullOrWhiteSpace(promptText))
                return $"{label}: {withoutPrompt} {Lower(targetName)}";
            return $"{label}: {Wrap(promptText.Trim(), ProfessorLineWidth)}\nAnswer: {targetName}";
        }

        private static string Counts(int right, int wrong) => $"{right} right, {wrong} wrong";

        private static string WrongLines(IReadOnlyList<(string name, int taps)> wrongPicks)
        {
            if (wrongPicks == null || wrongPicks.Count == 0)
                return string.Empty;
            var text = new StringBuilder("\nTapped instead:");
            foreach ((string name, int taps) in wrongPicks)
                text.Append('\n').Append(name).Append(": ").Append(taps);
            return text.ToString();
        }

        // "Left kidney" reads "the left kidney" in a sentence.
        private static string Lower(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "organ";
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
