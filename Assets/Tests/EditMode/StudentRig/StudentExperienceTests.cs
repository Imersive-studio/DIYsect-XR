using DIYsect.Editor;
using DIYsect.Interaction;
using DIYsect.Menu;
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

        [Test]
        public void StudentSceneHasAQuitMenuTheRaysCanPress()
        {
            EditorSceneManager.OpenScene(StudentScene, OpenSceneMode.Single);
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
