// Authority: none. The tracker supplies all learning state; this component owns presentation transitions.
using System;
using TMPro;
using UnityEngine;

namespace DIYsect.Learning
{
    public class ChecklistPanel : MonoBehaviour
    {
        [SerializeField] private ChecklistTracker _tracker;
        [SerializeField] private TMP_Text _card;
        [SerializeField] private TMP_Text _outgoingCard;
        [SerializeField] private TMP_Text[] _rows;
        [SerializeField] private LineRenderer[] _ticks;
        [SerializeField] private Transform _cardRoot;
        [SerializeField] private ChecklistBodyCues _cues;

        private static readonly Color Done = new Color(0.55f, 1f, 0.85f);
        private static readonly Color Later = new Color(0.76f, 0.8f, 0.85f);
        private ChecklistState _lastRendered;
        private ChecklistTransition _transition = ChecklistTransition.Snap;
        private float _started;
        private int _firstTick;
        private Vector3 _cardScale;
        private bool _refreshRequested;

        public event Action<ChecklistTransition> TransitionChanged;
        public ChecklistTransition Transition => _transition;
        public string Text => _card != null ? _card.text : string.Empty;

        private void Awake()
        {
            if (_tracker == null || _card == null || _outgoingCard == null || _cardRoot == null || _cues == null ||
                _rows == null || _rows.Length != ChecklistLabels.StepCount ||
                _ticks == null || _ticks.Length != ChecklistLabels.StepCount)
            {
                Debug.LogError($"{name}: checklist presentation references are missing. Run Tools > DIYsect > Add Dissection Checklist.", this);
                enabled = false;
                return;
            }
            for (int i = 0; i < _rows.Length; i++)
                if (_rows[i] == null || _ticks[i] == null)
                {
                    Debug.LogError($"{name}: checklist row {i} is missing.", this);
                    enabled = false;
                    return;
                }
            _cardScale = _cardRoot.localScale;
        }

        private void OnEnable()
        {
            if (_tracker == null || _card == null || _outgoingCard == null || _cardRoot == null || _cues == null) return;
            _tracker.StateChanged += RequestRefresh;
            _tracker.StateSynchronized += Snap;
            Render(ChecklistTransition.Snap);
        }

        private void OnDisable()
        {
            if (_tracker != null)
            {
                _tracker.StateChanged -= RequestRefresh;
                _tracker.StateSynchronized -= Snap;
            }
            _refreshRequested = false;
        }

        private void RequestRefresh(ChecklistState state) => _refreshRequested = true;
        private void Snap() => Render(ChecklistTransition.Snap);

        private void LateUpdate()
        {
            // Coalesce events from one frame, including an incision followed immediately by skin opening.
            if (_refreshRequested)
                Render(ChecklistTransitionRule.Evaluate(_lastRendered, _tracker.State));
            float elapsed = Time.unscaledTime - _started;
            float duration = ChecklistTransitionRule.Duration(_transition);
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            bool advancing = _transition == ChecklistTransition.Advance || _transition == ChecklistTransition.Complete;
            float cardFade = advancing ? Mathf.Clamp01((elapsed - 0.45f) / 0.25f) : t;
            if (_transition == ChecklistTransition.Snap) cardFade = 1f;
            float outgoingFade = advancing ? 1f - Mathf.Clamp01((elapsed - 0.2f) / 0.25f) : 1f - t;
            if (_transition == ChecklistTransition.Snap) outgoingFade = 0f;
            _outgoingCard.alpha = outgoingFade;
            _cues.OutgoingVisibility = outgoingFade;
            _card.alpha = cardFade;
            _cues.Visibility = cardFade;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool ticked = i < (int)_tracker.State;
                float draw = advancing && i >= _firstTick ? Mathf.Clamp01(elapsed / 0.2f) : 1f;
                _ticks[i].enabled = ticked;
                if (ticked) DrawTick(_ticks[i], draw);
                float settle = advancing && ticked && i >= _firstTick
                    ? 1f + 0.035f * (1f - Mathf.Clamp01((elapsed - 0.2f) / 0.25f)) : 1f;
                _rows[i].transform.localScale = Vector3.one * settle;
            }
            // Once complete the instruction card quietly collapses; the full compact list remains.
            float completeScale = _tracker.State == ChecklistState.Complete
                ? (_transition == ChecklistTransition.Snap ? 0f : 1f - Mathf.Clamp01((elapsed - 0.85f) / 0.35f)) : 1f;
            _cardRoot.localScale = _cardScale * completeScale;
        }

        private void Render(ChecklistTransition transition)
        {
            _refreshRequested = false;
            _firstTick = (int)_lastRendered;
            _cues.OutgoingStep = _lastRendered;
            _outgoingCard.text = ChecklistLabels.Instruction(_lastRendered);
            _lastRendered = _tracker.State;
            _transition = transition;
            _started = Time.unscaledTime;
            _outgoingCard.alpha = transition == ChecklistTransition.Snap ? 0f : 1f;
            _card.alpha = transition == ChecklistTransition.Snap ? 1f : 0f;
            _cues.Visibility = _card.alpha;
            _cues.OutgoingVisibility = _outgoingCard.alpha;
            _card.text = ChecklistLabels.Instruction(_tracker.State);
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].text = ChecklistLabels.StepText(i);
                _rows[i].color = i < (int)_tracker.State ? Done : i == (int)_tracker.State ? Color.white : Later;
                _rows[i].fontStyle = i == (int)_tracker.State ? FontStyles.Bold : FontStyles.Normal;
            }
            TransitionChanged?.Invoke(transition);
        }

        private static void DrawTick(LineRenderer line, float fraction)
        {
            Vector3 start = new Vector3(-0.009f, 0f, 0f);
            Vector3 middle = new Vector3(-0.002f, -0.007f, 0f);
            Vector3 end = new Vector3(0.012f, 0.012f, 0f);
            line.SetPosition(0, start);
            line.SetPosition(1, Vector3.Lerp(start, middle, Mathf.Clamp01(fraction * 3f)));
            line.SetPosition(2, fraction < 1f / 3f ? line.GetPosition(1)
                : Vector3.Lerp(middle, end, (fraction - 1f / 3f) * 1.5f));
        }
    }
}
