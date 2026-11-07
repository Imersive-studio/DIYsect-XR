// Run Tools > DIYsect > Add Body Dissection after Add Body Networking (Create Anatomy Body runs both).
using System;
using System.Collections.Generic;
using System.IO;
using DIYsect.Dissection;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the midline incision to Assets/Prefabs/AnatomyBody.prefab. It measures the two skin flaps and then:
    /// puts a hinge pivot at the outer edge of each flap (the flap becomes its child, so the closed pose is
    /// unchanged), adds a SkinFlapHinge to each pivot, adds a DissectionProgress to the root with the midline
    /// from just inside the flap's top end (xiphoid) to its bottom end (pubis), and adds the IncisionTrigger box
    /// along that line. Then AnatomyBodyWall adds the rectus hinges, the omentum hinge and their touch triggers. Safe to run more than once: the same measurements give the same result.
    /// </summary>
    public static class AnatomyBodyDissection
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string SkinPath = "Supine/Skin";
        private const string TriggerName = "IncisionTrigger";
        private const float OpenAngleDegrees = 150f;
        private const float MidlineBand = 0.01f;     // vertices this close to x = 0 count as the cut edge
        private const float EndInset = 0.03f;        // the cut starts and ends this far inside the flap ends
        private const float ToleranceMeters = 0.04f;
        private const float EdgeBand = 0.01f;        // vertices this close to the outer extreme count as the outer edge

        [MenuItem("Tools/DIYsect/Add Body Dissection")]
        public static void Apply()
        {
            ApplyToPrefab();
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform skin = root.transform.Find(SkinPath);
                if (skin == null)
                    throw new InvalidOperationException($"{PrefabPath} has no {SkinPath} group.");

                var progress = GetOrAdd<DissectionProgress>(root);
                Transform left = FindFlap(skin, "skin_flap_l");
                Transform right = FindFlap(skin, "skin_flap_r");

                List<Vector3> leftPoints = RootLocalVertices(root.transform, left);
                List<Vector3> rightPoints = RootLocalVertices(root.transform, right);

                (Vector3 start, Vector3 end) = MeasureMidline(leftPoints, rightPoints);
                SetMidline(progress, start, end);
                AddTrigger(root.transform, progress, start, end);

                AddHinge(root.transform, skin, left, leftPoints, progress);
                AddHinge(root.transform, skin, right, rightPoints, progress);

                AnatomyBodyWall.Apply(root, progress);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added dissection components to {PrefabPath}. Midline (body-local): {start} to {end}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The flap is either directly under the Skin group or already under its hinge pivot.
        private static Transform FindFlap(Transform skin, string flapName)
        {
            foreach (Transform candidate in skin.GetComponentsInChildren<Transform>(true))
                if (candidate.name == flapName)
                    return candidate;
            throw new InvalidOperationException($"No {flapName} under {SkinPath}.");
        }

        internal static List<Vector3> RootLocalVertices(Transform root, Transform flap)
        {
            var points = new List<Vector3>();
            Matrix4x4 toRoot = root.worldToLocalMatrix;
            foreach (Renderer renderer in flap.GetComponentsInChildren<Renderer>())
            {
                Mesh mesh = renderer switch
                {
                    MeshRenderer => renderer.GetComponent<MeshFilter>()?.sharedMesh,
                    SkinnedMeshRenderer skinned => skinned.sharedMesh,
                    _ => null,
                };
                if (mesh == null)
                    continue;
                Matrix4x4 toRootLocal = toRoot * renderer.transform.localToWorldMatrix;
                foreach (Vector3 vertex in mesh.vertices)
                    points.Add(toRootLocal.MultiplyPoint3x4(vertex));
            }
            if (points.Count == 0)
                throw new InvalidOperationException($"{flap.name} has no readable mesh.");
            return points;
        }

        // The long axis of the lying body is the root's Z. The cut runs on the x = 0 line at the skin surface.
        private static (Vector3, Vector3) MeasureMidline(List<Vector3> leftPoints, List<Vector3> rightPoints)
        {
            float minZ = float.MaxValue, maxZ = float.MinValue;
            var edge = new List<Vector3>();
            foreach (List<Vector3> points in new[] { leftPoints, rightPoints })
            {
                foreach (Vector3 point in points)
                {
                    minZ = Mathf.Min(minZ, point.z);
                    maxZ = Mathf.Max(maxZ, point.z);
                    if (Mathf.Abs(point.x) <= MidlineBand)
                        edge.Add(point);
                }
            }
            if (edge.Count == 0)
                throw new InvalidOperationException("The skin flaps have no vertices near the midline (x = 0).");

            // Head is toward -Z in the Supine pose. The cut runs from the upper end (xiphoid) to the lower end (pubis).
            float xiphoidZ = minZ + EndInset;
            float pubisZ = maxZ - EndInset;
            return (new Vector3(0f, SurfaceHeight(edge, xiphoidZ), xiphoidZ), new Vector3(0f, SurfaceHeight(edge, pubisZ), pubisZ));
        }

        // Highest midline-edge vertex within a small band of z: the skin surface there.
        private static float SurfaceHeight(List<Vector3> edge, float z)
        {
            float best = float.MinValue;
            foreach (Vector3 point in edge)
                if (Mathf.Abs(point.z - z) <= 0.02f)
                    best = Mathf.Max(best, point.y);
            if (best == float.MinValue)
                throw new InvalidOperationException($"No midline skin vertex near z = {z}.");
            return best;
        }

        private static void SetMidline(DissectionProgress progress, Vector3 start, Vector3 end)
        {
            var serialized = new SerializedObject(progress);
            serialized.FindProperty("_lineStart").vector3Value = start;
            serialized.FindProperty("_lineEnd").vector3Value = end;
            serialized.FindProperty("_toleranceMeters").floatValue = ToleranceMeters;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddTrigger(Transform root, DissectionProgress progress, Vector3 start, Vector3 end)
        {
            Transform zone = root.Find(TriggerName);
            if (zone == null)
            {
                zone = new GameObject(TriggerName).transform;
                zone.SetParent(root, false);
            }
            Vector3 line = end - start;
            zone.SetLocalPositionAndRotation((start + end) * 0.5f, Quaternion.LookRotation(line.normalized, Vector3.up));

            var box = GetOrAdd<BoxCollider>(zone.gameObject);
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(ToleranceMeters * 2f, ToleranceMeters * 2f, line.magnitude);

            var trigger = GetOrAdd<IncisionTrigger>(zone.gameObject);
            var serialized = new SerializedObject(trigger);
            serialized.FindProperty("_target").objectReferenceValue = progress;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddHinge(Transform root, Transform skin, Transform flap, List<Vector3> points, DissectionProgress progress)
        {
            // The side whose |x| is larger is the outer edge. The pivot sits on that edge, halfway up the flap's outer vertices.
            float outerX = 0f;
            foreach (Vector3 point in points)
                if (Mathf.Abs(point.x) > Mathf.Abs(outerX))
                    outerX = point.x;
            float side = Mathf.Sign(outerX);

            float heightSum = 0f;
            int count = 0;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 point in points)
            {
                minZ = Mathf.Min(minZ, point.z);
                maxZ = Mathf.Max(maxZ, point.z);
                if (Mathf.Abs(point.x - outerX) <= EdgeBand)
                {
                    heightSum += point.y;
                    count++;
                }
            }
            var pivotPosition = new Vector3(outerX, heightSum / count, (minZ + maxZ) * 0.5f);

            string pivotName = flap.name + "_Hinge";
            Transform pivot = skin.Find(pivotName);
            if (pivot == null)
            {
                pivot = new GameObject(pivotName).transform;
                pivot.SetParent(skin, false);
            }
            pivot.localRotation = Quaternion.identity;
            pivot.position = root.TransformPoint(pivotPosition);
            if (flap.parent != pivot)
                flap.SetParent(pivot, true);

            var hinge = GetOrAdd<SkinFlapHinge>(pivot.gameObject);
            var serialized = new SerializedObject(hinge);
            serialized.FindProperty("_progress").objectReferenceValue = progress;
            // The hinge axis is the body's long axis (root Z), written in the pivot's own space.
            serialized.FindProperty("_hingeAxis").vector3Value = pivot.InverseTransformDirection(root.TransformDirection(Vector3.forward));
            // About +Z a positive angle turns counter-clockwise (x towards y). A flap hinged at +x opens with a negative angle, at -x with a positive one.
            serialized.FindProperty("_openAngleDegrees").floatValue = -side * OpenAngleDegrees;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"{pivotName}: hinge at {pivotPosition}, open angle {-side * OpenAngleDegrees}.");
        }

        internal static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T existing) ? existing : target.AddComponent<T>();
        }
    }
}
