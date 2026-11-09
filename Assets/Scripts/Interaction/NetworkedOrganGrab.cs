// Authority: none of its own. It is a client-side bridge; the server (OrganGrabAuthority) decides every grab.
using System;
using System.Collections;
using System.Collections.Generic;
using DIYsect.Networking;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Connects an XRGrabInteractable on an organ to the body's <see cref="OrganGrabAuthority"/>.
    /// The request follows <see cref="GrabRequest"/>: selecting asks the server for the organ; while granted the
    /// pose is streamed; deselecting sends the last pose and gives the organ back; a rejected grab is cancelled;
    /// a yes that arrives after the user let go is given straight back. Selecting is also blocked up front while
    /// another client holds the organ.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class NetworkedOrganGrab : MonoBehaviour, IXRSelectFilter
    {
        private const float PoseEpsilon = 0.0005f;

        [SerializeField] private OrganGrabAuthority _authority;
        [SerializeField] private int _organIndex;
        [SerializeField, Min(0.01f)] private float _sendIntervalSeconds = 0.05f;

        private XRGrabInteractable _interactable;
        private Coroutine _streaming;
        private readonly GrabRequest _request = new GrabRequest();

        /// <summary>This client's grab phase for this organ (the replicated holder list can lag behind it).</summary>
        public GrabPhase Phase => _request.Phase;

        /// <summary>Raised on this client when the holder of this organ changes (new holder id or <see cref="OrganGrabAuthority.NoHolder"/>). Hook for ownership feedback.</summary>
        public event Action<ulong> HolderChanged;

        public bool IsHeldByLocalClient => _authority.IsHeldByLocalClient(_organIndex);

        public bool IsHeldByOtherClient
        {
            get
            {
                ulong holder = _authority.GetHolder(_organIndex);
                return holder != OrganGrabAuthority.NoHolder && !IsHeldByLocalClient;
            }
        }

        bool IXRSelectFilter.canProcess => isActiveAndEnabled;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            if (!Validate())
                enabled = false;
        }

        private void OnValidate()
        {
            Validate();
        }


        private void OnEnable()
        {
            _interactable.selectFilters.Add(this);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            _authority.GrabResolved += OnGrabResolved;
            _authority.HolderChanged += OnHolderChanged;
        }

        private void OnDisable()
        {
            // Do not leave the server holding an organ nobody can move any more.
            GrabRequest.Actions onDisable = _request.Handle(GrabRequest.Event.Disable);
            if ((onDisable & GrabRequest.Actions.SendRelease) == 0 && _authority != null && _authority.IsSpawned && _authority.IsHeldByLocalClient(_organIndex))
                onDisable |= GrabRequest.Actions.SendRelease;
            Apply(onDisable);
            _interactable.selectFilters.Remove(this);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            _authority.GrabResolved -= OnGrabResolved;
            _authority.HolderChanged -= OnHolderChanged;
            StopStreaming();
        }

        bool IXRSelectFilter.Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            return !_authority.IsSpawned || !IsHeldByOtherClient;
        }

        private bool Validate()
        {
            if (_authority == null)
            {
                Debug.LogError($"{name}: NetworkedOrganGrab has no OrganGrabAuthority. Run Tools > DIYsect > Add Body Networking.", this);
                return false;
            }

            bool valid = true;
            IReadOnlyList<NetworkTransform> organs = _authority.GetComponent<SharedBody>().Organs;
            if (organs != null && organs.Count > 0)
            {
                if (_organIndex < 0 || _organIndex >= organs.Count)
                {
                    Debug.LogError($"{name}: organ index {_organIndex} is outside 0..{organs.Count - 1}.", this);
                    valid = false;
                }
                else if (organs[_organIndex] == null || organs[_organIndex].transform != transform)
                {
                    Debug.LogError($"{name}: organ index {_organIndex} is not this organ in SharedBody. Run Tools > DIYsect > Add Body Networking.", this);
                    valid = false;
                }
            }
            return valid;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (_authority.IsSpawned)
                Apply(_request.Handle(GrabRequest.Event.Select));
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            Apply(_request.Handle(GrabRequest.Event.Exit));
        }

        private void OnGrabResolved(int organIndex, bool granted)
        {
            if (organIndex == _organIndex)
                Apply(_request.HandleAnswer(granted));
        }

        // Carries out what the grab state machine asked for, in this bridge's own terms.
        private void Apply(GrabRequest.Actions actions)
        {
            if ((actions & GrabRequest.Actions.SendRequest) != 0)
                _authority.RequestGrab(_organIndex);
            if ((actions & GrabRequest.Actions.StopStreaming) != 0)
                StopStreaming();
            if ((actions & GrabRequest.Actions.SendRelease) != 0)
                SendRelease();
            if ((actions & GrabRequest.Actions.StartStreaming) != 0)
                _streaming ??= StartCoroutine(StreamPose());
            if ((actions & GrabRequest.Actions.CancelSelection) != 0 && _interactable.isSelected)
                _interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_interactable);
        }

        // Sends the last pose and gives the organ back. The server ignores both unless this client is the holder.
        private void SendRelease()
        {
            if (_authority == null || !_authority.IsSpawned)
                return;
            _authority.SendPose(_organIndex, transform.localPosition, transform.localRotation);
            _authority.Release(_organIndex);
        }

        private void OnHolderChanged(int organIndex, ulong holder)
        {
            if (organIndex == _organIndex)
                HolderChanged?.Invoke(holder);
        }

        // Sends on a timer and only when the pose changed, so it needs no Update.
        private IEnumerator StreamPose()
        {
            var wait = new WaitForSeconds(_sendIntervalSeconds);
            Vector3 lastPosition = transform.localPosition;
            Quaternion lastRotation = transform.localRotation;
            while (true)
            {
                yield return wait;
                Vector3 position = transform.localPosition;
                Quaternion rotation = transform.localRotation;
                if (Vector3.Distance(position, lastPosition) < PoseEpsilon && Quaternion.Angle(rotation, lastRotation) < 0.05f)
                    continue;
                _authority.SendPose(_organIndex, position, rotation);
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
