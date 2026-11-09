// Authority: none. Local presentation only, sends nothing over the network.
using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DIYsect.Menu
{
    /// <summary>
    /// The in-app quit menu. This is the only writer of <see cref="State"/>; the panel and the confirm group are
    /// derived from it. A controller button or key fires the toggle action, and a hand gesture or a button can call
    /// <see cref="Toggle"/>. When the menu opens it is placed once, in front of the main camera.
    /// </summary>
    public class QuitMenu : MonoBehaviour
    {
        private const float DistanceInFront = 0.8f;

        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _confirmGroup;
        [SerializeField] private InputActionReference _toggleAction;

        private QuitMenuState _state = QuitMenuState.Closed;

        /// <summary>What runs when the student confirms. Tests replace it.</summary>
        public Action Quitter = DefaultQuit;

        /// <summary>The current state.</summary>
        public QuitMenuState State => _state;

        /// <summary>Raised after the state changes, with the new state.</summary>
        public event Action<QuitMenuState> Changed;

        private void OnEnable()
        {
            if (_toggleAction == null || _toggleAction.action == null)
                return;
            _toggleAction.action.performed += OnTogglePerformed;
            _toggleAction.action.Enable();
        }

        private void OnDisable()
        {
            if (_toggleAction == null || _toggleAction.action == null)
                return;
            _toggleAction.action.performed -= OnTogglePerformed;
        }

        /// <summary>Opens the menu if closed, closes it if open. Call from a hand gesture or a button.</summary>
        public void Toggle() => Send(QuitMenuEvent.Toggle);

        /// <summary>The Quit and Yes buttons. Asks first, then quits on the second call.</summary>
        public void RequestQuit() => Send(QuitMenuEvent.Quit);

        /// <summary>The Resume and Stay buttons.</summary>
        public void Cancel() => Send(QuitMenuEvent.Cancel);

        private void OnTogglePerformed(InputAction.CallbackContext context) => Toggle();

        // The only place the state changes.
        private void Send(QuitMenuEvent menuEvent)
        {
            QuitMenuState before = _state;
            bool quit = QuitMenuRules.IsQuitRequested(before, menuEvent);
            _state = QuitMenuRules.Next(before, menuEvent);
            if (_state != before)
            {
                if (before == QuitMenuState.Closed)
                    PlaceInFrontOfCamera();
                Apply();
                Changed?.Invoke(_state);
            }
            if (quit)
                Quitter?.Invoke();
        }

        // The only place the panel and confirm group are set, both derived from the state.
        private void Apply()
        {
            if (_panel != null)
                _panel.SetActive(_state != QuitMenuState.Closed);
            if (_confirmGroup != null)
                _confirmGroup.SetActive(_state == QuitMenuState.Confirming);
        }

        // Flat and upright, at eye height, facing the camera. Done once on opening.
        private void PlaceInFrontOfCamera()
        {
            Camera head = Camera.main;
            if (_panel == null || head == null)
                return;
            Vector3 eye = head.transform.position;
            Vector3 flatForward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.ProjectOnPlane(head.transform.up, Vector3.up);
            flatForward.Normalize();
            // A world canvas reads correctly from behind, so it looks the same way the student looks.
            _panel.transform.SetPositionAndRotation(eye + flatForward * DistanceInFront, Quaternion.LookRotation(flatForward, Vector3.up));
        }

        private static void DefaultQuit()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}
