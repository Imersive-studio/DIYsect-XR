// Authority: the server. It validates every request and is the only writer of the two pointer slots.
// Clients ask by Rpc and every peer receives the slots through NetworkVariables. Late joiners get them on spawn.
using System;
using DIYsect.Networking;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Where the professor and the student are pointing, as an organ index per role (or <see cref="None"/>).
    /// A client asks to point at an organ or to stop. The server rejects an organ index out of range and a client that
    /// asks too often, decides the client's role with <see cref="PointerRoleRule"/> (the scalpel steerer is the Student,
    /// everyone else the Professor), and clears a pointer when its owner stops or disconnects. A role holds one pointer:
    /// when a second client points as the same role, the newer pointer replaces the older one.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class PointerBoard : NetworkBehaviour, IOrganPointer
    {
        public const int None = -1;
        private const ulong NoOwner = ulong.MaxValue;

        [SerializeField, Min(1), Tooltip("Pointer requests one client may send inside the window.")]
        private int _maxRequestsPerWindow = 10;
        [SerializeField, Min(0.1f), Tooltip("Length of the rate limit window in seconds.")]
        private float _windowSeconds = 2f;

        private readonly NetworkVariable<int> _professorOrgan = new NetworkVariable<int>(None);
        private readonly NetworkVariable<int> _studentOrgan = new NetworkVariable<int>(None);
        private SharedBody _body;
        private PinRateLimiter _limiter;
        private ulong _professorOwner = NoOwner;
        private ulong _studentOwner = NoOwner;

        /// <summary>Raised on every peer when either pointer changes, and once on spawn.</summary>
        public event Action PointersChanged;

        /// <summary>Raised on the server when a request is refused (sender id, reason).</summary>
        public event Action<ulong, PointerRejection> RequestRejected;

        public SharedBody Body => _body;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _limiter = new PinRateLimiter(_maxRequestsPerWindow, _windowSeconds);
        }

        public override void OnNetworkSpawn()
        {
            _professorOrgan.OnValueChanged += HandleChanged;
            _studentOrgan.OnValueChanged += HandleChanged;
            if (IsServer)
            {
                _professorOrgan.Value = None;
                _studentOrgan.Value = None;
                _professorOwner = NoOwner;
                _studentOwner = NoOwner;
                NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
            }
            PointersChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _professorOrgan.OnValueChanged -= HandleChanged;
            _studentOrgan.OnValueChanged -= HandleChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>The organ index the role points at, or <see cref="None"/>.</summary>
        public int OrganOf(PointerRole role)
        {
            if (!IsSpawned)
                return None;
            return role == PointerRole.Professor ? _professorOrgan.Value : _studentOrgan.Value;
        }

        public void Point(int organIndex)
        {
            if (IsSpawned)
                PointRpc(organIndex);
        }

        public void StopPointing()
        {
            if (IsSpawned)
                StopRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void PointRpc(int organIndex, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!_limiter.TryAccept(sender, Time.realtimeSinceStartup))
            {
                RequestRejected?.Invoke(sender, PointerRejection.TooFast);
                return;
            }
            if (organIndex < 0 || organIndex >= _body.Organs.Count)
            {
                RequestRejected?.Invoke(sender, PointerRejection.BadOrgan);
                return;
            }
            PointerRole role = PointerRoleRule.For(sender, FindSteerer());
            // A client holds one pointer: if its role changed since the last request, the old slot is freed.
            PointerRole other = role == PointerRole.Professor ? PointerRole.Student : PointerRole.Professor;
            if (OwnerOf(other) == sender)
                Clear(other);
            SetOwner(role, sender);
            SlotOf(role).Value = organIndex;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StopRpc(RpcParams rpcParams = default)
        {
            ClearOwnedBy(rpcParams.Receive.SenderClientId);
        }

        // The scalpel is its own NetworkObject. This runs only on the server and only after a request that passed the rate limit.
        private ulong FindSteerer()
        {
            foreach (NetworkObject spawned in NetworkManager.SpawnManager.SpawnedObjectsList)
                if (spawned.TryGetComponent(out ScalpelSteering steering) && steering.HasSteerer)
                    return steering.Steerer;
            return ScalpelSteering.NoSteerer;
        }

        private void HandleDisconnect(ulong clientId)
        {
            _limiter.Forget(clientId);
            ClearOwnedBy(clientId);
        }

        private void ClearOwnedBy(ulong clientId)
        {
            if (_professorOwner == clientId)
                Clear(PointerRole.Professor);
            if (_studentOwner == clientId)
                Clear(PointerRole.Student);
        }

        private void Clear(PointerRole role)
        {
            SetOwner(role, NoOwner);
            SlotOf(role).Value = None;
        }

        private NetworkVariable<int> SlotOf(PointerRole role) => role == PointerRole.Professor ? _professorOrgan : _studentOrgan;

        private ulong OwnerOf(PointerRole role) => role == PointerRole.Professor ? _professorOwner : _studentOwner;

        private void SetOwner(PointerRole role, ulong owner)
        {
            if (role == PointerRole.Professor)
                _professorOwner = owner;
            else
                _studentOwner = owner;
        }

        private void HandleChanged(int previous, int current)
        {
            PointersChanged?.Invoke();
        }
    }
}
