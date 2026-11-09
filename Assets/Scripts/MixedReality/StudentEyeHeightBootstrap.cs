// Authority: none. Local to this headset; it only adds a local component and sends nothing over the network.
using Unity.XR.CoreUtils;
using UnityEngine;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Gives a headset that stands in the lecture hall <see cref="EyeHeightCalibration"/>, so the shared scene itself never carries
    /// the calibration. That is the student, and the professor while in the classroom view. In mixed reality the rig is left as the
    /// template ships it, with no calibration on it (a guard test in the student rig tests checks the saved scene).
    /// Saved switched off; <c>ClassroomRole</c> turns it on where it belongs, and off again when the professor goes back to mixed reality.
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public class StudentEyeHeightBootstrap : MonoBehaviour
    {
        private void OnEnable()
        {
            if (!TryGetComponent(out EyeHeightCalibration calibration))
                gameObject.AddComponent<EyeHeightCalibration>();
            else
                calibration.enabled = true;
        }

        private void OnDisable()
        {
            if (TryGetComponent(out EyeHeightCalibration calibration))
                calibration.enabled = false;
        }
    }
}
