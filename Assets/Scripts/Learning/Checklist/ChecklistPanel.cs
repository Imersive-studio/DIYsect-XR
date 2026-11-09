// Authority: none. The tracker supplies all learning state; this component owns presentation transitions.
using System;
using TMPro;
using UnityEngine;
using DIYsect.Networking;
using DIYsect.Presentation;

namespace DIYsect.Learning
{
    /// <summary>
    /// The learning guide panel: a "next step" card (icon tile, action headline, gesture hint, chips, progress
    /// segments) over a seven-row list. It only reads <see cref="ChecklistTracker"/>. When a step is finished the old
    /// headline shrinks into a check mark that draws itself, then the old text rolls out while the next one rolls in;
    /// the phases are <see cref="InstructionTransitionPhase"/> moved by <see cref="InstructionTransitionRule"/> and
    /// timed by <see cref="RevealTimeline"/>. Work per frame is a few colour and scale changes.
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
        [SerializeField] private GuideShape _tileRim;
        [SerializeField] private Transform _iconRoot;
        [SerializeField] private LineRenderer[] _iconStrokes;
        [SerializeField] private LineRenderer _checkMark;
        [SerializeField, Tooltip("Optional. When set, the icon gives a small pop when this device grabs an organ.")]
        private OrganGrabAuthority _grabs;
        [SerializeField] private GuideShape _halo;
        [SerializeField] private GuideShape[] _segments;
        [SerializeField] private TMP_Text _count;
        [SerializeField] private GuideShape _highlight;
        [SerializeField] private TMP_Text[] _rows;
        [SerializeField] private GuideShape[] _nodes;
        [SerializeField] private LineRenderer[] _ticks;
        [SerializeField] private GuideShape[] _connectors;

        private const float RowSpacing = 0.05f;
        private const float FirstRowY = 0.125f;
        private const float SlideSeconds = 0.25f;
        private const float RollRise = 0.006f;
        private const float CheckSize = 0.024f;
        private const float CheckX = -0.12f;
        private const float IconHalf = 0.032f;
        private const float HandPopSeconds = 0.3f;
        private const float PopDelay = 0.2f;
        private const float PopSeconds = 0.3f;
        private const float FinishGlowSeconds = 1.5f;
        private const float FinishSettleSeconds = 0.5f;
        private const float FinishGlowAlpha = 0.22f;
        private const float FinishRestAlpha = 0.14f;
        private const float MotionSeconds = 2.2f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // One clock per animated part. The check draws over CheckIn; the roll in and roll out run side by side.
        private readonly RevealTimeline _check = new RevealTimeline(MotionSpec.CheckIn, MotionSpec.CheckIn);
        private readonly RevealTimeline _rollIn = new RevealTimeline(MotionSpec.InstructionIn, MotionSpec.InstructionOut);
        private readonly RevealTimeline _rollOut = new RevealTimeline(MotionSpec.InstructionIn, MotionSpec.InstructionOut);
        private InstructionTransitionPhase _phase = InstructionTransitionPhase.Idle;
        private bool _outgoingChecked;
        private bool _arming;
        private Vector3 _headlineBase;
        private Vector3 _hintBase;
        private float _outgoingWidth;
        private float _popStarted = -10f;
        private MaterialPropertyBlock _iconBlock;
        private MaterialPropertyBlock _checkBlock;

        private ChecklistState _lastRendered;
        private ChecklistTransition _transition = ChecklistTransition.Snap;
        private float _started;
        private int _firstDone;
        private bool _refreshRequested;
        private bool _settled = true;
        private float _highlightFrom;
        private float _highlightTo;

        public event Action<ChecklistTransition> TransitionChanged;
        /// <summary>Raised when the instruction card enters a new <see cref="InstructionTransitionPhase"/>.</summary>
        public event Action<InstructionTransitionPhase> InstructionPhaseChanged;
        public InstructionTransitionPhase InstructionPhase => _phase;
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
            foreach (LineRenderer stroke in _iconStrokes) PrepareLine(stroke, 0.004f);
            PrepareLine(_checkMark, 0.005f);
            _checkMark.positionCount = 3;
            _checkMark.enabled = false;
            _iconBlock = new MaterialPropertyBlock();
            _checkBlock = new MaterialPropertyBlock();
            _headlineBase = _headline.transform.localPosition;
            _hintBase = _hint.transform.localPosition;
            _outgoingWidth = _outgoingHeadline.rectTransform.sizeDelta.x;
            _check.PhaseChanged += OnCheckPhase;
            _rollIn.PhaseChanged += OnRollPhase;
            _rollOut.PhaseChanged += OnRollPhase;
        }

        private static void PrepareLine(LineRenderer line, float width)
        {
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
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
            else if (_tileRim == null) problem = "icon tile rim";
            else if (_iconRoot == null) problem = "icon root";
            else if (_checkMark == null) problem = "check mark";
            else if (!ArrayOk(_iconStrokes, ChecklistIconShapes.MaxStrokes)) problem = "icon strokes need " + ChecklistIconShapes.MaxStrokes + " entries";
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
            if (_grabs != null) _grabs.HolderChanged += OnHolderChanged;
            Render(ChecklistTransition.Snap);
        }

        private void OnDisable()
        {
            if (_tracker != null)
            {
                _tracker.StateChanged -= RequestRefresh;
                _tracker.StateSynchronized -= Snap;
            }
            if (_grabs != null) _grabs.HolderChanged -= OnHolderChanged;
            _refreshRequested = false;
        }

        private void OnDestroy()
        {
            _check.PhaseChanged -= OnCheckPhase;
            _rollIn.PhaseChanged -= OnRollPhase;
            _rollOut.PhaseChanged -= OnRollPhase;
        }

        // A small pop of the icon when this device's hand takes an organ.
        private void OnHolderChanged(int organ, ulong holder)
        {
            if (holder != OrganGrabAuthority.NoHolder && _grabs.IsHeldByLocalClient(organ))
                _popStarted = Time.unscaledTime;
        }

        private void RequestRefresh(ChecklistState state) => _refreshRequested = true;
        private void Snap() => Render(ChecklistTransition.Snap);

        private void LateUpdate()
        {
            // Coalesce events from one frame, including an incision followed immediately by skin opening.
            if (_refreshRequested)
                Render(ChecklistTransitionRule.Evaluate(_lastRendered, _tracker.State));
            if (_phase != InstructionTransitionPhase.Idle)
            {
                float step = Time.unscaledDeltaTime;
                _check.Tick(step);
                _rollIn.Tick(step);
                _rollOut.Tick(step);
                ApplyInstruction();
            }
            ApplyIcon(Time.unscaledTime);
            float elapsed = Time.unscaledTime - _started;
            if (!_settled)
            {
                ApplyMotion(elapsed);
                if (elapsed >= MotionSeconds) _settled = true;
            }
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
            // The finished headline is struck through (it will shrink into a check); an undone one just rolls out.
            _outgoingChecked = cheering;
            _outgoingHeadline.text = ChecklistLabels.Headline(_lastRendered);
            _outgoingHeadline.fontStyle = cheering ? FontStyles.Bold | FontStyles.Strikethrough : FontStyles.Bold;
            _outgoingHeadline.color = cheering ? GuidePalette.TextDone : GuidePalette.TextPrimary;
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
            _count.text = ChecklistLabels.Count(state);
            _tileRim.Color = complete ? GuidePalette.Mint : GuidePalette.CoralSoft;
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

            if (snap) SetIcon(ChecklistIconRule.For(state));
            Dispatch(InstructionTransitionRule.EventFor(transition));
            ApplyInstruction();
            TransitionChanged?.Invoke(transition);
        }

        // ---- Instruction card: one phase enum, one rule table, timed by three reveal clocks. ----

        private void OnCheckPhase(RevealPhase phase)
        {
            if (!_arming && phase == RevealPhase.Shown && _phase == InstructionTransitionPhase.Completing)
                Dispatch(InstructionTransitionEvent.Finished);
        }

        private void OnRollPhase(RevealPhase phase)
        {
            if (_arming || _phase != InstructionTransitionPhase.Rolling) return;
            if (_rollIn.Phase == RevealPhase.Shown && _rollOut.Phase == RevealPhase.Hidden)
                Dispatch(InstructionTransitionEvent.Finished);
        }

        // The only writer of the phase. It starts the clocks that the new phase needs.
        private void Dispatch(InstructionTransitionEvent change)
        {
            InstructionTransitionPhase previous = _phase;
            _phase = InstructionTransitionRule.Next(previous, change);
            _arming = true;
            switch (_phase)
            {
                case InstructionTransitionPhase.Completing:
                    _check.Snap(false);
                    _rollIn.Snap(false);
                    _rollOut.Snap(false);
                    _check.Show();
                    break;
                case InstructionTransitionPhase.Rolling:
                    SetIcon(ChecklistIconRule.For(_lastRendered));
                    _rollIn.Snap(false);
                    _rollOut.Snap(true);
                    _rollIn.Show();
                    _rollOut.Hide();
                    break;
                default:
                    _check.Snap(false);
                    _rollIn.Snap(true);
                    _rollOut.Snap(false);
                    break;
            }
            _arming = false;
            if (_phase != previous) InstructionPhaseChanged?.Invoke(_phase);
        }

        // Places the headline, hint, outgoing text, check mark and cue lines for the current phase and clock values.
        private void ApplyInstruction()
        {
            switch (_phase)
            {
                case InstructionTransitionPhase.Completing:
                    ApplyCompleting();
                    break;
                case InstructionTransitionPhase.Rolling:
                    ApplyRolling();
                    break;
                default:
                    ApplyIdle();
                    break;
            }
        }

        private void ApplyIdle()
        {
            Place(_headline, _headlineBase, 0f, 1f);
            Place(_hint, _hintBase, 0f, 1f);
            _outgoingHeadline.alpha = 0f;
            _checkMark.enabled = false;
            _cues.Visibility = 1f;
            _cues.OutgoingVisibility = 0f;
        }

        // The old headline slides left and shrinks into the spot where the check draws, then spring-pops.
        private void ApplyCompleting()
        {
            float amount = _check.Amount;
            float squash = Ease.OutCubic(amount);
            float scale = Mathf.Lerp(1f, 0.25f, squash);
            float left = Mathf.Lerp(_headlineBase.x - _outgoingWidth * 0.5f, CheckX, squash);
            Transform outgoing = _outgoingHeadline.transform;
            outgoing.localPosition = new Vector3(left + scale * _outgoingWidth * 0.5f, _headlineBase.y, _headlineBase.z);
            outgoing.localScale = new Vector3(scale, scale, 1f);
            _outgoingHeadline.alpha = 1f - Mathf.Clamp01((amount - 0.3f) / 0.5f);
            float stroke = Mathf.Clamp01(amount / MotionSpec.CheckStrokeShare);
            float spring = Mathf.Clamp01((amount - MotionSpec.CheckStrokeShare) / (1f - MotionSpec.CheckStrokeShare));
            DrawCheck(stroke, Mathf.Lerp(0.85f, 1f, Ease.OutBack(spring)), 1f, 0f);
            _headline.alpha = 0f;
            _hint.alpha = 0f;
            _cues.Visibility = 0f;
            _cues.OutgoingVisibility = 1f;
        }

        // The next headline rolls up into place; the old one (or the check) rolls up and out.
        private void ApplyRolling()
        {
            float entered = Ease.OutCubic(_rollIn.Amount);
            float leaving = 1f - _rollOut.Amount;
            Place(_headline, _headlineBase, -RollRise * (1f - entered), entered);
            Place(_hint, _hintBase, -RollRise * 0.7f * (1f - entered), entered);
            float lift = RollRise * Ease.InCubic(leaving);
            if (_outgoingChecked)
            {
                _outgoingHeadline.alpha = 0f;
                DrawCheck(1f, 1f, 1f - leaving, lift);
            }
            else
            {
                _checkMark.enabled = false;
                Transform outgoing = _outgoingHeadline.transform;
                outgoing.localScale = Vector3.one;
                outgoing.localPosition = _headlineBase + Vector3.up * lift;
                _outgoingHeadline.alpha = 1f - leaving;
            }
            _cues.Visibility = entered;
            _cues.OutgoingVisibility = _rollOut.Amount;
        }

        private void Place(TMP_Text text, Vector3 home, float offsetY, float alpha)
        {
            Transform item = text.transform;
            item.localPosition = home + Vector3.up * offsetY;
            item.localScale = Vector3.one;
            text.alpha = alpha;
        }

        private void DrawCheck(float stroke, float scale, float alpha, float lift)
        {
            ChecklistCheckStroke.Points(stroke, CheckSize, out Vector3 first, out Vector3 second, out Vector3 third);
            _checkMark.SetPosition(0, first);
            _checkMark.SetPosition(1, second);
            _checkMark.SetPosition(2, third);
            Transform mark = _checkMark.transform;
            mark.localPosition = new Vector3(CheckX, _headlineBase.y + lift, _headlineBase.z);
            mark.localScale = new Vector3(scale, scale, 1f);
            Color color = GuidePalette.Mint;
            color.a = alpha;
            _checkBlock.SetColor(BaseColor, color);
            _checkMark.SetPropertyBlock(_checkBlock);
            _checkMark.enabled = true;
        }

        // ---- Icon tile ----

        // Draws the icon's strokes into the pooled lines. Unused lines are switched off.
        private void SetIcon(ChecklistIcon icon)
        {
            Vector2[][] strokes = ChecklistIconShapes.Strokes(icon);
            _iconBlock.SetColor(BaseColor, icon == ChecklistIcon.Check ? GuidePalette.Mint : GuidePalette.TextPrimary);
            for (int i = 0; i < _iconStrokes.Length; i++)
            {
                LineRenderer line = _iconStrokes[i];
                bool used = i < strokes.Length;
                line.enabled = used;
                if (!used) continue;
                Vector2[] points = strokes[i];
                line.positionCount = points.Length;
                for (int j = 0; j < points.Length; j++)
                    line.SetPosition(j, new Vector3(points[j].x * IconHalf, points[j].y * IconHalf, 0f));
                line.SetPropertyBlock(_iconBlock);
            }
        }

        // The board holds still while you work: the icon only pops when the hand takes an organ and as a new icon rolls in.
        private void ApplyIcon(float time)
        {
            float pop = Mathf.Lerp(0.85f, 1f, Ease.OutBack((time - _popStarted) / HandPopSeconds));
            float swap = _phase == InstructionTransitionPhase.Rolling ? Mathf.Lerp(0.75f, 1f, Ease.OutBack(_rollIn.Amount)) : 1f;
            float scale = pop * swap;
            _iconRoot.localScale = new Vector3(scale, scale, 1f);
        }

        // Tick drawing, node pop, highlight slide and the finishing glow, all driven by time since Render.
        private void ApplyMotion(float elapsed)
        {
            ChecklistState state = _tracker.State;
            bool advancing = _transition == ChecklistTransition.Advance || _transition == ChecklistTransition.Complete;
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

        private static Color HaloColor(float alpha)
        {
            Color color = GuidePalette.Mint;
            color.a = alpha;
            return color;
        }

        // A tick sized to sit inside a 0.026 m node.
        private static void DrawTick(LineRenderer line, float fraction)
        {
            ChecklistCheckStroke.Points(fraction, 0.013f, out Vector3 first, out Vector3 second, out Vector3 third);
            line.SetPosition(0, first);
            line.SetPosition(1, second);
            line.SetPosition(2, third);
        }
    }
}
