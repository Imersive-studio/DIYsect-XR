// Authority: none. Data and a pose setter; only the server calls SnapHome (through OrganSocketKeeper).
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Remembers where this organ belongs. The home pose and the mesh centre are recorded in the prefab by
    /// Tools > DIYsect > Add Organ Sockets, so the socket is exactly where the organ was built.
    /// </summary>
    public class SnappableOrgan : MonoBehaviour, ISnappable
    {
        [SerializeField] private Vector3 _homeLocalPosition;
        [SerializeField] private Quaternion _homeLocalRotation = Quaternion.identity;
        [SerializeField, Tooltip("Middle of the organ's mesh, in the organ's own space.")]
        private Vector3 _centreOffset;

        public bool IsValid => enabled && IsUsable(_homeLocalRotation) && IsFinite(_homeLocalPosition) && IsFinite(_centreOffset);

        public Pose HomeRootPose => new Pose(_homeLocalPosition, _homeLocalRotation);

        public Pose HomeCentrePose => CentreOf(HomeRootPose);

        public Pose CurrentCentrePose => CentreOf(new Pose(transform.localPosition, transform.localRotation));

        private void Awake()
        {
            if (!IsUsable(_homeLocalRotation) || !IsFinite(_homeLocalPosition) || !IsFinite(_centreOffset))
            {
                Debug.LogError($"{name}: SnappableOrgan has has an unusable home pose. Run Tools > DIYsect > Add Organ Sockets.", this);
                enabled = false;
            }
        }

        private static bool IsUsable(Quaternion q)
        {
            return float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w)
                && q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 1e-6f;
        }

        private static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }

        public void SnapHome()
        {
            if (!enabled)
                return;
            transform.SetLocalPositionAndRotation(_homeLocalPosition, _homeLocalRotation);
        }

        private Pose CentreOf(Pose root)
        {
            return new Pose(root.position + root.rotation * _centreOffset, root.rotation);
        }
    }
}
