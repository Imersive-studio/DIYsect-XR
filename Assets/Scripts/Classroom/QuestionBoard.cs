// Authority: the server. It validates every request and is the only writer of the pin list.
// Clients ask by Rpc and receive the list through the NetworkList. Late joiners get the whole list on spawn.
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using DIYsect.Networking;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The shared list of question pins on the anatomy body. A pin is stored against an organ index and an
    /// organ-local point, never a world point. The server rejects a request when the organ index is wrong,
    /// the point is not finite or not on the organ, the text is empty or too long, or the sender asked for
    /// too many pins recently. A request may carry the asker's camera pose, stored in the body's local space; an invalid
    /// pose never rejects the question, the pin is just stored without a view. A pin can be removed by its author or by the host.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class QuestionBoard : NetworkBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How far outside the organ's bounds, in metres, a pin point may be.")]
        private float _boundsMargin = 0.02f;
        [SerializeField, Min(1), Tooltip("Pins one client may add inside the window.")]
        private int _maxPinsPerWindow = 5;
        [SerializeField, Min(0.1f), Tooltip("Length of the rate limit window in seconds.")]
        private float _windowSeconds = 10f;
        [SerializeField, Min(1), Tooltip("Pins the board holds at most. Further requests are rejected.")]
        private int _maxPins = 200;

        private SharedBody _body;
        private NetworkList<QuestionPin> _pins;
        private PinRateLimiter _limiter;
        private Bounds[] _organBounds;
        private int _nextId;

        /// <summary>Raised on every peer when the pin list changes in any way.</summary>
        public event Action PinsChanged;

        /// <summary>Raised on the server when a request is refused (sender id, reason).</summary>
        public event Action<ulong, PinRejection> RequestRejected;

        public SharedBody Body => _body;
        public int Count => _pins.Count;
        public QuestionPin this[int index] => _pins[index];

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _pins = new NetworkList<QuestionPin>();
            _limiter = new PinRateLimiter(_maxPinsPerWindow, _windowSeconds);
        }

        public override void OnNetworkSpawn()
        {
            _pins.OnListChanged += HandleListChanged;
            // A joining client receives the existing pins without a change event, so announce them once.
            if (!IsServer)
            {
                PinsChanged?.Invoke();
                return;
            }
            _pins.Clear();
            _nextId = 1;
            _organBounds = new Bounds[_body.Organs.Count];
            for (int i = 0; i < _organBounds.Length; i++)
                _organBounds[i] = MeasureOrgan(_body.Organs[i].transform);
            NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
        }

        public override void OnNetworkDespawn()
        {
            _pins.OnListChanged -= HandleListChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>Asks the server to add a pin. The point is in the local space of the organ.</summary>
        public void RequestPin(int organIndex, Vector3 organLocalPoint, string text)
        {
            AddPinRpc(organIndex, organLocalPoint, text ?? string.Empty, Vector3.zero, default);
        }

        /// <summary>
        /// Asks the server to add a pin that also carries the asker's view. The pose is in world space here and is
        /// sent in this board's local space (the body root), so the view follows the body.
        /// </summary>
        public void RequestPin(int organIndex, Vector3 organLocalPoint, string text, Vector3 viewWorldPosition, Quaternion viewWorldRotation)
        {
            Vector3 localPosition = transform.InverseTransformPoint(viewWorldPosition);
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * viewWorldRotation;
            AddPinRpc(organIndex, organLocalPoint, text ?? string.Empty, localPosition, localRotation);
        }

        /// <summary>Asks the server to remove a pin. Only its author or the host is obeyed.</summary>
        public void RequestRemove(int pinId)
        {
            RemovePinRpc(pinId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void AddPinRpc(int organIndex, Vector3 organLocalPoint, string text,
            Vector3 viewPosition, Quaternion viewRotation, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (organIndex < 0 || organIndex >= _organBounds.Length)
            {
                Reject(sender, PinRejection.BadOrgan);
                return;
            }
            if (!PinRules.IsPointOnOrgan(organLocalPoint, _organBounds[organIndex], _boundsMargin))
            {
                Reject(sender, PinRejection.BadPoint);
                return;
            }
            if (!PinRules.TryCleanText(text, out string cleaned))
            {
                Reject(sender, PinRejection.BadText);
                return;
            }
            // Checked last so a malformed request does not use up the sender's allowance.
            if (_pins.Count >= _maxPins || !_limiter.TryAccept(sender, Time.realtimeSinceStartup))
            {
                Reject(sender, PinRejection.RateLimited);
                return;
            }

            // A zero rotation means "no view". A bad view is dropped, never a reason to refuse the question.
            if (!PinRules.TryCleanView(viewPosition, viewRotation, out Quaternion cleanedRotation))
            {
                viewPosition = Vector3.zero;
                cleanedRotation = default;
            }
            _pins.Add(new QuestionPin(_nextId++, organIndex, organLocalPoint, sender, new FixedString64Bytes(cleaned), viewPosition, cleanedRotation));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RemovePinRpc(int pinId, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            for (int i = 0; i < _pins.Count; i++)
            {
                if (_pins[i].Id != pinId)
                    continue;
                if (sender == NetworkManager.ServerClientId || _pins[i].AuthorClientId == sender)
                    _pins.RemoveAt(i);
                return;
            }
        }

        private void Reject(ulong sender, PinRejection reason)
        {
            RequestRejected?.Invoke(sender, reason);
        }

        private void HandleDisconnect(ulong clientId)
        {
            _limiter.Forget(clientId);
        }

        private void HandleListChanged(NetworkListEvent<QuestionPin> change)
        {
            PinsChanged?.Invoke();
        }

        // Bounds of every mesh under the organ, expressed in the organ's own local space.
        private static Bounds MeasureOrgan(Transform organ)
        {
            bool first = true;
            var result = new Bounds();
            foreach (MeshFilter filter in organ.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null)
                    continue;
                Bounds mesh = filter.sharedMesh.bounds;
                Matrix4x4 toOrgan = organ.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = new Vector3(
                        (corner & 1) == 0 ? -mesh.extents.x : mesh.extents.x,
                        (corner & 2) == 0 ? -mesh.extents.y : mesh.extents.y,
                        (corner & 4) == 0 ? -mesh.extents.z : mesh.extents.z);
                    Vector3 point = toOrgan.MultiplyPoint3x4(mesh.center + offset);
                    if (first)
                    {
                        result = new Bounds(point, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        result.Encapsulate(point);
                    }
                }
            }
            return result;
        }
    }
}
