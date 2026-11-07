// Authority: the server. It decides who holds each organ and is the only one that moves organs.
// Clients ask by Rpc; the server answers. The holder streams the target pose, the server applies it.
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Grab registry for the shared body. The organs are not separate NetworkObjects, so NGO ownership
    /// cannot be per organ. Instead this keeps one holder client id per organ (or <see cref="NoHolder"/>).
    /// A grab is granted only if the organ is free. Only the holder may move or release it.
    /// When a client disconnects, the server frees everything that client held.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class OrganGrabAuthority : NetworkBehaviour
    {
        /// <summary>Holder value of an organ nobody has grabbed.</summary>
        public const ulong NoHolder = ulong.MaxValue;

        private SharedBody _body;
        [SerializeField, Min(0.1f), Tooltip("Largest distance in metres a holder may put an organ from the body root.")]
        private float _maxPoseRadius = 2f;

        private NetworkList<ulong> _holders;
        private readonly HashSet<ulong> _warnedSenders = new HashSet<ulong>();
        private HoldableNetworkTransform[] _organTransforms;

        /// <summary>Raised on every peer when an organ index changes holder (organ index, new holder id or <see cref="NoHolder"/>).</summary>
        public event Action<int, ulong> HolderChanged;

        /// <summary>Raised on the requesting client when the server answers a grab request (organ index, granted).</summary>
        public event Action<int, bool> GrabResolved;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _holders = new NetworkList<ulong>();

            _organTransforms = new HoldableNetworkTransform[_body.Organs.Count];
            for (int i = 0; i < _organTransforms.Length; i++)
            {
                _organTransforms[i] = _body.Organs[i] as HoldableNetworkTransform;
                if (_organTransforms[i] == null)
                    Debug.LogError($"{name}: organ {i} needs a HoldableNetworkTransform. Run Tools > DIYsect > Add Body Networking.", this);
            }
        }

        public override void OnNetworkSpawn()
        {
            _holders.OnListChanged += HandleListChanged;
            if (!IsServer)
                return;

            // Clear first so a despawn followed by a respawn does not duplicate entries.
            _holders.Clear();
            _warnedSenders.Clear();
            for (int i = 0; i < _body.Organs.Count; i++)
                _holders.Add(NoHolder);
            NetworkManager.OnClientDisconnectCallback += ReleaseAllHeldBy;
        }

        public override void OnNetworkDespawn()
        {
            _holders.OnListChanged -= HandleListChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= ReleaseAllHeldBy;
        }

        /// <summary>Number of organs in the registry. Only meaningful once spawned.</summary>
        public int HolderCount => _holders.Count;

        /// <summary>Who holds the organ now, or <see cref="NoHolder"/>.</summary>
        public ulong GetHolder(int organIndex)
        {
            return IsSpawned && IsValidIndex(organIndex) ? _holders[organIndex] : NoHolder;
        }

        public bool IsHeldByLocalClient(int organIndex)
        {
            return IsSpawned && NetworkManager != null && GetHolder(organIndex) == NetworkManager.LocalClientId;
        }

        /// <summary>Asks the server for the organ. The answer arrives through <see cref="GrabResolved"/>.</summary>
        public void RequestGrab(int organIndex)
        {
            // Pause at once: the holder-to-be starts moving the organ before the server answers.
            SetPaused(organIndex, true);
            RequestGrabRpc(organIndex);
        }

        /// <summary>Gives the organ back. The server ignores this unless the caller is the holder.</summary>
        public void Release(int organIndex)
        {
            ReleaseRpc(organIndex);
        }

        /// <summary>Sends the holder's target pose (local space). The server ignores it unless the caller is the holder.</summary>
        public void SendPose(int organIndex, Vector3 localPosition, Quaternion localRotation)
        {
            MoveOrganRpc(organIndex, localPosition, localRotation);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestGrabRpc(int organIndex, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool granted = IsValidIndex(organIndex) && (_holders[organIndex] == NoHolder || _holders[organIndex] == sender);
            if (granted)
                _holders[organIndex] = sender;
            GrabResultRpc(organIndex, granted, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void GrabResultRpc(int organIndex, bool granted, RpcParams rpcParams = default)
        {
            if (!granted)
                SetPaused(organIndex, false);
            GrabResolved?.Invoke(organIndex, granted);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseRpc(int organIndex, RpcParams rpcParams = default)
        {
            if (IsHolder(organIndex, rpcParams.Receive.SenderClientId))
                _holders[organIndex] = NoHolder;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void MoveOrganRpc(int organIndex, Vector3 localPosition, Quaternion localRotation, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!IsHolder(organIndex, sender))
                return;

            Transform organ = _body.Organs[organIndex].transform;
            if (!IsAcceptablePose(organ, localPosition, localRotation))
            {
                if (_warnedSenders.Add(sender))
                    Debug.LogWarning($"{name}: ignored an invalid pose for organ {organIndex} from client {sender}. Further invalid poses from this client are ignored silently.", this);
                return;
            }
            organ.SetLocalPositionAndRotation(localPosition, localRotation.normalized);
        }

        private void ReleaseAllHeldBy(ulong clientId)
        {
            for (int i = 0; i < _holders.Count; i++)
            {
                if (_holders[i] == clientId)
                    _holders[i] = NoHolder;
            }
        }

        private void SetPaused(int organIndex, bool paused)
        {
            if (organIndex < 0 || organIndex >= _organTransforms.Length || _organTransforms[organIndex] == null)
                return;
            if (paused)
                _organTransforms[organIndex].Pause();
            else
                _organTransforms[organIndex].Resume();
        }

        // Rejects NaN/Infinity, a zero rotation, and positions too far from the body root.
        private bool IsAcceptablePose(Transform organ, Vector3 localPosition, Quaternion localRotation)
        {
            if (!IsFinite(localPosition) || !IsFinite(localRotation))
                return false;
            if (localRotation.x * localRotation.x + localRotation.y * localRotation.y + localRotation.z * localRotation.z + localRotation.w * localRotation.w < 1e-6f)
                return false;
            Vector3 world = organ.parent != null ? organ.parent.TransformPoint(localPosition) : localPosition;
            return Vector3.Distance(world, transform.position) <= _maxPoseRadius;
        }

        private static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        private static bool IsFinite(Quaternion q)
        {
            return float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w);
        }

        private bool IsHolder(int organIndex, ulong clientId)
        {
            return IsValidIndex(organIndex) && _holders[organIndex] == clientId;
        }

        private bool IsValidIndex(int organIndex)
        {
            return organIndex >= 0 && organIndex < _holders.Count;
        }

        private void HandleListChanged(NetworkListEvent<ulong> change)
        {
            if (change.Type != NetworkListEvent<ulong>.EventType.Value)
                return;
            // The local holder moves the organ itself, so stop applying server state to it until it is released.
            SetPaused(change.Index, change.Value == NetworkManager.LocalClientId);
            HolderChanged?.Invoke(change.Index, change.Value);
        }
    }
}
