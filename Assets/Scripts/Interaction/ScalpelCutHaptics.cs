// Authority: none. Local presentation only. Only the device whose controller holds the scalpel feels the ticks.
using DIYsect.Dissection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>
    /// A light tick on the holding hand while the cut advances. It sits on the scalpel next to <see cref="ScalpelTool"/>
    /// and follows the same incision zone: while the blade is in the zone it listens to the target's
    /// <see cref="DissectionProgress.ProgressChanged"/> and pulses when the progress goes up, at most once per interval
    /// (<see cref="CutHapticLimiter"/>). A copy of the scalpel that nobody holds on this device has no hand, so it stays silent.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ScalpelCutHaptics : MonoBehaviour
    {
        [SerializeField] private HapticPulse _tick = new HapticPulse(0.15f, 0.03f);
        [SerializeField, Min(0f)] private float _minIntervalSeconds = 0.1f;

        private XRGrabInteractable _interactable;
        private CutHapticLimiter _limiter;
        private DissectionProgress _target;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _limiter = new CutHapticLimiter(_minIntervalSeconds);
        }

        private void OnDisable()
        {
            Detach();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IncisionTrigger zone) || !(zone.Target is DissectionProgress progress) || progress == _target)
                return;
            Detach();
            _target = progress;
            _limiter.Begin(progress.IncisionProgress);
            _target.ProgressChanged += OnProgressChanged;
        }

        private void OnTriggerExit(Collider other)
        {
            if (_target != null && other.TryGetComponent(out IncisionTrigger zone) && zone.Target == (ICuttable)_target)
                Detach();
        }

        private void OnProgressChanged(float progress)
        {
            if (_limiter.ShouldPulse(progress, Time.unscaledTime))
                InteractorHaptics.Send(_interactable.firstInteractorSelecting, _tick);
        }

        private void Detach()
        {
            if (_target != null)
                _target.ProgressChanged -= OnProgressChanged;
            _target = null;
        }
    }
}
