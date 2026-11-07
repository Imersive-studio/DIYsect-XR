// Authority: none. Presentation and geometry only; it reads the replicated state of GuidingHand and ScalpelSteering.
using DIYsect.Dissection;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Replaces the depth of the blade tip with the guide's value while a guide controls the depth.
    /// "Depth" is how far the tip is below the skin surface, measured along the incision zone's up axis
    /// (0 = at the skin, 1 = <c>_maxDepthMeters</c> below it). The student's own height above or below the skin is
    /// discarded; the sideways and along-the-body position still follow the student's hand.
    /// The override is applied by sliding the <c>_model</c> child (handle, blade, tip, indicator) along that axis,
    /// so the tip trigger, the incision samples and every peer's view all use the same effective tip.
    /// The scalpel sits under the body, so the skin surface is the <see cref="IncisionTrigger"/> of its parent.
    /// This runs in LateUpdate because it has to follow the hand every frame, but only does work while a guide is active.
    /// </summary>
    [RequireComponent(typeof(GuidingHand), typeof(ScalpelSteering))]
    public class DepthOverride : MonoBehaviour
    {
        [SerializeField] private Transform _model;
        [SerializeField] private Transform _tip;
        [SerializeField, Min(0.001f)] private float _maxDepthMeters = 0.03f;

        private GuidingHand _hand;
        private ScalpelSteering _steering;
        private Transform _surface;
        private Vector3 _modelRestPosition;
        private Vector3 _tipLocal;
        private bool _active;
        private bool _reportedMissingSurface;

        public float MaxDepthMeters => _maxDepthMeters;

        /// <summary>True while a guide controls the depth of a scalpel somebody steers.</summary>
        public bool IsOverriding => _active;

        /// <summary>The tip as the student's hand alone places it, ignoring any depth override (world space).</summary>
        public Vector3 StudentTip => transform.TransformPoint(_tipLocal);

        /// <summary>The tip that counts: the student's tip with its depth replaced while a guide controls it (world space).</summary>
        public Vector3 EffectiveTip => _active && TryGetSurface(out Transform surface) ? WithDepth(surface, StudentTip, _hand.Depth) : StudentTip;

        /// <summary>How far below the skin surface a world point is, in metres. Negative above the skin.</summary>
        public float DepthBelowSurface(Vector3 worldPoint)
        {
            return TryGetSurface(out Transform surface) ? -Vector3.Dot(worldPoint - surface.position, surface.up) : 0f;
        }

        private void Awake()
        {
            _hand = GetComponent<GuidingHand>();
            _steering = GetComponent<ScalpelSteering>();
            if (!Validate())
            {
                enabled = false;
                return;
            }
            // The prefab is saved with the model at rest, so the tip's place relative to the root is the student's tip.
            _modelRestPosition = _model.localPosition;
            _tipLocal = transform.InverseTransformPoint(_tip.position);
        }

        private void OnEnable()
        {
            _hand.ControllerChanged += OnStateChanged;
            _steering.SteererChanged += OnStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            _hand.ControllerChanged -= OnStateChanged;
            _steering.SteererChanged -= OnStateChanged;
            _active = false;
            _model.localPosition = _modelRestPosition;
        }

        private void LateUpdate()
        {
            if (_active)
                Apply();
        }

        private void OnStateChanged(ulong unused)
        {
            Refresh();
        }

        private void Refresh()
        {
            _active = _hand.HasController && _steering.HasSteerer;
            if (_active)
                Apply();
            else
                _model.localPosition = _modelRestPosition;
        }

        private void Apply()
        {
            if (!TryGetSurface(out Transform surface))
            {
                if (!_reportedMissingSurface)
                    Debug.LogError($"{name}: the scalpel has no body with an IncisionTrigger as its parent, so the guided depth cannot be applied.", this);
                _reportedMissingSurface = true;
                return;
            }
            Vector3 student = StudentTip;
            Vector3 world = WithDepth(surface, student, _hand.Depth);
            _model.localPosition = _modelRestPosition + transform.InverseTransformVector(world - student);
        }

        private Vector3 WithDepth(Transform surface, Vector3 studentTip, float depth01)
        {
            Vector3 up = surface.up;
            float height = Vector3.Dot(studentTip - surface.position, up);
            float wanted = -Mathf.Clamp01(depth01) * _maxDepthMeters;
            return studentTip + up * (wanted - height);
        }

        private bool TryGetSurface(out Transform surface)
        {
            if (_surface == null && transform.parent != null)
            {
                IncisionTrigger zone = transform.parent.GetComponentInChildren<IncisionTrigger>(true);
                _surface = zone != null ? zone.transform : null;
            }
            surface = _surface;
            return surface != null;
        }

        private bool Validate()
        {
            bool valid = true;
            if (_model == null || _tip == null)
            {
                Debug.LogError($"{name}: DepthOverride needs the model and tip transforms. Run Tools > DIYsect > Add Placeholder Scalpel.", this);
                valid = false;
            }
            else if (!_tip.IsChildOf(_model))
            {
                Debug.LogError($"{name}: the tip must be a child of the model so it moves with the depth override.", this);
                valid = false;
            }
            return valid;
        }
    }
}
