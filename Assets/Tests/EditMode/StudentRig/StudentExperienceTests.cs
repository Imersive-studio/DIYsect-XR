using DIYsect.Editor;
using DIYsect.Interaction;
using DIYsect.Menu;
using DIYsect.MixedReality;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DIYsect.StudentRig.Tests
{
    /// <summary>
    /// Guards the parts of the headset experience that live in scenes and prefabs. A scene re-save or a missed editor
    /// step would silently remove them, and only a headset session would show it.
    /// </summary>
    public class StudentExperienceTests
    {
        private const string StudentScene = "Assets/Scenes/LectureHall_VR.unity";
        private const string ProfessorScene = "Assets/Scenes/Professor_MR.unity";
        private static readonly string[] ToolPrefabs =
        {
            "Assets/Prefabs/ScalpelPlaceholder.prefab",
            "Assets/Prefabs/ForcepsPlaceholder.prefab",
        };

        [TearDown]
        public void CloseScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TestCase(StudentScene)]
        [TestCase(ProfessorScene)]
        public void HeadsetSceneHasAQuitMenuTheRaysCanPress(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var menu = Object.FindFirstObjectByType<QuitMenu>(FindObjectsInactive.Include);
            Assert.IsNotNull(menu, "The student scene has no QuitMenu. Run Tools > DIYsect > Add Quit Menu.");
            Assert.AreEqual(QuitMenuState.Closed, menu.State, "The menu must start closed.");
            Assert.IsNotNull(menu.GetComponentInParent<Canvas>(true), "The menu must live on a canvas.");
            Assert.IsNotNull(Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include), "Buttons need an EventSystem.");
            Assert.IsNotNull(Object.FindFirstObjectByType<XRUIInputModule>(FindObjectsInactive.Include),
                "Controller rays and pinches cannot press buttons without an XRUIInputModule.");
        }

        [Test]
        public void ToolPrefabsHaveAGrabZoneAndHoverFeedback()
        {
            foreach (string path in ToolPrefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, $"{path} is missing.");
                var grab = prefab.GetComponent<XRGrabInteractable>();
                Assert.IsNotNull(grab, $"{path} has no XRGrabInteractable.");
                Transform zone = prefab.transform.Find(WidenInstrumentGrab.GrabZoneName);
                Assert.IsNotNull(zone, $"{path} has no grab zone. Run Tools > DIYsect > Widen Instrument Grab Zones.");
                Assert.IsTrue(zone.GetComponent<Collider>().isTrigger, $"{path}: the grab zone must be a trigger.");
                Assert.IsTrue(grab.colliders.Contains(zone.GetComponent<Collider>()), $"{path}: the grab zone is not listed on the interactable.");
                Assert.IsNotNull(prefab.GetComponent<ToolHoverFeedback>(), $"{path} has no hover feedback. Run Tools > DIYsect > Add Tool Hover Feedback.");
            }
        }

        [Test]
        public void TheProfessorsDepthHandleHasAGrabZone()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            var input = Object.FindFirstObjectByType<DepthControlInput>(FindObjectsInactive.Include);
            Assert.IsNotNull(input, "The professor scene has no depth handle.");
            Assert.IsNotNull(input.transform.Find(WidenInstrumentGrab.GrabZoneName), "The depth handle has no grab zone.");
        }

        [Test]
        public void StudentRigCalibratesItsEyeHeightOnTheHeadset()
        {
            EditorSceneManager.OpenScene(StudentScene, OpenSceneMode.Single);
            var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            var calibration = origin.GetComponent<EyeHeightCalibration>();
            Assert.IsNotNull(calibration, "Without it the eyes sit wherever the headset started. Run Tools > DIYsect > Configure Student Rig.");
            Assert.AreEqual(1.6f, calibration.EyeHeight, 0.001f);
        }

        [Test]
        public void TheDrapesAreSwitchedOffOnTheBody()
        {
            GameObject body = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AnatomyBody.prefab");
            Assert.IsNotNull(body, "The body prefab is missing.");
            Transform drapes = null;
            foreach (Transform t in body.GetComponentsInChildren<Transform>(true))
                if (t.name == "Drapes")
                    drapes = t;
            Assert.IsNotNull(drapes, "The body has no Drapes group.");
            Assert.IsFalse(drapes.gameObject.activeSelf, "The drapes tent over the half body and a loose piece floats in the air, so they stay off.");
        }

        [Test]
        public void ProfessorRigIsNotRecalibrated()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            Assert.IsNull(origin.GetComponent<EyeHeightCalibration>(), "The professor stands at a real table in passthrough and keeps the real floor.");
        }

        [TestCase(1.6f, 1.0f, 0.6f)]
        [TestCase(1.6f, 1.6f, 0f)]
        [TestCase(1.6f, 0f, 1.6f)]
        [TestCase(1.6f, 2.0f, -0.4f)]
        public void OffsetPutsTheEyesAtTheFixedHeight(float eyeHeight, float trackedHead, float expectedOffset)
        {
            Assert.AreEqual(expectedOffset, EyeHeightMath.OffsetFor(eyeHeight, trackedHead), 0.0001f);
        }

        [TestCase(1.6f, 1.0f, 1f, 0.5f)]
        [TestCase(1.6f, -1.0f, 1f, -0.5f)]
        [TestCase(1.6f, 0.3f, 1f, 0f)]
        [TestCase(1.6f, 1.0f, 0.5f, 0.25f)]
        public void ThumbstickMovesTheEyesAtASetSpeed(float eyeHeight, float stickY, float seconds, float expected)
        {
            Assert.AreEqual(expected, EyeHeightMath.AdjustmentFor(eyeHeight, stickY, seconds, 0.5f, 0.8f, 2.2f), 0.0001f);
        }

        [Test]
        public void ThumbstickNeverPassesTheLimits()
        {
            Assert.AreEqual(2.2f - 2.19f, EyeHeightMath.AdjustmentFor(2.19f, 1f, 10f, 0.5f, 0.8f, 2.2f), 0.0001f);
            Assert.AreEqual(0.8f - 0.81f, EyeHeightMath.AdjustmentFor(0.81f, -1f, 10f, 0.5f, 0.8f, 2.2f), 0.0001f);
        }

        [Test]
        public void OnlyRealHeightsAreUsable()
        {
            Assert.IsTrue(EyeHeightMath.IsUsable(1.2f));
            Assert.IsFalse(EyeHeightMath.IsUsable(float.NaN));
            Assert.IsFalse(EyeHeightMath.IsUsable(float.PositiveInfinity));
        }

        [TestCase(StudentScene)]
        [TestCase(ProfessorScene)]
        public void TheRigCameraDrawsEveryProjectLayer(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var origin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            Assert.IsNotNull(origin, $"{scenePath} has no rig.");
            foreach (string layerName in ShowAllLayersToRigCamera.LayerNames)
            {
                int layer = LayerMask.NameToLayer(layerName);
                Assert.GreaterOrEqual(layer, 0, $"The layer {layerName} does not exist.");
                Assert.IsTrue((origin.Camera.cullingMask & (1 << layer)) != 0,
                    $"{scenePath}: the headset camera does not draw layer {layerName}, so everything on it is invisible. Run Tools > DIYsect > Show All Layers To Rig Camera.");
            }
        }

        [TestCase(StudentScene)]
        [TestCase(ProfessorScene)]
        public void NearCastersCanSeeTriggerGrabZones(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var casters = Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNotEmpty(casters, $"{scenePath} has no near casters.");
            foreach (SphereInteractionCaster caster in casters)
                Assert.AreEqual(QueryTriggerInteraction.Collide, caster.physicsTriggerInteraction,
                    $"{caster.name} ignores triggers, so it would never find a grab zone.");
        }
    }
}
