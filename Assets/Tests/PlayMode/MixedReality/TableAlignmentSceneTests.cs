// Loads the real Professor_MR scene and drives TableAlignment without a headset. No network session is started.
using System.Collections;
using System.Collections.Generic;
using DIYsect.Networking;
using DIYsect.MixedReality;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DIYsect.MixedReality.Tests
{
    public class TableAlignmentSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";

        private TableAlignment _alignment;
        private Transform _root;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            // The template's network manager logs an error when the project is not linked to Unity Cloud. That is unrelated
            // to the table alignment, so log errors are not failures here. The tests check TableAlignment's own state instead.
            LogAssert.ignoreFailingMessages = true;
            // Professor_MR is the shared scene; in the editor it starts as a classmate, which switches the alignment off.
            XRMultiplayer.ClassroomRole.OverrideForTests = XRMultiplayer.ClassroomRoleKind.Professor;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            _alignment = Object.FindFirstObjectByType<TableAlignment>();
            Assert.IsNotNull(_alignment, "Professor_MR has no TableAlignment. Run Tools > DIYsect > Add Table Alignment.");
            _root = GameObject.Find("DIYsect").transform;
        }

        // The loaded scene would stay behind for every test that runs after this one, template network manager included.
        [UnityTearDown]
        public IEnumerator UnloadScene()
        {
            LogAssert.ignoreFailingMessages = true;
            XRMultiplayer.ClassroomRole.OverrideForTests = null;
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene professor = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterTableAlignmentTests");
            SceneManager.SetActiveScene(empty);
            if (professor.IsValid() && professor.isLoaded)
                yield return SceneManager.UnloadSceneAsync(professor);
            yield return null;
        }

        // Destroying the template's objects logs errors of its own after the test body, so checking resumes only once the fixture is done.
        [OneTimeTearDown]
        public void RestoreLogChecks()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator TableAlignmentValidatesInTheRealScene()
        {
            Assert.IsTrue(_alignment.enabled, "TableAlignment disabled itself, see the console error.");
            Assert.Greater(_alignment.TableTopLocal.y, 0.5f, "The table top should sit well above the root origin.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovingTheRootCarriesTheBodyAndKeepsItUpright()
        {
            Transform body = _root.GetComponentInChildren<SharedBody>(true).transform;
            Vector3 bodyLocalBefore = _root.InverseTransformPoint(body.position);

            Pose pose = TableAlignmentMath.RootPoseForHit(new Vector3(2f, 0.74f, 3f), _alignment.TableTopLocal, new Vector3(2f, 1.6f, 0f), _root.rotation);
            _alignment.ApplyRootPose(pose);
            yield return null;

            Assert.AreEqual(0f, Vector3.Distance(bodyLocalBefore, _root.InverseTransformPoint(body.position)), 0.001f, "The body must keep its place on the table.");
            Assert.AreEqual(0.74f, (_alignment.RootPose.position + _alignment.RootPose.rotation * _alignment.TableTopLocal).y, 0.001f);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.up, _root.up), 0.001f);
        }

        [UnityTest]
        public IEnumerator ReleasingTheHandleWithoutPlanesKeepsTheCarriedHeightAndHidesTheTable()
        {
            var handle = GameObject.Find("TableAlignmentHandle").GetComponent<XRGrabInteractable>();
            Renderer table = _root.Find("DissectionTable").GetComponentInChildren<Renderer>();
            Assert.IsTrue(table.enabled);

            Pose rootBefore = _alignment.RootPose;
            TableAlignment.Source source = TableAlignment.Source.None;
            _alignment.Placed += s => source = s;

            // Stand-in for a hand: select the handle through the interaction manager, move it, let go.
            yield return Carry(handle, new Vector3(0.4f, 0.1f, 0.3f), 20f);
            Pose placed = _alignment.RootPose;

            Assert.AreEqual(TableAlignment.Source.Handle, source);
            Assert.AreEqual(rootBefore.position.y, placed.position.y, 0.001f, "Carrying the handle must not change the root height when no plane is known.");
            Assert.Greater(Vector3.Distance(rootBefore.position, placed.position), 0.1f, "The root should have moved with the handle.");
            Assert.AreEqual(20f, Quaternion.Angle(rootBefore.rotation, placed.rotation), 2f, "The root should have turned with the handle.");
            Assert.Less(Vector3.Distance(handle.transform.position, _root.TransformPoint(_alignment.TableTopLocal)), 1.5f, "The handle should be parked beside the moved table.");
            Assert.IsFalse(table.enabled, "The virtual table should be hidden once placed.");
        }

        [UnityTest]
        public IEnumerator PhaseFollowsTheHandleFromDetectingToCarryingToPlaced()
        {
            var handle = GameObject.Find("TableAlignmentHandle").GetComponent<XRGrabInteractable>();
            var phases = new List<AlignmentPhase>();
            _alignment.PhaseChanged += phases.Add;
            Assert.AreEqual(AlignmentPhase.Detecting, _alignment.Phase, "Start should begin plane detection in the editor.");

            yield return Grab(handle);
            Assert.AreEqual(AlignmentPhase.Carrying, _alignment.Phase);

            Release(handle);
            yield return null;
            Assert.AreEqual(AlignmentPhase.Placed, _alignment.Phase);
            CollectionAssert.AreEqual(new[] { AlignmentPhase.Carrying, AlignmentPhase.Placed }, phases);
            Object.Destroy(_handObject);
        }

        // Planes cannot be faked here (ARPlane comes from the headset), so the detected table is simulated through PlaceOnTableAt,
        // the call PlaceOnDetectedTable makes once it has picked a plane.
        [UnityTest]
        public IEnumerator ADetectedTableWhileCarryingDoesNotPlaceOrHideTheTable()
        {
            var handle = GameObject.Find("TableAlignmentHandle").GetComponent<XRGrabInteractable>();
            Renderer table = _root.Find("DissectionTable").GetComponentInChildren<Renderer>();
            var sources = new List<TableAlignment.Source>();
            _alignment.Placed += sources.Add;

            yield return Grab(handle);
            yield return MoveHand(handle, new Vector3(0.4f, 0f, 0.3f), 0f);
            Pose carried = _alignment.RootPose;

            bool placed = _alignment.PlaceOnTableAt(new Vector3(5f, 0.74f, 5f));

            Assert.IsFalse(placed, "A detected table must not win over the carry.");
            Assert.AreEqual(0f, Vector3.Distance(carried.position, _alignment.RootPose.position), 0.001f, "The root must stay with the handle.");
            Assert.IsTrue(table.enabled, "The virtual table must stay visible while it is carried.");
            Assert.AreEqual(AlignmentPhase.Carrying, _alignment.Phase);
            Assert.AreEqual(TableAlignment.Source.None, _alignment.LastSource);
            Assert.IsEmpty(sources);

            Release(handle);
            yield return null;
            Assert.AreEqual(AlignmentPhase.Placed, _alignment.Phase);
            Assert.AreEqual(TableAlignment.Source.Handle, _alignment.LastSource, "Letting go places by the handle, not by the detected table.");
            Assert.AreEqual(new[] { TableAlignment.Source.Handle }, sources);
            Object.Destroy(_handObject);
        }

        [UnityTest]
        public IEnumerator DisablingMidCarryStopsFollowingAndKeepsTheTableVisible()
        {
            var handle = GameObject.Find("TableAlignmentHandle").GetComponent<XRGrabInteractable>();
            Renderer table = _root.Find("DissectionTable").GetComponentInChildren<Renderer>();
            var sources = new List<TableAlignment.Source>();
            _alignment.Placed += sources.Add;

            yield return Grab(handle);
            yield return MoveHand(handle, new Vector3(0.4f, 0f, 0.3f), 0f);
            Assert.AreEqual(AlignmentPhase.Carrying, _alignment.Phase);

            _alignment.enabled = false;
            Pose stopped = _alignment.RootPose;
            Assert.AreEqual(AlignmentPhase.Detecting, _alignment.Phase, "A cut short carry goes back to the phase before the grab.");
            Assert.IsTrue(table.enabled, "The virtual table must stay visible.");

            _handObject.transform.position += new Vector3(0.5f, 0f, 0f);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(0f, Vector3.Distance(stopped.position, _alignment.RootPose.position), 0.001f, "The root must stop following the handle.");

            // Letting go after the carry was cut short places nothing.
            Release(handle);
            yield return null;
            Assert.IsEmpty(sources);
            Assert.AreEqual(AlignmentPhase.Detecting, _alignment.Phase);
            Object.Destroy(_handObject);
        }

        // An interactor with no input device. XRBaseInteractor reports select as active, so a selection made through the manager lasts.
        private sealed class TestHand : XRBaseInteractor
        {
        }

        private GameObject _handObject;
        private TestHand _hand;

        private IEnumerator Grab(XRGrabInteractable handle)
        {
            var manager = handle.interactionManager;
            Assert.IsNotNull(manager, "The handle has no interaction manager.");
            _handObject = new GameObject("TestInteractor");
            _hand = _handObject.AddComponent<TestHand>();
            _handObject.transform.position = handle.transform.position;
            yield return null;

            manager.SelectEnter((IXRSelectInteractor)_hand, (IXRSelectInteractable)handle);
            yield return null;
            Assert.IsTrue(handle.isSelected, "The handle was not selected.");
        }

        private IEnumerator MoveHand(XRGrabInteractable handle, Vector3 move, float turnDegrees)
        {
            Vector3 before = handle.transform.position;
            _handObject.transform.position += move;
            _handObject.transform.rotation = Quaternion.Euler(0f, turnDegrees, 0f);
            // The grab eases the handle toward the hand, so give it time to arrive.
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(move.magnitude, Vector3.Distance(before, handle.transform.position), 0.05f, "The handle did not follow the hand.");
        }

        private void Release(XRGrabInteractable handle)
        {
            handle.interactionManager.SelectExit((IXRSelectInteractor)_hand, (IXRSelectInteractable)handle);
        }

        private IEnumerator Carry(XRGrabInteractable handle, Vector3 move, float turnDegrees)
        {
            yield return Grab(handle);
            yield return MoveHand(handle, move, turnDegrees);
            Release(handle);
            yield return null;
            Object.Destroy(_handObject);
        }
    }
}
