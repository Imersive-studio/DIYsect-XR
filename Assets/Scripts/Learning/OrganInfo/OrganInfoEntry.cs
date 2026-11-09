using System;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// What the card says about one organ: its name, what it does and one short fact. It is plain data. The entry is
    /// found by <see cref="OrganName"/>, the name of the organ's GameObject in the body (the model file name, for example
    /// "kidney_l"). <see cref="DisplayName"/> is the same words people see on the floating name label.
    /// </summary>
    [Serializable]
    public class OrganInfoEntry
    {
        [SerializeField, Tooltip("The organ's GameObject name in the body, for example kidney_l.")]
        private string _organName;
        [SerializeField, Tooltip("The name shown to people, for example Left kidney.")]
        private string _displayName;
        [SerializeField, TextArea(2, 4), Tooltip("One sentence about what the organ does. Keep it under 120 characters so it fits the card.")]
        private string _function;
        [SerializeField, TextArea(2, 4), Tooltip("One short true fact about the organ. Keep it under 120 characters so it fits the card.")]
        private string _fact;

        public OrganInfoEntry()
        {
        }

        public OrganInfoEntry(string organName, string displayName, string function, string fact)
        {
            _organName = organName;
            _displayName = displayName;
            _function = function;
            _fact = fact;
        }

        public string OrganName => _organName;
        public string DisplayName => _displayName;
        public string Function => _function;
        public string Fact => _fact;

        /// <summary>True when all four fields have text.</summary>
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(_organName)
            && !string.IsNullOrWhiteSpace(_displayName)
            && !string.IsNullOrWhiteSpace(_function)
            && !string.IsNullOrWhiteSpace(_fact);
    }
}
