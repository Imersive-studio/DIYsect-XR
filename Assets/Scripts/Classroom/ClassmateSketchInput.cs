// Authority: none here. It is a local input bridge; the sketch GuideLine on the server decides what everyone sees.
using System;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DIYsect.Classroom
{
    /// <summary>
    /// A classmate on a phone sketches on the body to show where they mean. While <see cref="Drawing"/> is on, a one-finger drag
    /// (or the left mouse button) is cast from the camera onto the body and the hits become the points of the classmate's sketch line,
    /// lifted a few millimetres off the surface. Finger down starts a stroke (it replaces the previous sketch), finger up ends it,
    /// and points that miss the body are skipped. The stroke is only asked for when the finger first touches the body, so a miss
    /// never wipes the old sketch. A second finger is a pinch: it ends the stroke and zooms as usual.
    /// While drawing, the tracker is told the one-finger press is taken, so the camera does not orbit and nothing is named,
    /// pointed at or pinned for that finger. A press that starts on UI never draws.
    /// A button in the classmate UI calls <see cref="SetDrawing"/> and <see cref="Clear"/>.
    /// </summary>
    public class ClassmateSketchInput : MonoBehaviour
    {
        [SerializeField, Tooltip("The shared body. Only hits on it count.")]
        private SharedBody _body;
        [SerializeField, Tooltip("The classmate's own sketch line (the ClassmateSketch child of the body), not the professor's guide line.")]
        private GuideLine _line;
        [SerializeField, Tooltip("Told that the one-finger press is taken while drawing.")]
        private PointerGestureTracker _gestures;
        [SerializeField, Tooltip("Camera that looks at the body. Leave empty to use the camera tagged MainCamera.")]
        private Camera _camera;
        [SerializeField, Min(0.1f)] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _hitLayers = ~0;
        [SerializeField, Min(0f), Tooltip("How far the line floats above the surface (metres).")]
        private float _liftMeters = 0.003f;
        [SerializeField, Min(0.001f), Tooltip("A point is sent only after the finger moved this far over the body (metres).")]
        private float _minSpacing = 0.006f;
        [SerializeField, Tooltip("Draws this phone's ink at once, before the room echoes it.")]
        private DIYsect.Interaction.SketchPresenter _presenter;
        [SerializeField, Tooltip("A soft violet disc that sits on the body under the finger while drawing. Only this phone sees it.")]
        private Transform _cursor;

        private const float CursorLift = 0.004f;
        private readonly System.Collections.Generic.List<Vector3> _ink = new System.Collections.Generic.List<Vector3>();

        private readonly GrabRequest _request = new GrabRequest();
        private bool _drawing;
        private bool _pressActive;
        private bool _pressBlocked;
        private bool _requestedThisPress;
        private Vector3? _lastSent;
        private Vector3? _pending;

        /// <summary>True while a finger drag draws on the body instead of orbiting the camera.</summary>
        public bool Drawing => _drawing;

        /// <summary>Raised when drawing is switched on or off, so a button can show its state.</summary>
        public event Action<bool> DrawingChanged;

        private void Awake()
        {
            bool valid = true;
            if (_body == null) { Debug.LogError($"{name}: ClassmateSketchInput has no SharedBody. Run Tools > DIYsect > Add Classmate Sketch.", this); valid = false; }
            if (_line == null) { Debug.LogError($"{name}: ClassmateSketchInput has no sketch line. Run Tools > DIYsect > Add Classmate Sketch.", this); valid = false; }
            if (_gestures == null) { Debug.LogError($"{name}: ClassmateSketchInput has no PointerGestureTracker.", this); valid = false; }
            if (!valid)
                enabled = false;
        }

        private void OnEnable()
        {
            DIYsect.Interaction.SketchPresenter.ChipHiddenOnThisDevice = true;
            _line.StrokeResolved += HandleStrokeResolved;
            _line.StrokeRevoked += HandleStrokeRevoked;
        }

        private void OnDisable()
        {
            DIYsect.Interaction.SketchPresenter.ChipHiddenOnThisDevice = false;
            EndPress();
            if (_drawing)
                SetDrawing(false);
            if (_line != null)
            {
                _line.StrokeResolved -= HandleStrokeResolved;
                _line.StrokeRevoked -= HandleStrokeRevoked;
            }
        }

        /// <summary>Switches drawing on or off. Turning it off ends a stroke in progress; the sketch itself stays.</summary>
        public void SetDrawing(bool on)
        {
            if (_drawing == on)
                return;
            if (!on)
                EndPress();
            _drawing = on;
            if (_gestures != null)
                _gestures.OneFingerClaimed = on;
            DrawingChanged?.Invoke(on);
        }

        /// <summary>Removes the sketch for everyone (the server decides if this classmate may).</summary>
        public void Clear()
        {
            if (_line != null)
                _line.Clear();
        }

        // Reading the finger every frame is what a drag needs; there is no event for a held-down finger moving.
        private void Update()
        {
            ReadPointer(out int fingers, out Vector2 position, out int touchId);
            bool wantsDraw = SketchRules.ShouldDraw(_drawing, fingers);
            if (fingers == 0)
                _pressBlocked = false;
            else if (fingers > 1)
                _pressBlocked = true; // after a pinch the remaining finger draws nothing until everyone lets go
            if (!wantsDraw || _pressBlocked)
            {
                EndPress();
                return;
            }
            if (!_pressActive)
            {
                if (PointerGestureTracker.IsOverUi(touchId))
                {
                    _pressBlocked = true; // a press that began on a button never draws, even if the finger slides off it
                    return;
                }
                _pressActive = true;
            }
            Sample(position);
        }

        private void Sample(Vector2 screenPosition)
        {
            Camera view = _camera != null ? _camera : Camera.main;
            if (view == null)
                return;
            Ray ray = view.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hitLayers, QueryTriggerInteraction.Ignore)
                || !hit.collider.transform.IsChildOf(_body.transform))
                return;
            Vector3 point = SketchRules.Lift(hit.point, hit.normal, _liftMeters);
            if (_cursor != null)
            {
                _cursor.gameObject.SetActive(true);
                _cursor.SetPositionAndRotation(hit.point + hit.normal * CursorLift, Quaternion.LookRotation(-hit.normal));
            }
            ShowInk(point);
            if (!SketchRules.IsFarEnough(_lastSent, point, _minSpacing))
                return;
            if (!_requestedThisPress)
            {
                _requestedThisPress = true;
                Debug.Log($"[PhoneTouch] Sketch starts on {hit.collider.name}. Sketch line in the room: {_line.IsSpawned}.");
                Apply(_request.Handle(GrabRequest.Event.Select));
            }
            if (_request.Phase == GrabPhase.Granted)
            {
                _lastSent = point;
                _line.AddPoint(point);
            }
            else
            {
                _pending = point; // the server has not answered yet; the latest point goes out when it does
            }
        }

        // This phone's own ink, in the line's space, so the finger sees the stroke before the room answers.
        private void ShowInk(Vector3 worldPoint)
        {
            if (_presenter == null)
                return;
            Vector3 local = _line.transform.InverseTransformPoint(worldPoint);
            if (_ink.Count > 0 && (_ink[_ink.Count - 1] - local).sqrMagnitude < _minSpacing * _minSpacing)
                return;
            _ink.Add(local);
            _presenter.ShowPreview(_ink);
        }

        private void EndPress()
        {
            if (_cursor != null)
                _cursor.gameObject.SetActive(false);
            if (!_pressActive)
                return;
            if (_presenter != null)
                _presenter.EndPreview();
            _ink.Clear();
            _pressActive = false;
            _requestedThisPress = false;
            _pending = null;
            _lastSent = null;
            Apply(_request.Handle(GrabRequest.Event.Exit));
        }

        private void HandleStrokeResolved(bool granted)
        {
            Debug.Log($"[PhoneTouch] The room {(granted ? "accepted" : "refused")} the sketch.");
            Apply(_request.HandleAnswer(granted));
        }

        private void HandleStrokeRevoked()
        {
            Apply(_request.Handle(GrabRequest.Event.Revoked));
        }

        // Carries out what the stroke state machine asked for.
        private void Apply(GrabRequest.Actions actions)
        {
            if ((actions & GrabRequest.Actions.SendRequest) != 0)
                _line.StartStroke();
            if ((actions & GrabRequest.Actions.StartStreaming) != 0 && _pending.HasValue)
            {
                _lastSent = _pending;
                _line.AddPoint(_pending.Value);
                _pending = null;
            }
            if ((actions & GrabRequest.Actions.SendRelease) != 0)
                _line.EndStroke();
        }

        // First finger of the touchscreen, or the mouse button when no finger is down. Fingers is how many are down.
        private static void ReadPointer(out int fingers, out Vector2 position, out int touchId)
        {
            fingers = 0;
            position = Vector2.zero;
            touchId = -1;
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (!touch.isInProgress)
                        continue;
                    if (fingers == 0)
                    {
                        position = touch.position.ReadValue();
                        touchId = touch.touchId.ReadValue();
                    }
                    fingers++;
                }
            }
            if (fingers > 0)
                return;
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                fingers = 1;
                position = mouse.position.ReadValue();
            }
        }
    }
}
