// The professor and the classmates run one scene. These tests check that the role switch turns on the right half of it.
using System.Collections;
using System.Reflection;
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
            Assert.AreEqual(ClassroomRoleKind.Professor, ClassroomRole.Decide("", false, ClassroomRoleKind.Classmate));
            Assert.AreEqual(ClassroomRoleKind.Classmate, ClassroomRole.Decide("com.team.diysect.professor", true, ClassroomRoleKind.Classmate));
            Assert.AreEqual(ClassroomRoleKind.Professor, ClassroomRole.Decide("com.team.diysect.classmate", true, ClassroomRoleKind.Professor));
        }

        [Test]
        public void TheRoleSwitch_TurnsOnlyTheMatchingHalfOn_ForEachRole()
        {
            foreach (ClassroomRoleKind kind in new[] { ClassroomRoleKind.Professor, ClassroomRoleKind.Classmate })
            {
                var professorObject = new GameObject("professor-only");
                var classmateObject = new GameObject("classmate-only");
                var holder = new GameObject("holder");
                var professorScript = holder.AddComponent<ProfessorAutoHost>();
                var classmateScript = holder.AddComponent<ClassmateStatusLabel>();
                var switchObject = new GameObject("switch");
                switchObject.SetActive(false);
                var role = switchObject.AddComponent<ClassroomRole>();
                Set(role, "_editorRole", kind);
                Set(role, "_professorObjects", new[] { professorObject });
                Set(role, "_classmateObjects", new[] { classmateObject });
                Set(role, "_professorBehaviours", new Behaviour[] { professorScript });
                Set(role, "_classmateBehaviours", new Behaviour[] { classmateScript });

                switchObject.SetActive(true);

                bool professor = kind == ClassroomRoleKind.Professor;
                Assert.AreEqual(kind, ClassroomRole.Current);
                Assert.AreEqual(professor, professorObject.activeSelf, $"{kind}: professor object");
                Assert.AreEqual(!professor, classmateObject.activeSelf, $"{kind}: classmate object");
                Assert.AreEqual(professor, professorScript.enabled, $"{kind}: professor script");
                Assert.AreEqual(!professor, classmateScript.enabled, $"{kind}: classmate script");

                foreach (GameObject created in new[] { professorObject, classmateObject, holder, switchObject })
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
            foreach (string headsetOnly in new[] { "XR Origin (XR Rig)", "DepthHandle", "GuidePen", "StudentViewWindow", "ClassRoster", "TableAlignmentHandle", "TableAlignment" })
                Assert.IsFalse(FindObject(headsetOnly).activeInHierarchy, $"{headsetOnly} must be off for a classmate.");
            Assert.IsFalse(Object.FindFirstObjectByType<ProfessorAutoHost>(FindObjectsInactive.Include).enabled, "A classmate must not host.");
            Assert.IsTrue(Object.FindFirstObjectByType<ClassmateStatusLabel>(FindObjectsInactive.Include).enabled);

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
            foreach (string headsetOnly in new[] { "XR Origin (XR Rig)", "DepthHandle", "GuidePen", "StudentViewWindow", "ClassRoster", "TableAlignmentHandle", "TableAlignment" })
                Assert.IsTrue(FindObject(headsetOnly).activeInHierarchy, $"{headsetOnly} must be on for the professor.");
            Assert.IsTrue(Object.FindFirstObjectByType<ProfessorAutoHost>(FindObjectsInactive.Include).enabled, "The professor creates the room.");
            Assert.IsFalse(Object.FindFirstObjectByType<ClassmateStatusLabel>(FindObjectsInactive.Include).enabled);

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
        public IEnumerator TheProfessorCanWalkAndSnapTurn_AndBringTheTableBackWithAButton()
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
