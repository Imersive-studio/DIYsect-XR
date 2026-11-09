// Authority: the server. Headsets send their head pose by Rpc; the server validates it and is the only writer of the two slots.
// Every peer receives the slots through NetworkVariables. A slot is cleared when its owner disconnects, withdraws or goes quiet.
using System;
using DIYsect.Networking;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Where the professor's head and the student's head are, in the body's local space (each device puts its own body
    /// somewhere different, so a world position would mean nothing to a classmate). A classmate reads these to look at the body
    /// from the same place. A role holds one head: the host is the professor, and when a second client publishes a head as
    /// student, the newer one replaces the older one.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class HeadPoseBoard : NetworkBehaviour, IHeadPoseSink
    {
        private const ulong NoOwner = ulong.MaxValue;

        [SerializeField, Min(1), Tooltip("Head updates one client may send inside the window.")]
        private int _maxRequestsPerWindow = 60;
        [SerializeField, Min(0.1f)] private float _windowSeconds = 2f;
        [SerializeField, Min(0.5f), Tooltip("A head farther than this many metres from the body is refused.")]
        private float _maxRadius = 10f;
        [SerializeField, Min(0.5f), Tooltip("A head that has not been updated for this many seconds is dropped.")]
        private float _quietSeconds = 3f;

        private readonly NetworkVariable<HeadPose> _professor = new NetworkVariable<HeadPose>(HeadPose.None);
        private readonly NetworkVariable<HeadPose> _student = new NetworkVariable<HeadPose>(HeadPose.None);
        private PinRateLimiter _limiter;
        private ulong _studentOwner = NoOwner;
        private float _professorSeenAt;
        private float _studentSeenAt;

        /// <summary>Raised on every peer when either head appears, disappears or moves.</summary>
        public event Action Changed;

        private void Awake()
        {
            _limiter = new PinRateLimiter(_maxRequestsPerWindow, _windowSeconds);
        }

        public override void OnNetworkSpawn()
        {
            _professor.OnValueChanged += HandleChanged;
            _student.OnValueChanged += HandleChanged;
            if (IsServer)
            {
                _professor.Value = HeadPose.None;
                _student.Value = HeadPose.None;
                _studentOwner = NoOwner;
                NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
            }
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _professor.OnValueChanged -= HandleChanged;
            _student.OnValueChanged -= HandleChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>True when that person's head is known.</summary>
        public bool IsPresent(PointerRole role) => IsSpawned && SlotOf(role).Value.Present;

        /// <summary>That person's head pose in the body's local space. False when they are not in the session.</summary>
        public bool TryGetLocalPose(PointerRole role, out Vector3 position, out Quaternion rotation)
        {
            HeadPose pose = IsSpawned ? SlotOf(role).Value : HeadPose.None;
            position = pose.Position;
            rotation = pose.Rotation;
            return pose.Present;
        }

        public void Publish(Vector3 localPosition, Quaternion localRotation)
        {
            if (IsSpawned)
                PublishRpc(localPosition, localRotation);
        }

        public void Withdraw()
        {
            if (IsSpawned)
                WithdrawRpc();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
                return;
            float now = Time.realtimeSinceStartup;
            if (_professor.Value.Present && now - _professorSeenAt > _quietSeconds)
                _professor.Value = HeadPose.None;
            if (_student.Value.Present && now - _studentSeenAt > _quietSeconds)
                ClearStudent();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void PublishRpc(Vector3 localPosition, Quaternion localRotation, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            float now = Time.realtimeSinceStartup;
            if (!_limiter.TryAccept(sender, now))
                return;
            if (!HeadPoseRule.IsAcceptable(localPosition, localRotation, _maxRadius))
                return;
            var pose = new HeadPose { Present = true, Position = localPosition, Rotation = localRotation.normalized };
            if (HeadPoseRule.RoleFor(sender, NetworkManager.ServerClientId) == PointerRole.Professor)
            {
                _professorSeenAt = now;
                _professor.Value = pose;
                return;
            }
            _studentOwner = sender;
            _studentSeenAt = now;
            _student.Value = pose;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void WithdrawRpc(RpcParams rpcParams = default)
        {
            ClearOwnedBy(rpcParams.Receive.SenderClientId);
        }

        private void HandleDisconnect(ulong clientId)
        {
            _limiter.Forget(clientId);
            ClearOwnedBy(clientId);
        }

        private void ClearOwnedBy(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
                _professor.Value = HeadPose.None;
            else if (clientId == _studentOwner)
                ClearStudent();
        }

        private void ClearStudent()
        {
            _studentOwner = NoOwner;
            _student.Value = HeadPose.None;
        }

        private NetworkVariable<HeadPose> SlotOf(PointerRole role) => role == PointerRole.Professor ? _professor : _student;

        private void HandleChanged(HeadPose previous, HeadPose current)
        {
            Changed?.Invoke();
        }
    }
}
