// Authority: none. Local presentation only; it reads this headset's controller buttons and bends a hand model.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DIYsect.Interaction
{
    /// <summary>
    /// A hand shown in place of the controller model, so people watching the demo see hands, not controllers. It sits on
    /// the controller and moves with it; the fingers follow the buttons through <see cref="HandPoseRule"/>: pulling the
    /// trigger pinches the index finger toward the thumb, squeezing the grip closes the hand around the tool.
    ///
    /// Everything else stays the controller's job: grabbing, cutting, vibration and the buttons work exactly as before.
    /// Run Tools > DIYsect > Add Controller Hands to put one on each controller and fill in the joints and buttons.
    /// </summary>
    public class ControllerHand : MonoBehaviour
    {
        [Serializable]
        private struct Finger
        {
            [Tooltip("The finger's bending joints, from the knuckle to the tip.")]
            public Transform[] Joints;
        }

        [SerializeField, Tooltip("The trigger value of this controller (XRI Activate Value).")]
        private InputActionReference _trigger;
        [SerializeField, Tooltip("The grip value of this controller (XRI Select Value).")]
        private InputActionReference _grip;
        [SerializeField, Tooltip("Thumb, index, middle, ring, little.")]
        private Finger[] _fingers = new Finger[HandPoseRule.FingerCount];

        private readonly float[] _bends = new float[HandPoseRule.FingerCount];
        private Quaternion[][] _rest;
        private bool _valid;

        private void Awake()
        {
            _valid = Validate();
            if (!_valid)
            {
                enabled = false;
                return;
            }
            // The model's own straight pose. Bends are added on top of it, so the hand never loses its shape.
            _rest = new Quaternion[_fingers.Length][];
            for (int f = 0; f < _fingers.Length; f++)
            {
                _rest[f] = new Quaternion[_fingers[f].Joints.Length];
                for (int j = 0; j < _fingers[f].Joints.Length; j++)
                    _rest[f][j] = _fingers[f].Joints[j].localRotation;
            }
        }

        // Polling exception: button values have no change event worth subscribing to every frame, and the hand must follow at once.
        private void LateUpdate()
        {
            HandPoseRule.Bends(Read(_trigger), Read(_grip), _bends);
            for (int f = 0; f < _fingers.Length; f++)
            {
                Quaternion bend = Quaternion.Euler(HandPoseRule.Degrees(f, _bends[f]), 0f, 0f);
                for (int j = 0; j < _fingers[f].Joints.Length; j++)
                    _fingers[f].Joints[j].localRotation = _rest[f][j] * bend;
            }
        }

        private void OnDisable()
        {
            if (!_valid)
                return;
            // Leave the model straight when the controller goes away, so it never reappears frozen in a fist.
            for (int f = 0; f < _fingers.Length; f++)
                for (int j = 0; j < _fingers[f].Joints.Length; j++)
                    _fingers[f].Joints[j].localRotation = _rest[f][j];
        }

        private static float Read(InputActionReference reference)
        {
            InputAction action = reference != null ? reference.action : null;
            return action != null && action.enabled ? action.ReadValue<float>() : 0f;
        }

        private bool Validate()
        {
            if (_fingers == null || _fingers.Length != HandPoseRule.FingerCount)
            {
                Debug.LogError($"{name}: ControllerHand needs five fingers. Run Tools > DIYsect > Add Controller Hands.", this);
                return false;
            }
            foreach (Finger finger in _fingers)
            {
                if (finger.Joints == null || finger.Joints.Length == 0 || Array.Exists(finger.Joints, joint => joint == null))
                {
                    Debug.LogError($"{name}: ControllerHand has a finger with missing joints. Run Tools > DIYsect > Add Controller Hands.", this);
                    return false;
                }
            }
            if (_trigger == null || _grip == null)
                Debug.LogWarning($"{name}: ControllerHand has no trigger or grip button, so the fingers stay straight.", this);
            return true;
        }
    }
}
