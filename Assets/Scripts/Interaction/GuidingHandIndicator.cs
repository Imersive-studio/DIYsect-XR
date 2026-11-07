// Authority: none. Presentation only; it reads the replicated state of GuidingHand and ScalpelSteering.
using System;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Shows who owns the scalpel's depth on every client: a coloured ring around the blade and a short label.
    /// Grey "Depth: Student" (or "Depth: You" for the student) while the student sets the depth, orange
    /// "Depth: Professor" while a guide controls it, green "Depth: You" for the guide themself. Nothing is shown
    /// while nobody holds the scalpel. It reacts to events from the networked components and needs no Update.
    /// </summary>
    [RequireComponent(typeof(GuidingHand), typeof(ScalpelSteering))]
    public class GuidingHandIndicator : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer _ring;
        [SerializeField] private TextMesh _label;
        [SerializeField] private Billboard _labelBillboard;
        [SerializeField] private Color _studentColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color _otherGuideColor = new Color(1f, 0.55f, 0.05f);
        [SerializeField] private Color _localGuideColor = new Color(0.1f, 0.8f, 0.3f);

        private GuidingHand _hand;
        private ScalpelSteering _steering;
        private MaterialPropertyBlock _block;

        /// <summary>Raised when the shown state changes. Hook for tests and other feedback (sound, haptics).</summary>
        public event Action<GuidingState> StateChanged;

        public GuidingState State { get; private set; } = GuidingState.Idle;

        /// <summary>The label text, empty while nothing is shown.</summary>
        public string LabelText { get; private set; } = string.Empty;

        public Color RingColor { get; private set; }

        private void Awake()
        {
            _hand = GetComponent<GuidingHand>();
            _steering = GetComponent<ScalpelSteering>();
            _block = new MaterialPropertyBlock();
            if (_ring == null || _label == null || _labelBillboard == null)
            {
                Debug.LogError($"{name}: GuidingHandIndicator needs the ring, label and billboard. Run Tools > DIYsect > Add Placeholder Scalpel.", this);
                enabled = false;
                return;
            }
            Show(GuidingState.Idle);
        }

        private void OnEnable()
        {
            _hand.ControllerChanged += OnOwnershipChanged;
            _steering.SteererChanged += OnOwnershipChanged;
            Refresh();
        }

        private void OnDisable()
        {
            _hand.ControllerChanged -= OnOwnershipChanged;
            _steering.SteererChanged -= OnOwnershipChanged;
        }

        private void OnOwnershipChanged(ulong unused)
        {
            Refresh();
        }

        private void Refresh()
        {
            GuidingState next;
            if (!_steering.HasSteerer)
                next = GuidingState.Idle;
            else if (!_hand.HasController)
                next = GuidingState.StudentDepth;
            else
                next = _hand.IsControlledByLocalClient ? GuidingState.GuidedByLocal : GuidingState.GuidedByOther;

            // The label for the free state depends on who is looking, so redraw even when the state is unchanged.
            bool changed = next != State;
            Show(next);
            if (changed)
                StateChanged?.Invoke(next);
        }

        private void Show(GuidingState state)
        {
            State = state;
            bool visible = state != GuidingState.Idle;
            _ring.gameObject.SetActive(visible);
            _label.gameObject.SetActive(visible);
            _labelBillboard.enabled = visible;

            switch (state)
            {
                case GuidingState.StudentDepth:
                    LabelText = _steering.IsSteeredByLocalClient ? "Depth: You" : "Depth: Student";
                    RingColor = _studentColor;
                    break;
                case GuidingState.GuidedByOther:
                    LabelText = "Depth: Professor";
                    RingColor = _otherGuideColor;
                    break;
                case GuidingState.GuidedByLocal:
                    LabelText = "Depth: You";
                    RingColor = _localGuideColor;
                    break;
                default:
                    LabelText = string.Empty;
                    break;
            }

            if (!visible)
                return;
            _label.text = LabelText;
            _label.color = RingColor;
            _ring.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, RingColor);
            _block.SetColor(_colorId, RingColor);
            _ring.SetPropertyBlock(_block);
        }
    }
}
