using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>A group of nearby pins on one organ. The centre is in the organ's local space.</summary>
    public class PinCluster
    {
        private readonly List<int> _pinIds = new List<int>();
        private Vector3 _sum;

        public PinCluster(int organIndex)
        {
            OrganIndex = organIndex;
        }

        public int OrganIndex { get; }
        public int Count => _pinIds.Count;
        public IReadOnlyList<int> PinIds => _pinIds;
        public Vector3 Centre => _sum / Mathf.Max(1, _pinIds.Count);

        public void Add(int pinId, Vector3 localPoint)
        {
            _pinIds.Add(pinId);
            _sum += localPoint;
        }
    }
}
