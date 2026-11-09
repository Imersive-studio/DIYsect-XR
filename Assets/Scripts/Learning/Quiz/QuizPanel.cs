// Authority: none. The professor's controls only ask the server through QuizRound; the server decides.
using System.Collections.Generic;
using UnityEngine;
using DIYsect.Interaction;

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
        [SerializeField] private TextMesh _status;
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
                Debug.LogError($"{name}: QuizPanel needs a QuizRound, a status TextMesh and a quiz question set with at least one question. Run Tools > DIYsect > Add Quiz.", this);
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

            string text = QuizMessages.ForProfessor(phase, NameOf(shown), _quiz.RightCount, _quiz.WrongCount, wrongPicks, prompt);
            _status.text = string.IsNullOrEmpty(_notice) ? text : $"{text}\n{_notice}";
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
