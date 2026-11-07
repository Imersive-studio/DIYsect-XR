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
    /// The pin may also carry the viewpoint of the classmate who asked it, in the local space of the QuestionBoard's
    /// transform (the body root), so the view follows the body. A default (all zero) rotation means no view.
    /// <see cref="Id"/> is a server-assigned sequence number and doubles as the creation order.
    /// </summary>
    public struct QuestionPin : INetworkSerializable, IEquatable<QuestionPin>
    {
        private int _id;
        private int _organIndex;
        private Vector3 _localPoint;
        private ulong _authorClientId;
        private FixedString64Bytes _text;
        private Vector3 _viewPosition;
        private Quaternion _viewRotation;

        /// <summary>A pin without a view.</summary>
        public QuestionPin(int id, int organIndex, Vector3 localPoint, ulong authorClientId, FixedString64Bytes text)
            : this(id, organIndex, localPoint, authorClientId, text, Vector3.zero, default)
        {
        }

        /// <summary>A pin with the asker's view, both in the body root's local space.</summary>
        public QuestionPin(int id, int organIndex, Vector3 localPoint, ulong authorClientId, FixedString64Bytes text,
            Vector3 viewPosition, Quaternion viewRotation)
        {
            _id = id;
            _organIndex = organIndex;
            _localPoint = localPoint;
            _authorClientId = authorClientId;
            _text = text;
            _viewPosition = viewPosition;
            _viewRotation = viewRotation;
        }

        public int Id => _id;
        public int OrganIndex => _organIndex;
        public Vector3 LocalPoint => _localPoint;
        public ulong AuthorClientId => _authorClientId;
        public FixedString64Bytes Text => _text;

        /// <summary>Where the asker's camera was, in the body root's local space. Only meaningful when <see cref="HasView"/>.</summary>
        public Vector3 ViewPosition => _viewPosition;

        /// <summary>How the asker's camera was turned, in the body root's local space. Only meaningful when <see cref="HasView"/>.</summary>
        public Quaternion ViewRotation => _viewRotation;

        /// <summary>True when the pin carries a view (the stored rotation is a valid, non-zero quaternion).</summary>
        public bool HasView => PinRules.IsUsableRotation(_viewRotation);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _id);
            serializer.SerializeValue(ref _organIndex);
            serializer.SerializeValue(ref _localPoint);
            serializer.SerializeValue(ref _authorClientId);
            serializer.SerializeValue(ref _text);
            serializer.SerializeValue(ref _viewPosition);
            serializer.SerializeValue(ref _viewRotation);
        }

        public bool Equals(QuestionPin other)
        {
            return _id == other._id && _organIndex == other._organIndex && _localPoint == other._localPoint
                && _authorClientId == other._authorClientId && _text.Equals(other._text)
                && _viewPosition == other._viewPosition
                // Quaternion == compares by dot product, so two "no view" (all zero) rotations would never be equal.
                && _viewRotation.Equals(other._viewRotation);
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
