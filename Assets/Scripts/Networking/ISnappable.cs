using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Something that has one home socket and can be put back in it. All poses are in the local space of its parent.
    /// "Centre" poses use the middle of the organ's mesh as the position, because the organs are modelled in place
    /// and their transforms all sit at the same point.
    /// </summary>
    public interface ISnappable
    {
        /// <summary>The pose of the transform when the organ sits in its socket.</summary>
        Pose HomeRootPose { get; }

        /// <summary>The mesh centre and rotation when the organ sits in its socket.</summary>
        Pose HomeCentrePose { get; }

        /// <summary>The mesh centre and rotation right now.</summary>
        Pose CurrentCentrePose { get; }

        /// <summary>Puts the organ at <see cref="HomeRootPose"/>.</summary>
        void SnapHome();
    }
}
