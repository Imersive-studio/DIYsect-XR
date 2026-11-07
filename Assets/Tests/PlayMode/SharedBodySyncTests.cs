// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server moves the organs, the clients only receive them.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DIYsect.Networking.Tests
{
    /// <summary>Editor only: the body prefab is loaded from the project with AssetDatabase.</summary>
    public class SharedBodySyncTests
    {
        private const string BodyPrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const ushort Port = 7788;
        private const float TimeoutSeconds = 8f;
        private const float PositionTolerance = 0.005f;
        private const float RotationToleranceDegrees = 0.5f;

        // Headless runs would otherwise render thousands of frames per second, and interpolation needs realistic frame times.
        private const int TestFrameRate = 60;

        private GameObject _bodyPrefab;
        private int _previousFrameRate;
        private NetworkManager _host;
        private readonly List<NetworkManager> _clients = new List<NetworkManager>();
        private SharedBody _serverBody;

        [SetUp]
        public void SetUp()
        {
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = TestFrameRate;
            _bodyPrefab = LoadBodyPrefab();
            Assert.IsNotNull(_bodyPrefab, $"{BodyPrefabPath} is missing.");
            Assert.IsNotNull(_bodyPrefab.GetComponent<NetworkObject>(), "The body prefab has no NetworkObject. Run Tools > DIYsect > Add Body Networking.");
        }

        [TearDown]
        public void TearDown()
        {
            Application.targetFrameRate = _previousFrameRate;
            foreach (NetworkManager client in _clients)
                DestroyManager(client);
            _clients.Clear();
            DestroyManager(_host);
            _host = null;
            if (_serverBody != null)
                UnityEngine.Object.Destroy(_serverBody.gameObject);
        }

        [UnityTest]
        public IEnumerator TwoClientsSeeTheSameOrgansAfterMovement()
        {
            yield return StartHostAndSpawnBody();
            NetworkManager clientA = StartClient("ClientA");
            NetworkManager clientB = StartClient("ClientB");
            SharedBody bodyA = null;
            SharedBody bodyB = null;
            yield return WaitForBody(clientA, body => bodyA = body);
            yield return WaitForBody(clientB, body => bodyB = body);

            yield return WaitForSamePoses(_serverBody, bodyA, "client A initial pose");
            yield return WaitForSamePoses(_serverBody, bodyB, "client B initial pose");

            MoveOrgans(_serverBody, step: 1);
            yield return WaitForSamePoses(_serverBody, bodyA, "client A after movement");
            yield return WaitForSamePoses(_serverBody, bodyB, "client B after movement");
            AssertOrgansMoved(bodyA);
        }

        [UnityTest]
        public IEnumerator ReconnectedClientReceivesTheCurrentOrganPoses()
        {
            yield return StartHostAndSpawnBody();
            NetworkManager client = StartClient("ClientBeforeDisconnect");
            SharedBody clientBody = null;
            yield return WaitForBody(client, body => clientBody = body);
            MoveOrgans(_serverBody, step: 1);
            yield return WaitForSamePoses(_serverBody, clientBody, "client before disconnect");

            _clients.Remove(client);
            DestroyManager(client);
            yield return WaitFor(() => _host.ConnectedClientsIds.Count == 1, "the client to leave the host");

            // The body keeps moving while the client is away.
            MoveOrgans(_serverBody, step: 2);

            NetworkManager returned = StartClient("ClientAfterReconnect");
            SharedBody returnedBody = null;
            yield return WaitForBody(returned, body => returnedBody = body);
            yield return WaitForSamePoses(_serverBody, returnedBody, "reconnected client");
            AssertOrgansMoved(returnedBody);
        }

        private IEnumerator StartHostAndSpawnBody()
        {
            _host = CreateManager("Host");
            Assert.IsTrue(_host.StartHost(), "The host did not start. Is port " + Port + " free?");
            yield return WaitFor(() => _host.IsListening && _host.IsServer, "the host to listen");

            var instance = UnityEngine.Object.Instantiate(_bodyPrefab, new Vector3(1f, 0.8f, 1.2f), Quaternion.Euler(0f, 90f, 0f));
            instance.GetComponent<NetworkObject>().Spawn();
            _serverBody = instance.GetComponent<SharedBody>();
            Assert.GreaterOrEqual(_serverBody.Organs.Count, 22, "The body should sync all 22 organs.");
        }

        private NetworkManager StartClient(string clientName)
        {
            NetworkManager client = CreateManager(clientName);
            _clients.Add(client);
            Assert.IsTrue(client.StartClient(), $"{clientName} did not start.");
            return client;
        }

        private static GameObject LoadBodyPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath);
#else
            throw new NotSupportedException("These tests load the body prefab from the project and run in the editor only.");
#endif
        }

        private static NetworkManager CreateManager(string managerName)
        {
            var manager = new GameObject(managerName).AddComponent<NetworkManager>();
            var transport = manager.gameObject.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", Port, "127.0.0.1");
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false,
            };
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab
            {
                Prefab = LoadBodyPrefab(),
            });
            return manager;
        }

        private static void DestroyManager(NetworkManager manager)
        {
            if (manager == null)
                return;
            manager.Shutdown();
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
        }

        private IEnumerator WaitForBody(NetworkManager client, Action<SharedBody> found)
        {
            SharedBody body = null;
            yield return WaitFor(() =>
            {
                if (!client.IsConnectedClient)
                    return false;
                foreach (NetworkObject spawned in client.SpawnManager.SpawnedObjects.Values)
                {
                    body = spawned.GetComponent<SharedBody>();
                    if (body != null)
                        return true;
                }
                return false;
            }, $"{client.name} to receive the body");
            found(body);
        }

        // Moves every organ in local space. Different organs get different offsets so a mix-up is caught.
        private static void MoveOrgans(SharedBody body, int step)
        {
            for (int i = 0; i < body.Organs.Count; i++)
            {
                Transform organ = body.Organs[i].transform;
                organ.localPosition += new Vector3(0.1f * step, 0.002f * i, -0.03f * step);
                organ.localRotation *= Quaternion.Euler(3f * step, 1f * i, 0f);
            }
        }

        private IEnumerator WaitForSamePoses(SharedBody expected, SharedBody actual, string label)
        {
            string lastDifference = "";
            yield return WaitFor(() =>
            {
                lastDifference = DescribeDifferences(expected, actual);
                return lastDifference.Length == 0;
            }, label, () => lastDifference);
        }

        private static string DescribeDifferences(SharedBody expected, SharedBody actual)
        {
            var report = new StringBuilder();
            if (expected.Organs.Count != actual.Organs.Count)
                return $"organ count {expected.Organs.Count} vs {actual.Organs.Count}";
            for (int i = 0; i < expected.Organs.Count; i++)
            {
                Transform want = expected.Organs[i].transform;
                Transform got = actual.Organs[i].transform;
                float distance = Vector3.Distance(want.localPosition, got.localPosition);
                float angle = Quaternion.Angle(want.localRotation, got.localRotation);
                if (distance > PositionTolerance || angle > RotationToleranceDegrees)
                    report.AppendLine($"{want.name}: server {want.localPosition.ToString("F4")} client {got.localPosition.ToString("F4")}, position off by {distance:F4} m, rotation off by {angle:F2} deg");
            }
            return report.ToString();
        }

        // Guards against a test that passes only because nothing ever moved: the client must differ from the prefab layout.
        private void AssertOrgansMoved(SharedBody actual)
        {
            SharedBody original = _bodyPrefab.GetComponent<SharedBody>();
            for (int i = 0; i < original.Organs.Count; i++)
            {
                float moved = Vector3.Distance(original.Organs[i].transform.localPosition, actual.Organs[i].transform.localPosition);
                Assert.Greater(moved, PositionTolerance, $"{actual.Organs[i].name} still sits at its prefab position.");
            }
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what, Func<string> detail = null)
        {
            float end = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end)
                    Assert.Fail($"Timed out waiting for {what}. {(detail != null ? detail() : "")}");
                yield return null;
            }
        }
    }
}
