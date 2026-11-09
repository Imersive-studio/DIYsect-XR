// Authority: none. Local input only; nothing here is sent over the network.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Reads the mouse and the touchscreen once per frame and turns them into simple things other scripts can use:
    /// a quick tap (<see cref="Tapped"/>), a press held still for about half a second (<see cref="Held"/>, raised while the
    /// finger is still down), this frame's one-finger or left-button drag (<see cref="FrameDrag"/>) and this frame's
    /// zoom (<see cref="FrameZoom"/>, from the scroll wheel or a two-finger pinch). A press that starts over a UI element is ignored.
    /// The orbit camera uses the drag and zoom, the organ name uses taps and the pin placer uses holds, so a drag can never also
    /// name an organ or place a pin, and a press raises Tapped or Held, never both.
    /// </summary>
    public class PointerGestureTracker : MonoBehaviour
    {
        // Windows reports a wheel notch as 120; other platforms report small numbers.
        // Windows reports 120 per mouse-wheel notch. macOS and Linux report about 1 per notch, and a trackpad sends
        // many small values in a row, so those are scaled down instead of divided.
        private const float WindowsWheelNotch = 120f;
        private const float NonWindowsScrollScale = 0.15f;
        // Caps one frame of wheel or trackpad zoom at one notch, so a fast flick cannot jump straight to a limit.
        private const float MaxWheelUnitsPerFrame = 1f;

        [SerializeField, Min(1f), Tooltip("A press that moves farther than this many pixels is a drag, not a tap.")]
        private float _dragPixels = 10f;
        [SerializeField, Min(0.1f), Tooltip("A press that stays put this many seconds is a hold (it places a question pin), not a tap.")]
        private float _holdToPinSeconds = 0.5f;
        [SerializeField, Min(1f), Tooltip("Pixels of pinch that count as one unit of zoom.")]
        private float _pinchPixelsPerUnit = 300f;

        private enum Source { None, Mouse, Touch }

        private TapDragClassifier _classifier;
        private Source _source;
        private bool _multiTouch;
        private bool _touchBlocked;
        private float _lastSpread;

        /// <summary>Raised on release when the press was a quick tap. The value is the screen position in pixels.</summary>
        public event Action<Vector2> Tapped;

        /// <summary>Raised once, when a press has stayed put long enough, without waiting for the finger to lift. The value is the screen position in pixels.</summary>
        public event Action<Vector2> Held;

        /// <summary>How far the pointer was dragged this frame, in pixels. Zero while the press may still be a tap.</summary>
        public Vector2 FrameDrag { get; private set; }

        /// <summary>Zoom this frame. Positive zooms in. One unit is about one wheel notch or <c>_pinchPixelsPerUnit</c> of pinch.</summary>
        public float FrameZoom { get; private set; }

        /// <summary>True while a press is being held that has already become a drag or pinch.</summary>
        public bool IsGesturing => _classifier != null && _classifier.Active && _classifier.IsDrag;

        private void Awake()
        {
            _classifier = new TapDragClassifier(_dragPixels, _holdToPinSeconds);
        }

        private void OnDisable()
        {
            _source = Source.None;
            _multiTouch = false;
            FrameDrag = Vector2.zero;
            FrameZoom = 0f;
            _classifier?.Cancel();
        }

        private void Update()
        {
            FrameDrag = Vector2.zero;
            FrameZoom = 0f;
            float now = Time.unscaledTime;
            Touchscreen touchscreen = Touchscreen.current;
            int touchCount = CountTouches(touchscreen);

            if (_source == Source.None)
                StartIfPressed(touchscreen, touchCount, now);

            if (_source == Source.Mouse)
                UpdateMouse(now);
            else if (_source == Source.Touch)
                UpdateTouch(touchscreen, touchCount, now);
            else
                ReadWheel();
        }

        private void StartIfPressed(Touchscreen touchscreen, int touchCount, float now)
        {
            if (touchscreen != null && touchCount == 0)
                _touchBlocked = false;
            if (touchscreen != null && touchCount > 0)
            {
                // A touch that began over UI stays ignored until every finger is lifted.
                TouchControl touch = FirstTouch(touchscreen);
                if (_touchBlocked || touch == null)
                    return;
                if (IsOverUi(touch.touchId.ReadValue()))
                {
                    _touchBlocked = true;
                    return;
                }
                _source = Source.Touch;
                _multiTouch = touchCount > 1;
                _lastSpread = -1f;
                _classifier.Begin(touch.position.ReadValue(), now);
                if (_multiTouch)
                    _classifier.MarkNotATap();
                return;
            }
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !IsOverUi(-1))
            {
                _source = Source.Mouse;
                _classifier.Begin(mouse.position.ReadValue(), now);
            }
        }

        private void UpdateMouse(float now)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
            {
                Vector2 position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
                Finish(position, now);
                return;
            }
            Vector2 current = mouse.position.ReadValue();
            FrameDrag = _classifier.Move(current, now);
            ReportHold(current);
            ReadWheel();
        }

        private void UpdateTouch(Touchscreen touchscreen, int touchCount, float now)
        {
            if (touchscreen == null || touchCount == 0)
            {
                Finish(LastTouchPosition(touchscreen), now);
                return;
            }
            if (touchCount >= 2)
            {
                _multiTouch = true;
                _classifier.MarkNotATap();
                UpdatePinch(touchscreen);
                return;
            }
            _lastSpread = -1f;
            TouchControl touch = FirstTouch(touchscreen);
            if (_multiTouch || touch == null)
                return; // after a pinch the remaining finger does nothing until everyone lets go
            Vector2 current = touch.position.ReadValue();
            FrameDrag = _classifier.Move(current, now);
            ReportHold(current);
        }

        private void ReportHold(Vector2 position)
        {
            if (_classifier.TakeHold())
                Held?.Invoke(position);
        }

        private void UpdatePinch(Touchscreen touchscreen)
        {
            Vector2 a = Vector2.zero;
            Vector2 b = Vector2.zero;
            int found = 0;
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.isInProgress)
                    continue;
                if (found == 0) a = touch.position.ReadValue();
                else if (found == 1) b = touch.position.ReadValue();
                found++;
                if (found == 2)
                    break;
            }
            float spread = Vector2.Distance(a, b);
            if (_lastSpread >= 0f)
                FrameZoom = (spread - _lastSpread) / _pinchPixelsPerUnit;
            _lastSpread = spread;
        }

        private void ReadWheel()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f) || IsOverUi(-1))
                return;
            bool windows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
            float units = windows ? scroll / WindowsWheelNotch : scroll * NonWindowsScrollScale;
            FrameZoom += Mathf.Clamp(units, -MaxWheelUnitsPerFrame, MaxWheelUnitsPerFrame);
        }

        private void Finish(Vector2 position, float now)
        {
            bool wasTap = _classifier.End(position, now);
            ReportHold(position); // a hold that was only noticed at release (a slow frame) still counts
            _source = Source.None;
            _multiTouch = false;
            _lastSpread = -1f;
            if (wasTap)
                Tapped?.Invoke(position);
        }

        private static Vector2 LastTouchPosition(Touchscreen touchscreen)
        {
            return touchscreen != null ? touchscreen.primaryTouch.position.ReadValue() : Vector2.zero;
        }

        private static int CountTouches(Touchscreen touchscreen)
        {
            if (touchscreen == null)
                return 0;
            int count = 0;
            foreach (TouchControl touch in touchscreen.touches)
                if (touch.isInProgress)
                    count++;
            return count;
        }

        private static TouchControl FirstTouch(Touchscreen touchscreen)
        {
            foreach (TouchControl touch in touchscreen.touches)
                if (touch.isInProgress)
                    return touch;
            return null;
        }

        // A press on a screen-space panel must not also move the camera or pin the organ behind it.
        // A touch is asked by its touch id (-1 means the mouse).
        private static bool IsOverUi(int touchId)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;
            return touchId >= 0 ? eventSystem.IsPointerOverGameObject(touchId) : eventSystem.IsPointerOverGameObject();
        }
    }
}
