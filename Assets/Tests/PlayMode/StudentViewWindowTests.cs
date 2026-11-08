// Starts a real in-process Netcode host over UnityTransport on localhost (no cloud services).
// Authority under test: none. The window only reads the server-owned pins and moves its own camera to the stored view.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class StudentViewWindowTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 0;
        private const float PositionTolerance = 0.001f;
        private const float AngleTolerance = 0.5f;

        private static readonly Vector3 LocalPositionA = new Vector3(0.3f, 0.6f, -1.2f);
        private static readonly Vector3 LocalPositionB = new Vector3(-0.5f, 0.7f, 1.0f);

        private QuestionBoard _server;
        private StudentViewWindow _window;
        private GameObject _windowObject;
        private GameObject _panel;
        private Camera _camera;
        private RenderTexture _texture;

        [UnityTearDown]
        public IEnumerator DestroyWindow()
        {
            if (_windowObject != null)
                Object.Destroy(_windowObject);
            if (_panel != null)
                Object.Destroy(_panel);
            if (_texture != null)
            {
                _texture.Release();
                Object.Destroy(_texture);
            }
            yield return null;
        }

        private IEnumerator StartHostWithWindow()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuestionBoard>();
            Assert.IsNotNull(_server, "The body prefab has no QuestionBoard. Run Tools > DIYsect > Add Question Pins.");

            _windowObject = new GameObject("StudentViewWindowUnderTest");
            _windowObject.SetActive(false);
            var cameraObject = new GameObject("ViewCamera");
            cameraObject.transform.SetParent(_windowObject.transform, false);
            _camera = cameraObject.AddComponent<Camera>();
            _texture = new RenderTexture(64, 64, 16);
            _camera.targetTexture = _texture;
            _panel = new GameObject("Panel");

            _window = _windowObject.AddComponent<StudentViewWindow>();
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(StudentViewWindow).GetField("_board", flags).SetValue(_window, _server);
            typeof(StudentViewWindow).GetField("_viewCamera", flags).SetValue(_window, _camera);
            typeof(StudentViewWindow).GetField("_panel", flags).SetValue(_window, _panel);
            _windowObject.SetActive(true);
        }

        private Vector3 PointOn(int organ)
        {
            Transform t = _serverBody.Organs[organ].transform;
            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            return t.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
        }

        // A camera pose in world space at the given body-local position, looking at the body root.
        private Pose WorldPoseLookingAtBody(Vector3 localPosition)
        {
            Vector3 position = _serverBody.transform.TransformPoint(localPosition);
            Quaternion rotation = Quaternion.LookRotation(_serverBody.transform.position - position, Vector3.up);
            return new Pose(position, rotation);
        }

        private void RequestWithView(string text, Pose pose)
        {
            _server.RequestPin(OrganIndex, PointOn(OrganIndex), text, pose.position, pose.rotation);
        }

        private IEnumerator WaitForPins(int count)
        {
            yield return WaitFor(() => _server.Count == count, $"{count} pin(s) on the server", () => $"Has {_server.Count}.");
        }

        private void AssertCameraAt(Pose pose, string who)
        {
            Assert.Less((_camera.transform.position - pose.position).magnitude, PositionTolerance,
                $"{who}: camera at {_camera.transform.position.ToString("F4")}, want {pose.position.ToString("F4")}.");
            Assert.Less(Quaternion.Angle(_camera.transform.rotation, pose.rotation), AngleTolerance, $"{who}: camera rotation differs.");
        }

        [UnityTest]
        public IEnumerator NoPinsHidesThePanel()
        {
            yield return StartHostWithWindow();
            Assert.IsFalse(_panel.activeSelf);
            Assert.AreEqual(-1, _window.SelectedId);
            Assert.IsFalse(_camera.enabled, "The window camera renders on demand only.");
        }

        [UnityTest]
        public IEnumerator PinWithAViewShowsThePanelAndMovesTheCamera()
        {
            yield return StartHostWithWindow();
            Pose pose = WorldPoseLookingAtBody(LocalPositionA);
            RequestWithView("Q1", pose);
            yield return WaitForPins(1);
            yield return WaitFor(() => _panel.activeSelf, "the panel to show");

            Assert.AreEqual(_server[0].Id, _window.SelectedId);
            AssertCameraAt(pose, "single pin");
        }

        [UnityTest]
        public IEnumerator PinWithoutAViewDoesNotShow()
        {
            yield return StartHostWithWindow();
            _server.RequestPin(OrganIndex, PointOn(OrganIndex), "Q1");
            yield return WaitForPins(1);
            yield return null;
            yield return null;

            Assert.IsFalse(_server[0].HasView);
            Assert.IsFalse(_panel.activeSelf);
            Assert.AreEqual(-1, _window.SelectedId);
        }

        [UnityTest]
        public IEnumerator FollowsTheNewestAndStepsWithPreviousAndNext()
        {
            yield return StartHostWithWindow();
            Pose older = WorldPoseLookingAtBody(LocalPositionA);
            Pose newer = WorldPoseLookingAtBody(LocalPositionB);
            RequestWithView("Q1", older);
            yield return WaitForPins(1);
            RequestWithView("Q2", newer);
            yield return WaitForPins(2);
            int olderId = _server[0].Id;
            int newerId = _server[1].Id;

            yield return WaitFor(() => _window.SelectedId == newerId, "the window to follow the newest pin", () => $"Shows {_window.SelectedId}.");
            AssertCameraAt(newer, "newest");

            _window.Previous();
            Assert.AreEqual(olderId, _window.SelectedId);
            AssertCameraAt(older, "previous");

            _window.Next();
            Assert.AreEqual(newerId, _window.SelectedId);
            AssertCameraAt(newer, "next");
        }

        [UnityTest]
        public IEnumerator ViewFollowsTheBodyWhenItMoves()
        {
            yield return StartHostWithWindow();
            Pose pose = WorldPoseLookingAtBody(LocalPositionA);
            RequestWithView("Q1", pose);
            yield return WaitForPins(1);
            yield return WaitFor(() => _panel.activeSelf, "the panel to show");

            Transform body = _serverBody.transform;
            Vector3 localPosition = body.InverseTransformPoint(pose.position);
            Quaternion localRotation = Quaternion.Inverse(body.rotation) * pose.rotation;
            body.position += new Vector3(2f, 0f, -1f);
            body.rotation *= Quaternion.Euler(0f, 35f, 0f);
            _window.ShowNewest();

            var moved = new Pose(body.TransformPoint(localPosition), body.rotation * localRotation);
            AssertCameraAt(moved, "after the body moved");
        }

        [UnityTest]
        public IEnumerator RemovingTheShownPinFallsBackThenHides()
        {
            yield return StartHostWithWindow();
            Pose older = WorldPoseLookingAtBody(LocalPositionA);
            Pose newer = WorldPoseLookingAtBody(LocalPositionB);
            RequestWithView("Q1", older);
            yield return WaitForPins(1);
            RequestWithView("Q2", newer);
            yield return WaitForPins(2);
            int olderId = _server[0].Id;
            int newerId = _server[1].Id;
            yield return WaitFor(() => _window.SelectedId == newerId, "the window to follow the newest pin");

            _server.RequestRemove(newerId);
            yield return WaitForPins(1);
            yield return WaitFor(() => _window.SelectedId == olderId, "the window to fall back to the older pin", () => $"Shows {_window.SelectedId}.");
            Assert.IsTrue(_panel.activeSelf);
            AssertCameraAt(older, "fallback");

            _server.RequestRemove(olderId);
            yield return WaitForPins(0);
            yield return WaitFor(() => !_panel.activeSelf, "the panel to hide");
            Assert.AreEqual(-1, _window.SelectedId);
        }
    }
}
