// Run Tools > DIYsect > Report Scene Budgets to check the three DIYsect scenes.
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Opens each DIYsect scene and logs its triangle count, body placement, close-up culling and missing references.
    /// Throws when a scene is over budget or the close-up camera can see the screen.
    /// </summary>
    public static class SceneReport
    {
        private const int TriangleBudget = 750000;
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Report Scene Budgets")]
        public static void Report()
        {
            var failures = new List<string>();
            foreach (string path in ScenePaths)
                ReportScene(path, failures);

            if (failures.Count > 0)
                throw new System.InvalidOperationException("Scene report failed:\n" + string.Join("\n", failures));
            Debug.Log("Scene report passed for all scenes.");
        }

        private static void ReportScene(string path, List<string> failures)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var log = new StringBuilder($"[SceneReport] {path}\n");

            int triangles = CountActiveTriangles();
            log.AppendLine($"  triangles (active): {triangles} (budget {TriangleBudget})");
            if (triangles > TriangleBudget)
                failures.Add($"{path}: {triangles} triangles is over the {TriangleBudget} budget.");

            ReportBody(log, path, failures);
            ReportCloseUpCamera(log, path, failures);

            CountMissing(scene, out int missingScripts, out int missingPrefabs);
            log.AppendLine($"  missing scripts: {missingScripts}, missing prefabs: {missingPrefabs}");
            if (missingScripts > 0 || missingPrefabs > 0)
                failures.Add($"{path}: {missingScripts} missing scripts, {missingPrefabs} missing prefabs.");

            Debug.Log(log.ToString());
        }

        private static int CountActiveTriangles()
        {
            int total = 0;
            foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var meshRenderer = filter.GetComponent<MeshRenderer>();
                if (meshRenderer != null && meshRenderer.enabled)
                    total += Triangles(filter.sharedMesh);
            }
            foreach (var skinned in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (skinned.enabled)
                    total += Triangles(skinned.sharedMesh);
            return total;
        }

        private static int Triangles(Mesh mesh)
        {
            if (mesh == null) return 0;
            long indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                indices += mesh.GetIndexCount(i);
            return (int)(indices / 3);
        }

        private static void ReportBody(StringBuilder log, string path, List<string> failures)
        {
            GameObject body = GameObject.Find("AnatomyBody");
            GameObject table = GameObject.Find("DissectionTable");
            if (body == null || table == null)
            {
                failures.Add($"{path}: AnatomyBody or DissectionTable is missing.");
                return;
            }
            Bounds bodyBounds = AnatomyBounds(body);
            Renderer top = null;
            foreach (Renderer candidate in table.GetComponentsInChildren<Renderer>())
                if (candidate.name == "Top") top = candidate;
            if (top == null)
            {
                failures.Add($"{path}: the table has no Top renderer.");
                return;
            }
            float gap = bodyBounds.min.y - top.bounds.max.y;
            log.AppendLine($"  body world bounds size (m): {bodyBounds.size.x:F3} x {bodyBounds.size.y:F3} x {bodyBounds.size.z:F3}");
            log.AppendLine($"  body bottom to table top (m): {gap:F4}");
            if (Mathf.Abs(gap) > 0.005f)
                failures.Add($"{path}: the body bottom is {gap:F4} m from the table top.");
        }

        private static void ReportCloseUpCamera(StringBuilder log, string path, List<string> failures)
        {
            GameObject cameraObject = GameObject.Find("CloseUpCamera");
            GameObject screen = GameObject.Find("CloseUpScreen");
            var closeUp = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
            if (closeUp == null || screen == null)
            {
                failures.Add($"{path}: CloseUpCamera or CloseUpScreen is missing.");
                return;
            }
            int layer = screen.layer;
            bool screenSeen = (closeUp.cullingMask & (1 << layer)) != 0 || layer == 0;
            log.AppendLine($"  screen layer: {LayerMask.LayerToName(layer)} ({layer}); camera culls screen: {!screenSeen}; " +
                           $"target texture: {(closeUp.targetTexture != null ? closeUp.targetTexture.name : "none")}");
            if (screenSeen)
                failures.Add($"{path}: the close-up camera can see the screen.");
            if (closeUp.targetTexture == null)
                failures.Add($"{path}: the close-up camera has no target texture.");
        }

        // The anatomy meshes only: the drapes hang over the table edges and the cut line is not tissue.
        private static Bounds AnatomyBounds(GameObject body)
        {
            Transform drapes = body.transform.Find("Supine/Drapes");
            var bounds = new Bounds();
            bool any = false;
            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>())
            {
                bool isDrape = drapes != null && renderer.transform.IsChildOf(drapes);
                if (isDrape || renderer is LineRenderer)
                    continue;
                if (any)
                    bounds.Encapsulate(renderer.bounds);
                else
                {
                    bounds = renderer.bounds;
                    any = true;
                }
            }
            return bounds;
        }

        private static void CountMissing(Scene scene, out int missingScripts, out int missingPrefabs)
        {
            missingScripts = 0;
            missingPrefabs = 0;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform node in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject);
                    if (PrefabUtility.GetPrefabInstanceStatus(node.gameObject) == PrefabInstanceStatus.MissingAsset)
                        missingPrefabs++;
                }
            }
        }
    }
}
