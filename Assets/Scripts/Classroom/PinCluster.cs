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

        /// <summary>Pins in the cluster that are not answered yet. Only these make the cluster glow.</summary>
        public int OpenCount { get; private set; }

        /// <summary>True when every pin in the cluster is answered.</summary>
        public bool AllAnswered => Count > 0 && OpenCount == 0;
        public IReadOnlyList<int> PinIds => _pinIds;
        public Vector3 Centre => _sum / Mathf.Max(1, _pinIds.Count);

        public void Add(int pinId, Vector3 localPoint, bool answered = false)
        {
            if (!answered)
                OpenCount++;
            _pinIds.Add(pinId);
            _sum += localPoint;
        }
    }
}
