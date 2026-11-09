// Authority: none. Local to this headset; it only moves this device's own camera offset and sends nothing over the network.
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Puts the student's eyes at one fixed height above the lecture hall floor, whatever their real height or posture.
    /// The headset's tracking origin differs between devices and between launches (it can start where the headset was
    /// lying), so the template's Device mode alone left the eyes at knee height. This measures the head once the
    /// headset is worn, and again after every recenter, then sets the rig's camera offset so the eyes sit at
    /// <see cref="EyeHeight"/>. Walking up and down afterwards still follows the head. Added to the rig by
    /// Tools > DIYsect > Configure Student Rig.
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public class EyeHeightCalibration : MonoBehaviour
    {
        public const float DefaultEyeHeight = 1.6f;

        [SerializeField, Tooltip("Eye height above the scene floor, in metres.")]
        private float _eyeHeight = DefaultEyeHeight;
        [SerializeField, Tooltip("Seconds to wait after the headset goes on or recenters, so tracking has settled.")]
        private float _settleSeconds = 2f;
        [SerializeField, Tooltip("Calibrate anyway after this long if the headset cannot say whether it is worn.")]
        private float _giveUpSeconds = 8f;

        private XROrigin _origin;
        private bool _calibrating;
        private readonly List<XRInputSubsystem> _subsystems = new List<XRInputSubsystem>();

        /// <summary>The height the eyes are put at, in metres above the scene floor.</summary>
        public float EyeHeight => _eyeHeight;

        private void Awake()
        {
            _origin = GetComponent<XROrigin>();
        }

        private IEnumerator Start()
        {
            SubsystemManager.GetSubsystems(_subsystems);
            foreach (XRInputSubsystem subsystem in _subsystems)
                subsystem.trackingOriginUpdated += OnRecentered;

            var wait = new WaitForSeconds(0.5f);
            float waited = 0f;
            while (!IsWorn() && waited < _giveUpSeconds)
            {
                yield return wait;
                waited += 0.5f;
            }
            yield return Calibrate();
        }

        private void OnDestroy()
        {
            foreach (XRInputSubsystem subsystem in _subsystems)
                if (subsystem != null)
                    subsystem.trackingOriginUpdated -= OnRecentered;
        }

        private void OnRecentered(XRInputSubsystem subsystem)
        {
            if (!_calibrating && isActiveAndEnabled)
                StartCoroutine(Calibrate());
        }

        private IEnumerator Calibrate()
        {
            _calibrating = true;
            yield return new WaitForSeconds(_settleSeconds);
            if (_origin != null && _origin.Camera != null)
            {
                float trackedHeadHeight = _origin.CameraInOriginSpacePos.y - _origin.CameraYOffset;
                if (EyeHeightMath.IsUsable(trackedHeadHeight))
                {
                    _origin.CameraYOffset = EyeHeightMath.OffsetFor(_eyeHeight, trackedHeadHeight);
                    Debug.Log($"[EyeHeightCalibration] head {trackedHeadHeight:F2} m, offset {_origin.CameraYOffset:F2} m, eyes at {_eyeHeight:F2} m.");
                }
                else
                {
                    Debug.LogWarning("[EyeHeightCalibration] The headset has no usable height yet, so the eye height was not set.", this);
                }
            }
            _calibrating = false;
        }

        private static bool IsWorn()
        {
            InputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return head.isValid && head.TryGetFeatureValue(CommonUsages.userPresence, out bool present) && present;
        }
    }
}
