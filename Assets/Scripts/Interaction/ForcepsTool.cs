// Authority: none of its own. Each client's forceps only asks; the server (DissectionProgress) decides whether a step happens.
using DIYsect.Dissection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Minimal forceps. When the tip enters a <see cref="LayerTouchTrigger"/> (the omentum, or the rectus) it asks that
    /// layer's target to do the step the trigger names, once per entry. The server accepts it only in the right
    /// stage; the answer comes back through <see cref="StepResolved"/> so a refusal can be shown. The forceps knows
    /// nothing about which layer comes first: that order lives in <see cref="LayerOrderRule"/>.
    /// The forceps needs a Rigidbody and a trigger collider on the tip so Unity raises the trigger events.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class ForcepsTool : MonoBehaviour, ITool
    {
        [SerializeField] private string _toolName = "Forceps";
        [SerializeField] private Transform _tip;

        private IDissectionSteps _target;
        private LayerTouchTrigger _touching;

        public string ToolName => _toolName;
        public Transform Tip => _tip;

        /// <summary>Raised when the server answers a request this forceps made.</summary>
        public event System.Action<DissectionAction, StepResult> StepResolved;

        private void Awake()
        {
            if (_tip == null)
            {
                Debug.LogError($"{name}: ForcepsTool has no tip transform.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            _touching = null;
            Unsubscribe();
        }

        private void OnTriggerEnter(Collider other)
        {
            // A trigger that disabled itself (no target, bad collider) is ignored.
            if (!other.TryGetComponent(out LayerTouchTrigger layer) || !layer.enabled || layer.Target == null || !IsTipCollider(other))
                return;
            // The handle and the tip both raise an enter for the same layer; ask once until the forceps leaves it.
            if (_touching == layer)
                return;
            _touching = layer;
            Subscribe(layer.Target);
            layer.Target.RequestStep(layer.Action);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_touching != null && other.gameObject == _touching.gameObject)
                _touching = null;
        }

        // The tip's own trigger is the only one that touches, not the handle or the arms.
        private bool IsTipCollider(Collider other)
        {
            return Vector3.Distance(_tip.position, other.ClosestPoint(_tip.position)) < 0.005f;
        }

        private void Subscribe(IDissectionSteps target)
        {
            if (_target == target)
                return;
            Unsubscribe();
            _target = target;
            _target.StepResolved += HandleStepResolved;
        }

        private void Unsubscribe()
        {
            if (_target != null)
                _target.StepResolved -= HandleStepResolved;
            _target = null;
        }

        private void HandleStepResolved(DissectionAction action, StepResult result)
        {
            StepResolved?.Invoke(action, result);
        }
    }
}
