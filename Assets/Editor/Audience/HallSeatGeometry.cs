using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Editor
{
    internal static class HallSeatGeometry
    {
        public static List<Vector3> FindSeats(Transform hall)
        {
            var seats = new List<Vector3>();
            foreach (MeshFilter filter in hall.GetComponentsInChildren<MeshFilter>())
            {
                string[] name = filter.name.Split('_');
                if (name.Length < 4 || name[0] != "chair" ||
                    !int.TryParse(name[2], out int row) || row < 1 || row > 3)
                    continue;

                ReadMesh(filter, out Vector3[] vertices, out int[] triangles);
                foreach (Bounds cushion in CushionBounds(vertices, triangles))
                {
                    Vector3 centre = cushion.center;
                    centre.y = cushion.max.y;
                    Vector3 seat = filter.transform.TransformPoint(centre);
                    int match = seats.FindIndex(existing => HorizontalDistanceSquared(existing, seat) < 0.42f * 0.42f);
                    if (match < 0)
                        seats.Add(seat);
                    else if (seat.y > seats[match].y)
                        seats[match] = seat;
                }
            }

            return seats.OrderBy(seat => seat.z).ThenBy(seat => seat.x).ThenBy(seat => seat.y).ToList();
        }

        public static List<Vector3> ReadFloor(Transform hall)
        {
            var floor = new List<Vector3>();
            foreach (MeshFilter filter in hall.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.StartsWith("floor_", StringComparison.OrdinalIgnoreCase))
                    continue;

                ReadMesh(filter, out Vector3[] vertices, out int[] triangles);
                foreach (int vertex in triangles)
                    floor.Add(filter.transform.TransformPoint(vertices[vertex]));
            }

            return floor;
        }

        public static bool TryFloorHeight(List<Vector3> floor, Vector3 point, out float height)
        {
            height = float.NegativeInfinity;
            for (int i = 0; i < floor.Count; i += 3)
            {
                Vector3 a = floor[i];
                Vector3 b = floor[i + 1] - a;
                Vector3 c = floor[i + 2] - a;
                Vector3 offset = point - a;
                float area = b.x * c.z - b.z * c.x;
                if (Mathf.Abs(area) < 0.00001f)
                    continue;

                float u = (offset.x * c.z - offset.z * c.x) / area;
                float v = (b.x * offset.z - b.z * offset.x) / area;
                if (u < 0f || v < 0f || u + v > 1f)
                    continue;

                float y = a.y + u * b.y + v * c.y;
                if (y < point.y && y > height)
                    height = y;
            }

            return !float.IsNegativeInfinity(height);
        }

        private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
        {
            Vector3 delta = first - second;
            return delta.x * delta.x + delta.z * delta.z;
        }

        // Each row is split into imported meshes. Weld duplicated vertices along UV seams
        // so each connected cushion can be measured separately.
        private static IEnumerable<Bounds> CushionBounds(Vector3[] vertices, int[] triangles)
        {
            var firstByPosition = new Dictionary<Vector3Int, int>();
            var parent = new int[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Int key = Vector3Int.RoundToInt(vertices[i] * 10000f);
                if (!firstByPosition.TryGetValue(key, out int first))
                {
                    first = i;
                    firstByPosition.Add(key, i);
                }
                parent[i] = first;
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int root = FindRoot(parent, triangles[i]);
                parent[FindRoot(parent, triangles[i + 1])] = root;
                parent[FindRoot(parent, triangles[i + 2])] = root;
            }

            var boundsByPart = new Dictionary<int, Bounds>();
            for (int i = 0; i < vertices.Length; i++)
            {
                int root = FindRoot(parent, i);
                if (!boundsByPart.TryGetValue(root, out Bounds bounds))
                    bounds = new Bounds(vertices[i], Vector3.zero);
                bounds.Encapsulate(vertices[i]);
                boundsByPart[root] = bounds;
            }

            foreach (Bounds bounds in boundsByPart.Values)
            {
                Vector3 size = bounds.size;
                // Cushion shells are wide, shallow and below the row's mesh origin.
                if (bounds.center.y < 0f && size.x >= 0.50f && size.x <= 0.90f &&
                    size.z >= 0.44f && size.z <= 0.90f && size.y >= 0.035f && size.y <= 0.17f)
                    yield return bounds;
            }
        }

        private static int FindRoot(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }

            return index;
        }

        private static void ReadMesh(MeshFilter filter, out Vector3[] vertices, out int[] triangles)
        {
            using var snapshot = MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh);
            Mesh.MeshData mesh = snapshot[0];
            using var positions = new NativeArray<Vector3>(mesh.vertexCount, Allocator.Temp);
            mesh.GetVertices(positions);
            vertices = positions.ToArray();

            var indices = new List<int>();
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                SubMeshDescriptor part = mesh.GetSubMesh(i);
                if (part.topology != MeshTopology.Triangles)
                    continue;

                using var submesh = new NativeArray<int>(part.indexCount, Allocator.Temp);
                mesh.GetIndices(submesh, i);
                foreach (int index in submesh)
                    indices.Add(index);
            }

            triangles = indices.ToArray();
        }
    }
}
