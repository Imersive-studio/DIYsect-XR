// A classmate camera in an empty scene with a fake attitude sensor (the Input System's test devices, not a real phone).
// Authority under test: none (local camera only). Nothing here needs the network.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class ClassmateGyroTests
    {
        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private AttitudeSensor _sensor;
        private GameObject _target;
        private GameObject _cameraObject;
        private ClassmateOrbitCamera _orbit;

        [SetUp]
        public void SetUpInput()
        {
            _input.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [TearDown]
        public void TearDownInput()
        {
            if (_cameraObject != null)
                Object.Destroy(_cameraObject);
            if (_target != null)
                Object.Destroy(_target);
            _input.TearDown();
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private IEnumerator StartCamera(bool withSensor)
        {
            if (withSensor)
            {
                _sensor = InputSystem.AddDevice<AttitudeSensor>();
                _input.Set(_sensor.attitude, Quaternion.Euler(0f, 30f, 0f));
            }
            _target = new GameObject("Body");
            _cameraObject = new GameObject("TestClassmateCamera");
            _cameraObject.SetActive(false);
            _cameraObject.AddComponent<Camera>();
            _cameraObject.transform.position = ClassmateOpeningView.Offset();
            var tracker = _cameraObject.AddComponent<PointerGestureTracker>();
            _orbit = _cameraObject.AddComponent<ClassmateOrbitCamera>();
            Set(_orbit, "_gestures", tracker);
            Set(_orbit, "_target", _target.transform);
            _cameraObject.SetActive(true);
            for (int i = 0; i < 5; i++)
                yield return null;
            _orbit.SnapToGoal();
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSeconds(1.2f);
        }

        // The rotation the orbit alone would give, without the tilt.
        private Quaternion OrbitOnlyRotation()
        {
            OrbitMath.Pose(_target.transform.position, _orbit.Yaw, _orbit.Pitch, _orbit.Distance, out _, out Quaternion rotation);
            return rotation;
        }

        [UnityTest]
        public IEnumerator WithoutASensorTheViewIsJustTheOrbit()
        {
            yield return StartCamera(false);
            yield return Settle();
            Assert.AreEqual(Vector2.zero, _orbit.GyroOffset);
            Assert.Less(Quaternion.Angle(OrbitOnlyRotation(), _orbit.transform.rotation), 0.01f);
        }

        [UnityTest]
        public IEnumerator HoldingThePhoneStillOrJitteringInsideTheDeadZoneChangesNothing()
        {
            yield return StartCamera(true);
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 31f, 0f)); // one degree
            yield return Settle();
            Assert.AreEqual(0f, _orbit.GyroOffset.magnitude, 0.001f);
        }

        [UnityTest]
        public IEnumerator TiltingThePhoneTurnsTheViewByTheTiltAndTheCameraStaysWhereItIs()
        {
            yield return StartCamera(true);
            Vector3 position = _orbit.transform.position;
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 40f, 0f)); // 10 degrees, 1.5 of them dead zone
            yield return Settle();

            Assert.AreEqual(8.5f, _orbit.GyroOffset.x, 0.1f);
            Assert.AreEqual(0f, _orbit.GyroOffset.y, 0.1f);
            Assert.AreEqual(8.5f, Quaternion.Angle(OrbitOnlyRotation(), _orbit.transform.rotation), 0.2f);
            Assert.Less(Vector3.Distance(position, _orbit.transform.position), 0.001f, "The view turns on the spot; the camera does not move.");
        }

        [UnityTest]
        public IEnumerator ATiltFarPastTheLimitStopsAtTheLimit()
        {
            yield return StartCamera(true);
            _input.Set(_sensor.attitude, Quaternion.Euler(60f, 120f, 0f));
            yield return Settle();
            Assert.AreEqual(GyroLookRule.DefaultMaxYaw, _orbit.GyroOffset.x, 0.1f);
            Assert.AreEqual(GyroLookRule.DefaultMaxPitch, Mathf.Abs(_orbit.GyroOffset.y), 0.1f);
        }

        [UnityTest]
        public IEnumerator DraggingRecentresTheViewAndThePhoneNowMeasuresFromThere()
        {
            yield return StartCamera(true);
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 45f, 0f));
            yield return Settle();
            Assert.Greater(_orbit.GyroOffset.x, 5f);

            _input.Set(_mouse.position, new Vector2(100f, 100f));
            yield return null;
            _input.Press(_mouse.leftButton);
            for (int i = 1; i <= 6; i++)
            {
                _input.Set(_mouse.position, new Vector2(100f + 30f * i, 100f));
                yield return null;
            }
            _input.Release(_mouse.leftButton);
            yield return Settle();

            Assert.AreEqual(0f, _orbit.GyroOffset.magnitude, 0.05f, "After a drag the current tilt is the new zero.");
        }

        [UnityTest]
        public IEnumerator ResettingTheViewRecentresTheTilt()
        {
            yield return StartCamera(true);
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 45f, 0f));
            yield return Settle();
            Assert.Greater(_orbit.GyroOffset.x, 5f);

            _orbit.ResetView();
            yield return Settle();
            Assert.AreEqual(0f, _orbit.GyroOffset.magnitude, 0.05f);
        }

        [UnityTest]
        public IEnumerator WhileTheCameraIsDrivenThereIsNoOffsetAndTheTransformIsLeftAlone()
        {
            yield return StartCamera(true);
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 45f, 0f));
            yield return Settle();
            Assert.Greater(_orbit.GyroOffset.x, 5f);

            _orbit.Driven = true; // what the follow camera does
            Pose placed = new Pose(new Vector3(5f, 5f, 5f), Quaternion.Euler(10f, 20f, 30f));
            _orbit.transform.SetPositionAndRotation(placed.position, placed.rotation);
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 70f, 0f));
            yield return Settle();

            Assert.AreEqual(0f, _orbit.GyroOffset.magnitude, 0.05f, "No tilt offset while following.");
            Assert.Less(Quaternion.Angle(placed.rotation, _orbit.transform.rotation), 0.001f, "The orbit camera must not touch the transform while driven.");

            _orbit.Driven = false;
            yield return Settle();
            Assert.AreEqual(0f, _orbit.GyroOffset.magnitude, 0.05f, "Coming back from following starts from the current tilt, with no jump.");
        }

        [UnityTest]
        public IEnumerator SwitchingTheTiltLookOffGivesBackThePlainOrbit()
        {
            yield return StartCamera(true);
            _input.Set(_sensor.attitude, Quaternion.Euler(0f, 45f, 0f));
            yield return Settle();
            _orbit.GyroLook = false;
            yield return Settle();
            Assert.AreEqual(Vector2.zero, _orbit.GyroOffset);
        }
    }
}
