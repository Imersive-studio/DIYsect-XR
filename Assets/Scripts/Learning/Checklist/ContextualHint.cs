// Authority: none. Presentation only; it listens to the organ holder event every device already gets and writes nothing.
using DIYsect.Networking;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// A small pill that says "Turn to examine" beside an organ the first time this device holds one. It stays for
    /// <see cref="MotionSpec.HintHold"/>, then dissolves, and goes at once if the organ is let go. It sits just clear of the
    /// organ on the viewer's right (<see cref="LabelAnchor.Beside"/>) so it never covers the anatomy, and it turns to face
    /// the camera. It only runs while it is visible; hidden, it costs nothing.
    /// </summary>
    public class ContextualHint : MonoBehaviour
    {
        [SerializeField] private OrganGrabAuthority _grabs;
        [SerializeField] private SharedBody _body;
        [SerializeField] private GameObject _root;
        [SerializeField] private GuideShape _pill;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private LineRenderer _glyph;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly RevealTimeline _timeline = new RevealTimeline(MotionSpec.LabelIn, MotionSpec.LabelOut);
        private MaterialPropertyBlock _glyphBlock;
        private Color _pillColor;
        private Color _textColor;
        private Camera _camera;
        private Transform _organ;
        private int _organIndex = -1;
        private float _shownAt;
        private bool _consumed;

        /// <summary>True once the hint has been shown on this device. It is shown only once.</summary>
        public bool HasBeenShown => _consumed;

        public RevealPhase Phase => _timeline.Phase;

        private void Awake()
        {
            if (_grabs == null || _body == null || _root == null || _pill == null || _text == null || _glyph == null)
            {
                Debug.LogError($"{name}: the contextual hint references are missing. Run Tools > DIYsect > Add Dissection Checklist.", this);
                enabled = false;
                return;
            }
            _glyphBlock = new MaterialPropertyBlock();
            _pillColor = _pill.Color;
            _textColor = _text.color;
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            if (_grabs == null) return;
            _grabs.HolderChanged += OnHolderChanged;
            _timeline.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (_grabs != null) _grabs.HolderChanged -= OnHolderChanged;
            _timeline.PhaseChanged -= OnPhaseChanged;
            _timeline.Snap(false);
            if (_root != null) _root.SetActive(false);
        }

        private void OnHolderChanged(int organIndex, ulong holder)
        {
            if (holder != OrganGrabAuthority.NoHolder)
            {
                if (_consumed || !_grabs.IsHeldByLocalClient(organIndex)) return;
                if (organIndex < 0 || organIndex >= _body.Organs.Count) return;
                _consumed = true;
                _organIndex = organIndex;
                _organ = _body.Organs[organIndex].transform;
                _camera = Camera.main;
                _shownAt = float.MaxValue;
                _timeline.Show();
                Place();
                Apply();
            }
            else if (organIndex == _organIndex && _timeline.IsVisible)
            {
                _timeline.Hide();
            }
        }

        private void OnPhaseChanged(RevealPhase phase)
        {
            _root.SetActive(phase != RevealPhase.Hidden);
            if (phase == RevealPhase.Shown) _shownAt = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            if (!_timeline.IsVisible) return;
            _timeline.Tick(Time.unscaledDeltaTime);
            if (_timeline.Phase == RevealPhase.Shown && Time.unscaledTime - _shownAt >= MotionSpec.HintHold)
                _timeline.Hide();
            Place();
            Apply();
        }

        // Beside the organ on the viewer's right, turned to face the camera.
        private void Place()
        {
            if (_organ == null) return;
            if (_camera == null) _camera = Camera.main;
            Bounds bounds = LabelAnchor.RendererBounds(_organ, transform);
            Vector3 right = _camera != null ? _camera.transform.right : Vector3.right;
            float rise = MotionSpec.LabelRise * (1f - Ease.OutCubic(_timeline.Amount));
            Transform root = _root.transform;
            root.position = LabelAnchor.Beside(bounds, right) + Vector3.up * rise;
            if (_camera != null)
            {
                Vector3 away = root.position - _camera.transform.position;
                if (away.sqrMagnitude > 1e-6f) root.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
        }

        private void Apply()
        {
            float alpha = Ease.OutCubic(_timeline.Amount);
            Color pill = _pillColor;
            pill.a *= alpha;
            _pill.Color = pill;
            Color text = _textColor;
            text.a *= alpha;
            _text.color = text;
            Color glyph = GuidePalette.Mint;
            glyph.a *= alpha;
            _glyphBlock.SetColor(BaseColor, glyph);
            _glyph.SetPropertyBlock(_glyphBlock);
        }
    }
}
