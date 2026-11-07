using System;
using System.Collections.Generic;
using DIYsect.Dissection;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// The deeper layers of the body wall, added to the loaded body prefab by AnatomyBodyDissection (so it also
    /// runs from Create Anatomy Body). Each rectus half gets a hinge pivot on its outer edge that opens at
    /// RectusOpen; the greater omentum gets a hinge on its head-side edge that lifts it at OmentumLifted. The
    /// transversus halves lie under the rectus across the front of the abdomen, so they open with it (slightly
    /// less far, so they rest beneath the rectus) and the cavity is exposed. A trigger box over the omentum asks for LiftOmentum and a slab over the
    /// rectus (kept clear of the omentum box) asks for OpenRectus. Hinge axes and trigger boxes come from the
    /// measured mesh bounds. Safe to run more than once.
    /// </summary>
    internal static class AnatomyBodyWall
    {
        private const string MuscleGroup = "Supine/Muscle";
        private const string OmentumGroup = "Supine/Omentum";
        private const float RectusOpenDegrees = 120f;
        private const float TransversusOpenDegrees = 110f;
        private const float OmentumLiftDegrees = 100f;
        private const float EdgeBand = 0.01f;
        private const float SlabHeight = 0.03f;
        private const float TriggerPadding = 0.01f;

        internal static void Apply(GameObject root, DissectionProgress progress)
        {
            Transform muscle = Require(root, MuscleGroup);
            Transform omentumGroup = Require(root, OmentumGroup);
            Transform rectusLeft = Find(muscle, "rectus_abdominis_muscle_l");
            Transform rectusRight = Find(muscle, "rectus_abdominis_muscle_r");
            Transform omentum = Find(omentumGroup, "greater_omentum");

            List<Vector3> leftPoints = AnatomyBodyDissection.RootLocalVertices(root.transform, rectusLeft);
            List<Vector3> rightPoints = AnatomyBodyDissection.RootLocalVertices(root.transform, rectusRight);
            List<Vector3> omentumPoints = AnatomyBodyDissection.RootLocalVertices(root.transform, omentum);

            AddSideHinge(root.transform, muscle, rectusLeft, leftPoints, progress, RectusOpenDegrees);
            AddSideHinge(root.transform, muscle, rectusRight, rightPoints, progress, RectusOpenDegrees);
            foreach (string side in new[] { "l", "r" })
            {
                Transform transversus = Find(muscle, $"transversus_abdominis_muscle_{side}");
                AddSideHinge(root.transform, muscle, transversus, AnatomyBodyDissection.RootLocalVertices(root.transform, transversus),
                    progress, TransversusOpenDegrees);
            }
            AddHeadHinge(root.transform, omentumGroup, omentum, omentumPoints, progress);

            Bounds omentumBounds = BoundsOf(omentumPoints);
            Bounds rectusBounds = BoundsOf(leftPoints);
            rectusBounds.Encapsulate(BoundsOf(rightPoints));
            AddTrigger(root.transform, progress, "OmentumTrigger", DissectionAction.LiftOmentum, Pad(omentumBounds));
            AddTrigger(root.transform, progress, "RectusTrigger", DissectionAction.OpenRectus, RectusSlab(rectusBounds, omentumBounds));
            Debug.Log($"Wall layers: rectus bounds {rectusBounds}, omentum bounds {omentumBounds}.");
        }

        // Rectus and transversus halves hinge on their outer (lateral) edge, like the skin flaps, and open at RectusOpen.
        private static void AddSideHinge(Transform root, Transform group, Transform layer, List<Vector3> points, DissectionProgress progress,
            float openDegrees)
        {
            float outerX = 0f;
            foreach (Vector3 point in points)
                if (Mathf.Abs(point.x) > Mathf.Abs(outerX))
                    outerX = point.x;
            float side = Mathf.Sign(outerX);
            float heightSum = 0f;
            int count = 0;
            foreach (Vector3 point in points)
            {
                if (Mathf.Abs(point.x - outerX) > EdgeBand)
                    continue;
                heightSum += point.y;
                count++;
            }
            Bounds bounds = BoundsOf(points);
            var pivotPosition = new Vector3(outerX, heightSum / count, bounds.center.z);
            // About root +Z a positive angle turns x towards y, so a layer hinged at +x opens with a negative one.
            AddHinge(root, group, layer, pivotPosition, Vector3.forward, -side * openDegrees, DissectionStage.RectusOpen, progress);
        }

        // The omentum hangs toward the pubis (+Z). It hinges on its head-side edge (min Z) and folds up and over (about root X).
        private static void AddHeadHinge(Transform root, Transform group, Transform layer, List<Vector3> points, DissectionProgress progress)
        {
            Bounds bounds = BoundsOf(points);
            float heightSum = 0f;
            int count = 0;
            foreach (Vector3 point in points)
            {
                if (point.z - bounds.min.z > EdgeBand)
                    continue;
                heightSum += point.y;
                count++;
            }
            var pivotPosition = new Vector3(bounds.center.x, heightSum / count, bounds.min.z);
            // About +X a positive angle turns the pubis-ward direction (+Z) downward, so lifting is negative.
            AddHinge(root, group, layer, pivotPosition, Vector3.right, -OmentumLiftDegrees, DissectionStage.OmentumLifted, progress);
        }

        private static void AddHinge(Transform root, Transform group, Transform layer, Vector3 pivotRootLocal, Vector3 axisRootLocal,
            float angle, DissectionStage openAt, DissectionProgress progress)
        {
            string pivotName = layer.name + "_Hinge";
            Transform pivot = group.Find(pivotName);
            if (pivot == null)
            {
                pivot = new GameObject(pivotName).transform;
                pivot.SetParent(group, false);
            }
            pivot.localRotation = Quaternion.identity;
            pivot.position = root.TransformPoint(pivotRootLocal);
            if (layer.parent != pivot)
                layer.SetParent(pivot, true);

            var hinge = AnatomyBodyDissection.GetOrAdd<LayerHinge>(pivot.gameObject);
            var serialized = new SerializedObject(hinge);
            serialized.FindProperty("_progress").objectReferenceValue = progress;
            serialized.FindProperty("_hingeAxis").vector3Value = pivot.InverseTransformDirection(root.TransformDirection(axisRootLocal));
            serialized.FindProperty("_openAngleDegrees").floatValue = angle;
            serialized.FindProperty("_openAtStage").enumValueIndex = (int)openAt;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"{pivotName}: hinge at {pivotRootLocal}, open angle {angle}, opens at {openAt}.");
        }

        // The slab sits on top of the rectus, starting above the (padded) omentum box so the two never overlap.
        private static Bounds RectusSlab(Bounds rectus, Bounds omentum)
        {
            float top = rectus.max.y;
            float bottom = Mathf.Max(top - SlabHeight, Pad(omentum).max.y + 0.002f);
            if (bottom >= top)
                throw new InvalidOperationException("The omentum box reaches the top of the rectus, so there is no room for the rectus trigger.");
            var slab = new Bounds(new Vector3(rectus.center.x, (top + bottom) * 0.5f, rectus.center.z), new Vector3(rectus.size.x, top - bottom, rectus.size.z));
            return slab;
        }

        private static Bounds Pad(Bounds bounds)
        {
            bounds.Expand(TriggerPadding * 2f);
            return bounds;
        }

        private static void AddTrigger(Transform root, DissectionProgress progress, string name, DissectionAction action, Bounds rootLocal)
        {
            Transform zone = root.Find(name);
            if (zone == null)
            {
                zone = new GameObject(name).transform;
                zone.SetParent(root, false);
            }
            zone.localPosition = Vector3.zero;
            zone.localRotation = Quaternion.identity;
            zone.localScale = Vector3.one;

            var box = AnatomyBodyDissection.GetOrAdd<BoxCollider>(zone.gameObject);
            box.isTrigger = true;
            box.center = rootLocal.center;
            box.size = rootLocal.size;

            var trigger = AnatomyBodyDissection.GetOrAdd<LayerTouchTrigger>(zone.gameObject);
            var serialized = new SerializedObject(trigger);
            serialized.FindProperty("_target").objectReferenceValue = progress;
            serialized.FindProperty("_action").enumValueIndex = (int)action;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds BoundsOf(List<Vector3> points)
        {
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points)
                bounds.Encapsulate(point);
            return bounds;
        }

        private static Transform Require(GameObject root, string path)
        {
            Transform found = root.transform.Find(path);
            if (found == null)
                throw new InvalidOperationException($"The body prefab has no {path} group.");
            return found;
        }

        // The layer is either directly under its group or already under its hinge pivot.
        private static Transform Find(Transform group, string layerName)
        {
            foreach (Transform candidate in group.GetComponentsInChildren<Transform>(true))
                if (candidate.name == layerName)
                    return candidate;
            throw new InvalidOperationException($"No {layerName} under {group.name}.");
        }
    }
}
