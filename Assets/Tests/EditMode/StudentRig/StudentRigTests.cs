using NUnit.Framework;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.StudentRig.Tests
{
    /// <summary>
    /// Guards the student's VR rig. The student scene gives every student one fixed standing eye height and lets them
    /// walk (left thumbstick) and snap-turn (right thumbstick). The professor's mixed reality scene keeps the
    /// template's tabletop rig, where the real room is the floor and nobody walks. Tools > DIYsect > Configure Student
    /// Rig builds this setup.
    /// </summary>
    public class StudentRigTests
    {
        private const string StudentScene = "Assets/Scenes/LectureHall_VR.unity";
        private const string ProfessorScene = "Assets/Scenes/Professor_MR.unity";

        [TearDown]
        public void CloseScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void StudentSceneKeepsOneFixedStandingEyeHeight()
        {
            XROrigin origin = Open(StudentScene);
            Assert.AreEqual(XROrigin.TrackingOriginMode.Device, origin.RequestedTrackingOriginMode,
                "Device mode puts the eyes at the camera offset, whatever the student's real height.");
            Assert.AreEqual(1.6f, origin.CameraYOffset, 0.001f, "The student's eyes must sit at 1.6 m.");
        }

        [Test]
        public void StudentSceneCanWalkAndSnapTurn()
        {
            XROrigin origin = Open(StudentScene);
            Transform move = origin.transform.Find("Locomotion/Move");
            Transform turn = origin.transform.Find("Locomotion/Turn");
            Assert.IsNotNull(move, "The rig has no Locomotion/Move.");
            Assert.IsNotNull(turn, "The rig has no Locomotion/Turn.");
            Assert.IsTrue(move.gameObject.activeSelf, "Walking is off.");
            Assert.IsTrue(turn.gameObject.activeSelf, "Turning is off.");
            Assert.IsTrue(EnabledByName(move, "DynamicMoveProvider"), "The thumbstick move provider is disabled.");
            Assert.IsTrue(EnabledByName(turn, "SnapTurnProvider"), "Snap turning is disabled.");
            Assert.IsFalse(EnabledByName(turn, "ContinuousTurnProvider"), "Smooth turning makes many people ill; use snap turning.");
        }

        [Test]
        public void ProfessorSceneKeepsTheTemplateTabletopRig()
        {
            XROrigin origin = Open(ProfessorScene);
            Transform move = origin.transform.Find("Locomotion/Move");
            Assert.IsNotNull(move, "The rig has no Locomotion/Move.");
            Assert.IsFalse(move.gameObject.activeSelf, "The professor stands at a real table and does not walk.");
        }

        private static XROrigin Open(string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var origin = Object.FindFirstObjectByType<XROrigin>();
            Assert.IsNotNull(origin, $"{path} has no XROrigin.");
            return origin;
        }

        // The providers live in the XR Interaction Toolkit sample assemblies, so they are matched by type name.
        private static bool EnabledByName(Transform owner, string typeName)
        {
            foreach (Behaviour behaviour in owner.GetComponents<Behaviour>())
                if (behaviour != null && behaviour.GetType().Name == typeName)
                    return behaviour.enabled;
            Assert.Fail($"{owner.name} has no {typeName}.");
            return false;
        }
    }
}
