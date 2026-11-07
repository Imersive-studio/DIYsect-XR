// No network state: a plain class that groups pins the same way on every client.
using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Groups pins into clusters. Pins join the first cluster on the same organ whose centre is within the radius,
    /// otherwise they start a new one. Pins on different organs never merge. The result depends only on the
    /// pin order, and every client sees the same order, so every client builds the same clusters.
    /// </summary>
    public class PinClusterer
    {
        /// <summary>Cluster size at which the glow is at its maximum.</summary>
        public const int FullGlowCount = 6;

        private readonly float _radius;

        public PinClusterer(float radius)
        {
            _radius = radius;
        }

        public List<PinCluster> Build(IEnumerable<QuestionPin> pins)
        {
            var clusters = new List<PinCluster>();
            foreach (QuestionPin pin in pins)
            {
                PinCluster target = null;
                foreach (PinCluster cluster in clusters)
                {
                    if (cluster.OrganIndex == pin.OrganIndex && Vector3.Distance(cluster.Centre, pin.LocalPoint) <= _radius)
                    {
                        target = cluster;
                        break;
                    }
                }
                if (target == null)
                {
                    target = new PinCluster(pin.OrganIndex);
                    clusters.Add(target);
                }
                target.Add(pin.Id, pin.LocalPoint);
            }
            return clusters;
        }

        /// <summary>Glow from 0 (one question) to 1 (<see cref="FullGlowCount"/> or more). Grows with every extra question.</summary>
        public static float Glow(int count)
        {
            return Mathf.Clamp01((count - 1) / (float)(FullGlowCount - 1));
        }
    }
}
