// Authority: none. Local presentation only; the server owns the pins it describes.
using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>Marks one pin cluster on an organ. It is a child of the organ, so it follows the organ.</summary>
    public class PinMarker : MonoBehaviour
    {
        public int OrganIndex { get; private set; }
        public int Count { get; private set; }
        public int OpenCount { get; private set; }
        public bool AllAnswered { get; private set; }
        public float Glow { get; private set; }
        public IReadOnlyList<int> PinIds { get; private set; }

        public void Show(PinCluster cluster, float glow)
        {
            OrganIndex = cluster.OrganIndex;
            Count = cluster.Count;
            OpenCount = cluster.OpenCount;
            AllAnswered = cluster.AllAnswered;
            Glow = glow;
            PinIds = cluster.PinIds;
        }
    }
}
