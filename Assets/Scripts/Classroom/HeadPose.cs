using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>A head pose in the body's local space, or nothing when that person is not in the session.</summary>
    public struct HeadPose : INetworkSerializable, IEquatable<HeadPose>
    {
        public bool Present;
        public Vector3 Position;
        public Quaternion Rotation;

        public static HeadPose None => new HeadPose { Present = false, Position = Vector3.zero, Rotation = Quaternion.identity };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Present);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
        }

        public bool Equals(HeadPose other)
        {
            return Present == other.Present && Position == other.Position && Rotation == other.Rotation;
        }

        public override bool Equals(object obj) => obj is HeadPose other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Present, Position, Rotation);
    }
}
