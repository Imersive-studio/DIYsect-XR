// Authority: none. This only reads state the server already shares (the dissection stage, the organ holders
// and the snap verdicts), so every device works out the same ticks and nothing new goes over the network.
using System;
using System.Collections;
using DIYsect.Dissection;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// Connects the real events to <see cref="ChecklistProgress"/>:
    /// the dissection stage ticks the layer steps (and tells a late joiner where the class is), an organ changing
    /// holder ticks "lift out an organ" and undoes it if it is let go early, holding it for a few seconds ticks
    /// "inspect", and the server accepting its snap into its own socket ticks "put back".
    /// The hold timer is a local clock started from the holder change that every device sees, and it only fires an
    /// event; the checklist state itself lives in <see cref="ChecklistProgress"/>.
    /// Panels and lines listen to <see cref="StateChanged"/>; they never keep their own copy.
    /// </summary>
    public class ChecklistTracker : MonoBehaviour
    {
        [SerializeField] private DissectionProgress _dissection;
        [SerializeField] private OrganGrabAuthority _grabs;
        [SerializeField] private OrganSocketKeeper _sockets;
        [SerializeField, Min(0.5f), Tooltip("How long in seconds an organ must stay in hand to count as inspected.")]
        private float _inspectSeconds = 3f;

        private readonly ChecklistProgress _progress = new ChecklistProgress();
        private Coroutine _inspectTimer;

        /// <summary>Raised with the new state each time the checklist changes.</summary>
        public event Action<ChecklistState> StateChanged;

        /// <summary>The step the student has to do next.</summary>
        public ChecklistState State => _progress.State;

        private void Awake()
        {
            _progress.Changed += HandleChanged;
            if (_dissection == null || _grabs == null || _sockets == null)
            {
                Debug.LogError($"{name}: ChecklistTracker needs the body's DissectionProgress, OrganGrabAuthority and OrganSocketKeeper. Run Tools > DIYsect > Add Dissection Checklist.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _dissection.StageChanged += HandleStage;
            _grabs.HolderChanged += HandleHolder;
            _sockets.SnapResolved += HandleSnap;
            // The body may have spawned before this component was enabled; then no stage event is coming, so read it.
            if (_dissection.IsSpawned)
                HandleStage(_dissection.Stage, true);
        }

        private void OnDisable()
        {
            if (_dissection == null || _grabs == null || _sockets == null)
                return;
            _dissection.StageChanged -= HandleStage;
            _grabs.HolderChanged -= HandleHolder;
            _sockets.SnapResolved -= HandleSnap;
            StopInspectTimer();
        }

        private void HandleStage(DissectionStage stage, bool initial)
        {
            _progress.ApplyStage(stage);
            // A joining device gets no holder events for what is already in someone's hand, so read the holders once.
            if (initial)
                ReadHolders();
        }

        private void ReadHolders()
        {
            if (!_grabs.IsSpawned)
                return;
            for (int i = 0; i < _grabs.HolderCount; i++)
                if (_grabs.GetHolder(i) != OrganGrabAuthority.NoHolder)
                    _progress.OnHolderChanged(i, true);
        }

        private void HandleHolder(int organ, ulong holder)
        {
            _progress.OnHolderChanged(organ, holder != OrganGrabAuthority.NoHolder);
        }

        private void HandleSnap(int organ, SnapOutcome outcome)
        {
            _progress.OnSnapResolved(organ, outcome == SnapOutcome.Accepted);
        }

        // The timer runs exactly while the state is InspectOrgan, so it needs no flag of its own.
        private void HandleChanged(ChecklistState state)
        {
            StopInspectTimer();
            if (state == ChecklistState.InspectOrgan)
                _inspectTimer = StartCoroutine(InspectAfterDelay());
            StateChanged?.Invoke(state);
        }

        private IEnumerator InspectAfterDelay()
        {
            yield return new WaitForSeconds(_inspectSeconds);
            _inspectTimer = null;
            _progress.OnHeldLongEnough(_progress.ActiveOrgan);
        }

        private void StopInspectTimer()
        {
            if (_inspectTimer == null)
                return;
            StopCoroutine(_inspectTimer);
            _inspectTimer = null;
        }
    }
}
