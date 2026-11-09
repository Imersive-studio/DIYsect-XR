// Authority: none. Presentation only; everything shown is derived from the replicated AssistPhase of the AssistBoard.
using DIYsect.Interaction;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The help beacon above the body, the same on every device: a soft ring with a ripple and a short label.
    /// Coral "Student needs help" while the request is open, mint "Professor is guiding" while the professor holds the
    /// depth handle. The professor's label also says what to do (grab the depth handle), the student's says what they can do
    /// (cancel). It sits above the top of the anatomy, never over it, and is rebuilt from the phase alone, so a late joiner or
    /// a scene reload shows the right thing. Showing and hiding run <see cref="RevealTimeline"/>s. The student also gets a quiet
    /// "ask for help" hint for a few seconds when the scene starts and when a request ends.
    /// </summary>
    [RequireComponent(typeof(AssistBoard))]
    public class AssistBeacon : MonoBehaviour
    {
        private const int RingSegments = 64;
        private const float RingRadius = 0.16f;
        private const float RingWidth = 0.010f;
        private const float EchoWidth = 0.006f;
        private const float HoverHeight = 0.30f;
        private const float LabelHeight = 0.15f;
        private const float LabelRise = 0.02f;
        private const float TitleCap = 0.030f;
        private const float SubtitleCap = 0.015f;
        private const float BreathSeconds = 1.8f;
        private const float EchoSeconds = 1.6f;
        private const float TintSeconds = 0.12f;
        private const float HintSeconds = 6f;
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        [SerializeField] private AssistBoard _board;
        [SerializeField, Tooltip("The label font (Inter). Run Tools > DIYsect > Add Help Request.")]
        private TMP_FontAsset _font;
        [SerializeField, Tooltip("A transparent unlit material for the rings. Run Tools > DIYsect > Add Help Request.")]
        private Material _ringMaterial;

        private readonly RevealTimeline _labelTimeline = new RevealTimeline(MotionSpec.LabelIn, MotionSpec.LabelOut);
        private readonly RevealTimeline _ringTimeline = new RevealTimeline(MotionSpec.TitleIn, MotionSpec.TitleOut);
        private MaterialPropertyBlock _block;
        private GameObject _root;
        private Transform _labelRoot;
        private LineRenderer _ring;
        private LineRenderer _echo;
        private TextMeshPro _title;
        private TextMeshPro _subtitle;
        private AssistPhase _phase;
        private bool _hintShown;
        private float _hintUntil;
        private bool _announced;
        private Color _tint;
        private Color _targetTint;
        private Color _appliedTint;
        private Color _ringTint;
        private Color _ringTarget;
        private float _appliedLabel = -1f;

        /// <summary>The label's first line as shown now, empty while hidden. For tests.</summary>
        public string TitleText => _title != null ? _title.text : string.Empty;

        /// <summary>The label's second line as shown now. For tests.</summary>
        public string SubtitleText => _subtitle != null ? _subtitle.text : string.Empty;

        /// <summary>True while any part of the beacon can be seen, including the fade out.</summary>
        public bool IsVisible => _labelTimeline.IsVisible || _ringTimeline.IsVisible;

        private void Awake()
        {
            if (_board == null)
                _board = GetComponent<AssistBoard>();
            if (_board == null || _font == null || _ringMaterial == null)
            {
                Debug.LogError($"{name}: AssistBeacon needs the AssistBoard, the font and the ring material. Run Tools > DIYsect > Add Help Request.", this);
                enabled = false;
                return;
            }
            _block = new MaterialPropertyBlock();
            Build();
            _labelTimeline.PhaseChanged += OnTimelinePhase;
            _ringTimeline.PhaseChanged += OnTimelinePhase;
        }

        private void OnEnable()
        {
            _board.PhaseChanged += OnPhaseChanged;
            // A board that spawned before this ran has already announced; read it directly.
            if (_board.IsSpawned)
                OnPhaseChanged(_board.Phase);
        }

        private void OnDisable()
        {
            _board.PhaseChanged -= OnPhaseChanged;
            _hintShown = false;
            _labelTimeline.Snap(false);
            _ringTimeline.Snap(false);
            if (_root != null)
                _root.SetActive(false);
        }

        private void Build()
        {
            _root = new GameObject("AssistBeacon");
            _root.transform.SetParent(transform, false);
            _ring = MakeRing("Ring", RingWidth);
            _echo = MakeRing("Echo", EchoWidth);

            var label = new GameObject("Label");
            label.transform.SetParent(_root.transform, false);
            label.AddComponent<Billboard>();
            _labelRoot = label.transform;
            _title = LabelText.Create(_labelRoot, "Title", _font, TitleCap, GuidePalette.Coral, FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0.9f, 0.05f), new Vector2(0.5f, 0.5f), false, onTop: true);
            _title.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            _subtitle = LabelText.Create(_labelRoot, "Subtitle", _font, SubtitleCap, GuidePalette.TextMuted, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0.9f, 0.03f), new Vector2(0.5f, 0.5f), false, onTop: true);
            _subtitle.transform.localPosition = new Vector3(0f, -0.028f, 0f);
            _root.SetActive(false);
        }

        private LineRenderer MakeRing(string objectName, float width)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(_root.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = RingSegments;
            line.widthMultiplier = width;
            line.sharedMaterial = _ringMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * 2f * Mathf.PI;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
            }
            return line;
        }

        private void OnPhaseChanged(AssistPhase phase)
        {
            AssistPhase previous = _phase;
            _phase = phase;
            bool student = ClassroomRole.Current == ClassroomRoleKind.Student;

            if (phase != AssistPhase.None)
            {
                _hintShown = false;
                Compose(out string title, out string subtitle, out Color tint);
                bool wasHidden = !IsVisible;
                if (wasHidden)
                    Place();
                SetText(title, subtitle, tint, snapTint: wasHidden);
                _ringTarget = tint;
                if (wasHidden)
                    _ringTint = tint;
                _ringTimeline.Show();
                _labelTimeline.Show();
                _echo.gameObject.SetActive(phase == AssistPhase.Requested);
            }
            else
            {
                _ringTimeline.Hide();
                // The student gets a gentle reminder of the button at the start and after a request ends.
                if (student && (!_announced || previous != AssistPhase.None))
                    ShowHint();
                else if (!_hintShown)
                    _labelTimeline.Hide();
            }
            _announced = true;
        }

        private void ShowHint()
        {
            _hintShown = true;
            _hintUntil = Time.unscaledTime + HintSeconds;
            if (!IsVisible)
                Place();
            Compose(out string title, out string subtitle, out Color tint);
            SetText(title, subtitle, tint, snapTint: true);
            _labelTimeline.Show();
        }

        // The words depend on who is looking: the student sees their own request, the professor sees what to do.
        private void Compose(out string title, out string subtitle, out Color tint)
        {
            bool professor = ClassroomRole.Current == ClassroomRoleKind.Professor;
            bool mine = _board.IsLocalRequest;
            switch (_phase)
            {
                case AssistPhase.Requested:
                    tint = GuidePalette.Coral;
                    title = mine ? "Help requested" : "Student needs help";
                    subtitle = mine ? "Press Y again to cancel" : professor ? "Grab the depth handle to guide" : string.Empty;
                    break;
                case AssistPhase.Helping:
                    tint = GuidePalette.Mint;
                    title = "Professor is guiding";
                    subtitle = mine ? "You keep steering, the depth is guided" : professor ? "Let go of the handle to finish" : string.Empty;
                    break;
                default:
                    tint = GuidePalette.TextMuted;
                    title = "Press Y to ask for help";
                    subtitle = string.Empty;
                    break;
            }
        }

        private void SetText(string title, string subtitle, Color tint, bool snapTint)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            _subtitle.gameObject.SetActive(subtitle.Length > 0);
            _targetTint = tint;
            if (snapTint)
                _tint = tint;
            _appliedLabel = -1f;
        }

        private void OnTimelinePhase(RevealPhase phase)
        {
            if (_root == null)
                return;
            bool visible = IsVisible;
            if (_root.activeSelf != visible)
                _root.SetActive(visible);
            _ring.gameObject.SetActive(_ringTimeline.IsVisible);
            _echo.gameObject.SetActive(_ringTimeline.IsVisible && _phase == AssistPhase.Requested);
            if (!_labelTimeline.IsVisible)
                _appliedLabel = -1f;
        }

        // Above the top of the anatomy, never over it. Skips the scalpel, text and lines, which move or float.
        private void Place()
        {
            bool found = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>())
            {
                bool solid = candidate is MeshRenderer || candidate is SkinnedMeshRenderer;
                if (!solid || !candidate.enabled || candidate.transform.IsChildOf(_root.transform)
                    || candidate.GetComponent<TMP_Text>() != null || candidate.GetComponentInParent<DIYsect.Networking.ScalpelSteering>() != null)
                    continue;
                if (found)
                    bounds.Encapsulate(candidate.bounds);
                else
                    bounds = candidate.bounds;
                found = true;
            }
            Vector3 anchor = found ? LabelAnchor.Above(bounds, HoverHeight) : transform.position + Vector3.up * 0.6f;
            Vector3 scale = transform.lossyScale;
            // Keep the beacon's size in the world the same whatever the body's scale.
            _root.transform.localScale = new Vector3(SafeInverse(scale.x), SafeInverse(scale.y), SafeInverse(scale.z));
            _root.transform.SetPositionAndRotation(anchor, Quaternion.identity);
            _labelRoot.localPosition = new Vector3(0f, LabelHeight, 0f);
        }

        private static float SafeInverse(float value)
        {
            return Mathf.Abs(value) > 1e-4f ? 1f / value : 1f;
        }

        // Polling exception: the ripple, the colour blend and the fades move every frame and have no change event.
        // It returns at once while nothing is visible.
        private void LateUpdate()
        {
            if (!IsVisible)
                return;
            float delta = Time.deltaTime;
            _labelTimeline.Tick(delta);
            _ringTimeline.Tick(delta);
            if (_hintShown && _phase == AssistPhase.None && Time.unscaledTime >= _hintUntil)
            {
                _hintShown = false;
                _labelTimeline.Hide();
            }
            float blend = 1f - Mathf.Exp(-delta / TintSeconds);
            _tint = Color.Lerp(_tint, _targetTint, blend);
            _ringTint = Color.Lerp(_ringTint, _ringTarget, blend);
            AnimateLabel();
            AnimateRings();
        }

        private void AnimateLabel()
        {
            float label = Ease.OutCubic(_labelTimeline.Amount);
            if (Mathf.Approximately(label, _appliedLabel) && _tint == _appliedTint)
                return;
            _appliedLabel = label;
            _appliedTint = _tint;
            _title.color = new Color(_tint.r, _tint.g, _tint.b, label);
            Color muted = GuidePalette.TextMuted;
            _subtitle.color = new Color(muted.r, muted.g, muted.b, label * 0.95f);
            _labelRoot.localPosition = new Vector3(0f, LabelHeight - LabelRise * (1f - label), 0f);
        }

        private void AnimateRings()
        {
            if (!_ringTimeline.IsVisible)
                return;
            float reveal = Ease.OutCubic(_ringTimeline.Amount);
            float time = Time.time;
            bool asking = _phase == AssistPhase.Requested;
            float breath = 0.5f - 0.5f * Mathf.Cos(time / BreathSeconds * 2f * Mathf.PI);
            float ringAlpha = reveal * (asking ? 0.65f + 0.3f * breath : 0.7f);
            _ring.transform.localScale = Vector3.one * (RingRadius * (0.9f + 0.1f * reveal) * (1f + (asking ? 0.04f * breath : 0f)));
            Paint(_ring, ringAlpha);
            if (!asking)
                return;
            // The ripple: a second ring grows outward and fades, over and over, so the call reads from across the room.
            float echoProgress = Mathf.Repeat(time / EchoSeconds, 1f);
            _echo.transform.localScale = Vector3.one * (RingRadius * (1f + 0.9f * Ease.OutCubic(echoProgress)));
            float fade = 1f - echoProgress;
            Paint(_echo, reveal * 0.55f * fade * fade);
        }

        private void Paint(LineRenderer line, float alpha)
        {
            Color color = new Color(_ringTint.r, _ringTint.g, _ringTint.b, Mathf.Clamp01(alpha));
            line.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, color);
            _block.SetColor(_colorId, color);
            line.SetPropertyBlock(_block);
        }
    }
}
