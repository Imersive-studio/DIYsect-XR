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
            Material[] materials = AddSeatedAudience.LoadMaterials();
            try
            {
                var root = scene.GetRootGameObjects().Single(item => item.name == "DIYsect").transform;
                var networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
                AddSeatedAudience.Populate(scene, materials);
                Transform crowd = root.Find(AddSeatedAudience.CrowdName);
                Assert.That(crowd.childCount, Is.EqualTo(AddSeatedAudience.FigureCount));
                Assert.That(crowd.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(crowd.GetComponentsInChildren<NetworkObject>(true), Is.Empty);
                CollectionAssert.AreEqual(networkObjects, root.GetComponentsInChildren<NetworkObject>(true));
                Assert.That(AddSeatedAudience.CountTriangles(crowd.gameObject), Is.LessThanOrEqualTo(AddSeatedAudience.TriangleBudget));

                Transform hall = root.Find("LectureHall");
                var seats = HallSeatGeometry.FindSeats(hall);
                Assert.That(crowd.GetComponentsInChildren<Renderer>().Select(item => item.sharedMaterial).Distinct().Count(), Is.EqualTo(materials.Length));

                foreach (Transform figure in crowd)
                {
                    Assert.That(seats.Any(seat => Vector3.Distance(seat, figure.position) < 0.005f), Is.True, figure.name);
                    var cutout = figure.Find("Cutout").GetComponent<Renderer>();
                    Assert.That(cutout.bounds.min.y, Is.EqualTo(figure.position.y - AddSeatedAudience.BelowSeat).Within(0.005f), figure.name);
                    Assert.That(Vector3.Dot(figure.forward, root.Find("AnatomyBody").position - figure.position), Is.LessThan(0f), figure.name + " must face the table.");
                }

                foreach (Renderer renderer in crowd.GetComponentsInChildren<Renderer>())
                {
                    CollectionAssert.Contains(materials, renderer.sharedMaterial);
                    Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
                    Assert.That(renderer.gameObject.layer, Is.Zero);
                }

                Assert.That(AddSeatedAudience.Populate(scene, materials), Is.False);
                Assert.That(root.Find(AddSeatedAudience.CrowdName), Is.SameAs(crowd));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
