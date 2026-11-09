// Loads the real scenes and checks the reconnect banner is wired in and does not sit on the Reset view button or the pin message.
// Authority under test: none (presentation only).
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using DIYsect.ClassmateConnection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMultiplayer;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DIYsect.Networking.Tests
{
    public class SharedSceneReconnectBannerTests
    {
        private const string SharedScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string DesktopScenePath = "Assets/Scenes/Classmate_Desktop.unity";

        private string _loaded;

        [TearDown]
        public void ClearRole()
        {
            ClassroomRole.OverrideForTests = null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            LogAssert.ignoreFailingMessages = true;
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            if (_loaded != null)
            {
                Scene scene = SceneManager.GetSceneByPath(_loaded);
                Scene empty = SceneManager.CreateScene("AfterReconnectBanner" + System.Guid.NewGuid());
                SceneManager.SetActiveScene(empty);
                if (scene.IsValid() && scene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(scene);
                _loaded = null;
            }
            yield return null;
        }

        private IEnumerator Load(string path, ClassroomRoleKind? role)
        {
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = role;
            _loaded = path;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            for (int i = 0; i < 5; i++)
                yield return null;
        }

        private T InScene<T>() where T : Component
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(_loaded).GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            Assert.Fail($"{_loaded} has no {typeof(T).Name}.");
            return null;
        }

        private static GameObject PanelOf(ReconnectBanner banner)
        {
            return (GameObject)typeof(ReconnectBanner).GetField("_panel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(banner);
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator TheSharedSceneHasTheBannerOnTheClassmateHalfOnly()
        {
            yield return Load(SharedScenePath, ClassroomRoleKind.Classmate);
            ReconnectBanner banner = InScene<ReconnectBanner>();
            Assert.IsTrue(banner.gameObject.activeInHierarchy, "A classmate must have the banner.");
            Assert.IsFalse(banner.IsShowing, "The banner must start hidden.");
            Assert.IsNotNull(banner.GetComponent<ClassmateRejoin>());
            yield return Unload();

            yield return Load(SharedScenePath, ClassroomRoleKind.Professor);
            Assert.IsFalse(InScene<ReconnectBanner>().gameObject.activeInHierarchy, "The professor's headset must not run the banner.");
        }

        [UnityTest]
        public IEnumerator TheClassmateDesktopSceneHasTheBanner()
        {
            yield return Load(DesktopScenePath, null);
            ReconnectBanner banner = InScene<ReconnectBanner>();
            Assert.IsTrue(banner.gameObject.activeInHierarchy);
            Assert.IsFalse(banner.IsShowing);
            Assert.IsNotNull(banner.GetComponent<ClassmateRejoin>());
        }

        [UnityTest]
        public IEnumerator TheBannerDoesNotCoverTheResetViewButtonOrThePinMessage_InBothScenes()
        {
            foreach ((string path, ClassroomRoleKind? role) in new[] { (SharedScenePath, (ClassroomRoleKind?)ClassroomRoleKind.Classmate), (DesktopScenePath, null) })
            {
                yield return Load(path, role);
                GameObject panel = PanelOf(InScene<ReconnectBanner>());
                panel.SetActive(true);
                Canvas.ForceUpdateCanvases();
                yield return null;
                Rect banner = ScreenRect((RectTransform)panel.transform);
                Rect reset = ScreenRect((RectTransform)InScene<ClassmateViewReset>().GetComponentInChildren<Button>().transform);
                var toastLabel = (Text)typeof(PinRejectionToast).GetField("_label", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(InScene<PinRejectionToast>());
                Rect toast = ScreenRect(toastLabel.rectTransform);
                Assert.IsFalse(banner.Overlaps(reset), $"{path}: the banner covers the Reset view button. Banner {banner}, button {reset}.");
                Assert.IsFalse(banner.Overlaps(toast), $"{path}: the banner covers the pin message. Banner {banner}, message {toast}.");
                yield return Unload();
            }
        }
    }
}
