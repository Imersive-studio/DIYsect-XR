// Authority: the server. It judges every release and moves the organ. Clients only hear the result.
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Every organ's home pose is a socket. When the server frees an organ, this checks where it was dropped
    /// with <see cref="SocketRule"/>:
    /// accepted organs ease into their exact home pose, organs dropped at another organ's socket ease back to their
    /// own home pose (rejected), organs dropped anywhere else over the open body ease home with no verdict
    /// (returned), and organs dropped away from the body are handed to <see cref="OrganPhysics"/> so they fall and
    /// rest where they land. The easing is a short server-side move; clients follow through the NetworkTransform.
    /// Every peer is told the outcome by <see cref="SnapResolved"/> so it can show feedback.
    /// </summary>
    [RequireComponent(typeof(SharedBody), typeof(OrganGrabAuthority), typeof(OrganPhysics))]
    public class OrganSocketKeeper : NetworkBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How close in metres the organ's centre must be to a socket's centre.")]
        private float _positionTolerance = 0.03f;
        [SerializeField, Range(0f, 180f), Tooltip("How far in degrees the organ may be turned from its home rotation to be accepted.")]
        private float _angleToleranceDegrees = 45f;
        [SerializeField, Min(0f), Tooltip("Up to this distance in metres the organ is accepted if its own socket is the nearest one.")]
        private float _captureRadius = 0.03f;
        [SerializeField, Tooltip("The open body, in the body root's local space. A drop inside it puts the organ back home.")]
        private Bounds _bodyZone;
        [SerializeField, Min(0f), Tooltip("How long an organ takes to ease into its home pose, in seconds.")]
        private float _easeSeconds = 0.15f;
        [SerializeField, Min(0.1f), Tooltip("A loose organ this far below the body root (metres) is lost and goes back home.")]
        private float _lostDepth = 1.5f;

        private SharedBody _body;
        private OrganGrabAuthority _authority;
        private OrganPhysics _physics;
        private Coroutine[] _easing;
        private ISnappable[] _snappables;
        private Pose[] _sockets;
        private SocketRule _rule;

        /// <summary>Raised on every peer when an organ was judged (organ index, outcome). Not raised for <see cref="SnapOutcome.None"/>.</summary>
        public event Action<int, SnapOutcome> SnapResolved;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _authority = GetComponent<OrganGrabAuthority>();
            _physics = GetComponent<OrganPhysics>();
            _rule = new SocketRule(_positionTolerance, _angleToleranceDegrees, Mathf.Max(_captureRadius, _positionTolerance));
            if (_bodyZone.size == Vector3.zero)
                Debug.LogError($"{name}: the body zone is empty, so organs dropped over the body will fall. Run Tools > DIYsect > Add Organ Physics.", this);

            _snappables = new ISnappable[_body.Organs.Count];
            _easing = new Coroutine[_snappables.Length];
            _sockets = new Pose[_snappables.Length];
            var skipped = new System.Collections.Generic.List<string>();
            for (int i = 0; i < _snappables.Length; i++)
            {
                _snappables[i] = _body.Organs[i] != null ? _body.Organs[i].GetComponent<ISnappable>() : null;
                if (_snappables[i] == null)
                {
                    Debug.LogError($"{name}: organ {i} has no SnappableOrgan. Run Tools > DIYsect > Add Organ Sockets.", this);
                    enabled = false;
                    return;
                }
                if (!_snappables[i].IsValid)
                {
                    // Skip it: with no usable home it can neither be judged nor moved. NaN makes SocketRule ignore the socket.
                    skipped.Add(_body.Organs[i].name);
                    _snappables[i] = null;
                    _sockets[i] = new Pose(new Vector3(float.NaN, float.NaN, float.NaN), Quaternion.identity);
                    continue;
                }
                _sockets[i] = _snappables[i].HomeCentrePose;
            }
            if (skipped.Count > 0)
                Debug.LogError($"{name}: these organs have no usable socket and will not snap: {string.Join(", ", skipped)}. Run Tools > DIYsect > Add Organ Sockets.", this);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && enabled)
            {
                _authority.ServerReleased += OnServerReleased;
                _authority.ServerGranted += StopEasing;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_authority != null)
            {
                _authority.ServerReleased -= OnServerReleased;
                _authority.ServerGranted -= StopEasing;
            }
        }

        /// <summary>Checks the organ against the sockets, moves it if needed and tells every peer. Server only.</summary>
        private void OnServerReleased(int organIndex)
        {
            if (organIndex < 0 || organIndex >= _snappables.Length || _snappables[organIndex] == null)
                return;
            SnapDecision decision = _rule.Evaluate(organIndex, _snappables[organIndex].CurrentCentrePose, _sockets);
            SnapOutcome outcome = decision.Outcome;
            if (outcome == SnapOutcome.None && IsOverBody(organIndex))
                outcome = SnapOutcome.Returned;
            if (outcome == SnapOutcome.None)
            {
                _physics.Loosen(organIndex);
                StopEasing(organIndex);
                _easing[organIndex] = StartCoroutine(WatchForLoss(organIndex));
                return;
            }
            _physics.Seat(organIndex);
            StopEasing(organIndex);
            _easing[organIndex] = StartCoroutine(EaseHome(organIndex));
            SnapResolvedRpc(organIndex, outcome);
        }

        /// <summary>Server only. True when the organ's centre is inside the open body zone.</summary>
        public bool IsOverBody(int organIndex)
        {
            Transform organ = _body.Organs[organIndex].transform;
            Vector3 centre = _snappables[organIndex].CurrentCentrePose.position;
            Vector3 rootLocal = transform.InverseTransformPoint(organ.parent.TransformPoint(centre));
            return _bodyZone.Contains(rootLocal);
        }

        // Moves the organ from where it was dropped to its home pose over a short time, with a smooth start and stop.
        private IEnumerator EaseHome(int organIndex)
        {
            Transform organ = _body.Organs[organIndex].transform;
            Pose home = _snappables[organIndex].HomeRootPose;
            Vector3 startPosition = organ.localPosition;
            Quaternion startRotation = organ.localRotation;
            for (float elapsed = 0f; elapsed < _easeSeconds; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _easeSeconds);
                organ.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, home.position, t), Quaternion.Slerp(startRotation, home.rotation, t));
                yield return null;
            }
            _snappables[organIndex].SnapHome();
            _easing[organIndex] = null;
        }

        // Safety net, like a reset for objects that fall out of the world: a loose organ that drops far below the
        // body (no floor caught it) is put back home. Checked twice a second while it is loose, not every frame.
        private IEnumerator WatchForLoss(int organIndex)
        {
            var wait = new WaitForSeconds(0.5f);
            Transform organ = _body.Organs[organIndex].transform;
            while (_physics.IsLoose(organIndex))
            {
                if (transform.InverseTransformPoint(organ.position).y < -_lostDepth)
                {
                    _physics.Seat(organIndex);
                    _easing[organIndex] = null;
                    _snappables[organIndex].SnapHome();
                    SnapResolvedRpc(organIndex, SnapOutcome.Returned);
                    yield break;
                }
                yield return wait;
            }
            _easing[organIndex] = null;
        }

        // A new grab takes the organ out of the hands of an unfinished ease or loss watch.
        private void StopEasing(int organIndex)
        {
            if (_easing == null || organIndex < 0 || organIndex >= _easing.Length || _easing[organIndex] == null)
                return;
            StopCoroutine(_easing[organIndex]);
            _easing[organIndex] = null;
        }

        [Rpc(SendTo.Everyone)]
        private void SnapResolvedRpc(int organIndex, SnapOutcome outcome)
        {
            SnapResolved?.Invoke(organIndex, outcome);
        }
    }
}
