// Authority: none. Local to this headset; it only adds a local component and sends nothing over the network.
using Unity.XR.CoreUtils;
using UnityEngine;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Gives the student's rig <see cref="EyeHeightCalibration"/> when the student role starts, so the shared scene itself never
    /// carries the calibration. The professor's rig stays exactly as the template ships it, with no calibration on it (a guard test
    /// in the student rig tests checks that). Saved switched off; <c>ClassroomRole</c> turns it on for the student only.
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public class StudentEyeHeightBootstrap : MonoBehaviour
    {
        private void OnEnable()
        {
            if (!TryGetComponent(out EyeHeightCalibration _))
                gameObject.AddComponent<EyeHeightCalibration>();
        }
    }
}
