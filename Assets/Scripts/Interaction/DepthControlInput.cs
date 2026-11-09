// Authority: none of its own. It is a client-side bridge; the server (GuidingHand) decides who controls the depth.
using System.Collections;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The professor's input for the cut depth: a small handle the professor grabs. Grabbing it asks the server for
    /// control of the depth. While granted, lowering the handle makes the cut deeper and raising it makes it shallower
    /// (<see cref="TravelMeters"/> of travel covers the full depth range). Letting go gives the depth back to the
    /// student and the handle returns to its resting place. If the server says no, or the student puts the scalpel
    /// down (the server revokes control), the grab is cancelled. The request follows <see cref="GrabRequest"/>.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class DepthControlInput : MonoBehaviour, IXRSelectFilter
    {
        [SerializeField] private GuidingHand _hand;
        [SerializeField, Min(0.05f)] private float _travelMeters = 0.2f;
        [SerializeField, Min(0.01f)] private float _sendIntervalSeconds = 0.05f;

        private XRGrabInteractable _interactable;
        private Coroutine _streaming;
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private readonly GrabRequest _request = new GrabRequest();

        public float TravelMeters => _travelMeters;

        /// <summary>This client's phase in controlling the depth.</summary>
        public GrabPhase Phase => _request.Phase;

        bool IXRSelectFilter.canProcess => isActiveAndEnabled;

        /// <summary>
        /// Maps the handle's height to a depth. Lowering the handle by the whole travel adds 1 to the depth it had
        /// when grabbed, so the depth never jumps at the moment of the grab. The result is clamped to 0..1.
        /// </summary>
        public static float DepthFromHandle(float depthAtGrab, float heightAtGrab, float height, float travelMeters)
        {
            return Mathf.Clamp01(depthAtGrab + (heightAtGrab - height) / travelMeters);
        }

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            if (_hand == null)
            {
                Debug.LogError($"{name}: DepthControlInput has no GuidingHand. Run Tools > DIYsect > Add Placeholder Scalpel.", this);
                enabled = false;
                return;
            }
            _restPosition = transform.position;
            _restRotation = transform.rotation;
        }

        private void OnEnable()
        {
            _interactable.selectFilters.Add(this);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            _hand.ControlResolved += OnControlResolved;
            _hand.ControlRevoked += OnControlRevoked;
        }

        private void OnDisable()
        {
            GrabRequest.Actions onDisable = _request.Handle(GrabRequest.Event.Disable);
            if ((onDisable & GrabRequest.Actions.SendRelease) == 0 && _hand != null && _hand.IsSpawned && _hand.IsControlledByLocalClient)
                onDisable |= GrabRequest.Actions.SendRelease;
            Apply(onDisable);
            _interactable.selectFilters.Remove(this);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            _hand.ControlResolved -= OnControlResolved;
            _hand.ControlRevoked -= OnControlRevoked;
            StopStreaming();
        }

        // Block the grab when it cannot succeed: nobody holds the scalpel, or someone else already guides. Without this
        // a hand that keeps squeezing would grab, be refused and cancelled, and grab again every frame.
        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return !_hand.IsSpawned || _hand.CanRequestControl;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_hand.IsSpawned)
                Apply(_request.Handle(GrabRequest.Event.Select));
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            Apply(_request.Handle(GrabRequest.Event.Exit));
            transform.SetPositionAndRotation(_restPosition, _restRotation);
        }

        private void OnControlResolved(bool granted)
        {
            Apply(_request.HandleAnswer(granted));
        }

        // The student put the scalpel down, so the server ended our control: let go of the handle.
        private void OnControlRevoked()
        {
            Apply(_request.Handle(GrabRequest.Event.Revoked));
        }

        // Carries out what the grab state machine asked for, in this bridge's own terms.
        private void Apply(GrabRequest.Actions actions)
        {
            if ((actions & GrabRequest.Actions.SendRequest) != 0)
                _hand.RequestControl();
            if ((actions & GrabRequest.Actions.StopStreaming) != 0)
                StopStreaming();
            if ((actions & GrabRequest.Actions.SendRelease) != 0 && _hand != null && _hand.IsSpawned)
                _hand.ReleaseControl();
            if ((actions & GrabRequest.Actions.StartStreaming) != 0)
                _streaming ??= StartCoroutine(StreamDepth());
            if ((actions & GrabRequest.Actions.CancelSelection) != 0 && _interactable.isSelected)
                _interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_interactable);
        }

        // Sends on a timer and only when the depth changed, so it needs no Update.
        private IEnumerator StreamDepth()
        {
            var wait = new WaitForSeconds(_sendIntervalSeconds);
            float depthAtGrab = _hand.Depth;
            float heightAtGrab = transform.position.y;
            float last = float.NaN;
            while (true)
            {
                float depth = DepthFromHandle(depthAtGrab, heightAtGrab, transform.position.y, _travelMeters);
                if (!Mathf.Approximately(depth, last))
                {
                    _hand.SendDepth(depth);
                    last = depth;
                }
                yield return wait;
            }
        }

        private void StopStreaming()
        {
            if (_streaming == null)
                return;
            StopCoroutine(_streaming);
            _streaming = null;
        }
    }
}
