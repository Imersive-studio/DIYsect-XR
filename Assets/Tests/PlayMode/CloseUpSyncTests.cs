// Starts a real in-process Netcode host and a client over UnityTransport on localhost (no cloud services).
// Authority under test: none new. The server owns the organ poses; the close-up camera is local to each client and only looks at
// that client's own synced copy of the body, so nothing about the camera itself is sent over the network.
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class CloseUpSyncTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 3;
        private const float MoveMetres = 0.15f;
        private const float ViewportTolerance = 0.01f;

        [UnityTest]
        public IEnumerator ClientSyncedOrganMovesInACloseUpStyleView()
        {
            yield return StartHostAndSpawnBody();
            Unity.Netcode.NetworkManager client = StartClient("Client");
            SharedBody clientBody = null;
            yield return WaitForBody(client, body => clientBody = body);

            Camera closeUp = CreateCloseUpCameraAbove(clientBody.transform);
            Transform organ = clientBody.Organs[OrganIndex].transform;
            yield return WaitFor(() => IsInView(closeUp, organ.position, out _), "the synced organ to be inside the client's close-up view");
            IsInView(closeUp, organ.position, out Vector3 before);

            // Move the organ on the host along the body's local x axis (across the image).
            _serverBody.Organs[OrganIndex].transform.localPosition += new Vector3(MoveMetres, 0f, 0f);
            Vector3 hostPosition = _serverBody.Organs[OrganIndex].transform.localPosition;
            yield return WaitFor(() => (organ.localPosition - hostPosition).sqrMagnitude < 1e-4f, "the client's organ to follow the host");

            Assert.IsTrue(IsInView(closeUp, organ.position, out Vector3 after), "The moved organ left the client's close-up view.");
            float shift = Mathf.Abs(after.x - before.x) + Mathf.Abs(after.y - before.y);
            Assert.Greater(shift, ViewportTolerance, "The close-up view did not change when the host moved the organ.");
        }

        // A stand-in with the same mounting as the scene camera (one metre above the body, looking straight down, 50 degree
        // field of view). The scene camera itself is checked by the edit tests in CloseUpSceneTests.
        private static Camera CreateCloseUpCameraAbove(Transform body)
        {
            var cameraObject = new GameObject("CloseUpCameraUnderTest");
            cameraObject.transform.SetPositionAndRotation(body.position + Vector3.up, Quaternion.Euler(90f, -90f, 0f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.05f;
            camera.aspect = 1f;
            return camera;
        }

        private static bool IsInView(Camera camera, Vector3 worldPoint, out Vector3 viewport)
        {
            viewport = camera.WorldToViewportPoint(worldPoint);
            return viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
        }

        [TearDown]
        public void DestroyCamera()
        {
            GameObject cameraObject = GameObject.Find("CloseUpCameraUnderTest");
            if (cameraObject != null)
                Object.Destroy(cameraObject);
        }
    }
}
