using DIYsect.Editor;
using DIYsect.Interaction;
using DIYsect.Menu;
using DIYsect.MixedReality;
using DIYsect.Networking;
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
        public void OrgansAreHeldWhereTheHandTouchesThem()
        {
            GameObject body = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/AnatomyBody.prefab");
            var grabs = body.GetComponentsInChildren<XRGrabInteractable>(true);
            Assert.AreEqual(22, grabs.Length, "The body should have 22 grabbable organs.");
            foreach (XRGrabInteractable grab in grabs)
                Assert.IsTrue(grab.useDynamicAttach,
                    $"{grab.name} jumps its pivot to the hand and floats away from it. Run Tools > DIYsect > Hold Organs Where Touched.");
        }

        [Test]
        public void RoomCodeBoardShowsTheCodeOnlyWhenThereIsARoom()
        {
            Assert.AreEqual(("KHC8QH", XRMultiplayer.RoomCodeBoardText.PhoneHint), XRMultiplayer.RoomCodeBoardText.For("khc8qh", true));
            Assert.AreEqual((XRMultiplayer.RoomCodeBoardText.NoCode, XRMultiplayer.RoomCodeBoardText.WaitingHint), XRMultiplayer.RoomCodeBoardText.For(null, false));
            Assert.AreEqual((XRMultiplayer.RoomCodeBoardText.NoCode, XRMultiplayer.RoomCodeBoardText.PracticeHint), XRMultiplayer.RoomCodeBoardText.For("", true));
        }

        [Test]
        public void TheProfessorsPanelsDoNotOverlap()
        {
            var scene = EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            Vector3 eye = ArrangeProfessorPanels.ProfessorEye(scene);
            var spans = new System.Collections.Generic.List<(string name, float left, float right, float bottom, float top)>();
            foreach (var slot in ArrangeProfessorPanels.Slots)
            {
                Transform panel = ArrangeProfessorPanels.Find(scene, slot.Name);
                if (panel == null)
                    continue;
                Bounds b = PanelBounds(panel);
                float distance = Vector2.Distance(new Vector2(eye.x, eye.z), new Vector2(b.center.x, b.center.z));
                float half = Mathf.Atan2(Mathf.Max(b.size.x, b.size.z) * 0.5f, distance) * Mathf.Rad2Deg;
                float bearing = ArrangeProfessorPanels.Bearing(eye, b.center);
                spans.Add((slot.Name, bearing - half, bearing + half, b.min.y, b.max.y));
            }
            Assert.GreaterOrEqual(spans.Count, 4, "The professor panels were not found.");
            for (int i = 0; i < spans.Count; i++)
                for (int j = i + 1; j < spans.Count; j++)
                {
                    var a = spans[i];
                    var c = spans[j];
                    bool sideBySide = a.left < c.right && c.left < a.right;
                    bool sameHeight = a.bottom < c.top && c.bottom < a.top;
                    Assert.IsFalse(sideBySide && sameHeight, $"{a.name} and {c.name} overlap in the professor's view. Run Tools > DIYsect > Arrange Professor Panels.");
                }
        }

        [Test]
        public void TheTemplatesLobbyMenuAndGridSlabAreOff()
        {
            var scene = EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            Transform ui = null, table = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "UI") ui = root.transform;
                if (root.name == "Virtual Table") table = root.transform;
            }
            Transform menu = ui != null ? ui.Find("World Space Canvas") : null;
            Assert.IsTrue(menu == null || !menu.gameObject.activeSelf, "The template lobby menu clutters the professor's view. Run Tools > DIYsect > Hide Template Clutter.");
            Transform grid = table != null ? table.Find("TableSystem/TableTop/Plane") : null;
            Assert.IsTrue(grid == null || !grid.GetComponent<Renderer>().enabled, "The template grid slab clutters the professor's view. Run Tools > DIYsect > Hide Template Clutter.");
        }

        [Test]
        public void OnlyTheStudentSeesTheChecklistCard()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            var role = Object.FindFirstObjectByType<XRMultiplayer.ClassroomRole>(FindObjectsInactive.Include);
            var students = new SerializedObject(role).FindProperty("_studentObjects");
            bool found = false;
            for (int i = 0; i < students.arraySize; i++)
            {
                var item = students.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (item != null && item.name == "Panel" && item.transform.parent != null && item.transform.parent.name == "DissectionChecklist")
                    found = true;
            }
            Assert.IsTrue(found, "The checklist card should show only to the student. Run Tools > DIYsect > Build Shared Scene.");
        }

        // A world space canvas has no renderer bounds, so it is measured from its rectangle.
        private static Bounds PanelBounds(Transform panel)
        {
            if (panel.TryGetComponent(out RectTransform rect) && panel.GetComponent<Canvas>() != null)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                var canvasBounds = new Bounds(corners[0], Vector3.zero);
                foreach (Vector3 corner in corners)
                    canvasBounds.Encapsulate(corner);
                return canvasBounds;
            }
            var renderers = panel.GetComponentsInChildren<Renderer>(true);
            var bounds = new Bounds(panel.position, Vector3.zero);
            foreach (Renderer r in renderers)
                if (r.enabled)
                    bounds.Encapsulate(r.bounds);
            return bounds;
        }

        [Test]
        public void SharedSceneHasARoomCodeBoardEveryoneCanSee()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            var board = Object.FindFirstObjectByType<XRMultiplayer.RoomCodeBoard>(FindObjectsInactive.Include);
            Assert.IsNotNull(board, "Without it nobody can read the room code. Run Tools > DIYsect > Add Room Code Board.");
            Assert.IsTrue(board.gameObject.activeSelf, "The board must be on for every role.");
            foreach (Transform t in board.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, t.gameObject.layer, $"{t.name} is on a layer the headset camera may not draw.");
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

        [TestCase(14f, 14f, false, true)]
        [TestCase(20f, 14f, false, true)]
        [TestCase(5f, 14f, false, false)]
        [TestCase(30f, 14f, true, false)]
        public void PracticeStartsOnlyWhenNoRoomWasJoinedInTime(float elapsed, float wait, bool networkRunning, bool expected)
        {
            Assert.AreEqual(expected, PracticeRules.ShouldStart(elapsed, wait, networkRunning));
        }

        [Test]
        public void StudentSceneCanPractiseAloneAndStopsLookingForARoomWhenItDoes()
        {
            EditorSceneManager.OpenScene(StudentScene, OpenSceneMode.Single);
            var practice = Object.FindFirstObjectByType<PracticeSession>(FindObjectsInactive.Include);
            Assert.IsNotNull(practice, "Without it nothing can be dissected alone. Run Tools > DIYsect > Add Practice Session.");
            var list = new SerializedObject(practice).FindProperty("_disableOnStart");
            Assert.AreEqual(1, list.arraySize);
            Assert.IsInstanceOf<XRMultiplayer.ClassmateAutoJoin>(list.GetArrayElementAtIndex(0).objectReferenceValue);
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
        public void HandsCanReachOrgansOnTheirOwnLayers(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int organs = LayerMask.GetMask("SeatedOrgan", "LooseOrgan");
            Assert.AreNotEqual(0, organs, "The organ layers are missing.");
            var casters = Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.IsNotEmpty(casters, $"{scenePath} has no near casters.");
            foreach (SphereInteractionCaster caster in casters)
                Assert.AreEqual(organs, caster.physicsLayerMask.value & organs,
                    $"{caster.transform.parent.name} cannot touch organs, so they cannot be lifted. Run Tools > DIYsect > Widen Instrument Grab Zones.");
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
