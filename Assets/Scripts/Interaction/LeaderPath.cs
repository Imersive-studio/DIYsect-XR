using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>Cuts a polyline short so a leader line can draw out from its first point and retract back toward it. Pure maths.</summary>
    public static class LeaderPath
    {
        /// <summary>
        /// Writes the first <paramref name="fraction"/> (0 to 1) of <paramref name="path"/>, measured by length, into
        /// <paramref name="output"/> and returns how many points it wrote. 0 at fraction 0, otherwise at least 2.
        /// <paramref name="output"/> must hold at least as many points as the path.
        /// </summary>
        public static int Trim(IReadOnlyList<Vector3> path, float fraction, Vector3[] output)
        {
            fraction = Mathf.Clamp01(fraction);
            if (path.Count < 2 || fraction <= 0f)
                return 0;
            float total = 0f;
            for (int i = 1; i < path.Count; i++)
                total += Vector3.Distance(path[i - 1], path[i]);
            float remaining = total * fraction;
            output[0] = path[0];
            int count = 1;
            for (int i = 1; i < path.Count; i++)
            {
                float length = Vector3.Distance(path[i - 1], path[i]);
                if (remaining >= length)
                {
                    output[count++] = path[i];
                    remaining -= length;
                    continue;
                }
                float t = length > 1e-6f ? remaining / length : 0f;
                output[count++] = Vector3.Lerp(path[i - 1], path[i], t);
                break;
            }
            return count;
        }
    }
}
