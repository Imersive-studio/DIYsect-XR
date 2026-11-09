// Authority: none. The professor's controls only ask the server through QuizRound; the server decides.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DIYsect.Interaction;
using DIYsect.Presentation;

namespace DIYsect.Learning
{
    /// <summary>
    /// The professor's quiz panel in the headset. Previous and next step through the questions of a quiz source; Start,
    /// End and Clear run the round. The text shows the question and, live, how many classmates got it right or wrong and
    /// which organs they tapped instead. The buttons call the public methods through persistent events
    /// (see Tools > DIYsect > Add Quiz). It reacts to <see cref="QuizRound"/> events and never polls.
    /// The network only gets the question's organ index and its text; the question list itself never leaves this panel.
    /// </summary>
    public class QuizPanel : MonoBehaviour
    {
        [SerializeField] private QuizRound _quiz;
        [SerializeField, Tooltip("The status line, for example \"Asking: 7 answers\".")] private TMP_Text _status;
        [SerializeField, Tooltip("The large question line. Optional.")] private TMP_Text _question;
        [SerializeField, Tooltip("Counts and wrong taps, small text under the status. Optional.")] private TMP_Text _detail;
        [SerializeField, Tooltip("Shown only when a new round can start (nothing asked yet, or a round is over). Optional.")] private GameObject _startButton;
        [SerializeField, Tooltip("Shown only while a round is asking. Optional.")] private GameObject _endButton;
        [SerializeField, Tooltip("Shown only once a round is over. Optional.")] private GameObject _clearButton;
        [SerializeField, Tooltip("Previous and next question; hidden while a round is asking. Optional.")] private GameObject[] _stepButtons;
        [SerializeField, Tooltip("The questions to ask. Swap this asset to change the quiz; no code changes.")]
        private QuizQuestionSet _questions;
        [SerializeField, Min(0), Tooltip("Wrong organs listed at most, most taps first.")]
        private int _maxWrongLines = 3;

        private IQuizSource _source;
        private int _selected = -1;
        private string _notice = string.Empty;

        /// <summary>The question the next Start will ask, as an index into the source.</summary>
        public int SelectedQuestion => _selected;

        /// <summary>The panel text now.</summary>
        public string StatusText => _status != null ? _status.text : string.Empty;

        /// <summary>Uses another source of questions (for example a test set) instead of the assigned asset.</summary>
        public void SetSource(IQuizSource source)
        {
            _source = source;
            _selected = -1;
            if (isActiveAndEnabled)
                Refresh();
        }

        private void Awake()
        {
            if (_source == null)
                _source = _questions;
            if (_quiz == null || _status == null || _source == null || _source.Count == 0)
            {
                Debug.LogError($"{name}: QuizPanel needs a QuizRound, a status text and a quiz question set with at least one question. Run Tools > DIYsect > Add Quiz.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _quiz.StateChanged += Refresh;
            _quiz.LocalRequestRejected += HandleRejected;
            Refresh();
        }

        private void OnDisable()
        {
            if (_quiz == null)
                return;
            _quiz.StateChanged -= Refresh;
            _quiz.LocalRequestRejected -= HandleRejected;
        }

        /// <summary>Picks the next question (wraps around). Wired to the panel's ">" button.</summary>
        public void NextQuestion() => Step(+1);

        /// <summary>Picks the previous question (wraps around). Wired to the panel's "&lt;" button.</summary>
        public void PreviousQuestion() => Step(-1);

        /// <summary>Starts a round for the picked question. Wired to the panel's Start button.</summary>
        public void StartRound()
        {
            EnsureSelection();
            _notice = string.Empty;
            QuizQuestion question = _source.Get(_selected);
            int organ = OrganIndexOf(question.TargetOrgan);
            if (organ < 0)
            {
                _notice = QuizMessages.MissingOrgan;
                Refresh();
                return;
            }
            _quiz.RequestStart(organ, question.Prompt);
        }

        /// <summary>Ends the running round. Wired to the panel's End button.</summary>
        public void EndRound()
        {
            _notice = string.Empty;
            _quiz.RequestEnd();
        }

        /// <summary>Clears the finished round from every screen. Wired to the panel's Clear button.</summary>
        public void ClearRound()
        {
            _notice = string.Empty;
            _quiz.RequestClose();
        }

        private void Step(int direction)
        {
            int count = _source.Count;
            if (count == 0)
                return;
            EnsureSelection();
            _selected = (_selected + direction + count) % count;
            _notice = string.Empty;
            Refresh();
        }

        private void HandleRejected(QuizResult result)
        {
            _notice = QuizMessages.ForRequest(result);
            Refresh();
        }

        private void Refresh()
        {
            EnsureSelection();
            QuizPhase phase = _quiz.IsSpawned ? _quiz.Phase : QuizPhase.Idle;

            // While a round is on or just ended, the panel talks about that round, not the next pick.
            int shown;
            string prompt;
            if (phase == QuizPhase.Idle || _quiz.TargetOrgan < 0)
            {
                QuizQuestion next = _source.Get(_selected);
                shown = OrganIndexOf(next.TargetOrgan);
                prompt = next.Prompt;
            }
            else
            {
                shown = _quiz.TargetOrgan;
                prompt = _quiz.Prompt;
            }

            var wrongPicks = new List<(string name, int taps)>();
            if (phase != QuizPhase.Idle)
                foreach (int organ in QuizSummary.TopWrong(_quiz.OrganCount, _quiz.TapsFor, shown, _maxWrongLines))
                    wrongPicks.Add((NameOf(organ), _quiz.TapsFor(organ)));

            ShowText(phase, NameOf(shown), prompt, wrongPicks);
            ShowButtons(phase);
        }

        private void ShowText(QuizPhase phase, string target, string prompt, List<(string name, int taps)> wrongPicks)
        {
            string question = string.IsNullOrWhiteSpace(prompt) ? $"Tap the {target}" : prompt.Trim();
            string mint = ColorUtility.ToHtmlStringRGB(GuidePalette.Mint);
            int answers = _quiz.RightCount + _quiz.WrongCount;
            string status;
            switch (phase)
            {
                case QuizPhase.Asking:
                    status = $"Round {_quiz.Round}  Asking: <color=#{mint}>{answers} {(answers == 1 ? "answer" : "answers")}</color>";
                    break;
                case QuizPhase.Revealed:
                    status = $"Round {_quiz.Round}  Revealed: {target}";
                    break;
                default:
                    status = "Ready. Press Start";
                    break;
            }

            string detail = string.Empty;
            if (phase != QuizPhase.Idle)
            {
                detail = $"{_quiz.RightCount} right, {_quiz.WrongCount} wrong";
                if (wrongPicks.Count > 0)
                {
                    var picks = new List<string>();
                    foreach ((string name, int taps) in wrongPicks)
                        picks.Add($"{name} {taps}");
                    detail += $"\nTapped instead: {string.Join(", ", picks)}";
                }
            }
            if (!string.IsNullOrEmpty(_notice))
                detail = string.IsNullOrEmpty(detail) ? _notice : $"{detail}\n{_notice}";

            if (_question != null)
            {
                _question.text = question;
                _status.text = status;
            }
            else
            {
                // No separate question line: keep the question in the status text.
                _status.text = $"{question}\n{status}";
            }
            if (_detail != null)
                _detail.text = detail;
        }

        // Only the buttons that make sense in this phase are shown.
        private void ShowButtons(QuizPhase phase)
        {
            SetShown(_startButton, phase != QuizPhase.Asking);
            SetShown(_endButton, phase == QuizPhase.Asking);
            SetShown(_clearButton, phase == QuizPhase.Revealed);
            if (_stepButtons != null)
                foreach (GameObject step in _stepButtons)
                    SetShown(step, phase != QuizPhase.Asking);
        }

        private static void SetShown(GameObject button, bool shown)
        {
            if (button != null && button.activeSelf != shown)
                button.SetActive(shown);
        }

        private void EnsureSelection()
        {
            if (_selected < 0 || _selected >= _source.Count)
                _selected = 0;
        }

        // The index of the body organ with this object name, or -1.
        private int OrganIndexOf(string organObjectName)
        {
            var organs = _quiz.Body.Organs;
            return QuizOrganLookup.IndexOf(organs.Count, i => organs[i] != null ? organs[i].name : string.Empty, organObjectName);
        }

        private string NameOf(int organ)
        {
            var organs = _quiz.Body.Organs;
            return organ >= 0 && organ < organs.Count ? OrganNames.Display(organs[organ].name) : string.Empty;
        }
    }
}
