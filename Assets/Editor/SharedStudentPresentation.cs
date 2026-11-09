using System;
using System.Linq;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>Composes local student scenery around the existing shared body.</summary>
    public static class SharedStudentPresentation
    {
        public const long SceneTriangleBudget = 750000;

        // Does not open or save scenes. The caller owns saving and role references.
        public static GameObject[] ApplyToScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var bodies = roots.SelectMany(root => root.GetComponentsInChildren<SharedBody>(true)).ToArray();
            if (bodies.Length != 1)
                throw new InvalidOperationException($"Expected one shared body in {scene.path}; found {bodies.Length}.");
            var root = roots.Single(item => item.name == "DIYsect").transform;
            if (root.Find("DissectionChecklist") == null)
                throw new InvalidOperationException("Run Add Dissection Checklist before composing student scenery.");

            GameObject hall = AddLectureHall.EnsureInScene(scene, removePlaceholder: false);
            GameObject lamp = AddOperatingLight.ApplyToScene(scene);
            AddSeatedAudience.Populate(scene, AddSeatedAudience.LoadMaterials());
            GameObject audience = root.Find(AddSeatedAudience.CrowdName).gameObject;
            long triangles = scene.GetRootGameObjects().Sum(AddSeatedAudience.CountTriangles);
            if (triangles > SceneTriangleBudget)
                throw new InvalidOperationException($"Shared scene has {triangles:N0} mesh triangles; budget is {SceneTriangleBudget:N0}.");
            Debug.Log($"Shared student scenery prepared: {triangles:N0} scene mesh triangles. Device timing still requires measurement.");
            return new[] { hall, lamp, audience };
        }
    }
}
