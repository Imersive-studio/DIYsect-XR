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
    /// Selecting asks the server for the organ. While granted, the pose is streamed to the server.
    /// Deselecting sends the last pose and gives the organ back. If the server rejects the grab,
    /// the selection is cancelled. Selecting is also blocked up front while another client holds the organ.
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
            if (_authority != null && _authority.IsSpawned && _authority.IsHeldByLocalClient(_organIndex))
            {
                _authority.SendPose(_organIndex, transform.localPosition, transform.localRotation);
                _authority.Release(_organIndex);
            }
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
                _authority.RequestGrab(_organIndex);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            StopStreaming();
            if (!_authority.IsSpawned)
                return;
            // The server ignores both calls unless this client is the holder, so they are safe after a rejection.
            _authority.SendPose(_organIndex, transform.localPosition, transform.localRotation);
            _authority.Release(_organIndex);
        }

        private void OnGrabResolved(int organIndex, bool granted)
        {
            if (organIndex != _organIndex)
                return;
            if (!_interactable.isSelected)
                return;

            if (granted)
                _streaming ??= StartCoroutine(StreamPose());
            else
                _interactable.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_interactable);
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
