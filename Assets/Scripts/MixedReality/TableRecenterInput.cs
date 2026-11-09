// Authority: none. Moves only this headset's own copy of the table; nothing is sent over the network.
using UnityEngine;
using UnityEngine.InputSystem;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Click either thumbstick (press it in) to bring the table and the body back in front of you.
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
            // XR controllers show up as XRController devices tagged with their hand. A and X grab, B and the left menu button open
            // the quit menu, and the thumbsticks walk and turn, so the click of a thumbstick is the one free control.
            _recenter = new InputAction("RecenterTable", InputActionType.Button);
            _recenter.AddBinding("<XRController>{RightHand}/{Primary2DAxisClick}");
            _recenter.AddBinding("<XRController>{LeftHand}/{Primary2DAxisClick}");
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
