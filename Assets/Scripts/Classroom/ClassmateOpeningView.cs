// Authority: none. Pure local numbers for where the classmate's camera starts.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The classmate camera's first view of the body: a little farther back and lower than looking straight down at the table,
    /// so the phone shows the lecture hall, the seated class and the body together. The camera starts on the foot side of the body.
    /// </summary>
    public static class ClassmateOpeningView
    {
        /// <summary>How steeply the camera looks down at the body, in degrees (0 is level, 90 is straight down).</summary>
        public const float Pitch = 28f;

        /// <summary>How far the camera starts from the body, in metres.</summary>
        public const float Distance = 1.6f;

        /// <summary>Where the camera starts, relative to the body it looks at.</summary>
        public static Vector3 Offset()
        {
            OrbitMath.Pose(Vector3.zero, 0f, Pitch, Distance, out Vector3 position, out _);
            return position;
        }
    }
}
