using DIYsect.Editor;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Audience.Tests
{
    public class SceneTriangleCountTests
    {
        [Test]
        public void IncludesInactiveStaticAndSkinnedInstancesOfTheSameMesh()
        {
            var root = new GameObject("scene geometry");
            var mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
                triangles = new[] { 0, 1, 2 }
            };
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var hidden = new GameObject("inactive role");
                hidden.transform.SetParent(root.transform);
                hidden.AddComponent<MeshFilter>().sharedMesh = mesh;
                hidden.AddComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
                hidden.SetActive(false);
                Assert.That(AddSeatedAudience.CountTriangles(root), Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(mesh);
            }
        }
    }
}
