// Authority: none of its own. It is a client-side bridge; the server (AssistBoard) decides whether the request counts.
using UnityEngine;
using UnityEngine.InputSystem;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The student's "Help me" button: the Y button, the secondary button on the left controller. Press it to ask for help and
    /// press it again to take the request back. The other buttons are taken: A and X grab, B and the left menu button open the
    /// quit menu, and the thumbsticks walk, turn and recentre the table. It only sends the press; the server ignores it
    /// unless this student is holding the scalpel. Add it only for the student (it is in the role's student list).
    /// </summary>
    public class AssistRequestInput : MonoBehaviour
    {
        [SerializeField, Tooltip("The body's help board.")]
        private AssistBoard _board;

        private InputAction _help;

        private void Awake()
        {
            if (_board == null)
            {
                Debug.LogError($"{name}: AssistRequestInput has no AssistBoard. Run Tools > DIYsect > Add Help Request.", this);
                enabled = false;
                return;
            }
            // XR controllers show up as XRController devices tagged with their hand, as in TableRecenterInput.
            _help = new InputAction("AskForHelp", InputActionType.Button);
            _help.AddBinding("<XRController>{LeftHand}/secondaryButton");
        }

        private void OnEnable()
        {
            if (_help == null)
                return;
            _help.performed += OnPressed;
            _help.Enable();
        }

        private void OnDisable()
        {
            if (_help == null)
                return;
            _help.Disable();
            _help.performed -= OnPressed;
        }

        private void OnDestroy()
        {
            _help?.Dispose();
        }

        private void OnPressed(InputAction.CallbackContext context)
        {
            _board.Toggle();
        }
    }
}
