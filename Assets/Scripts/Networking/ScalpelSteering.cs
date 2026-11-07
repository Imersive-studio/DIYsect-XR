// Authority: the server. It decides which client steers the scalpel and is the only one that moves it on the network.
// Clients ask by Rpc; the server answers. The steerer streams its pose and the server applies it, so the
// server-authoritative HoldableNetworkTransform carries it to everyone else.
using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Says who steers the shared scalpel (the student) and moves it for everyone.
    /// Steering means moving the scalpel with your hand: position and rotation. One client at a time.
    /// The pose travels in the local space of the scalpel's parent (the body), because each client places
    /// the body differently in its own room. The steerer moves the scalpel locally with XR Interaction Toolkit,
    /// so the incoming network state is paused on that client while it steers.
    /// </summary>
    [RequireComponent(typeof(HoldableNetworkTransform))]
    public class ScalpelSteering : NetworkBehaviour
    {
        /// <summary>Steerer value while nobody holds the scalpel.</summary>
        public const ulong NoSteerer = ulong.MaxValue;

        [SerializeField, Min(0.1f), Tooltip("Largest distance in metres the steerer may put the scalpel from its parent (the body).")]
        private float _maxPoseRadius = 3f;

        private readonly NetworkVariable<ulong> _steerer = new NetworkVariable<ulong>(NoSteerer);
        private HoldableNetworkTransform _networkTransform;
        private bool _warnedInvalidPose;

        /// <summary>Raised on every peer when the steerer changes (new steerer id or <see cref="NoSteerer"/>). Also raised once on spawn with the current value.</summary>
        public event Action<ulong> SteererChanged;

        /// <summary>Raised on the requesting client when the server answers a steer request.</summary>
        public event Action<bool> SteerResolved;

        public ulong Steerer => IsSpawned ? _steerer.Value : NoSteerer;
        public bool HasSteerer => Steerer != NoSteerer;
        public bool IsSteeredByLocalClient => IsSpawned && NetworkManager != null && _steerer.Value == NetworkManager.LocalClientId;

        private void Awake()
        {
            _networkTransform = GetComponent<HoldableNetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
            _steerer.OnValueChanged += HandleSteererChanged;
            if (IsServer)
            {
                _steerer.Value = NoSteerer;
                _warnedInvalidPose = false;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            }
            ApplyPause(_steerer.Value);
            SteererChanged?.Invoke(_steerer.Value);
        }

        public override void OnNetworkDespawn()
        {
            _steerer.OnValueChanged -= HandleSteererChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        /// <summary>Asks the server to let this client steer. The answer arrives through <see cref="SteerResolved"/>.</summary>
        public void RequestSteer()
        {
            // Pause at once: the steerer-to-be starts moving the scalpel before the server answers.
            if (!IsServer)
                _networkTransform.Pause();
            RequestSteerRpc();
        }

        /// <summary>Puts the scalpel down. The server ignores this unless the caller is the steerer.</summary>
        public void ReleaseSteer()
        {
            ReleaseSteerRpc();
        }

        /// <summary>Sends the steerer's pose in the parent's local space. The server ignores it unless the caller is the steerer.</summary>
        public void SendPose(Vector3 localPosition, Quaternion localRotation)
        {
            MoveRpc(localPosition, localRotation);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestSteerRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool granted = _steerer.Value == NoSteerer || _steerer.Value == sender;
            if (granted)
                _steerer.Value = sender;
            SteerResultRpc(granted, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SteerResultRpc(bool granted, RpcParams rpcParams = default)
        {
            if (!granted)
                ApplyPause(_steerer.Value);
            SteerResolved?.Invoke(granted);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseSteerRpc(RpcParams rpcParams = default)
        {
            if (_steerer.Value == rpcParams.Receive.SenderClientId)
                _steerer.Value = NoSteerer;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void MoveRpc(Vector3 localPosition, Quaternion localRotation, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (_steerer.Value != sender)
                return;
            if (!IsAcceptablePose(localPosition, localRotation))
            {
                if (!_warnedInvalidPose)
                    Debug.LogWarning($"{name}: ignored an invalid scalpel pose from client {sender}. Further invalid poses are ignored silently.", this);
                _warnedInvalidPose = true;
                return;
            }
            transform.SetLocalPositionAndRotation(localPosition, localRotation.normalized);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_steerer.Value == clientId)
                _steerer.Value = NoSteerer;
        }

        private void HandleSteererChanged(ulong previous, ulong current)
        {
            ApplyPause(current);
            SteererChanged?.Invoke(current);
        }

        // The server is the authority of the transform and must keep sending it, so only a client pauses.
        private void ApplyPause(ulong steerer)
        {
            if (IsServer || NetworkManager == null)
                return;
            if (steerer == NetworkManager.LocalClientId)
                _networkTransform.Pause();
            else
                _networkTransform.Resume();
        }

        private bool IsAcceptablePose(Vector3 localPosition, Quaternion localRotation)
        {
            if (!float.IsFinite(localPosition.x) || !float.IsFinite(localPosition.y) || !float.IsFinite(localPosition.z))
                return false;
            if (!float.IsFinite(localRotation.x) || !float.IsFinite(localRotation.y) || !float.IsFinite(localRotation.z) || !float.IsFinite(localRotation.w))
                return false;
            float squaredLength = localRotation.x * localRotation.x + localRotation.y * localRotation.y + localRotation.z * localRotation.z + localRotation.w * localRotation.w;
            if (squaredLength < 1e-6f)
                return false;
            return localPosition.magnitude <= _maxPoseRadius;
        }
    }
}
