using System.Collections.Generic;
using DIYsect.Interaction;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The text for every organ card, kept in one asset so it can be read and corrected without touching code.
    /// Human anatomy only. Create it with the menu DIYsect > Organ Info Library, or use the filled-in
    /// OrganInfoLibrary.asset next to this script. Look an organ up with <see cref="TryGet"/>.
    /// It is also the <see cref="OrganCaptions"/> the organ labels read (name, body system and role).
    /// </summary>
    [CreateAssetMenu(fileName = "OrganInfoLibrary", menuName = "DIYsect/Organ Info Library")]
    public class OrganInfoLibrary : OrganCaptions
    {
        [SerializeField] private List<OrganInfoEntry> _entries = new List<OrganInfoEntry>();

        private OrganInfoLookup _lookup;

        public IReadOnlyList<OrganInfoEntry> Entries => _entries;

        /// <summary>Gives the entry for the organ's GameObject name, or false when there is none.</summary>
        public bool TryGet(string organName, out OrganInfoEntry entry)
        {
            if (_lookup == null)
                _lookup = new OrganInfoLookup(_entries);
            return _lookup.TryGet(organName, out entry);
        }

        /// <summary>Gives the label words (name, body system, role) for the organ, or false when there is no entry.</summary>
        public override bool TryGetCaption(string organObjectName, out OrganCaption caption)
        {
            if (TryGet(organObjectName, out OrganInfoEntry entry))
            {
                caption = new OrganCaption(entry.DisplayName, entry.System, entry.Role);
                return true;
            }
            caption = default;
            return false;
        }

        // The list can be edited in the Inspector, so the lookup is rebuilt the next time it is needed.
        private void OnValidate()
        {
            _lookup = null;
            foreach (OrganInfoEntry entry in _entries)
                if (entry == null || !entry.IsComplete)
                    Debug.LogError($"{name}: an organ entry has an empty field. Every entry needs an organ name, a display name, a function, a fact, a body system and a role.", this);
        }
    }
}
