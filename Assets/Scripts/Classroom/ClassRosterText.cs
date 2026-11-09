using System;
using System.Collections.Generic;
using System.Text;

namespace DIYsect.Classroom
{
    /// <summary>What a student is using to take part.</summary>
    public enum RosterKind
    {
        /// <summary>A headset, such as a student in the VR lecture hall.</summary>
        Headset,
        /// <summary>A phone or laptop.</summary>
        Screen,
    }

    /// <summary>One student as the professor's roster shows them.</summary>
    public readonly struct RosterEntry
    {
        public RosterEntry(ulong clientId, string name, RosterKind kind, int questions)
        {
            ClientId = clientId;
            Name = name ?? string.Empty;
            Kind = kind;
            Questions = questions;
        }

        public ulong ClientId { get; }
        public string Name { get; }
        public RosterKind Kind { get; }
        public int Questions { get; }
    }

    /// <summary>
    /// Turns the connected students into the text of the professor's roster panel. Pure text work with no network or
    /// scene access, so it is tested on its own.
    /// </summary>
    public static class ClassRosterText
    {
        /// <summary>Students listed before the rest are summarised.</summary>
        public const int MaxLines = 8;
        /// <summary>Longest name shown in full.</summary>
        public const int MaxNameLength = 16;

        public static string Format(IReadOnlyList<RosterEntry> entries)
        {
            int count = entries?.Count ?? 0;
            var text = new StringBuilder();
            text.Append(count == 0 ? "Class: no students yet" : count == 1 ? "Class: 1 student" : $"Class: {count} students");
            if (count == 0)
                return text.ToString();

            var sorted = new List<RosterEntry>(entries);
            sorted.Sort((a, b) =>
            {
                int byName = string.Compare(Clean(a.Name), Clean(b.Name), StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : a.ClientId.CompareTo(b.ClientId);
            });

            int shown = Math.Min(sorted.Count, MaxLines);
            for (int i = 0; i < shown; i++)
            {
                RosterEntry entry = sorted[i];
                text.Append('\n').Append(Label(entry, sorted)).Append("  ").Append(KindLabel(entry.Kind));
                if (entry.Questions > 0)
                    text.Append("  ").Append(entry.Questions == 1 ? "1 question" : $"{entry.Questions} questions");
            }
            if (sorted.Count > shown)
                text.Append('\n').Append($"+{sorted.Count - shown} more");
            return text.ToString();
        }

        /// <summary>The name shown for a student. A name that two students share gets the client id added.</summary>
        public static string Label(RosterEntry entry, IReadOnlyList<RosterEntry> all)
        {
            string name = Clean(entry.Name);
            if (name.Length == 0)
                name = "Student";
            if (name.Length > MaxNameLength)
                name = name.Substring(0, MaxNameLength - 1) + "…";
            int sameName = 0;
            foreach (RosterEntry other in all)
                if (string.Equals(Clean(other.Name), Clean(entry.Name), StringComparison.OrdinalIgnoreCase))
                    sameName++;
            return sameName > 1 ? $"{name} #{entry.ClientId}" : name;
        }

        public static string KindLabel(RosterKind kind) => kind == RosterKind.Headset ? "headset" : "phone/laptop";

        // Names come from other players, and TextMesh reads <b>-style tags, so angle brackets and line breaks go.
        private static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;
            return name.Replace("<", string.Empty).Replace(">", string.Empty).Replace("\n", " ").Replace("\r", " ").Trim();
        }
    }
}
