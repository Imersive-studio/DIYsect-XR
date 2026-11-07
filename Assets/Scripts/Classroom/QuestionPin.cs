using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// One question pin. The point is in the local space of the organ at <see cref="OrganIndex"/>
    /// (index into SharedBody.Organs), so the pin stays on the organ when the organ moves.
    /// <see cref="Id"/> is a server-assigned sequence number and doubles as the creation order.
    /// </summary>
    public struct QuestionPin : INetworkSerializable, IEquatable<QuestionPin>
    {
        public int Id;
        public int OrganIndex;
        public Vector3 LocalPoint;
        public ulong AuthorClientId;
        public FixedString64Bytes Text;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref OrganIndex);
            serializer.SerializeValue(ref LocalPoint);
            serializer.SerializeValue(ref AuthorClientId);
            serializer.SerializeValue(ref Text);
        }

        public bool Equals(QuestionPin other)
        {
            return Id == other.Id && OrganIndex == other.OrganIndex && LocalPoint == other.LocalPoint
                && AuthorClientId == other.AuthorClientId && Text.Equals(other.Text);
        }

        public override bool Equals(object obj)
        {
            return obj is QuestionPin other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
