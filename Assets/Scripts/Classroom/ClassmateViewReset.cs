// Authority: none. This only moves the classmate's own camera on their own screen; nothing is sent over the network.
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DIYsect.Classroom
{
    /// <summary>
    /// A way back to the starting view of the body. The on-screen "Reset view" button (tap or click) and, on a laptop, the
    /// Home or R key all ask the <see cref="ClassmateOrbitCamera"/> to return to its starting framing.
    /// The button is a UI element, so the PointerGestureTracker ignores a press on it and it can never place a pin.
    /// </summary>
    public class ClassmateViewReset : MonoBehaviour
    {
        [SerializeField] private ClassmateOrbitCamera _orbit;
        [SerializeField, Tooltip("The on-screen button a phone or laptop classmate presses.")]
        private Button _button;

        [SerializeField, Tooltip("Optional. While the camera follows someone, Reset view means back to my view instead.")]
        private ClassmateFollowCamera _follow;

        private InputAction _keys;

        private void Awake()
        {
            bool valid = true;
            if (_orbit == null) { Debug.LogError($"{name}: ClassmateViewReset has no ClassmateOrbitCamera. Run Tools > DIYsect > Add Classmate Desktop Camera.", this); valid = false; }
            if (_button == null) { Debug.LogError($"{name}: ClassmateViewReset has no Button. Run Tools > DIYsect > Add Classmate Desktop Camera.", this); valid = false; }
            if (!valid)
            {
                enabled = false;
                return;
            }
            _keys = new InputAction("ResetView", InputActionType.Button);
            _keys.AddBinding("<Keyboard>/home");
            _keys.AddBinding("<Keyboard>/r");
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Reset);
            _keys.performed += OnKey;
            _keys.Enable();
        }

        private void OnDisable()
        {
            // The button can be destroyed first when the scene unloads.
            if (_button != null)
                _button.onClick.RemoveListener(Reset);
            if (_keys == null)
                return;
            _keys.performed -= OnKey;
            _keys.Disable();
        }

        private void OnDestroy()
        {
            _keys?.Dispose();
        }

        private void OnKey(InputAction.CallbackContext context)
        {
            Reset();
        }

        /// <summary>
        /// Returns the camera to its starting view. While following someone it only goes back to my view (where I was before
        /// I followed), so one press never throws away the framing I chose; a second press then resets it.
        /// </summary>
        public void Reset()
        {
            // Unity also calls a method named Reset when the component is added in the editor, before _orbit is assigned.
            if (_orbit == null)
                return;
            if (_follow != null && _follow.IsFollowing)
            {
                _follow.BackToMyView();
                return;
            }
            _orbit.ResetView();
        }
    }
}
