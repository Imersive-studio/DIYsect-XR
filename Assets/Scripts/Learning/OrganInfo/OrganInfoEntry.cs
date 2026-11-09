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

        [SerializeField, Tooltip("The body system, shown under the name on the hover label, for example Urinary system.")]
        private string _system;
        [SerializeField, Tooltip("What the organ does in two to four words, shown on the callout, for example Filters blood.")]
        private string _role;

        public OrganInfoEntry()
        {
        }

        public OrganInfoEntry(string organName, string displayName, string function, string fact, string system = null, string role = null)
        {
            _organName = organName;
            _displayName = displayName;
            _function = function;
            _fact = fact;
            _system = system;
            _role = role;
        }

        public string OrganName => _organName;
        public string DisplayName => _displayName;
        public string Function => _function;
        public string Fact => _fact;
        public string System => _system;
        public string Role => _role;

        /// <summary>True when all six fields have text.</summary>
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(_organName)
            && !string.IsNullOrWhiteSpace(_displayName)
            && !string.IsNullOrWhiteSpace(_function)
            && !string.IsNullOrWhiteSpace(_fact)
            && !string.IsNullOrWhiteSpace(_system)
            && !string.IsNullOrWhiteSpace(_role);
    }
}
