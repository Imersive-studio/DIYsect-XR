// Authority: none. Cues read the tracker and never request a dissection action.
using System;
using DIYsect.Dissection;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Learning
{
    [DefaultExecutionOrder(100)]
    public class ChecklistBodyCues : MonoBehaviour
    {
        [Serializable]
        public struct Cue
        {
            public ChecklistState Step;
            public Transform Target;
            public LineRenderer Line;
            public Vector3[] Points;
            public int OrganIndex;
        }

        [SerializeField] private ChecklistTracker _tracker;
        [SerializeField] private DissectionProgress _dissection;
        [SerializeField] private OrganGrabAuthority _grabs;
        [SerializeField] private Transform[] _organs;
        [SerializeField] private Transform _scalpelTip;
        [SerializeField] private Transform _forcepsTip;
        [SerializeField] private Cue[] _cues;
        [SerializeField] private LineRenderer _arrow;
        private readonly MaterialPropertyBlock _tint = new MaterialPropertyBlock();
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private float _lastProgress;
        private float _incisionProgress;
        private ChecklistCueStrength _strength;

        public float Visibility { get; set; } = 1f;
        public ChecklistState OutgoingStep { get; set; }
        public float OutgoingVisibility { get; set; }
        public ChecklistCueStrength Strength => _strength;
        public event Action<ChecklistCueStrength> StrengthChanged;

        private void Awake()
        {
            if (_tracker == null || _dissection == null || _grabs == null || _scalpelTip == null ||
                _forcepsTip == null || _arrow == null || _cues == null || _cues.Length == 0 || _organs == null)
            {
                Debug.LogError($"{name}: body cue references are missing. Run Tools > DIYsect > Add Dissection Checklist.", this);
                enabled = false;
                return;
            }
            foreach (Cue cue in _cues)
                if (cue.Target == null || cue.Line == null || cue.Points == null || cue.Points.Length < 2)
                {
                    Debug.LogError($"{name}: a body cue has no target, line or path.", this);
                    enabled = false;
                    return;
                }
        }

        private void OnEnable()
        {
            if (_tracker == null || _dissection == null) return;
            _tracker.StateChanged += ResetGuidance;
            _dissection.ProgressChanged += OnIncisionProgress;
            _lastProgress = Time.unscaledTime;
            _incisionProgress = _dissection.IsSpawned ? _dissection.IncisionProgress : 0f;
        }

        private void OnDisable()
        {
            if (_tracker != null) _tracker.StateChanged -= ResetGuidance;
            if (_dissection != null) _dissection.ProgressChanged -= OnIncisionProgress;
            if (_cues != null)
                foreach (Cue cue in _cues)
                    if (cue.Line != null) cue.Line.enabled = false;
            if (_arrow != null) _arrow.enabled = false;
        }

        private void ResetGuidance(ChecklistState state) => _lastProgress = Time.unscaledTime;

        private void OnIncisionProgress(float progress)
        {
            if (progress > _incisionProgress) _lastProgress = Time.unscaledTime;
            _incisionProgress = progress;
        }

        private void LateUpdate()
        {
            ChecklistCueStrength next = ChecklistEscalationRule.Evaluate(_tracker.State, Time.unscaledTime - _lastProgress);
            if (next != _strength)
            {
                _strength = next;
                StrengthChanged?.Invoke(next);
            }
            bool strong = _strength == ChecklistCueStrength.Explicit;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (strong ? 3f : 1.7f));
            Color tint = new Color(0.35f, 1f, 0.85f, Visibility * (strong ? 0.65f + 0.3f * pulse : 0.25f + 0.2f * pulse));
            Vector3 destination = default;
            int visible = 0;
            foreach (Cue cue in _cues)
            {
                float opacity = cue.Step == _tracker.State ? Visibility : cue.Step == OutgoingStep ? OutgoingVisibility : 0f;
                bool show = opacity > 0f && cue.Target.gameObject.activeInHierarchy;
                if (cue.Step == ChecklistState.LiftOrgan && _grabs.IsSpawned)
                    show &= cue.OrganIndex < _grabs.HolderCount && _grabs.GetHolder(cue.OrganIndex) == OrganGrabAuthority.NoHolder;
                if (cue.Step == ChecklistState.InspectOrgan || cue.Step == ChecklistState.PutOrganBack)
                    show &= cue.OrganIndex == _tracker.ActiveOrgan;
                cue.Line.enabled = show;
                if (!show) continue;
                float fraction = cue.Step == ChecklistState.InspectOrgan ? _tracker.InspectionFraction : 1f;
                float end = fraction * (cue.Points.Length - 1);
                for (int i = 0; i < cue.Points.Length; i++)
                {
                    float point = Mathf.Min(i, end);
                    int index = Mathf.Min((int)point, cue.Points.Length - 2);
                    Vector3 local = Vector3.Lerp(cue.Points[index], cue.Points[index + 1], point - index);
                    cue.Line.SetPosition(i, cue.Target.TransformPoint(local));
                }
                cue.Line.widthMultiplier = strong ? 0.004f + pulse * 0.002f : 0.0025f;
                tint.a = opacity * (strong ? 0.65f + 0.3f * pulse : 0.25f + 0.2f * pulse);
                _tint.SetColor(BaseColor, tint);
                cue.Line.SetPropertyBlock(_tint);
                if (cue.Step == _tracker.State && visible++ == 0) destination = cue.Target.TransformPoint(cue.Points[0]);
            }
            _arrow.enabled = strong && visible > 0 && Visibility > 0f;
            if (!_arrow.enabled) return;
            Vector3 source = _tracker.State == ChecklistState.TraceIncision ? _scalpelTip.position
                : _tracker.State <= ChecklistState.LiftOmentum ? _forcepsTip.position
                : _tracker.State == ChecklistState.PutOrganBack && _tracker.ActiveOrgan >= 0 && _tracker.ActiveOrgan < _organs.Length
                    ? _organs[_tracker.ActiveOrgan].position : destination + Vector3.up * 0.12f;
            // A short arrowhead at the target, with a thin line from the tool. No moving projectile.
            Vector3 direction = (destination - source).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized * 0.012f;
            if (side.sqrMagnitude < 0.000001f) side = Vector3.right * 0.012f;
            _arrow.SetPosition(0, source);
            _arrow.SetPosition(1, destination);
            _arrow.SetPosition(2, destination - direction * 0.025f + side);
            _arrow.SetPosition(3, destination);
            _arrow.SetPosition(4, destination - direction * 0.025f - side);
            _arrow.SetPropertyBlock(_tint);
        }
    }
}
