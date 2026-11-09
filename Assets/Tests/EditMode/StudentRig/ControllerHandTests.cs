using System.Linq;
using DIYsect.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace DIYsect.StudentRig.Tests
{
    public class ControllerHandTests
    {
        private const string ProfessorScene = "Assets/Scenes/Professor_MR.unity";
        private readonly float[] _bends = new float[HandPoseRule.FingerCount];

        [Test]
        public void TriggerPinchesTheIndexAndGripClosesTheOtherFingers()
        {
            HandPoseRule.Bends(1f, 0f, _bends);
            Assert.AreEqual(1f, _bends[HandPoseRule.Index]);
            Assert.AreEqual(0f, _bends[HandPoseRule.Middle]);
            Assert.AreEqual(HandPoseRule.ThumbShare, _bends[HandPoseRule.Thumb], 0.0001f);

            HandPoseRule.Bends(0f, 1f, _bends);
            Assert.AreEqual(0f, _bends[HandPoseRule.Index], "Grip alone leaves the index pointing.");
            Assert.AreEqual(1f, _bends[HandPoseRule.Middle]);
            Assert.AreEqual(1f, _bends[HandPoseRule.Ring]);
            Assert.AreEqual(1f, _bends[HandPoseRule.Little]);
        }

        [Test]
        public void ReleasedButtonsGiveAnOpenHand()
        {
            HandPoseRule.Bends(0f, 0f, _bends);
            Assert.IsTrue(_bends.All(bend => bend == 0f));
        }

        [Test]
        public void BadInputCountsAsReleased()
        {
            HandPoseRule.Bends(float.NaN, 2f, _bends);
            Assert.AreEqual(0f, _bends[HandPoseRule.Index], "NaN must read as a released trigger.");
            Assert.AreEqual(1f, _bends[HandPoseRule.Middle], "Values above one are clamped.");
            Assert.AreEqual(HandPoseRule.FingerDegrees, HandPoseRule.Degrees(HandPoseRule.Middle, 5f), 0.0001f);
            Assert.AreEqual(HandPoseRule.ThumbDegrees, HandPoseRule.Degrees(HandPoseRule.Thumb, 1f), 0.0001f);
        }

        [Test]
        public void EachControllerShowsAHandInsteadOfTheControllerModel()
        {
            var scene = EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            XRInputModalityManager modality = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<XRInputModalityManager>(true)).Single();
            foreach (var (controller, side) in new[] { (modality.leftController, "Left"), (modality.rightController, "Right") })
            {
                Transform visual = controller.transform.Find($"{side} Controller Visual");
                Assert.IsNotNull(visual, $"{controller.name} has no controller model.");
                Assert.IsFalse(visual.gameObject.activeSelf, $"The {side.ToLowerInvariant()} controller model should be hidden. Run Tools > DIYsect > Add Controller Hands.");

                var hand = controller.GetComponentInChildren<ControllerHand>(true);
                Assert.IsNotNull(hand, $"The {side.ToLowerInvariant()} controller has no hand. Run Tools > DIYsect > Add Controller Hands.");
                Assert.IsTrue(hand.gameObject.activeSelf);
                Assert.IsTrue(hand.GetComponentsInChildren<SkinnedMeshRenderer>(false).Length > 0, $"The {side.ToLowerInvariant()} hand has no visible mesh.");

                var serialized = new SerializedObject(hand);
                Assert.IsNotNull(serialized.FindProperty("_trigger").objectReferenceValue, "The hand has no trigger button.");
                Assert.IsNotNull(serialized.FindProperty("_grip").objectReferenceValue, "The hand has no grip button.");
                SerializedProperty fingers = serialized.FindProperty("_fingers");
                Assert.AreEqual(HandPoseRule.FingerCount, fingers.arraySize);
                for (int f = 0; f < fingers.arraySize; f++)
                {
                    SerializedProperty joints = fingers.GetArrayElementAtIndex(f).FindPropertyRelative("Joints");
                    Assert.Greater(joints.arraySize, 0, $"Finger {f} has no joints.");
                    for (int j = 0; j < joints.arraySize; j++)
                        Assert.IsNotNull(joints.GetArrayElementAtIndex(j).objectReferenceValue, $"Finger {f} joint {j} is missing.");
                }
            }
        }
    }
}
