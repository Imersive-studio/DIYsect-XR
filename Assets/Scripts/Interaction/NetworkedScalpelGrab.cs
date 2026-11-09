// Authority: none of its own. It is a client-side bridge; the server (ScalpelSteering) decides who steers the scalpel.
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
    /// Connects the scalpel's XRGrabInteractable to <see cref="ScalpelSteering"/>.
    /// The request follows <see cref="GrabRequest"/>: selecting asks the server to let this client steer; while
    /// granted the pose is streamed; deselecting sends the last pose and puts the scalpel down; a refusal cancels the
    /// selection; a yes that arrives after the user let go is given straight back.
    /// Selecting is also blocked up front while another client steers.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable), typeof(ScalpelSteering))]
    public class NetworkedScalpelGrab : MonoBehaviour, IXRSelectFilter
    {
        private const float PositionEpsilon = 0.0005f;
        private const float AngleEpsilon = 0.05f;

        [SerializeField, Min(0.01f)] private float _sendIntervalSeconds = 0.05f;

        private XRGrabInteractable _interactable;
        private ScalpelSteering _steering;
        private Coroutine _streaming;
        private readonly GrabRequest _request = new GrabRequest();

        /// <summary>This client's steering phase (the replicated steerer can lag behind it).</summary>
        public GrabPhase Phase => _request.Phase;

        bool IXRSelectFilter.canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _steering = GetComponent<ScalpelSteering>();
            if (_interactable == null || _steering == null)
            {
                Debug.LogError($"{name}: NetworkedScalpelGrab needs an XRGrabInteractable and a ScalpelSteering on the same object. Run Tools > DIYsect > Add Placeholder Scalpel.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _interactable.selectFilters.Add(this);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            _steering.SteerResolved += OnSteerResolved;
        }

        private void OnDisable()
        {
            // Do not leave the server thinking somebody steers a scalpel nobody can move any more.
            GrabRequest.Actions onDisable = _request.Handle(GrabRequest.Event.Disable);
            if ((onDisable & GrabRequest.Actions.SendRelease) == 0 && _steering != null && _steering.IsSpawned && _steering.IsSteeredByLocalClient)
                onDisable |= GrabRequest.Actions.SendRelease;
            Apply(onDisable);
            _interactable.selectFilters.Remove(this);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            _steering.SteerResolved -= OnSteerResolved;
            StopStreaming();
        }

        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (!_steering.IsSpawned)
                return true;
            return !_steering.HasSteerer || _steering.IsSteeredByLocalClient;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_steering.IsSpawned)
                Apply(_request.Handle(GrabRequest.Event.Select));
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            Apply(_request.Handle(GrabRequest.Event.Exit));
        }

        private void OnSteerResolved(bool granted)
        {
            Apply(_request.HandleAnswer(granted));
        }

        // Carries out what the grab state machine asked for, in this bridge's own terms.
        private void Apply(GrabRequest.Actions actions)
        {
            if ((actions & GrabRequest.Actions.SendRequest) != 0)
                _steering.RequestSteer();
            if ((actions & GrabRequest.Actions.StopStreaming) != 0)
                StopStreaming();
            if ((actions & GrabRequest.Actions.SendRelease) != 0)
                SendRelease();
            if ((actions & GrabRequest.Actions.StartStreaming) != 0)
                _streaming ??= StartCoroutine(StreamPose());
            if ((actions & GrabRequest.Actions.CancelSelection) != 0 && _interactable.isSelected)
                _interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_interactable);
        }

        // Sends the last pose and puts the scalpel down. The server ignores both unless this client steers.
        private void SendRelease()
        {
            if (_steering == null || !_steering.IsSpawned)
                return;
            _steering.SendPose(transform.localPosition, transform.localRotation);
            _steering.ReleaseSteer();
        }

        // Sends on a timer and only when the pose changed, so it needs no Update.
        private IEnumerator StreamPose()
        {
            var wait = new WaitForSeconds(_sendIntervalSeconds);
            Vector3 lastPosition = transform.localPosition;
            Quaternion lastRotation = transform.localRotation;
            _steering.SendPose(lastPosition, lastRotation);
            while (true)
            {
                yield return wait;
                Vector3 position = transform.localPosition;
                Quaternion rotation = transform.localRotation;
                if (Vector3.Distance(position, lastPosition) < PositionEpsilon && Quaternion.Angle(rotation, lastRotation) < AngleEpsilon)
                    continue;
                _steering.SendPose(position, rotation);
                lastPosition = position;
                lastRotation = rotation;
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
