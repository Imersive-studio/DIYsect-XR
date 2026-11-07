// Shared setup for PlayMode tests: a real in-process Netcode host and clients over UnityTransport on localhost.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
    public abstract class NetworkedBodyTestBase
    {
        protected const string BodyPrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        protected const float TimeoutSeconds = 8f;

        // Headless runs would otherwise render thousands of frames per second, and interpolation needs realistic frame times.
        private const int TestFrameRate = 60;

        protected GameObject _bodyPrefab;
        private int _previousFrameRate;
        protected NetworkManager _host;
        protected readonly List<NetworkManager> _clients = new List<NetworkManager>();
        protected SharedBody _serverBody;

        // Each test gets its own free port, so a test that fails or another process on the machine cannot block the next one.
        protected ushort _port;

        [SetUp]
        public void SetUp()
        {
            _port = FindFreeUdpPort();
            _previousFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = TestFrameRate;
            _bodyPrefab = LoadBodyPrefab();
            Assert.IsNotNull(_bodyPrefab, $"{BodyPrefabPath} is missing.");
            Assert.IsNotNull(_bodyPrefab.GetComponent<NetworkObject>(), "The body prefab has no NetworkObject. Run Tools > DIYsect > Add Body Networking.");
        }

        // Runs even when the test failed or timed out. Shuts everything down, then waits until the port is free again.
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.targetFrameRate = _previousFrameRate;
            foreach (NetworkManager client in _clients)
                DestroyManager(client);
            _clients.Clear();
            DestroyManager(_host);
            _host = null;
            if (_serverBody != null)
                UnityEngine.Object.Destroy(_serverBody.gameObject);

            float end = Time.realtimeSinceStartup + 3f;
            while (!IsUdpPortFree(_port) && Time.realtimeSinceStartup < end)
                yield return null;
        }

        protected IEnumerator StartHostAndSpawnBody()
        {
            _host = CreateManager("Host");
            Assert.IsTrue(_host.StartHost(), "The host did not start. Is port " + _port + " free?");
            yield return WaitFor(() => _host.IsListening && _host.IsServer, "the host to listen");

            var instance = UnityEngine.Object.Instantiate(_bodyPrefab, new Vector3(1f, 0.8f, 1.2f), Quaternion.Euler(0f, 90f, 0f));
            instance.GetComponent<NetworkObject>().Spawn();
            _serverBody = instance.GetComponent<SharedBody>();
            Assert.GreaterOrEqual(_serverBody.Organs.Count, 22, "The body should sync all 22 organs.");
        }

        protected NetworkManager StartClient(string clientName)
        {
            NetworkManager client = CreateManager(clientName);
            _clients.Add(client);
            Assert.IsTrue(client.StartClient(), $"{clientName} did not start.");
            return client;
        }

        private static ushort FindFreeUdpPort()
        {
            using (var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
                return (ushort)((IPEndPoint)socket.Client.LocalEndPoint).Port;
        }

        private static bool IsUdpPortFree(ushort port)
        {
            try
            {
                using (new UdpClient(new IPEndPoint(IPAddress.Loopback, port)))
                    return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        protected static GameObject LoadBodyPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(BodyPrefabPath);
#else
            throw new NotSupportedException("These tests load the body prefab from the project and run in the editor only.");
#endif
        }

        protected NetworkManager CreateManager(string managerName)
        {
            var manager = new GameObject(managerName).AddComponent<NetworkManager>();
            var transport = manager.gameObject.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", _port, "127.0.0.1");
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

        protected void DestroyManager(NetworkManager manager)
        {
            if (manager == null)
                return;
            manager.Shutdown();
            UnityEngine.Object.DestroyImmediate(manager.gameObject);
        }

        protected IEnumerator WaitForBody(NetworkManager client, Action<SharedBody> found)
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

        protected static IEnumerator WaitFor(Func<bool> condition, string what, Func<string> detail = null)
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
