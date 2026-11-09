namespace DIYsect.MixedReality
{
    /// <summary>The one calculation behind the student's fixed eye height, kept free of Unity so a test can check it.</summary>
    public static class EyeHeightMath
    {
        /// <summary>
        /// The camera offset that puts the eyes at <paramref name="eyeHeight"/> metres above the scene floor, given how
        /// high the head is above the rig origin without any offset (the tracked height). A head 1.0 m above the origin
        /// needs an offset of 0.6 m for eyes at 1.6 m. A head already at 1.6 m needs none.
        /// </summary>
        public static float OffsetFor(float eyeHeight, float trackedHeadHeight)
        {
            return eyeHeight - trackedHeadHeight;
        }

        /// <summary>
        /// How far to move the eyes this frame for a thumbstick push. Pushing up raises them, pushing down lowers them,
        /// at <paramref name="speed"/> metres a second. A push inside the dead zone does nothing, and the result never
        /// leaves the range <paramref name="min"/> to <paramref name="max"/>.
        /// </summary>
        public static float AdjustmentFor(float eyeHeight, float stickY, float deltaTime, float speed, float min, float max, float deadZone = 0.6f)
        {
            if (float.IsNaN(stickY) || System.Math.Abs(stickY) < deadZone)
                return 0f;
            float wanted = stickY * speed * deltaTime;
            float target = System.Math.Max(min, System.Math.Min(max, eyeHeight + wanted));
            return target - eyeHeight;
        }

        /// <summary>False for a value the runtime has not set yet (not a number or infinite).</summary>
        public static bool IsUsable(float trackedHeadHeight)
        {
            return !float.IsNaN(trackedHeadHeight) && !float.IsInfinity(trackedHeadHeight);
        }
    }
}
