// Authority: none. Local presentation only; it vibrates controllers on this device and sends nothing over the network.
using DIYsect.Interaction;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Controller feedback for the help request. The student feels a firm tick when their request opens, a soft one when the
    /// professor takes over and a short one when it ends, so they know the press worked. Everyone else with a headset
    /// (the professor) feels a double pulse when a request arrives. A phone has no controller, so nothing happens there.
    /// Event driven.
    /// </summary>
    [RequireComponent(typeof(AssistBoard))]
    public class AssistHaptics : MonoBehaviour
    {
        [SerializeField] private AssistBoard _board;
        [SerializeField] private HapticPulse _requested = new HapticPulse(0.6f, 0.12f);
        [SerializeField] private HapticPulse _helping = new HapticPulse(0.35f, 0.08f);
        [SerializeField] private HapticPulse _ended = new HapticPulse(0.2f, 0.05f);
        [SerializeField] private HapticPulse _incoming = new HapticPulse(0.8f, 0.2f);
        [SerializeField, Min(0.05f), Tooltip("Pause between the two pulses the professor feels, in seconds.")]
        private float _doubleGapSeconds = 0.25f;

        private AssistPhase _last;
        private bool _seeded;
        private bool _lastMine;

        private void Awake()
        {
            if (_board == null)
                _board = GetComponent<AssistBoard>();
            if (_board == null)
            {
                Debug.LogError($"{name}: AssistHaptics needs an AssistBoard on the same object. Run Tools > DIYsect > Add Help Request.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _board.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            _board.PhaseChanged -= OnPhaseChanged;
            StopAllCoroutines();
        }

        private void OnPhaseChanged(AssistPhase phase)
        {
            AssistPhase previous = _last;
            bool wasMine = _lastMine;
            _last = phase;
            _lastMine = _board.IsLocalRequest;
            // The first call only reports the state a joiner arrives to; nothing happened, so nothing is felt.
            if (!_seeded)
            {
                _seeded = true;
                return;
            }
            if (phase == previous)
                return;
            // Once a request ends it no longer names its owner, so the previous event says whether it was ours.
            if (_lastMine || wasMine)
            {
                Pulse(phase == AssistPhase.Requested ? _requested : phase == AssistPhase.Helping ? _helping : _ended);
                return;
            }
            if (phase == AssistPhase.Requested)
                StartCoroutine(DoublePulse());
        }

        private System.Collections.IEnumerator DoublePulse()
        {
            Pulse(_incoming);
            yield return new WaitForSeconds(_doubleGapSeconds);
            Pulse(_incoming);
        }

        private static void Pulse(HapticPulse pulse)
        {
            foreach (XRBaseInputInteractor interactor in FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None))
                InteractorHaptics.Send(interactor, pulse);
        }
    }
}
