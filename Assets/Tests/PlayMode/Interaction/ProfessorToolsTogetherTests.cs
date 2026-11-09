// The professor's three tools used at the same time: the guide pen, the depth handle and the student view window.
// The professor is the remote client here and the host is the student holding the scalpel. Hands are direct
// interactors made in code, so no controller is needed. Each test uses two or three tools together and then checks
// that every tool still works on its own afterwards.
using System.Collections;
using DIYsect.Classroom;
using DIYsect.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction.Tests
{
    public class ProfessorToolsTogetherTests : ScalpelSessionTestBase
    {
        private GuideLine _serverLine;
        private GuideLine _professorLine;
        private GuideLinePen _pen;
        private XRGrabInteractable _penGrab;
        private DepthControlInput _depthInput;
        private XRGrabInteractable _handleGrab;
        private StudentViewWindow _window;
        private GameObject _panel;
        private QuestionBoard _serverBoard;
        private XRDirectInteractor _leftHand;
        private XRDirectInteractor _rightHand;

        [UnityTearDown]
        public IEnumerator DestroyWindowParts()
        {
            if (_window != null)
                Object.Destroy(_window.gameObject);
            if (_panel != null)
                Object.Destroy(_panel);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PenDrawsWhileTheOtherHandHoldsTheDepthHandle()
        {
            yield return StartTools();

            yield return HoldHandle(_leftHand);
            yield return HoldPenAndDraw(_rightHand);

            Assert.IsTrue(_pen.IsDrawing, "The pen must draw while the handle is held.");
            Assert.AreEqual(_clientManager.LocalClientId, _serverHand.Controller, "Drawing must not take the depth away from the professor.");
            Assert.AreEqual(GrabPhase.Granted, _depthInput.Phase, "The handle must stay granted while the pen draws.");

            yield return MoveHandleDown(0.1f);
            Assert.AreEqual(_clientManager.LocalClientId, _serverLine.Drawer, "Moving the handle must not end the stroke.");
            Assert.IsTrue(_pen.IsDrawing);
        }

        [UnityTest]
        public IEnumerator LettingGoOfOneToolLeavesTheOtherRunning()
        {
            yield return StartTools();
            yield return HoldHandle(_leftHand);
            yield return HoldPenAndDraw(_rightHand);

            _rightHand.EndManualInteraction();
            yield return WaitFor(() => _serverLine.Drawer == GuideLine.NoDrawer, "the stroke to end when the pen is let go");
            Assert.AreEqual(_clientManager.LocalClientId, _serverHand.Controller, "Putting the pen down must not release the depth.");
            Assert.AreEqual(GrabPhase.Granted, _depthInput.Phase);
            Assert.Greater(_serverLine.Count, 0, "The finished line must stay on the body.");

            yield return HoldPenAndDraw(_rightHand);
            _leftHand.EndManualInteraction();
            yield return WaitFor(() => _serverHand.Controller == GuidingHand.NoController, "the depth to be given back when the handle is let go");
            Assert.IsTrue(_pen.IsDrawing, "Letting go of the handle must not end the stroke.");
            Assert.AreEqual(_clientManager.LocalClientId, _serverLine.Drawer);
        }

        [UnityTest]
        public IEnumerator StudentViewWindowOpeningDuringBothToolsChangesNeitherOfThem()
        {
            yield return StartTools();
            yield return HoldHandle(_leftHand);
            yield return HoldPenAndDraw(_rightHand);
            Assert.AreEqual(StudentViewMode.Hidden, _window.Mode, "Setup: the window starts hidden.");

            // A classmate asks a question with a view, so the window opens on its own while the professor is busy.
            _serverBoard.RequestPin(0, PointOnOrgan(0), "Why here?", _serverBody.transform.TransformPoint(new Vector3(0f, 0.5f, -1f)), Quaternion.identity);
            yield return WaitFor(() => _panel.activeSelf, "the window to open");

            Assert.AreEqual(StudentViewMode.FollowingNewest, _window.Mode);
            Assert.IsTrue(_pen.IsDrawing, "Opening the window must not end the stroke.");
            Assert.AreEqual(_clientManager.LocalClientId, _serverLine.Drawer);
            Assert.AreEqual(_clientManager.LocalClientId, _serverHand.Controller, "Opening the window must not take the depth away.");
            Assert.AreEqual(GrabPhase.Granted, _depthInput.Phase);

            // And the other way: stepping the window does not touch the tools.
            _window.Next();
            _window.ShowNewest();
            Assert.IsTrue(_pen.IsDrawing);
            Assert.AreEqual(GrabPhase.Granted, _depthInput.Phase);
        }

        [UnityTest]
        public IEnumerator OneHandCannotHoldThePenAndTheHandleAtOnce()
        {
            yield return StartTools();
            yield return HoldPenAndDraw(_rightHand);

            // The hand that draws reaches for the depth handle: it stays on the pen and the depth stays with the student.
            _rightHand.StartManualInteraction((IXRSelectInteractable)_handleGrab);
            yield return new WaitForSeconds(0.3f);

            Assert.IsTrue(_penGrab.isSelected, "The hand must keep the pen.");
            Assert.IsFalse(_handleGrab.isSelected, "A hand holds one tool at a time.");
            Assert.IsTrue(_pen.IsDrawing, "The stroke goes on.");
            Assert.AreEqual(GuidingHand.NoController, _serverHand.Controller, "The depth must stay with the student.");
            Assert.AreEqual(GrabPhase.Idle, _depthInput.Phase);
        }

        [UnityTest]
        public IEnumerator EveryToolWorksAloneAfterBeingUsedTogether()
        {
            yield return StartTools();
            yield return HoldHandle(_leftHand);
            yield return HoldPenAndDraw(_rightHand);
            _serverBoard.RequestPin(0, PointOnOrgan(0), "Q", _serverBody.transform.TransformPoint(new Vector3(0f, 0.5f, -1f)), Quaternion.identity);
            yield return WaitFor(() => _panel.activeSelf, "the window to open");

            _rightHand.EndManualInteraction();
            _leftHand.EndManualInteraction();
            yield return WaitFor(() => _serverLine.Drawer == GuideLine.NoDrawer && _serverHand.Controller == GuidingHand.NoController,
                "both tools to be free");

            // Pen alone.
            yield return HoldPenAndDraw(_rightHand);
            Assert.IsTrue(_pen.IsDrawing, "The pen must draw again on its own.");
            _rightHand.EndManualInteraction();
            yield return WaitFor(() => _serverLine.Drawer == GuideLine.NoDrawer, "the second stroke to end");

            // Handle alone.
            yield return HoldHandle(_leftHand);
            Assert.AreEqual(_clientManager.LocalClientId, _serverHand.Controller, "The handle must work again on its own.");
            _leftHand.EndManualInteraction();
            yield return WaitFor(() => _serverHand.Controller == GuidingHand.NoController, "the depth to be given back");

            // Window alone.
            _window.ShowNewest();
            Assert.AreEqual(StudentViewMode.FollowingNewest, _window.Mode);
            Assert.IsTrue(_panel.activeSelf);
        }

        private IEnumerator StartTools()
        {
            yield return StartScalpelSession();
            _serverSteering.RequestSteer();
            yield return WaitFor(() => _serverSteering.Steerer == _host.LocalClientId, "the student to steer the scalpel");
            yield return WaitFor(() => _clientSteering.HasSteerer, "the professor to see the scalpel being held");

            _serverLine = _serverBody.GetComponent<GuideLine>();
            _professorLine = _clientBody.GetComponent<GuideLine>();
            _serverBoard = _serverBody.GetComponent<QuestionBoard>();
            yield return WaitFor(() => _professorLine.IsSpawned, "the professor's guide line to spawn");

            _leftHand = CreateHand("Left hand");
            _rightHand = CreateHand("Right hand");
            BuildHandle();
            BuildPen();
            // Each hand starts on its tool, so grabbing does not move the tool.
            _leftHand.transform.position = _handleGrab.transform.position;
            _rightHand.transform.position = _pen.transform.position;
            BuildWindow();
            yield return null;
        }

        // Built inactive so the serialized fields are set before Awake runs.
        private void BuildHandle()
        {
            GameObject handleObject = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            handleObject.SetActive(false);
            handleObject.transform.localScale = Vector3.one * 0.05f;
            handleObject.transform.position = new Vector3(2f, 1f, 2f);
            handleObject.AddComponent<Rigidbody>().isKinematic = true;
            _handleGrab = handleObject.AddComponent<XRGrabInteractable>();
            _depthInput = handleObject.AddComponent<DepthControlInput>();
            SetSerialized(_depthInput, "_hand", _clientHand);
            handleObject.SetActive(true);
        }

        private void BuildPen()
        {
            GameObject penObject = Track(new GameObject("Pen"));
            penObject.SetActive(false);
            Transform tip = new GameObject("Tip").transform;
            tip.SetParent(penObject.transform, false);
            penObject.transform.position = _clientBody.transform.TransformPoint(new Vector3(0f, 0.2f, 0f));
            penObject.AddComponent<Rigidbody>().isKinematic = true;
            _penGrab = penObject.AddComponent<XRGrabInteractable>();
            _pen = penObject.AddComponent<GuideLinePen>();
            SetSerialized(_pen, "_line", _professorLine);
            SetSerialized(_pen, "_tip", tip);
            penObject.SetActive(true);
        }

        private void BuildWindow()
        {
            GameObject windowObject = new GameObject("StudentViewWindowUnderTest");
            windowObject.SetActive(false);
            var cameraObject = new GameObject("ViewCamera");
            cameraObject.transform.SetParent(windowObject.transform, false);
            Camera viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.targetTexture = new RenderTexture(64, 64, 16);
            _panel = new GameObject("Panel");
            _window = windowObject.AddComponent<StudentViewWindow>();
            SetSerialized(_window, "_board", _clientBody.GetComponent<QuestionBoard>());
            SetSerialized(_window, "_viewCamera", viewCamera);
            SetSerialized(_window, "_panel", _panel);
            windowObject.SetActive(true);
        }

        private IEnumerator HoldHandle(XRDirectInteractor hand)
        {
            hand.StartManualInteraction((IXRSelectInteractable)_handleGrab);
            yield return WaitFor(() => _serverHand.Controller == _clientManager.LocalClientId && _depthInput.Phase == GrabPhase.Granted,
                "the professor to get control of the depth", () => $"Controller {_serverHand.Controller}, phase {_depthInput.Phase}.");
        }

        // Takes the pen, starts a stroke as the trigger would, and moves the tip until the server has stored points.
        private IEnumerator HoldPenAndDraw(XRDirectInteractor hand)
        {
            hand.StartManualInteraction((IXRSelectInteractable)_penGrab);
            _pen.BeginStroke();
            yield return WaitFor(() => _serverLine.Drawer == _clientManager.LocalClientId && _pen.IsDrawing, "the pen to be granted the stroke");
            Vector3 start = _rightHand.transform.position;
            int count = _serverLine.Count;
            for (int i = 0; i < 20 && _serverLine.Count < count + 3; i++)
            {
                // The held pen follows the hand, so the hand is what moves.
                _rightHand.transform.position = start + _clientBody.transform.right * (0.02f * (i + 1));
                yield return new WaitForSeconds(0.05f);
            }
            Assert.GreaterOrEqual(_serverLine.Count, count + 3, "The pen must store points while it draws.");
        }

        private IEnumerator MoveHandleDown(float meters)
        {
            float before = _serverHand.Depth;
            _leftHand.transform.position += Vector3.down * meters;
            yield return WaitFor(() => !Mathf.Approximately(_serverHand.Depth, before), "the server to receive the new depth",
                () => $"Depth {_serverHand.Depth}.");
        }

        private Vector3 PointOnOrgan(int organ)
        {
            Transform t = _serverBody.Organs[organ].transform;
            MeshFilter filter = t.GetComponentInChildren<MeshFilter>();
            return t.InverseTransformPoint(filter.transform.TransformPoint(filter.sharedMesh.bounds.center));
        }
    }
}
