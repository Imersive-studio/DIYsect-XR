// Authority: none. Presentation only; it shows the server-owned quiz state and this client's own result.
using UnityEngine;
using UnityEngine.UI;
using DIYsect.Interaction;

namespace DIYsect.Learning
{
    /// <summary>
    /// The classmate's quiz line at the top of the screen: "Quiz: tap the liver" while a round is running, then
    /// "Right!" or "Not quite." once the server has judged this client's tap, and the final counts when the round
    /// ends. Hidden when no quiz is on. It reacts to <see cref="QuizRound"/> events and never polls.
    /// </summary>
    public class QuizPrompt : MonoBehaviour
    {
        [SerializeField] private QuizRound _quiz;
        [SerializeField, Tooltip("Text that shows the quiz line. It is hidden when no quiz is on.")]
        private Text _label;

        // The feedback belongs to one round; a new round shows the prompt again.
        private int _feedbackRound = -1;
        private string _feedback = string.Empty;

        /// <summary>The line on screen, or empty when hidden.</summary>
        public string CurrentText => _label != null && _label.gameObject.activeSelf ? _label.text : string.Empty;

        private void Awake()
        {
            if (_quiz == null || _label == null)
            {
                Debug.LogError($"{name}: QuizPrompt needs a QuizRound and a Text. Run Tools > DIYsect > Add Quiz.", this);
                enabled = false;
                return;
            }
            _label.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _quiz.StateChanged += Refresh;
            _quiz.LocalAnswerResolved += HandleAnswerResolved;
            Refresh();
        }

        private void OnDisable()
        {
            // The body can be destroyed first when the scene unloads.
            if (_quiz == null)
                return;
            _quiz.StateChanged -= Refresh;
            _quiz.LocalAnswerResolved -= HandleAnswerResolved;
        }

        private void HandleAnswerResolved(int round, QuizResult result, bool correct)
        {
            _feedbackRound = round;
            _feedback = QuizMessages.ForAnswer(result, correct, TargetName());
            Refresh();
        }

        private void Refresh()
        {
            if (!_quiz.IsSpawned || _quiz.Phase == QuizPhase.Idle || _quiz.TargetOrgan < 0)
            {
                _label.gameObject.SetActive(false);
                return;
            }
            string target = TargetName();
            if (_quiz.Phase == QuizPhase.Revealed)
                _label.text = QuizMessages.Ended(target, _quiz.RightCount, _quiz.WrongCount);
            else
                _label.text = _feedbackRound == _quiz.Round ? _feedback : QuizMessages.Prompt(target, _quiz.Prompt);
            _label.gameObject.SetActive(true);
        }

        private string TargetName()
        {
            int target = _quiz.TargetOrgan;
            var organs = _quiz.Body.Organs;
            return target >= 0 && target < organs.Count ? OrganNames.Display(organs[target].name) : string.Empty;
        }
    }
}
