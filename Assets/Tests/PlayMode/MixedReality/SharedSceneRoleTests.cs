// The professor and the classmates run one scene. These tests check that the role switch turns on the right half of it.
using System.Collections;
using System.Reflection;
using DIYsect.Learning;
using DIYsect.MixedReality;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using XRMultiplayer;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DIYsect.MixedReality.Tests
{
    public class SharedSceneRoleTests
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";

        [Test]
        public void Decide_UsesThePackageNameOnADevice_AndTheSettingInTheEditor()
        {
            Assert.AreEqual(ClassroomRoleKind.Classmate, ClassroomRole.Decide("com.team.diysect.classmate", false, ClassroomRoleKind.Professor));
            Assert.AreEqual(ClassroomRoleKind.Professor, ClassroomRole.Decide("com.team.diysect.professor", false, ClassroomRoleKind.Classmate));
            Assert.AreEqual(ClassroomRoleKind.Student, ClassroomRole.Decide("com.team.diysect.student", false, ClassroomRoleKind.Classmate));
            Assert.AreEqual(ClassroomRoleKind.Professor, ClassroomRole.Decide("", false, ClassroomRoleKind.Classmate));
            Assert.AreEqual(ClassroomRoleKind.Classmate, ClassroomRole.Decide("com.team.diysect.professor", true, ClassroomRoleKind.Classmate));
            Assert.AreEqual(ClassroomRoleKind.Professor, ClassroomRole.Decide("com.team.diysect.classmate", true, ClassroomRoleKind.Professor));
        }

        [Test]
        public void TheRoleSwitch_TurnsOnlyTheMatchingHalfOn_ForEachRole()
        {
            foreach (ClassroomRoleKind kind in new[] { ClassroomRoleKind.Professor, ClassroomRoleKind.Classmate, ClassroomRoleKind.Student })
            {
                var professorObject = new GameObject("professor-only");
                var classmateObject = new GameObject("classmate-only");
                var studentObject = new GameObject("student-only");
                var studentLight = studentObject.AddComponent<Light>();
                var holder = new GameObject("holder");
                var professorScript = holder.AddComponent<ProfessorAutoHost>();
                var classmateScript = holder.AddComponent<ClassmateStatusLabel>();
                var joiner = holder.AddComponent<ClassmateAutoJoin>();
                joiner.WaitForJoinPress = true;
                var switchObject = new GameObject("switch");
                switchObject.SetActive(false);
                var role = switchObject.AddComponent<ClassroomRole>();
                Set(role, "_editorRole", kind);
                Set(role, "_joiningBehaviours", new Behaviour[] { joiner });
                Set(role, "_studentObjects", new[] { studentObject });
                Set(role, "_studentBehaviours", new Behaviour[] { studentLight });
                Set(role, "_professorObjects", new[] { professorObject });
                Set(role, "_classmateObjects", new[] { classmateObject });
                Set(role, "_professorBehaviours", new Behaviour[] { professorScript });
                Set(role, "_classmateBehaviours", new Behaviour[] { classmateScript });

                switchObject.SetActive(true);

                bool professor = kind == ClassroomRoleKind.Professor;
                Assert.AreEqual(kind, ClassroomRole.Current);
                Assert.AreEqual(kind == ClassroomRoleKind.Student, studentObject.activeSelf);
                Assert.AreEqual(kind == ClassroomRoleKind.Student, studentLight.enabled);
                Assert.AreEqual(kind != ClassroomRoleKind.Student, joiner.WaitForJoinPress);
                Assert.AreEqual(professor, professorObject.activeSelf, $"{kind}: professor object");
                Assert.AreEqual(kind == ClassroomRoleKind.Classmate, classmateObject.activeSelf, $"{kind}: classmate object");
                Assert.AreEqual(professor, professorScript.enabled, $"{kind}: professor script");
                Assert.AreEqual(kind == ClassroomRoleKind.Classmate, classmateScript.enabled, $"{kind}: classmate script");

                foreach (GameObject created in new[] { professorObject, classmateObject, studentObject, holder, switchObject })
                    Object.DestroyImmediate(created);
            }
        }

        [UnityTest]
        public IEnumerator TheSharedScene_StartsAsAClassmateInTheEditor_WithOneCameraAndNoHeadsetParts()
        {
            LogAssert.ignoreFailingMessages = true;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;

            Assert.AreEqual(ClassroomRoleKind.Classmate, ClassroomRole.Current);
            Assert.IsTrue(FindObject("ClassmateCamera").activeInHierarchy, "The classmate camera must be on.");
            Assert.IsNotNull(FindObject("ClassmateCamera").GetComponent<QuizAnswerInput>(), "A classmate answers the quiz from the classmate camera.");
            foreach (string headsetOnly in new[] { "XR Origin (XR Rig)", "DepthHandle", "GuidePen", "StudentViewWindow", "ClassRoster", "TableAlignmentHandle", "TableAlignment", "QuizPanel" })
                Assert.IsFalse(FindObject(headsetOnly).activeInHierarchy, $"{headsetOnly} must be off for a classmate.");
            Assert.IsFalse(Object.FindFirstObjectByType<ProfessorAutoHost>(FindObjectsInactive.Include).enabled, "A classmate must not host.");
            Assert.IsTrue(Object.FindFirstObjectByType<ClassmateStatusLabel>(FindObjectsInactive.Include).enabled);
            Assert.IsTrue(FindObject("ResetViewButton").activeInHierarchy, "A classmate needs the Reset view button.");
            Assert.IsTrue(FindObject("LectureHall").activeInHierarchy, "A classmate sits in the same lecture hall as the student, so everyone sees one classroom.");
            Assert.IsFalse(Object.FindFirstObjectByType<DIYsect.Networking.PracticeSession>(FindObjectsInactive.Include).enabled, "A phone must never host a practice session.");

            int mainCameras = 0;
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (camera.enabled && camera.gameObject.activeInHierarchy && camera.CompareTag("MainCamera"))
                    mainCameras++;
            Assert.AreEqual(1, mainCameras, "Exactly one main camera may be on, or the pin ray and the picture disagree.");

            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterSharedSceneRole");
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSharedScene_StartsAsTheProfessorOnAHeadset_WithTheHeadsetPartsOnAndNoClassmateCamera()
        {
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = ClassroomRoleKind.Professor;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;

            Assert.AreEqual(ClassroomRoleKind.Professor, ClassroomRole.Current);
            Assert.IsFalse(FindObject("ClassmateCamera").activeInHierarchy, "The classmate camera must be off for the professor.");
            foreach (string headsetOnly in new[] { "XR Origin (XR Rig)", "DepthHandle", "GuidePen", "StudentViewWindow", "ClassRoster", "QuizPanel", "ProfessorViewToggle" })
                Assert.IsTrue(FindObject(headsetOnly).activeInHierarchy, $"{headsetOnly} must be on for the professor.");
            Assert.IsTrue(Object.FindFirstObjectByType<ProfessorAutoHost>(FindObjectsInactive.Include).enabled, "The professor creates the room.");
            Assert.IsFalse(Object.FindFirstObjectByType<ClassmateStatusLabel>(FindObjectsInactive.Include).enabled);
            Assert.IsFalse(FindObject("ResetViewButton").activeInHierarchy, "The Reset view button must be off for the professor.");
            Assert.IsTrue(FindObject("LectureHall").activeInHierarchy, "The professor starts in the classroom the students are in.");
            foreach (string mixedRealityOnly in new[] { "TableAlignmentHandle", "TableAlignment" })
                Assert.IsFalse(FindObject(mixedRealityOnly).activeInHierarchy, $"{mixedRealityOnly} places the table on a real table, so it is for mixed reality only.");
            Assert.IsFalse(Object.FindFirstObjectByType<DIYsect.Networking.PracticeSession>(FindObjectsInactive.Include).enabled, "The professor creates the room and never starts a practice session.");
            Assert.IsNotNull(FindObject("XR Origin (XR Rig)").GetComponent<DIYsect.MixedReality.EyeHeightCalibration>(), "In the classroom the professor stands at the student's fixed eye height.");
            foreach (UnityEngine.EventSystems.EventSystem events in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None))
                Assert.IsFalse(events.transform.IsChildOf(FindObject("ClassmateCamera").transform), "The classmate's EventSystem must be off for the professor.");

            ClassroomRole.OverrideForTests = null;
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterSharedSceneProfessor");
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheSharedStudentUsesTheVrRigAndJoinsWithoutProfessorOrPhoneControls()
        {
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = ClassroomRoleKind.Student;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;
            Assert.AreEqual(ClassroomRoleKind.Student, ClassroomRole.Current);
            Assert.IsTrue(FindObject("XR Origin (XR Rig)").activeInHierarchy);
            Assert.IsFalse(FindObject("ClassmateCamera").activeInHierarchy);
            foreach (string professorOnly in new[] { "DepthHandle", "GuidePen", "StudentViewWindow", "ClassRoster", "TableAlignmentHandle", "TableAlignment", "QuizPanel" })
                Assert.IsFalse(FindObject(professorOnly).activeInHierarchy, professorOnly);
            var host = Object.FindFirstObjectByType<ProfessorAutoHost>(FindObjectsInactive.Include);
            Assert.IsTrue(host.enabled, "A student with nobody to join opens the class's room so phones can join with its code.");
            Assert.IsFalse(host.GetComponent<ClassmateAutoJoin>().enabled, "The host logic does its own joining, so the join-only loop stays off.");
            Assert.IsFalse(Object.FindFirstObjectByType<ClassmateStatusLabel>(FindObjectsInactive.Include).enabled);
            var role = host.GetComponent<ClassroomRole>();
            var joins = (Behaviour[])typeof(ClassroomRole).GetField("_joiningBehaviours", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(role);
            CollectionAssert.Contains(joins, host.GetComponent<ClassmateAutoJoin>());
            // AutoJoin disables itself in the editor; on a device it stays enabled for both joining roles.
            var origin = FindObject("XR Origin (XR Rig)").GetComponent<Unity.XR.CoreUtils.XROrigin>();
            Assert.AreEqual(Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device, origin.RequestedTrackingOriginMode);
            Assert.AreEqual(1.6f, origin.CameraYOffset, 0.001f);
            Assert.AreEqual(CameraClearFlags.Skybox, origin.Camera.clearFlags);
            Assert.IsNotNull(origin.transform.Find("QuitMenu"), "The shared student must keep the quit menu.");
            Assert.IsTrue(FindObject("LectureHall").activeInHierarchy, "The student must stand in the lecture hall.");
            Assert.IsTrue(origin.GetComponent<DIYsect.MixedReality.EyeHeightCalibration>().enabled, "The student's eye height must be calibrated.");
            Assert.IsFalse(host.GetComponent<ClassmateAutoJoin>().WaitForJoinPress, "The student has no Join button, so it must not wait for one.");
            Assert.IsTrue(host.GetComponent<DIYsect.Networking.PracticeSession>().enabled, "The student practises alone when no room can be opened.");
            foreach (Behaviour provider in origin.transform.Find("Locomotion/Move").GetComponents<Behaviour>())
                Assert.IsTrue(provider.enabled);

            ClassroomRole.OverrideForTests = null;
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterSharedStudent");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        // A failed role assertion must not leave the scene's NetworkManager singleton behind for the next fixture.
        [UnityTearDown]
        public IEnumerator CleanUpAfterRoleTest()
        {
            ClassroomRole.OverrideForTests = null;
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                Scene empty = SceneManager.CreateScene("AfterRoleTest");
                SceneManager.SetActiveScene(empty);
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            yield return null;
        }

        [TearDown]
        public void ResetRoleOverride()
        {
            ClassroomRole.OverrideForTests = null;
            ClassroomRole.ProfessorViewOverrideForTests = null;
        }

        [UnityTest]
        public IEnumerator TheProfessorSwitchesBetweenTheClassroomAndMixedReality_AndOnlyTheProfessorCan()
        {
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = ClassroomRoleKind.Professor;
            ClassroomRole.ProfessorViewOverrideForTests = ProfessorViewMode.Classroom;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;

            var role = Object.FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include);
            var passthrough = (Behaviour)typeof(ClassroomRole).GetField("_passthrough", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(role);
            GameObject rig = FindObject("XR Origin (XR Rig)");
            var origin = rig.GetComponent<Unity.XR.CoreUtils.XROrigin>();

            Assert.AreEqual(ProfessorViewMode.Classroom, role.ProfessorView);
            Assert.IsTrue(FindObject("LectureHall").activeInHierarchy, "The classroom view shows the hall the students are in.");
            Assert.IsFalse(passthrough.enabled, "No passthrough in the classroom.");
            Assert.IsFalse(FindObject("TableAlignment").activeInHierarchy);
            Assert.AreEqual(Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Device, origin.RequestedTrackingOriginMode, "A fixed eye height, like the student.");
            Assert.AreEqual(1.6f, origin.CameraYOffset, 0.001f);
            Assert.AreEqual(CameraClearFlags.Skybox, origin.Camera.clearFlags);
            Assert.IsNotNull(rig.GetComponent<DIYsect.MixedReality.EyeHeightCalibration>());

            role.SetProfessorView(ProfessorViewMode.MixedReality);
            Assert.AreEqual(ProfessorViewMode.MixedReality, role.ProfessorView);
            Assert.IsFalse(FindObject("LectureHall").activeInHierarchy, "Mixed reality shows the real room, not the hall.");
            Assert.IsTrue(passthrough.enabled, "Mixed reality draws the real room behind the body.");
            Assert.IsTrue(FindObject("TableAlignment").activeInHierarchy, "Table placing is back.");
            Assert.IsTrue(FindObject("TableAlignmentHandle").activeInHierarchy);
            Assert.AreEqual(0f, origin.CameraYOffset, 0.001f, "The rig goes back to the real headset height, not the fixed 1.6 m.");
            Assert.AreEqual(CameraClearFlags.SolidColor, origin.Camera.clearFlags);
            Assert.IsFalse(rig.GetComponent<DIYsect.MixedReality.EyeHeightCalibration>().enabled, "The calibration is for the hall only.");

            role.SetProfessorView(ProfessorViewMode.Classroom);
            Assert.IsTrue(FindObject("LectureHall").activeInHierarchy, "Back in the classroom.");
            Assert.IsFalse(passthrough.enabled);
            Assert.IsFalse(FindObject("TableAlignment").activeInHierarchy);
            Assert.AreEqual(1.6f, origin.CameraYOffset, 0.001f);
            Assert.AreEqual(ProfessorViewMode.MixedReality, ProfessorViewRule.Other(ProfessorViewMode.Classroom));
        }

        [UnityTest]
        public IEnumerator AStudentCannotSwitchTheProfessorsView()
        {
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = ClassroomRoleKind.Student;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;

            var role = Object.FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include);
            role.SetProfessorView(ProfessorViewMode.MixedReality);

            Assert.IsTrue(FindObject("LectureHall").activeInHierarchy, "The student stays in the hall whatever the professor chose.");
            Assert.IsFalse(FindObject("TableAlignment").activeInHierarchy);
        }

        [UnityTest]
        public IEnumerator TheProfessorCanWalkAndSnapTurn_AndBringTheTableBackWithAButton()
        {
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = ClassroomRoleKind.Professor;
            // Placing the table on a real table belongs to the mixed reality view.
            ClassroomRole.ProfessorViewOverrideForTests = ProfessorViewMode.MixedReality;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;

            GameObject rig = FindObject("XR Origin (XR Rig)");
            Transform move = rig.transform.Find("Locomotion/Move");
            Transform turn = rig.transform.Find("Locomotion/Turn");
            Assert.IsNotNull(move, "The rig has no Locomotion/Move.");
            Assert.IsNotNull(turn, "The rig has no Locomotion/Turn.");
            Assert.IsTrue(move.gameObject.activeInHierarchy);
            foreach (Behaviour provider in move.GetComponents<Behaviour>())
                Assert.IsTrue(provider.enabled, $"{provider.GetType().Name} on Move must be on so the left thumbstick walks.");
            Assert.IsTrue(turn.gameObject.activeInHierarchy);
            foreach (Behaviour provider in turn.GetComponents<Behaviour>())
                Assert.AreEqual(provider.GetType().Name == "SnapTurnProvider", provider.enabled, $"{provider.GetType().Name} on Turn");

            TableAlignment alignment = Object.FindFirstObjectByType<TableAlignment>();
            TableRecenterInput recenter = alignment.GetComponent<TableRecenterInput>();
            Assert.IsNotNull(recenter, "TableAlignment needs the recenter button.");
            recenter.Recenter();
            Assert.AreEqual(TableAlignment.Source.InFrontOfViewer, alignment.LastSource);
            Assert.AreEqual(AlignmentPhase.Placed, alignment.Phase);

            Transform head = Camera.main != null ? Camera.main.transform : Object.FindFirstObjectByType<Camera>().transform;
            Vector3 top = alignment.RootPose.position + alignment.RootPose.rotation * alignment.TableTopLocal;
            Vector3 toTop = top - head.position;
            Assert.AreEqual(-0.7f, toTop.y, 0.05f, "The table top must be 0.7 m below the eyes.");
            Vector3 flat = new Vector3(toTop.x, 0f, toTop.z);
            Assert.AreEqual(0.9f, flat.magnitude, 0.05f, "The table top must be 0.9 m ahead.");

            ClassroomRole.OverrideForTests = null;
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterProfessorMove");
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
        }

        private static GameObject FindObject(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t.gameObject;
            Assert.Fail($"The shared scene has no {name}.");
            return null;
        }

        private static void Set(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{target.GetType().Name} has no {field} field.");
            info.SetValue(target, value);
        }
    }
}
