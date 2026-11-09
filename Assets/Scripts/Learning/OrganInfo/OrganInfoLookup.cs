using System;
using System.Collections.Generic;

namespace DIYsect.Learning
{
    /// <summary>
    /// Finds an <see cref="OrganInfoEntry"/> by organ object name. Plain C#, so it is easy to test. The match ignores
    /// capital letters and spaces around the name. Entries with no organ name are skipped; if two entries share a name
    /// the later one wins.
    /// </summary>
    public sealed class OrganInfoLookup
    {
        private readonly Dictionary<string, OrganInfoEntry> _byName = new Dictionary<string, OrganInfoEntry>(StringComparer.OrdinalIgnoreCase);

        public OrganInfoLookup(IEnumerable<OrganInfoEntry> entries)
        {
            if (entries == null)
                return;
            foreach (OrganInfoEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.OrganName))
                    continue;
                _byName[entry.OrganName.Trim()] = entry;
            }
        }

        /// <summary>How many different organ names have an entry.</summary>
        public int Count => _byName.Count;

        /// <summary>Gives the entry for the organ, or false (and a null entry) when the name is empty or unknown.</summary>
        public bool TryGet(string organName, out OrganInfoEntry entry)
        {
            entry = null;
            if (string.IsNullOrWhiteSpace(organName))
                return false;
            return _byName.TryGetValue(organName.Trim(), out entry);
        }
    }
}
