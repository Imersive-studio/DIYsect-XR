// Real mouse and touch input from the Input System's test fixture, read by a real PointerGestureTracker.
// Authority under test: none (local input only). A press is a quick tap, a hold or a drag, and never more than one of them.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class PointerGestureTrackerTests
    {
        private const float HoldSeconds = 0.4f;

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private Touchscreen _touch;
        private GameObject _holder;
        private PointerGestureTracker _tracker;
        private int _taps;
        private int _holds;

        [SetUp]
        public void Create()
        {
            _input.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _touch = InputSystem.AddDevice<Touchscreen>();
            _holder = new GameObject("Tracker");
            _holder.SetActive(false);
            _tracker = _holder.AddComponent<PointerGestureTracker>();
            typeof(PointerGestureTracker).GetField("_holdToPinSeconds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(_tracker, HoldSeconds);
            _holder.SetActive(true);
            _taps = 0;
            _holds = 0;
            _tracker.Tapped += _ => _taps++;
            _tracker.Held += _ => _holds++;
        }

        [TearDown]
        public void Destroy()
        {
            Object.Destroy(_holder);
            _input.TearDown();
        }

        [UnityTest]
        public IEnumerator AQuickClickIsATapAndNotAHold()
        {
            _input.Set(_mouse.position, new Vector2(200f, 200f));
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;

            Assert.AreEqual(1, _taps);
            Assert.AreEqual(0, _holds);
        }

        [UnityTest]
        public IEnumerator APressHeldStillIsAHoldRaisedWhileTheButtonIsStillDown_AndNotAlsoATap()
        {
            _input.Set(_mouse.position, new Vector2(200f, 200f));
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return new WaitForSeconds(HoldSeconds + 0.3f);

            Assert.AreEqual(1, _holds, "The hold is raised without waiting for the release.");
            Assert.AreEqual(0, _taps);

            _input.Release(_mouse.leftButton);
            yield return null;
            Assert.AreEqual(1, _holds, "A hold is raised once.");
            Assert.AreEqual(0, _taps, "Releasing after a hold is not a tap.");
        }

        [UnityTest]
        public IEnumerator ATouchHeldStillIsAHold()
        {
            _input.BeginTouch(1, new Vector2(300f, 300f));
            yield return new WaitForSeconds(HoldSeconds + 0.3f);
            _input.EndTouch(1, new Vector2(300f, 300f));
            yield return null;

            Assert.AreEqual(1, _holds);
            Assert.AreEqual(0, _taps);
        }

        [UnityTest]
        public IEnumerator ADragThatStartsAtOnceIsNeitherATapNorAHold()
        {
            _input.Set(_mouse.position, new Vector2(200f, 200f));
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return null;
            for (int i = 1; i <= 6; i++)
            {
                _input.Set(_mouse.position, new Vector2(200f + 30f * i, 200f));
                yield return null;
            }
            yield return new WaitForSeconds(HoldSeconds + 0.2f);
            _input.Release(_mouse.leftButton);
            yield return null;

            Assert.AreEqual(0, _taps);
            Assert.AreEqual(0, _holds);
        }

        [UnityTest]
        public IEnumerator AHoldThatThenMovesBecomesADragSoTheCameraCanOrbit()
        {
            _input.Set(_mouse.position, new Vector2(200f, 200f));
            yield return null;
            _input.Press(_mouse.leftButton);
            yield return new WaitForSeconds(HoldSeconds + 0.3f);
            Assert.AreEqual(1, _holds);

            _input.Set(_mouse.position, new Vector2(260f, 200f));
            yield return null;
            Assert.Greater(_tracker.FrameDrag.x, 1f, "Moving after the hold orbits.");
            Assert.IsTrue(_tracker.IsGesturing);
            _input.Release(_mouse.leftButton);
            yield return null;

            Assert.AreEqual(0, _taps);
            Assert.AreEqual(1, _holds);
        }

        [UnityTest]
        public IEnumerator ASecondFingerCancelsTheHold()
        {
            _input.BeginTouch(1, new Vector2(300f, 300f));
            yield return null;
            _input.BeginTouch(2, new Vector2(400f, 300f));
            yield return new WaitForSeconds(HoldSeconds + 0.3f);
            _input.EndTouch(1, new Vector2(300f, 300f));
            _input.EndTouch(2, new Vector2(400f, 300f));
            yield return null;

            Assert.AreEqual(0, _holds, "A pinch is never a hold.");
            Assert.AreEqual(0, _taps);
        }
    }
}
