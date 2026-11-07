// Authority: the server writes pins; clients read them.
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
        private int _id;
        private int _organIndex;
        private Vector3 _localPoint;
        private ulong _authorClientId;
        private FixedString64Bytes _text;

        public QuestionPin(int id, int organIndex, Vector3 localPoint, ulong authorClientId, FixedString64Bytes text)
        {
            _id = id;
            _organIndex = organIndex;
            _localPoint = localPoint;
            _authorClientId = authorClientId;
            _text = text;
        }

        public int Id => _id;
        public int OrganIndex => _organIndex;
        public Vector3 LocalPoint => _localPoint;
        public ulong AuthorClientId => _authorClientId;
        public FixedString64Bytes Text => _text;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _id);
            serializer.SerializeValue(ref _organIndex);
            serializer.SerializeValue(ref _localPoint);
            serializer.SerializeValue(ref _authorClientId);
            serializer.SerializeValue(ref _text);
        }

        public bool Equals(QuestionPin other)
        {
            return _id == other._id && _organIndex == other._organIndex && _localPoint == other._localPoint
                && _authorClientId == other._authorClientId && _text.Equals(other._text);
        }

        public override bool Equals(object obj)
        {
            return obj is QuestionPin other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _id;
        }
    }
}
