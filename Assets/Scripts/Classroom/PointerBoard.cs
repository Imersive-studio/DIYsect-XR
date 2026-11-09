// Authority: the server. It validates every request and is the only writer of the three pointer slots.
// Clients ask by Rpc and every peer receives the slots through NetworkVariables. Late joiners get them on spawn.
using System;
using DIYsect.Networking;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Where the professor, the student and the classmates are pointing, as an organ index per role (or <see cref="None"/>).
    /// A client asks to point at an organ or to stop. The server rejects an organ index out of range and a client that
    /// asks too often, decides the client's role with <see cref="PointerRoleRule"/> (the scalpel steerer is the Student,
    /// everyone else the Professor), and clears a pointer when its owner stops or disconnects. A role holds one pointer:
    /// when a second client points as the same role, the newer pointer replaces the older one.
    /// Classmates on phones use their own Rpc (<see cref="PointAsClassmate"/>) because a phone and a headset cannot be told apart
    /// by the server; they share one classmate slot and the scalpel steerer may not use it.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class PointerBoard : NetworkBehaviour, IOrganPointer
    {
        public const int None = -1;
        private const ulong NoOwner = PointerSlotRule.NoOwner;

        [SerializeField, Min(1), Tooltip("Pointer requests one client may send inside the window.")]
        private int _maxRequestsPerWindow = 10;
        [SerializeField, Min(0.1f), Tooltip("Length of the rate limit window in seconds.")]
        private float _windowSeconds = 2f;

        private readonly NetworkVariable<int> _professorOrgan = new NetworkVariable<int>(None);
        private readonly NetworkVariable<int> _studentOrgan = new NetworkVariable<int>(None);
        private readonly NetworkVariable<int> _classmateOrgan = new NetworkVariable<int>(None);
        private SharedBody _body;
        private PinRateLimiter _limiter;
        private ulong _professorOwner = NoOwner;
        private ulong _studentOwner = NoOwner;
        private ulong _classmateOwner = NoOwner;

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
            _classmateOrgan.OnValueChanged += HandleChanged;
            if (IsServer)
            {
                _professorOrgan.Value = None;
                _studentOrgan.Value = None;
                _classmateOrgan.Value = None;
                _professorOwner = NoOwner;
                _studentOwner = NoOwner;
                _classmateOwner = NoOwner;
                NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
            }
            PointersChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _professorOrgan.OnValueChanged -= HandleChanged;
            _studentOrgan.OnValueChanged -= HandleChanged;
            _classmateOrgan.OnValueChanged -= HandleChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>The organ index the role points at, or <see cref="None"/>.</summary>
        public int OrganOf(PointerRole role)
        {
            if (!IsSpawned)
                return None;
            return SlotOf(role).Value;
        }

        /// <summary>
        /// A classmate on a phone taps an organ: asks the server to show "look here" on it for everyone.
        /// Tapping the organ this client already points at stops the pointer (see <see cref="PointerSlotRule"/>).
        /// </summary>
        public void PointAsClassmate(int organIndex)
        {
            if (IsSpawned)
                PointAsClassmateRpc(organIndex);
        }

        /// <summary>A classmate taps empty space: asks the server to remove this client's classmate pointer.</summary>
        public void StopClassmate()
        {
            if (IsSpawned)
                StopClassmateRpc();
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

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void PointAsClassmateRpc(int organIndex, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            Debug.Log($"[PhoneTouch] Room received a classmate point at organ {organIndex} from client {sender}.");
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
            // The server cannot see which device a client is, but it does know who steers the scalpel: that is the student's headset.
            if (PointerRoleRule.For(sender, FindSteerer()) == PointerRole.Student)
            {
                RequestRejected?.Invoke(sender, PointerRejection.NotAClassmate);
                return;
            }
            int next = PointerSlotRule.Next(_classmateOrgan.Value, _classmateOwner, sender, organIndex);
            if (next == None)
            {
                Clear(PointerRole.Classmate);
                return;
            }
            SetOwner(PointerRole.Classmate, sender);
            _classmateOrgan.Value = next;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StopClassmateRpc(RpcParams rpcParams = default)
        {
            if (_classmateOwner == rpcParams.Receive.SenderClientId)
                Clear(PointerRole.Classmate);
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
            if (_classmateOwner == clientId)
                Clear(PointerRole.Classmate);
        }

        private void Clear(PointerRole role)
        {
            SetOwner(role, NoOwner);
            SlotOf(role).Value = None;
        }

        private NetworkVariable<int> SlotOf(PointerRole role)
        {
            switch (role)
            {
                case PointerRole.Professor: return _professorOrgan;
                case PointerRole.Student: return _studentOrgan;
                default: return _classmateOrgan;
            }
        }

        private ulong OwnerOf(PointerRole role)
        {
            switch (role)
            {
                case PointerRole.Professor: return _professorOwner;
                case PointerRole.Student: return _studentOwner;
                default: return _classmateOwner;
            }
        }

        private void SetOwner(PointerRole role, ulong owner)
        {
            switch (role)
            {
                case PointerRole.Professor: _professorOwner = owner; break;
                case PointerRole.Student: _studentOwner = owner; break;
                default: _classmateOwner = owner; break;
            }
        }

        private void HandleChanged(int previous, int current)
        {
            PointersChanged?.Invoke();
        }
    }
}
