// Authority: none here. It only sends this headset's head pose to the HeadPoseBoard; the server validates it and decides who sees it.
using System.Collections;
using DIYsect.Networking;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Tells the classroom where this headset's head is, in the body's local space, so a classmate can look at the body from the
    /// same place. It does nothing on a device with no headset rig (a laptop or phone classmate), because their rig is switched off.
    /// It sends a few times a second, and only when the head has moved, with a slow repeat so the server knows the head is still here.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class HeadPosePublisher : MonoBehaviour
    {
        [SerializeField, Min(0.02f), Tooltip("Seconds between checks of the head.")]
        private float _intervalSeconds = 0.1f;
        [SerializeField, Min(0.1f), Tooltip("Seconds after which an unmoved head is sent again.")]
        private float _repeatSeconds = 1f;
        [SerializeField, Min(0f), Tooltip("Metres the head must move before it is sent.")]
        private float _moveMetres = 0.005f;
        [SerializeField, Min(0f), Tooltip("Degrees the head must turn before it is sent.")]
        private float _turnDegrees = 0.5f;

        private SharedBody _body;
        private IHeadPoseSink _sink;
        private Transform _head;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation = Quaternion.identity;
        private float _lastSentAt = float.NegativeInfinity;
        private bool _published;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _sink = GetComponent<IHeadPoseSink>();
            if (_sink == null)
            {
                Debug.LogError($"{name}: HeadPosePublisher needs a HeadPoseBoard on the same object. Run Tools > DIYsect > Add Head Pose.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (_published)
                _sink.Withdraw();
            _published = false;
        }

        private IEnumerator Run()
        {
            var wait = new WaitForSeconds(_intervalSeconds);
            while (true)
            {
                yield return wait;
                if (_head == null)
                    _head = FindHead();
                if (_head == null)
                    continue;
                Send(_head);
            }
        }

        // The XR rig's camera is the head. A classmate's rig is inactive, so nothing is found.
        private static Transform FindHead()
        {
            XROrigin origin = FindAnyObjectByType<XROrigin>();
            return origin != null && origin.Camera != null ? origin.Camera.transform : null;
        }

        private void Send(Transform head)
        {
            Transform body = _body.transform;
            Vector3 position = body.InverseTransformPoint(head.position);
            Quaternion rotation = Quaternion.Inverse(body.rotation) * head.rotation;
            float now = Time.realtimeSinceStartup;
            bool moved = (position - _lastPosition).sqrMagnitude > _moveMetres * _moveMetres || Quaternion.Angle(rotation, _lastRotation) > _turnDegrees;
            if (!moved && now - _lastSentAt < _repeatSeconds)
                return;
            _lastPosition = position;
            _lastRotation = rotation;
            _lastSentAt = now;
            _published = true;
            _sink.Publish(position, rotation);
        }
    }
}
