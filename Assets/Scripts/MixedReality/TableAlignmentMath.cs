using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Pure pose math for lining the virtual table up with a real one. The root of the scene content only ever
    /// moves across the floor plane and turns about the up axis, so it always stays upright.
    /// "Table top local" is the middle of the virtual table top, measured in the root's own space.
    /// </summary>
    public static class TableAlignmentMath
    {
        private const float _minFacingDistance = 0.01f;

        /// <summary>
        /// Root pose that puts the middle of the table top on <paramref name="hitPoint"/> and turns the root so the
        /// viewer stands on the root's negative Z side, looking along its +Z. Directly above the hit, the heading is kept.
        /// </summary>
        public static Pose RootPoseForHit(Vector3 hitPoint, Vector3 tableTopLocal, Vector3 viewerPosition, Quaternion currentRotation)
        {
            Quaternion rotation = FacingRotation(hitPoint, viewerPosition, currentRotation);
            return new Pose(hitPoint - rotation * tableTopLocal, rotation);
        }

        /// <summary>Upright rotation whose forward points from the viewer to the target on the floor plane.</summary>
        public static Quaternion FacingRotation(Vector3 target, Vector3 viewerPosition, Quaternion fallback)
        {
            Vector3 toTarget = target - viewerPosition;
            toTarget.y = 0f;
            if (toTarget.magnitude < _minFacingDistance)
                return UprightYaw(fallback);
            return Quaternion.LookRotation(toTarget, Vector3.up);
        }

        /// <summary>Keeps only the heading of a rotation.</summary>
        public static Quaternion UprightYaw(Quaternion rotation)
        {
            return Quaternion.AngleAxis(TwistAboutUp(rotation), Vector3.up);
        }

        /// <summary>Same pose with the table top lifted or lowered to <paramref name="worldHeight"/>.</summary>
        public static Pose WithTableTopAtHeight(Pose rootPose, Vector3 tableTopLocal, float worldHeight)
        {
            Vector3 topNow = rootPose.position + rootPose.rotation * tableTopLocal;
            return new Pose(rootPose.position + Vector3.up * (worldHeight - topNow.y), rootPose.rotation);
        }

        /// <summary>
        /// Root pose while a handle is carried: the root slides with the handle across the floor plane and turns with
        /// the handle's twist about the up axis, pivoting around the handle. Height, tilt and roll of the hand are ignored.
        /// </summary>
        public static Pose RootPoseFromHandle(Pose handleAtGrab, Pose handleNow, Pose rootAtGrab)
        {
            float twist = TwistAboutUp(handleNow.rotation * Quaternion.Inverse(handleAtGrab.rotation));
            Quaternion turn = Quaternion.AngleAxis(twist, Vector3.up);

            Vector3 handleOffset = rootAtGrab.position - handleAtGrab.position;
            handleOffset.y = 0f;
            Vector3 position = new Vector3(handleNow.position.x, rootAtGrab.position.y, handleNow.position.z) + turn * handleOffset;
            return new Pose(position, turn * rootAtGrab.rotation);
        }

        /// <summary>Signed turn in degrees about the up axis that the rotation contains.</summary>
        public static float TwistAboutUp(Quaternion rotation)
        {
            if (Mathf.Approximately(rotation.y, 0f) && Mathf.Approximately(rotation.w, 0f))
                return 0f;
            return Mathf.DeltaAngle(0f, 2f * Mathf.Atan2(rotation.y, rotation.w) * Mathf.Rad2Deg);
        }

        /// <summary>True when the surface normal is within <paramref name="maxTiltDegrees"/> of straight up.</summary>
        public static bool IsLevel(Vector3 normal, float maxTiltDegrees)
        {
            return normal.sqrMagnitude > 0f && Vector3.Angle(normal, Vector3.up) <= maxTiltDegrees;
        }

        /// <summary>
        /// Picks the closest ray cast hit that is a level surface at about the reference height, so a floor far below
        /// or a ceiling above is never mistaken for the table. A hit's up axis is the surface normal.
        /// </summary>
        public static bool TryPickSurface(IReadOnlyList<Pose> hitsNearestFirst, float referenceHeight, float heightRange, float maxTiltDegrees, out Vector3 point)
        {
            for (int i = 0; i < hitsNearestFirst.Count; i++)
            {
                Pose hit = hitsNearestFirst[i];
                if (!IsLevel(hit.up, maxTiltDegrees) || Mathf.Abs(hit.position.y - referenceHeight) > heightRange)
                    continue;
                point = hit.position;
                return true;
            }
            point = default;
            return false;
        }

        /// <summary>Index of the candidate closest to the viewer across the floor plane, or -1 when there are none.</summary>
        public static int NearestIndex(IReadOnlyList<Vector3> candidates, Vector3 viewerPosition)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 flat = candidates[i] - viewerPosition;
                flat.y = 0f;
                if (flat.sqrMagnitude < bestDistance)
                {
                    bestDistance = flat.sqrMagnitude;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>True when the handle was turned on purpose, so the viewer-facing heading should not overwrite it.</summary>
        public static bool IsDeliberateTurn(Quaternion handleAtGrab, Quaternion handleAtRelease, float thresholdDegrees)
        {
            return Mathf.Abs(TwistAboutUp(handleAtRelease * Quaternion.Inverse(handleAtGrab))) > thresholdDegrees;
        }
    }
}
