// Test helpers for organ drops: a ground that catches loose organs, and drop points chosen so they cannot be
// mistaken for a neighbour's socket. All organ poses are in the local space of the organ's parent.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Networking.Tests
{
    public static class OrganDropSpots
    {
        /// <summary>A large static floor on the Environment layer whose top face is at <paramref name="topY"/> (world).</summary>
        public static GameObject CreateGround(float topY, Vector3 centreXZ)
        {
            int layer = LayerMask.NameToLayer("Environment");
            Assert.IsTrue(layer >= 0, "The Environment layer is missing from Project Settings > Tags and Layers.");
            var ground = new GameObject("TestGround") { layer = layer };
            ground.transform.position = new Vector3(centreXZ.x, topY - 0.5f, centreXZ.z);
            ground.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
            return ground;
        }

        /// <summary>A drop point (organ root, parent space) a world-space offset away from the organ's home pose.</summary>
        public static Vector3 DropRootPosition(SharedBody body, int organ, Vector3 worldOffset)
        {
            Transform parent = body.Organs[organ].transform.parent;
            Pose home = body.Organs[organ].GetComponent<ISnappable>().HomeRootPose;
            return parent.InverseTransformPoint(parent.TransformPoint(home.position) + worldOffset);
        }

        /// <summary>
        /// Finds the first offset (parent space) from the organ's home that keeps the organ's centre farther than
        /// <paramref name="minOtherDistance"/> from every other organ's socket centre. The drop root is home root plus the offset.
        /// </summary>
        public static bool TryFindClearOffset(SharedBody body, int organ, IEnumerable<Vector3> offsets, float minOtherDistance, out Vector3 found)
        {
            Vector3 homeCentre = body.Organs[organ].GetComponent<ISnappable>().HomeCentrePose.position;
            foreach (Vector3 offset in offsets)
            {
                bool clear = true;
                for (int i = 0; i < body.Organs.Count && clear; i++)
                {
                    if (i == organ)
                        continue;
                    ISnappable other = body.Organs[i].GetComponent<ISnappable>();
                    if (other != null && other.IsValid && Vector3.Distance(homeCentre + offset, other.HomeCentrePose.position) <= minOtherDistance)
                        clear = false;
                }
                if (clear)
                {
                    found = offset;
                    return true;
                }
            }
            found = default;
            return false;
        }

        /// <summary>Unit-ish directions to try, each scaled to <paramref name="length"/> metres.</summary>
        public static IEnumerable<Vector3> Directions(float length)
        {
            var axes = new[]
            {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back,
                new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(0f, 1f, 1f), new Vector3(0f, 1f, -1f),
                new Vector3(1f, 0f, 1f), new Vector3(-1f, 0f, -1f), new Vector3(1f, -1f, 0f), new Vector3(-1f, -1f, 0f),
            };
            foreach (Vector3 axis in axes)
                yield return axis.normalized * length;
        }
    }
}
