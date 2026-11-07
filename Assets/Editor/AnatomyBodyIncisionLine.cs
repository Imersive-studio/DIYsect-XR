// Run by Tools > DIYsect > Add Body Dissection (after the midline is measured).
using System;
using System.Collections.Generic;
using System.IO;
using DIYsect.Dissection;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the visible cut line to the body prefab: an IncisionLine child of the root (identity pose) with a
    /// LineRenderer and an IncisionLinePresenter. The path follows the skin along the midline: for evenly spaced
    /// points between the measured xiphoid and pubis ends, it takes the highest skin vertex near the midline,
    /// lifted slightly so the line sits on the surface. Safe to run more than once.
    /// </summary>
    public static class AnatomyBodyIncisionLine
    {
        private const string ChildName = "IncisionLine";
        private const string MaterialPath = "Assets/Prefabs/IncisionLine.mat";
        private const int PathPoints = 32;
        private const float MidlineBand = 0.012f;   // skin vertices this close to x = 0 lie on the midline
        private const float Lift = 0.0015f;         // keeps the line just above the skin
        private const float Width = 0.003f;
        private static readonly Color CutColor = new Color(0.42f, 0.03f, 0.03f);

        public static void Apply(GameObject root, DissectionProgress progress, IEnumerable<Transform> flaps)
        {
            var skin = new List<Vector3>();
            foreach (Transform flap in flaps)
                skin.AddRange(AnatomyBodyDissection.RootLocalVertices(root.transform, flap));
            Vector3[] path = MeasurePath(skin, progress.LineStart, progress.LineEnd);

            Transform child = root.transform.Find(ChildName);
            if (child == null)
            {
                child = new GameObject(ChildName).transform;
                child.SetParent(root.transform, false);
            }
            child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            child.localScale = Vector3.one;

            var line = AnatomyBodyDissection.GetOrAdd<LineRenderer>(child.gameObject);
            line.useWorldSpace = false;
            line.widthMultiplier = Width;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = LoadOrCreateMaterial();
            line.positionCount = 0;
            line.enabled = false;

            var presenter = AnatomyBodyDissection.GetOrAdd<IncisionLinePresenter>(child.gameObject);
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_progress").objectReferenceValue = progress;
            SerializedProperty points = serialized.FindProperty("_path");
            points.arraySize = path.Length;
            for (int i = 0; i < path.Length; i++)
                points.GetArrayElementAtIndex(i).vector3Value = path[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"{ChildName}: {path.Length} points on the skin from {path[0]} to {path[path.Length - 1]}.");
        }

        private static Vector3[] MeasurePath(List<Vector3> skin, Vector3 start, Vector3 end)
        {
            var midline = skin.FindAll(p => Mathf.Abs(p.x) <= MidlineBand);
            if (midline.Count == 0)
                throw new InvalidOperationException("No skin vertices near the midline (x = 0). Check the skin flaps.");
            float binHalf = Mathf.Abs(end.z - start.z) / (PathPoints - 1);
            var path = new Vector3[PathPoints];
            for (int i = 0; i < PathPoints; i++)
            {
                float t = i / (float)(PathPoints - 1);
                float z = Mathf.Lerp(start.z, end.z, t);
                float height = float.MinValue;
                foreach (Vector3 point in midline)
                    if (Mathf.Abs(point.z - z) <= binHalf)
                        height = Mathf.Max(height, point.y);
                // Where no skin vertex is close, follow the straight midline between the measured ends.
                if (height == float.MinValue)
                    height = Mathf.Lerp(start.y, end.y, t);
                path[i] = new Vector3(0f, height + Lift, z);
            }
            return path;
        }

        private static Material LoadOrCreateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader not found.");
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            material = new Material(shader) { name = "IncisionLine" };
            material.SetColor("_BaseColor", CutColor);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
