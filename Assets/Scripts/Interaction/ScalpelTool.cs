// Authority: none of its own. Every client has a copy of the scalpel, but only the steerer's copy reports contacts.
// The server (DissectionProgress) decides what the contacts do.
using System.Collections;
using DIYsect.Dissection;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>
    /// A minimal scalpel. While the blade tip is inside an <see cref="IncisionTrigger"/> it reports the tip
    /// position to the target on a timer, and only if the tip moved since the last report. When the incision
    /// this scalpel is still cutting becomes complete, it asks for the skin to be opened. The server still checks
    /// every sample and the request. When the scalpel is shared (it has a <see cref="ScalpelSteering"/> that is
    /// spawned), only the client that steers it reports; the others see the same tip but stay quiet. The reported
    /// tip is the effective tip: if a guide controls the depth, the tip transform already carries that depth.
    /// The scalpel needs a Rigidbody and a trigger collider on the tip so Unity
    /// raises the trigger events.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class ScalpelTool : MonoBehaviour, ITool
    {
        [SerializeField] private string _toolName = "Scalpel";
        [SerializeField] private Transform _tip;
        [SerializeField, Min(0.01f)] private float _sendIntervalSeconds = 0.05f;
        [SerializeField, Min(0.0005f)] private float _minMoveMeters = 0.003f;

        private ScalpelSteering _steering;
        private ICuttable _target;
        private Coroutine _sampling;
        private Vector3 _lastSent;
        private bool _hasSent;

        public string ToolName => _toolName;
        public Transform Tip => _tip;

        private void Awake()
        {
            _steering = GetComponent<ScalpelSteering>();
            Validate();
        }

        private void OnDisable()
        {
            StopSampling();
            Detach();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IncisionTrigger zone) || !IsTipCollider(other))
                return;
            Attach(zone.Target);
            _sampling ??= StartCoroutine(Sample());
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.TryGetComponent(out IncisionTrigger zone) || zone.Target != _target)
                return;
            StopSampling();
            // Leaving the zone ends this cut: a later completion by someone else must not make this scalpel open the skin.
            Detach();
        }

        // The tip's own trigger is the only one that cuts, not the handle or the blade body.
        private bool IsTipCollider(Collider other)
        {
            return _tip != null && Vector3.Distance(_tip.position, other.ClosestPoint(_tip.position)) < 0.005f;
        }

        private void Attach(ICuttable target)
        {
            if (_target == target)
                return;
            Detach();
            _target = target;
            _target.StageChanged += HandleStageChanged;
        }

        private void Detach()
        {
            if (_target != null)
                _target.StageChanged -= HandleStageChanged;
            _target = null;
            _hasSent = false;
        }

        private void HandleStageChanged(DissectionStage stage, bool initialState)
        {
            // Only a scalpel still cutting (tip in the zone, samples sent) asks to open it. Anyone else may still call RequestOpen.
            if (!initialState && stage == DissectionStage.Incised && _hasSent)
                _target.RequestOpen();
        }

        private IEnumerator Sample()
        {
            var wait = new WaitForSeconds(_sendIntervalSeconds);
            while (true)
            {
                Vector3 tip = _tip.position;
                if (MayReport && (!_hasSent || Vector3.Distance(tip, _lastSent) >= _minMoveMeters))
                {
                    _target.SubmitContact(tip);
                    _lastSent = tip;
                    _hasSent = true;
                }
                yield return wait;
            }
        }

        // A scalpel that is not networked (or not spawned yet) reports on its own; a shared one only for its steerer.
        private bool MayReport => _steering == null || !_steering.IsSpawned || _steering.IsSteeredByLocalClient;

        private void StopSampling()
        {
            if (_sampling == null)
                return;
            StopCoroutine(_sampling);
            _sampling = null;
        }

        private void Validate()
        {
            if (_tip == null)
                Debug.LogError($"{name}: ScalpelTool has no blade tip transform.", this);
        }
    }
}
