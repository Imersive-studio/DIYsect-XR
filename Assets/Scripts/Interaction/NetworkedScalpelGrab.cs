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
    /// Selecting asks the server to let this client steer. While granted, the pose is streamed to the server.
    /// Deselecting sends the last pose and puts the scalpel down. If the server says no, the selection is cancelled.
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

        // True from the moment this client asks to steer until it puts the scalpel down. The replicated steerer
        // can lag behind the server's answer, so release decisions cannot rely on it alone.
        private bool _maySteer;

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
            if (_steering != null && _steering.IsSpawned && (_maySteer || _steering.IsSteeredByLocalClient))
            {
                _maySteer = false;
                _steering.SendPose(transform.localPosition, transform.localRotation);
                _steering.ReleaseSteer();
            }
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
            if (!_steering.IsSpawned)
                return;
            _maySteer = true;
            _steering.RequestSteer();
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            StopStreaming();
            _maySteer = false;
            if (!_steering.IsSpawned)
                return;
            // The server ignores both calls unless this client is the steerer, so they are safe after a rejection.
            _steering.SendPose(transform.localPosition, transform.localRotation);
            _steering.ReleaseSteer();
        }

        private void OnSteerResolved(bool granted)
        {
            _maySteer = granted;
            if (!_interactable.isSelected)
                return;

            if (granted)
                _streaming ??= StartCoroutine(StreamPose());
            else
                _interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_interactable);
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
