using System.Linq;
using DIYsect.Editor;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DIYsect.Audience.Tests
{
    public sealed class SeatedAudienceSceneTests
    {
        [Test]
        public void FiguresOccupySeatsAndRerunningKeepsTheirObjects()
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(AddSeatedAudience.ScenePath);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                var root = scene.GetRootGameObjects().Single(item => item.name == "DIYsect").transform;
                var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
                AddSeatedAudience.Populate(scene, material);
                Transform crowd = root.Find(AddSeatedAudience.CrowdName);
                Assert.That(crowd.childCount, Is.EqualTo(AddSeatedAudience.FigureCount));
                Assert.That(crowd.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(crowd.GetComponentsInChildren<NetworkObject>(true), Is.Empty);
                CollectionAssert.AreEqual(networkObjects, root.GetComponentsInChildren<NetworkObject>(true));
                Assert.That(AddSeatedAudience.CountTriangles(crowd.gameObject), Is.LessThanOrEqualTo(AddSeatedAudience.TriangleBudget));

                Transform hall = root.Find("LectureHall");
                var seats = HallSeatGeometry.FindSeats(hall);
                var floor = HallSeatGeometry.ReadFloor(hall);
                var colliders = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Collider>())
                    .Where(collider => collider.enabled && !collider.isTrigger && SupportsClosestPoint(collider)).ToArray();

                foreach (Transform figure in crowd)
                {
                    Assert.That(seats.Any(seat => Vector3.Distance(seat, figure.position) < 0.005f), Is.True, figure.name);
                    var shoe = figure.Find("Left Shoe").GetComponent<Renderer>();
                    Vector3 foot = figure.position + figure.forward * 0.26f;
                    Assert.That(HallSeatGeometry.TryFloorHeight(floor, foot, out float height), Is.True, figure.name);
                    Assert.That(shoe.bounds.min.y, Is.EqualTo(height).Within(0.005f), figure.name);
                    Vector3 torso = figure.Find("Upper Body/Torso").position;
                    Assert.That(colliders.Any(collider =>
                        (collider.ClosestPoint(torso) - torso).sqrMagnitude < 0.000001f), Is.False, figure.name);
                }

                foreach (Renderer renderer in crowd.GetComponentsInChildren<Renderer>())
                {
                    Assert.That(renderer.sharedMaterial, Is.SameAs(material));
                    Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                    Assert.That(renderer.gameObject.layer, Is.Zero);
                }

                Assert.That(AddSeatedAudience.Populate(scene, material), Is.False);
                Assert.That(root.Find(AddSeatedAudience.CrowdName), Is.SameAs(crowd));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                Object.DestroyImmediate(material);
            }
        }
        // Unity only supports closest-point queries on primitives and convex meshes.
        private static bool SupportsClosestPoint(Collider collider) =>
            collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider ||
            collider is MeshCollider mesh && mesh.convex;
    }
}
