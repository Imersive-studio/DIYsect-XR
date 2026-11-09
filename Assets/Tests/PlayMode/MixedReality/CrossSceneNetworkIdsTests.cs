// Loads the professor, student and classmate scenes one at a time and checks that they agree on the id of every NetworkObject.
using System.Collections;
using System.Collections.Generic;
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
    public class CrossSceneNetworkIdsTests
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [Test]
        public void HashOfPath_IsStableNeverZeroAndDifferentForDifferentPaths()
        {
            Assert.AreEqual(StableNetworkObjectIds.HashOfPath("DIYsect/AnatomyBody"), StableNetworkObjectIds.HashOfPath("DIYsect/AnatomyBody"));
            Assert.AreNotEqual(StableNetworkObjectIds.HashOfPath("DIYsect/AnatomyBody"), StableNetworkObjectIds.HashOfPath("DIYsect/AnatomyBody/ScalpelPlaceholder"));
            Assert.AreNotEqual(0u, StableNetworkObjectIds.HashOfPath(string.Empty));
        }

        [UnityTest]
        public IEnumerator EveryScene_GivesItsNetworkObjectsTheSameIdsAsTheOthers()
        {
            LogAssert.ignoreFailingMessages = true;
            var idsByScene = new List<Dictionary<string, uint>>();
            foreach (string path in ScenePaths)
            {
                var ids = new Dictionary<string, uint>();
                yield return Load(path);
                foreach (NetworkObject networkObject in Object.FindObjectsByType<NetworkObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string objectPath = StableNetworkObjectIds.PathOf(networkObject.transform);
                    uint id = StableNetworkObjectIds.GetId(networkObject);
                    Assert.AreEqual(StableNetworkObjectIds.HashOfPath(objectPath), id, $"{path}: {objectPath} does not have its path-based id.");
                    ids[objectPath] = id;
                }
                Assert.Greater(ids.Count, 0, $"{path} has no NetworkObjects.");
                idsByScene.Add(ids);
                yield return Unload(path);
            }

            for (int i = 1; i < idsByScene.Count; i++)
                CollectionAssert.AreEquivalent(idsByScene[0], idsByScene[i], $"{ScenePaths[0]} and {ScenePaths[i]} must have the same NetworkObjects with the same ids.");
        }

        [UnityTest]
        public IEnumerator TheNetworkManager_DoesNotSyncScenes_SoEachRoleKeepsItsOwnScene()
        {
            LogAssert.ignoreFailingMessages = true;
            foreach (string path in ScenePaths)
            {
                yield return Load(path);
                NetworkManager manager = Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
                Assert.IsNotNull(manager, $"{path} has no NetworkManager.");
                Assert.IsFalse(manager.NetworkConfig.EnableSceneManagement, $"{path}: scene syncing would move every client into the host's scene.");
                yield return Unload(path);
            }
        }

        private static IEnumerator Load(string path)
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            yield return null;
            yield return null;
        }

        private static IEnumerator Unload(string path)
        {
            foreach (NetworkManager manager in Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                Object.DestroyImmediate(manager.gameObject);
            }
            Scene loaded = SceneManager.GetSceneByPath(path);
            Scene empty = SceneManager.CreateScene("AfterCrossSceneIds");
            SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loaded);
            yield return null;
        }
    }
}
