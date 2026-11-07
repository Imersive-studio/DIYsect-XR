// Checks the saved scenes: every room has one close-up camera that feeds the screen's texture and cannot see the screen.
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.CloseUp.Tests
{
    public class CloseUpSceneTests
    {
        private const string ScreenLayerName = "CloseUpScreen";
        private const string CameraName = "CloseUpCamera";
        private const string ScreenName = "CloseUpScreen";
        private const string BodyName = "AnatomyBody";
        // The torso is what the screen has to show. The drapes hang over the table edges and the instruments sit
        // beside the body, so neither is counted when judging where the body is in the picture.
        private const string TorsoPivotName = "Supine";
        private const string DrapesName = "Drapes";
        private const string LampName = "OperatingLight";
        private const string RenderTexturePath = "Assets/Models/Environment/CloseUp.renderTexture";

        // Every client role has a screen in its room: students (VR), the professor (MR) and the desktop classmate.
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [TearDown]
        public void CloseScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void EachSceneHasExactlyOneCameraRenderingToTheCloseUpTexture([ValueSource(nameof(ScenePaths))] string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            Assert.IsNotNull(texture, $"{RenderTexturePath} is missing.");
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            var writers = new List<Camera>();
            foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (camera.targetTexture == texture)
                    writers.Add(camera);

            Assert.AreEqual(1, writers.Count, $"{path} should have one camera rendering to the close-up texture.");
            Assert.AreEqual(CameraName, writers[0].name);
        }

        [Test]
        public void ScreenMaterialShowsTheSameTextureTheCameraRendersTo([ValueSource(nameof(ScenePaths))] string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Camera closeUp = FindCloseUpCamera(path);
            Renderer screen = FindScreenRenderer(path);

            Assert.IsNotNull(closeUp.targetTexture, $"{path}: the close-up camera has no target texture.");
            Assert.AreSame(closeUp.targetTexture, screen.sharedMaterial.mainTexture, $"{path}: the screen shows a different texture.");
        }

        [Test]
        public void CloseUpCameraCannotSeeTheScreen([ValueSource(nameof(ScenePaths))] string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Camera closeUp = FindCloseUpCamera(path);
            Renderer screen = FindScreenRenderer(path);

            int screenLayer = LayerMask.NameToLayer(ScreenLayerName);
            Assert.Greater(screenLayer, 0, $"The {ScreenLayerName} layer is not defined in the Tags and Layers settings.");
            Assert.AreEqual(screenLayer, screen.gameObject.layer, $"{path}: the screen is not on the {ScreenLayerName} layer.");
            Assert.AreEqual(0, closeUp.cullingMask & (1 << screenLayer), $"{path}: the close-up camera renders the screen layer, so the screen would show itself.");
            Assert.AreNotEqual(0, closeUp.cullingMask, $"{path}: the close-up camera renders nothing.");
        }

        [Test]
        public void CloseUpCameraLooksAtTheBodyAndIsNotNetworked([ValueSource(nameof(ScenePaths))] string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Camera closeUp = FindCloseUpCamera(path);

            Assert.IsNull(closeUp.GetComponentInParent<NetworkObject>(), $"{path}: the close-up camera must be local, not a NetworkObject.");

            GameObject body = GameObject.Find(BodyName);
            Assert.IsNotNull(body, $"{path} has no {BodyName}.");
            Transform torso = body.transform.Find(TorsoPivotName);
            Assert.IsNotNull(torso, $"{path}: the body has no {TorsoPivotName} pivot.");
            var renderers = new List<Renderer>();
            foreach (Renderer candidate in torso.GetComponentsInChildren<Renderer>())
                if (!IsUnder(candidate.transform, torso, DrapesName))
                    renderers.Add(candidate);
            Assert.IsNotEmpty(renderers, $"{path}: the torso has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer part in renderers)
                bounds.Encapsulate(part.bounds);

            Vector3 viewport = closeUp.WorldToViewportPoint(bounds.center);
            Assert.Greater(viewport.z, 0f, $"{path}: the body is behind the close-up camera.");
            Assert.That(viewport.x, Is.InRange(0.25f, 0.75f), $"{path}: the body is not centred horizontally in the close-up view.");
            Assert.That(viewport.y, Is.InRange(0.25f, 0.75f), $"{path}: the body is not centred vertically in the close-up view.");
        }

        [Test]
        public void CloseUpCameraDoesNotRenderTheOperatingLamp([ValueSource(nameof(ScenePaths))] string path)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Camera closeUp = FindCloseUpCamera(path);
            GameObject lamp = GameObject.Find(LampName);
            if (lamp == null)
                Assert.Pass($"{path} has no {LampName} (the professor's room is lit by the real room).");

            // The lens face is level with the close-up camera, so a rendered lamp would cover the whole picture.
            foreach (Renderer renderer in lamp.GetComponentsInChildren<Renderer>(true))
                Assert.AreEqual(0, closeUp.cullingMask & (1 << renderer.gameObject.layer),
                    $"{path}: the close-up camera renders {renderer.name} of the operating lamp, which hides the body.");
        }

        private static bool IsUnder(Transform part, Transform stop, string name)
        {
            for (Transform node = part; node != null && node != stop; node = node.parent)
                if (node.name == name)
                    return true;
            return false;
        }

        private static Camera FindCloseUpCamera(string path)
        {
            GameObject cameraObject = GameObject.Find(CameraName);
            Assert.IsNotNull(cameraObject, $"{path} has no {CameraName}.");
            return cameraObject.GetComponent<Camera>();
        }

        private static Renderer FindScreenRenderer(string path)
        {
            GameObject screen = GameObject.Find(ScreenName);
            Assert.IsNotNull(screen, $"{path} has no {ScreenName}.");
            Renderer renderer = screen.GetComponentInChildren<Renderer>();
            Assert.IsNotNull(renderer, $"{path}: the screen has no renderer.");
            return renderer;
        }
    }
}
