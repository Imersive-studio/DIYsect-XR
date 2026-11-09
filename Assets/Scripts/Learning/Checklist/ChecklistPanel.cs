// Authority: none. The tracker supplies all learning state; this component owns presentation transitions.
using System;
using TMPro;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The learning guide panel: a "next step" card (headline, hint, chips, badge, progress segments) over a
    /// seven-row list. It only reads <see cref="ChecklistTracker"/>; the timings come from
    /// <see cref="ChecklistTransitionRule"/>. Work per frame is a few colour changes, and nothing at all once the
    /// checklist is complete and the finishing glow has settled.
    /// </summary>
    public class ChecklistPanel : MonoBehaviour
    {
        [SerializeField] private ChecklistTracker _tracker;
        [SerializeField] private ChecklistBodyCues _cues;
        [SerializeField] private Transform _cardRoot;
        [SerializeField] private TMP_Text _headline;
        [SerializeField] private TMP_Text _outgoingHeadline;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private TMP_Text _stepChip;
        [SerializeField] private GameObject _toolChip;
        [SerializeField] private TMP_Text _toolChipText;
        [SerializeField] private GuideShape _badge;
        [SerializeField] private TMP_Text _badgeNumber;
        [SerializeField] private GuideShape _halo;
        [SerializeField] private GuideShape[] _segments;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GuideShape _highlight;
        [SerializeField] private TMP_Text[] _rows;
        [SerializeField] private GuideShape[] _nodes;
        [SerializeField] private LineRenderer[] _ticks;
        [SerializeField] private GuideShape[] _connectors;

        private const float PulsePeriod = 1.6f;
        private const float RowSpacing = 0.05f;
        private const float FirstRowY = 0.125f;
        private const float SlideSeconds = 0.25f;
        private const float PopDelay = 0.2f;
        private const float PopSeconds = 0.3f;
        private const float FinishGlowSeconds = 1.5f;
        private const float FinishSettleSeconds = 0.5f;
        private const float FinishGlowAlpha = 0.22f;
        private const float FinishRestAlpha = 0.14f;
        private const float MotionSeconds = 2.2f;

        // Colours that never change are built once, so a frame never allocates.
        private static readonly Color BadgeBright = Color.Lerp(GuidePalette.Coral, GuidePalette.TextPrimary, 0.3f);

        private ChecklistState _lastRendered;
        private ChecklistTransition _transition = ChecklistTransition.Snap;
        private float _started;
        private int _firstDone;
        private bool _refreshRequested;
        private bool _settled = true;
        private float _highlightFrom;
        private float _highlightTo;

        public event Action<ChecklistTransition> TransitionChanged;
        public ChecklistTransition Transition => _transition;

        /// <summary>The headline of the step now shown.</summary>
        public string Text => _headline != null ? _headline.text : string.Empty;

        private void Awake()
        {
            if (!HasReferences(out string problem))
            {
                Debug.LogError($"{name}: checklist presentation references are missing ({problem}). Run Tools > DIYsect > Add Dissection Checklist.", this);
                enabled = false;
                return;
            }

            // DrawTick writes three points in the line's own space, so make sure the line can take them.
            for (int i = 0; i < _ticks.Length; i++)
            {
                if (_ticks[i].positionCount != 3) _ticks[i].positionCount = 3;
                _ticks[i].useWorldSpace = false;
            }
        }

        private bool HasReferences(out string problem)
        {
            problem = null;
            if (_tracker == null) problem = "tracker";
            else if (_cues == null) problem = "cues";
            else if (_cardRoot == null) problem = "card root";
            else if (_headline == null) problem = "headline";
            else if (_outgoingHeadline == null) problem = "outgoing headline";
            else if (_hint == null) problem = "hint";
            else if (_stepChip == null) problem = "step chip";
            else if (_toolChip == null) problem = "tool chip";
            else if (_toolChipText == null) problem = "tool chip text";
            else if (_badge == null) problem = "badge";
            else if (_badgeNumber == null) problem = "badge number";
            else if (_halo == null) problem = "halo";
            else if (_count == null) problem = "count";
            else if (_highlight == null) problem = "highlight";
            else if (!ArrayOk(_segments, ChecklistLabels.StepCount)) problem = "segments need 7 entries";
            else if (!ArrayOk(_rows, ChecklistLabels.StepCount)) problem = "rows need 7 entries";
            else if (!ArrayOk(_nodes, ChecklistLabels.StepCount)) problem = "nodes need 7 entries";
            else if (!ArrayOk(_ticks, ChecklistLabels.StepCount)) problem = "ticks need 7 entries";
            else if (!ArrayOk(_connectors, ChecklistLabels.StepCount - 1)) problem = "connectors need 6 entries";
            return problem == null;
        }

        private static bool ArrayOk<T>(T[] items, int length) where T : UnityEngine.Object
        {
            if (items == null || items.Length != length) return false;
            for (int i = 0; i < items.Length; i++)
                if (items[i] == null) return false;
            return true;
        }

        private void OnEnable()
        {
            if (!enabled || _tracker == null) return;
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
            if (!_settled)
            {
                ApplyMotion(elapsed);
                if (elapsed >= MotionSeconds) _settled = true;
            }
            if (_tracker.State != ChecklistState.Complete)
                ApplyPulse(Time.unscaledTime);
        }

        // Everything that is fixed for a state: texts, shapes and colours. Motion is applied after, in ApplyMotion.
        private void Render(ChecklistTransition transition)
        {
            _refreshRequested = false;
            ChecklistState state = _tracker.State;
            bool complete = state == ChecklistState.Complete;
            bool snap = transition == ChecklistTransition.Snap;
            bool cheering = transition == ChecklistTransition.Advance || transition == ChecklistTransition.Complete;

            _firstDone = (int)_lastRendered;
            _cues.OutgoingStep = _lastRendered;
            _outgoingHeadline.text = cheering ? ChecklistLabels.Cheer(_lastRendered) : ChecklistLabels.Headline(_lastRendered);
            _outgoingHeadline.color = cheering ? GuidePalette.Mint : GuidePalette.TextPrimary;
            _lastRendered = state;
            _transition = transition;
            _started = Time.unscaledTime;
            _settled = false;

            _headline.text = ChecklistLabels.Headline(state);
            _hint.text = ChecklistLabels.Hint(state);
            _stepChip.text = ChecklistLabels.ProgressChip(state);
            string tool = ChecklistLabels.ToolName(state);
            _toolChip.SetActive(tool.Length > 0);
            _toolChipText.text = tool;
            _badgeNumber.text = ChecklistLabels.Badge(state);
            _count.text = ChecklistLabels.Count(state);
            _badge.Color = complete ? GuidePalette.Mint : GuidePalette.Coral;
            _halo.Color = complete && snap ? HaloColor(FinishRestAlpha) : GuidePalette.MintGlow;

            int current = (int)state;
            for (int i = 0; i < ChecklistLabels.StepCount; i++)
            {
                bool done = i < current;
                bool now = i == current;
                _segments[i].Color = done || complete ? GuidePalette.Mint : now ? GuidePalette.Coral : GuidePalette.Slate;
                _nodes[i].SetKind(done || now ? GuideShapeKind.Disc : GuideShapeKind.Ring);
                _nodes[i].Color = done ? GuidePalette.Mint : now ? GuidePalette.Coral : GuidePalette.Slate;
                _rows[i].text = ChecklistLabels.StepText(i);
                _rows[i].color = done ? GuidePalette.TextDone : now ? GuidePalette.TextPrimary : GuidePalette.TextMuted;
                _rows[i].fontStyle = now ? FontStyles.Bold : FontStyles.Normal;
                if (i < _connectors.Length)
                    _connectors[i].Color = i + 1 < current ? GuidePalette.Mint : GuidePalette.Slate;
            }

            // The highlight slides from wherever it is now to the current row, and hides when everything is done.
            _highlightFrom = _highlight.transform.localPosition.y;
            _highlightTo = FirstRowY - Mathf.Min(current, ChecklistLabels.StepCount - 1) * RowSpacing;
            if (snap) _highlightFrom = _highlightTo;
            _highlight.gameObject.SetActive(!complete);

            float cardFade = snap ? 1f : 0f;
            float outgoing = snap ? 0f : 1f;
            _headline.alpha = cardFade;
            _hint.alpha = cardFade;
            _outgoingHeadline.alpha = outgoing;
            _cues.Visibility = cardFade;
            _cues.OutgoingVisibility = outgoing;
            TransitionChanged?.Invoke(transition);
        }

        // Fades, tick drawing, node pop, highlight slide and the finishing glow, all driven by time since Render.
        private void ApplyMotion(float elapsed)
        {
            ChecklistState state = _tracker.State;
            bool advancing = _transition == ChecklistTransition.Advance || _transition == ChecklistTransition.Complete;
            float duration = ChecklistTransitionRule.Duration(_transition);
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float cardFade = advancing ? Mathf.Clamp01((elapsed - 0.45f) / 0.25f) : t;
            float outgoingFade = advancing ? 1f - Mathf.Clamp01((elapsed - 0.2f) / 0.25f) : 1f - t;
            if (_transition == ChecklistTransition.Snap)
            {
                cardFade = 1f;
                outgoingFade = 0f;
            }
            _headline.alpha = cardFade;
            _hint.alpha = cardFade;
            _outgoingHeadline.alpha = outgoingFade;
            _cues.Visibility = cardFade;
            _cues.OutgoingVisibility = outgoingFade;

            int done = (int)state;
            for (int i = 0; i < ChecklistLabels.StepCount; i++)
            {
                bool ticked = i < done;
                bool fresh = advancing && i >= _firstDone;
                _ticks[i].enabled = ticked;
                if (ticked) DrawTick(_ticks[i], fresh ? Mathf.Clamp01(elapsed / 0.2f) : 1f);
                float pop = 1f;
                if (ticked && fresh)
                {
                    float u = Mathf.Clamp01((elapsed - PopDelay) / PopSeconds);
                    pop = 1f + 0.25f * Mathf.Sin(u * Mathf.PI);
                }
                _nodes[i].transform.localScale = new Vector3(pop, pop, 1f);
            }

            if (state != ChecklistState.Complete)
            {
                float slide = _transition == ChecklistTransition.Snap ? 1f : Mathf.Clamp01(elapsed / SlideSeconds);
                float eased = 1f - (1f - slide) * (1f - slide);
                Vector3 place = _highlight.transform.localPosition;
                place.y = Mathf.Lerp(_highlightFrom, _highlightTo, eased);
                _highlight.transform.localPosition = place;
            }
            else
            {
                float since = _transition == ChecklistTransition.Snap ? FinishGlowSeconds + FinishSettleSeconds : elapsed;
                float alpha = since < FinishGlowSeconds
                    ? FinishGlowAlpha
                    : Mathf.Lerp(FinishGlowAlpha, FinishRestAlpha, Mathf.Clamp01((since - FinishGlowSeconds) / FinishSettleSeconds));
                _halo.Color = HaloColor(alpha);
            }
        }

        // The current node, current segment and badge breathe between two colours.
        private void ApplyPulse(float time)
        {
            int current = (int)_tracker.State;
            _nodes[current].Color = GuidePalette.Pulse(GuidePalette.Coral, GuidePalette.CoralSoft, time, PulsePeriod);
            _segments[current].Color = GuidePalette.Pulse(GuidePalette.Coral, GuidePalette.CoralSoft, time, PulsePeriod);
            _badge.Color = GuidePalette.Pulse(GuidePalette.Coral, BadgeBright, time, PulsePeriod);
        }

        private static Color HaloColor(float alpha)
        {
            Color color = GuidePalette.Mint;
            color.a = alpha;
            return color;
        }

        // A tick sized to sit inside a 0.026 m node.
        private static void DrawTick(LineRenderer line, float fraction)
        {
            Vector3 start = new Vector3(-0.006f, 0.0005f, 0f);
            Vector3 middle = new Vector3(-0.0015f, -0.0045f, 0f);
            Vector3 end = new Vector3(0.0065f, 0.005f, 0f);
            Vector3 second = Vector3.Lerp(start, middle, Mathf.Clamp01(fraction * 3f));
            line.SetPosition(0, start);
            line.SetPosition(1, second);
            line.SetPosition(2, fraction < 1f / 3f ? second : Vector3.Lerp(middle, end, (fraction - 1f / 3f) * 1.5f));
        }
    }
}
