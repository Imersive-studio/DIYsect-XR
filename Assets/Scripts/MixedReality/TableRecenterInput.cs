// Authority: none. Moves only this headset's own copy of the table; nothing is sent over the network.
using UnityEngine;
using UnityEngine.InputSystem;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Press A or X, the lower face button on either Quest controller, to bring the table and the body back in front of you.
    /// It is the way out when the table starts out of reach, behind you or above your eyes, and needs no ray and no walking.
    /// It does nothing while the table is being carried by its handle.
    /// </summary>
    [RequireComponent(typeof(TableAlignment))]
    public class TableRecenterInput : MonoBehaviour
    {
        private TableAlignment _alignment;
        private InputAction _recenter;

        private void Awake()
        {
            _alignment = GetComponent<TableAlignment>();
            // XR controllers show up as XRController devices tagged with their hand; primaryButton is A on the right, X on the left.
            _recenter = new InputAction("RecenterTable", InputActionType.Button);
            _recenter.AddBinding("<XRController>{RightHand}/primaryButton");
            _recenter.AddBinding("<XRController>{LeftHand}/primaryButton");
        }

        private void OnEnable()
        {
            _recenter.performed += OnPressed;
            _recenter.Enable();
        }

        private void OnDisable()
        {
            _recenter.Disable();
            _recenter.performed -= OnPressed;
        }

        private void OnDestroy()
        {
            _recenter?.Dispose();
        }

        private void OnPressed(InputAction.CallbackContext context)
        {
            Recenter();
        }

        /// <summary>Puts the table in front of the viewer, unless it is being carried.</summary>
        public void Recenter()
        {
            if (_alignment.Phase == AlignmentPhase.Carrying)
                return;
            _alignment.PlaceInFrontOfViewer();
        }
    }
}
